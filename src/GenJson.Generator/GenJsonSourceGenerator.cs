using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenJson.Generator;

public record PropertyData(string Name, bool IsString);
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
                properties.Add(new PropertyData(
                    propertySymbol.Name,
                    propertySymbol.Type.SpecialType == SpecialType.System_String
                ));
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
        sb.AppendLine(" obj)");
        sb.AppendLine("        {");

        sb.AppendLine("            var sb = new System.Text.StringBuilder();");
        sb.AppendLine("            sb.Append(\"{\");");

        for (int i = 0; i < data.Properties.Count; i++)
        {
            var prop = data.Properties[i];

            sb.Append("            sb.Append(\"");
            if (i > 0)
            {
                sb.Append(",");
            }
            sb.Append("\\\"");
            sb.Append(prop.Name);
            sb.Append("\\\":");
            if (prop.IsString)
            {
                sb.Append("\\\"");
            }
            sb.AppendLine("\");");

            sb.Append("            sb.Append(obj.");
            sb.Append(prop.Name);
            sb.AppendLine(");");

            if (prop.IsString)
            {
                sb.AppendLine("            sb.Append(\"\\\"\");");
            }
        }

        sb.AppendLine("            sb.Append(\"}\");");
        sb.AppendLine("            return sb.ToString();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        if (!string.IsNullOrEmpty(data.Namespace))
        {
            sb.AppendLine("}");
        }

        context.AddSource($"{data.ClassName}.GenJson.g.cs", sb.ToString());
    }
}