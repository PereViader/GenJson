using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Threading.Tasks;
using GenJson.Generator;
using VerifyNUnit;

namespace GenJson.Tests;

[TestFixture]
public class TestsSourceGenerator
{
    [Test]
    public void Generated()
    {
        var code = File.ReadAllText("TestSourceGenerator.Source.cs");
        var generated = Generate(code);

        Assert.That(generated.diagnostics, Is.Empty);
        
        var combinedCode = string.Join("\n", generated.code);
        Assert.That(combinedCode, Does.Contain("global::System.Buffers.Text.Utf8Formatter.TryFormat(this."));
        Assert.That(combinedCode, Does.Contain("global::System.Buffers.Text.Utf8Formatter.TryFormat(_count"));
    }

    [Test]
    public void CustomConverterGeneratorTest()
    {
        var code = """
using System;
using GenJson;

namespace MyTest
{
    public struct CustomId
    {
        public int Id { get; }
        public CustomId(int id) => Id = id;
    }

    [GenJsonConverter(typeof(CustomId))]
    public static class CustomIdConverter
    {
        public static int GetSize(CustomId value) => 5;
        public static void WriteJson(Span<char> span, ref int index, CustomId value) {}
        public static CustomId? FromJson(ReadOnlySpan<char> span, ref int index) => null;
        public static int GetSizeUtf8(CustomId value) => 5;
        public static void WriteJsonUtf8(Span<byte> span, ref int index, CustomId value) {}
        public static CustomId? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index) => null;
    }

    [GenJson]
    public partial class Player
    {
        public CustomId Id { get; set; }
    }
}
""";

        var generated = Generate(code);
        Assert.That(generated.diagnostics, Is.Empty);
        
        var combinedCode = string.Join("\n", generated.code);
        Assert.That(combinedCode, Does.Contain("global::MyTest.CustomIdConverter.WriteJson"));
        Assert.That(combinedCode, Does.Contain("global::MyTest.CustomIdConverter.FromJson"));
    }

    [Test]
    public void AssemblyInitializerDeinitializeTest()
    {
        var code = """
using GenJson;

namespace MyTest
{
    [GenJson]
    public partial class Player
    {
        public string? Name { get; set; }
    }
}
""";

        var generated = Generate(code);
        Assert.That(generated.diagnostics, Is.Empty);
        
        var combinedCode = string.Join("\n", generated.code);
        Assert.That(combinedCode, Does.Contain("public static void Deinitialize()"));
        Assert.That(combinedCode, Does.Contain("global::GenJson.GenJsonGenericRegistry.Deregister<global::MyTest.Player>();"));
    }

    [Test]
    public void SourceFilenameCollisionTest()
    {
        var code = """
using GenJson;

namespace NamespaceA
{
    [GenJson]
    public partial class User
    {
        public int Id { get; set; }
    }
}

namespace NamespaceB
{
    [GenJson]
    public partial class User
    {
        public string? Name { get; set; }
    }
}
""";

        var generated = Generate(code);
        var files = generated.code.ToList();
        
        // Let's print out the file contents or their count
        foreach (var file in files)
        {
            Console.WriteLine("GENERATED FILE:\n" + file + "\n---");
        }
        
        Assert.That(generated.diagnostics, Is.Empty);
        Assert.That(files.Count, Is.EqualTo(3)); // 1 assembly initializer + 2 classes
    }

    [Test]
    public void DictionaryKeyParsingTest()
    {
        var code = """
using System.Collections.Generic;
using GenJson;

namespace MyTest
{
    [GenJson]
    public partial class DictionaryHolder
    {
        public Dictionary<int, string>? IntKeyDict { get; set; }
        public Dictionary<double, string>? DoubleKeyDict { get; set; }
    }
}
""";

        var generated = Generate(code);
        Assert.That(generated.diagnostics, Is.Empty);
    }

    private static (IEnumerable<string> code, ImmutableArray<Diagnostic> diagnostics) Generate(string code)
    {
        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) && !a.Location.Contains("GenJson.Tests.dll"))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(GenJsonAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create("AssemblyName",
            [CSharpSyntaxTree.ParseText(SourceText.From(code, Encoding.UTF8), new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new GenJsonSourceGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generatedTrees = outputCompilation.SyntaxTrees.ToList();

        var generatedCode = generatedTrees.Skip(1).Select(x => x.ToString());
        return (code: generatedCode, diagnostics: outputCompilation.GetDiagnostics());
    }
}