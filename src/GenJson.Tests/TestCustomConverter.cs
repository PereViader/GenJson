using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GenJson.Tests;

public static class MyCustomConverter
{
    public static int GetSize(int value)
    {
        return value.ToString().Length + 4; // Add 2 quotes and 2 extra chars
    }

    public static void WriteJson(Span<char> span, ref int index, int value)
    {
        span[index++] = '"';
        span[index++] = 'X';
        value.TryFormat(span.Slice(index), out var written);
        index += written;
        span[index++] = 'X';
        span[index++] = '"';
    }

    public static int? FromJson(ReadOnlySpan<char> span, ref int index)
    {
        if (span[index] != '"') return null;
        index++;
        if (span[index] != 'X') return null;
        index++;

        int start = index;
        while (char.IsDigit(span[index])) index++;
        if (!int.TryParse(span.Slice(start, index - start), out int val)) return null;

        if (span[index] != 'X') return null;
        index++;
        if (span[index] != '"') return null;
        index++;

        return val;
    }

    public static int GetSizeUtf8(int value)
    {
        return GetSize(value);
    }

    public static void WriteJsonUtf8(Span<byte> span, ref int index, int value)
    {
        span[index++] = (byte)'"';
        span[index++] = (byte)'X';
        System.Buffers.Text.Utf8Formatter.TryFormat(value, span.Slice(index), out var written);
        index += written;
        span[index++] = (byte)'X';
        span[index++] = (byte)'"';
    }

    public static int? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
    {
        if (span[index] != (byte)'"') return null;
        index++;
        if (span[index] != (byte)'X') return null;
        index++;

        int start = index;
        while (span[index] >= (byte)'0' && span[index] <= (byte)'9') index++;
        if (!System.Buffers.Text.Utf8Parser.TryParse(span.Slice(start, index - start), out int val, out var _)) return null;

        if (span[index] != (byte)'X') return null;
        index++;
        if (span[index] != (byte)'"') return null;
        index++;

        return val;
    }
}

public static class MyStringConverter
{
    public static int GetSize(string value) => value.Length + 4;
    public static void WriteJson(Span<char> span, ref int index, string value)
    {
        span[index++] = '"';
        span[index++] = '[';
        for (int i = 0; i < value.Length; i++) span[index++] = value[i];
        span[index++] = ']';
        span[index++] = '"';
    }
    public static string? FromJson(ReadOnlySpan<char> span, ref int index)
    {
        if (span[index] != '"' || span[index + 1] != '[') return null;
        index += 2;
        int start = index;
        while (index < span.Length && span[index] != ']') index++;
        if (index >= span.Length || span[index + 1] != '"') return null;
        var val = new string(span.Slice(start, index - start));
        index += 2; // ]"
        return val;
    }

    public static int GetSizeUtf8(string value) => System.Text.Encoding.UTF8.GetByteCount(value) + 4;
    public static void WriteJsonUtf8(Span<byte> span, ref int index, string value)
    {
        span[index++] = (byte)'"';
        span[index++] = (byte)'[';
        int written = System.Text.Encoding.UTF8.GetBytes(value, span.Slice(index));
        index += written;
        span[index++] = (byte)']';
        span[index++] = (byte)'"';
    }
    public static string? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
    {
        if (span[index] != (byte)'"' || span[index + 1] != (byte)'[') return null;
        index += 2;
        int start = index;
        while (index < span.Length && span[index] != (byte)']') index++;
        if (index >= span.Length || span[index + 1] != (byte)'"') return null;
        var val = System.Text.Encoding.UTF8.GetString(span.Slice(start, index - start));
        index += 2; // ]"
        return val;
    }
}

[GenJson]
public partial class CustomStringConverterClass
{
    [GenJsonConverter(typeof(MyStringConverter))]
    public string Str { get; set; } = "";
}

[GenJson]
public partial class CustomConverterClass
{
    [GenJsonConverter(typeof(MyCustomConverter))]
    public int Value { get; set; }
}

