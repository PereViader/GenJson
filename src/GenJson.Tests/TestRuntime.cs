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


        var obj2 = EmptyClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
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

        var obj2 = StringClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
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

        var obj2 = IntClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
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

        var obj2 = ParentClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
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

        var obj2 = EnumerableIntClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
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


        var obj2 = EnumerableStringClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
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

        var obj2 = EnumerableParentClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
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

        var obj2 = NestedEnumerableClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
    }

    [Test]
    public void TestDictionaryClass()
    {
        var obj = new DictionaryClass()
        {
            PresentIntInt = new Dictionary<int, int>() { { 1, 2 }, { 3, 4 } },
            PresentIntString = new Dictionary<int, string>() { { 5, "6" } },
            PresentStringInt = new Dictionary<string, int>() { { "7", 8 } },
            PresentIntEnumerableInt = new Dictionary<int, IEnumerable<int>>() { { 9, [10] } },
            PresentDictionaryIntEmptyClasses = new Dictionary<int, EmptyClass>() { { 11, new EmptyClass() { Value = 12 } } },
            NullableDictionaryIntIntNull = null
        };
        var json = obj.ToJson();
        var expected = """{"PresentIntInt":{"1":2,"3":4},"PresentIntString":{"5":"6"},"PresentStringInt":{"7":8},"PresentIntEnumerableInt":{"9":[10]},"PresentDictionaryIntEmptyClasses":{"11":{"Value":12}}}""";
        Assert.That(json, Is.EqualTo(expected));

        var obj2 = DictionaryClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
    }

    [Test]
    public void TestNestedDictionaryClass()
    {
        var obj = new NestedDictionaryClass()
        {
            Present = new Dictionary<int, IReadOnlyDictionary<int, EmptyClass>>() { { 1, new Dictionary<int, EmptyClass>() { { 2, new EmptyClass() { Value = 3 } }, { 4, new EmptyClass() } } } },
        };
        var json = obj.ToJson();
        var expected = """{"Present":{"1":{"2":{"Value":3},"4":{}}}}""";
        Assert.That(json, Is.EqualTo(expected));

        var obj2 = NestedDictionaryClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
    }

    [Test]
    public void TestPrimitiveClass()
    {
        var obj = new PrimitiveClass()
        {
            Bool = true,
            Int = 1,
            Uint = 2,
            Char = 'c',
            Long = 3,
            Short = 4,
            Byte = 5,
            SByte = 6,
            Float = 1.1f,
            Double = 2.2,
            Decimal = 3.3m,
            String = "string",
            DateTime = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            TimeSpan = new TimeSpan(1, 2, 3),
            DateOnly = new DateOnly(2000, 1, 1),
            TimeOnly = new TimeOnly(12, 0, 0),
            DateTimeOffset = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.Zero),
            Guid = Guid.Parse("d7f8a9a0-1234-5678-9abc-def012345678"),
            Version = new Version(1, 2, 3)
        };
        var json = obj.ToJson();
        var expected = """{"Bool":true,"Int":1,"Uint":2,"Char":"c","Long":3,"Short":4,"Byte":5,"SByte":6,"Float":1.1,"Double":2.2,"Decimal":3.3,"String":"string","DateTime":"2000-01-01T12:00:00.0000000Z","TimeSpan":"01:02:03","DateOnly":"2000-01-01","TimeOnly":"12:00:00.0000000","DateTimeOffset":"2000-01-01T12:00:00.0000000+00:00","Guid":"d7f8a9a0-1234-5678-9abc-def012345678","Version":"1.2.3"}""";
        Assert.That(json, Is.EqualTo(expected));

        var obj2 = PrimitiveClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
    }

    [Test]
    public void TestIntEnumClass()
    {
        var obj = new IntEnumClass()
        {
            PresentNumber = (IntEnum)0,
            PresentText = IntEnum.One,
            NullablePresentNumber = IntEnum.Two,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"PresentNumber":0,"PresentText":"One","NullablePresentNumber":2}""";
        Assert.That(json, Is.EqualTo(expected));

        var obj2 = IntEnumClass.FromJson(json);
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));
    }
}

