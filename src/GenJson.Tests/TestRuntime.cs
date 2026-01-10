using NUnit.Framework;

namespace GenJson.Tests;

public class TestRuntime
{
    [Test]
    public void TestStringWithValue()
    {
        var obj = new StringClass()
        {
            Value = "Yes1",
            NullableValue = "Yes2"
        };

        var json = obj.ToJson();
        var expected = """{"Value":"Yes1","NullableValue":"Yes2"}""";
        Assert.That(json, Is.EqualTo(expected));
    }
    
    [Test]
    public void TestStringNull()
    {
        var obj = new StringClass()
        {
            Value = string.Empty,
            NullableValue = null
        };

        var json = obj.ToJson();
        var expected = """{"Value":""}""";

        Assert.That(json, Is.EqualTo(expected));
    }
    
    [Test]
    public void TestIntWithValue()
    {
        var obj = new IntClass()
        {
            Value = 1,
            NullableValue = 2
        };
        var json = obj.ToJson();
        var expected = """{"Value":1,"NullableValue":2}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestIntNull()
    {
        var obj = new IntClass()
        {
            Value = 0,
            NullableValue = null
        };
        var json = obj.ToJson();
        var expected = """{"Value":0}""";
        Assert.That(json, Is.EqualTo(expected));
    }
}

