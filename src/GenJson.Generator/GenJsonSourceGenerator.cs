using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenJson.Generator;

public record PropertyData(string Name, string TypeName, bool IsNullable, bool IsString, bool IsGenJson);
public record ClassData(string ClassName, string Namespace, List<PropertyData> Properties);

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
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        if (symbol is not INamedTypeSymbol classSymbol)
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
                bool isNullable = propertySymbol.Type.IsReferenceType ||
                                  propertySymbol.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

                bool isString = propertySymbol.Type.SpecialType == SpecialType.System_String;

                bool isGenJson = propertySymbol.Type.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonAttribute");

                // Also check if the underlying type of a nullable type has the attribute
                if (!isGenJson && propertySymbol.Type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    isGenJson = namedType.TypeArguments[0].GetAttributes()
                       .Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonAttribute");
                }

                properties.Add(new PropertyData(propertySymbol.Name, propertySymbol.Type.ToDisplayString(), isNullable, isString, isGenJson));
            }
        }

        return new ClassData(classSymbol.Name, ns, properties);
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
        sb.AppendLine("            bool first = true;");

        foreach (var prop in data.Properties)
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

            // Comma handling
            sb.Append(indent);
            sb.AppendLine("if (!first)");
            sb.Append(indent);
            sb.AppendLine("{");
            sb.Append(indent);
            sb.AppendLine("    sb.Append(\",\");");
            sb.Append(indent);
            sb.AppendLine("}");
            sb.Append(indent);
            sb.AppendLine("first = false;");

            // Property Key
            sb.Append(indent);
            sb.Append("sb.Append(\"\\\"");
            sb.Append(prop.Name);
            sb.AppendLine("\\\":\");"); // "Key":

            // Start Quote if string
            if (prop.IsString)
            {
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
            }

            // Property Value
            sb.Append(indent);

            if (prop.IsGenJson)
            {
                sb.Append("obj.");
                sb.Append(prop.Name);
                sb.AppendLine(".ToJson(sb);");
            }
            else
            {
                sb.Append("sb.Append(obj.");
                sb.Append(prop.Name);
                sb.AppendLine(");");
            }

            // End Quote if string
            if (prop.IsString)
            {
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
            }

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
}