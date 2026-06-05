using System;
using System.Collections.Generic;
using GenJson;
using GenJson.Tests;
using NUnit.Framework;

namespace GenJson.Tests
{
    [GenJsonSerializable(typeof(List<RootTestItem>))]
    public static partial class RootListSerializer
    {
    }

    [GenJsonSerializable(typeof(RootTestItem[]))]
    public static partial class RootArraySerializer
    {
    }

    [GenJsonSerializable(typeof(Dictionary<string, RootTestItem>))]
    public static partial class RootDictionarySerializer
    {
    }

    [GenJsonSerializable(typeof(List<int>))]
    public static partial class RootIntListSerializer
    {
    }

    [GenJsonSerializable(typeof(Dictionary<string, int>))]
    public static partial class RootStringIntDictionarySerializer
    {
    }

    [GenJson]
    public partial class RootTestItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [TestFixture]
    public class TestRootCollections
    {
        [Test]
        public void TestRootList()
        {
            var list = new List<RootTestItem>
            {
                new() { Id = 1, Name = "Item1" },
                new() { Id = 2, Name = "Item2" }
            };

            var expected = """[{"Id":1,"Name":"Item1"},{"Id":2,"Name":"Item2"}]""";

            // Test Extension Method
            var jsonExt = list.ToJson();
            Assert.That(jsonExt, Is.EqualTo(expected));

            // Test Static Method
            var jsonGen = RootListSerializer.ToJson(list);
            Assert.That(jsonGen, Is.EqualTo(expected));

            // Test Deserialization (No Generics!)
            var parsed = RootListSerializer.FromJson(expected);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.Count, Is.EqualTo(2));
            Assert.That(parsed[0].Id, Is.EqualTo(1));
            Assert.That(parsed[0].Name, Is.EqualTo("Item1"));
            Assert.That(parsed[1].Id, Is.EqualTo(2));
            Assert.That(parsed[1].Name, Is.EqualTo("Item2"));

            // Test UTF-8
            var utf8Bytes = list.ToJsonUtf8();
            var parsedUtf8 = RootListSerializer.FromJsonUtf8(utf8Bytes);
            Assert.That(parsedUtf8, Is.Not.Null);
            Assert.That(parsedUtf8.Count, Is.EqualTo(2));
            Assert.That(parsedUtf8[0].Id, Is.EqualTo(1));
            Assert.That(parsedUtf8[1].Name, Is.EqualTo("Item2"));
        }

        [Test]
        public void TestRootArray()
        {
            var array = new RootTestItem[]
            {
                new() { Id = 10, Name = "A" },
                new() { Id = 20, Name = "B" }
            };

            var expected = """[{"Id":10,"Name":"A"},{"Id":20,"Name":"B"}]""";

            var json = array.ToJson();
            Assert.That(json, Is.EqualTo(expected));

            var parsed = RootArraySerializer.FromJson(expected);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.Length, Is.EqualTo(2));
            Assert.That(parsed[0].Id, Is.EqualTo(10));
            Assert.That(parsed[1].Name, Is.EqualTo("B"));

            // UTF-8
            var utf8Bytes = array.ToJsonUtf8();
            var parsedUtf8 = RootArraySerializer.FromJsonUtf8(utf8Bytes);
            Assert.That(parsedUtf8, Is.Not.Null);
            Assert.That(parsedUtf8.Length, Is.EqualTo(2));
            Assert.That(parsedUtf8[0].Id, Is.EqualTo(10));
            Assert.That(parsedUtf8[1].Name, Is.EqualTo("B"));
        }

        [Test]
        public void TestRootDictionary()
        {
            var dict = new Dictionary<string, RootTestItem>
            {
                { "item_first", new() { Id = 100, Name = "First" } },
                { "item_second", new() { Id = 200, Name = "Second" } }
            };

            var expected = """{"item_first":{"Id":100,"Name":"First"},"item_second":{"Id":200,"Name":"Second"}}""";

            var json = dict.ToJson();
            Assert.That(json, Is.EqualTo(expected));

            var parsed = RootDictionarySerializer.FromJson(expected);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed.Count, Is.EqualTo(2));
            Assert.That(parsed["item_first"].Id, Is.EqualTo(100));
            Assert.That(parsed["item_second"].Name, Is.EqualTo("Second"));

            // UTF-8
            var utf8Bytes = dict.ToJsonUtf8();
            var parsedUtf8 = RootDictionarySerializer.FromJsonUtf8(utf8Bytes);
            Assert.That(parsedUtf8, Is.Not.Null);
            Assert.That(parsedUtf8.Count, Is.EqualTo(2));
            Assert.That(parsedUtf8["item_first"].Id, Is.EqualTo(100));
            Assert.That(parsedUtf8["item_second"].Name, Is.EqualTo("Second"));
        }

        [Test]
        public void TestRootPrimitives()
        {
            var list = new List<int> { 1, 2, 3, -4 };
            var expectedList = "[1,2,3,-4]";

            var jsonList = list.ToJson();
            Assert.That(jsonList, Is.EqualTo(expectedList));

            var parsedList = RootIntListSerializer.FromJson(expectedList);
            Assert.That(parsedList, Is.EquivalentTo(list));

            var dict = new Dictionary<string, int>
            {
                { "a", 10 },
                { "b", 20 }
            };
            var expectedDict = """{"a":10,"b":20}""";

            var jsonDict = dict.ToJson();
            Assert.That(jsonDict, Is.EqualTo(expectedDict));

            var parsedDict = RootStringIntDictionarySerializer.FromJson(expectedDict);
            Assert.That(parsedDict, Is.EquivalentTo(dict));
        }

        [Test]
        public void TestRootNullHandling()
        {
            List<RootTestItem> nullList = null;
            Assert.That(nullList.ToJson(), Is.EqualTo("null"));
            Assert.That(nullList.ToJsonUtf8(), Is.EqualTo(System.Text.Encoding.UTF8.GetBytes("null")));

            var parsed = RootListSerializer.FromJson("null");
            Assert.That(parsed, Is.Null);

            var parsedUtf8 = RootListSerializer.FromJsonUtf8(System.Text.Encoding.UTF8.GetBytes("null"));
            Assert.That(parsedUtf8, Is.Null);
        }
    }
}
