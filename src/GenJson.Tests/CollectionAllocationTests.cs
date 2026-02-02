using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GenJson.Tests
{
    public class CollectionAllocationTests
    {
        [Test]
        public void TestCountListItems_Simple()
        {
            var json = "[1,2,3]".AsSpan();
            // Count starts after '[' so index 1
            int count = GenJsonParser.CountListItems(json, 1);
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void TestCountListItems_Empty()
        {
            var json = "[]".AsSpan();
            int count = GenJsonParser.CountListItems(json, 1);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void TestCountListItems_Empty2()
        {
            var json = "[]".AsSpan();
            int count = GenJsonParser.CountListItems(json, 1);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void TestCountListItems_Nested()
        {
            var json = "[1,[2,3],4]".AsSpan();
            int count = GenJsonParser.CountListItems(json, 1);
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void TestCountListItems_StringWithBrackets()
        {
            var json = """["a]b", "c[d"]""".AsSpan();
            int count = GenJsonParser.CountListItems(json, 1);
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void TestCountDictionaryItems_Simple()
        {
            var json = """{"a":1,"b":2}""".AsSpan();
            // Count starts after '{' so index 1
            int count = GenJsonParser.CountDictionaryItems(json, 1);
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void TestCountDictionaryItems_Empty()
        {
            var json = "{}".AsSpan();
            int count = GenJsonParser.CountDictionaryItems(json, 1);
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void TestCountDictionaryItems_Nested()
        {
            var json = """{"a":{"x":1},"b":2}""".AsSpan();
            int count = GenJsonParser.CountDictionaryItems(json, 1);
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void TestCountDictionaryItems_StringWithBraces()
        {
            var json = """{"]a}":"1}]","b":"{c"}""".AsSpan();
            int count = GenJsonParser.CountDictionaryItems(json, 1);
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void TestCountDictionaryItems_ComplexNested()
        {
            var json = """[{"id":1,"data":"]1]} \",\" 2]"},{"id":2}]""".AsSpan();
            int count = GenJsonParser.CountListItems(json, 1);
            Assert.That(count, Is.EqualTo(2));
        }
    }
}
