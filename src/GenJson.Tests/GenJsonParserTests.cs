using NUnit.Framework;
using System;

namespace GenJson.Tests
{
    [TestFixture]
    public class GenJsonParserTests
    {
        [Test]
        public void TryParseString_ParsesSimpleString()
        {
            var json = "\"hello\"".AsSpan();
            int index = 0;
            var success = GenJsonParser.TryParseString(json, ref index, out var result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo("hello"));
            Assert.That(index, Is.EqualTo(json.Length));
        }

        [Test]
        public void TryParseString_ParsesEscapedString()
        {
            var json = "\"hello \\\"world\\\"\"".AsSpan();
            int index = 0;
            var success = GenJsonParser.TryParseString(json, ref index, out var result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo("hello \"world\""));
        }

        [Test]
        public void TryParseString_ParsesAllEscapes()
        {
            var json = "\"hello\\n\\r\\t\\b\\f\\/\\\\\\u0031world\"".AsSpan();
            int index = 0;
            var success = GenJsonParser.TryParseString(json, ref index, out var result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo("hello\n\r\t\b\f/\\1world"));

            // UnescapeString directly for long string fallback (>128 chars)
            var longStrObj = new System.Text.StringBuilder("\"");
            var expectedStr = new System.Text.StringBuilder();
            for (int i = 0; i < 200; i++)
            {
                longStrObj.Append("\\n");
                expectedStr.Append("\n");
            }
            longStrObj.Append("\"");
            index = 0;
            Assert.That(GenJsonParser.TryParseString(longStrObj.ToString().AsSpan(), ref index, out var longRes), Is.True);
            Assert.That(longRes, Is.EqualTo(expectedStr.ToString()));
        }

