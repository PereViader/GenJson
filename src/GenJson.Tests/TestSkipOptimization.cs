using System.Collections.Generic;
using NUnit.Framework;

namespace GenJson.Tests;

[GenJson]
[GenJsonSkipCountOptimization]
public partial record SkipOptimizationClass(List<int> List, int[] Array, Dictionary<string, int> Dictionary);

public class TestSkipOptimization
{
    [Test]
    public void TestSerialization_SkipsCountProperty()
    {
        var obj = new SkipOptimizationClass([1, 2], [3, 4], new() { { "5", 6 } });
        var json = obj.ToJson();

        // Expect NO count properties
        var expected = """{"List":[1,2],"Array":[3,4],"Dictionary":{"5":6}}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestDeserialization_WorksWithoutCountProperty()
    {
        var json = """{"List":[1,2],"Array":[3,4],"Dictionary":{"5":6}}""";
        var obj = SkipOptimizationClass.FromJson(json)!;

        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.List, Is.EqualTo(new List<int> { 1, 2 }));
        Assert.That(obj.Array, Is.EqualTo(new int[] { 3, 4 }));
        Assert.That(obj.Dictionary, Is.EqualTo(new Dictionary<string, int> { { "5", 6 } }));
    }

    [Test]
    public void TestDeserialization_IgnoresCountPropertyIfPresent()
    {
        // Even if count property is present in JSON (e.g. from older version or other system), 
        // it should be treated as unknown property and ignored since we are skipping optimization.
        var json = """{"$List":2,"List":[1,2],"$Array":2,"Array":[3,4],"$Dictionary":1,"Dictionary":{"5":6}}""";
        var obj = SkipOptimizationClass.FromJson(json)!;

        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.List, Is.EqualTo(new List<int> { 1, 2 }));
        Assert.That(obj.Array, Is.EqualTo(new int[] { 3, 4 }));
        Assert.That(obj.Dictionary, Is.EqualTo(new Dictionary<string, int> { { "5", 6 } }));
    }
}
