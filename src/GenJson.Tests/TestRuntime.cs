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
            EnumerablePresent = [new EmptyClass() { Value = 1 }, new EmptyClass(){ Value = 2 }],
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
}