        [Test]
        public void TryParseString_ParsesSingleUnicodeEscape()
        {
            var json = "\"\\u0041\"".AsSpan(); // 'A'
            int index = 0;
            var success = GenJsonParser.TryParseString(json, ref index, out var result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void TryParseString_ParsesSurrogatePair()
        {
            var json = "\"potato\\uD83D\\uDE01banana\"".AsSpan();
            int index = 0;
            var success = GenJsonParser.TryParseString(json, ref index, out var result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo("potato😁banana"));
        }

        [Test]
        public void TryParseString_HandlesEscapedQuotesMidString()
        {
            // The JSON literal: "The cat said \"Meow\" and ran away"
            var json = "\"The cat said \\\"Meow\\\" and ran away\"".AsSpan();
            int index = 0;

            var success = GenJsonParser.TryParseString(json, ref index, out var result);

            // The original code would fail this by returning "The cat said " 
            // and leaving index at the first \"
            Assert.That(success, Is.True, "Parser should succeed");
            Assert.That(result, Is.EqualTo("The cat said \"Meow\" and ran away"), "Result should contain the full unescaped string");
            Assert.That(index, Is.EqualTo(json.Length), "Index should be at the end of the JSON string");
        }

        [Test]
        public void TryParseString_MalformedUnicode_ReturnsFalse()
        {
            // The escape is missing digits and the closing quote
            var json = "\"test \\u00".AsSpan();
            int index = 0;

            // This should NOT throw ArgumentOutOfRangeException
            bool success = GenJsonParser.TryParseString(json, ref index, out var result);

            Assert.That(success, Is.False, "Parser should return false for truncated escapes.");
        }

        [Test]
        public void TryParseString_HandlesEscapesAtEnd()
        {
            // JSON: "Standard escape: \\"
            // The backslash is escaped by another backslash.
            var json = "\"Standard escape: \\\\\"".AsSpan();
            int index = 0;

            var success = GenJsonParser.TryParseString(json, ref index, out var result);

            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo("Standard escape: \\"));
            Assert.That(index, Is.EqualTo(json.Length), "Should consume the entire JSON string");
        }

        [Test]
        public void TryParseString_InvalidEscapes_ReturnsFalse()
        {
            int index = 0;
            Assert.That(GenJsonParser.TryParseString("\"hello \\".AsSpan(), ref index, out _), Is.False); // Incomplete escape at end

            index = 0;
            // Unescaped control characters < 32 inside strings are technically invalid per JSON spec,
            // but parser allows parsing them anyway. However, it fails if it starts with quote and ends randomly.
            Assert.That(GenJsonParser.TryParseString("\"hello \n".AsSpan(), ref index, out _), Is.False);
        }

        [Test]
        public void TryParseInt_ParsesIntegers()
        {
            var cases = new[] { ("123", 123), ("-123", -123), ("0", 0), ("2147483647", int.MaxValue), ("-2147483648", int.MinValue) };
            foreach (var (input, expected) in cases)
            {
                int index = 0;
                var success = GenJsonParser.TryParseInt(input.AsSpan(), ref index, out int result);
                Assert.That(success, Is.True);
                Assert.That(result, Is.EqualTo(expected));
            }
        }

        [Test]
        public void TryParseNumerics_Nullable_Invalid()
        {
            var json = "abc".AsSpan();
            int index = 0;

            Assert.That(GenJsonParser.TryParseUInt(json, ref index, out uint? ui), Is.False);
            Assert.That(GenJsonParser.TryParseShort(json, ref index, out short? s), Is.False);
            Assert.That(GenJsonParser.TryParseUShort(json, ref index, out ushort? us), Is.False);
            Assert.That(GenJsonParser.TryParseByte(json, ref index, out byte? b), Is.False);
            Assert.That(GenJsonParser.TryParseSByte(json, ref index, out sbyte? sb), Is.False);
            Assert.That(GenJsonParser.TryParseULong(json, ref index, out ulong? ul), Is.False);
            Assert.That(GenJsonParser.TryParseDouble(json, ref index, out double? d), Is.False);
            Assert.That(GenJsonParser.TryParseFloat(json, ref index, out float? f), Is.False);
            Assert.That(GenJsonParser.TryParseDecimal(json, ref index, out decimal? dec), Is.False);
        }

        [Test]
        public void TryParseBoolean_ParsesBooleans()
        {
            int index = 0;
            Assert.That(GenJsonParser.TryParseBoolean("true".AsSpan(), ref index, out bool t), Is.True);
            Assert.That(t, Is.True);

            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean("false".AsSpan(), ref index, out bool f), Is.True);
            Assert.That(f, Is.False);

            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean("true".AsSpan(), ref index, out bool? nt), Is.True);
            Assert.That(nt, Is.True);

            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean("false".AsSpan(), ref index, out bool? nf), Is.True);
            Assert.That(nf, Is.False);
        }

        [Test]
        public void MatchesKey_MatchesCorrectKey()
        {
            var json = "\"key\":123".AsSpan();
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key"), Is.True);
            Assert.That(json[index], Is.EqualTo(':'));
        }

        [Test]
        public void MatchesKey_DoesNotMatchWrongKey()
        {
            var json = "\"other\":123".AsSpan();
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key"), Is.False);
            Assert.That(index, Is.EqualTo(0)); // Should reset index
        }

        [Test]
        public void MatchesKey_HandlesEscapedKey()
        {
            var json = "\"k\\u0065y\":123".AsSpan(); // "key"
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key"), Is.True);
        }

        [Test]
        public void MatchesKey_HandlesAllEscapedKey()
        {
            var json = "\"all\\n\\r\\t\\b\\f\\/\\\\\\u0031world\":123".AsSpan();
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "all\n\r\t\b\f/\\1world"), Is.True);
        }

        [Test]
        public void MatchesKey_ByteSpan_MatchesCorrectKey()
        {
            var json = System.Text.Encoding.UTF8.GetBytes("\"key\":123").AsSpan();
            var expectedUtf8 = System.Text.Encoding.UTF8.GetBytes("key").AsSpan();
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key", expectedUtf8), Is.True);
            Assert.That(json[index], Is.EqualTo((byte)':'));
        }

        [Test]
        public void MatchesKey_ByteSpan_DoesNotMatchWrongKey()
        {
            var json = System.Text.Encoding.UTF8.GetBytes("\"other\":123").AsSpan();
            var expectedUtf8 = System.Text.Encoding.UTF8.GetBytes("key").AsSpan();
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key", expectedUtf8), Is.False);
            Assert.That(index, Is.EqualTo(0)); // Should reset index
        }

        [Test]
        public void MatchesKey_ByteSpan_HandlesEscapedKeyFallback()
        {
            // The JSON contains the escaped unicode character for 'e' in "key"
            var json = System.Text.Encoding.UTF8.GetBytes("\"k\\u0065y\":123").AsSpan();
            var expectedUtf8 = System.Text.Encoding.UTF8.GetBytes("key").AsSpan();
            int index = 0;

            // This tests that the fast-path SequenceEqual fails and the slow-path
            // correctly decodes the escaped value!
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key", expectedUtf8), Is.True);
        }

        [Test]
        public void MatchesKey_ByteSpan_HandlesQuoteEscapedKeyFallback()
        {
            // The JSON contains: "my\"key":123
            var json = System.Text.Encoding.UTF8.GetBytes("\"my\\\"key\":123").AsSpan();
            var expectedUtf8 = System.Text.Encoding.UTF8.GetBytes("my\"key").AsSpan();
            int index = 0;

            // This tests that the fast path avoids incorrectly jumping ahead due to early quotes
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "my\"key", expectedUtf8), Is.True);
        }

        [Test]
        public void MatchesKey_ByteSpan_HandlesSurrogatePairKeyFallback()
        {
            // The JSON contains the escaped unicode character for an emoji surrogate pair 😁
            var json = System.Text.Encoding.UTF8.GetBytes("\"m\\uD83D\\uDE01y\":123").AsSpan();
            var expectedUtf8 = System.Text.Encoding.UTF8.GetBytes("m😁y").AsSpan();
            int index = 0;

            // This tests that the fallback correctly handles 4-byte surrogate characters
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "m😁y", expectedUtf8), Is.True);
            Assert.That(index, Is.GreaterThan(0));
            Assert.That(json[index], Is.EqualTo((byte)':'));
        }

        [Test]
        public void MatchesKey_ByteSpan_HandlesAllEscapedKey()
        {
            var json = System.Text.Encoding.UTF8.GetBytes("\"all\\n\\r\\t\\b\\f\\/\\\\\\u0031world\":123").AsSpan();
            var expectedUtf8 = System.Text.Encoding.UTF8.GetBytes("all\n\r\t\b\f/\\1world").AsSpan();
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "all\n\r\t\b\f/\\1world", expectedUtf8), Is.True);

            var jsonErr = System.Text.Encoding.UTF8.GetBytes("\"all\\n\\u003Z\":123").AsSpan();
            index = 0;
            Assert.That(GenJsonParser.MatchesKey(jsonErr, ref index, "all\n1", System.Text.Encoding.UTF8.GetBytes("all\n1").AsSpan()), Is.False); // invalid hex

            var jsonErr2 = System.Text.Encoding.UTF8.GetBytes("\"all\\u00").AsSpan();
            index = 0;
            Assert.That(GenJsonParser.MatchesKey(jsonErr2, ref index, "all1", System.Text.Encoding.UTF8.GetBytes("all1").AsSpan()), Is.False); // truncated hex

            // Expected length mismatch in utf8 codepoint
            var validMultibyte = System.Text.Encoding.UTF8.GetBytes("\"all\\n🚀\u00A9á\":123").AsSpan();
            index = 0;
            Assert.That(GenJsonParser.MatchesKey(validMultibyte, ref index, "all\n🚀\u00A9", System.Text.Encoding.UTF8.GetBytes("all\n🚀\u00A9").AsSpan()), Is.False);

            // Invalid utf8 continuation bytes
            // 0xC0 requires 1 extra byte
            var invalidUtf8_1 = new byte[] { (byte)'"', (byte)'a', (byte)'\\', (byte)'n', 0xC0, (byte)'"', (byte)':', (byte)'1' };
            index = 0;
            Assert.That(GenJsonParser.MatchesKey(invalidUtf8_1.AsSpan(), ref index, "a\n\u0000", System.Text.Encoding.UTF8.GetBytes("a\n\u0000").AsSpan()), Is.False);

            // 0xE0 requires 2 extra bytes, give it valid string but cut early
            var invalidUtf8_2 = new byte[] { (byte)'"', (byte)'a', (byte)'\\', (byte)'n', 0xE0, 0x80, (byte)'"', (byte)':', (byte)'1' };
            index = 0;
            Assert.That(GenJsonParser.MatchesKey(invalidUtf8_2.AsSpan(), ref index, "a\n\u0000", System.Text.Encoding.UTF8.GetBytes("a\n\u0000").AsSpan()), Is.False);

            // 0xF0 requires 3 extra bytes, give it 2 invalid continuations
            var invalidUtf8_3 = new byte[] { (byte)'"', (byte)'a', (byte)'\\', (byte)'n', 0xF0, 0x80, 0x00, (byte)'"', (byte)':', (byte)'1' };
            index = 0;
            Assert.That(GenJsonParser.MatchesKey(invalidUtf8_3.AsSpan(), ref index, "a\n\u0000", System.Text.Encoding.UTF8.GetBytes("a\n\u0000").AsSpan()), Is.False);

            // Completely invalid start byte > 128 but not matching any pattern
            var invalidUtf8_4 = new byte[] { (byte)'"', (byte)'a', (byte)'\\', (byte)'n', 0xFF, (byte)'"', (byte)':', (byte)'1' };
            index = 0;
            Assert.That(GenJsonParser.MatchesKey(invalidUtf8_4.AsSpan(), ref index, "a\n\u0000", System.Text.Encoding.UTF8.GetBytes("a\n\u0000").AsSpan()), Is.False);

            // Give it a valid byte start 0xE0 (3 bytes valid prefix), but instead of a valid continuation 0x80..0xBF, give it something else.
            var invalidUtf8_5 = new byte[] { (byte)'"', (byte)'a', (byte)'\\', (byte)'n', 0xE0, 0xFF, 0x80, (byte)'"', (byte)':', (byte)'1' };
            index = 0;
            Assert.That(GenJsonParser.MatchesKey(invalidUtf8_5.AsSpan(), ref index, "a\n\u0000", System.Text.Encoding.UTF8.GetBytes("a\n\u0000").AsSpan()), Is.False);
        }

        [Test]
        public void TryParseStringSpan_InvalidStart_ReturnsFalse()
        {
            int index = 0;
            Assert.That(GenJsonParser.TryParseStringSpan("hello".AsSpan(), ref index, out _, out _), Is.False);
            index = 0;
            Assert.That(GenJsonParser.TryParseStringSpan(System.Text.Encoding.UTF8.GetBytes("hello").AsSpan(), ref index, out _, out _), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TryParseStringSpan("\"hello\\".AsSpan(), ref index, out _, out _), Is.False);
            index = 0;
            Assert.That(GenJsonParser.TryParseStringSpan(System.Text.Encoding.UTF8.GetBytes("\"hello\\").AsSpan(), ref index, out _, out _), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TryParseStringSpan("\"hello\\u0000".AsSpan(), ref index, out _, out _), Is.False); // missing end quote
            index = 0;
            Assert.That(GenJsonParser.TryParseStringSpan(System.Text.Encoding.UTF8.GetBytes("\"hello\\u0000").AsSpan(), ref index, out _, out _), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TryParseStringSpan("\"hello\\n".AsSpan(), ref index, out _, out _), Is.False); // missing end quote normal escape
            index = 0;
            Assert.That(GenJsonParser.TryParseStringSpan(System.Text.Encoding.UTF8.GetBytes("\"hello\\n").AsSpan(), ref index, out _, out _), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TryParseStringSpan("\"hello\\u0000\"".AsSpan(), ref index, out _, out _), Is.True); // valid match
            index = 0;
            Assert.That(GenJsonParser.TryParseStringSpan(System.Text.Encoding.UTF8.GetBytes("\"hello\\u0000\"").AsSpan(), ref index, out _, out _), Is.True);
        }

        [Test]
        public void TrySkipValue_SkipsObject()
        {
            var json = "{\"a\":1,\"b\":[2,3]}next".AsSpan();
            int index = 0;
            var success = GenJsonParser.TrySkipValue(json, ref index);
            Assert.That(success, Is.True);

            // Should be at "next"
            Assert.That(json[index], Is.EqualTo('n'));
        }

        [Test]
        public void TrySkipValue_SkipsArray()
        {
            var json = "[1,{\"a\":2},[3]]next".AsSpan();
            int index = 0;
            var success = GenJsonParser.TrySkipValue(json, ref index);
            Assert.That(success, Is.True);
            Assert.That(json[index], Is.EqualTo('n'));
        }

        [Test]
        public void TryParseDouble_ParsesExponents()
        {
            var cases = new[] { ("1e2", 100.0), ("1E2", 100.0), ("1.5e2", 150.0), ("1e-1", 0.1) };
            foreach (var (input, expected) in cases)
            {
                int index = 0;
                var success = GenJsonParser.TryParseDouble(input.AsSpan(), ref index, out double result);
                Assert.That(success, Is.True);
                Assert.That(result, Is.EqualTo(expected).Within(0.000001));
            }
        }

        [Test]
        public void TryParseBoolean_ReturnsFalseOnInvalid()
        {
            var cases = new[] { "truee", "tru", "TRUE", "False" };
            foreach (var input in cases)
            {
                int index = 0;
                Assert.That(GenJsonParser.TryParseBoolean(input.AsSpan(), ref index, out bool _), Is.False, $"Should return false for {input}");
            }
        }

        [Test]
        public void TryParseNull_ParsesNull()
        {
            int index = 0;
            var success = GenJsonParser.TryParseNull("null".AsSpan(), ref index);
            Assert.That(success, Is.True);
            Assert.That(index, Is.EqualTo(4));
        }

        [Test]
        public void TryParseNull_ReturnsFalseOnInvalid()
        {
            var cases = new[] { "nul", "NULL", "none" };
            foreach (var input in cases)
            {
                int index = 0;
                Assert.That(GenJsonParser.TryParseNull(input.AsSpan(), ref index), Is.False);
            }
        }
        [Test]
        public void TryParseDecimal_ParsesValidDecimals()
        {
            var cases = new[]
            {
                ("0", 0m),
                ("123", 123m),
                ("-123", -123m),
                ("0.0", 0.0m),
                ("123.456", 123.456m),
                ("-123.456", -123.456m),
                ("0.0000001", 0.0000001m),
                ("79228162514264337593543950335", decimal.MaxValue),
                ("-79228162514264337593543950335", decimal.MinValue)
            };

            foreach (var (input, expected) in cases)
            {
                int index = 0;
                var success = GenJsonParser.TryParseDecimal(input.AsSpan(), ref index, out decimal result);
                Assert.That(success, Is.True, $"Failed to parse {input}");
                Assert.That(result, Is.EqualTo(expected), $"Incorrect value for {input}");
            }
        }

        [Test]
        public void TryParseDecimal_ParsesScientificNotation()
        {
            var cases = new[]
            {
                ("1e2", 100m),
                ("1E2", 100m),
                ("1.5e2", 150m),
                ("1e-2", 0.01m),
                ("-1e2", -100m),
                ("1.2345e+3", 1234.5m)
            };

            foreach (var (input, expected) in cases)
            {
                int index = 0;
                var success = GenJsonParser.TryParseDecimal(input.AsSpan(), ref index, out decimal result);
                Assert.That(success, Is.True, $"Failed to parse {input}");
                Assert.That(result, Is.EqualTo(expected), $"Incorrect value for {input}");
            }
        }

        [Test]
        public void TryParseDecimal_Edges()
        {
            int index = 0;
            Assert.That(GenJsonParser.TryParseDecimal("+123".AsSpan(), ref index, out decimal res), Is.True);
            Assert.That(res, Is.EqualTo(123m));

            index = 0;
            Assert.That(GenJsonParser.TryParseDecimal(".5".AsSpan(), ref index, out res), Is.True);
            Assert.That(res, Is.EqualTo(0.5m));
        }

        [Test]
        public void TryParseDecimal_ReturnsFalseOnInvalid()
        {
            var cases = new[]
            {
                "1.2.3",
                "1-2",
                "e5",
                "abc",
                ""
            };

            foreach (var input in cases)
            {
                int index = 0;
                bool result = GenJsonParser.TryParseDecimal(input.AsSpan(), ref index, out decimal _);
                Assert.That(result, Is.False, $"Should fail for {input}");
            }
        }
        [Test]
        public void TryParseLong_ParsesValidAndInvalid()
        {
            int index = 0;
            Assert.That(GenJsonParser.TryParseLong("12345678901234", ref index, out long l), Is.True);
            Assert.That(l, Is.EqualTo(12345678901234));

            index = 0;
            Assert.That(GenJsonParser.TryParseLong("-123", ref index, out long? nl), Is.True);
            Assert.That(nl, Is.EqualTo(-123));

            index = 0;
            Assert.That(GenJsonParser.TryParseLong("abc", ref index, out l), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TryParseLong("abc", ref index, out nl), Is.False);
        }

        [Test]
        public void TryParseChar_InvalidLength_ReturnsFalse()
        {
            int index = 0;
            Assert.That(GenJsonParser.TryParseChar("\"abc\"", ref index, out char c), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TryParseChar("\"\"", ref index, out char? nc), Is.False);
        }

        [Test]
        public void TryParseBoolean_TrailingChars_ReturnsFalse()
        {
            int index = 0;
            // "truex" is invalid because 'x' is not a delimiter
            Assert.That(GenJsonParser.TryParseBoolean("truex", ref index, out bool b), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean("falsey", ref index, out bool? nb), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean("tru", ref index, out b), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean("fals", ref index, out nb), Is.False);
        }

        [Test]
        public void TrySkipValue_IncompleteJson_ReturnsFalse()
        {
            int index = 0;
            Assert.That(GenJsonParser.TrySkipValue("{", ref index), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TrySkipValue("[", ref index), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TrySkipValue("\"incomplete", ref index), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TrySkipValue("\"incomplete\\", ref index), Is.False);

            // Incomplete object keys/values
            index = 0;
            Assert.That(GenJsonParser.TrySkipValue("{\"a\"", ref index), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TrySkipValue("{\"a\":", ref index), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TrySkipValue("{\"a\":1,", ref index), Is.False);

            // Incomplete arrays
            index = 0;
            Assert.That(GenJsonParser.TrySkipValue("[1,", ref index), Is.False);
        }

        [Test]
        public void Utils_MalformedJson_HandlesCorrectly()
        {
            Assert.That(GenJsonParser.CountListItems("[1,", 0), Is.EqualTo(1));
            Assert.That(GenJsonParser.CountListItems("[\"incomplete\"", 0), Is.EqualTo(1));

            Assert.That(GenJsonParser.CountDictionaryItems("{\"a\":1,", 0), Is.EqualTo(1));
            Assert.That(GenJsonParser.CountDictionaryItems("{\"a", 0), Is.EqualTo(1));
            Assert.That(GenJsonParser.CountDictionaryItems("{\"a\":", 0), Is.EqualTo(1));

            Assert.That(GenJsonParser.TryFindProperty("{\"a", 0, "a", out int _), Is.False);
            Assert.That(GenJsonParser.TryFindProperty("{\"a\":", 0, "b", out int _), Is.False);
            Assert.That(GenJsonParser.TryFindProperty("{\"a\":1,", 0, "b", out int _), Is.False);
        }

        [Test]
        public void MatchesKey_CharSpan_HandlesFallbacks()
        {
            // Branch: expected sequence ends before parsed sequence
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey("\"abc\"".AsSpan(), ref index, "ab"), Is.False);

            // Branch: expected sequence contains mismatch after escape
            index = 0;
            Assert.That(GenJsonParser.MatchesKey("\"a\\\"b\"".AsSpan(), ref index, "a\"c"), Is.False);

            // Branch: unknown escape character just treats it as character
            index = 0;
            Assert.That(GenJsonParser.MatchesKey("\"a\\xb\"".AsSpan(), ref index, "axb"), Is.True);

            // Branch: EOF mid-escape
            index = 0;
            Assert.That(GenJsonParser.MatchesKey("\"a\\".AsSpan(), ref index, "a"), Is.False);
        }
        // ── Bug #1 / #2: TrySkipValue dead else branch & dead index >= Length check ───────────────

        [Test]
        public void TrySkipValue_InvalidLeadChar_ReturnsFalse()
        {
            // Characters that are not valid JSON value starters should return false.
            // The dead else-branch would advance index and return true for non-delimiter chars.
            int index = 0;
            Assert.That(GenJsonParser.TrySkipValue("@123".AsSpan(), ref index), Is.False);
            Assert.That(index, Is.EqualTo(0), "index must not be advanced on failure");

            index = 0;
            Assert.That(GenJsonParser.TrySkipValue(";".AsSpan(), ref index), Is.False);
        }

        [Test]
        public void TrySkipValue_DelimiterAtStart_ReturnsFalse()
        {
            // A comma or closing bracket/brace is not a value; the dead else-branch
            // returned false for these but only by coincidence via IsDelimiter — verify.
            int index = 0;
            Assert.That(GenJsonParser.TrySkipValue(",".AsSpan(), ref index), Is.False);
            Assert.That(index, Is.EqualTo(0));

            index = 0;
            Assert.That(GenJsonParser.TrySkipValue("}".AsSpan(), ref index), Is.False);

            index = 0;
            Assert.That(GenJsonParser.TrySkipValue("]".AsSpan(), ref index), Is.False);
        }

        // ── Bug #1 / #2: byte variant ────────────────────────────────────────────────────────────

        [Test]
        public void TrySkipValue_Utf8_InvalidLeadChar_ReturnsFalse()
        {
            int index = 0;
            Assert.That(GenJsonParser.TrySkipValue(System.Text.Encoding.UTF8.GetBytes("@123").AsSpan(), ref index), Is.False);
            Assert.That(index, Is.EqualTo(0));

            index = 0;
            Assert.That(GenJsonParser.TrySkipValue(System.Text.Encoding.UTF8.GetBytes(",").AsSpan(), ref index), Is.False);
            Assert.That(index, Is.EqualTo(0));
        }

        // ── Bug #3: TryParseStringSpan / TryParseString — \uXXXX advances index past end ────────

        [Test]
        public void TryParseStringSpan_TruncatedUnicodeEscape_IndexNotPastEnd()
        {
            // "\u00" — only 2 hex digits, closed without digits
            var json = "\"a\\u00\"".AsSpan(); // 7 chars; \u sequence has only 2 hex digits
            int index = 0;
            bool success = GenJsonParser.TryParseStringSpan(json, ref index, out _, out _);
            // May succeed or fail depending on implementation, but index must be <= json.Length
            Assert.That(index, Is.LessThanOrEqualTo(json.Length), "index must not exceed span length after truncated \\uXXXX");
        }

        [Test]
        public void TryParseString_TruncatedUnicodeEscape_IndexNotPastEnd()
        {
            // Raw truncated escape — no closing quote after the two hex digits
            var json = "\"test\\u00".AsSpan();
            int index = 0;
            bool success = GenJsonParser.TryParseString(json, ref index, out _);
            Assert.That(success, Is.False, "Should fail on truncated \\uXXXX");
            Assert.That(index, Is.LessThanOrEqualTo(json.Length), "index must not exceed span length");
        }

        [Test]
        public void TryParseString_TruncatedUnicodeEscape_Utf8_IndexNotPastEnd()
        {
            var json = System.Text.Encoding.UTF8.GetBytes("\"test\\u00").AsSpan();
            int index = 0;
            bool success = GenJsonParser.TryParseString(json, ref index, out _);
            Assert.That(success, Is.False);
            Assert.That(index, Is.LessThanOrEqualTo(json.Length));
        }

        // ── Bug #7: char numeric parsers advance index on failure ────────────────────────────────

        [Test]
        public void TryParseInt_Overflow_IndexNotAdvanced()
        {
            // int.MaxValue + 1 = 2147483648, which overflows int — TryParse should fail.
            // After failure, index must be restored to its initial position.
            var json = "2147483648next".AsSpan();
            int index = 0;
            bool success = GenJsonParser.TryParseInt(json, ref index, out int _);
            Assert.That(success, Is.False, "Should fail on overflow");
            // BUG: currently index is advanced to 10. After fixing it should be 0.
            Assert.That(index, Is.EqualTo(0), "index must be restored to 0 on failure (Bug #7)");
        }

        [Test]
        public void TryParseLong_Overflow_IndexNotAdvanced()
        {
            // ulong max value overflows long
            var json = "18446744073709551616next".AsSpan(); // > long.MaxValue
            int index = 0;
            bool success = GenJsonParser.TryParseLong(json, ref index, out long _);
            Assert.That(success, Is.False);
            Assert.That(index, Is.EqualTo(0), "index must be restored on failure (Bug #7)");
        }

        [Test]
        public void TryParseDouble_InvalidInput_IndexNotAdvanced()
        {
            // A string completely invalid for floating point
            var json = "abc".AsSpan();
            int index = 0;
            bool success = GenJsonParser.TryParseDouble(json, ref index, out double _);
            Assert.That(success, Is.False);
            Assert.That(index, Is.EqualTo(0), "index must not be advanced on failure (Bug #7)");
        }

        [Test]
        public void TryParseInt_ValidValueFollowedByJunk_IndexAtEndOfNumber()
        {
            // Sanity check: successful parse must still correctly position index
            var json = "42,next".AsSpan();
            int index = 0;
            bool success = GenJsonParser.TryParseInt(json, ref index, out int result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(42));
            Assert.That(index, Is.EqualTo(2)); // positioned at ','
        }

        // ── Bug #8: MatchesKey (char) doesn't restore index on EOF mid-escape ───────────────────

        [Test]
        public void MatchesKey_EofMidEscape_IndexRestored()
        {
            // JSON ends right after the backslash — EOF mid-escape
            var json = "\"a\\".AsSpan();
            int index = 0;
            bool result = GenJsonParser.MatchesKey(json, ref index, "a");
            Assert.That(result, Is.False);
            // BUG: currently index is NOT restored. After fixing it must be 0.
            Assert.That(index, Is.EqualTo(0), "index must be restored to originalIndex on EOF mid-escape (Bug #8)");
        }

        [Test]
        public void MatchesKey_Utf8_EofMidEscape_IndexRestored()
        {
            var json = System.Text.Encoding.UTF8.GetBytes("\"a\\").AsSpan();
            int index = 0;
            bool result = GenJsonParser.MatchesKey(json, ref index, "a");
            Assert.That(result, Is.False);
            Assert.That(index, Is.EqualTo(0), "index must be restored on EOF mid-escape (byte variant)");
        }

        // ── Bug #9: TryFindProperty — dead index >= Length check (behavior sanity) ────────────

        [Test]
        public void TryFindProperty_EmptyObject_ReturnsFalse()
        {
            // An empty object should not find any key
            Assert.That(GenJsonParser.TryFindProperty("{}".AsSpan(), 0, "key", out _), Is.False);
        }

        [Test]
        public void TryFindProperty_LastProperty_ReturnsCorrectIndex()
        {
            var json = "{\"a\":1,\"b\":42}".AsSpan();
            Assert.That(GenJsonParser.TryFindProperty(json, 0, "b", out int vi), Is.True);
            Assert.That(json[vi], Is.EqualTo('4'));
        }

        [Test]
        public void TryFindProperty_Utf8_EmptyObject_ReturnsFalse()
        {
            Assert.That(GenJsonParser.TryFindProperty(System.Text.Encoding.UTF8.GetBytes("{}").AsSpan(), 0, "key", out _), Is.False);
        }

        // ── Bug #15: TrySkipString doesn't skip hex digits after \u ──────────────────────────────

        [Test]
        public void TrySkipString_UnicodeEscape_SkipsCorrectly()
        {
            // String with \uXXXX escape — skip must land AFTER the full escape
            var json = "\"\\u0041next\"after".AsSpan(); // \u0041 = 'A'
            int index = 0;
            bool success = GenJsonParser.TrySkipString(json, ref index);
            Assert.That(success, Is.True);
            // After skipping "\"\\u0041next\"", index should be at 'a' of "after"
            Assert.That(json[index], Is.EqualTo('a'), "TrySkipString must skip past \\uXXXX hex digits (Bug #15)");
        }

        [Test]
        public void TrySkipString_Utf8_UnicodeEscape_SkipsCorrectly()
        {
            var json = System.Text.Encoding.UTF8.GetBytes("\"\\u0041next\"after").AsSpan();
            int index = 0;
            bool success = GenJsonParser.TrySkipString(json, ref index);
            Assert.That(success, Is.True);
            Assert.That(json[index], Is.EqualTo((byte)'a'), "byte TrySkipString must skip past \\uXXXX hex digits (Bug #15)");
        }

        [Test]
        public void TrySkipString_MultipleUnicodeEscapes_SkipsCorrectly()
        {
            // Multiple consecutive \uXXXX escapes
            var json = "\"\\u0041\\u0042\\u0043end\"X".AsSpan(); // ABC
            int index = 0;
            bool success = GenJsonParser.TrySkipString(json, ref index);
            Assert.That(success, Is.True);
            Assert.That(json[index], Is.EqualTo('X'));
        }
    }
}
