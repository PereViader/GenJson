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
        public void TryParseString_ParsesUnicodeEscape()
        {
            var json = "\"\\u0041\"".AsSpan(); // 'A'
            int index = 0;
            var success = GenJsonParser.TryParseString(json, ref index, out var result);
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo("A"));
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
        public void TryParseBoolean_ParsesBooleans()
        {
            int index = 0;
            Assert.That(GenJsonParser.TryParseBoolean("true".AsSpan(), ref index, out var t), Is.True);
            Assert.That(t, Is.True);

            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean("false".AsSpan(), ref index, out var f), Is.True);
            Assert.That(f, Is.False);
        }

        [Test]
        public void MatchesKey_MatchesCorrectKey()
        {
            var json = "\"key\": 123".AsSpan();
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key"), Is.True);
            Assert.That(json[index], Is.EqualTo(':'));
        }

        [Test]
        public void MatchesKey_DoesNotMatchWrongKey()
        {
            var json = "\"other\": 123".AsSpan();
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key"), Is.False);
            Assert.That(index, Is.EqualTo(0)); // Should reset index
        }

        [Test]
        public void MatchesKey_HandlesEscapedKey()
        {
            var json = "\"k\\u0065y\": 123".AsSpan(); // "key"
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key"), Is.True);
        }

        [Test]
        public void TrySkipValue_SkipsObject()
        {
            var json = "{\"a\": 1, \"b\": [2, 3]} next".AsSpan();
            int index = 0;
            var success = GenJsonParser.TrySkipValue(json, ref index);
            Assert.That(success, Is.True);
            GenJsonParser.SkipWhitespace(json, ref index);

            // Should be at "next"
            Assert.That(json[index], Is.EqualTo('n'));
        }

        [Test]
        public void TrySkipValue_SkipsArray()
        {
            var json = "[1, {\"a\": 2}, [3]] next".AsSpan();
            int index = 0;
            var success = GenJsonParser.TrySkipValue(json, ref index);
            Assert.That(success, Is.True);
            GenJsonParser.SkipWhitespace(json, ref index);
            Assert.That(json[index], Is.EqualTo('n'));
        }

        [Test]
        public void TryParseDouble_ParsesExponents()
        {
            var cases = new[] { ("1e2", 100.0), ("1E2", 100.0), ("1.5e2", 150.0), ("1e-1", 0.1) };
            foreach (var (input, expected) in cases)
            {
                int index = 0;
                var success = GenJsonParser.TryParseDouble(input.AsSpan(), ref index, out var result);
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
                Assert.That(GenJsonParser.TryParseBoolean(input.AsSpan(), ref index, out _), Is.False, $"Should return false for {input}");
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
                var success = GenJsonParser.TryParseDecimal(input.AsSpan(), ref index, out var result);
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
                var success = GenJsonParser.TryParseDecimal(input.AsSpan(), ref index, out var result);
                Assert.That(success, Is.True, $"Failed to parse {input}");
                Assert.That(result, Is.EqualTo(expected), $"Incorrect value for {input}");
            }
        }

        [Test]
        public void TryParseDecimal_Edges()
        {
            int index = 0;
            Assert.That(GenJsonParser.TryParseDecimal("+123".AsSpan(), ref index, out var res), Is.True);
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
                bool result = GenJsonParser.TryParseDecimal(input.AsSpan(), ref index, out _);
                Assert.That(result, Is.False, $"Should fail for {input}");
            }
        }
    }
}
