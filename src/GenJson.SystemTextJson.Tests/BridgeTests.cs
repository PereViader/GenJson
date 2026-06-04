using System;
using System.Text.Json;
using NUnit.Framework;
using GenJson;
using GenJson.SystemTextJson;

[assembly: GenJsonConverterFor(typeof(GenJson.SystemTextJson.Tests.StjExternalStruct), typeof(GenJson.SystemTextJson.Tests.StjExternalStructConverter))]


namespace GenJson.SystemTextJson.Tests
{
    // Test Models
    
    [GenJson]
    public partial class SimpleTestModel
    {
        public string Name { get; set; } = "";
        
        [GenJsonPropertyName("custom_age")]
        public int Age { get; set; }
        
        [GenJsonIgnore]
        public string IgnoredField { get; set; } = "should not exist";
    }

    [GenJsonEnumAsText]
    public enum TextEnum
    {
        First,
        Second
    }

    public enum NumberEnum
    {
        Value1 = 10,
        Value2 = 20
    }

    [GenJson]
    public partial class EnumTestModel
    {
        public TextEnum TextVal { get; set; }
        public NumberEnum NumberVal { get; set; }
        
        [GenJsonEnumAsText]
        public NumberEnum NumberValAsText { get; set; }
        
        [GenJsonEnumAsNumber]
        public TextEnum TextValAsNumber { get; set; }
    }

    [GenJson]
    [GenJsonPolymorphic("$type_test")]
    [GenJsonDerivedType(typeof(DerivedDog), "dog")]
    [GenJsonDerivedType(typeof(DerivedCat), "cat")]
    public abstract partial class BaseAnimal
    {
        public string Name { get; set; } = "";
    }

    [GenJson]
    public partial class DerivedDog : BaseAnimal
    {
        public string Breed { get; set; } = "";
    }

    [GenJson]
    public partial class DerivedCat : BaseAnimal
    {
        public bool Lazy { get; set; }
    }

    public static class TestCustomIntConverter
    {
        public static int GetSize(int value) => value.ToString().Length + 4;
        public static void WriteJson(Span<char> span, ref int index, int value)
        {
            span[index++] = '"';
            span[index++] = 'C';
            value.TryFormat(span.Slice(index), out var written);
            index += written;
            span[index++] = 'C';
            span[index++] = '"';
        }
        public static int? FromJson(ReadOnlySpan<char> span, ref int index)
        {
            if (span[index] != '"') return null;
            index++;
            if (span[index] != 'C') return null;
            index++;
            int start = index;
            while (char.IsDigit(span[index])) index++;
            if (!int.TryParse(span.Slice(start, index - start), out int val)) return null;
            if (span[index] != 'C') return null;
            index++;
            if (span[index] != '"') return null;
            index++;
            return val;
        }

        public static int GetSizeUtf8(int value) => GetSize(value);
        public static void WriteJsonUtf8(Span<byte> span, ref int index, int value)
        {
            span[index++] = (byte)'"';
            span[index++] = (byte)'C';
            System.Buffers.Text.Utf8Formatter.TryFormat(value, span.Slice(index), out var written);
            index += written;
            span[index++] = (byte)'C';
            span[index++] = (byte)'"';
        }
        public static int? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
        {
            if (span[index] != (byte)'"') return null;
            index++;
            if (span[index] != (byte)'C') return null;
            index++;
            int start = index;
            while (span[index] >= (byte)'0' && span[index] <= (byte)'9') index++;
            if (!System.Buffers.Text.Utf8Parser.TryParse(span.Slice(start, index - start), out int val, out var _)) return null;
            if (span[index] != (byte)'C') return null;
            index++;
            if (span[index] != (byte)'"') return null;
            index++;
            return val;
        }
    }

    [GenJson]
    public partial class CustomConverterModel
    {
        [GenJsonConverter(typeof(TestCustomIntConverter))]
        public int ConvertedValue { get; set; }
    }

    public struct StjExternalStruct
    {
        public int Value { get; set; }
    }

