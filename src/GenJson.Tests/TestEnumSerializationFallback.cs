
using GenJson;
using NUnit.Framework;

namespace GenJson.Tests
{
    [GenJson.Enum.AsText]
    public enum FallbackTextEnum
    {
        First = 1,
        Second = 2
    }

    [GenJson]
    public partial class FallbackTextEnumClass
    {
        public FallbackTextEnum Value { get; set; }
    }

    public class TestEnumSerializationFallback
    {
        [Test]
        public void TestInvalidEnumSerialization_FallsBackToQuotedNumber()
        {
            // 3 is not defined in FallbackTextEnum
            var obj = new FallbackTextEnumClass { Value = (FallbackTextEnum)3 };
            var json = obj.ToJson();

            // Requested behavior: formatted as quoted number "3"
            Assert.That(json, Is.EqualTo("""{"Value":"3"}"""));
        }
    }
}
