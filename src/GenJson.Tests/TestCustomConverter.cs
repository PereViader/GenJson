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
    }
}
