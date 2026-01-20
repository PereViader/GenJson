using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GenJson.Tests;

public class TestRuntime
{
    [Test]
    public void TestParentNullableDisableClass()
    {
        var obj = new ParentNullableDisableClass(
            Present: new ChildNullableDisableClass(1),
            Null: null
        );

        var json = obj.ToJson();
        var expected = """{"Present":{"Value":1}}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = ParentNullableDisableClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        var obj3 = StringNullableDisableClass.FromJson("{}")!;
        Assert.That(obj3, Is.Not.Null);
        Assert.That(obj3.Present, Is.Null);
        Assert.That(obj3.Null, Is.Null);
    }

    [Test]
    public void TestStringNullableDisableClass()
    {
        var obj = new StringNullableDisableClass()
        {
            Present = "1",
            Null = null,
        };

        var json = obj.ToJson();
        var expected = """{"Present":"1"}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = StringNullableDisableClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        var obj3 = StringNullableDisableClass.FromJson("{}")!;
        Assert.That(obj3, Is.Not.Null);
        Assert.That(obj3.Present, Is.Null);
        Assert.That(obj3.Null, Is.Null);
    }

    [Test]
    public void TestDateTimeNullableDisableClass()
    {
        var obj = new DateTimeNullableDisableClass()
        {
            Present = new DateTime(2000, 1, 1),
            NullablePresent = new DateTime(2000, 1, 2),
            NullableNull = null
        };

        var json = obj.ToJson();
        var expected = """{"Present":"2000-01-01T00:00:00.0000000","NullablePresent":"2000-01-02T00:00:00.0000000"}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = DateTimeNullableDisableClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        var obj3 = DateTimeNullableDisableClass.FromJson("{}")!;
        Assert.That(obj3, Is.Not.Null);
        Assert.That(obj3.Present, Is.EqualTo(default(DateTime)));
        Assert.That(obj3.NullablePresent.HasValue, Is.False);
        Assert.That(obj3.NullableNull.HasValue, Is.False);
    }

    [Test]
    public void TestByteNullableDisableClass()
    {
        var obj = new ByteNullableDisableClass()
        {
            Present = 1,
            NullablePresent = 2,
            NullableNull = null
        };

        var json = obj.ToJson();
        var expected = """{"Present":1,"NullablePresent":2}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = ByteNullableDisableClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        var obj3 = ByteNullableDisableClass.FromJson("{}")!;
        Assert.That(obj3, Is.Not.Null);
        Assert.That(obj3.Present, Is.EqualTo(0));
        Assert.That(obj3.NullablePresent.HasValue, Is.False);
        Assert.That(obj3.NullableNull.HasValue, Is.False);
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

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = StringClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(StringClass.FromJson("{}"), Is.Null);
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

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = IntClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(IntClass.FromJson("{}"), Is.Null);
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

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = IntEnumClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(IntEnumClass.FromJson("{}"), Is.Null);
    }


