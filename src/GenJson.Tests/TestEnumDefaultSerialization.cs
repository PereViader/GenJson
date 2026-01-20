using System.Collections.Generic;
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

[GenJson]
public partial record EnumList(List<DefaultAsNumberEnum> Number, List<DefaultAsTextEnum> Text);

[GenJson]
public partial record EnumDictionary(Dictionary<DefaultAsNumberEnum, DefaultAsNumberEnum> Number, Dictionary<DefaultAsTextEnum, DefaultAsTextEnum> Text);

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

    [Test]
    public void TestEnumList()
    {
        var obj = new EnumList(
            [DefaultAsNumberEnum.One, DefaultAsNumberEnum.Two],
            [DefaultAsTextEnum.One, DefaultAsTextEnum.Two]
            );
        var json = obj.ToJson();
        var expected = """{"Number":[1,2],"Text":["One","Two"]}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = EnumList.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(EnumList.FromJson("{}"), Is.Null);
    }

    [Test]
    public void TestEnumDictionary()
    {
        var obj = new EnumDictionary(
        new(){
            { DefaultAsNumberEnum.One, DefaultAsNumberEnum.One },
            { DefaultAsNumberEnum.Two, DefaultAsNumberEnum.Two },
        },
        new(){
            { DefaultAsTextEnum.One, DefaultAsTextEnum.One },
            { DefaultAsTextEnum.Two, DefaultAsTextEnum.Two },
        }
        );
        var json = obj.ToJson();
        var expected = """{"Number":{"1":1,"2":2},"Text":{"One":"One","Two":"Two"}}""";
        Assert.That(json, Is.EqualTo(expected));

        var size = obj.CalculateJsonSize();
        Assert.That(size, Is.EqualTo(expected.Length));

        var obj2 = EnumDictionary.FromJson(json)!;
        var json2 = obj2.ToJson();
        Assert.That(json, Is.EqualTo(json2));

        Assert.That(EnumDictionary.FromJson("{}"), Is.Null);
    }
}