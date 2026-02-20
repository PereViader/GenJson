
using GenJson;
using NUnit.Framework;

namespace GenJson.Tests
{
    [GenJsonEnumAsNumber]
    public enum NumberEnum
    {
        First = 1,
        Second = 2
    }

    [GenJsonEnumAsText]
    public enum TextEnum
    {
        First = 1,
        Second = 2
    }

    [GenJson]
    public partial class NumberEnumClass
    {
        public NumberEnum Value { get; set; }
    }

    [GenJson]
    public partial class TextEnumClass
    {
        public TextEnum Value { get; set; }
    }

    [GenJsonEnumAsText]
    [GenJsonEnumFallback(Unknown)]
    public enum InvalidFallbackEnum
    {
        Unknown = -1,
        One = 1,
        Two = 2,
    }

    [GenJson]
    public partial class InvalidFallbackEnumClass
    {
        public InvalidFallbackEnum Value { get; set; }
    }

    public class TestEnumInvalidValues
    {
        [Test]
        public void TestInvalidEnumValue_AsNumber()
        {
            var json = """{"Value":3}""";
            Assert.That(NumberEnumClass.FromJson(json), Is.Null, "Should return null for undefined enum value 3");
            var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);
            Assert.That(NumberEnumClass.FromJsonUtf8(utf8Json), Is.Null, "Should return null for undefined enum value 3");
        }

        [Test]
        public void TestInvalidEnumValue_AsText()
        {
            var json = """{"Value":"Third"}""";
            Assert.That(TextEnumClass.FromJson(json), Is.Null, "Should return null for undefined enum value 'Third'");
            var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);
            Assert.That(TextEnumClass.FromJsonUtf8(utf8Json), Is.Null, "Should return null for undefined enum value 'Third'");
        }

        [Test]
        public void TestInvalidEnumValue_AsText_NumericString()
        {
            // "3" matches the underlying int type but is not defined in TextEnum
            // Enum.TryParse("3", ...) returns true, so IsDefined is needed to reject it.
            var json = """{"Value":"3"}""";
            Assert.That(TextEnumClass.FromJson(json), Is.Null, "Should return null for undefined enum value '3' in text mode");
            var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);
            Assert.That(TextEnumClass.FromJsonUtf8(utf8Json), Is.Null, "Should return null for undefined enum value '3' in text mode");
        }

        [Test]
        public void TestInvalidEnumValue_AsTextFallback()
        {
            var json = """{"Value":"3"}""";
            var result = InvalidFallbackEnumClass.FromJson(json);

            Assert.That(result!.Value, Is.EqualTo(InvalidFallbackEnum.Unknown));

            var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);
            var resultUtf8 = InvalidFallbackEnumClass.FromJsonUtf8(utf8Json);
            Assert.That(resultUtf8!.Value, Is.EqualTo(InvalidFallbackEnum.Unknown));
        }
    }
}
