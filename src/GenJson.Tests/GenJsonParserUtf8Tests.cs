using NUnit.Framework;
using System;
using System.Text;

namespace GenJson.Tests
{
    [TestFixture]
    public class GenJsonParserUtf8Tests
    {
        private static ReadOnlySpan<byte> Utf8(string s) => Encoding.UTF8.GetBytes(s);

        [Test]
        public void TryParseNumerics_Utf8_Valid()
        {
            var json = Utf8("123");
            int index = 0;

            index = 0;
            Assert.That(GenJsonParser.TryParseInt(json, ref index, out int i), Is.True);
            Assert.That(i, Is.EqualTo(123));

            index = 0;
            Assert.That(GenJsonParser.TryParseUInt(json, ref index, out uint ui), Is.True);
            Assert.That(ui, Is.EqualTo(123u));

            index = 0;
            Assert.That(GenJsonParser.TryParseShort(json, ref index, out short s), Is.True);
            Assert.That(s, Is.EqualTo(123));

            index = 0;
            Assert.That(GenJsonParser.TryParseUShort(json, ref index, out ushort us), Is.True);
            Assert.That(us, Is.EqualTo(123));

            index = 0;
            Assert.That(GenJsonParser.TryParseByte(json, ref index, out byte b), Is.True);
            Assert.That(b, Is.EqualTo(123));

            index = 0;
            Assert.That(GenJsonParser.TryParseSByte(json, ref index, out sbyte sb), Is.True);
            Assert.That(sb, Is.EqualTo(123));

            index = 0;
            Assert.That(GenJsonParser.TryParseLong(json, ref index, out long l), Is.True);
            Assert.That(l, Is.EqualTo(123L));

            index = 0;
            Assert.That(GenJsonParser.TryParseULong(json, ref index, out ulong ul), Is.True);
            Assert.That(ul, Is.EqualTo(123ul));

            index = 0;
            Assert.That(GenJsonParser.TryParseDouble(json, ref index, out double d), Is.True);
            Assert.That(d, Is.EqualTo(123.0));

            index = 0;
            Assert.That(GenJsonParser.TryParseFloat(json, ref index, out float f), Is.True);
            Assert.That(f, Is.EqualTo(123.0f));

            index = 0;
            Assert.That(GenJsonParser.TryParseDecimal(json, ref index, out decimal dec), Is.True);
            Assert.That(dec, Is.EqualTo(123.0m));
        }

        [Test]
        public void TryParseNumerics_Utf8_Nullable_Valid()
        {
            var json = Utf8("123");
            int index = 0;

            index = 0;
            Assert.That(GenJsonParser.TryParseInt(json, ref index, out int? i), Is.True);
            Assert.That(i, Is.EqualTo(123));

            index = 0;
            Assert.That(GenJsonParser.TryParseUInt(json, ref index, out uint? ui), Is.True);
            Assert.That(ui, Is.EqualTo(123u));

            index = 0;
            Assert.That(GenJsonParser.TryParseShort(json, ref index, out short? s), Is.True);
            Assert.That(s, Is.EqualTo(123));

            index = 0;
            Assert.That(GenJsonParser.TryParseUShort(json, ref index, out ushort? us), Is.True);
            Assert.That(us, Is.EqualTo(123));

            index = 0;
            Assert.That(GenJsonParser.TryParseByte(json, ref index, out byte? b), Is.True);
            Assert.That(b, Is.EqualTo(123));

            index = 0;
            Assert.That(GenJsonParser.TryParseSByte(json, ref index, out sbyte? sb), Is.True);
            Assert.That(sb, Is.EqualTo(123));

            index = 0;
            Assert.That(GenJsonParser.TryParseLong(json, ref index, out long? l), Is.True);
            Assert.That(l, Is.EqualTo(123L));

            index = 0;
            Assert.That(GenJsonParser.TryParseULong(json, ref index, out ulong? ul), Is.True);
            Assert.That(ul, Is.EqualTo(123ul));

            index = 0;
            Assert.That(GenJsonParser.TryParseDouble(json, ref index, out double? d), Is.True);
            Assert.That(d, Is.EqualTo(123.0));

            index = 0;
            Assert.That(GenJsonParser.TryParseFloat(json, ref index, out float? f), Is.True);
            Assert.That(f, Is.EqualTo(123.0f));

            index = 0;
            Assert.That(GenJsonParser.TryParseDecimal(json, ref index, out decimal? dec), Is.True);
            Assert.That(dec, Is.EqualTo(123.0m));
        }

