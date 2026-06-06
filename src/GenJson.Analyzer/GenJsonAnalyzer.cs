using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GenJson.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GenJsonAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor GENJSON001 = new DiagnosticDescriptor(
            "GENJSON001",
            "GenJson attribute cannot be applied to static types",
            "The type '{0}' is static. GenJson attribute cannot be applied to static types.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GENJSON002 = new DiagnosticDescriptor(
            "GENJSON002",
            "GenJson attribute can only be applied to partial types",
            "The type '{0}' must be declared as partial to use the GenJson attribute.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GENJSON003 = new DiagnosticDescriptor(
            "GENJSON003",
            "Custom converter methods must return a nullable type",
            "The custom converter '{0}' has a FromJson or FromJsonUtf8 method that returns a non-nullable type. Custom converter methods must return a nullable type (either a reference type or a Nullable<T>).",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GENJSON004 = new DiagnosticDescriptor(
            "GENJSON004",
            "Unsupported type in root registration",
            "The type '{0}' registered as a root type is not supported. It must be a primitive, enum, marked with [GenJson], or have a registered custom converter.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor GENJSON005 = new DiagnosticDescriptor(
            "GENJSON005",
            "Serializer class must be static and partial",
            "The class '{0}' decorated with [GenJsonSerializable] must be both static and partial.",
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(GENJSON001, GENJSON002, GENJSON003, GENJSON004, GENJSON005);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.RecordStructDeclaration);

            context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
        }

        private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var typeDecl = (TypeDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(typeDecl, context.CancellationToken);
            if (symbol == null) return;

            // Check GenJsonAttribute rules
            var hasGenJson = symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonAttribute");
            if (hasGenJson)
            {
                bool isStatic = typeDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
                bool isPartial = typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword);

                if (isStatic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GENJSON001, typeDecl.Identifier.GetLocation(), symbol.Name));
                }

                if (!isPartial)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GENJSON002, typeDecl.Identifier.GetLocation(), symbol.Name));
                }
            }

            // Check GenJsonConverterAttribute on type declaration
            var converterAttr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute");
            if (converterAttr != null)
            {
                ValidateConverter(context, symbol, converterAttr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation() ?? typeDecl.Identifier.GetLocation());
            }

            // Check GenJsonSerializableAttribute rules
            var serializableAttr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonSerializableAttribute");
            if (serializableAttr != null)
            {
                bool isStatic = typeDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
                bool isPartial = typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword);
                var errorLocation = typeDecl.Identifier.GetLocation();

                if (!isStatic || !isPartial)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GENJSON005, errorLocation, symbol.Name));
                }

                if (serializableAttr.ConstructorArguments.Length == 1 && serializableAttr.ConstructorArguments[0].Value is ITypeSymbol typeSymbolArg)
                {
                    var registry = BuildRegistry(context.Compilation);
                    if (!IsSupportedType(typeSymbolArg, context.Compilation, registry))
                    {
                        var attrLocation = serializableAttr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation() ?? errorLocation;
                        context.ReportDiagnostic(Diagnostic.Create(GENJSON004, attrLocation, typeSymbolArg.ToDisplayString()));
                    }
                }
            }
        }

        private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            var propertyDecl = (PropertyDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(propertyDecl, context.CancellationToken);
            if (symbol == null) return;

            var converterAttr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute");
            if (converterAttr != null && converterAttr.ConstructorArguments.Length == 1 && converterAttr.ConstructorArguments[0].Value is ITypeSymbol converterType)
            {
                ValidateConverter(context, converterType, converterAttr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation() ?? propertyDecl.Identifier.GetLocation());
            }
        }

        private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDecl = (FieldDeclarationSyntax)context.Node;
            foreach (var variable in fieldDecl.Declaration.Variables)
            {
                var symbol = context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken);
                if (symbol == null) continue;

                var converterAttr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute");
                if (converterAttr != null && converterAttr.ConstructorArguments.Length == 1 && converterAttr.ConstructorArguments[0].Value is ITypeSymbol converterType)
                {
                    ValidateConverter(context, converterType, converterAttr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation() ?? variable.Identifier.GetLocation());
                }
            }
        }

        private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
        {
            var paramSyntax = (ParameterSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(paramSyntax, context.CancellationToken);
            if (symbol == null) return;

            var converterAttr = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute");
            if (converterAttr != null && converterAttr.ConstructorArguments.Length == 1 && converterAttr.ConstructorArguments[0].Value is ITypeSymbol converterType)
            {
                ValidateConverter(context, converterType, converterAttr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation() ?? paramSyntax.Identifier.GetLocation());
            }
        }

        private static void ValidateConverter(SyntaxNodeAnalysisContext context, ITypeSymbol converterType, Location location)
        {
            var fromJsonMethod = converterType.GetMembers("FromJson")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters.Length == 2 && m.IsStatic);
            if (fromJsonMethod != null)
            {
                var ret = fromJsonMethod.ReturnType;
                bool isNullable = ret.IsReferenceType || ret.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
                if (!isNullable)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GENJSON003, location, converterType.Name));
                    return;
                }
            }

            var fromJsonUtf8Method = converterType.GetMembers("FromJsonUtf8")
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters.Length == 2 && m.IsStatic);
            if (fromJsonUtf8Method != null)
            {
                var ret = fromJsonUtf8Method.ReturnType;
                bool isNullable = ret.IsReferenceType || ret.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
                if (!isNullable)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GENJSON003, location, converterType.Name));
                }
            }
        }

        private static ConverterRegistry BuildRegistry(Compilation compilation)
        {
            var mappings = new List<ConverterMapping>();

            // Current assembly
            FindConverters(compilation.Assembly.GlobalNamespace, mappings);

            // Assembly attributes of current assembly
            foreach (var attr in compilation.Assembly.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute")
                {
                    if (attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is ITypeSymbol converterType)
                    {
                        var converterAttr = converterType.GetAttributes().FirstOrDefault(a =>
                            a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute");
                        if (converterAttr != null && converterAttr.ConstructorArguments.Length == 1 &&
                            converterAttr.ConstructorArguments[0].Value is ITypeSymbol targetType)
                        {
                            mappings.Add(new ConverterMapping(
                                targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                converterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                        }
                    }
                }
            }

            // Referenced assemblies
            foreach (var refAssembly in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                FindConverters(refAssembly.GlobalNamespace, mappings);
            }

            return new ConverterRegistry(mappings);
        }

        private static void FindConverters(INamespaceSymbol ns, List<ConverterMapping> mappings)
        {
            foreach (var member in ns.GetMembers())
            {
                if (member is INamespaceSymbol childNs)
                {
                    FindConverters(childNs, mappings);
                }
                else if (member is INamedTypeSymbol typeSymbol)
                {
                    FindConvertersInType(typeSymbol, mappings);
                }
            }
        }

        private static void FindConvertersInType(INamedTypeSymbol typeSymbol, List<ConverterMapping> mappings)
        {
            var attr = typeSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonConverterAttribute");
            if (attr != null && attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is ITypeSymbol targetType)
            {
                mappings.Add(new ConverterMapping(
                    targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }

            foreach (var nested in typeSymbol.GetTypeMembers())
            {
                FindConvertersInType(nested, mappings);
            }
        }

        // Support methods for checking supported types
        private static bool IsSupportedType(ITypeSymbol type, Compilation compilation, ConverterRegistry registry)
        {
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                type is INamedTypeSymbol { TypeArguments.Length: > 0 } namedRaw)
            {
                return IsSupportedType(namedRaw.TypeArguments[0], compilation, registry);
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_String:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                    return true;
            }

            var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (typeName == "global::System.Guid" || typeName == "System.Guid" ||
                typeName == "global::System.DateTime" || typeName == "System.DateTime" ||
                typeName == "global::System.TimeSpan" || typeName == "System.TimeSpan" ||
                typeName == "global::System.DateTimeOffset" || typeName == "System.DateTimeOffset" ||
                typeName == "global::System.Version" || typeName == "System.Version" ||
                typeName == "global::System.Uri" || typeName == "System.Uri")
            {
                return true;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                return true;
            }

            if (type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "GenJson.GenJsonAttribute"))
            {
                return true;
            }

            var targetTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (registry.GetConverterForType(targetTypeName) != null)
            {
                return true;
            }

            if (TryGetDictionaryTypes(type, out var keyType, out var valueType))
            {
                return IsSupportedType(keyType!, compilation, registry) && IsSupportedType(valueType!, compilation, registry);
            }

            var resolvedElementType = GetEnumerableElementType(type);
            if (resolvedElementType != null)
            {
                return IsSupportedType(resolvedElementType, compilation, registry);
            }

            return false;
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
                   type.OriginalDefinition.Name == "IDictionary" &&
                   type.OriginalDefinition.Arity == 2;
        }

        private static bool IsCollection(INamedTypeSymbol type)
        {
            return type.OriginalDefinition.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic" &&
                   type.OriginalDefinition.Name == "ICollection" &&
                   type.OriginalDefinition.Arity == 1;
        }

        private static ITypeSymbol? GetEnumerableElementType(ITypeSymbol type)
        {
            switch (type)
            {
                case IArrayTypeSymbol arrayType:
                    return arrayType.ElementType;

                case INamedTypeSymbol namedTypeSym:
                    {
                        if (IsCollection(namedTypeSym))
                        {
                            return namedTypeSym.TypeArguments[0];
                        }

                        var collInterface = namedTypeSym.AllInterfaces.FirstOrDefault(IsCollection);
                        if (collInterface != null)
                        {
                            return collInterface.TypeArguments[0];
                        }

                        break;
                    }
            }

            return null;
        }
    }

    internal struct ConverterMapping
    {
        public string TargetTypeName { get; }
        public string ConverterTypeName { get; }

        public ConverterMapping(string targetTypeName, string converterTypeName)
        {
            TargetTypeName = targetTypeName;
            ConverterTypeName = converterTypeName;
        }
    }

    internal class ConverterRegistry
    {
        private readonly Dictionary<string, string> _map;

        public ConverterRegistry(List<ConverterMapping> mappings)
        {
            _map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var mapping in mappings)
            {
                _map[mapping.TargetTypeName] = mapping.ConverterTypeName;
            }
        }

        public string? GetConverterForType(string targetTypeName)
        {
            return _map.TryGetValue(targetTypeName, out var converterTypeName) ? converterTypeName : null;
        }
    }
}
