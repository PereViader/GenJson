using System;
using System.Collections.Generic;

#pragma warning disable CS8618
#pragma warning disable CS8601
namespace GenJson.Tests;

public interface ITestGenJson
{
    string ToJson(bool useCountOptimization = false);
    int CalculateJsonSize(bool useCountOptimization = false);
    byte[] ToJsonUtf8(bool useCountOptimization = false);
    int CalculateJsonSizeUtf8(bool useCountOptimization = false);
}

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

[GenJson]
public partial class UriClass
{
    public Uri Present { get; init; }
    public Uri? NullablePresent { get; init; }
    public Uri? NullableNull { get; init; }
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
    [GenJsonEnumAsText] public IntEnum PresentText { get; init; }
    [GenJsonEnumAsNumber] public IntEnum NullablePresentNumber { get; init; }
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
    public ICollection<int> EnumerablePresent { get; init; }
    public int[] ArrayPresent { get; init; }
    public List<int> ListPresent { get; init; }

    public ICollection<int>? NullableEnumerablePresent { get; init; }
    public int[]? NullableArrayPresent { get; init; }
    public List<int>? NullableListPresent { get; init; }

    public ICollection<int>? NullableEnumerableNull { get; init; }
    public int[]? NullableArrayNull { get; init; }
    public List<int>? NullableListNull { get; init; }
}

[GenJson]
public partial class EnumerableStringClass
{
    public ICollection<string> EnumerablePresent { get; init; }
    public string[] ArrayPresent { get; init; }
    public List<string> ListPresent { get; init; }

    public ICollection<string>? NullableEnumerablePresent { get; init; }
    public string[]? NullableArrayPresent { get; init; }
    public List<string>? NullableListPresent { get; init; }

    public ICollection<string>? NullableEnumerableNull { get; init; }
    public string[]? NullableArrayNull { get; init; }
    public List<string>? NullableListNull { get; init; }
}

[GenJson]
public partial class EnumerableParentClass
{
    public ICollection<EmptyClass> EnumerablePresent { get; init; }
    public EmptyClass[] ArrayPresent { get; init; }
    public List<EmptyClass> ListPresent { get; init; }

    public ICollection<EmptyClass>? NullableEnumerablePresent { get; init; }
    public EmptyClass[]? NullableArrayPresent { get; init; }
    public List<EmptyClass>? NullableListPresent { get; init; }

    public ICollection<EmptyClass>? NullableEnumerableNull { get; init; }
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
    public IDictionary<int, int> PresentIntInt { get; init; }
    public IDictionary<int, string> PresentIntString { get; init; }
    public IDictionary<string, int> PresentStringInt { get; init; }
    public IDictionary<int, ICollection<int>> PresentIntEnumerableInt { get; init; }
    public IDictionary<int, EmptyClass> PresentDictionaryIntEmptyClasses { get; init; }
    public IDictionary<int, int>? NullableDictionaryIntIntNull { get; init; }
}

[GenJson]
public partial class NestedDictionaryClass
{
    public IDictionary<int, IDictionary<int, EmptyClass>> Present { get; init; }
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

[GenJsonEnumFallback(Unknown)]
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

[GenJson]
public partial class BaseScenario
{
    public int BaseProp { get; set; }
}

[GenJson]
public partial class DerivedScenario : BaseScenario
{
    public int DerivedProp { get; set; }
}

// ── STJ comparison edge-case types ────────────────────────────────────────
// These cover a wide range of serialization edge cases for STJ comparison tests.
// By default, the generator now omits $Count properties.

[GenJson]
public partial class EdgeStringClass : ITestGenJson
{
    public string Plain { get; init; } = "";
    public string WithNewline { get; init; } = "";
    public string WithTab { get; init; } = "";
    public string WithCarriageReturn { get; init; } = "";
    public string WithBackslash { get; init; } = "";
    public string WithQuote { get; init; } = "";
    public string WithUnicode { get; init; } = "";
    public string Empty { get; init; } = "";
    public string? Nullable { get; init; }
}

[GenJson]
public partial class EdgeNumberClass : ITestGenJson
{
    public int IntMin { get; init; }
    public int IntMax { get; init; }
    public long LongMin { get; init; }
    public long LongMax { get; init; }
    public ulong ULongMax { get; init; }
    public uint UIntMin { get; init; }
    public uint UIntMax { get; init; }
    public double DoubleZero { get; init; }
    public double DoubleNegative { get; init; }
    public double DoubleLarge { get; init; }
    public float FloatSmall { get; init; }
    public decimal DecimalPrecise { get; init; }
    public int? NullableInt { get; init; }
    public double? NullableDouble { get; init; }

