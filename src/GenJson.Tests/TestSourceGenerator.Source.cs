using GenJson;

[GenJson]
public class StringClass
{
    public string? Value { get; init; }
    public string? NullableValue { get; init; }
}

[GenJson]
public class IntClass
{
    public int Value { get; init; }
    public int? NullableValue { get; init; }
}