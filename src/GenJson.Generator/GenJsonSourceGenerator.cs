using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenJson.Generator;

public abstract record GenJsonDataType
{
    public sealed record Primitive(string TypeName) : GenJsonDataType; // Integers

    public sealed record Boolean : GenJsonDataType
    {
        public static readonly Boolean Instance = new();
        private Boolean() { }
    }

    public sealed record FloatingPoint(string TypeName) : GenJsonDataType; // Float, Double, Decimal

    public sealed record String : GenJsonDataType
    {
        public static readonly String Instance = new();
        private String() { }
    }

    public sealed record Char : GenJsonDataType
    {
        public static readonly Char Instance = new();
        private Char() { }
    }

    public sealed record Guid : GenJsonDataType
    {
        public static readonly Guid Instance = new();
        private Guid() { }
    }

    public sealed record DateTime : GenJsonDataType
    {
        public static readonly DateTime Instance = new();
        private DateTime() { }
    }

    public sealed record TimeSpan : GenJsonDataType
    {
        public static readonly TimeSpan Instance = new();
        private TimeSpan() { }
    }

    public sealed record DateTimeOffset : GenJsonDataType
    {
        public static readonly DateTimeOffset Instance = new();
        private DateTimeOffset() { }
    }

    public sealed record Version : GenJsonDataType
    {
        public static readonly Version Instance = new();
        private Version() { }
    }

    public sealed record Object(string TypeName) : GenJsonDataType; // Another GenJson class

    public sealed record Nullable(GenJsonDataType Underlying) : GenJsonDataType;
    public sealed record Enumerable(GenJsonDataType ElementType, bool IsArray, string ConstructionTypeName, string ElementTypeName, bool IsElementValueType) : GenJsonDataType;
    public sealed record Dictionary(GenJsonDataType KeyType, GenJsonDataType ValueType, string ConstructionTypeName, string KeyTypeName, string ValueTypeName, bool IsValueValueType) : GenJsonDataType;
    public sealed record Enum(string TypeName, bool AsString, string UnderlyingType, string? FallbackValue, EquatableList<string> Members) : GenJsonDataType;
    public sealed record CustomConverter(string ConverterTypeName) : GenJsonDataType;
}

public record PropertyData(string Name, string JsonName, string TypeName, bool IsNullable, bool IsValueType, GenJsonDataType Type);

public record ClassData(
    string ClassName,
    string Namespace,
    EquatableList<PropertyData> ConstructorArgs,
    EquatableList<PropertyData> InitProperties,
    EquatableList<PropertyData> AllProperties,
    string Keyword,
    bool IsAbstract,
    bool HasGenJsonBase,
    bool IsNullableContext,
    string? PolymorphicDiscriminatorProp,
    bool SkipCountOptimization,
    EquatableList<DerivedTypeData> DerivedTypes);

public record DerivedTypeData(string TypeName, string DiscriminatorValue, bool IsIntDiscriminator);

