using System;
using System.Collections.Generic;

#pragma warning disable CS8618
namespace GenJson.Tests;

[GenJson]
public partial class StringClass
{
    public string Present { get; init; }
    public string? NullablePresent { get; init; }
    public string? NullableNull { get; init; }
}

[GenJson]
public partial class IntClass
{
    public int Present { get; init; }
    public int? NullablePresent { get; init; }
    public int? NullableNull { get; init; }
}

[GenJson]
public partial class UIntClass
{
    public uint Present { get; init; }
    public uint? NullablePresent { get; init; }
    public uint? NullableNull { get; init; }
}

[GenJson]
public partial class ULongClass
{
    public ulong Present { get; init; }
    public ulong? NullablePresent { get; init; }
    public ulong? NullableNull { get; init; }
}

[GenJson]
public partial class BoolClass
{
    public bool Present { get; init; }
    public bool? NullablePresent { get; init; }
    public bool? NullableNull { get; init; }
}

[GenJson]
public partial class CharClass
{
    public char Present { get; init; }
    public char? NullablePresent { get; init; }
    public char? NullableNull { get; init; }
}

[GenJson]
public partial class FloatClass
{
    public float Present { get; init; }
    public float? NullablePresent { get; init; }
    public float? NullableNull { get; init; }
}

[GenJson]
public partial class DoubleClass
{
    public double Present { get; init; }
    public double? NullablePresent { get; init; }
    public double? NullableNull { get; init; }
}

[GenJson]
public partial class DecimalClass
{
    public decimal Present { get; init; }
    public decimal? NullablePresent { get; init; }
    public decimal? NullableNull { get; init; }
}

[GenJson]
public partial class DateTimeClass
{
    public DateTime Present { get; init; }
    public DateTime? NullablePresent { get; init; }
    public DateTime? NullableNull { get; init; }
}

[GenJson]
public partial class ByteClass
{
    public byte Present { get; init; }
    public byte? NullablePresent { get; init; }
    public byte? NullableNull { get; init; }
}

[GenJson]
public partial class SByteClass
{
    public sbyte Present { get; init; }
    public sbyte? NullablePresent { get; init; }
    public sbyte? NullableNull { get; init; }
}

[GenJson]
public partial class ShortClass
{
    public short Present { get; init; }
    public short? NullablePresent { get; init; }
    public short? NullableNull { get; init; }
}

[GenJson]
public partial class UShortClass
{
    public ushort Present { get; init; }
    public ushort? NullablePresent { get; init; }
    public ushort? NullableNull { get; init; }
}

[GenJson]
public partial class DateTimeOffsetClass
{
    public DateTimeOffset Present { get; init; }
    public DateTimeOffset? NullablePresent { get; init; }
    public DateTimeOffset? NullableNull { get; init; }
}

[GenJson]
public partial class TimeSpanClass
{
    public TimeSpan Present { get; init; }
    public TimeSpan? NullablePresent { get; init; }
    public TimeSpan? NullableNull { get; init; }
}

[GenJson]
public partial class GuidClass
{
    public Guid Present { get; init; }
    public Guid? NullablePresent { get; init; }
    public Guid? NullableNull { get; init; }
}

[GenJson]
public partial class VersionClass
{
    public Version Present { get; init; }
    public Version? NullablePresent { get; init; }
    public Version? NullableNull { get; init; }
}

public enum IntEnum
{
    One = 1,
    Two = 2,
}

public enum ByteEnum : byte
{
    One = 1,
    Two = 2,
}

[GenJson]
public partial class IntEnumClass
{
    public IntEnum PresentNumber { get; init; }
    [GenJson.Enum.AsText] public IntEnum PresentText { get; init; }
    [GenJson.Enum.AsNumber] public IntEnum NullablePresentNumber { get; init; }
    public IntEnum? NullableNull { get; init; }
}

[GenJson]
public partial class ByteEnumClass
{
    public ByteEnum Present { get; init; }
}

[GenJson]
public partial class EmptyClass
{
    public int? Value { get; init; }
}

[GenJson]
public partial class ParentClass
{
    public EmptyClass Present { get; init; }
    public EmptyClass? NullablePresent { get; init; }
    public EmptyClass? NullableNull { get; init; }
}

[GenJson]
public partial class EnumerableIntClass
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
public partial class EnumerableStringClass
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
public partial class EnumerableParentClass
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
public partial class NestedEnumerableClass
{
    public EmptyClass[][] EnumerablePresent { get; init; }
}

[GenJson]
public partial class DictionaryClass
{
    public IReadOnlyDictionary<int, int> PresentIntInt { get; init; }
    public IReadOnlyDictionary<int, string> PresentIntString { get; init; }
    public IReadOnlyDictionary<string, int> PresentStringInt { get; init; }
    public IReadOnlyDictionary<int, IEnumerable<int>> PresentIntEnumerableInt { get; init; }
    public IReadOnlyDictionary<int, EmptyClass> PresentDictionaryIntEmptyClasses { get; init; }
    public IReadOnlyDictionary<int, int>? NullableDictionaryIntIntNull { get; init; }
}

[GenJson]
public partial class NestedDictionaryClass
{
    public IReadOnlyDictionary<int, IReadOnlyDictionary<int, EmptyClass>> Present { get; init; }
}

[GenJson]
public partial record ParentRecordDefault(EmptyClass Child);

[GenJson]
public partial record class ParentRecordClass(EmptyClass Child);

[GenJson]
public partial record struct ParentRecordStruct(EmptyClass Child);

[GenJson]
public partial class StrictClass
{
    public string Required { get; init; }
    public string? Optional { get; init; }
}

[GenJson]
public partial record StrictRecordReference(string Required, string? Optional);

[GenJson]
public partial record StrictRecordValue(int Required, int? Optional);

[GenJson.Enum.Fallback(Unknown)]
public enum FallbackEnum
{
    Unknown,
    One,
    Two,
}

[GenJson]
public partial class FallbackEnumClass
{
    public FallbackEnum Value { get; init; }
    public FallbackEnum? NullableValue { get; init; }
}

#nullable disable
[GenJson]
public partial class StringNullableDisableClass
{
    public string Present { get; init; }
    public string Null { get; init; }
}

[GenJson]
public partial class DateTimeNullableDisableClass
{
    public DateTime Present { get; init; }
    public DateTime? NullablePresent { get; init; }
    public DateTime? NullableNull { get; init; }
}

[GenJson]
public partial class ByteNullableDisableClass
{
    public byte Present { get; init; }
    public byte? NullablePresent { get; init; }
    public byte? NullableNull { get; init; }
}

[GenJson]
public partial record ParentNullableDisableClass(
    ChildNullableDisableClass Present,
    ChildNullableDisableClass Null
    );

[GenJson]
public partial record ChildNullableDisableClass(int Value);
#nullable restore
