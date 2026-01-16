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
    public sealed record Primitive : GenJsonDataType // Integers
    {
        public static readonly Primitive Instance = new();
        private Primitive() { }
    }

    public sealed record Boolean : GenJsonDataType
    {
        public static readonly Boolean Instance = new();
        private Boolean() { }
    }

    public sealed record FloatingPoint : GenJsonDataType // Float, Double, Decimal
    {
        public static readonly FloatingPoint Instance = new();
        private FloatingPoint() { }
    }

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

    public sealed record DateOnly : GenJsonDataType
    {
        public static readonly DateOnly Instance = new();
        private DateOnly() { }
    }

    public sealed record TimeOnly : GenJsonDataType
    {
        public static readonly TimeOnly Instance = new();
        private TimeOnly() { }
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

    public sealed record Object : GenJsonDataType
    {
        public static readonly Object Instance = new();
        private Object() { }
    }

    public sealed record Nullable(GenJsonDataType Underlying) : GenJsonDataType;
    public sealed record Enumerable(GenJsonDataType ElementType) : GenJsonDataType;
    public sealed record Dictionary(GenJsonDataType KeyType, GenJsonDataType ValueType) : GenJsonDataType;
    public sealed record Enum(bool AsString, string UnderlyingType) : GenJsonDataType;
}

