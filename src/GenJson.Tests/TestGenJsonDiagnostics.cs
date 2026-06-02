using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using GenJson.Generator;

namespace GenJson.Tests;

[TestFixture]
public class TestGenJsonDiagnostics
{
    [Test]
    public void StaticClass_ReportsError()
    {
        var code = """
            using GenJson;

            namespace TestNamespace;

            [GenJson]
            public static class StaticTestClass
            {
                public static string Name { get; set; } = "";
            }
            """;

        var diagnostics = GetDiagnostics(code);
        Assert.That(diagnostics.Any(d => d is { Id: "GENJSON001", Severity: DiagnosticSeverity.Error }), Is.True, 
            "GENJSON001 not found. All diagnostics:\n" + string.Join("\n", diagnostics.Select(d => $"{d.Id} ({d.Severity}): {d.GetMessage()}")));
    }

    [Test]
    public void NonPartialClass_ReportsError()
    {
        var code = """
            using GenJson;

            namespace TestNamespace;

            [GenJson]
            public class NonPartialTestClass
            {
                public string Name { get; set; } = "";
            }
            """;

        var diagnostics = GetDiagnostics(code);
        Assert.That(diagnostics.Any(d => d is { Id: "GENJSON002", Severity: DiagnosticSeverity.Error }), Is.True, 
            "GENJSON002 not found. All diagnostics:\n" + string.Join("\n", diagnostics.Select(d => $"{d.Id} ({d.Severity}): {d.GetMessage()}")));
    }

    [Test]
    public void PartialClass_NoErrors()
    {
        var code = """
            using GenJson;

            namespace TestNamespace;

            [GenJson]
            public partial class ValidTestClass
            {
                public string Name { get; set; } = "";
            }
            """;

        var diagnostics = GetDiagnostics(code);
        Assert.That(diagnostics.Where(d => d.Id.StartsWith("GENJSON")), Is.Empty);
    }

    private static ImmutableArray<Diagnostic> GetDiagnostics(string code)
    {
        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) && !a.Location.Contains("GenJson.Tests.dll"))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(GenJsonAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create("TestAssembly",
            [CSharpSyntaxTree.ParseText(SourceText.From(code, Encoding.UTF8), new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(new GenJsonSourceGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        return outputCompilation.GetDiagnostics().AddRange(generatorDiagnostics);
    }
}
