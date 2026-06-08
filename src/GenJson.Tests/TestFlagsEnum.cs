using System;
using NUnit.Framework;

namespace GenJson.Tests
{
    [Flags]
    public enum MyFlagsEnum
    {
        None = 0,
        A = 1,
        B = 2,
        C = 4
    }

    [GenJson]
    public partial class FlagsEnumClass
    {
        public MyFlagsEnum Value { get; set; }
    }

    public class TestFlagsEnum
    {
        [Test]
        public void TestCombinedFlags()
        {
            var json = """{"Value":3}""";
            var parsed = FlagsEnumClass.FromJson(json);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed!.Value, Is.EqualTo(MyFlagsEnum.A | MyFlagsEnum.B));

            var jsonUtf8 = """{"Value":3}"""u8;
            var parsedUtf8 = FlagsEnumClass.FromJsonUtf8(jsonUtf8);
            Assert.That(parsedUtf8, Is.Not.Null);
            Assert.That(parsedUtf8!.Value, Is.EqualTo(MyFlagsEnum.A | MyFlagsEnum.B));
        }
    }
}
