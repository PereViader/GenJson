using System;
using System.Collections.Generic;

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

[GenJson]
public class EnumerableIntClass
{
    public IEnumerable<int> EnumerablePresent { get; init; }
    public int[] ArrayPresent { get; init; }
    public List<int> ListPresent { get; init; }
    
    public IEnumerable<int>? NullableEnumerablePresent { get; init; }
    public int[]? NullableArrayPresent { get; init; }
    public List<int>? NullableListPresent { get; init; }
    
    public IEnumerable<int>? NullableEnumerableNull { get; init; }
    public int[]? NullableArrayNull { get; init; }
    public List<int>? NullableListNull { get; init; }
}

[GenJson]
public class EnumerableStringClass
{
    public IEnumerable<string> EnumerablePresent { get; init; }
    public string[] ArrayPresent { get; init; }
    public List<string> ListPresent { get; init; }
    
    public IEnumerable<string>? NullableEnumerablePresent { get; init; }
    public string[]? NullableArrayPresent { get; init; }
    public List<string>? NullableListPresent { get; init; }
    
    public IEnumerable<string>? NullableEnumerableNull { get; init; }
    public string[]? NullableArrayNull { get; init; }
    public List<string>? NullableListNull { get; init; }
}

[GenJson]
public class EnumerableParentClass
{
    public IEnumerable<EmptyClass> EnumerablePresent { get; init; }
    public EmptyClass[] ArrayPresent { get; init; }
    public List<EmptyClass> ListPresent { get; init; }
    
    public IEnumerable<EmptyClass>? NullableEnumerablePresent { get; init; }
    public EmptyClass[]? NullableArrayPresent { get; init; }
    public List<EmptyClass>? NullableListPresent { get; init; }
    
    public IEnumerable<EmptyClass>? NullableEnumerableNull { get; init; }
    public EmptyClass[]? NullableArrayNull { get; init; }
    public List<EmptyClass>? NullableListNull { get; init; }
}

[GenJson]
public class NestedEnumerableClass
{
    public EmptyClass[][] EnumerablePresent { get;  init; }
}

[GenJson]
public class DictionaryClass
{
    public IReadOnlyDictionary<int, int> PresentIntInt { get; init; }
    public IReadOnlyDictionary<int, string> PresentIntString { get; init; }
    public IReadOnlyDictionary<string, int> PresentStringInt { get; init; }
    public IReadOnlyDictionary<int, IEnumerable<int>> PresentIntEnumerableInt { get; init; }
    public IReadOnlyDictionary<int, EmptyClass> PresentDictionaryIntEmptyClasses { get; init; }
    public IReadOnlyDictionary<int, int>? NullableDictionaryIntIntNull { get; init; }
}

[GenJson]
public class NestedDictionaryClass
{
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, EmptyClass>> Present { get; init; }
}

[GenJson]
public class PrimitiveClass
{
    public bool Bool { get; init; }
    public int Int { get; init; }
    public uint Uint { get; init; }
    public char Char { get; init; }
    public long Long { get; init; }
    public short Short { get; init; } 
    public byte Byte { get; init; }
    public sbyte SByte { get; init; }
    public float Float { get; init; }
    public double Double { get; init; }
    public decimal Decimal { get; init; }
    public string String { get; init; }
    public DateTime DateTime { get; init; }
    public TimeSpan TimeSpan { get; init; }
    public DateOnly DateOnly { get; init; }
    public TimeOnly TimeOnly { get; init; }
    public DateTimeOffset DateTimeOffset { get; init; }
    public Guid Guid { get; init; }
    public Version Version { get; init; }
}