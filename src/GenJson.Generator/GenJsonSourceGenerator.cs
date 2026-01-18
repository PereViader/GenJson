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
    public sealed record Enumerable(GenJsonDataType ElementType, bool IsArray, string ConstructionTypeName, string ElementTypeName) : GenJsonDataType;
    public sealed record Dictionary(GenJsonDataType KeyType, GenJsonDataType ValueType, string ConstructionTypeName, string KeyTypeName, string ValueTypeName) : GenJsonDataType;
    public sealed record Enum(string TypeName, bool AsString, string UnderlyingType) : GenJsonDataType;
}

public record PropertyData(string Name, string TypeName, bool IsNullable, GenJsonDataType Type);

public record ClassData(
    string ClassName,
    string Namespace,
    EquatableList<PropertyData> ConstructorArgs,
    EquatableList<PropertyData> Properties,
    string Keyword);

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
        var propertiesMap = new Dictionary<string, PropertyData>(StringComparer.OrdinalIgnoreCase);

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IPropertySymbol propertySymbol &&
                propertySymbol.DeclaredAccessibility == Accessibility.Public &&
                !propertySymbol.IsStatic)
            {
                bool isNullable = propertySymbol.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
                                  propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;

                var type = GetGenJsonDataType(propertySymbol, propertySymbol.Type);

                var propData = new PropertyData(propertySymbol.Name, propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), isNullable, type);
                properties.Add(propData);
                propertiesMap[propertySymbol.Name] = propData;
            }
        }

        var constructorArgs = new List<PropertyData>();
        if (typeDeclaration is RecordDeclarationSyntax)
        {
            var ctor = typeSymbol.Constructors
                .OrderByDescending(c => c.Parameters.Length)
                .FirstOrDefault();
            if (ctor is { Parameters.Length: > 0 })
            {
                foreach (var param in ctor.Parameters)
                {
                    if (propertiesMap.TryGetValue(param.Name, out var prop))
                    {
                        constructorArgs.Add(prop);
                        properties.Remove(prop); // Remove from property list as it will be set via constructor
                    }
                }
            }
        }

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

        return new ClassData(typeSymbol.Name, typeNameSpace, new EquatableList<PropertyData>(constructorArgs), new EquatableList<PropertyData>(properties), keyword);
    }

    private static GenJsonDataType GetGenJsonDataType(IPropertySymbol propertySymbol, ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType)
        {
            var asString = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJson.Enum.AsText");
            var underlyingType = enumType.EnumUnderlyingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "int";
            return new GenJsonDataType.Enum(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), asString, underlyingType);
        }

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            type is INamedTypeSymbol namedRaw && namedRaw.TypeArguments.Length > 0)
        {
            return new GenJsonDataType.Nullable(GetGenJsonDataType(propertySymbol, namedRaw.TypeArguments[0]));
        }

        if (type.SpecialType == SpecialType.System_String)
        {
            return GenJsonDataType.String.Instance;
        }

        if (type.SpecialType == SpecialType.System_Boolean)
        {
            return GenJsonDataType.Boolean.Instance;
        }

        if (type.SpecialType == SpecialType.System_Char)
        {
            return GenJsonDataType.Char.Instance;
        }

        if (type.SpecialType is SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal)
        {
            return new GenJsonDataType.FloatingPoint(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (typeName == "global::System.Guid" || typeName == "System.Guid") return GenJsonDataType.Guid.Instance;
        if (typeName == "global::System.DateTime" || typeName == "System.DateTime") return GenJsonDataType.DateTime.Instance;
        if (typeName == "global::System.TimeSpan" || typeName == "System.TimeSpan") return GenJsonDataType.TimeSpan.Instance;
        if (typeName == "global::System.DateTimeOffset" || typeName == "System.DateTimeOffset") return GenJsonDataType.DateTimeOffset.Instance;
        if (typeName == "global::System.Version" || typeName == "System.Version") return GenJsonDataType.Version.Instance;

        if (HasGenJsonAttribute(type))
        {
            return new GenJsonDataType.Object(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        if (TryGetDictionaryTypes(type, out var keyType, out var valueType))
        {
            var keyGenType = GetGenJsonDataType(propertySymbol, keyType!);
            var valueGenType = GetGenJsonDataType(propertySymbol, valueType!);
            var constructionTypeName = $"global::System.Collections.Generic.Dictionary<{keyType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {valueType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
            return new GenJsonDataType.Dictionary(keyGenType, valueGenType, constructionTypeName, keyType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), valueType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }

        ITypeSymbol? resolvedElementType = GetEnumerableElementType(type);
        if (resolvedElementType != null)
        {
            var isArray = type is IArrayTypeSymbol;
            var constructionTypeName = isArray
               ? null // Not used for array
               : $"global::System.Collections.Generic.List<{resolvedElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
            return new GenJsonDataType.Enumerable(GetGenJsonDataType(propertySymbol, resolvedElementType), isArray, constructionTypeName!, resolvedElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
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
               (type.OriginalDefinition.Name == "IDictionary" || type.OriginalDefinition.Name == "IReadOnlyDictionary") &&
               type.TypeParameters.Length == 2;
    }

    private static bool HasGenJsonAttribute(ITypeSymbol type)
    {
        if (type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonAttribute"))
        {
            return true;
        }

        return false;
    }

    private static ITypeSymbol? GetEnumerableElementType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        if (type is INamedTypeSymbol namedTypeSym)
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
        }
        return null;
    }

    private void Generate(SourceProductionContext context, ClassData data)
    {
        var sb = new StringBuilder();

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

        sb.AppendLine("        public int CalculateJsonSize()");
        sb.AppendLine("        {");
        sb.AppendLine("            int size = 2;");
        sb.AppendLine("            int propertyCount = 0;");

        var allProperties = new List<PropertyData>();
        allProperties.AddRange(data.ConstructorArgs.Value);
        allProperties.AddRange(data.Properties.Value);

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
            sb.Append(indent);
            sb.Append("size += ");
            sb.Append(prop.Name.Length + 3); // "Key":
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

        sb.AppendLine("""
        public string ToJson()
        {
            return string.Create(CalculateJsonSize(), this, (span, state) =>
            {
                int index = 0;
                state.WriteJson(span, ref index);
            });
        }
""");


        sb.AppendLine();
        sb.AppendLine("        public void WriteJson(System.Span<char> span, ref int index)");
        sb.AppendLine("        {");
        sb.AppendLine("            span[index++] = '{';");

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

            sb.Append(indent);
            sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, \"");
            sb.Append(prop.Name);
            sb.AppendLine("\");");
            sb.Append(indent);
            sb.AppendLine("span[index++] = ':';");

            GenerateWriteJsonValue(sb, prop.Type, $"this.{prop.Name}", indent, 0);

            if (prop.IsNullable)
            {
                sb.AppendLine("            }");
            }
        }
        sb.AppendLine("            span[index++] = '}';");
        sb.AppendLine("        }");

        sb.AppendLine();
        sb.AppendLine("        public static " + data.ClassName + " FromJson(string json)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (json is null) throw new System.ArgumentNullException(nameof(json));");
        sb.AppendLine("            System.ReadOnlySpan<char> span = json;");
        sb.AppendLine("            var index = 0;");
        sb.AppendLine("            global::GenJson.GenJsonParser.SkipWhitespace(span, ref index);");
        sb.AppendLine("            var result = Parse(span, ref index);");
        sb.AppendLine("            global::GenJson.GenJsonParser.SkipWhitespace(span, ref index);");
        sb.AppendLine("            if (index != span.Length) throw new System.ArgumentException(\"Unexpected extra characters\", nameof(json));");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");

        sb.AppendLine();
        sb.AppendLine("        internal static " + data.ClassName + " Parse(System.ReadOnlySpan<char> json, ref int index)");
        sb.AppendLine("        {");
        sb.AppendLine("            global::GenJson.GenJsonParser.Expect(json, ref index, '{');");

        foreach (var prop in allProperties)
        {
            sb.Append("            ");
            sb.Append(prop.TypeName);
            sb.Append(" _");
            sb.Append(prop.Name);
            sb.AppendLine(" = default;");
        }

        sb.AppendLine("            while (index < json.Length)");
        sb.AppendLine("            {");
        sb.AppendLine("                global::GenJson.GenJsonParser.SkipWhitespace(json, ref index);");
        sb.AppendLine("                if (json[index] == '}')");
        sb.AppendLine("                {");
        sb.AppendLine("                    index++;");
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
        if (data.Properties.Value.Count > 0)
        {
            sb.AppendLine("                    {");
            foreach (var prop in data.Properties.Value)
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
        sb.AppendLine("                }");

        sb.AppendLine("                bool matched = false;");
        sb.AppendLine("                if (false) { }"); // This is a dummy block to start the else if chain
        foreach (var prop in allProperties)
        {
            sb.Append("                else if (global::GenJson.GenJsonParser.MatchesKey(json, ref index, \"");
            sb.Append(prop.Name);
            sb.AppendLine("\"))");
            sb.AppendLine("                {");
            sb.AppendLine("                    global::GenJson.GenJsonParser.Expect(json, ref index, ':');");
            GenerateParseValue(sb, prop.Type, "_" + prop.Name, "                    ", 0);
            sb.AppendLine("                    matched = true;");
            sb.AppendLine("                }");
        }

        sb.AppendLine("                if (!matched)");
        sb.AppendLine("                {");
        sb.AppendLine("                    global::GenJson.GenJsonParser.SkipString(json, ref index);");
        sb.AppendLine("                    global::GenJson.GenJsonParser.Expect(json, ref index, ':');");
        sb.AppendLine("                    global::GenJson.GenJsonParser.SkipValue(json, ref index);");
        sb.AppendLine("                }");

        sb.AppendLine("                global::GenJson.GenJsonParser.SkipWhitespace(json, ref index);");
        sb.AppendLine("                if (json[index] == ',')");
        sb.AppendLine("                {");
        sb.AppendLine("                    index++;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("            throw new System.Exception(\"Unterminated object at \" + index);");
        sb.AppendLine("        }");

        sb.AppendLine("    }");

        if (!string.IsNullOrEmpty(data.Namespace))
        {
            sb.AppendLine("}");
        }

        context.AddSource($"{data.ClassName}.GenJson.g.cs", sb.ToString());
    }

    private void GenerateParseValue(StringBuilder sb, GenJsonDataType type, string target, string indent, int depth)
    {
        switch (type)
        {


            case GenJsonDataType.Boolean:
                sb.Append(indent);
                sb.Append(target);
                sb.AppendLine(" = global::GenJson.GenJsonParser.ParseBoolean(json, ref index);");
                break;

            case GenJsonDataType.String:
                sb.Append(indent);
                sb.Append(target);
                sb.AppendLine(" = global::GenJson.GenJsonParser.ParseString(json, ref index);");
                break;



            case GenJsonDataType.Char:
                sb.Append(indent);
                sb.Append(target);
                sb.AppendLine(" = global::GenJson.GenJsonParser.ParseChar(json, ref index);");
                break;

            case GenJsonDataType.FloatingPoint p:
                sb.Append(indent);
                sb.Append(target);
                sb.Append(" = global::GenJson.GenJsonParser.Parse");
                sb.Append(GetPrimitiveParserName(p.TypeName));
                sb.AppendLine("(json, ref index);");
                break;

            case GenJsonDataType.Guid:
                sb.Append(indent);
                sb.Append(target);
                sb.AppendLine(" = System.Guid.Parse(global::GenJson.GenJsonParser.ParseString(json, ref index));");
                break;

            case GenJsonDataType.DateTime:
                sb.Append(indent);
                sb.Append(target);
                sb.AppendLine(" = System.DateTime.Parse(global::GenJson.GenJsonParser.ParseString(json, ref index), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);");
                break;

            case GenJsonDataType.TimeSpan:
                sb.Append(indent);
                sb.Append(target);
                sb.AppendLine(" = System.TimeSpan.Parse(global::GenJson.GenJsonParser.ParseString(json, ref index));");
                break;

            case GenJsonDataType.DateTimeOffset:
                sb.Append(indent);
                sb.Append(target);
                sb.AppendLine(" = System.DateTimeOffset.Parse(global::GenJson.GenJsonParser.ParseString(json, ref index), System.Globalization.CultureInfo.InvariantCulture);");
                break;

            case GenJsonDataType.Version:
                sb.Append(indent);
                sb.Append(target);
                sb.AppendLine(" = System.Version.Parse(global::GenJson.GenJsonParser.ParseString(json, ref index));");
                break;

            case GenJsonDataType.Primitive p:
                sb.Append(indent);
                sb.Append(target);
                sb.Append(" = global::GenJson.GenJsonParser.Parse");
                sb.Append(GetPrimitiveParserName(p.TypeName));
                sb.AppendLine("(json, ref index);");
                break;

            case GenJsonDataType.Object o:
                sb.Append(indent);
                sb.Append(target);
                sb.Append(" = ");
                sb.Append(o.TypeName); // Assuming generated class has Parse
                sb.AppendLine(".Parse(json, ref index);");
                break;

            case GenJsonDataType.Nullable n:
                sb.Append(indent);
                sb.AppendLine("if (global::GenJson.GenJsonParser.IsNull(json, ref index))");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    global::GenJson.GenJsonParser.ParseNull(json, ref index);");
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
                GenerateParseValue(sb, n.Underlying, target, indent + "    ", depth);
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
                    if (e.IsArray)
                    {
                        sb.AppendLine($"var {listVar} = new global::System.Collections.Generic.List<{e.ElementTypeName}>();");
                    }
                    else
                    {
                        sb.AppendLine($"var {listVar} = new {e.ConstructionTypeName}();");
                    }

                    sb.Append(scopedIndent);
                    sb.AppendLine("global::GenJson.GenJsonParser.Expect(json, ref index, '[');");

                    sb.Append(scopedIndent);
                    sb.AppendLine("while (index < json.Length)");
                    sb.Append(scopedIndent);
                    sb.AppendLine("{");

                    string loopIndent = scopedIndent + "    ";

                    sb.Append(loopIndent);
                    sb.AppendLine("global::GenJson.GenJsonParser.SkipWhitespace(json, ref index);");

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
                    sb.AppendLine($"{e.ElementTypeName} {itemVar} = default;");

                    GenerateParseValue(sb, e.ElementType, itemVar, loopIndent, depth + 1);

                    sb.Append(loopIndent);
                    sb.AppendLine($"{listVar}.Add({itemVar});");

                    sb.Append(loopIndent);
                    sb.AppendLine("global::GenJson.GenJsonParser.SkipWhitespace(json, ref index);");
                    sb.Append(loopIndent);
                    sb.AppendLine("if (json[index] == ',')");
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

                    var dictVar = $"dict{depth}";
                    sb.Append(scopedIndent);
                    sb.AppendLine($"var {dictVar} = new {d.ConstructionTypeName}();");

                    sb.Append(scopedIndent);
                    sb.AppendLine("global::GenJson.GenJsonParser.Expect(json, ref index, '{');");

                    sb.Append(scopedIndent);
                    sb.AppendLine("while (index < json.Length)");
                    sb.Append(scopedIndent);
                    sb.AppendLine("{");

                    string loopIndent = scopedIndent + "    ";

                    sb.Append(loopIndent);
                    sb.AppendLine("global::GenJson.GenJsonParser.SkipWhitespace(json, ref index);");

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

                    string keyStrVar = $"keyStr{depth}";
                    sb.Append(loopIndent);
                    sb.AppendLine($"var {keyStrVar} = global::GenJson.GenJsonParser.ParseString(json, ref index);");

                    sb.Append(loopIndent);
                    sb.AppendLine("global::GenJson.GenJsonParser.Expect(json, ref index, ':');");

                    string keyVar = $"key{depth}";
                    sb.Append(loopIndent);

                    // Parse Key Logic
                    if (d.KeyType is GenJsonDataType.String)
                    {
                        sb.AppendLine($"{d.KeyTypeName} {keyVar} = {keyStrVar};");
                    }
                    else if (d.KeyType is GenJsonDataType.Primitive || d.KeyType is GenJsonDataType.FloatingPoint || d.KeyType is GenJsonDataType.Guid || d.KeyType is GenJsonDataType.DateTime || d.KeyType is GenJsonDataType.TimeSpan || d.KeyType is GenJsonDataType.DateTimeOffset)
                    {
                        // Use .Parse(string, CultureInfo) if applicable, or just .Parse(string)
                        if (d.KeyType is GenJsonDataType.FloatingPoint)
                        {
                            sb.AppendLine($"{d.KeyTypeName} {keyVar} = {d.KeyTypeName}.Parse({keyStrVar}, System.Globalization.CultureInfo.InvariantCulture);");
                        }
                        else
                        {
                            sb.AppendLine($"{d.KeyTypeName} {keyVar} = {d.KeyTypeName}.Parse({keyStrVar});");
                        }
                    }
                    else if (d.KeyType is GenJsonDataType.Enum)
                    {
                        sb.AppendLine($"{d.KeyTypeName} {keyVar} = ({d.KeyTypeName})System.Enum.Parse(typeof({d.KeyTypeName}), {keyStrVar});");
                    }
                    else
                    {
                        // Fallback or error? Assuming Parse works.
                        sb.AppendLine($"{d.KeyTypeName} {keyVar} = {d.KeyTypeName}.Parse({keyStrVar});");
                    }

                    string valVar = $"val{depth}";
                    sb.Append(loopIndent);
                    sb.AppendLine($"{d.ValueTypeName} {valVar} = default;");

                    GenerateParseValue(sb, d.ValueType, valVar, loopIndent, depth + 1);

                    sb.Append(loopIndent);
                    if (d.ConstructionTypeName.Contains("IReadOnlyDictionary"))
                    {
                        sb.AppendLine($"{dictVar}.Add({keyVar}, {valVar});");
                    }
                    else
                    {
                        sb.AppendLine($"{dictVar}.Add({keyVar}, {valVar});");
                    }

                    sb.Append(loopIndent);
                    sb.AppendLine("global::GenJson.GenJsonParser.SkipWhitespace(json, ref index);");
                    sb.Append(loopIndent);
                    sb.AppendLine("if (json[index] == ',')");
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

            case GenJsonDataType.Enum en:
                if (en.AsString)
                {
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = System.Enum.Parse<");
                    sb.Append(en.TypeName);
                    sb.AppendLine(">(global::GenJson.GenJsonParser.ParseString(json, ref index));");
                }
                else
                {
                    sb.Append(indent);
                    sb.Append(target);
                    sb.Append(" = (");
                    sb.Append(en.TypeName);
                    sb.Append(")global::GenJson.GenJsonParser.Parse");
                    sb.Append(GetPrimitiveParserName(en.UnderlyingType));
                    sb.AppendLine("(json, ref index);");
                }
                break;
        }
    }

    private string GetPrimitiveParserName(string typeName)
    {
        if (typeName.StartsWith("global::")) typeName = typeName.Substring(8);
        if (typeName == "int" || typeName == "System.Int32") return "Int";
        if (typeName == "uint" || typeName == "System.UInt32") return "UInt";
        if (typeName == "long" || typeName == "System.Int64") return "Long";
        if (typeName == "ulong" || typeName == "System.UInt64") return "ULong";
        if (typeName == "short" || typeName == "System.Int16") return "Short";
        if (typeName == "ushort" || typeName == "System.UInt16") return "UShort";
        if (typeName == "byte" || typeName == "System.Byte") return "Byte";
        if (typeName == "sbyte" || typeName == "System.SByte") return "SByte";
        if (typeName == "float" || typeName == "System.Single") return "Float";
        if (typeName == "double" || typeName == "System.Double") return "Double";
        if (typeName == "decimal" || typeName == "System.Decimal") return "Decimal";
        return "Int";
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
                if (enumType.AsString)
                {
                    sb.Append(indent);
                    sb.Append("size += ");
                    sb.Append(valueAccessor);
                    sb.Append(".ToString().Length");
                    if (!unquoted) sb.Append(" + 2");
                    sb.AppendLine(";");
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
                break;
        }
    }

    private void GenerateWriteJsonValue(StringBuilder sb, GenJsonDataType type, string valueAccessor, string indent, int depth)
    {
        switch (type)
        {
            case GenJsonDataType.Primitive:
                sb.Append(indent);
                sb.Append("{ ");
                if (valueAccessor.Contains("this."))
                {
                    // Primitive writing
                }
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
                string fmt = !fp.TypeName.EndsWith("Decimal") ? "R" : "G";
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
                if (fmt2 != null)
                    sb.Append($".TryFormat(span.Slice(index), out int written, \"{fmt2}\", System.Globalization.CultureInfo.InvariantCulture))");
                else
                    sb.Append($".TryFormat(span.Slice(index), out int written))");

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
                    sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, ");
                    sb.Append(kvpVar);
                    sb.AppendLine(".Key.ToString());");
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
                sb.Append(indent);
                if (enumType.AsString)
                {
                    sb.Append("global::GenJson.GenJsonWriter.WriteString(span, ref index, ");
                    sb.Append(valueAccessor);
                    sb.AppendLine(".ToString());");
                }
                else
                {
                    sb.Append("{ if (!");
                    sb.Append($"(({enumType.UnderlyingType}){valueAccessor}).TryFormat(span.Slice(index), out int written, default, System.Globalization.CultureInfo.InvariantCulture)");
                    sb.AppendLine(") throw new System.Exception(\"Buffer too small\"); index += written; }");
                }
                break;
        }
    }
}
