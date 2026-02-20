using NUnit.Framework;
using System.Collections.Generic;

namespace GenJson.Tests
{
    [TestFixture]
    public class TestGenJsonIgnoreProperty
    {
        [Test]
        public void TestRecordWithIgnoredParam()
        {
            // As per requirements: "not parameter of a record otherwise it won't compile"
            // So this property should NOT be ignored because it is a constructor parameter
            var obj = new RecordWithIgnoredParam(1, "test");
            var json = obj.ToJson();
            var expected = """{"Id":1,"Name":"test"}""";
            Assert.That(json, Is.EqualTo(expected));

            var utf8Json = obj.ToJsonUtf8();
            Assert.That(utf8Json, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(expected)));

            var deserialized = RecordWithIgnoredParam.FromJson(json)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Id, Is.EqualTo(1));
            Assert.That(deserialized.Name, Is.EqualTo("test"));

            var utf8Deserialized = RecordWithIgnoredParam.FromJsonUtf8(utf8Json)!;
            Assert.That(utf8Deserialized, Is.Not.Null);
            Assert.That(utf8Deserialized.Id, Is.EqualTo(1));
            Assert.That(utf8Deserialized.Name, Is.EqualTo("test"));
        }

        [Test]
        public void TestMixedProperties()
        {
            var obj = new MixedClass { Normal = 10, Ignored = 99 };
            var json = obj.ToJson();
            var expected = """{"Normal":10}""";
            Assert.That(json, Is.EqualTo(expected));

            var utf8Json = obj.ToJsonUtf8();
            Assert.That(utf8Json, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(expected)));

            var inputJson = """{"Normal":20,"Ignored":888,"ReadOnly":999}""";
            var deserialized = MixedClass.FromJson(inputJson)!;

            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Normal, Is.EqualTo(20));
            Assert.That(deserialized.Ignored, Is.EqualTo(0)); // Should not be set

            var utf8InputJson = System.Text.Encoding.UTF8.GetBytes(inputJson);
            var utf8Deserialized = MixedClass.FromJsonUtf8(utf8InputJson)!;
            Assert.That(utf8Deserialized, Is.Not.Null);
            Assert.That(utf8Deserialized.Normal, Is.EqualTo(20));
            Assert.That(utf8Deserialized.Ignored, Is.EqualTo(0));
        }
    }

    [GenJson]
    public partial record RecordWithIgnoredParam([property: GenJsonIgnore] int Id, string Name);

    [GenJson]
    public partial class MixedClass
    {
        public int Normal { get; set; }

        [GenJsonIgnore]
        public int Ignored { get; set; }

        public int ReadOnly => 42;
    }
}
