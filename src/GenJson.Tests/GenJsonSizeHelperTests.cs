using NUnit.Framework;
using System;

namespace GenJson.Tests
{
    [TestFixture]
    public class GenJsonSizeHelperTests
    {
        public static System.Collections.Generic.IEnumerable<byte> ByteTestValues()
        {
            yield return byte.MinValue;
            yield return byte.MaxValue;
            for (int i = 0; i <= 2; i++)
            {
                byte val = (byte)Math.Pow(10, i);
                yield return val;
                if (val > 1) yield return (byte)(val - 1);
            }
        }

        public static System.Collections.Generic.IEnumerable<sbyte> SByteTestValues()
        {
            yield return sbyte.MinValue;
            yield return sbyte.MaxValue;
            for (int i = 0; i <= 2; i++)
            {
                sbyte val = (sbyte)Math.Pow(10, i);
                yield return val;
                yield return (sbyte)(-val);
                if (val > 1)
                {
                    yield return (sbyte)(val - 1);
                    yield return (sbyte)(-(val - 1));
                }
            }
        }

        public static System.Collections.Generic.IEnumerable<short> ShortTestValues()
        {
            yield return short.MinValue;
            yield return short.MaxValue;
            for (int i = 0; i <= 4; i++)
            {
                short val = (short)Math.Pow(10, i);
                yield return val;
                yield return (short)(-val);
                if (val > 1)
                {
                    yield return (short)(val - 1);
                    yield return (short)(-(val - 1));
                }
            }
        }

        public static System.Collections.Generic.IEnumerable<ushort> UShortTestValues()
        {
            yield return ushort.MinValue;
            yield return ushort.MaxValue;
            for (int i = 0; i <= 4; i++)
            {
                ushort val = (ushort)Math.Pow(10, i);
                yield return val;
                if (val > 1) yield return (ushort)(val - 1);
            }
        }

        public static System.Collections.Generic.IEnumerable<int> IntTestValues()
        {
            yield return int.MinValue;
            yield return int.MaxValue;
            for (int i = 0; i <= 9; i++)
            {
                int val = (int)Math.Pow(10, i);
                yield return val;
                yield return -val;
                if (val > 1)
                {
                    yield return val - 1;
                    yield return -(val - 1);
                }
            }
        }

        public static System.Collections.Generic.IEnumerable<uint> UIntTestValues()
        {
            yield return uint.MinValue;
            yield return uint.MaxValue;
            for (int i = 0; i <= 9; i++)
            {
                uint val = (uint)Math.Pow(10, i);
                yield return val;
                if (val > 1) yield return val - 1;
            }
        }

        public static System.Collections.Generic.IEnumerable<long> LongTestValues()
        {
            yield return long.MinValue;
            yield return long.MaxValue;
            for (int i = 0; i <= 18; i++)
            {
                long val = (long)Math.Pow(10, i);
                yield return val;
                yield return -val;
                if (val > 1)
                {
                    yield return val - 1;
                    yield return -(val - 1);
                }
            }
        }

        public static System.Collections.Generic.IEnumerable<ulong> ULongTestValues()
        {
            yield return ulong.MinValue;
            yield return ulong.MaxValue;
            for (int i = 0; i <= 19; i++)
            {
                if (i == 19)
                {
                    yield return 10000000000000000000ul;
                    yield return 9999999999999999999ul;
                    continue;
                }
                ulong val = (ulong)Math.Pow(10, i);
                yield return val;
                if (val > 1) yield return val - 1;
            }
        }

        public static System.Collections.Generic.IEnumerable<float> FloatTestValues()
        {
            yield return float.MinValue;
            yield return float.MaxValue;
            yield return float.NaN;
            yield return float.PositiveInfinity;
            yield return float.NegativeInfinity;
            yield return float.Epsilon;
            yield return 0f;
            yield return -0f;
            for (int i = -38; i <= 38; i += 4)
            {
                yield return (float)Math.Pow(10, i);
                yield return (float)-Math.Pow(10, i);
            }
        }

        public static System.Collections.Generic.IEnumerable<double> DoubleTestValues()
        {
            yield return double.MinValue;
            yield return double.MaxValue;
            yield return double.NaN;
            yield return double.PositiveInfinity;
            yield return double.NegativeInfinity;
            yield return double.Epsilon;
            yield return 0d;
            yield return -0d;
            for (int i = -308; i <= 308; i += 30) // sample exponents
            {
                yield return Math.Pow(10, i);
                yield return -Math.Pow(10, i);
            }
        }

        public static System.Collections.Generic.IEnumerable<decimal> DecimalTestValues()
        {
            yield return decimal.MinValue;
            yield return decimal.MaxValue;
            yield return decimal.MinusOne;
            yield return decimal.One;
            yield return decimal.Zero;
            for (int i = 0; i <= 28; i++)
            {
                // To avoid decimal OverflowException with Math.Pow, we build parsing strings
                string s = "1" + new string('0', i);
                if (decimal.TryParse(s, out decimal d))
                {
                    yield return d;
                    yield return -d;
                }
                string decStr = "0." + new string('0', i > 0 ? i - 1 : 0) + "1";
                if (decimal.TryParse(decStr, out decimal f))
                {
                    yield return f;
                    yield return -f;
                }
            }
        }

        [Test]
        public void GetSize_Byte_AllMagnitudes()
        {
            foreach (var value in ByteTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
        }

        [Test]
        public void GetSize_SByte_AllMagnitudes()
        {
            foreach (var value in SByteTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
        }

        [Test]
        public void GetSize_Short_AllMagnitudes()
        {
            foreach (var value in ShortTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
        }

        [Test]
        public void GetSize_UShort_AllMagnitudes()
        {
            foreach (var value in UShortTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
        }

        [Test]
        public void GetSize_Int_AllMagnitudes()
        {
            foreach (var value in IntTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
        }

        [Test]
        public void GetSize_UInt_AllMagnitudes()
        {
            foreach (var value in UIntTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
        }

        [Test]
        public void GetSize_Long_AllMagnitudes()
        {
            foreach (var value in LongTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
        }

        [Test]
        public void GetSize_ULong_AllMagnitudes()
        {
            foreach (var value in ULongTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
        }

        [Test]
        public void GetSize_Float_AllMagnitudes()
        {
            foreach (var value in FloatTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
        }

        [Test]
        public void GetSize_Double_AllMagnitudes()
        {
            foreach (var value in DoubleTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
        }

        [Test]
        public void GetSize_Decimal_AllMagnitudes()
        {
            foreach (var value in DecimalTestValues())
            {
                int reportedSize = GenJsonSizeHelper.GetSize(value);
                int actualSize = value.ToString("G", System.Globalization.CultureInfo.InvariantCulture).Length;
                Assert.That(reportedSize, Is.EqualTo(actualSize));
            }
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