public record PropertyData(string Name, bool IsNullable, GenJsonDataType Type);
public record ClassData(string ClassName, string Namespace, EquatableList<PropertyData> Properties);

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
        if (node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return false;
        }

        if (classDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return false;
        }

        return classDeclarationSyntax.AttributeLists.Count > 0;
    }

    private static ClassData? GetClassData(GeneratorSyntaxContext context, CancellationToken ct)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        if (classSymbol is null)
        {
            return null;
        }

        if (!classSymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonAttribute"))
        {
            return null;
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? ""
            : classSymbol.ContainingNamespace.ToDisplayString();

        var properties = new List<PropertyData>();

        foreach (var member in classSymbol.GetMembers())
        {
            if (member is IPropertySymbol propertySymbol &&
                propertySymbol.DeclaredAccessibility == Accessibility.Public &&
                !propertySymbol.IsStatic)
            {
                bool isNullable = propertySymbol.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
                                  propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;

                var type = GetGenJsonDataType(propertySymbol, propertySymbol.Type);

                properties.Add(new PropertyData(propertySymbol.Name, isNullable, type));
            }
        }

        return new ClassData(classSymbol.Name, ns, new EquatableList<PropertyData>(properties));
    }

    private static GenJsonDataType GetGenJsonDataType(IPropertySymbol propertySymbol, ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol enumType)
        {
            var asString = propertySymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJson.Enum.AsText");
            var underlyingType = enumType.EnumUnderlyingType?.ToDisplayString() ?? "int";
            return new GenJsonDataType.Enum(asString, underlyingType);
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
            return GenJsonDataType.FloatingPoint.Instance;
        }

        var typeName = type.ToDisplayString();
        if (typeName == "System.Guid") return GenJsonDataType.Guid.Instance;
        if (typeName == "System.DateTime") return GenJsonDataType.DateTime.Instance;
        if (typeName == "System.DateOnly") return GenJsonDataType.DateOnly.Instance;
        if (typeName == "System.TimeOnly") return GenJsonDataType.TimeOnly.Instance;
        if (typeName == "System.TimeSpan") return GenJsonDataType.TimeSpan.Instance;
        if (typeName == "System.DateTimeOffset") return GenJsonDataType.DateTimeOffset.Instance;
        if (typeName == "System.Version") return GenJsonDataType.Version.Instance;

        if (HasGenJsonAttribute(type))
        {
            return GenJsonDataType.Object.Instance;
        }

        if (TryGetDictionaryTypes(type, out var keyType, out var valueType))
        {
            return new GenJsonDataType.Dictionary(GetGenJsonDataType(propertySymbol, keyType!), GetGenJsonDataType(propertySymbol, valueType!));
        }

        ITypeSymbol? resolvedElementType = GetEnumerableElementType(type);
        if (resolvedElementType != null)
        {
            return new GenJsonDataType.Enumerable(GetGenJsonDataType(propertySymbol, resolvedElementType));
        }

        return GenJsonDataType.Primitive.Instance;
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

        sb.Append("    public static class GenJson_");
        sb.Append(data.ClassName);
        sb.AppendLine("_Extensions");
        sb.AppendLine("    {");

        sb.Append("        public static string ToJson(this ");
        sb.Append(data.ClassName);
        sb.AppendLine("""
         obj)
                {
                    var sb = new System.Text.StringBuilder();
                    obj.ToJson(sb);
                    return sb.ToString();
                }
        """);

        sb.AppendLine();
        sb.Append("        public static void ToJson(this ");
        sb.Append(data.ClassName);
        sb.AppendLine(" obj, System.Text.StringBuilder sb)");
        sb.AppendLine("        {");
        sb.AppendLine("            sb.Append(\"{\");");

        bool needFirst = data.Properties.Value.Count > 1 && data.Properties.Value[0].IsNullable;
        if (needFirst)
        {
            sb.AppendLine("            bool first = true;");
        }

        var state = 0; // 0: True, 1: False, 2: Unknown

        foreach (var prop in data.Properties.Value)
        {
            string indent = "            ";

            if (prop.IsNullable)
            {
                sb.Append("            if (obj.");
                sb.Append(prop.Name);
                sb.AppendLine(" is not null)");
                sb.AppendLine("            {");
                indent = "                ";
            }

            if (state == 0) // True
            {
                if (prop.IsNullable)
                {
                    if (needFirst)
                    {
                        sb.Append(indent);
                        sb.AppendLine("first = false;");
                    }
                    state = 2; // Unknown
                }
                else
                {
                    state = 1; // False
                }
            }
            else if (state == 1) // False
            {
                sb.Append(indent);
                sb.AppendLine("sb.Append(\",\");");
            }
            else // Unknown
            {
                sb.Append(indent);
                sb.AppendLine("if (!first)");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    sb.Append(\",\");");
                sb.Append(indent);
                sb.AppendLine("}");

                if (prop.IsNullable)
                {
                    sb.Append(indent);
                    sb.AppendLine("first = false;");
                }
                else
                {
                    state = 1; // False
                }
            }

            // Property Key
            sb.Append(indent);
            sb.Append("sb.Append(\"\\\"");
            sb.Append(prop.Name);
            sb.AppendLine("\\\":\");"); // "Key":

            GenerateValue(sb, prop.Type, $"obj.{prop.Name}", indent, 0);

            if (prop.IsNullable)
            {
                sb.AppendLine("            }");
            }
        }

        sb.AppendLine("            sb.Append(\"}\");");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        if (!string.IsNullOrEmpty(data.Namespace))
        {
            sb.AppendLine("}");
        }

        context.AddSource($"{data.ClassName}.GenJson.g.cs", sb.ToString());
    }

    private void GenerateValue(StringBuilder sb, GenJsonDataType type, string valueAccessor, string indent, int depth)
    {
        switch (type)
        {
            case GenJsonDataType.Primitive:
                sb.Append(indent);
                sb.Append("sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(");");
                break;

            case GenJsonDataType.Boolean:
                sb.Append(indent);
                sb.Append("sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(" ? \"true\" : \"false\");");
                break;

            case GenJsonDataType.Char:
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                sb.Append(indent);
                sb.Append("sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(");");
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                break;

            case GenJsonDataType.FloatingPoint:
                sb.Append(indent);
                sb.Append("sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(".ToString(System.Globalization.CultureInfo.InvariantCulture));");
                break;

            case GenJsonDataType.Guid:
            case GenJsonDataType.Version:
            case GenJsonDataType.TimeSpan:
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                sb.Append(indent);
                sb.Append("sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(".ToString());");
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                break;

            case GenJsonDataType.DateTime:
            case GenJsonDataType.DateOnly:
            case GenJsonDataType.TimeOnly:
            case GenJsonDataType.DateTimeOffset:
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                sb.Append(indent);
                sb.Append("sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(".ToString(\"O\"));");
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                break;

            case GenJsonDataType.String:
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                sb.Append(indent);
                sb.Append("sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(");");
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                break;

            case GenJsonDataType.Object:
                sb.Append(indent);
                sb.Append(valueAccessor);
                sb.AppendLine(".ToJson(sb);");
                break;

            case GenJsonDataType.Nullable nullable:
                sb.Append(indent);
                sb.Append("if (");
                sb.Append(valueAccessor);
                sb.AppendLine(" is null)");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    sb.Append(\"null\");");
                sb.Append(indent);
                sb.AppendLine("}");
                sb.Append(indent);
                sb.AppendLine("else");
                sb.Append(indent);
                sb.AppendLine("{");
                GenerateValue(sb, nullable.Underlying, $"{valueAccessor}.Value", indent + "    ", depth);
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Enumerable enumerable:
                {
                    sb.Append(indent);
                    sb.AppendLine("{");

                    string arrayIndent = indent + "    ";

                    sb.Append(arrayIndent);
                    sb.AppendLine("sb.Append(\"[\");");

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
                    sb.AppendLine("    sb.Append(\",\");");
                    sb.Append(loopIndent);
                    sb.AppendLine("}");
                    sb.Append(loopIndent);
                    sb.Append(firstItemVar);
                    sb.AppendLine(" = false;");

                    GenerateValue(sb, enumerable.ElementType, itemVar, loopIndent, depth + 1);

                    sb.Append(arrayIndent);
                    sb.AppendLine("}"); // end foreach

                    sb.Append(arrayIndent);
                    sb.AppendLine("sb.Append(\"]\");");

                    sb.Append(indent);
                    sb.AppendLine("}"); // end block
                }
                break;

            case GenJsonDataType.Dictionary dictionary:
                {
                    // Enclose in a block to scope variables
                    sb.Append(indent);
                    sb.AppendLine("{");

                    string dictIndent = indent + "    ";

                    sb.Append(dictIndent);
                    sb.AppendLine("sb.Append(\"{\");");

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
                    sb.AppendLine("    sb.Append(\",\");");
                    sb.Append(loopIndentDict);
                    sb.AppendLine("}");
                    sb.Append(loopIndentDict);
                    sb.Append(firstItemVarDict);
                    sb.AppendLine(" = false;");

                    // Key
                    sb.Append(loopIndentDict);
                    sb.AppendLine("sb.Append(\"\\\"\");");

                    sb.Append(loopIndentDict);
                    sb.Append("sb.Append(");
                    sb.Append(kvpVar);
                    sb.AppendLine(".Key);");

                    sb.Append(loopIndentDict);
                    sb.AppendLine("sb.Append(\"\\\":\");");

                    // Value
                    GenerateValue(sb, dictionary.ValueType, $"{kvpVar}.Value", loopIndentDict, depth + 1);

                    sb.Append(dictIndent);
                    sb.AppendLine("}"); // end foreach

                    sb.Append(dictIndent);
                    sb.AppendLine("sb.Append(\"}\");");

                    sb.Append(indent);
                    sb.AppendLine("}"); // end block
                }
                break;

            case GenJsonDataType.Enum enumType:
                sb.Append(indent);
                if (enumType.AsString)
                {
                    sb.AppendLine("sb.Append(\"\\\"\");");
                    sb.Append(indent);
                    sb.Append("sb.Append(");
                    sb.Append(valueAccessor);
                    sb.AppendLine(".ToString());");
                    sb.Append(indent);
                    sb.AppendLine("sb.Append(\"\\\"\");");
                }
                else
                {
                    sb.Append("sb.Append((");
                    sb.Append(enumType.UnderlyingType);
                    sb.Append(")");
                    sb.Append(valueAccessor);
                    sb.AppendLine(");");
                }
                break;
        }
    }
}