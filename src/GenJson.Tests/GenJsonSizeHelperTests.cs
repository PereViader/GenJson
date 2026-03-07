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
            Assert.That(GenJsonSizeHelper.GetSize('\0'), Is.EqualTo(8));  // "\u0000" (2 quotes + \u0000 = 6 chars)
            Assert.That(GenJsonSizeHelper.GetSize('\x1f'), Is.EqualTo(8)); // \u001f
            Assert.That(GenJsonSizeHelper.GetSize('a'), Is.EqualTo(3));
        }

        [Test]
        public void GetSize_String_Escapes()
        {
            var input = "\n\r\t\\\"\0\u001fa";
            // Quotes: 2
            // \n\r\t\\\" each take 2 -> 5 * 2 = 10
            // \0 takes 6 (\u0000)
            // \x1f (control) takes 6 (\u001f)
            // 'a' takes 1
            // Total: 2 + 10 + 6 + 6 + 1 = 25
            Assert.That(GenJsonSizeHelper.GetSize(input.AsSpan()), Is.EqualTo(25));
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
        // ── Bug #5: GetSize(char '\0') returns 4 but GenJsonWriter emits 6 chars (\u0000) ────────

        [Test]
        public void GetSize_Char_NullCharMatchesWriterOutput()
        {
            // '\0' must be written as \u0000 (6 chars when quoted: "\u0000").
            // GetSize(char) is used to pre-allocate the quoted serialization buffer.
            // BUG: currently returns 4, but the writer emits 6 characters.
            char c = '\0';
            int reportedSize = GenJsonSizeHelper.GetSize(c);

            Span<char> buffer = stackalloc char[32];
            int index = 0;
            GenJsonWriter.WriteString(buffer, ref index, c.ToString());
            int actualSize = index; // includes surrounding quotes

            Assert.That(reportedSize, Is.EqualTo(actualSize),
                $"GetSize('\\0') returned {reportedSize} but GenJsonWriter wrote {actualSize} chars (Bug #5)");
        }

        [Test]
        public void GetSize_String_NullCharMatchesWriterOutput()
        {
            // Same mismatch for GetSize(ReadOnlySpan<char>) when the string contains '\0'.
            // GetSize(ReadOnlySpan<char>): '\0' => 2, but writer emits 6 bytes for \u0000.
            var input = "\0";
            int reportedSize = GenJsonSizeHelper.GetSize(input.AsSpan());

            Span<char> buffer = stackalloc char[32];
            int index = 0;
            GenJsonWriter.WriteString(buffer, ref index, input);
            int actualSize = index;

            Assert.That(reportedSize, Is.EqualTo(actualSize),
                $"GetSize span with '\\0' returned {reportedSize} but GenJsonWriter wrote {actualSize} chars (Bug #5)");
        }

        // ── Bug #6: GetSize(char '\0') vs GetSizeUtf8(char '\0') inconsistency ─────────────────

        [Test]
        public void GetSize_vs_GetSizeUtf8_NullChar_Consistent()
        {
            // '\0' is a control character. The UTF-8 writer also emits \u0000 (8 bytes when quoted).
            // GetSizeUtf8('\0') should correctly return 8 (char.IsControl('\0') => true => 8).
            // GetSize('\0') incorrectly returns 4 — they are inconsistent.
            int charSize = GenJsonSizeHelper.GetSize('\0');
            int utf8Size = GenJsonSizeHelper.GetSizeUtf8('\0');

            // After fixing Bug #5, charSize should be 6 to match the writer.
            // This test documents the current inconsistency.
            Assert.That(utf8Size, Is.EqualTo(8), "GetSizeUtf8('\\0') should return 8 (quoted \\u0000 in UTF-8)");
            Assert.That(charSize, Is.EqualTo(8), "GetSize('\\0') should return 8 (quoted \\u0000 in char span) (Bug #5 fixed)");
        }

        // ── Round-trip consistency: every special char's GetSize must match write output ────────

        [Test]
        public void GetSize_Char_EscapeSequences_AllMatchWriterOutput()
        {
            char[] specials = { '"', '\\', '\b', '\f', '\n', '\r', '\t', '\0', '\x01', '\x1f' };
            Span<char> buffer = stackalloc char[32];
            foreach (var c in specials)
            {
                int reportedSize = GenJsonSizeHelper.GetSize(c);

                int index = 0;
                GenJsonWriter.WriteString(buffer, ref index, c.ToString());
                int actualSize = index;

                Assert.That(reportedSize, Is.EqualTo(actualSize),
                    $"GetSize('\\x{(int)c:x2}') = {reportedSize} but writer emitted {actualSize} chars");
            }
        }
    }
}
