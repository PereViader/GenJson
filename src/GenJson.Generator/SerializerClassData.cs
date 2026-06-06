namespace GenJson.Generator;

public record SerializerClassData(
    string ClassName,
    string Namespace,
    string Keyword,
    RootTypeInfo? RootType);