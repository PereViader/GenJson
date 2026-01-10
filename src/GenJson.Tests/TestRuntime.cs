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
            Present = "Yes1",
            NullablePresent = "Yes2",
            NullableNull = null
        };

        var json = obj.ToJson();
        var expected = """{"Present":"Yes1","NullablePresent":"Yes2"}""";
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
}

