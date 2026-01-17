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

    public sealed record Object(string TypeName) : GenJsonDataType; // Another GenJson class

    public sealed record Nullable(GenJsonDataType Underlying) : GenJsonDataType;
    public sealed record Enumerable(GenJsonDataType ElementType, bool IsArray, string ConstructionTypeName, string ElementTypeName) : GenJsonDataType;
    public sealed record Dictionary(GenJsonDataType KeyType, GenJsonDataType ValueType, string ConstructionTypeName, string KeyTypeName, string ValueTypeName) : GenJsonDataType;
    public sealed record Enum(string TypeName, bool AsString, string UnderlyingType) : GenJsonDataType;
}

public record PropertyData(string Name, string TypeName, bool IsNullable, GenJsonDataType Type);
public record ClassData(string ClassName, string Namespace, EquatableList<PropertyData> Properties, bool IsPartial);

[Generator]
public class GenJsonSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classData = context.SyntaxProvider
            .CreateSyntaxProvider(IsSyntaxNodeValid, GetClassData)
            .Where(x => x is not null);

        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("GenJsonParser.g.cs", ParserSource));
        context.RegisterSourceOutput(classData, Generate!);
    }

    private const string ParserSource = """
#nullable enable
namespace GenJson
{
    using System;
    using System.Globalization;
    using System.Text;

    internal static class GenJsonParser
    {
        public static void SkipWhitespace(ReadOnlySpan<char> json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
            {
                index++;
            }
        }

        public static void Expect(ReadOnlySpan<char> json, ref int index, char expected)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length || json[index] != expected)
            {
                throw new Exception($"Expected '{expected}' at {index}");
            }
            index++;
        }

        public static string ParseString(ReadOnlySpan<char> json, ref int index)
        {
            Expect(json, ref index, '"');
            var sb = new StringBuilder();
            while (index < json.Length)
            {
                var c = json[index++];
                if (c == '"') return sb.ToString();
                if (c == '\\')
                {
                    if (index >= json.Length) throw new Exception("Unexpected end of json string at " + index);
                    c = json[index++];
                    switch (c)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u': // Unicode escape
                            if (index + 4 > json.Length) throw new Exception("Invalid unicode escape at " + index);
                            var hexSequence = json.Slice(index, 4);
                            index += 4;
                            sb.Append((char)int.Parse(hexSequence, NumberStyles.HexNumber));
                            break;
                        default: sb.Append(c); break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            throw new Exception("Unterminated string at " + index);
        }

        public static char ParseChar(ReadOnlySpan<char> json, ref int index)
        {
            var s = ParseString(json, ref index);
            if (s.Length != 1) throw new Exception("Expected string of length 1 for char at " + index);
            return s[0];
        }

        public static int ParseInt(ReadOnlySpan<char> json, ref int index) => (int)ParseLong(json, ref index);
        public static uint ParseUInt(ReadOnlySpan<char> json, ref int index) => (uint)ParseLong(json, ref index);
        public static short ParseShort(ReadOnlySpan<char> json, ref int index) => (short)ParseLong(json, ref index);
        public static ushort ParseUShort(ReadOnlySpan<char> json, ref int index) => (ushort)ParseLong(json, ref index);
        public static byte ParseByte(ReadOnlySpan<char> json, ref int index) => (byte)ParseLong(json, ref index);
        public static sbyte ParseSByte(ReadOnlySpan<char> json, ref int index) => (sbyte)ParseLong(json, ref index);

        public static long ParseLong(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            int start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && char.IsDigit(json[index])) index++;
            var slice = json.Slice(start, index - start);
            return long.Parse(slice);
        }
        
        public static ulong ParseULong(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            int start = index;
            while (index < json.Length && char.IsDigit(json[index])) index++;
            var slice = json.Slice(start, index - start);
            return ulong.Parse(slice);
        }

        public static double ParseDouble(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            int start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            return double.Parse(slice, CultureInfo.InvariantCulture);
        }
        
        public static float ParseFloat(ReadOnlySpan<char> json, ref int index) => (float)ParseDouble(json, ref index);

        public static decimal ParseDecimal(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            int start = index;
            if (index < json.Length && json[index] == '-') index++;
            while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            var slice = json.Slice(start, index - start);
            return decimal.Parse(slice, CultureInfo.InvariantCulture);
        }

        public static bool ParseBoolean(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual("true"))
            {
                index += 4;
                return true;
            }
            if (json.Length - index >= 5 && json.Slice(index, 5).SequenceEqual("false"))
            {
                index += 5;
                return false;
            }
            throw new Exception("Expected boolean at " + index);
        }

        public static bool IsNull(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            return json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual("null");
        }

        public static void ParseNull(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (json.Length - index >= 4 && json.Slice(index, 4).SequenceEqual("null"))
            {
                index += 4;
                return;
            }
             throw new Exception("Expected null at " + index);
        }
        
        public static void SkipValue(ReadOnlySpan<char> json, ref int index)
        {
            SkipWhitespace(json, ref index);
            if (index >= json.Length) return;
            char c = json[index];
            if (c == '"')
            {
                ParseString(json, ref index);
            }
            else if (c == '{')
            {
                index++;
                while (index < json.Length)
                {
                    SkipWhitespace(json, ref index);
                    if (json[index] == '}')
                    {
                        index++;
                        return;
                    }
                    SkipValue(json, ref index); // Key (string) is a value
                    SkipWhitespace(json, ref index);
                    if (json[index] == ':') index++;
                    SkipValue(json, ref index); // Value
                    SkipWhitespace(json, ref index);
                    if (json[index] == ',') index++;
                }
            }
            else if (c == '[')
            {
                index++;
                while (index < json.Length)
                {
                    SkipWhitespace(json, ref index);
                    if (json[index] == ']')
                    {
                        index++;
                        return;
                    }
                    SkipValue(json, ref index);
                    SkipWhitespace(json, ref index);
                    if (json[index] == ',') index++;
                }
            }
            else if (char.IsDigit(c) || c == '-')
            {
                if (c == '-') index++;
                while (index < json.Length && (char.IsDigit(json[index]) || json[index] == '.' || json[index] == 'e' || json[index] == 'E' || json[index] == '+' || json[index] == '-')) index++;
            }
            else if (c == 't') // true
            {
                index += 4;
            }
            else if (c == 'f') // false
            {
                index += 5;
            }
            else if (c == 'n') // null
            {
                index += 4;
            }
            else
            {
                index++; // Unknown
            }
        }
    }
    internal static class GenJsonSizeHelper
    {
        public static int GetSize(int value)
        {
            if (value == 0) return 1;
            if (value == int.MinValue) return 11;
            if (value < 0) return 1 + GetSize(-value);
            if (value < 10) return 1;
            if (value < 100) return 2;
            if (value < 1000) return 3;
            if (value < 10000) return 4;
            if (value < 100000) return 5;
            if (value < 1000000) return 6;
            if (value < 10000000) return 7;
            if (value < 100000000) return 8;
            if (value < 1000000000) return 9;
            return 10;
        }

        public static int GetSize(long value)
        {
            if (value == 0) return 1;
            if (value == long.MinValue) return 20;
            if (value < 0) return 1 + GetSize(-value);
            if (value < 10L) return 1;
            if (value < 100L) return 2;
            if (value < 1000L) return 3;
            if (value < 10000L) return 4;
            if (value < 100000L) return 5;
            if (value < 1000000L) return 6;
            if (value < 10000000L) return 7;
            if (value < 100000000L) return 8;
            if (value < 1000000000L) return 9;
            if (value < 10000000000L) return 10;
            if (value < 100000000000L) return 11;
            if (value < 1000000000000L) return 12;
            if (value < 10000000000000L) return 13;
            if (value < 100000000000000L) return 14;
            if (value < 1000000000000000L) return 15;
            if (value < 10000000000000000L) return 16;
            if (value < 100000000000000000L) return 17;
            if (value < 1000000000000000000L) return 18;
            return 19;
        }
        
        public static int GetSize(bool value) => value ? 4 : 5;
        public static int GetSize(char value) => 3; // "c"
        public static int GetSize(string? value) => value is null ? 0 : value.Length + 2;

        public static int GetSize(Guid value) => 38;

        public static int GetSize(double value)
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, default, global::System.Globalization.CultureInfo.InvariantCulture))
            {
                return written;
            }
#endif
            return value.ToString(global::System.Globalization.CultureInfo.InvariantCulture).Length;
        }

        public static int GetSize(float value)
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, default, global::System.Globalization.CultureInfo.InvariantCulture))
            {
                return written;
            }
#endif
            return value.ToString(global::System.Globalization.CultureInfo.InvariantCulture).Length;
        }

        public static int GetSize(decimal value)
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, default, global::System.Globalization.CultureInfo.InvariantCulture))
            {
                return written;
            }
#endif
            return value.ToString(global::System.Globalization.CultureInfo.InvariantCulture).Length;
        }

        public static int GetSize(DateTime value)
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, "O", global::System.Globalization.CultureInfo.InvariantCulture))
            {
                return written + 2;
            }
#endif
            return value.ToString("O", global::System.Globalization.CultureInfo.InvariantCulture).Length + 2;
        }

        public static int GetSize(DateTimeOffset value)
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, "O", global::System.Globalization.CultureInfo.InvariantCulture))
            {
                return written + 2;
            }
#endif
            return value.ToString("O", global::System.Globalization.CultureInfo.InvariantCulture).Length + 2;
        }

        public static int GetSize(TimeSpan value)
        {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written, "c", global::System.Globalization.CultureInfo.InvariantCulture))
            {
                return written + 2;
            }
#endif
            return value.ToString("c", global::System.Globalization.CultureInfo.InvariantCulture).Length + 2;
        }



        public static int GetSize(Version value)
        {
#if NET6_0_OR_GREATER
            Span<char> buffer = stackalloc char[128];
            if (value.TryFormat(buffer, out int written))
            {
                return written + 2;
            }
#endif
            return value.ToString().Length + 2;
        }
    }
}
""";

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

                properties.Add(new PropertyData(propertySymbol.Name, propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), isNullable, type));
            }
        }

        bool isPartial = classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword);
        return new ClassData(classSymbol.Name, ns, new EquatableList<PropertyData>(properties), isPartial);
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
        if (typeName == "global::System.DateOnly" || typeName == "System.DateOnly") return GenJsonDataType.DateOnly.Instance;
        if (typeName == "global::System.TimeOnly" || typeName == "System.TimeOnly") return GenJsonDataType.TimeOnly.Instance;
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

        if (!data.IsPartial)
        {
            return;
        }

        if (!string.IsNullOrEmpty(data.Namespace))
        {
            sb.Append("namespace ");
            sb.Append(data.Namespace);
            sb.AppendLine();
            sb.AppendLine("{");
        }

        sb.Append("    partial class ");
        sb.Append(data.ClassName);
        sb.AppendLine();
        sb.AppendLine("    {");

        sb.AppendLine("        public int CalculateJsonSize()");
        sb.AppendLine("        {");
        sb.AppendLine("            int size = 2;");
        sb.AppendLine("            int propertyCount = 0;");

        foreach (var prop in data.Properties.Value)
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
            var sb = new System.Text.StringBuilder(CalculateJsonSize());
            this.ToJson(sb);
            return sb.ToString();
        }
