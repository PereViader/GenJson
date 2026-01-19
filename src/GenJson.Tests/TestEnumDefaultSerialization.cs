using NUnit.Framework;

namespace GenJson.Tests;

[GenJson.Enum.AsText]
public enum DefaultAsTextEnum
{
    One = 1,
    Two = 2
}

[GenJson]
public partial record DefaultAsText(DefaultAsTextEnum Value);

[GenJson.Enum.AsNumber]
public enum DefaultAsNumberEnum
{
    One = 1,
    Two = 2
}

[GenJson]
public partial record DefaultAsNumber(DefaultAsNumberEnum Value);

[GenJson]
public partial record OverrideAsNumber([GenJson.Enum.AsNumber] DefaultAsTextEnum Value);

[GenJson]
public partial record OverrideAsText([GenJson.Enum.AsText] DefaultAsNumberEnum Value);

public class TestEnumDefaultSerialization
{
    [Test]
    public void TestDefaultAsText()
    {
        var obj = new DefaultAsText(DefaultAsTextEnum.One);
        var json = obj.ToJson();
        var expected = """{"Value":"One"}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestDefaultAsNumber()
    {
        var obj = new DefaultAsNumber(DefaultAsNumberEnum.One);
        var json = obj.ToJson();
        var expected = """{"Value":1}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestOverrideAsNumber()
    {
        var obj = new OverrideAsNumber(DefaultAsTextEnum.One);
        var json = obj.ToJson();
        var expected = """{"Value":1}""";
        Assert.That(json, Is.EqualTo(expected));
    }

    [Test]
    public void TestOverrideAsText()
    {
        var obj = new OverrideAsText(DefaultAsNumberEnum.One);
        var json = obj.ToJson();
        var expected = """{"Value":"One"}""";
        Assert.That(json, Is.EqualTo(expected));
    }
}