    public double DoubleMin { get; init; }
    public double DoubleMax { get; init; }
    public double DoubleEpsilon { get; init; }
    public double DoubleNaN { get; init; }
    public double DoublePositiveInfinity { get; init; }
    public double DoubleNegativeInfinity { get; init; }

    public float FloatMin { get; init; }
    public float FloatMax { get; init; }
    public float FloatEpsilon { get; init; }
    public float FloatNaN { get; init; }
    public float FloatPositiveInfinity { get; init; }
    public float FloatNegativeInfinity { get; init; }
}

[GenJson]
public partial class EdgeCollectionClass : ITestGenJson
{
    public List<string> StringList { get; init; } = new();
    public List<int> IntList { get; init; } = new();
    public List<int>? NullList { get; init; }
    public List<int> EmptyList { get; init; } = new();
    public int[] IntArray { get; init; } = Array.Empty<int>();
    public Dictionary<string, int> Dict { get; init; } = new();
    public Dictionary<string, string> StringDict { get; init; } = new();
    public List<List<int>> NestedIntList { get; init; } = new();
}

[GenJson]
public partial class EdgeNestedClass : ITestGenJson
{
    public EdgeStringClass? Child { get; init; }
    public EdgeStringClass? NullChild { get; init; }
    public List<EdgeStringClass> Children { get; init; } = new();
}

[GenJson]
public partial class EdgeBoolClass : ITestGenJson
{
    public bool True { get; init; }
    public bool False { get; init; }
    public bool? NullableBool { get; init; }
    public bool? NullBool { get; init; }
}

[GenJson]
public partial class EdgeControlCharClass : ITestGenJson
{
    public string WithNull { get; init; } = "";
    public string WithCtrl1 { get; init; } = "";
    public string WithCtrl1F { get; init; } = "";
    public string WithEmoji { get; init; } = "";
    public string WithMixed { get; init; } = "";
}

[GenJson]
public partial class EdgeDateGuidCharClass : ITestGenJson
{
    public DateTime DatePresent { get; init; }
    public DateTime? DateNull { get; init; }
    public DateTimeOffset DateOffsetPresent { get; init; }
    public DateTimeOffset? DateOffsetNull { get; init; }
    public TimeSpan TimeSpanPresent { get; init; }
    public TimeSpan? TimeSpanNull { get; init; }
    public Guid GuidPresent { get; init; }
    public Guid? GuidNull { get; init; }
    public Version VersionPresent { get; init; } = new("1.0.0");
    public Version? VersionNull { get; init; }
    public char CharPresent { get; init; }
    public char? CharNull { get; init; }
    public char CharSpecial { get; init; }
}

[GenJson]
public partial class EdgeDateTimeKindClass : ITestGenJson
{
    public DateTime Utc { get; init; }
    public DateTime Local { get; init; }
    public DateTime Unspecified { get; init; }
}

[GenJson]
public partial class EdgeDateTimeOffsetExtraClass : ITestGenJson
{
    public DateTimeOffset ZeroOffset { get; init; }
    public DateTimeOffset NegativeOffset { get; init; }
    public DateTimeOffset LargePositiveOffset { get; init; }
}

[GenJson]
public partial class EdgeTimeSpanClass : ITestGenJson
{
    public TimeSpan Zero { get; init; }
    public TimeSpan Negative { get; init; }
    public TimeSpan SubSecond { get; init; }
    public TimeSpan MaxValue { get; init; }
    public TimeSpan MinValue { get; init; }
}

[GenJson]
public partial class EdgeGuidClass : ITestGenJson
{
    public Guid Empty { get; init; }
    public Guid AllFs { get; init; }
}

[GenJson]
public partial class EdgeVersionClass : ITestGenJson
{
    public Version TwoComponent { get; init; } = new(1, 0);
    public Version ThreeComponent { get; init; } = new(1, 2, 3);
    public Version FourComponent { get; init; } = new(1, 2, 3, 4);
}

[GenJson]
public partial class EdgeUriClass : ITestGenJson
{
    public Uri HttpUri { get; init; } = new("http://example.com");
    public Uri HttpsUri { get; init; } = new("https://example.com/path?q=1&r=2");
    public Uri? NullableUri { get; init; }
    public Uri UriWithSpecialChars { get; init; } = new("https://example.com/path with spaces", UriKind.Absolute);
}

[GenJson]
public partial class EdgeDecimalClass : ITestGenJson
{
    public decimal Zero { get; init; }
    public decimal One { get; init; }
    public decimal MinusOne { get; init; }
    public decimal MaxValue { get; init; }
    public decimal MinValue { get; init; }
    public decimal Precise { get; init; }
}

[GenJson]
public partial class EdgeCharEdgeClass : ITestGenJson
{
    public char NullChar { get; init; }      // '\0'
    public char MaxChar { get; init; }       // '\uffff'
    public char BackslashChar { get; init; } // '\\'
    public char QuoteChar { get; init; }     // '"'
    public char DelChar { get; init; }       // '\x7f' — boundary between control and printable
    public char SpaceChar { get; init; }     // ' ' — lowest printable ASCII
}

[GenJson]
public partial class EdgeByteShortClass : ITestGenJson
{
    public byte ByteMin { get; init; }
    public byte ByteMax { get; init; }
    public sbyte SByteMin { get; init; }
    public sbyte SByteMax { get; init; }
    public short ShortMin { get; init; }
    public short ShortMax { get; init; }
    public ushort UShortMin { get; init; }
    public ushort UShortMax { get; init; }
}

[GenJson]
public partial class EdgeEnumClass : ITestGenJson
{
    public IntEnum EnumNumber { get; init; }
    [GenJsonEnumAsText] public IntEnum EnumText { get; init; }
    public ByteEnum ByteEnum { get; init; }
}

public class CustomDictionary<K, V> : IDictionary<K, V> where K : notnull
{
    private readonly Dictionary<K, V> _dict;

