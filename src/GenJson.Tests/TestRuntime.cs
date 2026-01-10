using NUnit.Framework;

namespace GenJson.Tests;

public class TestRuntime
{
    [Test]
    public void TestStringWithValue()
    {
        var obj = new StringClass()
        {
            Value = "Hello World"
        };

        var json = obj.ToJson();
        Assert.That(json, Is.EqualTo("{\"Value\":\"Hello World\"}"));
    }
    
    [Test]
    public void TestStringNull()
    {
        var obj = new StringClass()
        {
            Value = null
        };

        var json = obj.ToJson();
        Assert.That(json, Is.EqualTo("{}"));
    }
}

