namespace GenJson.Generator;

public record PropertyData(string Name, string JsonName, string TypeName, bool IsNullable, bool IsValueType, GenJsonDataType Type, string? ConstructorParamName = null);