    [Test]
    public void TestUIntClass()
    {
        var obj = new UIntClass()
        {
            Present = 1,
            NullablePresent = 2,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":1,"NullablePresent":2}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = UIntClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(UIntClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestULongClass()
    {
        var obj = new ULongClass()
        {
            Present = 1UL,
            NullablePresent = 2UL,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":1,"NullablePresent":2}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = ULongClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(ULongClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestBoolClass()
    {
        var obj = new BoolClass()
        {
            Present = true,
            NullablePresent = false,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":true,"NullablePresent":false}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = BoolClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(BoolClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestCharClass()
    {
        var obj = new CharClass()
        {
            Present = 'a',
            NullablePresent = 'b',
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":"a","NullablePresent":"b"}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = CharClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(CharClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestFloatClass()
    {
        var obj = new FloatClass()
        {
            Present = 1.1f,
            NullablePresent = 2.2f,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":1.1,"NullablePresent":2.2}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = FloatClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(FloatClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestDoubleClass()
    {
        var obj = new DoubleClass()
        {
            Present = 1.1,
            NullablePresent = 2.2,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":1.1,"NullablePresent":2.2}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = DoubleClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(DoubleClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestDecimalClass()
    {
        var obj = new DecimalClass()
        {
            Present = 1.1m,
            NullablePresent = 2.2m,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":1.1,"NullablePresent":2.2}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = DecimalClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(DecimalClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestDateTimeClass()
    {
        var obj = new DateTimeClass()
        {
            Present = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            NullablePresent = new DateTime(2001, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":"2000-01-01T12:00:00.0000000Z","NullablePresent":"2001-01-01T12:00:00.0000000Z"}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = DateTimeClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(DateTimeClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestByteClass()
    {
        var obj = new ByteClass()
        {
            Present = 1,
            NullablePresent = 2,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":1,"NullablePresent":2}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = ByteClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(ByteClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestSByteClass()
    {
        var obj = new SByteClass()
        {
            Present = 1,
            NullablePresent = 2,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":1,"NullablePresent":2}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = SByteClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(SByteClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestShortClass()
    {
        var obj = new ShortClass()
        {
            Present = 1,
            NullablePresent = 2,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":1,"NullablePresent":2}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = ShortClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(ShortClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestUShortClass()
    {
        var obj = new UShortClass()
        {
            Present = 1,
            NullablePresent = 2,
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":1,"NullablePresent":2}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = UShortClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(UShortClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestDateTimeOffsetClass()
    {
        var obj = new DateTimeOffsetClass()
        {
            Present = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeSpan.Zero),
            NullablePresent = new DateTimeOffset(2001, 1, 1, 12, 0, 0, TimeSpan.Zero),
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":"2000-01-01T12:00:00.0000000+00:00","NullablePresent":"2001-01-01T12:00:00.0000000+00:00"}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = DateTimeOffsetClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(DateTimeClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestTimeSpanClass()
    {
        var obj = new TimeSpanClass()
        {
            Present = new TimeSpan(1, 2, 3),
            NullablePresent = new TimeSpan(4, 5, 6),
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":"01:02:03","NullablePresent":"04:05:06"}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = TimeSpanClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(TimeSpanClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestGuidClass()
    {
        var obj = new GuidClass()
        {
            Present = Guid.Parse("d7f8a9a0-1234-5678-9abc-def012345678"),
            NullablePresent = Guid.Parse("d7f8a9a0-1234-5678-9abc-def012345679"),
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":"d7f8a9a0-1234-5678-9abc-def012345678","NullablePresent":"d7f8a9a0-1234-5678-9abc-def012345679"}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = GuidClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(GuidClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestVersionClass()
    {
        var obj = new VersionClass()
        {
            Present = new Version(1, 2, 3),
            NullablePresent = new Version(4, 5, 6),
            NullableNull = null
        };
        var json = obj.ToJson();
        var expected = """{"Present":"1.2.3","NullablePresent":"4.5.6"}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = VersionClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(VersionClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestByteEnumClass()
    {
        var obj = new ByteEnumClass()
        {
            Present = ByteEnum.One
        };
        var json = obj.ToJson();
        var expected = """{"Present":1}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = ByteEnumClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(ByteEnumClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestEmptyClass()
    {
        var obj = new EmptyClass();
        var json = obj.ToJson();
        var expected = """{}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = EmptyClass.FromJson(json)!;
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

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = ParentClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(ParentClass.FromJson("{}"), Is.Null);
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

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = EnumerableIntClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(EnumerableIntClass.FromJson("{}"), Is.Null);
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

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = EnumerableStringClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(EnumerableIntClass.FromJson("{}"), Is.Null);
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

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = EnumerableParentClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(EnumerableParentClass.FromJson("{}"), Is.Null);
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

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = NestedEnumerableClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(NestedEnumerableClass.FromJson("{}"), Is.Null);
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

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = DictionaryClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(DictionaryClass.FromJson("{}"), Is.Null);
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

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = NestedDictionaryClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(NestedDictionaryClass.FromJson("{}"), Is.Null);
    }


    [Test]
    public void TestParentRecordClass()
    {
        var obj = new ParentRecordClass(new EmptyClass() { Value = 1 });
        var json = obj.ToJson();
        var expected = """{"Child":{"Value":1}}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = ParentRecordClass.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(ParentRecordClass.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestFallbackEnumClass()
    {
        var obj = new FallbackEnumClass()
        {
            Value = FallbackEnum.One,
            NullableValue = FallbackEnum.Two
        };
        var json = obj.ToJson();
        var expected = """{"Value":1,"NullableValue":2}""";
        Assert.That(json, Is.EqualTo(expected));

        var obj2 = FallbackEnumClass.FromJson(json)!;
        Assert.That(obj2.Value, Is.EqualTo(FallbackEnum.One));
        Assert.That(obj2.NullableValue, Is.EqualTo(FallbackEnum.Two));

        // Test fallback for invalid number
        // 99 is not defined in FallbackEnum
        var jsonInvalidNumber = """{"Value":99,"NullableValue":99}""";
        var obj3 = FallbackEnumClass.FromJson(jsonInvalidNumber)!;
        Assert.That(obj3.Value, Is.EqualTo(FallbackEnum.Unknown));
        Assert.That(obj3.NullableValue, Is.EqualTo(FallbackEnum.Unknown));

        // Test fallback for invalid type (string instead of int)
        var jsonInvalidType = """{"Value":"NotANumber","NullableValue":"NotANumber"}""";
        var obj4 = FallbackEnumClass.FromJson(jsonInvalidType)!;
        Assert.That(obj4.Value, Is.EqualTo(FallbackEnum.Unknown));
        Assert.That(obj4.NullableValue, Is.EqualTo(FallbackEnum.Unknown));
    }

    [Test]
    public void TestParentRecordStruct()
    {
        var obj = new ParentRecordStruct(new EmptyClass() { Value = 1 });
        var json = obj.ToJson();
        var expected = """{"Child":{"Value":1}}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = ParentRecordStruct.FromJson(json)!.Value;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(ParentRecordStruct.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestExtraPropertiesAndSpecialCharacters()
    {
        var json = """
        {
            "Present" : "1",
            "NullablePresent": "2"
            "Extra": {},
            "Special Characters": "\n \"\r\t \u0041 \u0000 { {\"",
            "Potato": ["hello", "Hi", "Banana"],
            "Apple": [1, "2", 33333333, " "],
            "Empty_Values": {
            "null_val": null,
            "empty_str": "",
            "empty_arr": [],
            "empty_obj": {}
            },
            "Numbers_Stress": {
            "large_int": 9223372036854775807,
            "negative": -123.456,
            "scientific": 1.23e+10,
            "small_decimal": 0.0000000000001,
            "zero": 0
            },
            "Nesting_Depth": [[[[["Deep"]]]]],
            "Booleans": {
            "t": true,
            "f": false
            },
            "Unicode_and_Emojis": "🚀 🍟 中文 日本語",
            "Whitespace_Chaos":     "trailing space check"    ,
            "Trailing_Comma_Test": "Some parsers fail if the last item has a comma",
            "Dictionary" : {
                "SpecialChars": "{\n  \"key\": \"value\"\n}"
            }
        }
        """;

        var obj = StringClass.FromJson(json)!;
        var result = obj.ToJson();
        var expected = """{"Present":"1","NullablePresent":"2"}""";
        Assert.That(result, Is.EqualTo(expected));
    }
    [Test]
    public void TestStrictClass()
    {
        var jsonFull = """{"Required": "needed", "Optional": "maybe"}""";
        var objFull = StrictClass.FromJson(jsonFull);
        Assert.That(objFull, Is.Not.Null);
        Assert.That(objFull!.Required, Is.EqualTo("needed"));
        Assert.That(objFull!.Optional, Is.EqualTo("maybe"));

        var jsonMissingOptional = """{"Required": "needed"}""";
        var objMissingOptional = StrictClass.FromJson(jsonMissingOptional);
        Assert.That(objMissingOptional, Is.Not.Null);
        Assert.That(objMissingOptional!.Required, Is.EqualTo("needed"));
        Assert.That(objMissingOptional!.Optional, Is.Null);

        var jsonMissingRequired = """{"Optional": "maybe"}""";
        var objMissingRequired = StrictClass.FromJson(jsonMissingRequired);
        Assert.That(objMissingRequired, Is.Null, "Should return null when required property is missing in nullable context");
    }

    [Test]
    public void TestStrictRecordReference()
    {
        var jsonFull = """{"Required": "needed", "Optional": "maybe"}""";
        var objFull = StrictRecordReference.FromJson(jsonFull);
        Assert.That(objFull, Is.Not.Null);
        Assert.That(objFull!.Required, Is.EqualTo("needed"));
        Assert.That(objFull!.Optional, Is.EqualTo("maybe"));

        var jsonMissingRequired = """{"Optional": "maybe"}""";
        var objMissingRequired = StrictRecordReference.FromJson(jsonMissingRequired);
        Assert.That(objMissingRequired, Is.Null, "Should return null when required constructor parameter is missing");
    }

    [Test]
    public void TestStrictRecordValue()
    {
        var jsonFull = """{"Required": 1, "Optional": 2}""";
        var objFull = StrictRecordValue.FromJson(jsonFull);
        Assert.That(objFull, Is.Not.Null);
        Assert.That(objFull!.Required, Is.EqualTo(1));
        Assert.That(objFull!.Optional, Is.EqualTo(2));

        var jsonMissingRequired = """{}""";
        var objMissingRequired = StrictRecordValue.FromJson(jsonMissingRequired);
        Assert.That(objMissingRequired, Is.Null, "Should return null when required constructor parameter is missing");
    }

    [Test]
    public void IncorrectJson()
    {
        Assert.That(IntClass.FromJson(""), Is.Null);
        Assert.That(IntClass.FromJson("{"), Is.Null);
        Assert.That(IntClass.FromJson("}"), Is.Null);
        Assert.That(IntClass.FromJson("""{"Present": 1"""), Is.Null);
        Assert.That(IntClass.FromJson("""{"Present": 1 b}"""), Is.Null);
        Assert.That(IntClass.FromJson("""{"Present": 1, "NullablePresent": true}"""), Is.Null);

        Assert.That(StringClass.FromJson("""{"Present": "val" """), Is.Null);
        Assert.That(StringClass.FromJson("""{"Present": "val", """), Is.Null);
    }
}

