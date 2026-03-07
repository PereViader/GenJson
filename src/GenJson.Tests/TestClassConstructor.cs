using GenJson;
using NUnit.Framework;

namespace GenJson.Tests;

[GenJson]
public partial class ClassWithConstructor
{
    public int A { get; }
    public string B { get; }

    public ClassWithConstructor(int a, string b)
    {
        A = a;
        B = b;
    }
}

[GenJson]
public partial class ClassWithPrimaryConstructor(int a, string b)
{
    public int A { get; } = a;
    public string B { get; } = b;
}

public class TestClassConstructor
{
    [Test]
    public void Parse_ClassWithConstructor_Works()
    {
        var json = """{"A":1,"B":"test"}""";
        var parsed = ClassWithConstructor.FromJson(json)!;

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed.A, Is.EqualTo(1));
        Assert.That(parsed.B, Is.EqualTo("test"));
    }

    [Test]
    public void Parse_ClassWithPrimaryConstructor_Works()
    {
        var json = """{"A":2,"B":"test2"}""";
        var parsed = ClassWithPrimaryConstructor.FromJson(json)!;

        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed.A, Is.EqualTo(2));
        Assert.That(parsed.B, Is.EqualTo("test2"));
    }
}
