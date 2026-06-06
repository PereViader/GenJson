using Microsoft.CodeAnalysis;

namespace GenJson.Generator;

public record DiagnosticInfo(
    string Id,
    string Title,
    string MessageFormat,
    string Category,
    DiagnosticSeverity Severity,
    Location? Location,
    string MessageArg)
{
    public Diagnostic ToDiagnostic()
    {
        var descriptor = new DiagnosticDescriptor(
            Id, Title, MessageFormat, Category, Severity, isEnabledByDefault: true);
        return Diagnostic.Create(descriptor, Location, MessageArg);
    }
}