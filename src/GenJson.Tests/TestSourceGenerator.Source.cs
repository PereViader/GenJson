#pragma warning disable CS8618
namespace GenJson.Tests;

[GenJson]
public class EmptyClass
{
    public int? Value { get; init; }
}

[GenJson]
public class StringClass
{
    public string Present { get; init; }
    public string? NullablePresent {get; init;}
    public string? NullableNull {get; init;}
}

[GenJson]
public class IntClass
{
    public int Present { get; init; }
    public int? NullablePresent {get; init;}
    public int? NullableNull {get; init;}
}

[GenJson]
public class ParentClass
{
    public EmptyClass Present { get; init; }
    public EmptyClass? NullablePresent { get; init; }
    public EmptyClass? NullableNull { get; init; }
}