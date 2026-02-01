using System;
using NUnit.Framework;

namespace GenJson.Tests;

[GenJson]
public partial class PropertyOrderClass
{
    public int A { get; set; }
    public int B { get; set; }
}

public class TestPropertyOrder
{
    [Test]
    public void TestNormalChain()
    {
        var json = """{"A": 1, "B": 2}""";
        var obj = PropertyOrderClass.FromJson(json)!;
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.A, Is.EqualTo(1));
        Assert.That(obj.B, Is.EqualTo(2));
    }

    [Test]
    public void TestReverseChain()
    {
        var json = """{"B": 2, "A": 1}""";
        var obj = PropertyOrderClass.FromJson(json)!;
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.A, Is.EqualTo(1));
        Assert.That(obj.B, Is.EqualTo(2));
    }

    [Test]
    public void TestUnknownMiddle()
    {
        var json = """{"A": 1, "Z": 99, "B": 2}""";
        var obj = PropertyOrderClass.FromJson(json)!;
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.A, Is.EqualTo(1));
        Assert.That(obj.B, Is.EqualTo(2));
    }

    [Test]
    public void TestUnknownStart()
    {
        var json = """{"Z": 99, "A": 1, "B": 2}""";
        var obj = PropertyOrderClass.FromJson(json)!;
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.A, Is.EqualTo(1));
        Assert.That(obj.B, Is.EqualTo(2));
    }

    [Test]
    public void TestUnknownEnd()
    {
        var json = """{"A": 1, "B": 2, "Z": 99}""";
        var obj = PropertyOrderClass.FromJson(json)!;
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.A, Is.EqualTo(1));
        Assert.That(obj.B, Is.EqualTo(2));
    }

    [Test]
    public void TestMultipleUnknowns()
    {
        var json = """{"X": 10, "A": 1, "Y": 20, "B": 2, "Z": 30}""";
        var obj = PropertyOrderClass.FromJson(json)!;
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.A, Is.EqualTo(1));
        Assert.That(obj.B, Is.EqualTo(2));
    }
}
