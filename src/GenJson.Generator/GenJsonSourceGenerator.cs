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

public record PropertyData(string Name, string JsonName, string TypeName, bool IsNullable, bool IsValueType, GenJsonDataType Type, string? ConstructorParamName = null);

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

        if (typeSymbol.DeclaringSyntaxReferences.Length > 1)
        {
            var firstRef = typeSymbol.DeclaringSyntaxReferences[0];
            if (firstRef.SyntaxTree != context.Node.SyntaxTree || firstRef.Span != context.Node.Span)
            {
                return null; // We only want to generate code once for partial classes
            }
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

                    var declaredType = propertySymbol.Type;
                    if (declaredType.IsValueType && declaredType is INamedTypeSymbol namedType && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    {
                        declaredType = namedType.TypeArguments[0];
                    }

                    var nonNullableType = declaredType.WithNullableAnnotation(NullableAnnotation.None);
                    var typeName = nonNullableType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                    var propData = new PropertyData(propertySymbol.Name, propName, typeName, isNullable, isValueType, type);

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

                        var newPropData = propData with { Type = newType, JsonName = newJsonName, ConstructorParamName = param.Name };

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
                            parameterSymbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute") ??
                            type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute");

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
        if (data.IsAbstract && data.DerivedTypes.Value.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable");
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

        GenerateCalculateJsonSize(sb, data, allProperties, newModifier, false);
        GenerateCalculateJsonSize(sb, data, allProperties, newModifier, true);
        GenerateToJson(sb, newModifier, false);
        GenerateToJson(sb, newModifier, true);
        GenerateWriteJson(sb, data, allProperties, newModifier, false);
        GenerateWriteJson(sb, data, allProperties, newModifier, true);
        GenerateWriteJsonContent(sb, data, allProperties, newModifier, false);
        GenerateWriteJsonContent(sb, data, allProperties, newModifier, true);
        sb.AppendLine();
        sb.AppendLine();

        GenerateFromJson(sb, data, newModifier, false);
        GenerateFromJson(sb, data, newModifier, true);
        GenerateParse(sb, data, allProperties, newModifier, false);
        GenerateParse(sb, data, allProperties, newModifier, true);

        sb.AppendLine("    }");

        if (!string.IsNullOrEmpty(data.Namespace))
        {
            sb.AppendLine("}");
        }

        context.AddSource($"{data.ClassName}.GenJson.g.cs", sb.ToString());
    }

    private void GenerateParseValue(StringBuilder sb, GenJsonDataType type, string targetVar, string indent, int depth, bool isUtf8, string? explicitCountVar = null)
    {
        string parseMethod = isUtf8 ? "ParseUtf8" : "Parse";
        string parserPrefix = "TryParse";
        string spanType = isUtf8 ? "byte" : "char";

        switch (type)
        {
            case GenJsonDataType.CustomConverter customConverter:
                sb.Append(indent);
                sb.Append(targetVar);
                sb.Append(" = ");
                sb.Append(customConverter.ConverterTypeName);
                if (isUtf8)
                {
                    sb.Append(".FromJsonUtf8(json, ref index);");
                }
                else
                {
                    sb.Append(".FromJson(json, ref index);"); // Assuming custom converter handles span type
                }
                sb.AppendLine();
                break;

            case GenJsonDataType.Boolean:
                sb.Append(indent);
                sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}Boolean(json, ref index, out {targetVar}))");
                sb.Append($" return null;"); sb.AppendLine();
                break;

            case GenJsonDataType.String:
                sb.Append(indent);
                sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}String(json, ref index, out {targetVar}))");
                sb.Append($" return null;"); sb.AppendLine();
                break;

            case GenJsonDataType.Char:
                sb.Append(indent);
                sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}Char(json, ref index, out {targetVar}))");
                sb.Append($" return null;"); sb.AppendLine();
                break;

            case GenJsonDataType.FloatingPoint p:
                sb.Append(indent);
                sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}{GetPrimitiveParserName(p.TypeName)}(json, ref index, out {targetVar}))");
                sb.Append($" return null;"); sb.AppendLine();
                break;

            case GenJsonDataType.Guid:
                sb.Append(indent);
                sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}String(json, ref index, out var {targetVar}_str) || !System.Guid.TryParse({targetVar}_str, out System.Guid {targetVar}_val))");
                sb.Append($" return null;"); sb.AppendLine();
                sb.Append(indent);
                sb.AppendLine($"{targetVar} = {targetVar}_val;");
                break;

            case GenJsonDataType.DateTime:
                sb.Append(indent);
                sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}String(json, ref index, out var {targetVar}_str) || !System.DateTime.TryParse({targetVar}_str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out System.DateTime {targetVar}_val))");
                sb.Append($" return null;"); sb.AppendLine();
                sb.Append(indent);
                sb.AppendLine($"{targetVar} = {targetVar}_val;");
                break;

            case GenJsonDataType.TimeSpan:
                sb.Append(indent);
                sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}String(json, ref index, out var {targetVar}_str) || !System.TimeSpan.TryParse({targetVar}_str, out System.TimeSpan {targetVar}_val))");
                sb.Append($" return null;"); sb.AppendLine();
                sb.Append(indent);
                sb.AppendLine($"{targetVar} = {targetVar}_val;");
                break;

            case GenJsonDataType.DateTimeOffset:
                sb.Append(indent);
                sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}String(json, ref index, out var {targetVar}_str) || !System.DateTimeOffset.TryParse({targetVar}_str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out System.DateTimeOffset {targetVar}_val))");
                sb.Append($" return null;"); sb.AppendLine();
                sb.Append(indent);
                sb.AppendLine($"{targetVar} = {targetVar}_val;");
                break;

            case GenJsonDataType.Version:
                sb.Append(indent);
                sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}String(json, ref index, out var {targetVar}_str) || !System.Version.TryParse({targetVar}_str, out System.Version? {targetVar}_val))");
                sb.Append($" return null;"); sb.AppendLine();
                sb.Append(indent);
                sb.AppendLine($"{targetVar} = {targetVar}_val;");
                break;

            case GenJsonDataType.Enum en:
                GenerateEnumParseLogic(sb, en, targetVar, indent, isUtf8);
                break;

            case GenJsonDataType.Primitive p:
                sb.Append(indent);
                sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}{GetPrimitiveParserName(p.TypeName)}(json, ref index, out {targetVar}))");
                sb.Append($" return null;"); sb.AppendLine();
                break;

            case GenJsonDataType.Object o:
                sb.Append(indent);
                sb.Append(targetVar);
                sb.Append(" = ");
                sb.Append(o.TypeName); // Assuming generated class has Parse
                sb.Append($".{parseMethod}(json, ref index);");
                sb.AppendLine();
                break;

            case GenJsonDataType.Nullable n:
                sb.Append(indent);
                sb.Append($"if (global::GenJson.GenJsonParser.{parserPrefix}Null(json, ref index))");
                sb.AppendLine();
                sb.Append(indent);
                sb.AppendLine("{");
                //NOTE: This is not really necessary now
                sb.Append(indent);
                sb.Append("    ");
                sb.Append(targetVar);
                sb.AppendLine(" = null;");
                sb.Append(indent);
                sb.AppendLine("}");
                sb.Append(indent);
                sb.AppendLine("else");
                sb.Append(indent);
                sb.AppendLine("{");
                GenerateParseValue(sb, n.Underlying, targetVar, indent + "    ", depth, isUtf8, explicitCountVar);
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
                    sb.Append($"if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, {(isUtf8 ? "(byte)'['" : "'['")})) return null;");
                    sb.AppendLine();

                    sb.Append(scopedIndent);
                    string countVar = $"count{depth}";
                    if (explicitCountVar != null)
                    {
                        sb.Append(scopedIndent);
                        sb.Append($"int {countVar} = ({explicitCountVar} >= 0) ? {explicitCountVar} : ");
                        sb.AppendLine($"global::GenJson.GenJsonParser.CountListItems(json, index);");
                    }
                    else
                    {
                        sb.Append(scopedIndent);
                        sb.AppendLine($"var {countVar} = global::GenJson.GenJsonParser.CountListItems(json, index);");
                    }

                    sb.Append(scopedIndent);
                    if (e.IsArray)
                    {
                        string ctorType = e.ElementTypeName;
                        int arrayRankStart = -1;
                        int genericDepth = 0;
                        for (int i = 0; i < ctorType.Length; i++)
                        {
                            if (ctorType[i] == '<') genericDepth++;
                            else if (ctorType[i] == '>') genericDepth--;
                            else if (genericDepth == 0 && ctorType[i] == '[' && i + 1 < ctorType.Length && ctorType[i + 1] == ']')
                            {
                                arrayRankStart = i;
                                break;
                            }
                        }
                        if (arrayRankStart >= 0)
                        {
                            sb.AppendLine($"var {listVar} = new {ctorType.Substring(0, arrayRankStart)}[{countVar}]{ctorType.Substring(arrayRankStart)};");
                        }
                        else
                        {
                            sb.AppendLine($"var {listVar} = new {ctorType}[{countVar}];");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"var {listVar} = new {e.ConstructionTypeName}({countVar});");
                    }

                    string itemIndexVar = $"itemIndex{depth}";
                    sb.Append(scopedIndent);
                    sb.AppendLine($"var {itemIndexVar} = 0;");

                    sb.Append(scopedIndent);
                    sb.AppendLine("while (index < json.Length)");
                    sb.Append(scopedIndent);
                    sb.AppendLine("{");

                    string loopIndent = scopedIndent + "    ";

                    sb.Append(loopIndent);
                    sb.Append($"if (index < json.Length && json[index] == {(isUtf8 ? "(byte)']'" : "']'")})");
                    sb.AppendLine();
                    sb.Append(loopIndent);
                    sb.AppendLine("{");
                    sb.Append(loopIndent + "    ");
                    sb.AppendLine("index++;");
                    sb.Append(loopIndent + "    ");
                    sb.Append(targetVar);
                    sb.AppendLine($" = {listVar};");
                    sb.Append(loopIndent + "    ");
                    sb.AppendLine("break;");
                    sb.Append(loopIndent);
                    sb.AppendLine("}");

                    string itemVar = $"item{depth}";
                    sb.Append(loopIndent);
                    sb.Append($"{e.ElementTypeName}");
                    if (!e.IsElementValueType && !e.ElementTypeName.EndsWith("?")) sb.Append("?");
                    sb.AppendLine($" {itemVar} = default;");

                    GenerateParseValue(sb, e.ElementType, itemVar, loopIndent, depth + 1, isUtf8, null);

                    if (e.IsArray)
                    {
                        sb.Append(loopIndent);
                        sb.Append($"{listVar}[{itemIndexVar}++] = {itemVar}");
                        if (!e.IsElementValueType) sb.Append("!");
                        sb.AppendLine(";");
                    }
                    else
                    {
                        sb.Append(loopIndent);
                        sb.Append($"{listVar}.Add({itemVar}");
                        if (!e.IsElementValueType) sb.Append("!");
                        sb.AppendLine(");");
                    }

                    sb.Append(loopIndent);
                    sb.Append($"if (index < json.Length && json[index] == {(isUtf8 ? "(byte)','" : "','")})");
                    sb.AppendLine();
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
                    sb.Append($"if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, {(isUtf8 ? "(byte)'{'" : "'{'")})) return null;");
                    sb.AppendLine();

                    string countVar = $"count{depth}";
                    if (explicitCountVar != null)
                    {
                        sb.Append(scopedIndent);
                        sb.Append($"int {countVar} = ({explicitCountVar} >= 0) ? {explicitCountVar} : ");
                        sb.AppendLine($"global::GenJson.GenJsonParser.CountDictionaryItems(json, index);");
                    }
                    else
                    {
                        sb.Append(scopedIndent);
                        sb.AppendLine($"var {countVar} = global::GenJson.GenJsonParser.CountDictionaryItems(json, index);");
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
                    sb.Append($"if (index < json.Length && json[index] == {(isUtf8 ? "(byte)'}'" : "'}'")})");
                    sb.AppendLine();
                    sb.Append(loopIndent);
                    sb.AppendLine("{");
                    sb.Append(loopIndent + "    ");
                    sb.AppendLine("index++;");
                    sb.Append(loopIndent + "    ");
                    sb.Append(targetVar);
                    sb.AppendLine($" = {dictVar};");
                    sb.Append(loopIndent + "    ");
                    sb.AppendLine("break;");
                    sb.Append(loopIndent);
                    sb.AppendLine("}");

                    string keyVar = $"key{depth}";
                    sb.Append(loopIndent);
                    sb.Append($"{d.KeyTypeName} {keyVar} = default!;");
                    sb.AppendLine();

                    string keyStrVar = $"keyStr{depth}";
                    string escapedVar = $"escaped{depth}";
                    sb.Append(loopIndent);
                    sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}StringSpan(json, ref index, out var {keyStrVar}, out var {escapedVar})) return null;");
                    sb.AppendLine();

                    if (!(d.KeyType is GenJsonDataType.String))
                    {
                        sb.Append(loopIndent);
                        sb.AppendLine($"if ({escapedVar}) return null;");
                    }

                    sb.Append(loopIndent);
                    sb.Append($"if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, {(isUtf8 ? "(byte)':'" : "':'")})) return null;");
                    sb.AppendLine();

                    string skipInvalidDictionaryEnumKey;
                    if (d.KeyType is GenJsonDataType.Enum enumKeyTypeForFallback && enumKeyTypeForFallback.FallbackValue != null)
                    {
                        skipInvalidDictionaryEnumKey = $"{{ global::GenJson.GenJsonParser.TrySkipValue(json, ref index); if (index < json.Length && json[index] == {(isUtf8 ? "(byte)','" : "','")}) index++; continue; }}";
                    }
                    else
                    {
                        skipInvalidDictionaryEnumKey = $"return null;";
                    }

                    if (d.KeyType is GenJsonDataType.Enum enumKeyType)
                    {
                        if (enumKeyType.AsString)
                        {
                            sb.Append(loopIndent);
                            foreach (var member in enumKeyType.Members.Value)
                            {
                                if (isUtf8)
                                {
                                    var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(member);
                                    var utf8BytesStr = string.Join(", ", utf8Bytes.Select(b => $"(byte){b}"));
                                    sb.AppendLine($"if (global::System.MemoryExtensions.SequenceEqual({keyStrVar}, new byte[] {{ {utf8BytesStr} }})) {keyVar} = {enumKeyType.TypeName}.{member};");
                                }
                                else
                                {
                                    sb.AppendLine($"if (global::System.MemoryExtensions.SequenceEqual({keyStrVar}, global::System.MemoryExtensions.AsSpan(\"{member}\"))) {keyVar} = {enumKeyType.TypeName}.{member};");
                                }
                                sb.Append(loopIndent);
                                sb.Append("else ");
                            }
                            sb.AppendLine(skipInvalidDictionaryEnumKey);
                        }
                        else
                        {
                            sb.Append(loopIndent);
                            sb.AppendLine($"{enumKeyType.UnderlyingType} dictEnumVal{depth};");
                            sb.Append(loopIndent);
                            if (isUtf8)
                            {
                                sb.AppendLine($"if (!global::System.Buffers.Text.Utf8Parser.TryParse({keyStrVar}, out dictEnumVal{depth}, out _)) {skipInvalidDictionaryEnumKey}");
                            }
                            else
                            {
                                sb.AppendLine($"if (!{enumKeyType.UnderlyingType}.TryParse({keyStrVar}, out dictEnumVal{depth})) {skipInvalidDictionaryEnumKey}");
                            }
                            sb.Append(loopIndent);
                            sb.AppendLine($"if (!System.Enum.IsDefined<{enumKeyType.TypeName}>(({enumKeyType.TypeName})dictEnumVal{depth})) {skipInvalidDictionaryEnumKey}");
                            sb.Append(loopIndent);
                            sb.AppendLine($"{keyVar} = ({enumKeyType.TypeName})dictEnumVal{depth};");
                        }
                    }
                    else
                    {
                        switch (d.KeyType)
                        {
                            case GenJsonDataType.String:
                                sb.Append(loopIndent);
                                sb.AppendLine($"{keyVar} = {escapedVar} ? global::GenJson.GenJsonParser.{(isUtf8 ? "UnescapeStringUtf8" : "UnescapeString")}({keyStrVar}) : {(isUtf8 ? $"global::System.Text.Encoding.UTF8.GetString({keyStrVar})" : $"new string({keyStrVar})")};");
                                break;
                            case GenJsonDataType.Primitive:
                            case GenJsonDataType.FloatingPoint:
                            case GenJsonDataType.Guid:
                            case GenJsonDataType.DateTime:
                            case GenJsonDataType.TimeSpan:
                            case GenJsonDataType.DateTimeOffset:
                            case GenJsonDataType.Version:
                                {
                                    sb.Append(loopIndent);
                                    if (isUtf8)
                                    {
                                        if (d.KeyType is GenJsonDataType.Version)
                                        {
                                            sb.AppendLine($"if (!{d.KeyTypeName}.TryParse(global::System.Text.Encoding.UTF8.GetString({keyStrVar}), out {keyVar})) return null;");
                                        }
                                        else
                                        {
                                            char fmtChar = (d.KeyType is GenJsonDataType.DateTime || d.KeyType is GenJsonDataType.DateTimeOffset) ? 'O' :
                                                       (d.KeyType is GenJsonDataType.TimeSpan) ? 'c' :
                                                       (d.KeyType is GenJsonDataType.Guid) ? 'D' : default;

                                            if (fmtChar != default(char))
                                            {
                                                sb.AppendLine($"if (!global::System.Buffers.Text.Utf8Parser.TryParse({keyStrVar}, out {keyVar}, out _, '{fmtChar}')) return null;");
                                            }
                                            else
                                            {
                                                sb.AppendLine($"if (!global::System.Buffers.Text.Utf8Parser.TryParse({keyStrVar}, out {keyVar}, out _)) return null;");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        string parseMethodName = d.KeyType is GenJsonDataType.FloatingPoint ? "TryParse" : "TryParse";
                                        string cultureArg = d.KeyType is GenJsonDataType.FloatingPoint ? ", System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture" : "";
                                        if (d.KeyType is GenJsonDataType.DateTime || d.KeyType is GenJsonDataType.DateTimeOffset)
                                        {
                                            cultureArg = ", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind";
                                        }
                                        sb.AppendLine($"if (!{d.KeyTypeName}.{parseMethodName}({keyStrVar}{cultureArg}, out {keyVar})) return null;");
                                    }
                                    break;
                                }
                            default:
                                sb.Append(loopIndent);
                                if (isUtf8)
                                {
                                    sb.AppendLine($"if (!global::System.Buffers.Text.Utf8Parser.TryParse({keyStrVar}, out {keyVar}, out _)) return null;");
                                }
                                else
                                {
                                    sb.AppendLine($"if (!{d.KeyTypeName}.TryParse({keyStrVar}, out {keyVar})) return null;");
                                }
                                break;
                        }
                    }

                    string valVar = $"val{depth}";
                    sb.Append(loopIndent);
                    sb.Append($"{d.ValueTypeName}");
                    if (!d.IsValueValueType && !d.ValueTypeName.EndsWith("?")) sb.Append("?");
                    sb.AppendLine($" {valVar} = default;");

                    GenerateParseValue(sb, d.ValueType, valVar, loopIndent, depth + 1, isUtf8, null);

                    sb.Append(loopIndent);
                    sb.Append($"{dictVar}.Add({keyVar}, {valVar}");
                    if (!d.IsValueValueType) sb.Append("!");
                    sb.AppendLine(");");

                    sb.Append(loopIndent);
                    sb.Append($"if (index < json.Length && json[index] == {(isUtf8 ? "(byte)','" : "','")})");
                    sb.AppendLine();
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



    private void GenerateSizeValue(StringBuilder sb, GenJsonDataType type, string valueAccessor, string indent, int depth, bool isUtf8, bool unquoted = false)
    {
        string sizeHelper = isUtf8 ? "global::GenJson.GenJsonSizeHelper.GetSizeUtf8" : "global::GenJson.GenJsonSizeHelper.GetSize";
        string calcMethod = isUtf8 ? "CalculateJsonSizeUtf8" : "CalculateJsonSize";

        switch (type)
        {
            case GenJsonDataType.Primitive:
            case GenJsonDataType.Boolean:
            case GenJsonDataType.FloatingPoint:
                sb.Append(indent);
                sb.Append($"size += {sizeHelper}(");
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
                sb.Append($"size += {sizeHelper}(");
                sb.Append(valueAccessor);
                sb.Append(")");
                if (unquoted) sb.Append(" - 2");
                sb.AppendLine(";");
                break;

            case GenJsonDataType.Object:
                sb.Append(indent);
                sb.Append("size += ");
                sb.Append(valueAccessor);
                sb.AppendLine($".{calcMethod}();");
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
                GenerateSizeValue(sb, nullable.Underlying, $"{valueAccessor}.Value", indent + "    ", depth, isUtf8, unquoted);
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

                    GenerateSizeValue(sb, enumerable.ElementType, itemVar, loopIndent, depth + 1, isUtf8);

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
                    GenerateSizeValue(sb, dictionary.KeyType, $"{kvpVar}.Key", loopIndent, depth + 1, isUtf8, true);

                    // Value
                    GenerateSizeValue(sb, dictionary.ValueType, $"{kvpVar}.Value", loopIndent, depth + 1, isUtf8);

                    sb.Append(dictIndent);
                    sb.AppendLine("}"); // end foreach

                    sb.Append(dictIndent);
                    sb.AppendLine($"if ({countVar} > 1) size += {countVar} - 1;"); // Commas

                    sb.Append(indent);
                    sb.AppendLine("}"); // end block
                }
                break;



            case GenJsonDataType.Enum enumType:
                GenerateEnumSizeLogic(sb, enumType, valueAccessor, indent, isUtf8, unquoted);
                break;

            case GenJsonDataType.CustomConverter customConverter:
                sb.Append(indent);
                sb.Append("size += ");
                sb.Append(customConverter.ConverterTypeName);
                sb.Append($".{(isUtf8 ? "GetSizeUtf8" : "GetSize")}(");
                sb.Append(valueAccessor);
                sb.AppendLine(");");
                break;
        }
    }

    private void GenerateEnumSizeLogic(StringBuilder sb, GenJsonDataType.Enum enumType, string valueAccessor, string indent, bool isUtf8, bool unquoted)
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
                int len = isUtf8 ? System.Text.Encoding.UTF8.GetByteCount(member) : member.Length;
                sb.Append(len + (unquoted ? 0 : 2));
                sb.AppendLine(",");
            }
            sb.Append(indent);
            sb.Append("    _ => ");
            sb.Append(isUtf8 ? "global::GenJson.GenJsonSizeHelper.GetSizeUtf8((" : "global::GenJson.GenJsonSizeHelper.GetSize((");
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
            sb.Append("size += ");
            sb.Append(isUtf8 ? "global::GenJson.GenJsonSizeHelper.GetSizeUtf8((" : "global::GenJson.GenJsonSizeHelper.GetSize((");
            sb.Append(enumType.UnderlyingType);
            sb.Append(")");
            sb.Append(valueAccessor);
            sb.AppendLine(");");
        }
    }

    private void GenerateEnumWriteLogic(StringBuilder sb, GenJsonDataType.Enum enumType, string valueAccessor, string indent, bool isUtf8, bool forceQuotes)
    {
        string spanType = isUtf8 ? "byte" : "char";

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
            sb.AppendLine(isUtf8 ? "        span[index++] = (byte)'\"';" : "        span[index++] = '\"';");
            sb.Append(indent);
            if (isUtf8)
            {
                sb.Append("        if (!global::System.Buffers.Text.Utf8Formatter.TryFormat");
                sb.Append($"(({enumType.UnderlyingType}){valueAccessor}, span.Slice(index), out int written))");
            }
            else
            {
                sb.Append("        if (!");
                sb.Append($"(({enumType.UnderlyingType}){valueAccessor})");
                sb.AppendLine(".TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
            }

            if (isUtf8) sb.AppendLine();
            sb.Append(indent);
            sb.AppendLine("        {");
            sb.Append(indent);
            sb.AppendLine("            throw new System.Exception(\"Buffer too small\");");
            sb.Append(indent);
            sb.AppendLine("        }");
            sb.Append(indent);
            sb.AppendLine("        index += written;");
            sb.Append(indent);
            sb.AppendLine(isUtf8 ? "        span[index++] = (byte)'\"';" : "        span[index++] = '\"';");
            sb.Append(indent);
            sb.AppendLine("        break;");
            sb.Append(indent);
            sb.AppendLine("}");
        }
        else
        {
            if (forceQuotes)
            {
                sb.AppendLine(isUtf8 ? "span[index++] = (byte)'\"';" : "span[index++] = '\"';");
                sb.Append(indent);
            }

            sb.AppendLine("{");
            sb.Append(indent);
            sb.Append("    if (!");
            if (isUtf8)
            {
                sb.AppendLine($"global::System.Buffers.Text.Utf8Formatter.TryFormat(({enumType.UnderlyingType}){valueAccessor}, span.Slice(index), out int written))");
            }
            else
            {
                sb.AppendLine($"(({enumType.UnderlyingType}){valueAccessor}).TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
            }
            sb.Append(indent);
            sb.AppendLine("    {");
            sb.Append(indent);
            sb.AppendLine("        throw new System.Exception(\"Buffer too small\");");
            sb.Append(indent);
            sb.AppendLine("    }");
            sb.Append(indent);
            sb.AppendLine("    index += written;");
            sb.Append(indent);
            sb.AppendLine("}");

            if (forceQuotes)
            {
                sb.Append(indent);
                sb.AppendLine(isUtf8 ? "span[index++] = (byte)'\"';" : "span[index++] = '\"';");
            }
        }
    }

    private void GenerateWriteJsonValue(StringBuilder sb, GenJsonDataType type, string valueAccessor, string indent, int depth, bool isUtf8)
    {
        string spanType = isUtf8 ? "byte" : "char";
        string writeMethod = isUtf8 ? "WriteJsonUtf8" : "WriteJson";

        switch (type)
        {
            case GenJsonDataType.Primitive:
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.Append("    if (!");
                if (isUtf8)
                {
                    sb.AppendLine($"global::System.Buffers.Text.Utf8Formatter.TryFormat({valueAccessor}, span.Slice(index), out int written))");
                }
                else
                {
                    sb.AppendLine($"{valueAccessor}.TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
                }
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.AppendLine("        throw new System.Exception(\"Buffer too small (Primitive)\");");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("    index += written;");
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Boolean:
                sb.Append(indent);
                sb.Append("if (");
                sb.Append(valueAccessor);
                if (isUtf8)
                {
                    sb.AppendLine(")");
                    sb.Append(indent);
                    sb.AppendLine("{");
                    sb.Append(indent);
                    sb.AppendLine("    span[index++] = (byte)'t'; span[index++] = (byte)'r'; span[index++] = (byte)'u'; span[index++] = (byte)'e';");
                    sb.Append(indent);
                    sb.AppendLine("}");
                    sb.Append(indent);
                    sb.AppendLine("else");
                    sb.Append(indent);
                    sb.AppendLine("{");
                    sb.Append(indent);
                    sb.AppendLine("    span[index++] = (byte)'f'; span[index++] = (byte)'a'; span[index++] = (byte)'l'; span[index++] = (byte)'s'; span[index++] = (byte)'e';");
                    sb.Append(indent);
                    sb.AppendLine("}");
                }
                else
                {
                    sb.AppendLine(")");
                    sb.Append(indent);
                    sb.AppendLine("{");
                    sb.Append(indent);
                    sb.AppendLine("    span[index++] = 't'; span[index++] = 'r'; span[index++] = 'u'; span[index++] = 'e';");
                    sb.Append(indent);
                    sb.AppendLine("}");
                    sb.Append(indent);
                    sb.AppendLine("else");
                    sb.Append(indent);
                    sb.AppendLine("{");
                    sb.Append(indent);
                    sb.AppendLine("    span[index++] = 'f'; span[index++] = 'a'; span[index++] = 'l'; span[index++] = 's'; span[index++] = 'e';");
                    sb.Append(indent);
                    sb.AppendLine("}");
                }
                break;

            case GenJsonDataType.Char:
                sb.Append(indent);
                sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, ");
                sb.Append(valueAccessor);
                sb.AppendLine(".ToString());");
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

                string fmt = (!fp.TypeName.EndsWith("Decimal") && !fp.TypeName.EndsWith("decimal")) ? "R" : "G";

                if (isUtf8)
                {
                    sb.AppendLine($"global::System.Buffers.Text.Utf8Formatter.TryFormat({valueAccessor}, span.Slice(index), out int written, new global::System.Buffers.StandardFormat('{fmt}')))");
                }
                else
                {
                    sb.AppendLine($"{valueAccessor}.TryFormat(span.Slice(index), out int written, \"{fmt}\", System.Globalization.CultureInfo.InvariantCulture))");
                }
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.AppendLine("        throw new System.Exception(\"Buffer too small (FloatingPoint)\");");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("    index += written;");
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Guid:
            case GenJsonDataType.Version: // Version doesn't support Utf8Formatter directly?
            case GenJsonDataType.TimeSpan:
            case GenJsonDataType.DateTime:
            case GenJsonDataType.DateTimeOffset:
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine(isUtf8 ? "    span[index++] = (byte)'\"';" : "    span[index++] = '\"';");
                sb.Append(indent);
                sb.Append("    if (!");

                if (isUtf8 && !(type is GenJsonDataType.Version)) // Version not supported by Utf8Formatter? Check docs. standard 2.1 no.
                {
                    // Guid, DateTime, DateTimeOffset, TimeSpan supported.
                    char fmtChar = (type is GenJsonDataType.DateTime || type is GenJsonDataType.DateTimeOffset) ? 'O' :
                                   (type is GenJsonDataType.TimeSpan) ? 'c' :
                                   (type is GenJsonDataType.Guid) ? 'D' : default; // Guid default D

                    sb.AppendLine($"global::System.Buffers.Text.Utf8Formatter.TryFormat({valueAccessor}, span.Slice(index), out int written, new global::System.Buffers.StandardFormat('{fmtChar}')))");
                }
                else
                {
                    sb.Append(valueAccessor);
                    string? fmt2 = (type is GenJsonDataType.DateTime || type is GenJsonDataType.DateTimeOffset) ? "O" :
                                 (type is GenJsonDataType.TimeSpan) ? "c" : default;
                    sb.AppendLine(fmt2 != null
                        ? $".TryFormat(span.Slice(index), out int written, \"{fmt2}\", System.Globalization.CultureInfo.InvariantCulture))"
                        : $".TryFormat(span.Slice(index), out int written))");
                }
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.AppendLine("        throw new System.Exception(\"Buffer too small (Formatted)\");");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("    index += written;");
                sb.Append(indent);
                sb.AppendLine(isUtf8 ? "    span[index++] = (byte)'\"';" : "    span[index++] = '\"';");
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Object:
                sb.Append(indent);
                sb.Append(valueAccessor);
                sb.AppendLine($".{writeMethod}(span, ref index);");
                break;

            case GenJsonDataType.Nullable nullable:
                sb.Append(indent);
                sb.Append("if (");
                sb.Append(valueAccessor);
                sb.AppendLine(" is null)");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                if (isUtf8)
                {
                    sb.AppendLine("    span[index++] = (byte)'n'; span[index++] = (byte)'u'; span[index++] = (byte)'l'; span[index++] = (byte)'l';");
                }
                else
                {
                    sb.AppendLine("    span[index++] = 'n'; span[index++] = 'u'; span[index++] = 'l'; span[index++] = 'l';");
                }
                sb.Append(indent);
                sb.AppendLine("}");
                sb.Append(indent);
                sb.AppendLine("else");
                sb.Append(indent);
                sb.AppendLine("{");
                GenerateWriteJsonValue(sb, nullable.Underlying, $"{valueAccessor}.Value", indent + "    ", depth, isUtf8);
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Enumerable enumerable:
                {
                    sb.Append(indent);
                    sb.AppendLine("{");

                    string arrayIndent = indent + "    ";

                    sb.Append(arrayIndent);
                    sb.AppendLine(isUtf8 ? "span[index++] = (byte)'[';" : "span[index++] = '[';");

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
                    sb.AppendLine(isUtf8 ? "    span[index++] = (byte)',';" : "    span[index++] = ',';");
                    sb.Append(loopIndent);
                    sb.AppendLine("}");
                    sb.Append(loopIndent);
                    sb.Append(firstItemVar);
                    sb.AppendLine(" = false;");

                    GenerateWriteJsonValue(sb, enumerable.ElementType, itemVar, loopIndent, depth + 1, isUtf8);

                    sb.Append(arrayIndent);
                    sb.AppendLine("}"); // end foreach

                    sb.Append(arrayIndent);
                    sb.AppendLine(isUtf8 ? "span[index++] = (byte)']';" : "span[index++] = ']';");

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
                    sb.AppendLine(isUtf8 ? "span[index++] = (byte)'{';" : "span[index++] = '{';");

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
                    sb.AppendLine(isUtf8 ? "    span[index++] = (byte)',';" : "    span[index++] = ',';");
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
                            GenerateEnumWriteLogic(sb, en, $"{kvpVar}.Key", loopIndentDict, isUtf8, forceQuotes: true);
                            break;
                        case GenJsonDataType.Primitive:
                        case GenJsonDataType.FloatingPoint:
                        case GenJsonDataType.Guid:
                        case GenJsonDataType.DateTime:
                        case GenJsonDataType.TimeSpan:
                        case GenJsonDataType.DateTimeOffset:
                        case GenJsonDataType.Version:
                            {
                                sb.AppendLine(isUtf8 ? "span[index++] = (byte)'\"';" : "span[index++] = '\"';");
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

                                if (isUtf8)
                                {
                                    char fmtChar2 = (dictionary.KeyType is GenJsonDataType.DateTime || dictionary.KeyType is GenJsonDataType.DateTimeOffset) ? 'O' :
                                                   (dictionary.KeyType is GenJsonDataType.TimeSpan) ? 'c' :
                                                   (dictionary.KeyType is GenJsonDataType.Guid) ? 'D' :
                                                    (dictionary.KeyType is GenJsonDataType.FloatingPoint fp2 && !fp2.TypeName.EndsWith("Decimal")) ? 'R' :
                                                    (dictionary.KeyType is GenJsonDataType.FloatingPoint fp3 && fp3.TypeName.EndsWith("Decimal")) ? 'G' :
                                                    'G';

                                    sb.Append($"global::System.Buffers.Text.Utf8Formatter.TryFormat({keyVal}, span.Slice(index), out int written, new global::System.Buffers.StandardFormat('{fmtChar2}')))");
                                }
                                else
                                {
                                    sb.Append(keyVal);
                                    sb.Append(fmtKey != null
                                        ? $".TryFormat(span.Slice(index), out int written, \"{fmtKey}\", System.Globalization.CultureInfo.InvariantCulture))"
                                        : $".TryFormat(span.Slice(index), out int written))");
                                }

                                sb.Append(loopIndentDict);
                                sb.AppendLine("    { throw new System.Exception(\"Buffer too small (Key)\"); }");
                                sb.Append(loopIndentDict);
                                sb.AppendLine("index += written; }");
                                sb.Append(loopIndentDict);
                                sb.AppendLine(isUtf8 ? "span[index++] = (byte)'\"';" : "span[index++] = '\"';");
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
                    sb.AppendLine(isUtf8 ? "span[index++] = (byte)':';" : "span[index++] = ':';");

                    // Value
                    GenerateWriteJsonValue(sb, dictionary.ValueType, $"{kvpVar}.Value", loopIndentDict, depth + 1, isUtf8);

                    sb.Append(dictIndent);
                    sb.AppendLine("}"); // end foreach

                    sb.Append(dictIndent);
                    sb.AppendLine(isUtf8 ? "span[index++] = (byte)'}';" : "span[index++] = '}';");

                    sb.Append(indent);
                    sb.AppendLine("}"); // end block
                }
                break;

            case GenJsonDataType.Enum enumType:
                GenerateEnumWriteLogic(sb, enumType, valueAccessor, indent, isUtf8, forceQuotes: false);
                break;

            case GenJsonDataType.CustomConverter customConverter:
                sb.Append(indent);
                sb.Append(customConverter.ConverterTypeName);
                sb.Append($".{writeMethod}(span, ref index, ");
                sb.Append(valueAccessor);
                sb.AppendLine(");");
                break;
        }
    }

    private void GenerateCalculateJsonSize(StringBuilder sb, ClassData data, System.Collections.Generic.IReadOnlyList<PropertyData> allProperties, string newModifier, bool isUtf8)
    {
        string methodName = isUtf8 ? "CalculateJsonSizeUtf8" : "CalculateJsonSize";
        string sizeHelper = isUtf8 ? "global::GenJson.GenJsonSizeHelper.GetSizeUtf8" : "global::GenJson.GenJsonSizeHelper.GetSize";

        sb.Append("        public ");
        sb.Append(newModifier);
        sb.AppendLine($"int {methodName}()");
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
                int overhead = (isUtf8 ? System.Text.Encoding.UTF8.GetByteCount(discKey) : discKey.Length) + 3 + 1;
                overhead += isUtf8 ? System.Text.Encoding.UTF8.GetByteCount(derived.DiscriminatorValue) : derived.DiscriminatorValue.Length; // discriminator value includes quotes if string, so byte count is safe
                sb.AppendLine($"                    return d" + derivedIdx + $".{methodName}() + " + overhead + ";");
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
                // For abstract types, we just return here if it was a derived type (returned above) or throw (default case).
                // But wait, the original code had a goto GenerateToJson.
                // If it's abstract, we can't emit the rest of the method which assumes concrete properties?
                // Actually, the original code skipped the rest of the method using `goto GenerateToJson`.
                // So here we should just return.
                sb.AppendLine("        }");
                sb.AppendLine();
                return;
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
                int countOverhead = (isUtf8 ? System.Text.Encoding.UTF8.GetByteCount(prop.JsonName) : prop.JsonName.Length) + 4; // "$Name":
                if (en.IsArray)
                {
                    sb.Append(indent);
                    sb.Append("size += ");
                    sb.Append(countOverhead);
                    sb.AppendLine(";");

                    sb.Append(indent);
                    sb.Append($"size += {sizeHelper}(this.");
                    sb.Append(prop.Name);
                    sb.AppendLine(".Length);");

                    sb.Append(indent);
                    sb.AppendLine("size += 1;"); // Comma
                }
                else
                {
                    sb.Append(indent);
                    sb.AppendLine("{");
                    GenerateGetCollectionCount(sb, indent, prop.Name, en.ElementTypeName);

                    sb.Append(indent);
                    sb.AppendLine("    if (_count >= 0)");
                    sb.Append(indent);
                    sb.AppendLine("    {");
                    sb.Append(indent);
                    sb.Append("        size += ");
                    sb.Append(countOverhead);
                    sb.AppendLine(";");
                    sb.Append(indent);
                    sb.AppendLine($"        size += {sizeHelper}(_count);");
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
                int countOverhead = (isUtf8 ? System.Text.Encoding.UTF8.GetByteCount(prop.JsonName) : prop.JsonName.Length) + 4; // "$Name":
                sb.Append(indent);
                sb.Append("size += ");
                sb.Append(countOverhead);
                sb.AppendLine(";");

                sb.Append(indent);
                sb.Append($"size += {sizeHelper}(this.");
                sb.Append(prop.Name);
                sb.AppendLine(".Count);");

                sb.Append(indent);
                sb.AppendLine("size += 1;"); // Comma
            }

            sb.Append(indent);
            sb.Append("size += ");
            sb.Append((isUtf8 ? System.Text.Encoding.UTF8.GetByteCount(prop.JsonName) : prop.JsonName.Length) + 3); // "Key":
            sb.AppendLine(";");

            GenerateSizeValue(sb, prop.Type, $"this.{prop.Name}", indent, 0, isUtf8);

            if (prop.IsNullable)
            {
                sb.AppendLine("            }");
            }
        }

        sb.AppendLine("            if (propertyCount > 0) size += propertyCount - 1;");
        sb.AppendLine("            return size;");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private void GenerateToJson(StringBuilder sb, string newModifier, bool isUtf8)
    {
        string methodName = isUtf8 ? "ToJsonUtf8" : "ToJson";
        string returnType = isUtf8 ? "byte[]" : "string";
        string methodSig = $"public {newModifier}{returnType} {methodName}()";

        sb.AppendLine($"        {methodSig}");
        sb.AppendLine("        {");
        if (isUtf8)
        {
            sb.AppendLine("            var bytes = new byte[CalculateJsonSizeUtf8()];");
            sb.AppendLine("            System.Span<byte> span = bytes;");
            sb.AppendLine("            int index = 0;");
            sb.AppendLine("            WriteJsonUtf8(span, ref index);");
            sb.AppendLine("            return bytes;");
        }
        else
        {
            sb.AppendLine("            return string.Create(CalculateJsonSize(), this, (span, state) =>");
            sb.AppendLine("            {");
            sb.AppendLine("                int index = 0;");
            sb.AppendLine("                state.WriteJson(span, ref index);");
            sb.AppendLine("            });");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine();
    }

    private void GenerateWriteJson(StringBuilder sb, ClassData data, System.Collections.Generic.IReadOnlyList<PropertyData> allProperties, string newModifier, bool isUtf8)
    {
        string spanType = isUtf8 ? "System.Span<byte>" : "System.Span<char>";
        string methodName = isUtf8 ? "WriteJsonUtf8" : "WriteJson";
        string methodContent = isUtf8 ? "WriteJsonContentUtf8" : "WriteJsonContent";

        sb.Append("        public ");
        sb.Append(newModifier);
        sb.AppendLine($"void {methodName}({spanType} span, ref int index)");
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
                sb.AppendLine(isUtf8 ? "                    span[index++] = (byte)'{';" : "                    span[index++] = '{';");
                var discKey = data.PolymorphicDiscriminatorProp ?? "$type";
                sb.AppendLine("                    global::GenJson.GenJsonWriter.WriteString(span, ref index, \"" + discKey + "\");");
                sb.AppendLine(isUtf8 ? "                    span[index++] = (byte)':';" : "                    span[index++] = ':';");
                if (derived.IsIntDiscriminator)
                {
                    foreach (char c in derived.DiscriminatorValue)
                        sb.AppendLine("                    span[index++] = " + (isUtf8 ? "(byte)" : "") + "'" + c + "';");
                }
                else
                {
                    sb.AppendLine("                    global::GenJson.GenJsonWriter.WriteString(span, ref index, " + derived.DiscriminatorValue + ");");
                }
                sb.AppendLine(isUtf8 ? "                    span[index++] = (byte)',';" : "                    span[index++] = ',';");
                sb.AppendLine("                    d" + derivedIdx + $".{methodContent}(span, ref index);");
                sb.AppendLine(isUtf8 ? "                    span[index++] = (byte)'}';" : "                    span[index++] = '}';");
                sb.AppendLine("                    return;");
                sb.AppendLine("                }");
            }

            sb.AppendLine("                default:");
            if (data.IsAbstract)
            {
                // ...
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
                return;
            }
        }
        sb.AppendLine(isUtf8 ? "            span[index++] = (byte)'{';" : "            span[index++] = '{';");
        sb.AppendLine($"            {methodContent}(span, ref index);");
        sb.AppendLine(isUtf8 ? "            span[index++] = (byte)'}';" : "            span[index++] = '}';");
        sb.AppendLine("        }");

        sb.AppendLine();
    }


    private void GenerateWriteJsonContent(StringBuilder sb, ClassData data, System.Collections.Generic.IReadOnlyList<PropertyData> allProperties, string newModifier, bool isUtf8)
    {
        string spanType = isUtf8 ? "System.Span<byte>" : "System.Span<char>";
        string methodName = isUtf8 ? "WriteJsonContentUtf8" : "WriteJsonContent";

        sb.Append("        public ");
        sb.Append(newModifier);
        sb.AppendLine($"void {methodName}({spanType} span, ref int index)");
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
                sb.AppendLine(isUtf8 ? "span[index++] = (byte)',';" : "span[index++] = ',';");
            }
            else // Unknown
            {
                sb.Append(indent);
                sb.AppendLine("if (!first)");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine(isUtf8 ? "    span[index++] = (byte)',';" : "    span[index++] = ',';");
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
                    sb.AppendLine(isUtf8 ? "span[index++] = (byte)':';" : "span[index++] = ':';");

                    sb.Append(indent);
                    sb.Append("{ if (!this.");
                    sb.Append(prop.Name);
                    sb.AppendLine(".Length.TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
                    sb.Append(indent);
                    sb.AppendLine("    { throw new System.Exception(\"Buffer too small (Count)\"); }");
                    sb.Append(indent);
                    sb.AppendLine("    index += written; }");

                    sb.Append(indent);
                    sb.AppendLine(isUtf8 ? "span[index++] = (byte)',';" : "span[index++] = ',';");
                }
                else
                {
                    sb.Append(indent);
                    sb.AppendLine("{");
                    GenerateGetCollectionCount(sb, indent, prop.Name, en.ElementTypeName);

                    sb.Append(indent);
                    sb.AppendLine("    if (_count >= 0)");
                    sb.Append(indent);
                    sb.AppendLine("    {");
                    sb.Append(indent);
                    sb.Append("        global::GenJson.GenJsonWriter.WriteString(span, ref index, \"$");
                    sb.Append(prop.JsonName);
                    sb.AppendLine("\");");

                    sb.Append(indent);
                    sb.AppendLine(isUtf8 ? "        span[index++] = (byte)':';" : "        span[index++] = ':';");

                    sb.Append(indent);
                    sb.AppendLine("        { if (!_count.TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
                    sb.Append(indent);
                    sb.AppendLine("            { throw new System.Exception(\"Buffer too small (Count)\"); }");
                    sb.Append(indent);
                    sb.AppendLine("            index += written; }");

                    sb.Append(indent);
                    sb.AppendLine(isUtf8 ? "        span[index++] = (byte)',';" : "        span[index++] = ',';");
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
                sb.AppendLine(isUtf8 ? "span[index++] = (byte)':';" : "span[index++] = ':';");

                sb.Append(indent);
                sb.Append("{ if (!this.");
                sb.Append(prop.Name);
                sb.AppendLine(".Count.TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture))");
                sb.Append(indent);
                sb.AppendLine("    { throw new System.Exception(\"Buffer too small (Count)\"); }");
                sb.Append(indent);
                sb.AppendLine("    index += written; }");

                sb.Append(indent);
                sb.AppendLine(isUtf8 ? "span[index++] = (byte)',';" : "span[index++] = ',';");
            }

            sb.Append(indent);
            sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, \"");
            sb.Append(prop.JsonName);
            sb.AppendLine("\");");
            sb.Append(indent);
            sb.AppendLine(isUtf8 ? "span[index++] = (byte)':';" : "span[index++] = ':';");

            GenerateWriteJsonValue(sb, prop.Type, $"this.{prop.Name}", indent, 0, isUtf8);

            if (prop.IsNullable)
            {
                sb.AppendLine("            }");
            }
        }
        sb.AppendLine("        }");
    }



    private void GenerateGetCollectionCount(StringBuilder sb, string indent, string propName, string elementTypeName)
    {
        sb.Append(indent);
        sb.AppendLine("    int _count = -1;");
        sb.Append(indent);
        sb.AppendLine($"    if (this.{propName} is global::System.Collections.Generic.ICollection<{elementTypeName}> c) _count = c.Count;");
        sb.Append(indent);
        sb.AppendLine($"    else if (this.{propName} is global::System.Collections.Generic.IReadOnlyCollection<{elementTypeName}> r) _count = r.Count;");
    }

    private void GenerateFromJson(StringBuilder sb, ClassData data, string newModifier, bool isUtf8)
    {
        string inputType = isUtf8 ? "System.ReadOnlySpan<byte>" : "System.ReadOnlySpan<char>";
        string methodName = isUtf8 ? "FromJsonUtf8" : "FromJson";
        string parseMethod = isUtf8 ? "ParseUtf8" : "Parse";

        sb.Append("        public static ");
        sb.Append(newModifier);
        sb.AppendLine(data.ClassName + $"? {methodName}({inputType} json)");
        sb.AppendLine("        {");
        if (isUtf8)
        {
            sb.AppendLine($"            int index = 0;");
            sb.AppendLine($"            return {parseMethod}(json, ref index);");
        }
        else
        {
            sb.AppendLine($"            int index = 0;");
            sb.AppendLine($"            return {parseMethod}(json, ref index);");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private void GenerateParse(StringBuilder sb, ClassData data, System.Collections.Generic.IReadOnlyList<PropertyData> allProperties, string newModifier, bool isUtf8)
    {
        string spanType = isUtf8 ? "System.ReadOnlySpan<byte>" : "System.ReadOnlySpan<char>";
        string methodName = isUtf8 ? "ParseUtf8" : "Parse";
        string parserPrefix = "TryParse";
        string indent = "            ";

        sb.Append("        internal static ");
        sb.Append(newModifier);
        sb.AppendLine(data.ClassName + $"? {methodName}({spanType} json, ref int index)");
        sb.AppendLine("        {");

        // Ensure Expect '{'
        sb.Append(indent);
        sb.Append($"if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, ");
        sb.AppendLine(isUtf8 ? "(byte)'{'))" : "'{'))");
        sb.Append(indent);
        sb.AppendLine("    return null;");
        sb.AppendLine();

        // Cached Byte arrays for Utf8 keys
        if (isUtf8)
        {
            foreach (var prop in allProperties)
            {
                sb.Append(indent);
                sb.Append($"System.ReadOnlySpan<byte> _Utf8");
                sb.Append(prop.Name);
                sb.Append($" = new byte[] {{ ");
                var bytes = System.Text.Encoding.UTF8.GetBytes(prop.JsonName);
                sb.Append(string.Join(", ", bytes));
                sb.AppendLine($" }};");
            }
            if (data.PolymorphicDiscriminatorProp != null)
            {
                sb.Append(indent);
                sb.Append($"System.ReadOnlySpan<byte> _Utf8_type = new byte[] {{ ");
                var bytes = System.Text.Encoding.UTF8.GetBytes(data.PolymorphicDiscriminatorProp);
                sb.Append(string.Join(", ", bytes));
                sb.AppendLine($" }};");
            }
            sb.AppendLine();
        }

        // Polymorphic Discriminator Logic
        if (data.DerivedTypes.Value.Count > 0)
        {
            var discName = data.PolymorphicDiscriminatorProp ?? "$type";
            // Optimization: If discriminator is first or early, we can shortcut to derived parser.
            // But we need to scan for it or just check if next property is it.
            // For now, let's just attempt to find it proactively if we suspect derived type?
            // Actually, simplest is to just parse normally and if we encounter discriminator, switch?
            // But constructor structure requires values.
            // Strategy: 
            // 1. Scan for discriminator property first (inefficient but correct for polymorphism without rewriting parser flow completely).
            // 2. Or assume if it exists it is first? JSON standard doesn't guarantee order.
            // Let's stick to the previous logic which seemed to try to find property.

            sb.Append(indent);
            sb.Append($"if (global::GenJson.GenJsonParser.TryFindProperty(json, index - 1, \"{discName}\", out var valIndex))");
            sb.AppendLine();
            sb.Append(indent);
            sb.AppendLine("{");
            sb.Append(indent);
            sb.AppendLine("    int tempIndex = valIndex;");

            sb.Append(indent);
            sb.AppendLine("    if (tempIndex < json.Length)");
            sb.Append(indent);
            sb.AppendLine("    {");
            sb.Append(indent);
            string cType = isUtf8 ? "byte" : "char";
            sb.AppendLine($"        {cType} c = json[tempIndex];");

            sb.Append(indent);
            sb.AppendLine(isUtf8 ? "        if (c == (byte)'\"')" : "        if (c == '\"')");
            sb.Append(indent);
            sb.AppendLine("        {");

            foreach (var derived in data.DerivedTypes.Value.Where(d => !d.IsIntDiscriminator))
            {
                var rawVal = derived.DiscriminatorValue.Trim('"');
                sb.Append(indent);
                sb.Append($"            if (global::GenJson.GenJsonParser.MatchesKey(json, ref tempIndex, \"{rawVal}\"))");
                sb.AppendLine();
                sb.Append(indent);
                sb.AppendLine("            {");
                sb.Append(indent);
                sb.AppendLine("                int i = index - 1;");
                sb.Append(indent);
                sb.AppendLine($"                var result = {derived.TypeName}.{methodName}(json, ref i);");
                sb.Append(indent);
                sb.AppendLine("                index = i;");
                sb.Append(indent);
                sb.AppendLine("                return result;");
                sb.Append(indent);
                sb.AppendLine("            }");
            }
            sb.Append(indent);
            sb.AppendLine("        }");

            sb.Append(indent);
            sb.AppendLine(isUtf8 ? "        else if ((c >= (byte)'0' && c <= (byte)'9') || c == (byte)'-')" : "        else if (char.IsDigit(c) || c == '-')");
            sb.Append(indent);
            sb.AppendLine("        {");
            sb.Append(indent);
            sb.AppendLine($"            if (global::GenJson.GenJsonParser.{parserPrefix}Int(json, ref tempIndex, out int iVal))");
            sb.Append(indent);
            sb.AppendLine("            {");
            if (data.DerivedTypes.Value.Any(d => d.IsIntDiscriminator))
            {
                sb.Append(indent);
                sb.AppendLine("                switch (iVal)");
                sb.Append(indent);
                sb.AppendLine("                {");
                foreach (var derived in data.DerivedTypes.Value.Where(d => d.IsIntDiscriminator))
                {
                    sb.Append(indent);
                    sb.AppendLine($"                    case {derived.DiscriminatorValue}:");
                    sb.Append(indent);
                    sb.AppendLine("                    {");
                    sb.Append(indent);
                    sb.AppendLine("                        int i = index - 1;");
                    sb.Append(indent);
                    sb.AppendLine($"                        var result = {derived.TypeName}.{methodName}(json, ref i);");
                    sb.Append(indent);
                    sb.AppendLine("                        index = i;");
                    sb.Append(indent);
                    sb.AppendLine("                        return result;");
                    sb.Append(indent);
                    sb.AppendLine("                    }");
                }
                sb.Append(indent);
                sb.AppendLine("                }");
            }
            sb.Append(indent);
            sb.AppendLine("            }");
            sb.Append(indent);
            sb.AppendLine("        }");

            sb.Append(indent);
            sb.AppendLine("    }");
            sb.Append(indent);
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // Locals for properties
        foreach (var prop in allProperties)
        {
            sb.Append(indent);
            sb.Append(prop.TypeName);
            if (prop.IsNullable) sb.Append("?");
            sb.Append(" _");
            sb.Append(prop.Name);
            sb.AppendLine(" = default;");

            if (!data.SkipCountOptimization && (prop.Type is GenJsonDataType.Enumerable or GenJsonDataType.Dictionary))
            {
                sb.Append(indent);
                sb.Append("int _");
                sb.Append(prop.Name);
                sb.AppendLine("_count = -1;");
            }

            if (data.IsNullableContext && !prop.IsNullable)
            {
                sb.Append(indent);
                sb.Append("bool _found");
                sb.Append(prop.Name);
                sb.AppendLine(" = false;");
            }
        }
        sb.AppendLine();

        sb.Append(indent);
        sb.AppendLine("bool _closed = false;");
        sb.Append(indent);
        sb.AppendLine("while (index < json.Length)");
        sb.Append(indent);
        sb.AppendLine("{");

        sb.Append(indent);
        sb.Append($"    if (index < json.Length && json[index] == {(isUtf8 ? "(byte)'}'" : "'}'")})");
        sb.AppendLine();
        sb.Append(indent);
        sb.AppendLine("    {");
        sb.Append(indent);
        sb.AppendLine("        index++;");
        sb.Append(indent);
        sb.AppendLine("        _closed = true;");
        sb.Append(indent);
        sb.AppendLine("        break;");
        sb.Append(indent);
        sb.AppendLine("    }");

        // Parse Key
        sb.Append(indent);
        sb.AppendLine("    // Parse Key");

        sb.Append(indent);
        sb.AppendLine("    bool _parsedAny = false;");

        // Optimization: Linear independent ifs
        foreach (var prop in allProperties)
        {
            if (!data.SkipCountOptimization && (prop.Type is GenJsonDataType.Enumerable or GenJsonDataType.Dictionary))
            {
                sb.Append(indent);
                sb.Append("    if (");
                sb.Append("global::GenJson.GenJsonParser.MatchesKey(json, ref index, \"$");
                sb.Append(prop.JsonName);
                sb.AppendLine("\"))");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.AppendLine($"        if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, {(isUtf8 ? "(byte)':'" : "':'")})) return null;");
                sb.Append(indent);
                if (isUtf8)
                {
                    sb.Append("        if (!global::System.Buffers.Text.Utf8Parser.TryParse(json.Slice(index), out _");
                    sb.Append(prop.Name);
                    sb.AppendLine("_count, out int consumed)) return null; index += consumed;");
                }
                else
                {
                    sb.Append("        if (!global::GenJson.GenJsonParser.TryParseInt(json, ref index, out _");
                    sb.Append(prop.Name);
                    sb.AppendLine("_count)) return null;");
                }
                sb.Append(indent);
                sb.AppendLine($"        if (index < json.Length && json[index] == {(isUtf8 ? "(byte)','" : "','")}) index++;");
                sb.Append(indent);
                sb.AppendLine("        _parsedAny = true;");
                sb.Append(indent);
                sb.AppendLine("    }");
            }

            sb.Append(indent);
            sb.Append("    if (");
            if (isUtf8)
            {
                sb.Append("global::GenJson.GenJsonParser.MatchesKey(json, ref index, \"");
                sb.Append(prop.JsonName);
                sb.Append($"\", _Utf8{prop.Name}");
                sb.AppendLine("))");
            }
            else
            {
                sb.Append("global::GenJson.GenJsonParser.MatchesKey(json, ref index, \"");
                sb.Append(prop.JsonName);
                sb.AppendLine("\"))");
            }
            sb.Append(indent);
            sb.AppendLine("    {");
            sb.Append(indent);
            sb.AppendLine($"        if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, {(isUtf8 ? "(byte)':'" : "':'")})) return null;");

            GenerateParseValue(sb, prop.Type, "_" + prop.Name, indent + "        ", 0, isUtf8, (!data.SkipCountOptimization && (prop.Type is GenJsonDataType.Enumerable or GenJsonDataType.Dictionary)) ? $"_{prop.Name}_count" : null);

            if (data.IsNullableContext && !prop.IsNullable)
            {
                sb.Append(indent);
                sb.Append("        _found");
                sb.Append(prop.Name);
                sb.AppendLine(" = true;");
            }
            sb.Append(indent);
            sb.AppendLine($"        if (index < json.Length && json[index] == {(isUtf8 ? "(byte)','" : "','")}) index++;");
            sb.Append(indent);
            sb.AppendLine("        _parsedAny = true;");
            sb.Append(indent);
            sb.AppendLine("    }");
        }

        if (data.PolymorphicDiscriminatorProp != null)
        {
            sb.Append(indent);
            sb.Append("    if (");
            if (isUtf8)
            {
                sb.Append("global::GenJson.GenJsonParser.MatchesKey(json, ref index, \"");
                sb.Append(data.PolymorphicDiscriminatorProp);
                sb.AppendLine("\", _Utf8_type))");
            }
            else
            {
                sb.Append("global::GenJson.GenJsonParser.MatchesKey(json, ref index, \"");
                sb.Append(data.PolymorphicDiscriminatorProp);
                sb.AppendLine("\"))");
            }
            sb.Append(indent);
            sb.AppendLine("    {");
            sb.Append(indent);
            sb.AppendLine($"        if (!global::GenJson.GenJsonParser.TryExpect(json, ref index, {(isUtf8 ? "(byte)':'" : "':'")})) return null;");
            sb.Append(indent);
            sb.AppendLine("        global::GenJson.GenJsonParser.TrySkipValue(json, ref index);");
            sb.Append(indent);
            sb.AppendLine($"        if (index < json.Length && json[index] == {(isUtf8 ? "(byte)','" : "','")}) index++;");
            sb.Append(indent);
            sb.AppendLine("        _parsedAny = true;");
            sb.Append(indent);
            sb.AppendLine("    }");
        }

        sb.Append(indent);
        sb.AppendLine("    if (!_parsedAny)");
        sb.Append(indent);
        sb.AppendLine("    {");
        sb.Append(indent);
        sb.AppendLine("        global::GenJson.GenJsonParser.TrySkipString(json, ref index);");
        sb.Append(indent);
        sb.AppendLine($"        if (index < json.Length && json[index] == {(isUtf8 ? "(byte)':'" : "':'")}) index++;");
        sb.Append(indent);
        sb.AppendLine("        global::GenJson.GenJsonParser.TrySkipValue(json, ref index);");
        sb.Append(indent);
        sb.AppendLine($"        if (index < json.Length && json[index] == {(isUtf8 ? "(byte)','" : "','")}) index++;");
        sb.Append(indent);
        sb.AppendLine("    }");

        sb.Append(indent);
        sb.AppendLine("}"); // End While

        sb.Append(indent);
        sb.AppendLine($"if (!_closed) return null;");

        // Validation & Return
        if (data.IsNullableContext)
        {
            foreach (var prop in allProperties)
            {
                if (!prop.IsNullable)
                {
                    sb.Append(indent);
                    sb.AppendLine($"if (!_found{prop.Name}) return null;");
                }
            }
        }

        if (data.IsAbstract)
        {
            sb.Append(indent);
            sb.Append($" return null;"); sb.AppendLine();
            sb.AppendLine("        }");
            sb.AppendLine();
            return;
        }

        sb.Append(indent);
        sb.Append("return new ");
        sb.Append(data.ClassName);

        var ctorArgs = data.ConstructorArgs.Value;
        bool isRecord = data.Keyword.IndexOf("record", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (isRecord && ctorArgs.Count > 0)
        {
            sb.AppendLine("(");
            for (int i = 0; i < ctorArgs.Count; i++)
            {
                var prop = ctorArgs[i];
                sb.Append(indent);
                sb.Append("    ");
                sb.Append(prop.ConstructorParamName ?? prop.Name);
                sb.Append(": _");
                sb.Append(prop.Name);
                if (i < ctorArgs.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.Append(indent);
            sb.Append(")");
        }
        else
        {
            sb.Append("()");
        }

        var initProps = data.InitProperties.Value;
        bool hasCtorArgsAsInit = !isRecord && ctorArgs.Count > 0;

        if (initProps.Count > 0 || hasCtorArgsAsInit)
        {
            sb.AppendLine();
            sb.Append(indent);
            sb.AppendLine("{");

            if (hasCtorArgsAsInit)
            {
                foreach (var prop in ctorArgs)
                {
                    sb.Append(indent);
                    sb.Append("    ");
                    sb.Append(prop.Name);
                    sb.Append(" = _");
                    sb.Append(prop.Name);
                    sb.AppendLine(",");
                }
            }

            foreach (var prop in initProps)
            {
                sb.Append(indent);
                sb.Append("    ");
                sb.Append(prop.Name);
                sb.Append(" = _");
                sb.Append(prop.Name);
                sb.AppendLine(",");
            }
            sb.Append(indent);
            sb.AppendLine("};");
        }
        else
        {
            sb.AppendLine(";");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private void GenerateEnumParseLogic(StringBuilder sb, GenJsonDataType.Enum en, string targetVar, string indent, bool isUtf8)
    {
        string parserPrefix = "TryParse";

        if (en.AsString)
        {
            string safeTarget = targetVar.Replace(".", "").Replace("[", "").Replace("]", "");
            sb.Append(indent);
            sb.AppendLine($"string enumStr_{safeTarget} = null!;");
            sb.Append(indent);
            sb.AppendLine($"bool enumEscaped_{safeTarget} = false;");
            sb.Append(indent);
            if (isUtf8)
            {
                sb.AppendLine($"global::System.ReadOnlySpan<byte> enumSpan_{safeTarget} = default;");
            }
            else
            {
                sb.AppendLine($"global::System.ReadOnlySpan<char> enumSpan_{safeTarget} = default;");
            }
            sb.Append(indent);
            sb.Append($"if (!global::GenJson.GenJsonParser.{parserPrefix}StringSpan(json, ref index, out enumSpan_{safeTarget}, out enumEscaped_{safeTarget}))");
            sb.Append($" return null;"); sb.AppendLine();

            // Build an if-else chain instead of a switch since we can't switch on spans in all C# versions easily (or we need if chains anyway for spans)
            sb.Append(indent);
            sb.AppendLine($"bool enumFound_{safeTarget} = false;");
            sb.Append(indent);
            sb.AppendLine($"if (!enumEscaped_{safeTarget})");
            sb.Append(indent);
            sb.AppendLine("{");
            bool first = true;
            foreach (var member in en.Members.Value)
            {
                sb.Append(indent);
                sb.Append("    ");
                if (!first) sb.Append("else ");
                if (isUtf8)
                {
                    var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(member);
                    var utf8BytesStr = string.Join(", ", utf8Bytes.Select(b => $"(byte){b}"));
                    sb.AppendLine($"if (global::System.MemoryExtensions.SequenceEqual(enumSpan_{safeTarget}, new byte[] {{ {utf8BytesStr} }})) {{ {targetVar} = {en.TypeName}.{member}; enumFound_{safeTarget} = true; }}");
                }
                else
                {
                    sb.AppendLine($"if (global::System.MemoryExtensions.SequenceEqual(enumSpan_{safeTarget}, global::System.MemoryExtensions.AsSpan(\"{member}\"))) {{ {targetVar} = {en.TypeName}.{member}; enumFound_{safeTarget} = true; }}");
                }
                first = false;
            }
            sb.Append(indent);
            sb.AppendLine("}");
            sb.Append(indent);
            sb.AppendLine("else");
            sb.Append(indent);
            sb.AppendLine("{");
            sb.Append(indent);
            sb.AppendLine($"    enumStr_{safeTarget} = global::GenJson.GenJsonParser.{(isUtf8 ? "UnescapeStringUtf8" : "UnescapeString")}(enumSpan_{safeTarget});");
            first = true;
            foreach (var member in en.Members.Value)
            {
                sb.Append(indent);
                sb.Append("    ");
                if (!first) sb.Append("else ");
                sb.AppendLine($"if (enumStr_{safeTarget} == \"{member}\") {{ {targetVar} = {en.TypeName}.{member}; enumFound_{safeTarget} = true; }}");
                first = false;
            }
            sb.Append(indent);
            sb.AppendLine("}");

            sb.Append(indent);
            sb.AppendLine($"if (!enumFound_{safeTarget})");
            sb.Append(indent);
            sb.AppendLine("{");
            if (en.FallbackValue != null)
            {
                sb.Append(indent);
                sb.Append("    ");
                sb.Append(targetVar);
                sb.Append(" = ");
                sb.Append(en.FallbackValue);
                sb.AppendLine(";");
            }
            else
            {
                sb.Append(indent);
                if (isUtf8)
                {
                    sb.Append($" return null;"); sb.AppendLine();
                }
                else
                {
                    sb.Append($" return null;"); sb.AppendLine();
                }
            }
            sb.Append(indent);
            sb.AppendLine("}");
        }
        else
        {
            string safeTarget = targetVar.Replace(".", "").Replace("[", "").Replace("]", "");
            sb.Append(indent);
            sb.Append(en.UnderlyingType);
            sb.AppendLine($" enumVal_{safeTarget};");
            sb.Append(indent);
            string parser = GetPrimitiveParserName(en.UnderlyingType);
            sb.AppendLine($"if (!global::GenJson.GenJsonParser.{parserPrefix}{parser}(json, ref index, out enumVal_{safeTarget}))");
            sb.Append(indent);
            sb.AppendLine("{");
            if (en.FallbackValue != null)
            {
                sb.Append(indent);
                sb.AppendLine("    global::GenJson.GenJsonParser.TrySkipValue(json, ref index);");
                sb.Append(indent);
                sb.AppendLine($"    {targetVar} = {en.FallbackValue};");
            }
            else
            {
                sb.Append(indent);
                sb.Append($" return null;"); sb.AppendLine();
            }
            sb.Append(indent);
            sb.AppendLine("}");
            sb.Append(indent);
            sb.AppendLine("else");
            sb.Append(indent);
            sb.AppendLine("{");
            sb.Append(indent);
            sb.Append($"    if (!System.Enum.IsDefined<{en.TypeName}>(({en.TypeName})enumVal_{safeTarget}))");
            sb.AppendLine();
            sb.Append(indent);
            sb.AppendLine("    {");
            if (en.FallbackValue != null)
            {
                sb.Append(indent);
                sb.Append("        ");
                sb.Append(targetVar);
                sb.Append(" = ");
                sb.Append(en.FallbackValue);
                sb.AppendLine(";");
            }
            else
            {
                sb.Append(indent);
                sb.Append($" return null;"); sb.AppendLine();
            }
            sb.Append(indent);
            sb.AppendLine("    }");
            sb.Append(indent);
            sb.AppendLine("    else");
            sb.Append(indent);
            sb.AppendLine("    {");
            sb.Append(indent);
            sb.Append("        ");
            sb.Append(targetVar);
            sb.Append(" = (");
            sb.Append(en.TypeName);
            sb.AppendLine($")enumVal_{safeTarget};");
            sb.Append(indent);
            sb.AppendLine("    }");
            sb.Append(indent);
            sb.AppendLine("}");
        }
    }
}
