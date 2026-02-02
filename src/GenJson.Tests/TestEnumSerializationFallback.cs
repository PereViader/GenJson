
using System.Collections.Generic;
using GenJson;
using NUnit.Framework;

namespace GenJson.Tests
{
    [GenJsonEnumAsText]
    public enum NoFallbackTextEnum
    {
        First = 1,
        Second = 2
    }

    [GenJson]
    public partial class NoFallbackTextEnumClass
    {
        public required NoFallbackTextEnum Value { get; init; }
    }

    [GenJsonEnumAsText]
    [GenJsonEnumFallback(Unknown)]
    public enum FallbackTextEnum
    {
        Unknown = -1,
        First = 1,
        Second = 2
    }

    [GenJson]
    public partial class FallbackDictionaryTextEnumClass
    {
        public required Dictionary<FallbackTextEnum, int> Value { get; init; }
    }

    [GenJson]
    public partial class NoFallbackDictionaryTextEnumClass
    {
        public required Dictionary<NoFallbackTextEnum, int> Value { get; init; }
    }


    [GenJsonEnumFallback(Unknown)]
    public enum FallbackNumericEnum
    {
        Unknown = 0,
        One = 1,
        Two = 2
    }

    [GenJson]
    public partial class FallbackDictionaryNumericEnumClass
    {
        public required Dictionary<FallbackNumericEnum, int> Value { get; init; }
    }

    public class TestEnumSerializationFallback
    {
        [Test]
        public void TestInvalidEnumSerialization_WithoutFallback_SerializesUnderlyingValue()
        {
            // 3 is not defined
            var obj = new NoFallbackTextEnumClass { Value = (NoFallbackTextEnum)3 };
            var json = obj.ToJson();

            // Requested behavior: formatted as quoted number "3"
            Assert.That(json, Is.EqualTo("""{"Value":"3"}"""));
        }

        [Test]
        public void TestInvalidEnumOnDictioanry_WithFallback_SkipsTheKeyValuePair()
        {
            var json = """{"Value":{"Zero":0,"First":1,"Second":2,"Third":3}}""";
            var obj = FallbackDictionaryTextEnumClass.FromJson(json);
            Assert.That(obj, Is.Not.Null);
            var result = obj!.ToJson();
            Assert.That(result, Is.EqualTo("""{"Value":{"First":1,"Second":2}}"""));
        }

        [Test]
        public void TestInvalidEnumOnDictioanry_NoFallback_DoesNotDeserialize()
        {
            var json = """{"Value":{"Zero":0,"First":1,"Second":2,"Third":3}}""";
            var obj = NoFallbackDictionaryTextEnumClass.FromJson(json);
            Assert.That(obj, Is.Null);
        }

        [Test]
        public void TestNumericEnumOnDictionary_WithFallback_SkipsInvalidKeys()
        {
            // "1" -> One (valid)
            // "2" -> Two (valid)
            // "99" -> Invalid (should be skipped)
            var json = """{"Value":{"1":10,"2":20,"99":30}}""";
            var obj = FallbackDictionaryNumericEnumClass.FromJson(json);

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj!.Value, Has.Count.EqualTo(2));
            Assert.That(obj.Value.ContainsKey(FallbackNumericEnum.One));
            Assert.That(obj.Value.ContainsKey(FallbackNumericEnum.Two));
        }

        [Test]
        public void TestEnumOnDictionary_AllInvalidKeys_ReturnsEmptyDictionary()
        {
            var json = """{"Value":{"99":1,"100":2}}""";
            var obj = FallbackDictionaryNumericEnumClass.FromJson(json);

            Assert.That(obj, Is.Not.Null);
            Assert.That(obj!.Value, Is.Empty);
        }
    }
}
