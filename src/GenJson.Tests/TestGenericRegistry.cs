using System;
using System.Text;
using NUnit.Framework;

namespace GenJson.Tests
{
    [TestFixture]
    public class TestGenericRegistry
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Register all types in the assembly
            GenJson_GenJson_Tests_AssemblyInitializer.Initialize();
        }

        [Test]
        public void TestClassSerialization()
        {
            var obj = new StringClass
            {
                Present = "Hello",
                NullablePresent = "World",
                NullableNull = null
            };

            // ToJson (Class)
            var json = GenJsonGenericRegistry.ToJson(obj);
            Assert.That(json, Is.EqualTo("""{"Present":"Hello","NullablePresent":"World"}"""));

            // FromJson (Class, string)
            var obj2 = GenJsonGenericRegistry.FromJson<StringClass>(json);
            Assert.That(obj2, Is.Not.Null);
            Assert.That(obj2!.Present, Is.EqualTo("Hello"));
            Assert.That(obj2.NullablePresent, Is.EqualTo("World"));
            Assert.That(obj2.NullableNull, Is.Null);

            // ToJsonUtf8 (Class)
            var utf8 = GenJsonGenericRegistry.ToJsonUtf8(obj);
            var expectedUtf8 = Encoding.UTF8.GetBytes("""{"Present":"Hello","NullablePresent":"World"}""");
            Assert.That(utf8, Is.EqualTo(expectedUtf8));

            // FromJsonUtf8 (Class, byte[])
            var obj3 = GenJsonGenericRegistry.FromJsonUtf8<StringClass>(utf8);
            Assert.That(obj3, Is.Not.Null);
            Assert.That(obj3!.Present, Is.EqualTo("Hello"));

            // FromJson (Class, ReadOnlySpan<char>)
            var obj4 = GenJsonGenericRegistry.FromJson<StringClass>(json.AsSpan());
            Assert.That(obj4, Is.Not.Null);
            Assert.That(obj4!.Present, Is.EqualTo("Hello"));

            // FromJsonUtf8 (Class, ReadOnlySpan<byte>)
            var obj5 = GenJsonGenericRegistry.FromJsonUtf8<StringClass>(new ReadOnlySpan<byte>(utf8));
            Assert.That(obj5, Is.Not.Null);
            Assert.That(obj5!.Present, Is.EqualTo("Hello"));
        }

        [Test]
        public void TestStructSerialization()
        {
            var obj = new ParentRecordStruct(new EmptyClass { Value = 42 });

            // ToJson (Struct)
            var json = GenJsonGenericRegistry.ToJson(obj);
            Assert.That(json, Is.EqualTo("""{"Child":{"Value":42}}"""));

            // FromJson (Struct, string)
            var obj2 = GenJsonGenericRegistry.FromJson<ParentRecordStruct>(json);
            Assert.That(obj2, Is.Not.Null);
            Assert.That(obj2!.Value.Child, Is.Not.Null);
            Assert.That(obj2.Value.Child.Value, Is.EqualTo(42));

            // ToJsonUtf8 (Struct)
            var utf8 = GenJsonGenericRegistry.ToJsonUtf8(obj);
            var expectedUtf8 = Encoding.UTF8.GetBytes("""{"Child":{"Value":42}}""");
            Assert.That(utf8, Is.EqualTo(expectedUtf8));

            // FromJsonUtf8 (Struct, byte[])
            var obj3 = GenJsonGenericRegistry.FromJsonUtf8<ParentRecordStruct>(utf8);
            Assert.That(obj3, Is.Not.Null);
            Assert.That(obj3!.Value.Child.Value, Is.EqualTo(42));

            // FromJson (Struct, ReadOnlySpan<char>)
            var obj4 = GenJsonGenericRegistry.FromJson<ParentRecordStruct>(json.AsSpan());
            Assert.That(obj4, Is.Not.Null);
            Assert.That(obj4!.Value.Child.Value, Is.EqualTo(42));

            // FromJsonUtf8 (Struct, ReadOnlySpan<byte>)
            var obj5 = GenJsonGenericRegistry.FromJsonUtf8<ParentRecordStruct>(new ReadOnlySpan<byte>(utf8));
            Assert.That(obj5, Is.Not.Null);
            Assert.That(obj5!.Value.Child.Value, Is.EqualTo(42));
        }

        [Test]
        public void TestNullHandling()
        {
            // Null string/array to FromJson should return null
            Assert.That(GenJsonGenericRegistry.FromJson<StringClass>((string?)null), Is.Null);
            Assert.That(GenJsonGenericRegistry.FromJsonUtf8<StringClass>((byte[]?)null), Is.Null);

            Assert.That(GenJsonGenericRegistry.FromJson<ParentRecordStruct>((string?)null), Is.Null);
            Assert.That(GenJsonGenericRegistry.FromJsonUtf8<ParentRecordStruct>((byte[]?)null), Is.Null);

            // Null object to ToJson should return null (for classes)
            Assert.That(GenJsonGenericRegistry.ToJson<StringClass>(null), Is.Null);
            Assert.That(GenJsonGenericRegistry.ToJsonUtf8<StringClass>(null), Is.Null);
        }

        [Test]
        public void TestMultipleInitializationIsSafe()
        {
            // Call again and verify it is safe and doesn't throw or cause issues
            Assert.DoesNotThrow(() => GenJson_GenJson_Tests_AssemblyInitializer.Initialize());
            Assert.DoesNotThrow(() => GenJson_GenJson_Tests_AssemblyInitializer.Initialize());
        }
    }
}
