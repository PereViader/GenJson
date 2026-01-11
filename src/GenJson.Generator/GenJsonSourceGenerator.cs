using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenJson.Generator;

public enum GenJsonDataType
{
    Primitive,
    String,
    Object,
    Enumerable
}

public class EquatableArray<T> : IEquatable<EquatableArray<T>>
{
    public EquatableArray(IEnumerable<T> collection)
    {
        Value = collection.ToArray();
    }

    public T[] Value { get; }

    public bool Equals(EquatableArray<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value.SequenceEqual(other.Value);
    }

    public override bool Equals(object? obj) => Equals(obj as EquatableArray<T>);

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var item in Value)
        {
            hash = hash * 23 + (item?.GetHashCode() ?? 0);
        }
        return hash;
    }
}

public record PropertyData(string Name, string TypeName, bool IsNullable, GenJsonDataType Type, GenJsonDataType ElementType);
public record ClassData(string ClassName, string Namespace, EquatableArray<PropertyData> Properties);

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
                bool isNullable = propertySymbol.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
                                  propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;

                GenJsonDataType type = GenJsonDataType.Primitive;
                GenJsonDataType elementType = GenJsonDataType.Primitive;

                if (propertySymbol.Type.SpecialType == SpecialType.System_String)
                {
                    type = GenJsonDataType.String;
                }
                else if (HasGenJsonAttribute(propertySymbol.Type))
                {
                    type = GenJsonDataType.Object;
                }
                else
                {
                    ITypeSymbol? resolvedElementType = GetEnumerableElementType(propertySymbol.Type);

                    if (resolvedElementType != null)
                    {
                        type = GenJsonDataType.Enumerable;
                        if (resolvedElementType.SpecialType == SpecialType.System_String)
                        {
                            elementType = GenJsonDataType.String;
                        }
                        else if (HasGenJsonAttribute(resolvedElementType))
                        {
                            elementType = GenJsonDataType.Object;
                        }
                        else
                        {
                            elementType = GenJsonDataType.Primitive;
                        }
                    }
                    else
                    {
                        type = GenJsonDataType.Primitive;
                    }
                }

                properties.Add(new PropertyData(propertySymbol.Name, propertySymbol.Type.ToDisplayString(), isNullable, type, elementType));
            }
        }

        return new ClassData(classSymbol.Name, ns, new EquatableArray<PropertyData>(properties));
    }

    private static bool HasGenJsonAttribute(ITypeSymbol type)
    {
        if (type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonAttribute"))
        {
            return true;
        }

        // Check nullable underlying type
        if (type is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return namedType.TypeArguments[0].GetAttributes()
                  .Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonAttribute");
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
        sb.AppendLine("            bool first = true;");

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

            if (prop.Type == GenJsonDataType.Enumerable)
            {
                // Enclose in a block to scope 'firstItem'
                sb.Append(indent);
                sb.AppendLine("{");

                string arrayIndent = indent + "    ";

                sb.Append(arrayIndent);
                sb.AppendLine("sb.Append(\"[\");");

                sb.Append(arrayIndent);
                sb.AppendLine("bool firstItem = true;");

                sb.Append(arrayIndent);
                sb.Append("foreach (var item in obj.");
                sb.Append(prop.Name);
                sb.AppendLine(")");
                sb.Append(arrayIndent);
                sb.AppendLine("{");

                string loopIndent = arrayIndent + "    ";

                sb.Append(loopIndent);
                sb.AppendLine("if (!firstItem)");
                sb.Append(loopIndent);
                sb.AppendLine("{");
                sb.Append(loopIndent);
                sb.AppendLine("    sb.Append(\",\");");
                sb.Append(loopIndent);
                sb.AppendLine("}");
                sb.Append(loopIndent);
                sb.AppendLine("firstItem = false;");

                if (prop.ElementType == GenJsonDataType.Object)
                {
                    sb.Append(loopIndent);
                    sb.AppendLine("item.ToJson(sb);");
                }
                else if (prop.ElementType == GenJsonDataType.String)
                {
                    sb.Append(loopIndent);
                    sb.AppendLine("sb.Append(\"\\\"\");");
                    sb.Append(loopIndent);
                    sb.AppendLine("sb.Append(item);");
                    sb.Append(loopIndent);
                    sb.AppendLine("sb.Append(\"\\\"\");");
                }
                else
                {
                    sb.Append(loopIndent);
                    sb.AppendLine("sb.Append(item);");
                }

                sb.Append(arrayIndent);
                sb.AppendLine("}"); // end foreach

                sb.Append(arrayIndent);
                sb.AppendLine("sb.Append(\"]\");");

                sb.Append(indent);
                sb.AppendLine("}"); // end block
            }
            else
            {
                // Start Quote if string
                if (prop.Type == GenJsonDataType.String)
                {
                    sb.Append(indent);
                    sb.AppendLine("sb.Append(\"\\\"\");");
                }

                // Property Value
                sb.Append(indent);

                if (prop.Type == GenJsonDataType.Object)
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
                if (prop.Type == GenJsonDataType.String)
                {
                    sb.Append(indent);
                    sb.AppendLine("sb.Append(\"\\\"\");");
                }
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