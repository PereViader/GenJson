using NUnit.Framework;

namespace GenJson.Tests;

[GenJson]
public abstract partial record InheritanceBaseRecord(int A);

[GenJson]
public partial record InheritanceDerivedDifferentOrderRecord(int B, int A) : InheritanceBaseRecord(A);

[GenJson]
public partial record InheritanceDerivedDifferentNameRecord(int B, int C) : InheritanceBaseRecord(C);

[GenJson]
public partial record InheritanceDerivedPresetValueRecord(int B) : InheritanceBaseRecord(42);

[GenJson]
public abstract partial class InheritanceBaseClass
{
    public int A { get; set; }
}

[GenJson]
public partial class InheritanceDerivedClass : InheritanceBaseClass
{
    public int B { get; set; }
}

public class TestInheritance
{
    [Test]
    public void TestInheritanceDerivedDifferentOrderRecord()
    {
        var obj = new InheritanceDerivedDifferentOrderRecord(2, 1);
        var expected = """{"A":1,"B":2}""";
        Assert.That(obj.ToJson(), Is.EqualTo(expected));

        var obj2 = InheritanceDerivedDifferentOrderRecord.FromJson(expected)!;
        var obj2Json = obj2.ToJson();
        Assert.That(obj2Json, Is.EqualTo(expected));
    }

    [Test]
    public void TestInheritanceDerivedDifferentNameRecord()
    {
        var obj = new InheritanceDerivedDifferentNameRecord(2, 1);
        var expected = """{"A":1,"B":2}""";
        Assert.That(obj.ToJson(), Is.EqualTo(expected));

        var obj2 = InheritanceDerivedDifferentNameRecord.FromJson(expected)!;
        var obj2Json = obj2.ToJson();
        Assert.That(obj2Json, Is.EqualTo(expected));
    }

    [Test]
    public void TestInheritanceDerivedPresetValueRecord()
    {
        var obj = new InheritanceDerivedPresetValueRecord(2);
        var expected = """{"A":42,"B":2}""";
        Assert.That(obj.ToJson(), Is.EqualTo(expected));

        var obj2 = InheritanceDerivedPresetValueRecord.FromJson(expected)!;
        var obj2Json = obj2.ToJson();
        Assert.That(obj2Json, Is.EqualTo(expected));
    }

    [Test]
    public void TestInheritanceDerivedClass()
    {
        var obj = new InheritanceDerivedClass { A = 1, B = 2 };
        var expected = """{"A":1,"B":2}""";
        Assert.That(obj.ToJson(), Is.EqualTo(expected));

        var obj2 = InheritanceDerivedClass.FromJson(expected)!;
        var obj2Json = obj2.ToJson();
        Assert.That(obj2Json, Is.EqualTo(expected));
    }
}