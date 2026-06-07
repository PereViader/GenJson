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

        private static string? Serialize<T>(T? obj)
        {
            if (obj == null) return null;
            return GenJsonGenericRegistry.ToJson(obj);
        }

        private static byte[]? SerializeUtf8<T>(T? obj)
        {
            if (obj == null) return null;
            return GenJsonGenericRegistry.ToJsonUtf8(obj);
        }

        private static bool Deserialize<T>(string json, out T? result)
        {
            return GenJsonGenericRegistry.TryFromJson<T>(json, out result);
        }

        private static bool Deserialize<T>(ReadOnlySpan<char> json, out T? result)
        {
            return GenJsonGenericRegistry.TryFromJson<T>(json, out result);
        }

        private static bool DeserializeUtf8<T>(byte[] json, out T? result)
        {
            return GenJsonGenericRegistry.TryFromJsonUtf8<T>(json, out result);
        }

        private static bool DeserializeUtf8<T>(ReadOnlySpan<byte> json, out T? result)
        {
            return GenJsonGenericRegistry.TryFromJsonUtf8<T>(json, out result);
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
            var json = Serialize(obj);
            Assert.That(json, Is.EqualTo("""{"Present":"Hello","NullablePresent":"World"}"""));

            // TryFromJson (Class, string)
            var success2 = Deserialize<StringClass>(json!, out var obj2);
            Assert.That(success2, Is.True);
            Assert.That(obj2, Is.Not.Null);
            Assert.That(obj2!.Present, Is.EqualTo("Hello"));
            Assert.That(obj2.NullablePresent, Is.EqualTo("World"));
            Assert.That(obj2.NullableNull, Is.Null);

            // ToJsonUtf8 (Class)
            var utf8 = SerializeUtf8(obj);
            var expectedUtf8 = Encoding.UTF8.GetBytes("""{"Present":"Hello","NullablePresent":"World"}""");
            Assert.That(utf8, Is.EqualTo(expectedUtf8));

            // TryFromJsonUtf8 (Class, byte[])
            var success3 = DeserializeUtf8<StringClass>(utf8!, out var obj3);
            Assert.That(success3, Is.True);
            Assert.That(obj3, Is.Not.Null);
            Assert.That(obj3!.Present, Is.EqualTo("Hello"));

            // TryFromJson (Class, ReadOnlySpan<char>)
            var success4 = Deserialize<StringClass>(json.AsSpan(), out var obj4);
            Assert.That(success4, Is.True);
            Assert.That(obj4, Is.Not.Null);
            Assert.That(obj4!.Present, Is.EqualTo("Hello"));

            // TryFromJsonUtf8 (Class, ReadOnlySpan<byte>)
            var success5 = DeserializeUtf8<StringClass>(new ReadOnlySpan<byte>(utf8), out var obj5);
            Assert.That(success5, Is.True);
            Assert.That(obj5, Is.Not.Null);
            Assert.That(obj5!.Present, Is.EqualTo("Hello"));
        }

        [Test]
        public void TestStructSerialization()
        {
            var obj = new ParentRecordStruct(new EmptyClass { Value = 42 });

            // ToJson (Struct)
            var json = Serialize(obj);
            Assert.That(json, Is.EqualTo("""{"Child":{"Value":42}}"""));

            // TryFromJson (Struct, string)
            var success2 = Deserialize<ParentRecordStruct>(json!, out var obj2);
            Assert.That(success2, Is.True);
            Assert.That(obj2.Child, Is.Not.Null);
            Assert.That(obj2.Child.Value, Is.EqualTo(42));

            // ToJsonUtf8 (Struct)
            var utf8 = SerializeUtf8(obj);
            var expectedUtf8 = Encoding.UTF8.GetBytes("""{"Child":{"Value":42}}""");
            Assert.That(utf8, Is.EqualTo(expectedUtf8));

            // TryFromJsonUtf8 (Struct, byte[])
            var success3 = DeserializeUtf8<ParentRecordStruct>(utf8!, out var obj3);
            Assert.That(success3, Is.True);
            Assert.That(obj3.Child.Value, Is.EqualTo(42));

            // TryFromJson (Struct, ReadOnlySpan<char>)
            var success4 = Deserialize<ParentRecordStruct>(json.AsSpan(), out var obj4);
            Assert.That(success4, Is.True);
            Assert.That(obj4.Child.Value, Is.EqualTo(42));

            // TryFromJsonUtf8 (Struct, ReadOnlySpan<byte>)
            var success5 = DeserializeUtf8<ParentRecordStruct>(new ReadOnlySpan<byte>(utf8), out var obj5);
            Assert.That(success5, Is.True);
            Assert.That(obj5.Child.Value, Is.EqualTo(42));
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

