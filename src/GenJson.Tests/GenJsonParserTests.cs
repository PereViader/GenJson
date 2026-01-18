using NUnit.Framework;
using System;

namespace GenJson.Tests
{
    [TestFixture]
    public class GenJsonParserTests
    {
        [Test]
        public void ParseString_ParsesSimpleString()
        {
            var json = "\"hello\"".AsSpan();
            int index = 0;
            var result = GenJsonParser.ParseString(json, ref index);
            Assert.That(result, Is.EqualTo("hello"));
            Assert.That(index, Is.EqualTo(json.Length));
        }

        [Test]
        public void ParseString_ParsesEscapedString()
        {
            var json = "\"hello \\\"world\\\"\"".AsSpan();
            int index = 0;
            var result = GenJsonParser.ParseString(json, ref index);
            Assert.That(result, Is.EqualTo("hello \"world\""));
        }

        [Test]
        public void ParseString_ParsesUnicodeEscape()
        {
            var json = "\"\\u0041\"".AsSpan(); // 'A'
            int index = 0;
            var result = GenJsonParser.ParseString(json, ref index);
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void ParseInt_ParsesIntegers()
        {
            var cases = new[] { ("123", 123), ("-123", -123), ("0", 0), ("2147483647", int.MaxValue), ("-2147483648", int.MinValue) };
            foreach (var (input, expected) in cases)
            {
                int index = 0;
                var result = GenJsonParser.ParseInt(input.AsSpan(), ref index);
                Assert.That(result, Is.EqualTo(expected));
            }
        }

        [Test]
        public void ParseBoolean_ParsesBooleans()
        {
            int index = 0;
            Assert.That(GenJsonParser.ParseBoolean("true".AsSpan(), ref index), Is.True);
            index = 0;
            Assert.That(GenJsonParser.ParseBoolean("false".AsSpan(), ref index), Is.False);
        }

        [Test]
        public void MatchesKey_MatchesCorrectKey()
        {
            var json = "\"key\": 123".AsSpan();
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key"), Is.True);
            // MatchesKey consumes the string if it matches
            Assert.That(json[index], Is.EqualTo(':')); // Should point to colon (impl skipped whitespace?)
            // Actually MatchesKey implementation: 
            // Parses string, if matches expected, returns true. 
            // It advances index past the string.
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
        public void SkipValue_SkipsObject()
        {
            var json = "{\"a\": 1, \"b\": [2, 3]} next".AsSpan();
            int index = 0;
            GenJsonParser.SkipValue(json, ref index);
            GenJsonParser.SkipWhitespace(json, ref index);

            // Should be at "next"
            Assert.That(json[index], Is.EqualTo('n'));
        }

        [Test]
        public void SkipValue_SkipsArray()
        {
            var json = "[1, {\"a\": 2}, [3]] next".AsSpan();
            int index = 0;
            GenJsonParser.SkipValue(json, ref index);
            GenJsonParser.SkipWhitespace(json, ref index);
            Assert.That(json[index], Is.EqualTo('n'));
        }

        [Test]
        public void ParseDouble_ParsesExponents()
        {
            var cases = new[] { ("1e2", 100.0), ("1E2", 100.0), ("1.5e2", 150.0), ("1e-1", 0.1) };
            foreach (var (input, expected) in cases)
            {
                int index = 0;
                var result = GenJsonParser.ParseDouble(input.AsSpan(), ref index);
                Assert.That(result, Is.EqualTo(expected).Within(0.000001));
            }
        }

        [Test]
        public void ParseBoolean_ThrowsOnInvalid()
        {
            var cases = new[] { "truee", "tru", "TRUE", "False" };
            foreach (var input in cases)
            {
                int index = 0;
                Assert.Throws<Exception>(() => GenJsonParser.ParseBoolean(input.AsSpan(), ref index), $"Faield to throw on {input}");
            }
        }

        [Test]
        public void ParseNull_ParsesNull()
        {
            int index = 0;
            GenJsonParser.ParseNull("null".AsSpan(), ref index);
            Assert.That(index, Is.EqualTo(4));
        }

        [Test]
        public void ParseNull_ThrowsOnInvalid()
        {
            var cases = new[] { "nul", "NULL", "none" };
            foreach (var input in cases)
            {
                int index = 0;
                Assert.Throws<Exception>(() => GenJsonParser.ParseNull(input.AsSpan(), ref index));
            }
        }
    }
}
