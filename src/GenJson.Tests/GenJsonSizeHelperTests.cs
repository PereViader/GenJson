using NUnit.Framework;
using System;

namespace GenJson.Tests
{
    [TestFixture]
    public class GenJsonSizeHelperTests
    {
        [Test]
        public void GetSize_Int_ReturnsCorrectSize()
        {
            Assert.That(GenJsonSizeHelper.GetSize(0), Is.EqualTo(1));
            Assert.That(GenJsonSizeHelper.GetSize(5), Is.EqualTo(1));
            Assert.That(GenJsonSizeHelper.GetSize(10), Is.EqualTo(2));
            Assert.That(GenJsonSizeHelper.GetSize(100), Is.EqualTo(3));
            Assert.That(GenJsonSizeHelper.GetSize(-5), Is.EqualTo(2)); // -5 -> 1 + size(5) = 2
        }

        [Test]
        public void GetSize_String_ReturnsCorrectSize()
        {
            Assert.That(GenJsonSizeHelper.GetSize((string?)null), Is.EqualTo(0));
            Assert.That(GenJsonSizeHelper.GetSize(""), Is.EqualTo(2)); // ""
            Assert.That(GenJsonSizeHelper.GetSize("hello"), Is.EqualTo(7)); // "hello" -> 2 quotes + 5 chars = 7
            Assert.That(GenJsonSizeHelper.GetSize("\n"), Is.EqualTo(4)); // "\n" -> 2 quotes + 2 chars (\n) = 4
        }

        [Test]
        public void GetSize_Bool_ReturnsCorrectSize()
        {
            Assert.That(GenJsonSizeHelper.GetSize(true), Is.EqualTo(4)); // true
            Assert.That(GenJsonSizeHelper.GetSize(false), Is.EqualTo(5)); // false
        }

        [Test]
        public void GetSize_Double_HandlesSpecialValues()
        {
            // JSON does not standardly support NaN/Infinity, but let's check what GetSize returns
            // It relies on Utf8Formatter or string.Create usually.
            // If it returns a size, it means it would be written.
            Assert.That(GenJsonSizeHelper.GetSize(double.NaN), Is.GreaterThan(0));
            Assert.That(GenJsonSizeHelper.GetSize(double.PositiveInfinity), Is.GreaterThan(0));
            Assert.That(GenJsonSizeHelper.GetSize(double.NegativeInfinity), Is.GreaterThan(0));
        }

        [Test]
        public void GetSize_MinMax_ReturnsCorrectSize()
        {
            Assert.That(GenJsonSizeHelper.GetSize(int.MinValue), Is.EqualTo(11)); // -2147483648
            Assert.That(GenJsonSizeHelper.GetSize(int.MaxValue), Is.EqualTo(10)); // 2147483647
            Assert.That(GenJsonSizeHelper.GetSize(long.MinValue), Is.EqualTo(20)); // -9223372036854775808
            Assert.That(GenJsonSizeHelper.GetSize(long.MaxValue), Is.EqualTo(19)); // 9223372036854775807
        }
    }
}
