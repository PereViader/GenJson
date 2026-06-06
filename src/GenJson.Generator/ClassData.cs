namespace GenJson.Generator;

public record ClassData(
    string ClassName,
    string Namespace,
    EquatableList<PropertyData> ConstructorArgs,
    EquatableList<PropertyData> InitProperties,
    EquatableList<PropertyData> AllProperties,
    string Keyword,
    bool IsAbstract,
    bool HasGenJsonBase,
    bool IsNullableContext,
    string? PolymorphicDiscriminatorProp,
    EquatableList<DerivedTypeData> DerivedTypes);