[Generator]
public class GenJsonSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classData = context.SyntaxProvider
            .CreateSyntaxProvider(IsSyntaxNodeValid, GetClassData)
            .Where(x => x is not null);

        context.RegisterSourceOutput(classData, Generate!);
    }

    private static bool IsSyntaxNodeValid(SyntaxNode node, CancellationToken ct)
    {
        if (node is not TypeDeclarationSyntax typeDecl)
        {
            return false;
        }

        if (typeDecl.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return false;
        }

        return typeDecl.AttributeLists.Count > 0;
    }

    private static ClassData? GetClassData(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        if (!typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return null;
        }

        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);
        if (typeSymbol is null)
        {
            return null;
        }

        if (!typeSymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonAttribute"))
        {
            return null;
        }

        var properties = new List<PropertyData>();
        var propertiesMap = new Dictionary<string, (PropertyData Data, IPropertySymbol Symbol)>(StringComparer.OrdinalIgnoreCase);

        var currentType = typeSymbol;
        var typeHierarchy = new Stack<INamedTypeSymbol>();
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            typeHierarchy.Push(currentType);
            currentType = currentType.BaseType;
        }

        foreach (var t in typeHierarchy)
        {
            foreach (var member in t.GetMembers())
            {
                if (member is IPropertySymbol propertySymbol &&
                    propertySymbol.DeclaredAccessibility == Accessibility.Public &&
                    !propertySymbol.IsStatic)
                {
                    bool isNullable = propertySymbol.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
                                      propertySymbol.NullableAnnotation == NullableAnnotation.Annotated ||
                                      (!propertySymbol.Type.IsValueType && propertySymbol.NullableAnnotation == NullableAnnotation.None);

                    var type = GetGenJsonDataType(propertySymbol, null, propertySymbol.Type);
                    bool isValueType = propertySymbol.Type.IsValueType;

                    var propName = GetJsonName(propertySymbol, null);
                    var propData = new PropertyData(propertySymbol.Name, propName, propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), isNullable, isValueType, type);

                    if (propertiesMap.TryGetValue(propertySymbol.Name, out var existing))
                    {
                        properties.Remove(existing.Data);
                    }

                    properties.Add(propData);
                    propertiesMap[propertySymbol.Name] = (propData, propertySymbol);
                }
            }
        }

        var constructorArgs = new List<PropertyData>();
        var utilizedPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (typeDeclaration is RecordDeclarationSyntax)
        {
            var ctor = typeSymbol.Constructors
                .OrderByDescending(c => c.Parameters.Length)
                .FirstOrDefault();
            if (ctor is { Parameters.Length: > 0 })
            {
                foreach (var param in ctor.Parameters)
                {
                    var propData = GetPropertyForParameter(param, propertiesMap, context.SemanticModel.Compilation);

                    if (propData != null)
                    {
                        // We found a matching property (either direct or via base)
                        // Re-evaluate types/names in context of the parameter
                        var originalSymbol = propertiesMap[propData.Name].Symbol;

                        var newType = GetGenJsonDataType(originalSymbol, param, originalSymbol.Type);
                        // IMPORTANT: For inherited parameters like "C" mapped to "A", we want to use the parameter's attributes if any, 
                        // BUT if the parameter is just a pass-through (like C -> A), and C has NO attributes, we want A's JSON name.
                        // GetJsonName checks param first, then property.
                        // If param C has no attributes, it returns "C" (param.Name) by default in some logic? 
                        // Wait, GetJsonName(prop, param): checks Attr on Prop. checks Attr on Param. 
                        // If neither, returns prop.Name.
                        // So for C -> A mapping: Prop is A. Param is C.
                        // If C has no Attr, GetJsonName returns "A". Perfect.
                        var newJsonName = GetJsonName(originalSymbol, param);

                        var newPropData = propData with { Type = newType, JsonName = newJsonName };

                        // If param name differs from property name (mapping), remove the param-named property if it exists
                        // This prevents serializing 'C' when it just maps to base 'A'
                        if (param.Name != propData.Name && propertiesMap.TryGetValue(param.Name, out var selfProp))
                        {
                            properties.Remove(selfProp.Data);
                        }

                        // Update the main list with new data to keep consistency
                        var index = properties.IndexOf(propData);
                        if (index >= 0)
                        {
                            properties[index] = newPropData;
                        }

                        // Add to constructor args with the NAME OF THE PROPERTY (because we parse properties by JSON key -> _PropName)
                        // Yes, we need to pass _PropName to constructor.
                        // But wait.
                        // If we map Param C -> Property A.
                        // We parse JSON key "A" -> variable `_A`.
                        // Constructor expects `_A`.
                        // But generated code loop:
                        // foreach(var arg in data.ConstructorArgs.Value) { append("_" + arg.Name) }
                        // arg.Name must be "A".
                        // newPropData.Name is "A". 
                        // Correct.

                        constructorArgs.Add(newPropData);
                        utilizedPropertyNames.Add(propData.Name);
                    }
                }
            }
        }

        // Filter out ignored properties
        // We do this AFTER processing constructor arguments because we want to keep constructor arguments even if they are ignored or readonly
        // (readonly properties are common in records, and we still need to set them via constructor)
        for (int i = properties.Count - 1; i >= 0; i--)
        {
            var propName = properties[i].Name;
            var propSymbol = propertiesMap[propName].Symbol;

            if (utilizedPropertyNames.Contains(propName)) continue;

            if (propSymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonIgnoreAttribute"))
            {
                properties.RemoveAt(i);
                continue;
            }

            if (propSymbol.IsReadOnly)
            {
                properties.RemoveAt(i);
                continue;
            }
        }

        var initProperties = properties.Where(p => !utilizedPropertyNames.Contains(p.Name)).ToList();

        var keyword = typeDeclaration switch
        {
            RecordDeclarationSyntax recordDeclarationSyntax => recordDeclarationSyntax.ClassOrStructKeyword.Text == "struct"
                ? "record struct"
                : "record class",
            _ => typeDeclaration.Keyword.Text
        };

        var typeNameSpace = typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } ns
            ? ns.ToDisplayString()
            : "";

        var nullableContext = context.SemanticModel.GetNullableContext(typeDeclaration.SpanStart);
        var isNullableContext = (nullableContext & NullableContext.Enabled) == NullableContext.Enabled ||
                                (context.SemanticModel.Compilation.Options.NullableContextOptions == NullableContextOptions.Enable && (nullableContext & NullableContext.Disabled) != NullableContext.Disabled);

        bool hasGenJsonBase = false;
        var checkBase = typeSymbol.BaseType;
        while (checkBase != null && checkBase.SpecialType != SpecialType.System_Object)
        {
            if (HasGenJsonAttribute(checkBase))
            {
                hasGenJsonBase = true;
                break;
            }
            checkBase = checkBase.BaseType;
        }

        string? polymorphicDiscriminatorProp = null;
        var polyAttr = typeSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonPolymorphicAttribute");
        if (polyAttr != null)
        {
            polymorphicDiscriminatorProp = polyAttr.ConstructorArguments.Length > 0 && polyAttr.ConstructorArguments[0].Value is string s ? s : "$type";
        }

        var derivedTypes = new List<DerivedTypeData>();
        foreach (var attr in typeSymbol.GetAttributes().Where(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonDerivedTypeAttribute"))
        {
            if (attr.ConstructorArguments.Length >= 2 && attr.ConstructorArguments[0].Value is ITypeSymbol derivedType && attr.ConstructorArguments[1].Value is object discVal)
            {
                bool isInt = discVal is int or byte or short or long; // or other integer types
                string valStr = isInt ? discVal.ToString() : $"\"{discVal}\"";
                derivedTypes.Add(new DerivedTypeData(derivedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), valStr, isInt));
            }
        }

        bool skipCountOptimization = typeSymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonSkipCountOptimizationAttribute");

        return new ClassData(typeSymbol.Name, typeNameSpace, new EquatableList<PropertyData>(constructorArgs), new EquatableList<PropertyData>(initProperties), new EquatableList<PropertyData>(properties), keyword, typeSymbol.IsAbstract, hasGenJsonBase, isNullableContext, polymorphicDiscriminatorProp, skipCountOptimization, new EquatableList<DerivedTypeData>(derivedTypes));
    }

    private static PropertyData? GetPropertyForParameter(
        IParameterSymbol parameter,
        Dictionary<string, (PropertyData Data, IPropertySymbol Symbol)> propertiesMap,
        Compilation compilation)
    {
        // 1. Check if parameter is passed to base constructor (Prioritize base mapping to resolve renames/redundancy)
        var containingType = parameter.ContainingType;
        if (containingType != null)
        {
            var syntaxRef = containingType.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef != null)
            {
                var typeDecl = syntaxRef.GetSyntax() as RecordDeclarationSyntax;
                if (typeDecl != null && typeDecl.ParameterList != null)
                {
                    // Find index of parameter in declaration
                    int paramIndex = -1;
                    for (int i = 0; i < typeDecl.ParameterList.Parameters.Count; i++)
                    {
                        if (typeDecl.ParameterList.Parameters[i].Identifier.Text == parameter.Name)
                        {
                            paramIndex = i;
                            break;
                        }
                    }

                    if (paramIndex != -1 && typeDecl.BaseList != null)
                    {
                        var baseInit = typeDecl.BaseList.Types.FirstOrDefault(t => t is PrimaryConstructorBaseTypeSyntax) as PrimaryConstructorBaseTypeSyntax;
                        if (baseInit != null)
                        {
                            // Find which argument uses our parameter
                            var args = baseInit.ArgumentList.Arguments;
                            for (int j = 0; j < args.Count; j++)
                            {
                                if (args[j].Expression is IdentifierNameSyntax id && id.Identifier.Text == parameter.Name)
                                {
                                    // This parameter matches base arg at index j
                                    // Find base constructor parameter at index j
                                    var model = compilation.GetSemanticModel(typeDecl.SyntaxTree);
                                    var baseType = model.GetTypeInfo(baseInit.Type).Type as INamedTypeSymbol;

                                    if (baseType != null)
                                    {
                                        var baseCtor = baseType.Constructors.OrderByDescending(c => c.Parameters.Length).FirstOrDefault();
                                        if (baseCtor != null && j < baseCtor.Parameters.Length)
                                        {
                                            var baseParam = baseCtor.Parameters[j];
                                            var baseResult = GetPropertyForParameter(baseParam, propertiesMap, compilation);
                                            if (baseResult != null) return baseResult;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 2. Check if parameter maps directly to a declared property
        if (propertiesMap.TryGetValue(parameter.Name, out var prop))
        {
            return prop.Data;
        }

        return null;
    }

    private static GenJsonDataType GetGenJsonDataType(IPropertySymbol propertySymbol, IParameterSymbol? parameterSymbol, ITypeSymbol type)
    {
        var converterAttr = propertySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute") ??
                            parameterSymbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute");

        if (converterAttr != null && converterAttr.ConstructorArguments.Length > 0 && converterAttr.ConstructorArguments[0].Value is ITypeSymbol converterType)
        {
            return new GenJsonDataType.CustomConverter(converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType)
        {
            var propertyHasAsText = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonEnumAsTextAttribute") ||
                                    (parameterSymbol?.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonEnumAsTextAttribute") ?? false);
            var propertyHasAsNumber = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonEnumAsNumberAttribute") ||
                                      (parameterSymbol?.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonEnumAsNumberAttribute") ?? false);
            var typeHasAsText = type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonEnumAsTextAttribute");

            var fallbackAttr = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonEnumFallbackAttribute");
            string? fallbackValue = null;
            if (fallbackAttr != null && fallbackAttr.ConstructorArguments.Length > 0)
            {
                var arg = fallbackAttr.ConstructorArguments[0];
                if (arg.Value != null && arg.Type != null)
                {
                    fallbackValue = $"({arg.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({arg.Value})";
                }
            }

            var asString = propertyHasAsText || (!propertyHasAsNumber && typeHasAsText);
            var underlyingType = enumType.EnumUnderlyingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "int";
            var members = new List<string>();
            foreach (var member in enumType.GetMembers())
            {
                if (member is IFieldSymbol { ConstantValue: not null })
                {
                    members.Add(member.Name);
                }
            }
            return new GenJsonDataType.Enum(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), asString, underlyingType, fallbackValue, new EquatableList<string>(members));
        }

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            type is INamedTypeSymbol { TypeArguments.Length: > 0 } namedRaw)
        {
            return new GenJsonDataType.Nullable(GetGenJsonDataType(propertySymbol, parameterSymbol, namedRaw.TypeArguments[0]));
        }

        switch (type.SpecialType)
        {
            case SpecialType.System_String:
                return GenJsonDataType.String.Instance;
            case SpecialType.System_Boolean:
                return GenJsonDataType.Boolean.Instance;
            case SpecialType.System_Char:
                return GenJsonDataType.Char.Instance;
            case SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal:
                return new GenJsonDataType.FloatingPoint(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        switch (typeName)
        {
            case "global::System.Guid" or "System.Guid":
                return GenJsonDataType.Guid.Instance;
            case "global::System.DateTime":
            case "System.DateTime":
                return GenJsonDataType.DateTime.Instance;
            case "global::System.TimeSpan":
            case "System.TimeSpan":
                return GenJsonDataType.TimeSpan.Instance;
            case "global::System.DateTimeOffset":
            case "System.DateTimeOffset":
                return GenJsonDataType.DateTimeOffset.Instance;
            case "global::System.Version":
            case "System.Version":
                return GenJsonDataType.Version.Instance;
        }

        if (HasGenJsonAttribute(type))
        {
            return new GenJsonDataType.Object(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        if (TryGetDictionaryTypes(type, out var keyType, out var valueType))
        {
            var keyGenType = GetGenJsonDataType(propertySymbol, parameterSymbol, keyType!);
            var valueGenType = GetGenJsonDataType(propertySymbol, parameterSymbol, valueType!);
            var constructionTypeName = $"global::System.Collections.Generic.Dictionary<{keyType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {valueType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
            return new GenJsonDataType.Dictionary(keyGenType, valueGenType, constructionTypeName, keyType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), valueType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), valueType!.IsValueType);
        }

        ITypeSymbol? resolvedElementType = GetEnumerableElementType(type);
        if (resolvedElementType != null)
        {
            var isArray = type is IArrayTypeSymbol;
            var constructionTypeName = isArray
               ? null // Not used for array
               : $"global::System.Collections.Generic.List<{resolvedElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
            return new GenJsonDataType.Enumerable(GetGenJsonDataType(propertySymbol, parameterSymbol, resolvedElementType), isArray, constructionTypeName!, resolvedElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), resolvedElementType.IsValueType);
        }

        return new GenJsonDataType.Primitive(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static bool TryGetDictionaryTypes(ITypeSymbol type, out ITypeSymbol? keyType, out ITypeSymbol? valueType)
    {
        keyType = null;
        valueType = null;

        if (type is INamedTypeSymbol namedType)
        {
            if (IsDictionary(namedType))
            {
                keyType = namedType.TypeArguments[0];
                valueType = namedType.TypeArguments[1];
                return true;
            }

            // Check interfaces
            var dictInterface = namedType.AllInterfaces.FirstOrDefault(IsDictionary);

            if (dictInterface != null)
            {
                keyType = dictInterface.TypeArguments[0];
                valueType = dictInterface.TypeArguments[1];
                return true;
            }
        }

        return false;
    }

    private static bool IsDictionary(INamedTypeSymbol type)
    {
        return type.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic" &&
               type.OriginalDefinition.Name is "IDictionary" or "IReadOnlyDictionary" &&
               type.TypeParameters.Length == 2;
    }

    private static bool HasGenJsonAttribute(ITypeSymbol type)
    {
        return type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonAttribute");
    }

    private static ITypeSymbol? GetEnumerableElementType(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol arrayType:
                return arrayType.ElementType;

            case INamedTypeSymbol namedTypeSym:
                {
                    if (namedTypeSym.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                    {
                        if (namedTypeSym.TypeArguments.Length > 0)
                        {
                            return namedTypeSym.TypeArguments[0];
                        }
                    }

                    var enumerableInterface = namedTypeSym.AllInterfaces.FirstOrDefault(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
                    if (enumerableInterface != null)
                    {
                        return enumerableInterface.TypeArguments[0];
                    }

                    break;
                }
        }

        return null;
    }

    private static string GetJsonName(IPropertySymbol propertySymbol, IParameterSymbol? parameterSymbol)
    {
        var converterAttr = propertySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonPropertyNameAttribute") ??
                            parameterSymbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonPropertyNameAttribute");

        if (converterAttr is { ConstructorArguments.Length: > 0 } && converterAttr.ConstructorArguments[0].Value is string name)
        {
            return name;
        }

        return propertySymbol.Name;
    }

    private void Generate(SourceProductionContext context, ClassData data)
    {
        var sb = new StringBuilder();

        sb.AppendLine("#nullable enable");
        if (!string.IsNullOrEmpty(data.Namespace))
        {
            sb.Append("namespace ");
            sb.Append(data.Namespace);
            sb.AppendLine();
            sb.AppendLine("{");
        }

        sb.Append("    partial ");
        sb.Append(data.Keyword);
        sb.Append(" ");
        sb.Append(data.ClassName);
        sb.AppendLine();
        sb.AppendLine("    {");



        var allProperties = data.AllProperties.Value;
        var newModifier = data.HasGenJsonBase ? "new " : "";

        sb.Append("        public ");
        sb.Append(newModifier);
        sb.AppendLine("int CalculateJsonSize()");
        sb.AppendLine("        {");
        if (data.DerivedTypes.Value.Count > 0)
        {
            sb.AppendLine("            switch (this)");
            sb.AppendLine("            {");
            int derivedIdx = 0;
            foreach (var derived in data.DerivedTypes.Value)
            {
                derivedIdx++;
                sb.AppendLine("                case " + derived.TypeName + " d" + derivedIdx + ":");
                var discKey = data.PolymorphicDiscriminatorProp ?? "$type";
                int overhead = discKey.Length + 3 + 1;
                overhead += derived.DiscriminatorValue.Length;
                sb.AppendLine("                    return d" + derivedIdx + ".CalculateJsonSize() + " + overhead + ";");
            }
            sb.AppendLine("                default:");
            if (data.IsAbstract)
            {
                sb.AppendLine("                    throw new System.NotSupportedException(\"Unknown derived type: \" + this.GetType().Name);");
            }
            else
            {
                sb.AppendLine("                    if (this.GetType() != typeof(" + data.ClassName + "))");
                sb.AppendLine("                    {");
                sb.AppendLine("                        throw new System.NotSupportedException(\"Unknown derived type: \" + this.GetType().Name);");
                sb.AppendLine("                    }");
                sb.AppendLine("                    break;");
            }
            sb.AppendLine("            }");

            if (data.IsAbstract)
            {
                sb.AppendLine("        }");
                sb.AppendLine();
                goto GenerateToJson; // Skip base calculation for abstract types
            }
        }
        sb.AppendLine("            int size = 2;");
        sb.AppendLine("            int propertyCount = 0;");




        foreach (var prop in allProperties)
        {
            string indent = "            ";
            if (prop.IsNullable)
            {
                sb.Append(indent);
                sb.Append("if (this.");
                sb.Append(prop.Name);
                sb.AppendLine(" is not null)");
                sb.Append(indent);
                sb.AppendLine("{");
                indent = "                ";
            }

            sb.Append(indent);
            sb.AppendLine("propertyCount++;");
            if (!data.SkipCountOptimization && prop.Type is GenJsonDataType.Enumerable en)
            {
                if (en.IsArray)
                {
                    sb.Append(indent);
                    sb.Append("size += ");
                    sb.Append(prop.JsonName.Length + 4); // "$Name":
                    sb.AppendLine(";");

                    sb.Append(indent);
                    sb.Append("size += global::GenJson.GenJsonSizeHelper.GetSize(this.");
                    sb.Append(prop.Name);
                    sb.AppendLine(".Length);");

                    sb.Append(indent);
                    sb.AppendLine("size += 1;"); // Comma
                }
                else
                {
                    sb.Append(indent);
                    sb.AppendLine("{");
                    sb.Append(indent);
                    sb.AppendLine("    int _count = -1;");
                    sb.Append(indent);
                    sb.AppendLine($"    if (this.{prop.Name} is global::System.Collections.Generic.ICollection<{en.ElementTypeName}> c) _count = c.Count;");
                    sb.Append(indent);
                    sb.AppendLine($"    else if (this.{prop.Name} is global::System.Collections.Generic.IReadOnlyCollection<{en.ElementTypeName}> r) _count = r.Count;");
                    sb.Append(indent);
                    sb.AppendLine("    if (_count >= 0)");
                    sb.Append(indent);
                    sb.AppendLine("    {");
                    sb.Append(indent);
                    sb.Append("        size += ");
                    sb.Append(prop.JsonName.Length + 4);
                    sb.AppendLine(";");
                    sb.Append(indent);
                    sb.AppendLine("        size += global::GenJson.GenJsonSizeHelper.GetSize(_count);");
                    sb.Append(indent);
                    sb.AppendLine("        size += 1;");
                    sb.Append(indent);
                    sb.AppendLine("    }");
                    sb.Append(indent);
                    sb.AppendLine("}");
                }
            }
            else if (!data.SkipCountOptimization && prop.Type is GenJsonDataType.Dictionary)
            {
                sb.Append(indent);
                sb.Append("size += ");
                sb.Append(prop.JsonName.Length + 4); // "$Name":
                sb.AppendLine(";");

                sb.Append(indent);
                sb.Append("size += global::GenJson.GenJsonSizeHelper.GetSize(this.");
                sb.Append(prop.Name);
                sb.AppendLine(".Count);");

                sb.Append(indent);
                sb.AppendLine("size += 1;"); // Comma
            }

            sb.Append(indent);
            sb.Append("size += ");
            sb.Append(prop.JsonName.Length + 3); // "Key":
            sb.AppendLine(";");

            GenerateSizeValue(sb, prop.Type, $"this.{prop.Name}", indent, 0);

            if (prop.IsNullable)
            {
                sb.AppendLine("            }");
            }
        }

        sb.AppendLine("            if (propertyCount > 0) size += propertyCount - 1;");
        sb.AppendLine("            return size;");
        sb.AppendLine("        }");
        sb.AppendLine();

    GenerateToJson:
        sb.Append("        public ");
        sb.Append(newModifier);
        sb.AppendLine("string ToJson()");
        sb.AppendLine("        {");
        sb.AppendLine("            return string.Create(CalculateJsonSize(), this, (span, state) =>");
        sb.AppendLine("            {");
        sb.AppendLine("                int index = 0;");
        sb.AppendLine("                state.WriteJson(span, ref index);");
        sb.AppendLine("            });");
        sb.AppendLine("        }");


        sb.AppendLine();
        sb.AppendLine();
        sb.Append("        public ");
        sb.Append(newModifier);
        sb.AppendLine("void WriteJson(System.Span<char> span, ref int index)");
        sb.AppendLine("        {");
        if (data.DerivedTypes.Value.Count > 0)
        {
            sb.AppendLine("            switch (this)");
            sb.AppendLine("            {");
            int derivedIdx = 0;
            foreach (var derived in data.DerivedTypes.Value)
            {
                derivedIdx++;
                sb.AppendLine("                case " + derived.TypeName + " d" + derivedIdx + ":");
                sb.AppendLine("                {");
                sb.AppendLine("                    span[index++] = '{';");
                var discKey = data.PolymorphicDiscriminatorProp ?? "$type";
                sb.AppendLine("                    global::GenJson.GenJsonWriter.WriteString(span, ref index, \"" + discKey + "\");");
                sb.AppendLine("                    span[index++] = ':';");
                if (derived.IsIntDiscriminator)
                {
                    foreach (char c in derived.DiscriminatorValue)
                        sb.AppendLine("                    span[index++] = '" + c + "';");
                }
                else
                {
                    sb.AppendLine("                    global::GenJson.GenJsonWriter.WriteString(span, ref index, " + derived.DiscriminatorValue + ");");
                }
                sb.AppendLine("                    span[index++] = ',';");
                sb.AppendLine("                    d" + derivedIdx + ".WriteJsonContent(span, ref index);");
                sb.AppendLine("                    span[index++] = '}';");
                sb.AppendLine("                    return;");
                sb.AppendLine("                }");
            }

            sb.AppendLine("                default:");
            if (data.IsAbstract)
            {
                sb.AppendLine("                    throw new System.NotSupportedException(\"Unknown derived type: \" + this.GetType().Name);");
            }
            else
            {
                sb.AppendLine("                    if (this.GetType() != typeof(" + data.ClassName + "))");
                sb.AppendLine("                    {");
                sb.AppendLine("                        throw new System.NotSupportedException(\"Unknown derived type: \" + this.GetType().Name);");
                sb.AppendLine("                    }");
                sb.AppendLine("                    break;");
            }
            sb.AppendLine("            }");

            if (data.IsAbstract)
            {
                sb.AppendLine("        }");
                sb.AppendLine();
                goto GenerateWriteJsonContent;
            }
        }
        sb.AppendLine("            span[index++] = '{';");
        sb.AppendLine("            WriteJsonContent(span, ref index);");
        sb.AppendLine("            span[index++] = '}';");
        sb.AppendLine("        }");

        sb.AppendLine();
    GenerateWriteJsonContent:
        sb.Append("        public ");
        sb.Append(newModifier);
        sb.AppendLine("void WriteJsonContent(System.Span<char> span, ref int index)");
        sb.AppendLine("        {");
        bool needFirstSpan = allProperties.Count > 1 && allProperties[0].IsNullable;
        if (needFirstSpan)
        {
            sb.AppendLine("            bool first = true;");
        }
        var stateSpan = 0;

        foreach (var prop in allProperties)
        {
            string indent = "            ";
            if (prop.IsNullable)
            {
                sb.Append(indent);
                sb.Append("if (this.");
                sb.Append(prop.Name);
                sb.AppendLine(" is not null)");
                sb.Append(indent);
                sb.AppendLine("{");
                indent = "                ";
            }

            if (stateSpan == 0) // True
            {
                if (prop.IsNullable)
                {
                    if (needFirstSpan)
                    {
                        sb.Append(indent);
                        sb.AppendLine("first = false;");
                    }
                    stateSpan = 2;
                }
                else stateSpan = 1;
            }
            else if (stateSpan == 1) // False
            {
                sb.Append(indent);
                sb.AppendLine("span[index++] = ',';");
            }
            else // Unknown
            {
                sb.Append(indent);
                sb.AppendLine("if (!first)");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    span[index++] = ',';");
                sb.Append(indent);
                sb.AppendLine("}");
                if (prop.IsNullable)
                {
                    sb.Append(indent);
                    sb.AppendLine("first = false;");
                }
                else stateSpan = 1;
            }

            if (!data.SkipCountOptimization && prop.Type is GenJsonDataType.Enumerable en)
            {
                if (en.IsArray)
                {
                    sb.Append(indent);
                    sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, \"$");
                    sb.Append(prop.JsonName);
                    sb.AppendLine("\");");

                    sb.Append(indent);
                    sb.AppendLine("span[index++] = ':';");

                    sb.Append(indent);
                    sb.Append("{ if (!this.");
                    sb.Append(prop.Name);
                    sb.AppendLine(".Length.TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
                    sb.Append(indent);
                    sb.AppendLine("    { throw new System.Exception(\"Buffer too small (Count)\"); }");
                    sb.Append(indent);
                    sb.AppendLine("    index += written; }");

                    sb.Append(indent);
                    sb.AppendLine("span[index++] = ',';");
                }
                else
                {
                    sb.Append(indent);
                    sb.AppendLine("{");
                    sb.Append(indent);
                    sb.AppendLine("    int _count = -1;");
                    sb.Append(indent);
                    sb.AppendLine($"    if (this.{prop.Name} is global::System.Collections.Generic.ICollection<{en.ElementTypeName}> c) _count = c.Count;");
                    sb.Append(indent);
                    sb.AppendLine($"    else if (this.{prop.Name} is global::System.Collections.Generic.IReadOnlyCollection<{en.ElementTypeName}> r) _count = r.Count;");
                    sb.Append(indent);
                    sb.AppendLine("    if (_count >= 0)");
                    sb.Append(indent);
                    sb.AppendLine("    {");
                    sb.Append(indent);
                    sb.Append("        global::GenJson.GenJsonWriter.WriteString(span, ref index, \"$");
                    sb.Append(prop.JsonName);
                    sb.AppendLine("\");");

                    sb.Append(indent);
                    sb.AppendLine("        span[index++] = ':';");

                    sb.Append(indent);
                    sb.AppendLine("        { if (!_count.TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
                    sb.Append(indent);
                    sb.AppendLine("            { throw new System.Exception(\"Buffer too small (Count)\"); }");
                    sb.Append(indent);
                    sb.AppendLine("            index += written; }");

                    sb.Append(indent);
                    sb.AppendLine("        span[index++] = ',';");
                    sb.Append(indent);
                    sb.AppendLine("    }");
                    sb.Append(indent);
                    sb.AppendLine("}");
                }
            }
            else if (!data.SkipCountOptimization && prop.Type is GenJsonDataType.Dictionary)
            {
                sb.Append(indent);
                sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, \"$");
                sb.Append(prop.JsonName);
                sb.AppendLine("\");");

                sb.Append(indent);
                sb.AppendLine("span[index++] = ':';");

                sb.Append(indent);
                sb.Append("{ if (!this.");
                sb.Append(prop.Name);
                sb.AppendLine(".Count.TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
                sb.Append(indent);
                sb.AppendLine("    { throw new System.Exception(\"Buffer too small (Count)\"); }");
                sb.Append(indent);
                sb.AppendLine("    index += written; }");

                sb.Append(indent);
                sb.AppendLine("span[index++] = ',';");
            }

            sb.Append(indent);
            sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, \"");
            sb.Append(prop.JsonName);
            sb.AppendLine("\");");
            sb.Append(indent);
            sb.AppendLine("span[index++] = ':';");

            GenerateWriteJsonValue(sb, prop.Type, $"this.{prop.Name}", indent, 0);

            if (prop.IsNullable)
            {
                sb.AppendLine("            }");
            }
        }
        sb.AppendLine("        }");

        sb.AppendLine();

        sb.AppendLine();
        sb.Append("        public ");
        sb.Append(newModifier);
        sb.AppendLine("static " + data.ClassName + "? FromJson(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            System.ReadOnlySpan<char> span = json;");
        sb.AppendLine("            var index = 0;");
        sb.AppendLine("            var result = Parse(span, ref index);");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");

        sb.AppendLine();

        sb.AppendLine();
        sb.Append("        internal ");
        sb.Append(newModifier);
        sb.AppendLine("static " + data.ClassName + "? Parse(System.ReadOnlySpan<char> json, ref int index)");
        sb.AppendLine("        {");
        if (data.DerivedTypes.Value.Count > 0)
        {
            var discName = data.PolymorphicDiscriminatorProp ?? "$type";
            sb.AppendLine("            if (global::GenJson.GenJsonParser.TryFindProperty(json, index, \"" + discName + "\", out var valIndex))");
            sb.AppendLine("            {");
            sb.AppendLine("                int tempIndex = valIndex;");
            sb.AppendLine("");
            sb.AppendLine("                if (tempIndex < json.Length)");
            sb.AppendLine("                {");
            sb.AppendLine("                    char c = json[tempIndex];");
            sb.AppendLine("                    if (c == '\"')");
            sb.AppendLine("                    {");
            foreach (var derived in data.DerivedTypes.Value.Where(d => !d.IsIntDiscriminator))
            {
                var rawVal = derived.DiscriminatorValue.Trim('"');
                sb.AppendLine("                        if (global::GenJson.GenJsonParser.MatchesKey(json, ref tempIndex, \"" + rawVal + "\"))");
                sb.AppendLine("                        {");
                sb.AppendLine("                            int i = index;");
                sb.AppendLine("                            var result = " + derived.TypeName + ".Parse(json, ref i);");
                sb.AppendLine("                            index = i;");
                sb.AppendLine("                            return result;");
                sb.AppendLine("                        }");
            }
            sb.AppendLine("                    }");
            sb.AppendLine("                    else if (char.IsDigit(c) || c == '-')");
            sb.AppendLine("                    {");
            sb.AppendLine("                        if (global::GenJson.GenJsonParser.TryParseInt(json, ref tempIndex, out var iVal))");
            sb.AppendLine("                        {");
            if (data.DerivedTypes.Value.Any(d => d.IsIntDiscriminator))
            {
                sb.AppendLine("                            switch (iVal)");
                sb.AppendLine("                            {");
                foreach (var derived in data.DerivedTypes.Value.Where(d => d.IsIntDiscriminator))
                {
                    sb.AppendLine("                                case " + derived.DiscriminatorValue + ":");
                    sb.AppendLine("                                {");
                    sb.AppendLine("                                    int i = index;");
                    sb.AppendLine("                                    var result = " + derived.TypeName + ".Parse(json, ref i);");
                    sb.AppendLine("                                    index = i;");
                    sb.AppendLine("                                    return result;");
                    sb.AppendLine("                                }");
                }
                sb.AppendLine("                            }");
            }
            sb.AppendLine("                        }");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");

            // If we found property but failed to match value:
            sb.AppendLine("                return null;");
            sb.AppendLine("            }");

            // If we didn't find property:
            if (data.IsAbstract)
            {
                sb.AppendLine("            return null;"); // User requested null return
                goto GenerateParseEnd;
            }
            else
            {
                // Concrete base: If missing, maybe it IS the base?
                // User said: "if the type is not identified it should fail".
                // But if I have a concrete base, and no discriminator, it is usually assumed to be the base.
                // However, if "type not identified" implies strictness.
                // Let's assume for Concrete Base, missing discriminator -> Base.
                // This is consistent with "Keep the check when [..] not abstract".
            }
        }
        sb.AppendLine();
        sb.AppendLine("");
        sb.AppendLine("            if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, '{')) return null;");

        foreach (var prop in allProperties)
        {
            sb.Append("            ");
            sb.Append(prop.TypeName);
            if (!prop.IsValueType && !prop.TypeName.EndsWith("?")) sb.Append("?");
            sb.Append(" _");
            sb.Append(prop.Name);
            sb.AppendLine(" = default;");

            if (!data.SkipCountOptimization && (prop.Type is GenJsonDataType.Enumerable or GenJsonDataType.Dictionary))
            {
                sb.Append("            int _");
                sb.Append(prop.Name);
                sb.AppendLine("_count = -1;");
            }

            if (data.IsNullableContext && prop.IsValueType && !prop.IsNullable)
            {
                sb.Append("            bool _");
                sb.Append(prop.Name);
                sb.AppendLine("_set = false;");
            }
        }

        sb.AppendLine("            while (index < json.Length)");
        sb.AppendLine("            {");
        sb.AppendLine("    ");
        sb.AppendLine("                if (index >= json.Length) return null;");
        sb.AppendLine("                if (json[index] == '}')");
        sb.AppendLine("                {");
        sb.AppendLine("                    index++;");
        if (data.IsNullableContext)
        {
            foreach (var prop in allProperties)
            {
                if (!prop.IsNullable)
                {
                    if (!prop.IsValueType)
                    {
                        sb.Append("                    if (_");
                        sb.Append(prop.Name);
                        sb.AppendLine(" is null) return null;");
                    }
                    else
                    {
                        sb.Append("                    if (!_");
                        sb.Append(prop.Name);
                        sb.AppendLine("_set) return null;");
                    }
                }
            }
        }

        if (data.IsAbstract)
        {
            sb.AppendLine("                    throw new System.NotSupportedException(\"Cannot deserialize abstract type\");");
        }
        else
        {
            sb.Append("                    return new ");
            sb.Append(data.ClassName);
            sb.Append("(");

            bool firstArg = true;
            foreach (var arg in data.ConstructorArgs.Value)
            {
                if (!firstArg) sb.Append(", ");
                sb.Append("_");
                sb.Append(arg.Name);
                firstArg = false;
            }

            sb.AppendLine(")");
            if (data.InitProperties.Value.Count > 0)
            {
                sb.AppendLine("                    {");
                foreach (var prop in data.InitProperties.Value)
                {
                    sb.Append("                        ");
                    sb.Append(prop.Name);
                    sb.Append(" = _");
                    sb.Append(prop.Name);
                    sb.AppendLine(",");
                }
                sb.Append("                    }");
            }
            sb.AppendLine(";");
        }
        sb.AppendLine("                }");

        sb.AppendLine("                bool matched = false;");
        foreach (var prop in allProperties)
        {
            if (!data.SkipCountOptimization && (prop.Type is GenJsonDataType.Enumerable or GenJsonDataType.Dictionary))
            {
                sb.Append("                if (global::GenJson.GenJsonParser.MatchesKey(json, ref index, \"$");
                sb.Append(prop.JsonName);
                sb.AppendLine("\"))");
                sb.AppendLine("                {");
                sb.AppendLine("                    if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, ':')) return null;");
                sb.Append("                    if (!global::GenJson.GenJsonParser.TryParseInt(json, ref index, out _");
                sb.Append(prop.Name);
                sb.AppendLine("_count)) return null;");
                sb.AppendLine("                    if (index < json.Length && json[index] == ',') index++;");
                sb.AppendLine("                    matched = true;");
                sb.AppendLine("                }");
            }

            sb.Append("                if (global::GenJson.GenJsonParser.MatchesKey(json, ref index, \"");
            sb.Append(prop.JsonName);
            sb.AppendLine("\"))");
            sb.AppendLine("                {");
            sb.AppendLine("                    if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, ':')) return null;");
            GenerateParseValue(sb, prop.Type, "_" + prop.Name, "                    ", 0, (!data.SkipCountOptimization && (prop.Type is GenJsonDataType.Enumerable or GenJsonDataType.Dictionary)) ? $"_{prop.Name}_count" : null, (!prop.IsValueType || prop.IsNullable));
            if (data.IsNullableContext && prop.IsValueType && !prop.IsNullable)
            {
                sb.Append("                    _");
                sb.Append(prop.Name);
                sb.AppendLine("_set = true;");
            }
            sb.AppendLine("                    matched = true;");
            sb.AppendLine("        ");
            sb.AppendLine("                    if (index < json.Length && json[index] == ',')");
            sb.AppendLine("                    {");
            sb.AppendLine("                        index++;");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
        }

        sb.AppendLine("                if (!matched && (index < json.Length && json[index] == '\"'))");
        sb.AppendLine("                {");
        sb.AppendLine("                    if (!global::GenJson.GenJsonParser.TrySkipString(json, ref index)) return null;");
        sb.AppendLine("                    if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, ':')) return null;");
        sb.AppendLine("                    if (!global::GenJson.GenJsonParser.TrySkipValue(json, ref index)) return null;");
        sb.AppendLine("        ");
        sb.AppendLine("                    if (index < json.Length && json[index] == ',')");
        sb.AppendLine("                    {");
        sb.AppendLine("                        index++;");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            return null;");
    GenerateParseEnd:
        sb.AppendLine("        }");

        sb.AppendLine("    }");

        if (!string.IsNullOrEmpty(data.Namespace))
        {
            sb.AppendLine("}");
        }

        context.AddSource($"{data.ClassName}.GenJson.g.cs", sb.ToString());
    }

    private void GenerateParseValue(StringBuilder sb, GenJsonDataType type, string target, string indent, int depth, string? explicitCountVar, bool targetIsNullable)
    {
        switch (type)
        {
            case GenJsonDataType.CustomConverter customConverter:
                sb.Append(indent);
                sb.Append(target);
                sb.Append(" = ");
                sb.Append(customConverter.ConverterTypeName);
                sb.AppendLine(".FromJson(json, ref index);");
                break;


            case GenJsonDataType.Boolean:
                sb.Append(indent);
                if (targetIsNullable)
                {
                    sb.Append("if (!global::GenJson.GenJsonParser.TryParseBoolean(json, ref index, out var ");
                    sb.Append(target);
                    sb.AppendLine("_val)) return null;");
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(target);
                    sb.AppendLine("_val;");
                }
                else
                {
                    sb.Append("if (!global::GenJson.GenJsonParser.TryParseBoolean(json, ref index, out ");
                    sb.Append(target);
                    sb.AppendLine(")) return null;");
                }
                break;

            case GenJsonDataType.String:
                sb.Append(indent);
                sb.Append("if (!global::GenJson.GenJsonParser.TryParseString(json, ref index, out ");
                sb.Append(target);
                sb.AppendLine(")) return null;");
                break;

            case GenJsonDataType.Char:
                sb.Append(indent);
                if (targetIsNullable)
                {
                    sb.Append("if (!global::GenJson.GenJsonParser.TryParseChar(json, ref index, out var ");
                    sb.Append(target);
                    sb.AppendLine("_val)) return null;");
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(target);
                    sb.AppendLine("_val;");
                }
                else
                {
                    sb.Append("if (!global::GenJson.GenJsonParser.TryParseChar(json, ref index, out ");
                    sb.Append(target);
                    sb.AppendLine(")) return null;");
                }
                break;

            case GenJsonDataType.FloatingPoint p:
                sb.Append(indent);
                if (targetIsNullable)
                {
                    sb.Append("if (!global::GenJson.GenJsonParser.TryParse");
                    sb.Append(GetPrimitiveParserName(p.TypeName));
                    sb.Append("(json, ref index, out var ");
                    sb.Append(target);
                    sb.AppendLine("_val)) return null;");
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(target);
                    sb.AppendLine("_val;");
                }
                else
                {
                    sb.Append("if (!global::GenJson.GenJsonParser.TryParse");
                    sb.Append(GetPrimitiveParserName(p.TypeName));
                    sb.Append("(json, ref index, out ");
                    sb.Append(target);
                    sb.AppendLine(")) return null;");
                }
                break;

            case GenJsonDataType.Guid:
                sb.Append(indent);
                sb.Append("if (!global::GenJson.GenJsonParser.TryParseString(json, ref index, out var ");
                sb.Append(target);
                sb.Append("_str) || !System.Guid.TryParse(");
                sb.Append(target);
                if (targetIsNullable)
                {
                    sb.Append("_str, out var ");
                    sb.Append(target);
                    sb.AppendLine("_val)) return null;");
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(target);
                    sb.AppendLine("_val;");
                }
                else
                {
                    sb.Append("_str, out ");
                    sb.Append(target);
                    sb.AppendLine(")) return null;");
                }
                break;

            case GenJsonDataType.DateTime:
                sb.Append(indent);
                sb.Append("if (!global::GenJson.GenJsonParser.TryParseString(json, ref index, out var ");
                sb.Append(target);
                sb.Append("_str) || !System.DateTime.TryParse(");
                sb.Append(target);
                if (targetIsNullable)
                {
                    sb.Append("_str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var ");
                    sb.Append(target);
                    sb.AppendLine("_val)) return null;");
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(target);
                    sb.AppendLine("_val;");
                }
                else
                {
                    sb.Append("_str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out ");
                    sb.Append(target);
                    sb.AppendLine(")) return null;");
                }
                break;

            case GenJsonDataType.TimeSpan:
                sb.Append(indent);
                sb.Append("if (!global::GenJson.GenJsonParser.TryParseString(json, ref index, out var ");
                sb.Append(target);
                sb.Append("_str) || !System.TimeSpan.TryParse(");
                sb.Append(target);
                if (targetIsNullable)
                {
                    sb.Append("_str, out var ");
                    sb.Append(target);
                    sb.AppendLine("_val)) return null;");
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(target);
                    sb.AppendLine("_val;");
                }
                else
                {
                    sb.Append("_str, out ");
                    sb.Append(target);
                    sb.AppendLine(")) return null;");
                }
                break;

            case GenJsonDataType.DateTimeOffset:
                sb.Append(indent);
                sb.Append("if (!global::GenJson.GenJsonParser.TryParseString(json, ref index, out var ");
                sb.Append(target);
                sb.Append("_str) || !System.DateTimeOffset.TryParse(");
                sb.Append(target);
                if (targetIsNullable)
                {
                    sb.Append("_str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var ");
                    sb.Append(target);
                    sb.AppendLine("_val)) return null;");
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(target);
                    sb.AppendLine("_val;");
                }
                else
                {
                    sb.Append("_str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out ");
                    sb.Append(target);
                    sb.AppendLine(")) return null;");
                }
                break;

            case GenJsonDataType.Version:
                sb.Append(indent);
                sb.Append("if (!global::GenJson.GenJsonParser.TryParseString(json, ref index, out var ");
                sb.Append(target);
                sb.Append("_str) || !System.Version.TryParse(");
                sb.Append(target);
                if (targetIsNullable)
                {
                    sb.Append("_str, out var ");
                    sb.Append(target);
                    sb.AppendLine("_val)) return null;");
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(target);
                    sb.AppendLine("_val;");
                }
                else
                {
                    sb.Append("_str, out ");
                    sb.Append(target);
                    sb.AppendLine(")) return null;");
                }
                break;

            case GenJsonDataType.Enum en:
                GenerateEnumParseLogic(sb, en, target, indent, false, depth, targetIsNullable);
                break;

            case GenJsonDataType.Primitive p:
                sb.Append(indent);
                if (targetIsNullable)
                {
                    sb.Append("if (!global::GenJson.GenJsonParser.TryParse");
                    sb.Append(GetPrimitiveParserName(p.TypeName));
                    sb.Append("(json, ref index, out var ");
                    sb.Append(target);
                    sb.AppendLine("_tmp)) return null;");
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(target);
                    sb.AppendLine("_tmp;");
                }
                else
                {
                    sb.Append("if (!global::GenJson.GenJsonParser.TryParse");
                    sb.Append(GetPrimitiveParserName(p.TypeName));
                    sb.Append("(json, ref index, out ");
                    sb.Append(target);
                    sb.AppendLine(")) return null;");
                }
                break;

            case GenJsonDataType.Object o:
                sb.Append(indent);
                sb.Append(target);
                sb.Append(" = ");
                sb.Append(o.TypeName); // Assuming generated class has Parse
                sb.AppendLine(".Parse(json, ref index);");
                sb.Append(indent);
                sb.Append("if (");
                sb.Append(target);
                sb.AppendLine(" is null) return null;");
                break;

            case GenJsonDataType.Nullable n:
                sb.Append(indent);
                sb.AppendLine("if (global::GenJson.GenJsonParser.IsNull(json, ref index))");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    if (!global::GenJson.GenJsonParser.TryParseNull(json, ref index)) return null;");
                sb.Append(indent);
                sb.Append("    ");
                sb.Append(target);
                sb.AppendLine(" = null;");
                sb.Append(indent);
                sb.AppendLine("}");
                sb.Append(indent);
                sb.AppendLine("else");
                sb.Append(indent);
                sb.AppendLine("{");
                GenerateParseValue(sb, n.Underlying, target, indent + "    ", depth, explicitCountVar, true);
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Enumerable e:
                {
                    sb.Append(indent);
                    sb.AppendLine("{");
                    string scopedIndent = indent + "    ";

                    var listVar = $"list{depth}";
                    sb.Append(scopedIndent);
                    sb.AppendLine("if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, '[')) return null;");

                    string countVar = $"count{depth}";
                    sb.Append(scopedIndent);
                    if (explicitCountVar != null)
                    {
                        sb.Append($"int {countVar} = ({explicitCountVar} >= 0) ? {explicitCountVar} : ");
                        sb.AppendLine($"global::GenJson.GenJsonParser.CountListItems(json, index);");
                    }
                    else
                    {
                        sb.AppendLine($"int {countVar} = global::GenJson.GenJsonParser.CountListItems(json, index);");
                    }

                    sb.Append(scopedIndent);
                    if (e.IsArray)
                    {
                        sb.AppendLine($"var {listVar} = new global::System.Collections.Generic.List<{e.ElementTypeName}>({countVar});");
                    }
                    else
                    {
                        sb.AppendLine($"var {listVar} = new {e.ConstructionTypeName}({countVar});");
                    }

                    sb.Append(scopedIndent);
                    sb.AppendLine("while (index < json.Length)");
                    sb.Append(scopedIndent);
                    sb.AppendLine("{");

                    string loopIndent = scopedIndent + "    ";

                    sb.Append(loopIndent);

                    sb.Append(loopIndent);
                    sb.AppendLine("if (index >= json.Length) return null;");

                    sb.Append(loopIndent);
                    sb.AppendLine("if (json[index] == ']')");
                    sb.Append(loopIndent);
                    sb.AppendLine("{");
                    sb.Append(loopIndent + "    ");
                    sb.AppendLine("index++;");
                    sb.Append(loopIndent + "    ");
                    sb.Append(target);
                    if (e.IsArray)
                    {
                        sb.AppendLine($" = {listVar}.ToArray();");
                    }
                    else
                    {
                        sb.AppendLine($" = {listVar};");
                    }
                    sb.Append(loopIndent + "    ");
                    sb.AppendLine("break;");
                    sb.Append(loopIndent);
                    sb.AppendLine("}");

                    string itemVar = $"item{depth}";
                    sb.Append(loopIndent);
                    sb.Append($"{e.ElementTypeName}");
                    if (!e.IsElementValueType && !e.ElementTypeName.EndsWith("?")) sb.Append("?");
                    sb.AppendLine($" {itemVar} = default;");

                    GenerateParseValue(sb, e.ElementType, itemVar, loopIndent, depth + 1, null, (!e.IsElementValueType || e.ElementTypeName.EndsWith("?")));

                    sb.Append(loopIndent);
                    sb.Append($"{listVar}.Add({itemVar}");
                    if (!e.IsElementValueType) sb.Append("!");
                    sb.AppendLine(");");

                    sb.Append(loopIndent);

                    sb.Append(loopIndent);
                    sb.AppendLine("if (index < json.Length && json[index] == ',')");
                    sb.Append(loopIndent);
                    sb.AppendLine("{");
                    sb.Append(loopIndent + "    ");
                    sb.AppendLine("index++;");
                    sb.Append(loopIndent);
                    sb.AppendLine("}");

                    sb.Append(scopedIndent);
                    sb.AppendLine("}");
                    sb.Append(indent);
                    sb.AppendLine("}");
                }
                break;

            case GenJsonDataType.Dictionary d:
                {
                    sb.Append(indent);
                    sb.AppendLine("{");
                    string scopedIndent = indent + "    ";

                    sb.Append(scopedIndent);
                    sb.AppendLine("if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, '{')) return null;");

                    string countVar = $"count{depth}";
                    sb.Append(scopedIndent);
                    if (explicitCountVar != null)
                    {
                        sb.Append($"int {countVar} = ({explicitCountVar} >= 0) ? {explicitCountVar} : ");
                        sb.AppendLine($"global::GenJson.GenJsonParser.CountDictionaryItems(json, index);");
                    }
                    else
                    {
                        sb.AppendLine($"int {countVar} = global::GenJson.GenJsonParser.CountDictionaryItems(json, index);");
                    }

                    string dictVar = $"dict{depth}";
                    sb.Append(scopedIndent);
                    sb.AppendLine($"var {dictVar} = new {d.ConstructionTypeName}({countVar});");

                    sb.Append(scopedIndent);
                    sb.AppendLine("while (index < json.Length)");
                    sb.Append(scopedIndent);
                    sb.AppendLine("{");

                    string loopIndent = scopedIndent + "    ";

                    sb.Append(loopIndent);

                    sb.Append(loopIndent);
                    sb.AppendLine("if (index >= json.Length) return null;");

                    sb.Append(loopIndent);
                    sb.AppendLine("if (json[index] == '}')");
                    sb.Append(loopIndent);
                    sb.AppendLine("{");
                    sb.Append(loopIndent + "    ");
                    sb.AppendLine("index++;");
                    sb.Append(loopIndent + "    ");
                    sb.Append(target);
                    sb.AppendLine($" = {dictVar};");
                    sb.Append(loopIndent + "    ");
                    sb.AppendLine("break;");
                    sb.Append(loopIndent);
                    sb.AppendLine("}");

                    string keyVar = $"key{depth}";
                    sb.Append(loopIndent);

                    if (d.KeyType is GenJsonDataType.Enum enumKeyType)
                    {
                        sb.AppendLine($"{d.KeyTypeName} {keyVar} = default;");
                        GenerateEnumParseLogic(sb, enumKeyType, keyVar, loopIndent, true, depth, false);
                    }
                    else
                    {
                        string keyStrVar = $"keyStr{depth}";
                        sb.Append(loopIndent);
                        sb.AppendLine($"if (!global::GenJson.GenJsonParser.TryParseString(json, ref index, out var {keyStrVar}) || {keyStrVar} is null) return null;");

                        sb.Append(loopIndent);
                        sb.AppendLine("if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, ':')) return null;");

                        switch (d.KeyType)
                        {
                            case GenJsonDataType.String:
                                sb.AppendLine($"{d.KeyTypeName} {keyVar} = {keyStrVar};");
                                break;
                            case GenJsonDataType.Primitive:
                            case GenJsonDataType.FloatingPoint:
                            case GenJsonDataType.Guid:
                            case GenJsonDataType.DateTime:
                            case GenJsonDataType.TimeSpan:
                            case GenJsonDataType.DateTimeOffset:
                                {
                                    // Use .Parse(string, CultureInfo) if applicable, or just .Parse(string)
                                    if (d.KeyType is GenJsonDataType.FloatingPoint)
                                    {
                                        sb.AppendLine($"if (!{d.KeyTypeName}.TryParse({keyStrVar}, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out {d.KeyTypeName} {keyVar})) return null;");
                                    }
                                    else
                                    {
                                        sb.AppendLine($"if (!{d.KeyTypeName}.TryParse({keyStrVar}, out {d.KeyTypeName} {keyVar})) return null;");
                                    }

                                    break;
                                }
                            default:
                                sb.AppendLine($"if (!{d.KeyTypeName}.TryParse({keyStrVar}, out {d.KeyTypeName} {keyVar})) return null;");
                                break;
                        }
                    }

                    string valVar = $"val{depth}";
                    sb.Append(loopIndent);
                    sb.Append($"{d.ValueTypeName}");
                    if (!d.IsValueValueType && !d.ValueTypeName.EndsWith("?")) sb.Append("?");
                    sb.AppendLine($" {valVar} = default;");

                    GenerateParseValue(sb, d.ValueType, valVar, loopIndent, depth + 1, null, (!d.IsValueValueType || d.ValueTypeName.EndsWith("?")));

                    sb.Append(loopIndent);
                    if (d.ConstructionTypeName.Contains("IReadOnlyDictionary"))
                    {
                        sb.Append($"{dictVar}.Add({keyVar}, {valVar}");
                        if (!d.IsValueValueType) sb.Append("!");
                        sb.AppendLine(");");
                    }
                    else
                    {
                        sb.Append($"{dictVar}.Add({keyVar}, {valVar}");
                        if (!d.IsValueValueType) sb.Append("!");
                        sb.AppendLine(");");
                    }

                    sb.Append(loopIndent);

                    sb.Append(loopIndent);
                    sb.AppendLine("if (index < json.Length && json[index] == ',')");
                    sb.Append(loopIndent);
                    sb.AppendLine("{");
                    sb.Append(loopIndent + "    ");
                    sb.AppendLine("index++;");
                    sb.Append(loopIndent);
                    sb.AppendLine("}");

                    sb.Append(scopedIndent);
                    sb.AppendLine("}");
                    sb.Append(indent);
                    sb.AppendLine("}");
                }
                break;


        }
    }

    private void GenerateEnumParseLogic(StringBuilder sb, GenJsonDataType.Enum en, string target, string indent, bool isDictionaryKey, int depth, bool targetIsNullable)
    {
        if (en.AsString)
        {
            string matchedVar = $"matched{depth}";
            sb.Append(indent);
            sb.AppendLine("{");
            sb.Append(indent);
            sb.AppendLine($"    bool {matchedVar} = false;");

            bool first = true;
            foreach (var member in en.Members.Value)
            {
                sb.Append(indent);
                sb.Append("    ");
                if (!first) sb.Append("else ");
                sb.Append("if (global::GenJson.GenJsonParser.MatchesKey(json, ref index, \"");
                sb.Append(member);
                sb.AppendLine("\"))");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.Append("        ");
                sb.Append(target);
                sb.Append(" = ");
                sb.Append(en.TypeName);
                sb.Append(".");
                sb.Append(member);
                sb.AppendLine(";");

                if (isDictionaryKey)
                {
                    sb.Append(indent);
                    sb.AppendLine("        if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, ':')) return null;");
                }

                sb.Append(indent);
                sb.AppendLine($"        {matchedVar} = true;");
                sb.Append(indent);
                sb.AppendLine("    }");
                first = false;
            }

            sb.Append(indent);
            sb.AppendLine($"    if (!{matchedVar})");
            sb.Append(indent);
            sb.AppendLine("    {");

            if (en.FallbackValue != null)
            {
                if (isDictionaryKey)
                {
                    // Dictionary Key Fallback: Skip Entry
                    sb.Append(indent);
                    sb.AppendLine("        if (!global::GenJson.GenJsonParser.TrySkipString(json, ref index)) return null;");
                    sb.Append(indent);
                    sb.AppendLine("        if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, ':')) return null;");
                    sb.Append(indent);
                    sb.AppendLine("        if (!global::GenJson.GenJsonParser.TrySkipValue(json, ref index)) return null;");
                    sb.Append(indent);

                    sb.Append(indent);
                    sb.AppendLine("        if (index < json.Length && json[index] == ',')");
                    sb.Append(indent);
                    sb.AppendLine("        {");
                    sb.Append(indent);
                    sb.AppendLine("            index++;");
                    sb.Append(indent);
                    sb.AppendLine("        }");
                    sb.Append(indent);
                    sb.AppendLine("        continue;");
                }
                else
                {
                    // Property Value Fallback: Assign Fallback
                    sb.Append(indent);
                    sb.AppendLine("        if (!global::GenJson.GenJsonParser.TrySkipString(json, ref index)) return null;");
                    sb.Append(indent);
                    sb.Append("        ");
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(en.FallbackValue);
                    sb.AppendLine(";");
                }
            }
            else
            {
                sb.Append(indent);
                sb.AppendLine("        return null;");
            }

            sb.Append(indent);
            sb.AppendLine("    }");
            sb.Append(indent);
            sb.AppendLine("}");
        }
        else
        {
            // AsNumber
            var parserName = GetPrimitiveParserName(en.UnderlyingType);

            if (isDictionaryKey)
            {
                string keyStrVar = $"keyStr{depth}";
                sb.Append(indent);
                sb.AppendLine($"if (!global::GenJson.GenJsonParser.TryParseString(json, ref index, out var {keyStrVar}) || {keyStrVar} is null) return null;");

                sb.Append(indent);
                sb.AppendLine("if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, ':')) return null;");

                sb.Append(indent);
                sb.AppendLine($"if (!{en.UnderlyingType}.TryParse({keyStrVar}, out var {target}_val))");
                sb.Append(indent);
                sb.AppendLine("{");
                // Parse Failed
                if (en.FallbackValue != null)
                {
                    sb.Append(indent);
                    sb.AppendLine("    if (!global::GenJson.GenJsonParser.TrySkipValue(json, ref index)) return null;");
                    sb.Append(indent);

                    sb.Append(indent);
                    sb.AppendLine("    if (index < json.Length && json[index] == ',')");
                    sb.Append(indent);
                    sb.AppendLine("    {");
                    sb.Append(indent);
                    sb.AppendLine("        index++;");
                    sb.Append(indent);
                    sb.AppendLine("    }");
                    sb.Append(indent);
                    sb.AppendLine("    continue;");
                }
                else
                {
                    sb.Append(indent);
                    sb.AppendLine("    return null;");
                }
                sb.Append(indent);
                sb.AppendLine("}");
            }
            else
            {
                // Property Value
                sb.Append(indent);
                sb.Append("if (global::GenJson.GenJsonParser.TryParse");
                sb.Append(parserName);
                sb.Append("(json, ref index, out var ");
                sb.Append(target);
                sb.AppendLine("_val))");
            }

            // Common IsDefined check
            if (isDictionaryKey)
            {
                sb.Append(indent);
                sb.AppendLine($"{target} = ({en.TypeName}){target}_val;"); // Cast to enum
            }

            sb.Append(indent);
            if (isDictionaryKey) sb.Append("if"); else sb.Append("{ if");
            sb.Append(" (System.Enum.IsDefined(typeof(");
            sb.Append(en.TypeName);
            sb.Append("), (");
            sb.Append(en.TypeName);
            sb.Append(")");
            sb.Append(target);
            sb.AppendLine("_val))");
            sb.Append(indent);
            sb.AppendLine("{");
            sb.Append(indent);
            sb.Append("    ");
            sb.Append(target);
            sb.Append(" = (");
            sb.Append(en.TypeName);
            sb.Append(")");
            sb.Append(target);
            sb.AppendLine("_val;");
            sb.Append(indent);
            sb.AppendLine("}");
            sb.Append(indent);
            sb.AppendLine("else");
            sb.Append(indent);
            sb.AppendLine("{");

            if (en.FallbackValue != null)
            {
                if (isDictionaryKey)
                {
                    sb.Append(indent);
                    sb.AppendLine("    if (!global::GenJson.GenJsonParser.TrySkipValue(json, ref index)) return null;");
                    sb.Append(indent);

                    sb.Append(indent);
                    sb.AppendLine("    if (index < json.Length && json[index] == ',')");
                    sb.Append(indent);
                    sb.AppendLine("    {");
                    sb.Append(indent);
                    sb.AppendLine("        index++;");
                    sb.Append(indent);
                    sb.AppendLine("    }");
                    sb.Append(indent);
                    sb.AppendLine("    continue;");
                }
                else
                {
                    // Property - value already parsed into _val
                    sb.Append(indent);
                    sb.Append("    ");
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(en.FallbackValue);
                    sb.AppendLine(";");
                }
            }
            else
            {
                if (isDictionaryKey)
                {
                    sb.Append(indent);
                    sb.AppendLine("    return null;");
                }
                else
                {
                    sb.Append(indent);
                    sb.AppendLine("    return null;");
                }
            }
            sb.Append(indent);
            sb.AppendLine("}");
            if (!isDictionaryKey) sb.AppendLine("}"); // Close IsDefined block

            if (!isDictionaryKey)
            {
                // The else for TryParse failure
                sb.Append(indent);
                sb.AppendLine("else");
                sb.Append(indent);
                sb.AppendLine("{");
                if (en.FallbackValue != null)
                {
                    sb.Append(indent);
                    sb.AppendLine("    if (!global::GenJson.GenJsonParser.TrySkipValue(json, ref index)) return null;");
                    sb.Append(indent);
                    sb.Append("    ");
                    sb.Append(target);
                    sb.Append(" = ");
                    sb.Append(en.FallbackValue);
                    sb.AppendLine(";");
                }
                else
                {
                    sb.Append(indent);
                    sb.AppendLine("    return null;");
                }
                sb.Append(indent);
                sb.AppendLine("}");
            }
        }
    }

    private string GetPrimitiveParserName(string typeName)
    {
        if (typeName.StartsWith("global::")) typeName = typeName.Substring(8);
        return typeName switch
        {
            "int" or "System.Int32" => "Int",
            "uint" or "System.UInt32" => "UInt",
            "long" or "System.Int64" => "Long",
            "ulong" or "System.UInt64" => "ULong",
            "short" or "System.Int16" => "Short",
            "ushort" or "System.UInt16" => "UShort",
            "byte" or "System.Byte" => "Byte",
            "sbyte" or "System.SByte" => "SByte",
            "float" or "System.Single" => "Float",
            "double" or "System.Double" => "Double",
            "decimal" or "System.Decimal" => "Decimal",
            _ => "Int"
        };
    }



    private void GenerateSizeValue(StringBuilder sb, GenJsonDataType type, string valueAccessor, string indent, int depth, bool unquoted = false)
    {
        switch (type)
        {
            case GenJsonDataType.Primitive:
            case GenJsonDataType.Boolean:
            case GenJsonDataType.FloatingPoint:
                sb.Append(indent);
                sb.Append("size += global::GenJson.GenJsonSizeHelper.GetSize(");
                sb.Append(valueAccessor);
                sb.AppendLine(");");
                break;

            case GenJsonDataType.Char:
            case GenJsonDataType.String:
            case GenJsonDataType.Guid:
            case GenJsonDataType.DateTime:
            case GenJsonDataType.DateTimeOffset:
            case GenJsonDataType.Version:
            case GenJsonDataType.TimeSpan:
                sb.Append(indent);
                sb.Append("size += global::GenJson.GenJsonSizeHelper.GetSize(");
                sb.Append(valueAccessor);
                sb.Append(")");
                if (unquoted) sb.Append(" - 2");
                sb.AppendLine(";");
                break;

            case GenJsonDataType.Object:
                sb.Append(indent);
                sb.Append("size += ");
                sb.Append(valueAccessor);
                sb.AppendLine(".CalculateJsonSize();");
                break;

            case GenJsonDataType.Nullable nullable:
                sb.Append(indent);
                sb.Append("if (");
                sb.Append(valueAccessor);
                sb.AppendLine(" is null)");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    size += 4;");
                sb.Append(indent);
                sb.AppendLine("}");
                sb.Append(indent);
                sb.AppendLine("else");
                sb.Append(indent);
                sb.AppendLine("{");
                GenerateSizeValue(sb, nullable.Underlying, $"{valueAccessor}.Value", indent + "    ", depth, unquoted);
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Enumerable enumerable:
                {
                    sb.Append(indent);
                    sb.AppendLine("{");
                    string arrayIndent = indent + "    ";

                    sb.Append(arrayIndent);
                    sb.AppendLine("size += 2;"); // []

                    string itemVar = $"item{depth}";
                    string countVar = $"count{depth}";
                    sb.Append(arrayIndent);
                    sb.AppendLine($"int {countVar} = 0;");

                    sb.Append(arrayIndent);
                    sb.Append("foreach (var ");
                    sb.Append(itemVar);
                    sb.Append(" in ");
                    sb.Append(valueAccessor);
                    sb.AppendLine(")");
                    sb.Append(arrayIndent);
                    sb.AppendLine("{");

                    string loopIndent = arrayIndent + "    ";

                    sb.Append(loopIndent);
                    sb.AppendLine($"{countVar}++;");

                    GenerateSizeValue(sb, enumerable.ElementType, itemVar, loopIndent, depth + 1);

                    sb.Append(arrayIndent);
                    sb.AppendLine("}"); // end foreach

                    sb.Append(arrayIndent);
                    sb.AppendLine($"if ({countVar} > 1) size += {countVar} - 1;"); // Commas

                    sb.Append(indent);
                    sb.AppendLine("}"); // end block
                }
                break;

            case GenJsonDataType.Dictionary dictionary:
                {
                    sb.Append(indent);
                    sb.AppendLine("{");
                    string dictIndent = indent + "    ";

                    sb.Append(dictIndent);
                    sb.AppendLine("size += 2;"); // {}

                    string kvpVar = $"kvp{depth}";
                    string countVar = $"count{depth}";
                    sb.Append(dictIndent);
                    sb.AppendLine($"int {countVar} = 0;");

                    sb.Append(dictIndent);
                    sb.Append("foreach (var ");
                    sb.Append(kvpVar);
                    sb.Append(" in ");
                    sb.Append(valueAccessor);
                    sb.AppendLine(")");
                    sb.Append(dictIndent);
                    sb.AppendLine("{");

                    string loopIndent = dictIndent + "    ";

                    sb.Append(loopIndent);
                    sb.AppendLine($"{countVar}++;");

                    // Key
                    sb.Append(loopIndent);
                    sb.AppendLine("size += 3;"); // "":
                    GenerateSizeValue(sb, dictionary.KeyType, $"{kvpVar}.Key", loopIndent, depth + 1, true);

                    // Value
                    GenerateSizeValue(sb, dictionary.ValueType, $"{kvpVar}.Value", loopIndent, depth + 1);

                    sb.Append(dictIndent);
                    sb.AppendLine("}"); // end foreach

                    sb.Append(dictIndent);
                    sb.AppendLine($"if ({countVar} > 1) size += {countVar} - 1;"); // Commas

                    sb.Append(indent);
                    sb.AppendLine("}"); // end block
                }
                break;



            case GenJsonDataType.Enum enumType:
                GenerateEnumSizeLogic(sb, enumType, valueAccessor, indent, unquoted);
                break;

            case GenJsonDataType.CustomConverter customConverter:
                sb.Append(indent);
                sb.Append("size += ");
                sb.Append(customConverter.ConverterTypeName);
                sb.Append(".GetSize(");
                sb.Append(valueAccessor);
                sb.AppendLine(");");
                break;
        }
    }

    private void GenerateEnumSizeLogic(StringBuilder sb, GenJsonDataType.Enum enumType, string valueAccessor, string indent, bool unquoted)
    {
        if (enumType.AsString)
        {
            sb.Append(indent);
            sb.Append("size += ");
            sb.Append(valueAccessor);
            sb.AppendLine(" switch");
            sb.Append(indent);
            sb.AppendLine("{");
            foreach (var member in enumType.Members.Value)
            {
                sb.Append(indent);
                sb.Append("    ");
                sb.Append(enumType.TypeName);
                sb.Append(".");
                sb.Append(member);
                sb.Append(" => ");
                sb.Append(member.Length + (unquoted ? 0 : 2));
                sb.AppendLine(",");
            }
            sb.Append(indent);
            sb.Append("    _ => ");
            sb.Append("global::GenJson.GenJsonSizeHelper.GetSize((");
            sb.Append(enumType.UnderlyingType);
            sb.Append(")");
            sb.Append(valueAccessor);
            sb.Append(")");
            if (!unquoted) sb.Append(" + 2");
            sb.AppendLine(",");
            sb.Append(indent);
            sb.AppendLine("};");
        }
        else
        {
            sb.Append(indent);
            sb.Append("size += global::GenJson.GenJsonSizeHelper.GetSize((");
            sb.Append(enumType.UnderlyingType);
            sb.Append(")");
            sb.Append(valueAccessor);
            sb.AppendLine(");");
        }
    }

    private void GenerateEnumWriteLogic(StringBuilder sb, GenJsonDataType.Enum enumType, string valueAccessor, string indent, bool forceQuotes)
    {
        sb.Append(indent);
        if (enumType.AsString)
        {
            sb.Append("switch (");
            sb.Append(valueAccessor);
            sb.AppendLine(")");
            sb.Append(indent);
            sb.AppendLine("{");
            foreach (var member in enumType.Members.Value)
            {
                sb.Append(indent);
                sb.Append("    case ");
                sb.Append(enumType.TypeName);
                sb.Append(".");
                sb.Append(member);
                sb.AppendLine(":");
                sb.Append(indent);
                sb.Append("        global::GenJson.GenJsonWriter.WriteString(span, ref index, \"");
                sb.Append(member);
                sb.AppendLine("\");");
                sb.Append(indent);
                sb.AppendLine("        break;");
            }
            sb.Append(indent);
            sb.AppendLine("    default:");
            sb.Append(indent);
            sb.AppendLine("        span[index++] = '\"';");
            sb.Append(indent);
            sb.Append("        if (!");
            sb.Append($"(({enumType.UnderlyingType}){valueAccessor})");
            sb.AppendLine(".TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
            sb.Append(indent);
            sb.AppendLine("        { throw new System.Exception(\"Buffer too small\"); }");
            sb.Append(indent);
            sb.AppendLine("        index += written;");
            sb.Append(indent);
            sb.AppendLine("        span[index++] = '\"';");
            sb.Append(indent);
            sb.AppendLine("        break;");
            sb.Append(indent);
            sb.AppendLine("}");
        }
        else
        {
            if (forceQuotes)
            {
                sb.AppendLine("span[index++] = '\"';");
                sb.Append(indent);
            }

            sb.Append("{ if (!");
            sb.Append($"(({enumType.UnderlyingType}){valueAccessor}).TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture)");
            sb.AppendLine(") throw new System.Exception(\"Buffer too small\"); index += written; }");

            if (forceQuotes)
            {
                sb.Append(indent);
                sb.AppendLine("span[index++] = '\"';");
            }
        }
    }

    private void GenerateWriteJsonValue(StringBuilder sb, GenJsonDataType type, string valueAccessor, string indent, int depth)
    {
        switch (type)
        {
            case GenJsonDataType.Primitive:
                sb.Append(indent);
                sb.Append("{ ");
                sb.Append("if (!");
                sb.Append(valueAccessor);
                sb.AppendLine(".TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
                sb.Append(indent);
                sb.AppendLine("{ throw new System.Exception(\"Buffer too small (Primitive)\"); }");
                sb.Append(indent);
                sb.AppendLine("index += written;");
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Boolean:
                sb.Append(indent);
                sb.Append("if (");
                sb.Append(valueAccessor);
                sb.AppendLine(") { span[index++] = 't'; span[index++] = 'r'; span[index++] = 'u'; span[index++] = 'e'; }");
                sb.Append(indent);
                sb.AppendLine("else { span[index++] = 'f'; span[index++] = 'a'; span[index++] = 'l'; span[index++] = 's'; span[index++] = 'e'; }");
                break;

            case GenJsonDataType.Char:
                sb.Append(indent);
                sb.AppendLine("span[index++] = '\"';");
                sb.Append(indent);
                sb.Append("span[index++] = ");
                sb.Append(valueAccessor);
                sb.AppendLine(";");
                sb.Append(indent);
                sb.AppendLine("span[index++] = '\"';");
                break;

            case GenJsonDataType.String:
                sb.Append(indent);
                sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, ");
                sb.Append(valueAccessor);
                sb.AppendLine(");");
                break;

            case GenJsonDataType.FloatingPoint fp:
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.Append("    if (!");
                sb.Append(valueAccessor);
                string fmt = (!fp.TypeName.EndsWith("Decimal") && !fp.TypeName.EndsWith("decimal")) ? "R" : "G";
                sb.Append($".TryFormat(span.Slice(index), out int written, \"{fmt}\", System.Globalization.CultureInfo.InvariantCulture))");
                sb.Append(indent);
                sb.AppendLine("    { throw new System.Exception(\"Buffer too small (FloatingPoint)\"); }");
                sb.Append(indent);
                sb.AppendLine("    index += written;");
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Guid:
            case GenJsonDataType.Version:
            case GenJsonDataType.TimeSpan:
            case GenJsonDataType.DateTime:
            case GenJsonDataType.DateTimeOffset:
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    span[index++] = '\"';");
                sb.Append(indent);
                sb.Append("    if (!");
                sb.Append(valueAccessor);
                string? fmt2 = (type is GenJsonDataType.DateTime || type is GenJsonDataType.DateTimeOffset) ? "O" :
                             (type is GenJsonDataType.TimeSpan) ? "c" : default;
                sb.Append(fmt2 != null
                    ? $".TryFormat(span.Slice(index), out int written, \"{fmt2}\", System.Globalization.CultureInfo.InvariantCulture))"
                    : $".TryFormat(span.Slice(index), out int written))");

                sb.Append(indent);
                sb.AppendLine("    { throw new System.Exception(\"Buffer too small (Formatted)\"); }");
                sb.Append(indent);
                sb.AppendLine("    index += written;");
                sb.Append(indent);
                sb.AppendLine("    span[index++] = '\"';");
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Object:
                sb.Append(indent);
                sb.Append(valueAccessor);
                sb.AppendLine(".WriteJson(span, ref index);");
                break;

            case GenJsonDataType.Nullable nullable:
                sb.Append(indent);
                sb.Append("if (");
                sb.Append(valueAccessor);
                sb.AppendLine(" is null)");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    span[index++] = 'n'; span[index++] = 'u'; span[index++] = 'l'; span[index++] = 'l';");
                sb.Append(indent);
                sb.AppendLine("}");
                sb.Append(indent);
                sb.AppendLine("else");
                sb.Append(indent);
                sb.AppendLine("{");
                GenerateWriteJsonValue(sb, nullable.Underlying, $"{valueAccessor}.Value", indent + "    ", depth);
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Enumerable enumerable:
                {
                    sb.Append(indent);
                    sb.AppendLine("{");

                    string arrayIndent = indent + "    ";

                    sb.Append(arrayIndent);
                    sb.AppendLine("span[index++] = '[';");

                    sb.Append(arrayIndent);
                    string firstItemVar = $"firstItem{depth}";
                    sb.Append("bool ");
                    sb.Append(firstItemVar);
                    sb.AppendLine(" = true;");

                    string itemVar = $"item{depth}";

                    sb.Append(arrayIndent);
                    sb.Append("foreach (var ");
                    sb.Append(itemVar);
                    sb.Append(" in ");
                    sb.Append(valueAccessor);
                    sb.AppendLine(")");
                    sb.Append(arrayIndent);
                    sb.AppendLine("{");

                    string loopIndent = arrayIndent + "    ";

                    sb.Append(loopIndent);
                    sb.Append("if (!");
                    sb.Append(firstItemVar);
                    sb.AppendLine(")");
                    sb.Append(loopIndent);
                    sb.AppendLine("{");
                    sb.Append(loopIndent);
                    sb.AppendLine("    span[index++] = ',';");
                    sb.Append(loopIndent);
                    sb.AppendLine("}");
                    sb.Append(loopIndent);
                    sb.Append(firstItemVar);
                    sb.AppendLine(" = false;");

                    GenerateWriteJsonValue(sb, enumerable.ElementType, itemVar, loopIndent, depth + 1);

                    sb.Append(arrayIndent);
                    sb.AppendLine("}"); // end foreach

                    sb.Append(arrayIndent);
                    sb.AppendLine("span[index++] = ']';");

                    sb.Append(indent);
                    sb.AppendLine("}"); // end block
                }
                break;

            case GenJsonDataType.Dictionary dictionary:
                {
                    sb.Append(indent);
                    sb.AppendLine("{");

                    string dictIndent = indent + "    ";

                    sb.Append(dictIndent);
                    sb.AppendLine("span[index++] = '{';");

                    sb.Append(dictIndent);
                    string firstItemVarDict = $"firstItem{depth}";
                    sb.Append("bool ");
                    sb.Append(firstItemVarDict);
                    sb.AppendLine(" = true;");

                    string kvpVar = $"kvp{depth}";

                    sb.Append(dictIndent);
                    sb.Append("foreach (var ");
                    sb.Append(kvpVar);
                    sb.Append(" in ");
                    sb.Append(valueAccessor);
                    sb.AppendLine(")");
                    sb.Append(dictIndent);
                    sb.AppendLine("{");

                    string loopIndentDict = dictIndent + "    ";

                    sb.Append(loopIndentDict);
                    sb.Append("if (!");
                    sb.Append(firstItemVarDict);
                    sb.AppendLine(")");
                    sb.Append(loopIndentDict);
                    sb.AppendLine("{");
                    sb.Append(loopIndentDict);
                    sb.AppendLine("    span[index++] = ',';");
                    sb.Append(loopIndentDict);
                    sb.AppendLine("}");
                    sb.Append(loopIndentDict);
                    sb.Append(firstItemVarDict);
                    sb.AppendLine(" = false;");

                    // Key
                    sb.Append(loopIndentDict);
                    switch (dictionary.KeyType)
                    {
                        case GenJsonDataType.Enum en:
                            GenerateEnumWriteLogic(sb, en, $"{kvpVar}.Key", loopIndentDict, forceQuotes: true);
                            break;
                        case GenJsonDataType.Primitive:
                        case GenJsonDataType.FloatingPoint:
                        case GenJsonDataType.Guid:
                        case GenJsonDataType.DateTime:
                        case GenJsonDataType.TimeSpan:
                        case GenJsonDataType.DateTimeOffset:
                            {
                                sb.AppendLine("span[index++] = '\"';");
                                sb.Append(loopIndentDict);

                                string keyVal = $"{kvpVar}.Key";

                                string? fmtKey = dictionary.KeyType switch
                                {
                                    GenJsonDataType.FloatingPoint fp => !fp.TypeName.EndsWith("Decimal") ? "R" : "G",
                                    GenJsonDataType.DateTime or GenJsonDataType.DateTimeOffset => "O",
                                    GenJsonDataType.TimeSpan => "c",
                                    _ => null
                                };

                                sb.Append("{ if (!");
                                sb.Append(keyVal);
                                sb.Append(fmtKey != null
                                    ? $".TryFormat(span.Slice(index), out int written, \"{fmtKey}\", System.Globalization.CultureInfo.InvariantCulture))"
                                    : $".TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");

                                sb.Append(loopIndentDict);
                                sb.AppendLine("{ throw new System.Exception(\"Buffer too small (Key)\"); }");
                                sb.Append(loopIndentDict);
                                sb.AppendLine("index += written; }");
                                sb.Append(loopIndentDict);
                                sb.AppendLine("span[index++] = '\"';");
                                break;
                            }
                        case GenJsonDataType.String:
                            sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, ");
                            sb.Append(kvpVar);
                            sb.AppendLine(".Key);");
                            break;
                        default:
                            sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, ");
                            sb.Append(kvpVar);
                            sb.AppendLine(".Key.ToString());");
                            break;
                    }
                    sb.Append(loopIndentDict);
                    sb.AppendLine("span[index++] = ':';");

                    // Value
                    GenerateWriteJsonValue(sb, dictionary.ValueType, $"{kvpVar}.Value", loopIndentDict, depth + 1);

                    sb.Append(dictIndent);
                    sb.AppendLine("}"); // end foreach

                    sb.Append(dictIndent);
                    sb.AppendLine("span[index++] = '}';");

                    sb.Append(indent);
                    sb.AppendLine("}"); // end block
                }
                break;

            case GenJsonDataType.Enum enumType:
                GenerateEnumWriteLogic(sb, enumType, valueAccessor, indent, forceQuotes: false);
                break;

            case GenJsonDataType.CustomConverter customConverter:
                sb.Append(indent);
                sb.Append(customConverter.ConverterTypeName);
                sb.Append(".WriteJson(span, ref index, ");
                sb.Append(valueAccessor);
                sb.AppendLine(");");
                break;
        }
    }
}
