using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace GenJson.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GenJsonCodeFixProvider)), Shared]
    public class GenJsonCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("GENJSON006");

        public sealed override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (declaration == null) return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "GenJson: implement converter members",
                    createChangedDocument: c => ImplementConverterMembersAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(GenJsonCodeFixProvider)),
                diagnostic);
        }

        private async Task<Document> ImplementConverterMembersAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null) return document;

            var symbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken) as INamedTypeSymbol;
            if (symbol == null) return document;

            var converterAttr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute");
            if (converterAttr == null || converterAttr.ConstructorArguments.Length != 1 || !(converterAttr.ConstructorArguments[0].Value is ITypeSymbol targetType))
            {
                return document;
            }

            string targetTypeName = targetType.ToMinimalDisplayString(semanticModel, typeDecl.Identifier.SpanStart);

            bool needGetSize = !GenJsonAnalyzer.HasGetSize(symbol, targetType);
            bool needWriteJson = !GenJsonAnalyzer.HasWriteJson(symbol, targetType);
            bool needFromJson = !GenJsonAnalyzer.HasFromJson(symbol, targetType);
            bool needGetSizeUtf8 = !GenJsonAnalyzer.HasGetSizeUtf8(symbol, targetType);
            bool needWriteJsonUtf8 = !GenJsonAnalyzer.HasWriteJsonUtf8(symbol, targetType);
            bool needFromJsonUtf8 = !GenJsonAnalyzer.HasFromJsonUtf8(symbol, targetType);

            var methodsToAdd = new List<MemberDeclarationSyntax>();

            if (needGetSize)
            {
                var code = $@"public static int GetSize({targetTypeName} value)
{{
    throw new System.NotImplementedException();
}}";
                methodsToAdd.Add(ParseMember(code));
            }

            if (needWriteJson)
            {
                var code = $@"public static void WriteJson(System.Span<char> span, ref int index, {targetTypeName} value)
{{
    throw new System.NotImplementedException();
}}";
                methodsToAdd.Add(ParseMember(code));
            }

            if (needFromJson)
            {
                var code = $@"public static {targetTypeName}? FromJson(System.ReadOnlySpan<char> span, ref int index)
{{
    throw new System.NotImplementedException();
}}";
                methodsToAdd.Add(ParseMember(code));
            }

            if (needGetSizeUtf8)
            {
                var code = $@"public static int GetSizeUtf8({targetTypeName} value)
{{
    throw new System.NotImplementedException();
}}";
                methodsToAdd.Add(ParseMember(code));
            }

            if (needWriteJsonUtf8)
            {
                var code = $@"public static void WriteJsonUtf8(System.Span<byte> span, ref int index, {targetTypeName} value)
{{
    throw new System.NotImplementedException();
}}";
                methodsToAdd.Add(ParseMember(code));
            }

            if (needFromJsonUtf8)
            {
                var code = $@"public static {targetTypeName}? FromJsonUtf8(System.ReadOnlySpan<byte> span, ref int index)
{{
    throw new System.NotImplementedException();
}}";
                methodsToAdd.Add(ParseMember(code));
            }

            if (methodsToAdd.Count == 0) return document;

            var newTypeDecl = typeDecl.AddMembers(methodsToAdd.ToArray());
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);
            var newDocument = document.WithSyntaxRoot(newRoot);

            var formattedDocument = await Formatter.FormatAsync(newDocument, cancellationToken: cancellationToken).ConfigureAwait(false);
            return formattedDocument;
        }

        private static MemberDeclarationSyntax ParseMember(string code)
        {
            var member = SyntaxFactory.ParseMemberDeclaration(code);
            if (member == null) throw new InvalidOperationException("Failed to parse member code: " + code);
            return member.WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