""");

        sb.AppendLine();
        sb.AppendLine("        public void ToJson(System.Text.StringBuilder sb)");
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
                sb.Append("            if (this.");
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

            GenerateValue(sb, prop.Type, $"this.{prop.Name}", indent, 0);

            if (prop.IsNullable)
            {
                sb.AppendLine("            }");
            }
        }

        sb.AppendLine("            sb.Append(\"}\");");
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

        foreach (var prop in data.Properties.Value)
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
        sb.AppendLine("                    return new " + data.ClassName);
        sb.AppendLine("                    {");
        foreach (var prop in data.Properties.Value)
        {
            sb.Append("                        ");
            sb.Append(prop.Name);
            sb.Append(" = _");
            sb.Append(prop.Name);
            sb.AppendLine(",");
        }
        sb.AppendLine("                    };");
        sb.AppendLine("                }");

        sb.AppendLine("                var key = global::GenJson.GenJsonParser.ParseString(json, ref index);");
        sb.AppendLine("                global::GenJson.GenJsonParser.Expect(json, ref index, ':');");

        sb.AppendLine("                switch (key)");
        sb.AppendLine("                {");
        foreach (var prop in data.Properties.Value)
        {
            sb.Append("                    case \"");
            sb.Append(prop.Name);
            sb.AppendLine("\":");
            GenerateParseValue(sb, prop.Type, "_" + prop.Name, "                        ", 0);
            sb.AppendLine("                        break;");
        }
        sb.AppendLine("                    default:");
        sb.AppendLine("                        global::GenJson.GenJsonParser.SkipValue(json, ref index);");
        sb.AppendLine("                        break;");
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
            case GenJsonDataType.Primitive p:
                sb.Append(indent);
                sb.Append(target);
                sb.Append(" = global::GenJson.GenJsonParser.Parse");
                sb.Append(GetPrimitiveParserName(p.TypeName));
                sb.AppendLine("(json, ref index);");
                break;

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

            case GenJsonDataType.DateOnly:
                sb.Append(indent);
                sb.Append(target);
                sb.AppendLine(" = System.DateOnly.Parse(global::GenJson.GenJsonParser.ParseString(json, ref index));");
                break;

            case GenJsonDataType.TimeOnly:
                sb.Append(indent);
                sb.Append(target);
                sb.AppendLine(" = System.TimeOnly.Parse(global::GenJson.GenJsonParser.ParseString(json, ref index));");
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
                    else if (d.KeyType is GenJsonDataType.Primitive || d.KeyType is GenJsonDataType.FloatingPoint || d.KeyType is GenJsonDataType.Guid || d.KeyType is GenJsonDataType.DateTime || d.KeyType is GenJsonDataType.DateOnly || d.KeyType is GenJsonDataType.TimeOnly || d.KeyType is GenJsonDataType.TimeSpan || d.KeyType is GenJsonDataType.DateTimeOffset)
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
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    System.Span<char> buffer = stackalloc char[32];");
                sb.Append(indent);
                sb.Append("    if (");
                sb.Append(valueAccessor);
                sb.AppendLine(".TryFormat(buffer, out int written, \"R\", System.Globalization.CultureInfo.InvariantCulture))");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.AppendLine("        sb.Append(buffer.Slice(0, written));");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("    else");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.Append("        sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(".ToString(System.Globalization.CultureInfo.InvariantCulture));");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("}");
                break;

            case GenJsonDataType.Guid:
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    System.Span<char> buffer = stackalloc char[36];");
                sb.Append(indent);
                sb.Append("    if (");
                sb.Append(valueAccessor);
                sb.AppendLine(".TryFormat(buffer, out int written))");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.AppendLine("        sb.Append(buffer.Slice(0, written));");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("    else");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.Append("        sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(".ToString());");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("}");
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                break;

            case GenJsonDataType.Version:
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    System.Span<char> buffer = stackalloc char[64];");
                sb.Append(indent);
                sb.Append("    if (");
                sb.Append(valueAccessor);
                sb.AppendLine(".TryFormat(buffer, out int written))");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.AppendLine("        sb.Append(buffer.Slice(0, written));");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("    else");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.Append("        sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(".ToString());");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("}");
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                break;

            case GenJsonDataType.TimeSpan:
                sb.Append(indent);
                sb.AppendLine("sb.Append(\"\\\"\");");
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    System.Span<char> buffer = stackalloc char[64];");
                sb.Append(indent);
                sb.Append("    if (");
                sb.Append(valueAccessor);
                sb.AppendLine(".TryFormat(buffer, out int written))");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.AppendLine("        sb.Append(buffer.Slice(0, written));");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("    else");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.Append("        sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(".ToString());");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("}");
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
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    System.Span<char> buffer = stackalloc char[64];");
                sb.Append(indent);
                sb.Append("    if (");
                sb.Append(valueAccessor);
                sb.AppendLine(".TryFormat(buffer, out int written, \"O\", System.Globalization.CultureInfo.InvariantCulture))");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.AppendLine("        sb.Append(buffer.Slice(0, written));");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("    else");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.Append("        sb.Append(");
                sb.Append(valueAccessor);
                sb.AppendLine(".ToString(\"O\"));");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("}");
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

            case GenJsonDataType.DateOnly:
            case GenJsonDataType.TimeOnly:
                sb.Append(indent);
                sb.AppendLine("{");
                sb.Append(indent);
                sb.AppendLine("    global::System.Span<char> buffer = stackalloc char[128];");
                sb.Append(indent);
                sb.Append("    if (");
                sb.Append(valueAccessor);
                sb.AppendLine(".TryFormat(buffer, out int written, \"O\", global::System.Globalization.CultureInfo.InvariantCulture))");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.Append("        size += written");
                if (!unquoted) sb.Append(" + 2");
                sb.AppendLine(";");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("    else");
                sb.Append(indent);
                sb.AppendLine("    {");
                sb.Append(indent);
                sb.Append("        size += ");
                sb.Append(valueAccessor);
                sb.Append(".ToString(\"O\", global::System.Globalization.CultureInfo.InvariantCulture).Length");
                if (!unquoted) sb.Append(" + 2");
                sb.AppendLine(";");
                sb.Append(indent);
                sb.AppendLine("    }");
                sb.Append(indent);
                sb.AppendLine("}");
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
}