
using GenJson;
using NUnit.Framework;

namespace GenJson.Tests
{
    [GenJson.Enum.AsNumber]
    public enum NumberEnum
    {
        First = 1,
        Second = 2
    }

    [GenJson.Enum.AsText]
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

    [GenJson.Enum.AsText]
    [GenJson.Enum.Fallback(Unknown)]
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
            var json = """{"Value": 3}""";
            var result = NumberEnumClass.FromJson(json);

            Assert.That(result, Is.Null, "Should return null for undefined enum value 3");
        }

        [Test]
        public void TestInvalidEnumValue_AsText()
        {
            var json = """{"Value": "Third"}""";
            var result = TextEnumClass.FromJson(json);

            Assert.That(result, Is.Null, "Should return null for undefined enum value 'Third'");
        }

        [Test]
        public void TestInvalidEnumValue_AsText_NumericString()
        {
            // "3" matches the underlying int type but is not defined in TextEnum
            // Enum.TryParse("3", ...) returns true, so IsDefined is needed to reject it.
            var json = """{"Value": "3"}""";
            var result = TextEnumClass.FromJson(json);

            Assert.That(result, Is.Null, "Should return null for undefined enum value '3' in text mode");
        }

        [Test]
        public void TestInvalidEnumValue_AsTextFallback()
        {
            var json = """{"Value": "3"}""";
            var result = InvalidFallbackEnumClass.FromJson(json);

            Assert.That(result!.Value, Is.EqualTo(InvalidFallbackEnum.Unknown));
        }
    }
}
