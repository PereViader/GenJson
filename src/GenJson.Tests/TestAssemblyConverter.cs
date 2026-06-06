using System;
using NUnit.Framework;
using GenJson;
using GenJson.Tests;

[assembly: GenJsonConverter(typeof(ExternalStructAssemblyConverter))]

namespace GenJson.Tests
{
    public struct ExternalStruct
    {
        public int Value { get; set; }
    }

    public struct PriorityStruct
    {
        public int Value { get; set; }
    }

    [GenJsonConverter(typeof(ExternalStruct))]
    public static class ExternalStructAssemblyConverter
    {
        public static int GetSize(ExternalStruct value) => 5;
        public static void WriteJson(Span<char> span, ref int index, ExternalStruct value)
        {
            span[index++] = '"';
            span[index++] = 'E';
            span[index++] = (char)('0' + value.Value);
            span[index++] = 'E';
            span[index++] = '"';
        }
        public static ExternalStruct? FromJson(ReadOnlySpan<char> span, ref int index)
        {
            if (span[index] != '"' || span[index + 1] != 'E') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != 'E' || span[index + 1] != '"') return null;
            index += 2;
            return new ExternalStruct { Value = val };
        }

        public static int GetSizeUtf8(ExternalStruct value) => 5;
        public static void WriteJsonUtf8(Span<byte> span, ref int index, ExternalStruct value)
        {
            span[index++] = (byte)'"';
            span[index++] = (byte)'E';
            span[index++] = (byte)('0' + value.Value);
            span[index++] = (byte)'E';
            span[index++] = (byte)'"';
        }
        public static ExternalStruct? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
        {
            if (span[index] != (byte)'"' || span[index + 1] != (byte)'E') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != (byte)'E' || span[index + 1] != (byte)'"') return null;
            index += 2;
            return new ExternalStruct { Value = val };
        }
    }

    public static class ExternalStructMemberConverter
    {
        public static int GetSize(ExternalStruct value) => 5;
        public static void WriteJson(Span<char> span, ref int index, ExternalStruct value)
        {
            span[index++] = '"';
            span[index++] = 'M';
            span[index++] = (char)('0' + value.Value);
            span[index++] = 'M';
            span[index++] = '"';
        }
        public static ExternalStruct? FromJson(ReadOnlySpan<char> span, ref int index)
        {
            if (span[index] != '"' || span[index + 1] != 'M') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != 'M' || span[index + 1] != '"') return null;
            index += 2;
            return new ExternalStruct { Value = val };
        }

        public static int GetSizeUtf8(ExternalStruct value) => 5;
        public static void WriteJsonUtf8(Span<byte> span, ref int index, ExternalStruct value)
        {
            span[index++] = (byte)'"';
            span[index++] = (byte)'M';
            span[index++] = (byte)('0' + value.Value);
            span[index++] = (byte)'M';
            span[index++] = (byte)'"';
        }
        public static ExternalStruct? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
        {
            if (span[index] != (byte)'"' || span[index + 1] != (byte)'M') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != (byte)'M' || span[index + 1] != (byte)'"') return null;
            index += 2;
            return new ExternalStruct { Value = val };
        }
    }

    public static class PriorityStructAssemblyConverter
    {
        public static int GetSize(PriorityStruct value) => 5;
        public static void WriteJson(Span<char> span, ref int index, PriorityStruct value)
        {
            span[index++] = '"';
            span[index++] = 'A';
            span[index++] = (char)('0' + value.Value);
            span[index++] = 'A';
            span[index++] = '"';
        }
        public static PriorityStruct? FromJson(ReadOnlySpan<char> span, ref int index)
        {
            if (span[index] != '"' || span[index + 1] != 'A') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != 'A' || span[index + 1] != '"') return null;
            index += 2;
            return new PriorityStruct { Value = val };
        }

        public static int GetSizeUtf8(PriorityStruct value) => 5;
        public static void WriteJsonUtf8(Span<byte> span, ref int index, PriorityStruct value)
        {
            span[index++] = (byte)'"';
            span[index++] = (byte)'A';
            span[index++] = (byte)('0' + value.Value);
            span[index++] = (byte)'A';
            span[index++] = (byte)'"';
        }
        public static PriorityStruct? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
        {
            if (span[index] != (byte)'"' || span[index + 1] != (byte)'A') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != (byte)'A' || span[index + 1] != (byte)'"') return null;
            index += 2;
            return new PriorityStruct { Value = val };
        }
    }

    [GenJsonConverter(typeof(PriorityStruct))]
    public static class PriorityStructTypeConverter
    {
        public static int GetSize(PriorityStruct value) => 5;
        public static void WriteJson(Span<char> span, ref int index, PriorityStruct value)
        {
            span[index++] = '"';
            span[index++] = 'T';
            span[index++] = (char)('0' + value.Value);
            span[index++] = 'T';
            span[index++] = '"';
        }
        public static PriorityStruct? FromJson(ReadOnlySpan<char> span, ref int index)
        {
            if (span[index] != '"' || span[index + 1] != 'T') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != 'T' || span[index + 1] != '"') return null;
            index += 2;
            return new PriorityStruct { Value = val };
        }

        public static int GetSizeUtf8(PriorityStruct value) => 5;
        public static void WriteJsonUtf8(Span<byte> span, ref int index, PriorityStruct value)
        {
            span[index++] = (byte)'"';
            span[index++] = (byte)'T';
            span[index++] = (byte)('0' + value.Value);
            span[index++] = (byte)'T';
            span[index++] = (byte)'"';
        }
        public static PriorityStruct? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
        {
            if (span[index] != (byte)'"' || span[index + 1] != (byte)'T') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != (byte)'T' || span[index + 1] != (byte)'"') return null;
            index += 2;
            return new PriorityStruct { Value = val };
        }
    }

    public static class PriorityStructMemberConverter
    {
        public static int GetSize(PriorityStruct value) => 5;
        public static void WriteJson(Span<char> span, ref int index, PriorityStruct value)
        {
            span[index++] = '"';
            span[index++] = 'M';
            span[index++] = (char)('0' + value.Value);
            span[index++] = 'M';
            span[index++] = '"';
        }
        public static PriorityStruct? FromJson(ReadOnlySpan<char> span, ref int index)
        {
            if (span[index] != '"' || span[index + 1] != 'M') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != 'M' || span[index + 1] != '"') return null;
            index += 2;
            return new PriorityStruct { Value = val };
        }

        public static int GetSizeUtf8(PriorityStruct value) => 5;
        public static void WriteJsonUtf8(Span<byte> span, ref int index, PriorityStruct value)
        {
            span[index++] = (byte)'"';
            span[index++] = (byte)'M';
            span[index++] = (byte)('0' + value.Value);
            span[index++] = (byte)'M';
            span[index++] = (byte)'"';
        }
        public static PriorityStruct? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
        {
            if (span[index] != (byte)'"' || span[index + 1] != (byte)'M') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != (byte)'M' || span[index + 1] != (byte)'"') return null;
            index += 2;
            return new PriorityStruct { Value = val };
        }
    }

    [GenJson]
    public partial class ConverterPriorityTestClass
    {
        public ExternalStruct ExternalVal { get; set; }

        [GenJsonConverter(typeof(ExternalStructMemberConverter))]
        public ExternalStruct ExternalOverridden { get; set; }

        public PriorityStruct PriorityVal { get; set; }

        [GenJsonConverter(typeof(PriorityStructMemberConverter))]
        public PriorityStruct PriorityOverridden { get; set; }
    }

    [TestFixture]
    public class TestAssemblyConverter
    {
        [Test]
        public void TestPriorityResolution()
        {
            var obj = new ConverterPriorityTestClass
            {
                ExternalVal = new ExternalStruct { Value = 1 },
                ExternalOverridden = new ExternalStruct { Value = 2 },
                PriorityVal = new PriorityStruct { Value = 3 },
                PriorityOverridden = new PriorityStruct { Value = 4 }
            };

            // Expected formats:
            // ExternalVal: "E1E" (assembly converter)
            // ExternalOverridden: "M2M" (member converter overrides assembly converter)
            // PriorityVal: "T3T" (type converter overrides assembly converter)
            // PriorityOverridden: "M4M" (member converter overrides both type and assembly converter)

            var expected = """{"ExternalVal":"E1E","ExternalOverridden":"M2M","PriorityVal":"T3T","PriorityOverridden":"M4M"}""";
            var json = obj.ToJson();
            Assert.That(json, Is.EqualTo(expected));

            var parsed = ConverterPriorityTestClass.FromJson(json);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed!.ExternalVal.Value, Is.EqualTo(1));
            Assert.That(parsed.ExternalOverridden.Value, Is.EqualTo(2));
            Assert.That(parsed.PriorityVal.Value, Is.EqualTo(3));
            Assert.That(parsed.PriorityOverridden.Value, Is.EqualTo(4));

            // Test UTF8 path
            var utf8Json = obj.ToJsonUtf8();
            var parsedUtf8 = ConverterPriorityTestClass.FromJsonUtf8(utf8Json);
            Assert.That(parsedUtf8, Is.Not.Null);
            Assert.That(parsedUtf8!.ExternalVal.Value, Is.EqualTo(1));
            Assert.That(parsedUtf8.ExternalOverridden.Value, Is.EqualTo(2));
            Assert.That(parsedUtf8.PriorityVal.Value, Is.EqualTo(3));
            Assert.That(parsedUtf8.PriorityOverridden.Value, Is.EqualTo(4));
        }
    }
}