        [Test]
        public void TryParseNumerics_Utf8_Invalid()
        {
            var json = Utf8("abc");
            int index = 0;

            Assert.That(GenJsonParser.TryParseInt(json, ref index, out int i), Is.False);
            Assert.That(i, Is.EqualTo(0));
            Assert.That(GenJsonParser.TryParseInt(json, ref index, out int? ni), Is.False);
            Assert.That(ni, Is.Null);

            Assert.That(GenJsonParser.TryParseUInt(json, ref index, out uint ui), Is.False);
            Assert.That(GenJsonParser.TryParseUInt(json, ref index, out uint? nui), Is.False);

            Assert.That(GenJsonParser.TryParseShort(json, ref index, out short s), Is.False);
            Assert.That(GenJsonParser.TryParseShort(json, ref index, out short? ns), Is.False);

            Assert.That(GenJsonParser.TryParseUShort(json, ref index, out ushort us), Is.False);
            Assert.That(GenJsonParser.TryParseUShort(json, ref index, out ushort? nus), Is.False);

            Assert.That(GenJsonParser.TryParseByte(json, ref index, out byte b), Is.False);
            Assert.That(GenJsonParser.TryParseByte(json, ref index, out byte? nb), Is.False);

            Assert.That(GenJsonParser.TryParseSByte(json, ref index, out sbyte sb), Is.False);
            Assert.That(GenJsonParser.TryParseSByte(json, ref index, out sbyte? nsb), Is.False);

            Assert.That(GenJsonParser.TryParseLong(json, ref index, out long l), Is.False);
            Assert.That(GenJsonParser.TryParseLong(json, ref index, out long? nl), Is.False);

            Assert.That(GenJsonParser.TryParseULong(json, ref index, out ulong ul), Is.False);
            Assert.That(GenJsonParser.TryParseULong(json, ref index, out ulong? nul), Is.False);

            Assert.That(GenJsonParser.TryParseDouble(json, ref index, out double d), Is.False);
            Assert.That(GenJsonParser.TryParseDouble(json, ref index, out double? nd), Is.False);

            Assert.That(GenJsonParser.TryParseFloat(json, ref index, out float f), Is.False);
            Assert.That(GenJsonParser.TryParseFloat(json, ref index, out float? nf), Is.False);

            Assert.That(GenJsonParser.TryParseDecimal(json, ref index, out decimal dec), Is.False);
            Assert.That(GenJsonParser.TryParseDecimal(json, ref index, out decimal? ndec), Is.False);
        }
        [Test]
        public void TryParseString_Utf8_And_Unescape()
        {
            var json = Utf8("\"hello\\nworld\\\"\\b\\f\\r\\t\\u0031\"");
            int index = 0;
            Assert.That(GenJsonParser.TryParseString(json, ref index, out string? str), Is.True);
            Assert.That(str, Is.EqualTo("hello\nworld\"\b\f\r\t1"));

            var unescapedFast = GenJsonParser.UnescapeStringUtf8(Utf8("fast"));
            Assert.That(unescapedFast, Is.EqualTo("fast"));

            var unescapedSlow = GenJsonParser.UnescapeStringUtf8(Utf8("slow\\nslow"));
            Assert.That(unescapedSlow, Is.EqualTo("slow\nslow"));

            var unescapedAll = GenJsonParser.UnescapeStringUtf8(Utf8("a\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0031z"));
            Assert.That(unescapedAll, Is.EqualTo("a\"\\/\b\f\n\r\t1z"));
        }

