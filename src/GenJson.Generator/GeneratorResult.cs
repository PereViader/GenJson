namespace GenJson.Generator;

public record GeneratorResult(
    ClassData? ClassData,
    EquatableList<DiagnosticInfo> Diagnostics);