    public CustomDictionary()
    {
        _dict = new Dictionary<K, V>();
    }

    public CustomDictionary(int capacity)
    {
        _dict = new Dictionary<K, V>(capacity);
    }

    public void Add(K key, V value) => _dict.Add(key, value);
    public bool ContainsKey(K key) => _dict.ContainsKey(key);
    public bool Remove(K key) => _dict.Remove(key);
    public bool TryGetValue(K key, out V value) => _dict.TryGetValue(key, out value);

    public V this[K key]
    {
        get => _dict[key];
        set => _dict[key] = value;
    }

    public ICollection<K> Keys => _dict.Keys;
    public ICollection<V> Values => _dict.Values;

    public int Count => _dict.Count;
    public bool IsReadOnly => false;

    public void Add(KeyValuePair<K, V> item) => ((ICollection<KeyValuePair<K, V>>)_dict).Add(item);
    public void Clear() => _dict.Clear();
    public bool Contains(KeyValuePair<K, V> item) => ((ICollection<KeyValuePair<K, V>>)_dict).Contains(item);
    public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) => ((ICollection<KeyValuePair<K, V>>)_dict).CopyTo(array, arrayIndex);
    public bool Remove(KeyValuePair<K, V> item) => ((ICollection<KeyValuePair<K, V>>)_dict).Remove(item);

    public IEnumerator<KeyValuePair<K, V>> GetEnumerator() => _dict.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _dict.GetEnumerator();
}

public class CustomCollection<T> : ICollection<T>
{
    private readonly List<T> _list;

    public CustomCollection()
    {
        _list = new List<T>();
    }

    public void Add(T item) => _list.Add(item);

    public int Count => _list.Count;
    public bool IsReadOnly => false;
    public void Clear() => _list.Clear();
    public bool Contains(T item) => _list.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
    public bool Remove(T item) => _list.Remove(item);

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _list.GetEnumerator();
}

[GenJson]
public partial class CustomCollectionClass
{
    public CustomDictionary<string, int> CustomDict { get; set; }
    public CustomCollection<int> CustomColl { get; set; }
}

[GenJson]
public partial class FieldClass
{
    public string Present;
    public string? NullablePresent;
    public string? NullableNull;
    public readonly int IgnoredReadonlyField = 42;
}

[GenJson]
public partial class CtorFieldClass
{
    public readonly string Name;
    public string Role { get; init; }

    public CtorFieldClass(string name, string role)
    {
        Name = name;
        Role = role;
    }
}

[GenJson]
public partial class DecoratedFieldClass
{
    [GenJsonPropertyName("custom_name")]
    public string PlainField;

    [GenJsonIgnore]
    public string IgnoredField;
}