        [Test]
        public void TryParseBoolean_Utf8_ValidAndInvalid()
        {
            var json = Utf8("true,false,tru,fals,truex,falsey");
            int index = 0;

            Assert.That(GenJsonParser.TryParseBoolean(json, ref index, out bool b), Is.True);
            Assert.That(b, Is.True);
            index++; // skip ,

            Assert.That(GenJsonParser.TryParseBoolean(json, ref index, out bool? nb), Is.True);
            Assert.That(nb, Is.False);
            index++; // skip ,

            var tempIndex = index;
            Assert.That(GenJsonParser.TryParseBoolean(json, ref tempIndex, out b), Is.False); // tru
            tempIndex += 4; // skip tru,
            Assert.That(GenJsonParser.TryParseBoolean(json, ref tempIndex, out nb), Is.False); // fals
            tempIndex += 5; // skip fals,
            Assert.That(GenJsonParser.TryParseBoolean(json, ref tempIndex, out b), Is.False); // truex
            tempIndex += 6; // skip truex,
            Assert.That(GenJsonParser.TryParseBoolean(json, ref tempIndex, out nb), Is.False); // falsey

            // EOF valid
            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean(Utf8("true"), ref index, out b), Is.True);
            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean(Utf8("true"), ref index, out nb), Is.True);
            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean(Utf8("false"), ref index, out b), Is.True);
            index = 0;
            Assert.That(GenJsonParser.TryParseBoolean(Utf8("false"), ref index, out nb), Is.True);
        }

        [Test]
        public void TryParseChar_Utf8_ValidAndInvalid()
        {
            var json = Utf8("\"A\",\"AB\",\"\"");
            int index = 0;

            Assert.That(GenJsonParser.TryParseChar(json, ref index, out char c), Is.True);
            Assert.That(c, Is.EqualTo('A'));
            index++;

            Assert.That(GenJsonParser.TryParseChar(json, ref index, out char? nc), Is.False);
            index += 4; // Skip ,"AB"

            Assert.That(GenJsonParser.TryParseChar(json, ref index, out c), Is.False);
        }

        [Test]
        public void TryParseNull_Utf8()
        {
            var json = Utf8("null,nul,nullx");
            int index = 0;
            Assert.That(GenJsonParser.TryParseNull(json, ref index), Is.True);
            index++;
            Assert.That(GenJsonParser.TryParseNull(json, ref index), Is.False);
        }

        [Test]
        public void TrySkipString_Utf8()
        {
            var json = Utf8("\"simple\",\"esc\\\"aped\"");
            int index = 0;
            Assert.That(GenJsonParser.TrySkipString(json, ref index), Is.True);
            index++;
            Assert.That(GenJsonParser.TrySkipString(json, ref index), Is.True);
        }

        [Test]
        public void TrySkipValue_Utf8()
        {
            var json = Utf8("{\"a\":[1,true,false,null,\"s\"]}");
            int index = 0;
            Assert.That(GenJsonParser.TrySkipValue(json, ref index), Is.True);

            // Incomplete
            index = 0;
            Assert.That(GenJsonParser.TrySkipValue(Utf8("{"), ref index), Is.False);
            index = 0;
            Assert.That(GenJsonParser.TrySkipValue(Utf8("["), ref index), Is.False);
        }

        [Test]
        public void CountItems_Utf8()
        {
            Assert.That(GenJsonParser.CountListItems(Utf8("[1,2,3]"), 1), Is.EqualTo(3));
            Assert.That(GenJsonParser.CountListItems(Utf8("[]"), 1), Is.EqualTo(0));
            Assert.That(GenJsonParser.CountListItems(Utf8("[1,"), 1), Is.EqualTo(1));

            Assert.That(GenJsonParser.CountDictionaryItems(Utf8("{\"a\":1,\"b\":2}"), 1), Is.EqualTo(2));
            Assert.That(GenJsonParser.CountDictionaryItems(Utf8("{}"), 1), Is.EqualTo(0));
            Assert.That(GenJsonParser.CountDictionaryItems(Utf8("{\"a\":1,"), 1), Is.EqualTo(1));
        }

        [Test]
        public void MatchesKey_ByteSpan_Utf8Multibyte()
        {
            var json = Utf8("\"á🚀\\n\":1");
            var expectedUtf8 = Utf8("á🚀\n");
            int index = 0;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "á🚀\n", expectedUtf8), Is.True);
        }

        [Test]
        public void MatchesKey_Utf8()
        {
            var json = Utf8("{\"key\":1,\"esc\\\"key\":2,\"all\\\"\\\\\\/\\b\\f\\n\\r\\t\\u0031z\":3}");
            int index = 1; // skip {
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "key"), Is.True);
            index += 3; // skip :1,
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "esc\"key"), Is.True);
            index += 3; // skip :2,
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "all\"\\/\b\f\n\r\t1z"), Is.True);

            index = 1;
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "wrong"), Is.False);

            index = 10; // Point to "esc...
            Assert.That(GenJsonParser.MatchesKey(json, ref index, "wrong\\n_esc"), Is.False);
        }

        [Test]
        public void TryFindProperty_Utf8()
        {
            var json = Utf8("{\"a\":1,\"b\":2}");
            Assert.That(GenJsonParser.TryFindProperty(json, 0, "b", out int valIndex), Is.True);
            Assert.That(valIndex, Is.GreaterThan(0));

            Assert.That(GenJsonParser.TryFindProperty(json, 0, "c", out _), Is.False);
        }
    }
}