public class TestCustomConverter
{
    [Test]
    public void TestCustomConverterFlow()
    {
        var obj = new CustomConverterClass { Value = 123 };

        // Expected JSON: {"Value":"X123X"}
        // Size: 2 ({}) + 7 ("Value":) + 7 ("X123X") = 16

        var json = obj.ToJson();
        var expected = """{"Value":"X123X"}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = CustomConverterClass.FromJson(json)!;
        Assert.That(obj2, Is.Not.Null);
        Assert.That(obj2.Value, Is.EqualTo(123));

        var utf8Json = obj.ToJsonUtf8();
        var utf8Expected = System.Text.Encoding.UTF8.GetBytes(expected);
        Assert.That(utf8Json, Is.EqualTo(utf8Expected));

        var utf8Size = obj.CalculateJsonSizeUtf8();
        Assert.That(utf8Size, Is.EqualTo(utf8Expected.Length));

        var utf8Obj = CustomConverterClass.FromJsonUtf8(utf8Json)!;
        Assert.That(utf8Obj, Is.Not.Null);
        Assert.That(utf8Obj.Value, Is.EqualTo(123));
    }

    [Test]
    public void TestCustomConverterOnTypeAndOverride()
    {
        var obj = new CustomConverterOnTypeClass
        {
            TypedProp = new MyStruct { Value = 1 },
            OverriddenProp = new MyStruct { Value = 2 }
        };

        var json = obj.ToJson();
        var expected = """{"TypedProp":"A1A","OverriddenProp":"B2B"}""";
        Assert.That(json, Is.EqualTo(expected));

        var parsed = CustomConverterOnTypeClass.FromJson(json)!;
        Assert.That(parsed.TypedProp.Value, Is.EqualTo(1));
        Assert.That(parsed.OverriddenProp.Value, Is.EqualTo(2));

        var utf8Json = obj.ToJsonUtf8();
        var utf8Expected = System.Text.Encoding.UTF8.GetBytes(expected);
        Assert.That(utf8Json, Is.EqualTo(utf8Expected));

        var parsedUtf8 = CustomConverterOnTypeClass.FromJsonUtf8(utf8Json)!;
        Assert.That(parsedUtf8.TypedProp.Value, Is.EqualTo(1));
        Assert.That(parsedUtf8.OverriddenProp.Value, Is.EqualTo(2));
    }

    [Test]
    public void TestCustomConverterFailure()
    {
        // Invalid custom converter format (missing X, invalid layout)
        var invalidJson = """{"Value":"123"}""";
        var parsed = CustomConverterClass.FromJson(invalidJson);
        Assert.That(parsed, Is.Null);

        var invalidUtf8Json = System.Text.Encoding.UTF8.GetBytes(invalidJson);
        var parsedUtf8 = CustomConverterClass.FromJsonUtf8(invalidUtf8Json);
        Assert.That(parsedUtf8, Is.Null);
    }

    [Test]
    public void TestCustomStringConverterFlow()
    {
        var obj = new CustomStringConverterClass { Str = "hello" };
        var json = obj.ToJson();
        Assert.That(json, Is.EqualTo("""{"Str":"[hello]"}"""));

        var parsed = CustomStringConverterClass.FromJson(json)!;
        Assert.That(parsed.Str, Is.EqualTo("hello"));

        var invalidJson = """{"Str":"hello"}"""; // Missing brackets
        var parsedInvalid = CustomStringConverterClass.FromJson(invalidJson);
        Assert.That(parsedInvalid, Is.Null);

        var utf8Json = obj.ToJsonUtf8();
        var parsedUtf8 = CustomStringConverterClass.FromJsonUtf8(utf8Json)!;
        Assert.That(parsedUtf8.Str, Is.EqualTo("hello"));

        var parsedInvalidUtf8 = CustomStringConverterClass.FromJsonUtf8(System.Text.Encoding.UTF8.GetBytes(invalidJson));
        Assert.That(parsedInvalidUtf8, Is.Null);
    }

    [Test]
    public void TestDictionaryWithCustomKey()
    {
        var obj = new DictionaryWithCustomKeyClass
        {
            ResourceAmounts = new Dictionary<ResId, int>
            {
                { new ResId(1), 10 },
                { new ResId(2), 20 }
            }
        };

        var json = obj.ToJson();
        var expected = """{"ResourceAmounts":{"R1":10,"R2":20}}""";
        Assert.That(json, Is.EqualTo(expected));

        var parsed = DictionaryWithCustomKeyClass.FromJson(json);
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed.ResourceAmounts, Is.Not.Null);
        Assert.That(parsed.ResourceAmounts.Count, Is.EqualTo(2));
        Assert.That(parsed.ResourceAmounts[new ResId(1)], Is.EqualTo(10));
        Assert.That(parsed.ResourceAmounts[new ResId(2)], Is.EqualTo(20));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var utf8Json = obj.ToJsonUtf8();
        var utf8Expected = System.Text.Encoding.UTF8.GetBytes(expected);
        Assert.That(utf8Json, Is.EqualTo(utf8Expected));

        var utf8Size = obj.CalculateJsonSizeUtf8();
        Assert.That(utf8Size, Is.EqualTo(utf8Expected.Length));

        var parsedUtf8 = DictionaryWithCustomKeyClass.FromJsonUtf8(utf8Json);
        Assert.That(parsedUtf8, Is.Not.Null);
        Assert.That(parsedUtf8.ResourceAmounts, Is.Not.Null);
        Assert.That(parsedUtf8.ResourceAmounts.Count, Is.EqualTo(2));
        Assert.That(parsedUtf8.ResourceAmounts[new ResId(1)], Is.EqualTo(10));
        Assert.That(parsedUtf8.ResourceAmounts[new ResId(2)], Is.EqualTo(20));
    }
}

public static class StructConverterA
{
    public static int GetSize(MyStruct value) => 5;
    public static void WriteJson(Span<char> span, ref int index, MyStruct value)
    {
        span[index++] = '"';
        span[index++] = 'A';
        span[index++] = (char)('0' + value.Value);
        span[index++] = 'A';
        span[index++] = '"';
    }
    public static MyStruct? FromJson(ReadOnlySpan<char> span, ref int index)
    {
        if (span[index] != '"' || span[index + 1] != 'A') return null;
        index += 2; // "A
        int val = span[index++] - '0';
        if (span[index] != 'A' || span[index + 1] != '"') return null;
        index += 2; // A"
        return new MyStruct { Value = val };
    }
    public static int GetSizeUtf8(MyStruct value) => 5;
    public static void WriteJsonUtf8(Span<byte> span, ref int index, MyStruct value)
    {
        span[index++] = (byte)'"';
        span[index++] = (byte)'A';
        span[index++] = (byte)('0' + value.Value);
        span[index++] = (byte)'A';
        span[index++] = (byte)'"';
    }
    public static MyStruct? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
    {
        if (span[index] != (byte)'"' || span[index + 1] != (byte)'A') return null;
        index += 2;
        int val = span[index++] - '0';
        if (span[index] != (byte)'A' || span[index + 1] != (byte)'"') return null;
        index += 2;
        return new MyStruct { Value = val };
    }
}

public static class StructConverterB
{
    public static int GetSize(MyStruct value) => 5;
    public static void WriteJson(Span<char> span, ref int index, MyStruct value)
    {
        span[index++] = '"';
        span[index++] = 'B';
        span[index++] = (char)('0' + value.Value);
        span[index++] = 'B';
        span[index++] = '"';
    }
    public static MyStruct? FromJson(ReadOnlySpan<char> span, ref int index)
    {
        if (span[index] != '"' || span[index + 1] != 'B') return null;
        index += 2; // "B
        int val = span[index++] - '0';
        if (span[index] != 'B' || span[index + 1] != '"') return null;
        index += 2; // B"
        return new MyStruct { Value = val };
    }
    public static int GetSizeUtf8(MyStruct value) => 5;
    public static void WriteJsonUtf8(Span<byte> span, ref int index, MyStruct value)
    {
        span[index++] = (byte)'"';
        span[index++] = (byte)'B';
        span[index++] = (byte)('0' + value.Value);
        span[index++] = (byte)'B';
        span[index++] = (byte)'"';
    }
    public static MyStruct? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
    {
        if (span[index] != (byte)'"' || span[index + 1] != (byte)'B') return null;
        index += 2;
        int val = span[index++] - '0';
        if (span[index] != (byte)'B' || span[index + 1] != (byte)'"') return null;
        index += 2;
        return new MyStruct { Value = val };
    }
}

[GenJsonConverter(typeof(StructConverterA))]
public struct MyStruct
{
    public int Value { get; set; }
}

[GenJson]
public partial class CustomConverterOnTypeClass
{
    public MyStruct TypedProp { get; set; }

    [GenJsonConverter(typeof(StructConverterB))]
    public MyStruct OverriddenProp { get; set; }
}

[GenJsonConverter(typeof(ResIdConverter))]
public struct ResId : IEquatable<ResId>
{
    public int Value { get; }
    public ResId(int value) => Value = value;
    public bool Equals(ResId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is ResId other && Equals(other);
    public override int GetHashCode() => Value;
}

public static class ResIdConverter
{
    public static int GetSize(ResId value) => value.Value.ToString().Length + 3; // e.g. "R1" -> quotes + length
    
    public static void WriteJson(Span<char> span, ref int index, ResId value)
    {
        span[index++] = '"';
        span[index++] = 'R';
        value.Value.TryFormat(span.Slice(index), out var written);
        index += written;
        span[index++] = '"';
    }

    public static ResId? FromJson(ReadOnlySpan<char> span, ref int index)
    {
        if (span[index] != '"' || span[index + 1] != 'R') return null;
        index += 2;
        int start = index;
        while (char.IsDigit(span[index])) index++;
        if (!int.TryParse(span.Slice(start, index - start), out int val)) return null;
        if (span[index] != '"') return null;
        index++;
        return new ResId(val);
    }

    public static int GetSizeUtf8(ResId value) => GetSize(value);
    
    public static void WriteJsonUtf8(Span<byte> span, ref int index, ResId value)
    {
        span[index++] = (byte)'"';
        span[index++] = (byte)'R';
        System.Buffers.Text.Utf8Formatter.TryFormat(value.Value, span.Slice(index), out var written);
        index += written;
        span[index++] = (byte)'"';
    }

    public static ResId? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
    {
        if (span[index] != (byte)'"' || span[index + 1] != (byte)'R') return null;
        index += 2;
        int start = index;
        while (span[index] >= (byte)'0' && span[index] <= (byte)'9') index++;
        if (!System.Buffers.Text.Utf8Parser.TryParse(span.Slice(start, index - start), out int val, out var _)) return null;
        if (span[index] != (byte)'"') return null;
        index++;
        return new ResId(val);
    }
}

[GenJson]
public partial class DictionaryWithCustomKeyClass
{
    public IDictionary<ResId, int> ResourceAmounts { get; set; } = new Dictionary<ResId, int>();
}
