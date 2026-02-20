using System;
using NUnit.Framework;

namespace GenJson.Tests
{
    [TestFixture]
    public class TestPropertyName
    {
        [Test]
        public void TestSerialization()
        {
            var value = new RenamedClass { OriginalName = 42, Other = "foo" };
            var json = value.ToJson();
            var expected = """{"new_name":42,"Other":"foo"}""";
            Assert.That(json, Is.EqualTo(expected));

            var utf8Json = value.ToJsonUtf8();
            Assert.That(utf8Json, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(expected)));
        }

        [Test]
        public void TestDeserialization()
        {
            var json = "{\"new_name\":100,\"Other\":\"bar\"}";
            var value = RenamedClass.FromJson(json)!;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.OriginalName, Is.EqualTo(100));
            Assert.That(value.Other, Is.EqualTo("bar"));

            var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);
            var utf8Value = RenamedClass.FromJsonUtf8(utf8Json)!;
            Assert.That(utf8Value, Is.Not.Null);
            Assert.That(utf8Value.OriginalName, Is.EqualTo(100));
            Assert.That(utf8Value.Other, Is.EqualTo("bar"));
        }

        [Test]
        public void TestDeserialization_MissingRenamedProp()
        {
            var json = "{\"Other\":\"bar\"}";
            var value = RenamedClass.FromJson(json);
            Assert.That(value, Is.Null);

            var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);
            var utf8Value = RenamedClass.FromJsonUtf8(utf8Json);
            Assert.That(utf8Value, Is.Null);
        }

        [Test]
        public void TestRecordSerialization()
        {
            var value = new RenamedRecord(123);
            var json = value.ToJson();
            var expected = """{"renamed_prop":123}""";
            Assert.That(json, Is.EqualTo(expected));

            var utf8Json = value.ToJsonUtf8();
            Assert.That(utf8Json, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(expected)));
        }

        [Test]
        public void TestRecordDeserialization()
        {
            var json = "{\"renamed_prop\":456}";
            var value = RenamedRecord.FromJson(json)!;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.Prop, Is.EqualTo(456));

            var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);
            var utf8Value = RenamedRecord.FromJsonUtf8(utf8Json)!;
            Assert.That(utf8Value, Is.Not.Null);
            Assert.That(utf8Value.Prop, Is.EqualTo(456));
        }
    }

    [GenJson]
    partial class RenamedClass
    {
        [GenJsonPropertyName("new_name")]
        public int OriginalName { get; set; }

        public string Other { get; set; } = "";
    }

    [GenJson]
    partial record RenamedRecord([GenJsonPropertyName("renamed_prop")] int Prop);
}
