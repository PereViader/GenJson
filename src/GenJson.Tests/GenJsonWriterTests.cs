using NUnit.Framework;
using System;

namespace GenJson.Tests
{
    [TestFixture]
    public class GenJsonWriterTests
    {
        [Test]
        public void WriteString_WritesSimpleString()
        {
            Span<char> buffer = stackalloc char[100];
            int index = 0;
            GenJsonWriter.WriteString(buffer, ref index, "hello");
            var result = buffer.Slice(0, index).ToString();
            Assert.That(result, Is.EqualTo("\"hello\""));
        }

        [Test]
        public void WriteString_EscapesSpecialChars()
        {
            Span<char> buffer = stackalloc char[100];
            int index = 0;
            GenJsonWriter.WriteString(buffer, ref index, "line1\nline2\t\"quoted\"");
            var result = buffer.Slice(0, index).ToString();
            Assert.That(result, Is.EqualTo("\"line1\\nline2\\t\\\"quoted\\\"\""));
        }

        [Test]
        public void WriteString_WritesControlChars()
        {
            Span<char> buffer = stackalloc char[100];
            int index = 0;
            // Test ascii 0-31 (except \n, \t, etc which have special handling)
            // \b \f \n \r \t are special. Others are \u00xx
            var input = "\u0000\u001f";
            GenJsonWriter.WriteString(buffer, ref index, input);
            var result = buffer.Slice(0, index).ToString();
            Assert.That(result, Is.EqualTo("\"\\u0000\\u001f\""));
        }

        [Test]
        public void WriteString_WritesUnicode()
        {
            Span<char> buffer = stackalloc char[100];
            int index = 0;
            var input = "🚀"; // Surrogate pair
            GenJsonWriter.WriteString(buffer, ref index, input);
            var result = buffer.Slice(0, index).ToString();
            Assert.That(result, Is.EqualTo("\"🚀\""));
        }
        [Test]
        public void WriteString_Utf8_WritesSimpleString()
        {
            Span<byte> buffer = stackalloc byte[100];
            int index = 0;
            GenJsonWriter.WriteString(buffer, ref index, "hello");
            var result = System.Text.Encoding.UTF8.GetString(buffer.Slice(0, index));
            Assert.That(result, Is.EqualTo("\"hello\""));
        }

        [Test]
        public void WriteString_Utf8_EscapesSpecialChars()
        {
            Span<byte> buffer = stackalloc byte[100];
            int index = 0;
            GenJsonWriter.WriteString(buffer, ref index, "line1\nline2\t\"quoted\"\b\f\r");
            var result = System.Text.Encoding.UTF8.GetString(buffer.Slice(0, index));
            Assert.That(result, Is.EqualTo("\"line1\\nline2\\t\\\"quoted\\\"\\b\\f\\r\""));
        }

        [Test]
        public void WriteString_Utf8_WritesControlChars()
        {
            Span<byte> buffer = stackalloc byte[100];
            int index = 0;
            var input = "\u0000\u001f";
            GenJsonWriter.WriteString(buffer, ref index, input);
            var result = System.Text.Encoding.UTF8.GetString(buffer.Slice(0, index));
            // Expecting standard control formatting: \u0000 and \u001f
            Assert.That(result, Is.EqualTo("\"\\u0000\\u001f\""));
        }
        // ── Bug #14: GenJsonWriter.WriteString (char) — surrogate pair handling ──────────────────

        [Test]
        public void WriteString_SurrogatePair_ProducesValidJson()
        {
            // A surrogate pair like 😁 (U+1F601) requires \\uD83D\\uDE01 in JSON.
            // The current writer emits the surrogate chars as-is (technically invalid JSON).
            // This test captures expected output of a correct implementation.
            var emoji = "😁"; // U+1F601, surrogate pair in C# string
            Span<char> buffer = stackalloc char[32];
            int index = 0;
            GenJsonWriter.WriteString(buffer, ref index, emoji);
            var result = buffer.Slice(0, index).ToString();

            // The round-trip must at minimum survive re-parsing
            int parseIdx = 0;
            bool parsed = GenJsonParser.TryParseString(result.AsSpan(), ref parseIdx, out string? roundtrip);
            Assert.That(parsed, Is.True, "Written surrogate pair must be re-parseable as valid JSON");
            Assert.That(roundtrip, Is.EqualTo(emoji), "Round-trip must preserve the emoji (Bug #14)");
        }

        [Test]
        public void WriteString_IsolatedHighSurrogate_IndexAdvancesCorrectly()
        {
            // An isolated high surrogate (no matching low surrogate) is pathological input.
            // At minimum the writer must not throw or write past the buffer.
            var badString = "\uD83D"; // isolated high surrogate
            Span<char> buffer = stackalloc char[32];
            int index = 0;
            try { GenJsonWriter.WriteString(buffer, ref index, badString); }
            catch (Exception ex) { Assert.Fail($"Writer must not throw for isolated surrogates: {ex.Message}"); }
            Assert.That(index, Is.GreaterThan(0).And.LessThanOrEqualTo(32),
                "Writer must stay within buffer bounds even for isolated surrogates");
        }

        // ── Bug #13: GetSize(char) includes quotes; GetSize(span) adds quotes separately ─────────

        [Test]
        public void GetSize_Char_IncludesQuotes_SpanAddsSeparately()
        {
            // GetSize(char c) returns the total quoted size, e.g. "A" = 3.
            // GetSize(ReadOnlySpan<char>) starts at 2 (for ""), then adds per-char costs.
            // This is by design — but verifying consistency with actual write output.
            char c = 'A';
            int charSize = GenJsonSizeHelper.GetSize(c); // should be 3 ("A")

            string singleCharStr = c.ToString();
            int spanSize = GenJsonSizeHelper.GetSize(singleCharStr.AsSpan()); // should also be 3

            Assert.That(charSize, Is.EqualTo(spanSize),
                "GetSize(char) and GetSize(span) must agree on quoted size for single-char strings");

            Span<char> buffer = stackalloc char[32];
            int index = 0;
            GenJsonWriter.WriteString(buffer, ref index, singleCharStr);
            Assert.That(charSize, Is.EqualTo(index), "GetSize(char) must match actual writer output size");
        }
    }
}
