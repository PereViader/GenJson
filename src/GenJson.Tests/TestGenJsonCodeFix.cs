using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using GenJson.Analyzer;

namespace GenJson.Tests
{
    [TestFixture]
    public class TestGenJsonCodeFix
    {
        [Test]
        public void ApplyCodeFix_GeneratesConverterMembers()
        {
            var inputCode = """
                using System;
                using GenJson;

                namespace TestNamespace
                {
                    public struct ResId
                    {
                        public int Value { get; }
                    }

                    [GenJsonConverter(typeof(ResId))]
                    public static class ResIdConverter
                    {
                    }
                }
                """;

            var expectedCode = """
                using System;
                using GenJson;

                namespace TestNamespace
                {
                    public struct ResId
                    {
                        public int Value { get; }
                    }

                    [GenJsonConverter(typeof(ResId))]
                    public static class ResIdConverter
                    {
                        public static int GetSize(ResId value)
                        {
                            throw new System.NotImplementedException();
                        }

                        public static void WriteJson(System.Span<char> span, ref int index, ResId value)
                        {
                            throw new System.NotImplementedException();
                        }

                        public static ResId? FromJson(System.ReadOnlySpan<char> span, ref int index)
                        {
                            throw new System.NotImplementedException();
                        }

                        public static int GetSizeUtf8(ResId value)
                        {
                            throw new System.NotImplementedException();
                        }

                        public static void WriteJsonUtf8(System.Span<byte> span, ref int index, ResId value)
                        {
                            throw new System.NotImplementedException();
                        }

                        public static ResId? FromJsonUtf8(System.ReadOnlySpan<byte> span, ref int index)
                        {
                            throw new System.NotImplementedException();
                        }
                    }
                }
                """;

            var result = ApplyCodeFix(inputCode);
            
            Assert.That(NormalizeCode(result), Is.EqualTo(NormalizeCode(expectedCode)));
        }

        private static string NormalizeCode(string code)
        {
            return string.Join("\n", code
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0));
        }

        private static string ApplyCodeFix(string code)
        {
            using (var workspace = new AdhocWorkspace())
            {
                var projectId = ProjectId.CreateNewId();
                var documentId = DocumentId.CreateNewId(projectId);

                var references = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) && !a.Location.Contains("GenJson.Tests.dll"))
                    .Select(a => MetadataReference.CreateFromFile(a.Location))
                    .ToList();
                references.Add(MetadataReference.CreateFromFile(typeof(GenJsonAttribute).Assembly.Location));

                var projectInfo = ProjectInfo.Create(
                    projectId,
                    VersionStamp.Create(),
                    "TestProject",
                    "TestAssembly",
                    LanguageNames.CSharp,
                    metadataReferences: references,
                    compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable),
                    parseOptions: new CSharpParseOptions(LanguageVersion.Latest));

                var documentInfo = DocumentInfo.Create(
                    documentId,
                    "TestFile.cs",
                    loader: TextLoader.From(TextAndVersion.Create(SourceText.From(code, Encoding.UTF8), VersionStamp.Create())));

                var solution = workspace.CurrentSolution
                    .AddProject(projectInfo)
                    .AddDocument(documentInfo);

                var project = solution.GetProject(projectId)!;
                var document = project.GetDocument(documentId)!;

                var compilation = project.GetCompilationAsync().Result!;
                var compilationWithAnalyzers = compilation.WithAnalyzers(
                    ImmutableArray.Create<DiagnosticAnalyzer>(new GenJsonAnalyzer()));

                var diagnostics = compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().Result;
                var targetDiagnostic = diagnostics.FirstOrDefault(d => d.Id == "GENJSON006");
                if (targetDiagnostic == null)
                {
                    throw new Exception("GENJSON006 diagnostic not found. Diagnostics: " +
                                        string.Join("\n", diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}")));
                }

                var codeFixProvider = new GenJsonCodeFixProvider();
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(
                    document,
                    targetDiagnostic,
                    (action, diag) => actions.Add(action),
                    CancellationToken.None);

                codeFixProvider.RegisterCodeFixesAsync(context).Wait();
                if (actions.Count == 0)
                {
                    throw new Exception("No code fix registered.");
                }

                var actionToApply = actions[0];
                var operations = actionToApply.GetOperationsAsync(CancellationToken.None).Result;
                var applyChangesOperation = operations.OfType<ApplyChangesOperation>().FirstOrDefault();
                if (applyChangesOperation == null)
                {
                    throw new Exception("ApplyChangesOperation not found.");
                }

                var newSolution = applyChangesOperation.ChangedSolution;
                var newDocument = newSolution.GetDocument(documentId)!;
                var newSourceText = newDocument.GetTextAsync().Result!;

                return newSourceText.ToString();
            }
        }
    }
}
