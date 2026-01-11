using System;
using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;

namespace GenJson.Tests;

public class TestRuntime
{
    [Test]
    public void TestEmptyClass()
    {
        var obj = new EmptyClass();
        var json = obj.ToJson();
        var expected = """{}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestStringClass()
    {
        var obj = new StringClass()
        {
            Present = "1",
            NullablePresent = "2",
            NullableNull = null
        };

        var json = obj.ToJson();
        var expected = """{"Present":"1","NullablePresent":"2"}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestIntClass()
    {
        var obj = new IntClass()
        {
            Present = 1,
            NullablePresent = 2,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":1,"NullablePresent":2}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestParentClass()
    {
        var obj = new ParentClass()
        {
            Present = new EmptyClass()
            {
                Value = 1
            },
            NullablePresent = new EmptyClass()
        };
        var json = obj.ToJson();
        var expected = """{"Present":{"Value":1},"NullablePresent":{}}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestEnumerableIntClass()
    {
        var obj = new EnumerableIntClass()
        {
            EnumerablePresent = [1, 2],
            ArrayPresent = [3],
            ListPresent = [4],
            NullableEnumerablePresent = [5],
            NullableArrayPresent = [6],
            NullableListPresent = [7],
            NullableEnumerableNull = null,
            NullableArrayNull = null,
            NullableListNull = null,
        };
        var json = obj.ToJson();
        var expected = """{"EnumerablePresent":[1,2],"ArrayPresent":[3],"ListPresent":[4],"NullableEnumerablePresent":[5],"NullableArrayPresent":[6],"NullableListPresent":[7]}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestEnumerableStringClass()
    {
        var obj = new EnumerableStringClass()
        {
            EnumerablePresent = ["1", "2"],
            ArrayPresent = ["3"],
            ListPresent = ["4"],
            NullableEnumerablePresent = ["5"],
            NullableArrayPresent = ["6"],
            NullableListPresent = ["7"],
            NullableEnumerableNull = null,
            NullableArrayNull = null,
            NullableListNull = null,
        };
        var json = obj.ToJson();
        var expected = """{"EnumerablePresent":["1","2"],"ArrayPresent":["3"],"ListPresent":["4"],"NullableEnumerablePresent":["5"],"NullableArrayPresent":["6"],"NullableListPresent":["7"]}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestEnumerableParentClass()
    {
        var obj = new EnumerableParentClass()
        {
            EnumerablePresent = [new EmptyClass() { Value = 1 }, new EmptyClass() { Value = 2 }],
            ArrayPresent = [new EmptyClass()],
            ListPresent = [new EmptyClass()],
            NullableEnumerablePresent = [new EmptyClass()],
            NullableArrayPresent = [new EmptyClass()],
            NullableListPresent = [new EmptyClass()],
            NullableEnumerableNull = null,
            NullableArrayNull = null,
            NullableListNull = null,
        };
        var json = obj.ToJson();
        var expected = """{"EnumerablePresent":[{"Value":1},{"Value":2}],"ArrayPresent":[{}],"ListPresent":[{}],"NullableEnumerablePresent":[{}],"NullableArrayPresent":[{}],"NullableListPresent":[{}]}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestNestedEnumerableClass()
    {
        var obj = new NestedEnumerableClass()
        {
            EnumerablePresent = [[new EmptyClass() { Value = 1 }, new EmptyClass()], [new EmptyClass(), new EmptyClass() { Value = 2 }]],
        };
        var json = obj.ToJson();
        var expected = """{"EnumerablePresent":[[{"Value":1},{}],[{},{"Value":2}]]}""";
        Assert.That(json, Is.EqualTo(expected));
    }
    
    [Test]
    public void TestDictionaryClass()
    {
        var obj = new DictionaryClass()
        {
            PresentIntInt = new Dictionary<int, int>() { { 1, 2 },  { 3, 4 } },
            PresentIntString = new Dictionary<int, string>() { { 5, "6"} },
            PresentStringInt = new Dictionary<string, int>() { { "7", 8 } },
            PresentIntEnumerableInt = new Dictionary<int, IEnumerable<int>>() { {9, [10]}},
            PresentDictionaryIntEmptyClasses = new Dictionary<int, EmptyClass>() { {11, new EmptyClass() {Value = 12}} },
            NullableDictionaryIntIntNull = null
        };
        var json = obj.ToJson();
        var expected = """{"PresentIntInt":{"1":2,"3":4},"PresentIntString":{"5":"6"},"PresentStringInt":{"7":8},"PresentIntEnumerableInt":{"9":[10]},"PresentDictionaryIntEmptyClasses":{"11":{"Value":12}}}""";
        Assert.That(json, Is.EqualTo(expected));
    }
    
    [Test]
    public void TestNestedDictionaryClass()
    {
        var obj = new NestedDictionaryClass()
        {
            Present = new Dictionary<int, IReadOnlyDictionary<int, EmptyClass>>() { { 1, new Dictionary<int, EmptyClass>() { { 2, new EmptyClass() { Value = 3 } }, { 4, new EmptyClass()} } } },
        };
        var json = obj.ToJson();
        var expected = """{"Present":{"1":{"2":{"Value":3},"4":{}}}}""";
        Assert.That(json, Is.EqualTo(expected));
    }
}

