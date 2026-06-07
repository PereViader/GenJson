using GenJson;
using NUnit.Framework;

[GenJson]
public partial class SomeObject
{
    public int Value { get; set; }
    public OtherValue OtherValue { get; set; }
}
    
[GenJson]
public partial class OtherValue
{
    public bool Value { get; set; }
}

public class Test
{
    [Test]
    public void Test1()
    {
    }
}
