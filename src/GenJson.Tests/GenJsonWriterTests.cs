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
    }
}
