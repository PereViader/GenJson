using System.Collections.Generic;
using NUnit.Framework;

namespace GenJson.Tests;

[GenJson]
[GenJsonDerivedType(typeof(PolymorphicStringNoAbstractRecord), 1)]
public partial record PolymorphicBaseNoAbstractRecord(int Base);

[GenJson]
public partial record PolymorphicStringNoAbstractRecord(string Value, int Base) : PolymorphicBaseNoAbstractRecord(Base);

[GenJson]
[GenJsonDerivedType(typeof(PolymorphicStringRecord), 1)]
[GenJsonDerivedType(typeof(PolymorphicIntRecord), 2)]
public abstract partial record PolymorphicBaseRecord(int Base);

[GenJson]
public partial record PolymorphicStringRecord(string Value, int Base) : PolymorphicBaseRecord(Base);

[GenJson]
public partial record PolymorphicIntRecord(int Base, int Value) : PolymorphicBaseRecord(Base);

[GenJson]
[GenJsonPolymorphic("$base-type")]
[GenJsonDerivedType(typeof(PolymorphicDerivedClass), "derived")]
public abstract partial class PolymorphicBaseClass
{
    public int Base { get; set; }
}

[GenJson]
public partial class PolymorphicDerivedClass : PolymorphicBaseClass
{
    public int Derived { get; set; }
}

[GenJson]
public partial record PolymorphicContainerRecord(List<PolymorphicBaseRecord> Records);

public class TestPolymorphic
{
    [Test]
    public void TestPolymorphicStringNoAbstractRecord()
    {
        var obj = new PolymorphicStringNoAbstractRecord("test", 1);
        var expected = """{"Base":1,"Value":"test"}""";
        Assert.That(obj.ToJson(), Is.EqualTo(expected));

        var obj2 = PolymorphicStringNoAbstractRecord.FromJson(expected)!;
        var obj2Json = obj2.ToJson();
        Assert.That(obj2Json, Is.EqualTo(expected));

        var baseObj = (PolymorphicBaseNoAbstractRecord)obj2;
        var baseObjJson = baseObj.ToJson();
        var baseExpected = """{"$type":1,"Base":1,"Value":"test"}""";
        Assert.That(baseObjJson, Is.EqualTo(baseExpected));

        var baseObj2 = PolymorphicBaseNoAbstractRecord.FromJson(baseExpected)!;
        var baseObj2Json = baseObj2.ToJson();
        Assert.That(baseObj2Json, Is.EqualTo(baseExpected));
    }

    [Test]
    public void TestPolymorphicBaseNoAbstractRecord()
    {
        var obj = new PolymorphicBaseNoAbstractRecord(1);
        var expected = """{"Base":1}""";
        Assert.That(obj.ToJson(), Is.EqualTo(expected));

        var obj2 = PolymorphicBaseNoAbstractRecord.FromJson(expected)!;
        var obj2Json = obj2.ToJson();
        Assert.That(obj2Json, Is.EqualTo(expected));
    }

    [Test]
    public void TestPolymorphicStringRecord()
    {
        var obj = new PolymorphicStringRecord("test", 1);
        var expected = """{"Base":1,"Value":"test"}""";
        Assert.That(obj.ToJson(), Is.EqualTo(expected));

        var obj2 = PolymorphicStringRecord.FromJson(expected)!;
        var obj2Json = obj2.ToJson();
        Assert.That(obj2Json, Is.EqualTo(expected));

        var baseObj = (PolymorphicBaseRecord)obj2;
        var baseObjJson = baseObj.ToJson();
        var baseExpected = """{"$type":1,"Base":1,"Value":"test"}""";
        Assert.That(baseObjJson, Is.EqualTo(baseExpected));

        var baseObj2 = PolymorphicBaseRecord.FromJson(baseExpected)!;
        var baseObj2Json = baseObj2.ToJson();
        Assert.That(baseObj2Json, Is.EqualTo(baseExpected));
    }

    [Test]
    public void TestPolymorphicIntRecord()
    {
        var obj = new PolymorphicIntRecord(1, 2);
        var expected = """{"Base":1,"Value":2}""";
        Assert.That(obj.ToJson(), Is.EqualTo(expected));

        var obj2 = PolymorphicIntRecord.FromJson(expected)!;
        var obj2Json = obj2.ToJson();
        Assert.That(obj2Json, Is.EqualTo(expected));

        var baseObj = (PolymorphicBaseRecord)obj2;
        var baseObjJson = baseObj.ToJson();
        var baseExpected = """{"$type":2,"Base":1,"Value":2}""";
        Assert.That(baseObjJson, Is.EqualTo(baseExpected));

        var baseObj2 = PolymorphicBaseRecord.FromJson(baseExpected)!;
        var baseObj2Json = baseObj2.ToJson();
        Assert.That(baseObj2Json, Is.EqualTo(baseExpected));
    }

    [Test]
    public void TestPolymorphicDerivedClass()
    {
        var obj = new PolymorphicDerivedClass { Base = 1, Derived = 2 };
        var expected = """{"Base":1,"Derived":2}""";
        Assert.That(obj.ToJson(), Is.EqualTo(expected));

        var obj2 = PolymorphicDerivedClass.FromJson(expected)!;
        var obj2Json = obj2.ToJson();
        Assert.That(obj2Json, Is.EqualTo(expected));

        var baseObj = (PolymorphicBaseClass)obj2;
        var baseObjJson = baseObj.ToJson();
        var baseExpected = """{"$base-type":"derived","Base":1,"Derived":2}""";
        Assert.That(baseObjJson, Is.EqualTo(baseExpected));

        var baseObj2 = PolymorphicBaseClass.FromJson(baseExpected)!;
        var baseObj2Json = baseObj2.ToJson();
        Assert.That(baseObj2Json, Is.EqualTo(baseExpected));
    }

    [Test]
    public void TestPolymorphicContainerRecord()
    {
        var obj = new PolymorphicContainerRecord(new List<PolymorphicBaseRecord> { new PolymorphicStringRecord("test", 1), new PolymorphicIntRecord(1, 2) });
        var expected = """{"$Records":2,"Records":[{"$type":1,"Base":1,"Value":"test"},{"$type":2,"Base":1,"Value":2}]}""";
        Assert.That(obj.ToJson(), Is.EqualTo(expected));

        var obj2 = PolymorphicContainerRecord.FromJson(expected)!;
        var obj2Json = obj2.ToJson();
        Assert.That(obj2Json, Is.EqualTo(expected));
    }
}