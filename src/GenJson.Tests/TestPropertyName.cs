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

        [Test]
        public void TestFieldSerialization()
        {
            var value = new RenamedFieldClass { OriginalName = 42, Other = "foo" };
            var json = value.ToJson();
            var expected = """{"new_name":42,"Other":"foo"}""";
            Assert.That(json, Is.EqualTo(expected));

            var utf8Json = value.ToJsonUtf8();
            Assert.That(utf8Json, Is.EqualTo(System.Text.Encoding.UTF8.GetBytes(expected)));
        }

        [Test]
        public void TestFieldDeserialization()
        {
            var json = "{\"new_name\":100,\"Other\":\"bar\"}";
            var value = RenamedFieldClass.FromJson(json)!;
            Assert.That(value, Is.Not.Null);
            Assert.That(value.OriginalName, Is.EqualTo(100));
            Assert.That(value.Other, Is.EqualTo("bar"));

            var utf8Json = System.Text.Encoding.UTF8.GetBytes(json);
            var utf8Value = RenamedFieldClass.FromJsonUtf8(utf8Json)!;
            Assert.That(utf8Value, Is.Not.Null);
            Assert.That(utf8Value.OriginalName, Is.EqualTo(100));
            Assert.That(utf8Value.Other, Is.EqualTo("bar"));
        }

        [Test]
        public void TestCamelCase()
        {
            var value = new CamelCaseClass { OriginalName = 42, OtherProperty = "foo", CustomName = "bar", SomeField = 10 };
            var json = value.ToJson();
            var expected = """{"originalName":42,"otherProperty":"foo","ExplicitlyNamed":"bar","someField":10}""";
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = CamelCaseClass.FromJson(expected)!;
            Assert.That(deserialized.OriginalName, Is.EqualTo(42));
            Assert.That(deserialized.OtherProperty, Is.EqualTo("foo"));
            Assert.That(deserialized.CustomName, Is.EqualTo("bar"));
            Assert.That(deserialized.SomeField, Is.EqualTo(10));
        }

        [Test]
        public void TestCamelCaseRecord()
        {
            var value = new CamelCaseRecord(42, "bar");
            var json = value.ToJson();
            var expected = """{"originalName":42,"ExplicitlyNamed":"bar"}""";
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = CamelCaseRecord.FromJson(expected)!;
            Assert.That(deserialized.OriginalName, Is.EqualTo(42));
            Assert.That(deserialized.CustomName, Is.EqualTo("bar"));
        }

        [Test]
        public void TestKebabCaseLower()
        {
            var value = new KebabCaseLowerClass { OriginalName = 42, OtherProperty = "foo" };
            var json = value.ToJson();
            var expected = """{"original-name":42,"other-property":"foo"}""";
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = KebabCaseLowerClass.FromJson(expected)!;
            Assert.That(deserialized.OriginalName, Is.EqualTo(42));
            Assert.That(deserialized.OtherProperty, Is.EqualTo("foo"));
        }

        [Test]
        public void TestKebabCaseUpper()
        {
            var value = new KebabCaseUpperClass { OriginalName = 42, OtherProperty = "foo" };
            var json = value.ToJson();
            var expected = """{"ORIGINAL-NAME":42,"OTHER-PROPERTY":"foo"}""";
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = KebabCaseUpperClass.FromJson(expected)!;
            Assert.That(deserialized.OriginalName, Is.EqualTo(42));
            Assert.That(deserialized.OtherProperty, Is.EqualTo("foo"));
        }

        [Test]
        public void TestSnakeCaseLower()
        {
            var value = new SnakeCaseLowerClass { OriginalName = 42, OtherProperty = "foo" };
            var json = value.ToJson();
            var expected = """{"original_name":42,"other_property":"foo"}""";
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = SnakeCaseLowerClass.FromJson(expected)!;
            Assert.That(deserialized.OriginalName, Is.EqualTo(42));
            Assert.That(deserialized.OtherProperty, Is.EqualTo("foo"));
        }

        [Test]
        public void TestSnakeCaseUpper()
        {
            var value = new SnakeCaseUpperClass { OriginalName = 42, OtherProperty = "foo" };
            var json = value.ToJson();
            var expected = """{"ORIGINAL_NAME":42,"OTHER_PROPERTY":"foo"}""";
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = SnakeCaseUpperClass.FromJson(expected)!;
            Assert.That(deserialized.OriginalName, Is.EqualTo(42));
            Assert.That(deserialized.OtherProperty, Is.EqualTo("foo"));
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

    [GenJson]
    partial class RenamedFieldClass
    {
        [GenJsonPropertyName("new_name")]
        public int OriginalName;

        public string Other = "";
    }

    [GenJson(NamingPolicy = GenJsonNamingPolicy.CamelCase)]
    partial class CamelCaseClass
    {
        public int OriginalName { get; set; }
        public string OtherProperty { get; set; } = "";
        
        [GenJsonPropertyName("ExplicitlyNamed")]
        public string CustomName { get; set; } = "";

        public int SomeField;
    }

    [GenJson(NamingPolicy = GenJsonNamingPolicy.CamelCase)]
    partial record CamelCaseRecord(int OriginalName, [GenJsonPropertyName("ExplicitlyNamed")] string CustomName);

    [GenJson(NamingPolicy = GenJsonNamingPolicy.KebabCaseLower)]
    partial class KebabCaseLowerClass
    {
        public int OriginalName { get; set; }
        public string OtherProperty { get; set; } = "";
    }

    [GenJson(NamingPolicy = GenJsonNamingPolicy.KebabCaseUpper)]
    partial class KebabCaseUpperClass
    {
        public int OriginalName { get; set; }
        public string OtherProperty { get; set; } = "";
    }

    [GenJson(NamingPolicy = GenJsonNamingPolicy.SnakeCaseLower)]
    partial class SnakeCaseLowerClass
    {
        public int OriginalName { get; set; }
        public string OtherProperty { get; set; } = "";
    }

    [GenJson(NamingPolicy = GenJsonNamingPolicy.SnakeCaseUpper)]
    partial class SnakeCaseUpperClass
    {
        public int OriginalName { get; set; }
        public string OtherProperty { get; set; } = "";
    }
}
