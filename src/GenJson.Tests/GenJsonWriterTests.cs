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
    }
}
