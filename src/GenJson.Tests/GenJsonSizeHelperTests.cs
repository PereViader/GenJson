using NUnit.Framework;
using System;

namespace GenJson.Tests
{
    [TestFixture]
    public class GenJsonSizeHelperTests
    {
        [Test]
        public void GetSize_Int_ReturnsCorrectSize()
        {
            Assert.That(GenJsonSizeHelper.GetSize(0), Is.EqualTo(1));
            Assert.That(GenJsonSizeHelper.GetSize(5), Is.EqualTo(1));
            Assert.That(GenJsonSizeHelper.GetSize(10), Is.EqualTo(2));
            Assert.That(GenJsonSizeHelper.GetSize(100), Is.EqualTo(3));
            Assert.That(GenJsonSizeHelper.GetSize(-5), Is.EqualTo(2)); // -5 -> 1 + size(5) = 2
        }

        [Test]
        public void GetSize_String_ReturnsCorrectSize()
        {
            Assert.That(GenJsonSizeHelper.GetSize(""), Is.EqualTo(2)); // ""
            Assert.That(GenJsonSizeHelper.GetSize("hello"), Is.EqualTo(7)); // "hello" -> 2 quotes + 5 chars = 7
            Assert.That(GenJsonSizeHelper.GetSize("\n"), Is.EqualTo(4)); // "\n" -> 2 quotes + 2 chars (\n) = 4
        }

        [Test]
        public void GetSize_Bool_ReturnsCorrectSize()
        {
            Assert.That(GenJsonSizeHelper.GetSize(true), Is.EqualTo(4)); // true
            Assert.That(GenJsonSizeHelper.GetSize(false), Is.EqualTo(5)); // false
        }

        [Test]
        public void GetSize_Double_HandlesSpecialValues()
        {
            // JSON does not standardly support NaN/Infinity, but let's check what GetSize returns
            // It relies on Utf8Formatter or string.Create usually.
            // If it returns a size, it means it would be written.
            Assert.That(GenJsonSizeHelper.GetSize(double.NaN), Is.GreaterThan(0));
            Assert.That(GenJsonSizeHelper.GetSize(double.PositiveInfinity), Is.GreaterThan(0));
            Assert.That(GenJsonSizeHelper.GetSize(double.NegativeInfinity), Is.GreaterThan(0));
        }

        [Test]
        public void GetSize_MinMax_ReturnsCorrectSize()
        {
            Assert.That(GenJsonSizeHelper.GetSize(int.MinValue), Is.EqualTo(11)); // -2147483648
            Assert.That(GenJsonSizeHelper.GetSize(int.MaxValue), Is.EqualTo(10)); // 2147483647
            Assert.That(GenJsonSizeHelper.GetSize(long.MinValue), Is.EqualTo(20)); // -9223372036854775808
            Assert.That(GenJsonSizeHelper.GetSize(long.MaxValue), Is.EqualTo(19)); // 9223372036854775807
        }

        [Test]
        public void GetSize_Char_Escapes()
        {
            Assert.That(GenJsonSizeHelper.GetSize('\n'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSize('\r'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSize('\t'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSize('\\'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSize('\"'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSize('\0'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSize('\x1f'), Is.EqualTo(8)); // \u001f
            Assert.That(GenJsonSizeHelper.GetSize('a'), Is.EqualTo(3));
        }

        [Test]
        public void GetSize_String_Escapes()
        {
            var input = "\n\r\t\\\"\0\u001fa";
            // Quotes: 2
            // \n\r\t\\\"\0 each take 2 -> 6 * 2 = 12
            // \x1f (control) takes 6 (\u001f)
            // 'a' takes 1
            // Total: 2 + 12 + 6 + 1 = 21
            Assert.That(GenJsonSizeHelper.GetSize(input.AsSpan()), Is.EqualTo(21));
        }

        [Test]
        public void GetSizeUtf8_Primitives_ReturnSameAsGetSize()
        {
            Assert.That(GenJsonSizeHelper.GetSizeUtf8((byte)1), Is.EqualTo(GenJsonSizeHelper.GetSize((byte)1)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8((sbyte)1), Is.EqualTo(GenJsonSizeHelper.GetSize((sbyte)1)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8((short)1), Is.EqualTo(GenJsonSizeHelper.GetSize((short)1)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8((ushort)1), Is.EqualTo(GenJsonSizeHelper.GetSize((ushort)1)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(1), Is.EqualTo(GenJsonSizeHelper.GetSize(1)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(1u), Is.EqualTo(GenJsonSizeHelper.GetSize(1u)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(1L), Is.EqualTo(GenJsonSizeHelper.GetSize(1L)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(1ul), Is.EqualTo(GenJsonSizeHelper.GetSize(1ul)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(true), Is.EqualTo(GenJsonSizeHelper.GetSize(true)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(Guid.Empty), Is.EqualTo(GenJsonSizeHelper.GetSize(Guid.Empty)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(1.0), Is.EqualTo(GenJsonSizeHelper.GetSize(1.0)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(1.0f), Is.EqualTo(GenJsonSizeHelper.GetSize(1.0f)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(1.0m), Is.EqualTo(GenJsonSizeHelper.GetSize(1.0m)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(DateTime.MaxValue), Is.EqualTo(GenJsonSizeHelper.GetSize(DateTime.MaxValue)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(DateTimeOffset.MaxValue), Is.EqualTo(GenJsonSizeHelper.GetSize(DateTimeOffset.MaxValue)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(TimeSpan.MaxValue), Is.EqualTo(GenJsonSizeHelper.GetSize(TimeSpan.MaxValue)));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(new Version(1, 0)), Is.EqualTo(GenJsonSizeHelper.GetSize(new Version(1, 0))));
        }

        [Test]
        public void GetSizeUtf8_Char_Escapes()
        {
            Assert.That(GenJsonSizeHelper.GetSizeUtf8('\n'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8('\r'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8('\t'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8('\\'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8('\"'), Is.EqualTo(4));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8('\x1f'), Is.EqualTo(8)); // \u001f
            Assert.That(GenJsonSizeHelper.GetSizeUtf8('a'), Is.EqualTo(3));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8('á'), Is.EqualTo(2 + 2)); // 2 quotes + 2 bytes for 'á'
        }

        [Test]
        public void GetSizeUtf8_String_EscapesAndSurrogates()
        {
            var input = "\n\r\t\\\"\u001faá🚀";
            // Quotes: 2
            // \n\r\t\\\" -> 5 * 2 = 10
            // \x1f -> 6 (\u001f)
            // 'a' -> 1
            // 'á' -> 2
            // '🚀' -> 4 (surrogate pair -> 4 bytes in utf8)
            // Total: 2 + 10 + 6 + 1 + 2 + 4 = 25
            Assert.That(GenJsonSizeHelper.GetSizeUtf8(input.AsSpan()), Is.EqualTo(25));
        }
    }
}
