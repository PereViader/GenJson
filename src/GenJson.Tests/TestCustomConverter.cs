using System;
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

    public static int FromJson(ReadOnlySpan<char> span, ref int index)
    {
        if (span[index] != '"') throw new Exception("Expected quote");
        index++;
        if (span[index] != 'X') throw new Exception("Expected X");
        index++;

        int start = index;
        while (char.IsDigit(span[index])) index++;
        int val = int.Parse(span.Slice(start, index - start));

        if (span[index] != 'X') throw new Exception("Expected X");
        index++;
        if (span[index] != '"') throw new Exception("Expected quote");
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

    public static int FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
    {
        if (span[index] != (byte)'"') throw new Exception("Expected quote");
        index++;
        if (span[index] != (byte)'X') throw new Exception("Expected X");
        index++;

        int start = index;
        while (span[index] >= (byte)'0' && span[index] <= (byte)'9') index++;
        System.Buffers.Text.Utf8Parser.TryParse(span.Slice(start, index - start), out int val, out var _);

        if (span[index] != (byte)'X') throw new Exception("Expected X");
        index++;
        if (span[index] != (byte)'"') throw new Exception("Expected quote");
        index++;

        return val;
    }
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
    public static MyStruct FromJson(ReadOnlySpan<char> span, ref int index)
    {
        index += 2; // "A
        int val = span[index++] - '0';
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
    public static MyStruct FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
    {
        index += 2;
        int val = span[index++] - '0';
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
    public static MyStruct FromJson(ReadOnlySpan<char> span, ref int index)
    {
        index += 2; // "B
        int val = span[index++] - '0';
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
    public static MyStruct FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
    {
        index += 2;
        int val = span[index++] - '0';
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