    public static class StjExternalStructConverter
    {
        public static int GetSize(StjExternalStruct value) => 5;
        public static void WriteJson(Span<char> span, ref int index, StjExternalStruct value)
        {
            span[index++] = '"';
            span[index++] = 'S';
            span[index++] = (char)('0' + value.Value);
            span[index++] = 'S';
            span[index++] = '"';
        }
        public static StjExternalStruct? FromJson(ReadOnlySpan<char> span, ref int index)
        {
            if (span[index] != '"' || span[index + 1] != 'S') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != 'S' || span[index + 1] != '"') return null;
            index += 2;
            return new StjExternalStruct { Value = val };
        }

        public static int GetSizeUtf8(StjExternalStruct value) => 5;
        public static void WriteJsonUtf8(Span<byte> span, ref int index, StjExternalStruct value)
        {
            span[index++] = (byte)'"';
            span[index++] = (byte)'S';
            span[index++] = (byte)('0' + value.Value);
            span[index++] = (byte)'S';
            span[index++] = (byte)'"';
        }
        public static StjExternalStruct? FromJsonUtf8(ReadOnlySpan<byte> span, ref int index)
        {
            if (span[index] != (byte)'"' || span[index + 1] != (byte)'S') return null;
            index += 2;
            int val = span[index++] - '0';
            if (span[index] != (byte)'S' || span[index + 1] != (byte)'"') return null;
            index += 2;
            return new StjExternalStruct { Value = val };
        }
    }

    [GenJson]
    public partial class StjAssemblyConverterModel
    {
        public StjExternalStruct Val { get; set; }
    }

    [GenJson]
    public partial class NullTestModel
    {
        public string? NullableString { get; set; }
        public string RegularString { get; set; } = "hello";
    }

    [GenJson]
    public partial class FloatTestModel
    {
        public double DoubleNaN { get; set; }
        public double DoubleInfinity { get; set; }
    }

    [TestFixture]
    public class BridgeTests
    {
        private readonly JsonSerializerOptions _options = GenJsonStjOptions.CreateOptions();

        [Test]
        public void TestSimplePropertiesAndIgnore()
        {
            var model = new SimpleTestModel
            {
                Name = "Alice",
                Age = 30,
                IgnoredField = "ignored"
            };

            // Serialize with STJ
            string json = JsonSerializer.Serialize(model, _options);

            // Verify json formatting exactly
            Assert.That(json, Is.EqualTo("{\"Name\":\"Alice\",\"custom_age\":30}"));
            
            // Deserialize with GenJson
            var deserialized = SimpleTestModel.FromJson(json)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Name, Is.EqualTo("Alice"));
            Assert.That(deserialized.Age, Is.EqualTo(30));
            Assert.That(deserialized.IgnoredField, Is.EqualTo("should not exist")); // default value
        }

        [Test]
        public void TestEnumSerialization()
        {
            var model = new EnumTestModel
            {
                TextVal = TextEnum.Second,
                NumberVal = NumberEnum.Value2,
                NumberValAsText = NumberEnum.Value1,
                TextValAsNumber = TextEnum.First
            };

            string json = JsonSerializer.Serialize(model, _options);

            // Verify json formatting exactly
            Assert.That(json, Is.EqualTo("{\"TextVal\":\"Second\",\"NumberVal\":20,\"NumberValAsText\":\"Value1\",\"TextValAsNumber\":0}"));

            // Deserialize with GenJson
            var deserialized = EnumTestModel.FromJson(json)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.TextVal, Is.EqualTo(TextEnum.Second));
            Assert.That(deserialized.NumberVal, Is.EqualTo(NumberEnum.Value2));
            Assert.That(deserialized.NumberValAsText, Is.EqualTo(NumberEnum.Value1));
            Assert.That(deserialized.TextValAsNumber, Is.EqualTo(TextEnum.First));
        }

        [Test]
        public void TestPolymorphicSerialization()
        {
            BaseAnimal dog = new DerivedDog { Name = "Fido", Breed = "Labrador" };
            BaseAnimal cat = new DerivedCat { Name = "Whiskers", Lazy = true };

            string dogJson = JsonSerializer.Serialize(dog, _options);
            string catJson = JsonSerializer.Serialize(cat, _options);

            // Verify json formatting exactly (derived class properties are serialized first, then base class properties)
            Assert.That(dogJson, Is.EqualTo("{\"$type_test\":\"dog\",\"Breed\":\"Labrador\",\"Name\":\"Fido\"}"));
            Assert.That(catJson, Is.EqualTo("{\"$type_test\":\"cat\",\"Lazy\":true,\"Name\":\"Whiskers\"}"));

            // Deserialize with GenJson
            var deserializedDog = BaseAnimal.FromJson(dogJson);
            var deserializedCat = BaseAnimal.FromJson(catJson);

            Assert.That(deserializedDog, Is.InstanceOf<DerivedDog>());
            Assert.That(((DerivedDog)deserializedDog!).Breed, Is.EqualTo("Labrador"));
            Assert.That(deserializedDog.Name, Is.EqualTo("Fido"));

            Assert.That(deserializedCat, Is.InstanceOf<DerivedCat>());
            Assert.That(((DerivedCat)deserializedCat!).Lazy, Is.True);
            Assert.That(deserializedCat.Name, Is.EqualTo("Whiskers"));
        }

        [Test]
        public void TestCustomConverterBridging()
        {
            var model = new CustomConverterModel { ConvertedValue = 99 };

            string json = JsonSerializer.Serialize(model, _options);

            // Verify Custom converter output exactly
            Assert.That(json, Is.EqualTo("{\"ConvertedValue\":\"C99C\"}"));

            // Deserialize with GenJson
            var deserialized = CustomConverterModel.FromJson(json)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.ConvertedValue, Is.EqualTo(99));
        }

        [Test]
        public void TestCustomConverterBridgingFailure()
        {
            // Invalid custom converter format (missing C prefix/suffix)
            var invalidJson = "{\"ConvertedValue\":\"99\"}";

            // Deserialize using System.Text.Json with GenJson bridge options
            var deserialized = JsonSerializer.Deserialize<CustomConverterModel>(invalidJson, _options)!;
            Assert.That(deserialized, Is.Not.Null);
            // Since ConvertedValue is a non-nullable value type (int), the converter returns null on failure,
            // which the bridge unboxes/maps to default(int) (0)
            Assert.That(deserialized.ConvertedValue, Is.EqualTo(0));
        }

        [Test]
        public void TestAssemblyConverterBridging()
        {
            var model = new StjAssemblyConverterModel { Val = new StjExternalStruct { Value = 5 } };

            string json = JsonSerializer.Serialize(model, _options);

            // Verify Custom converter output exactly
            Assert.That(json, Is.EqualTo("{\"Val\":\"S5S\"}"));

            // Deserialize with GenJson
            var deserialized = StjAssemblyConverterModel.FromJson(json)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Val.Value, Is.EqualTo(5));
        }

        [Test]
        public void TestNullPropertyOmission()
        {
            var model = new NullTestModel
            {
                NullableString = null,
                RegularString = "hello"
            };

            string json = JsonSerializer.Serialize(model, _options);

            // Verify null property is completely omitted exactly
            Assert.That(json, Is.EqualTo("{\"RegularString\":\"hello\"}"));

            // Deserialize with GenJson
            var deserialized = NullTestModel.FromJson(json)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.NullableString, Is.Null);
            Assert.That(deserialized.RegularString, Is.EqualTo("hello"));
        }

        [Test]
        public void TestSpecialFloats()
        {
            var model = new FloatTestModel
            {
                DoubleNaN = double.NaN,
                DoubleInfinity = double.PositiveInfinity
            };

            string json = JsonSerializer.Serialize(model, _options);

            // Verify they are written as strings exactly
            Assert.That(json, Is.EqualTo("{\"DoubleNaN\":\"NaN\",\"DoubleInfinity\":\"Infinity\"}"));

            // Deserialize with GenJson
            var deserialized = FloatTestModel.FromJson(json)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.DoubleNaN, Is.NaN);
            Assert.That(deserialized.DoubleInfinity, Is.EqualTo(double.PositiveInfinity));
        }
    }
}
