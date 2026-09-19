using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace GenJson.Tests
{
    [TestFixture]
    public class TestCorrectnessEdgeCases
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            GenJson_GenJson_Tests_AssemblyInitializer.Initialize();
        }

        [Test]
        public void TestExplicitNullProperties_DeserializeSuccessfully()
        {
            var json = """{"NullableString":null,"NullableUri":null,"NullableVersion":null,"NullableChild":null,"NullableList":null,"NullableDict":null,"NullableInt":null}""";

            var model = NullableEdgeModel.FromJson(json);
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.NullableString, Is.Null);
            Assert.That(model.NullableUri, Is.Null);
            Assert.That(model.NullableVersion, Is.Null);
            Assert.That(model.NullableChild, Is.Null);
            Assert.That(model.NullableList, Is.Null);
            Assert.That(model.NullableDict, Is.Null);
            Assert.That(model.NullableInt, Is.Null);

            var utf8Json = Encoding.UTF8.GetBytes(json);
            var utf8Model = NullableEdgeModel.FromJsonUtf8(utf8Json);
            Assert.That(utf8Model, Is.Not.Null);
            Assert.That(utf8Model!.NullableString, Is.Null);
            Assert.That(utf8Model.NullableUri, Is.Null);
            Assert.That(utf8Model.NullableVersion, Is.Null);
            Assert.That(utf8Model.NullableChild, Is.Null);
            Assert.That(utf8Model.NullableList, Is.Null);
            Assert.That(utf8Model.NullableDict, Is.Null);
            Assert.That(utf8Model.NullableInt, Is.Null);
        }

        [Test]
        public void TestNullElementsInCollectionsAndDictionaries()
        {
            var json = """{"NullableList":["hello",null,"world"],"NullableDict":{"key1":"value1","key2":null}}""";

            var model = NullableEdgeModel.FromJson(json);
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.NullableList, Is.Not.Null);
            Assert.That(model.NullableList!.Count, Is.EqualTo(3));
            Assert.That(model.NullableList[0], Is.EqualTo("hello"));
            Assert.That(model.NullableList[1], Is.Null);
            Assert.That(model.NullableList[2], Is.EqualTo("world"));

            Assert.That(model.NullableDict, Is.Not.Null);
            Assert.That(model.NullableDict!.Count, Is.EqualTo(2));
            Assert.That(model.NullableDict["key1"], Is.EqualTo("value1"));
            Assert.That(model.NullableDict["key2"], Is.Null);

            var utf8Json = Encoding.UTF8.GetBytes(json);
            var utf8Model = NullableEdgeModel.FromJsonUtf8(utf8Json);
            Assert.That(utf8Model, Is.Not.Null);
            Assert.That(utf8Model!.NullableList, Is.Not.Null);
            Assert.That(utf8Model.NullableList!.Count, Is.EqualTo(3));
            Assert.That(utf8Model.NullableList[1], Is.Null);
            Assert.That(utf8Model.NullableDict!["key2"], Is.Null);
        }

        [Test]
        public void TestDictionaryKeyTypes_RoundTrip()
        {
            var ver = new Version(2, 3, 4);
            var guid = Guid.NewGuid();
            var dt = new DateTime(2026, 8, 30, 23, 50, 0, DateTimeKind.Utc);
            var ts = TimeSpan.FromMinutes(45);

            var model = new DictionaryKeyTypesModel();
            model.VersionDict[ver] = "version-value";
            model.GuidDict[guid] = 999;
            model.DateTimeDict[dt] = "datetime-value";
            model.TimeSpanDict[ts] = 123;
            model.DoubleDict[3.14159] = "pi";

            // Char roundtrip
            var json = model.ToJson();
            var deserialized = DictionaryKeyTypesModel.FromJson(json);
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized!.VersionDict.ContainsKey(ver), Is.True);
            Assert.That(deserialized.VersionDict[ver], Is.EqualTo("version-value"));
            Assert.That(deserialized.GuidDict.ContainsKey(guid), Is.True);
            Assert.That(deserialized.GuidDict[guid], Is.EqualTo(999));
            Assert.That(deserialized.DateTimeDict.ContainsKey(dt), Is.True);
            Assert.That(deserialized.DateTimeDict[dt], Is.EqualTo("datetime-value"));
            Assert.That(deserialized.TimeSpanDict.ContainsKey(ts), Is.True);
            Assert.That(deserialized.TimeSpanDict[ts], Is.EqualTo(123));
            Assert.That(deserialized.DoubleDict.ContainsKey(3.14159), Is.True);
            Assert.That(deserialized.DoubleDict[3.14159], Is.EqualTo("pi"));

            // UTF-8 roundtrip
            var utf8 = model.ToJsonUtf8();
            var deserializedUtf8 = DictionaryKeyTypesModel.FromJsonUtf8(utf8);
            Assert.That(deserializedUtf8, Is.Not.Null);
            Assert.That(deserializedUtf8!.VersionDict.ContainsKey(ver), Is.True);
            Assert.That(deserializedUtf8.VersionDict[ver], Is.EqualTo("version-value"));
            Assert.That(deserializedUtf8.GuidDict.ContainsKey(guid), Is.True);
            Assert.That(deserializedUtf8.GuidDict[guid], Is.EqualTo(999));
            Assert.That(deserializedUtf8.DateTimeDict.ContainsKey(dt), Is.True);
            Assert.That(deserializedUtf8.TimeSpanDict.ContainsKey(ts), Is.True);
            Assert.That(deserializedUtf8.DoubleDict.ContainsKey(3.14159), Is.True);
        }

        [Test]
        public void TestEmptyAndNullPolymorphicDerivedClasses_NoTrailingCommas()
        {
            // 1. Empty derived class (0 properties)
            PolyBaseWithEmptyDerived emptyObj = new EmptyDerivedClass();
            var emptyJson = emptyObj.ToJson();
            Assert.That(emptyJson, Is.EqualTo("""{"$type":"empty"}"""));
            Assert.That(emptyObj.CalculateJsonSize(), Is.EqualTo(emptyJson.Length));

            var emptyUtf8 = emptyObj.ToJsonUtf8();
            Assert.That(Encoding.UTF8.GetString(emptyUtf8), Is.EqualTo("""{"$type":"empty"}"""));
            Assert.That(emptyObj.CalculateJsonSizeUtf8(), Is.EqualTo(emptyUtf8.Length));

            var parsedEmpty = PolyBaseWithEmptyDerived.FromJson(emptyJson);
            Assert.That(parsedEmpty, Is.InstanceOf<EmptyDerivedClass>());
            var parsedEmptyUtf8 = PolyBaseWithEmptyDerived.FromJsonUtf8(emptyUtf8);
            Assert.That(parsedEmptyUtf8, Is.InstanceOf<EmptyDerivedClass>());

            // 2. Derived class with all null properties
            PolyBaseWithEmptyDerived nullPropsObj = new NullPropsDerivedClass { NullProp = null };
            var nullPropsJson = nullPropsObj.ToJson();
            Assert.That(nullPropsJson, Is.EqualTo("""{"$type":"nullProps"}"""));
            Assert.That(nullPropsObj.CalculateJsonSize(), Is.EqualTo(nullPropsJson.Length));

            var nullPropsUtf8 = nullPropsObj.ToJsonUtf8();
            Assert.That(Encoding.UTF8.GetString(nullPropsUtf8), Is.EqualTo("""{"$type":"nullProps"}"""));
            Assert.That(nullPropsObj.CalculateJsonSizeUtf8(), Is.EqualTo(nullPropsUtf8.Length));

            var parsedNullProps = PolyBaseWithEmptyDerived.FromJson(nullPropsJson);
            Assert.That(parsedNullProps, Is.InstanceOf<NullPropsDerivedClass>());
            Assert.That(((NullPropsDerivedClass)parsedNullProps!).NullProp, Is.Null);

            // 3. Derived class with non-null property
            PolyBaseWithEmptyDerived nonNullPropsObj = new NullPropsDerivedClass { NullProp = "hello" };
            var nonNullJson = nonNullPropsObj.ToJson();
            Assert.That(nonNullJson, Is.EqualTo("""{"$type":"nullProps","NullProp":"hello"}"""));
            Assert.That(nonNullPropsObj.CalculateJsonSize(), Is.EqualTo(nonNullJson.Length));

            var nonNullUtf8 = nonNullPropsObj.ToJsonUtf8();
            Assert.That(Encoding.UTF8.GetString(nonNullUtf8), Is.EqualTo("""{"$type":"nullProps","NullProp":"hello"}"""));
            Assert.That(nonNullPropsObj.CalculateJsonSizeUtf8(), Is.EqualTo(nonNullUtf8.Length));

            var parsedNonNull = PolyBaseWithEmptyDerived.FromJson(nonNullJson);
            Assert.That(parsedNonNull, Is.InstanceOf<NullPropsDerivedClass>());
            Assert.That(((NullPropsDerivedClass)parsedNonNull!).NullProp, Is.EqualTo("hello"));
        }

        [Test]
        public void TestCharMaxValue_Utf8BufferSizeAndRoundTrip()
        {
            char maxChar = char.MaxValue; // '\uffff'
            string strWithMaxChar = "start\uffffend";

            // Char helper vs Utf8 helper size
            int sizeChar = GenJsonSizeHelper.GetSize(maxChar);
            int sizeUtf8 = GenJsonSizeHelper.GetSizeUtf8(maxChar);
            Assert.That(sizeChar, Is.EqualTo(8)); // "\uffff"
            Assert.That(sizeUtf8, Is.EqualTo(8)); // "\uffff" in UTF-8 bytes

            int strSizeChar = GenJsonSizeHelper.GetSize(strWithMaxChar);
            int strSizeUtf8 = GenJsonSizeHelper.GetSizeUtf8(strWithMaxChar);
            Assert.That(strSizeChar, Is.EqualTo(strSizeUtf8));

            var obj = new StringClass { Present = strWithMaxChar };
            var utf8Bytes = obj.ToJsonUtf8();
            Assert.That(utf8Bytes.Length, Is.EqualTo(obj.CalculateJsonSizeUtf8()));

            var parsedUtf8 = StringClass.FromJsonUtf8(utf8Bytes);
            Assert.That(parsedUtf8, Is.Not.Null);
            Assert.That(parsedUtf8!.Present, Is.EqualTo(strWithMaxChar));
        }

        [Test]
        public void TestGenericRegistry_NullReferenceType_ReturnsNullJson()
        {
            StringClass? nullObj = null;
            var json = GenJsonGenericRegistry.ToJson(nullObj);
            Assert.That(json, Is.EqualTo("null"));

            var utf8 = GenJsonGenericRegistry.ToJsonUtf8(nullObj);
            Assert.That(Encoding.UTF8.GetString(utf8), Is.EqualTo("null"));
        }

        [Test]
        public void TestCustomConverter_NullableProperty_WithNullJson()
        {
            var json = """{"NullableCustomVal":null,"NullableCustomStr":null}""";
            var model = CustomConverterNullableModel.FromJson(json);
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.NullableCustomVal, Is.Null);
            Assert.That(model.NullableCustomStr, Is.Null);

            var utf8Json = Encoding.UTF8.GetBytes(json);
            var utf8Model = CustomConverterNullableModel.FromJsonUtf8(utf8Json);
            Assert.That(utf8Model, Is.Not.Null);
            Assert.That(utf8Model!.NullableCustomVal, Is.Null);
            Assert.That(utf8Model.NullableCustomStr, Is.Null);
        }

        [Test]
        public void TestControlCharSizing_NoTrailingNullBytes()
        {
            // Test \u007F (DEL, ASCII 127) and \u0085 (NEL, Next Line, U+0085)
            // They are char.IsControl == true, but are NOT < 32 ASCII controls, so they do NOT get \uXXXX escaped in standard JSON
            Assert.That(GenJsonSizeHelper.GetSize('\u007F'), Is.EqualTo(3)); // '"' + char + '"'
            Assert.That(GenJsonSizeHelper.GetSizeUtf8('\u007F'), Is.EqualTo(3)); // '"' + 1 byte + '"'
            Assert.That(GenJsonSizeHelper.GetSize('\u0085'), Is.EqualTo(3)); // '"' + char + '"'
            Assert.That(GenJsonSizeHelper.GetSizeUtf8('\u0085'), Is.EqualTo(4)); // '"' + 2 UTF-8 bytes + '"'

            Assert.That(GenJsonSizeHelper.GetSize("\u007F".AsSpan()), Is.EqualTo(3));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8("\u007F".AsSpan()), Is.EqualTo(3));
            Assert.That(GenJsonSizeHelper.GetSize("\u0085".AsSpan()), Is.EqualTo(3));
            Assert.That(GenJsonSizeHelper.GetSizeUtf8("\u0085".AsSpan()), Is.EqualTo(4));

            var obj = new StringClass { Present = "hello\u007Fworld\u0085!" };

            var json = obj.ToJson();
            Assert.That(obj.CalculateJsonSize(), Is.EqualTo(json.Length));
            Assert.That(json.Contains('\0'), Is.False);

            var utf8 = obj.ToJsonUtf8();
            Assert.That(obj.CalculateJsonSizeUtf8(), Is.EqualTo(utf8.Length));
            Assert.That(utf8, Does.Not.Contain((byte)0));

            var parsed = StringClass.FromJson(json);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed!.Present, Is.EqualTo("hello\u007Fworld\u0085!"));

            var parsedUtf8 = StringClass.FromJsonUtf8(utf8);
            Assert.That(parsedUtf8, Is.Not.Null);
            Assert.That(parsedUtf8!.Present, Is.EqualTo("hello\u007Fworld\u0085!"));
        }

        [Test]
        public void TestDoubleDictionary_SpecialKeys_ExactSizing_NoTrailingNullBytes()
        {
            var model = new DictionaryKeyTypesModel();
            model.DoubleDict[double.NaN] = "nan_val";
            model.DoubleDict[double.PositiveInfinity] = "pos_inf_val";
            model.DoubleDict[double.NegativeInfinity] = "neg_inf_val";

            var json = model.ToJson();
            Assert.That(model.CalculateJsonSize(), Is.EqualTo(json.Length));
            Assert.That(json.Contains('\0'), Is.False);

            var utf8 = model.ToJsonUtf8();
            Assert.That(model.CalculateJsonSizeUtf8(), Is.EqualTo(utf8.Length));
            Assert.That(utf8, Does.Not.Contain((byte)0));

            var parsed = DictionaryKeyTypesModel.FromJson(json);
            Assert.That(parsed, Is.Not.Null);
            Assert.That(parsed!.DoubleDict.Any(kvp => double.IsNaN(kvp.Key) && kvp.Value == "nan_val"), Is.True);
            Assert.That(parsed.DoubleDict[double.PositiveInfinity], Is.EqualTo("pos_inf_val"));
            Assert.That(parsed.DoubleDict[double.NegativeInfinity], Is.EqualTo("neg_inf_val"));

            var parsedUtf8 = DictionaryKeyTypesModel.FromJsonUtf8(utf8);
            Assert.That(parsedUtf8, Is.Not.Null);
            Assert.That(parsedUtf8!.DoubleDict.Any(kvp => double.IsNaN(kvp.Key) && kvp.Value == "nan_val"), Is.True);
            Assert.That(parsedUtf8.DoubleDict[double.PositiveInfinity], Is.EqualTo("pos_inf_val"));
            Assert.That(parsedUtf8.DoubleDict[double.NegativeInfinity], Is.EqualTo("neg_inf_val"));
        }

        [Test]
        public void TestCharSerialization_ZeroHeapAllocations()
        {


            Span<byte> bSpan = stackalloc byte[10];
            int bIndex = 0;
            for (int i = 0; i < 10000; i++)
            {
                bIndex = 0;
                GenJsonWriter.WriteChar(bSpan, ref bIndex, 'A');
            }
            long a = GC.GetAllocatedBytesForCurrentThread();
            long b = GC.GetAllocatedBytesForCurrentThread();
            long c = GC.GetAllocatedBytesForCurrentThread();
            long d = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(d - c, Is.EqualTo(0), $"b-a={b - a}, c-b={c - b}, d-c={d - c}");

            var obj = new CharAllocTestModel { Value = 'X' };
            int charSize = obj.CalculateJsonSize();
            Span<char> charSpan = stackalloc char[charSize];
            int idx = 0;
            for (int i = 0; i < 10000; i++)
            {
                idx = 0;
                obj.WriteJson(charSpan, ref idx);
            }

            long minAlloc = long.MaxValue;
            for (int retry = 0; retry < 10; retry++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                idx = 0;
                obj.WriteJson(charSpan, ref idx);
                long after = GC.GetAllocatedBytesForCurrentThread();
                long diff = after - before;
                if (diff < minAlloc) minAlloc = diff;
            }
            Assert.That(minAlloc, Is.EqualTo(0));

            int utf8Size = obj.CalculateJsonSizeUtf8();
            Span<byte> byteSpan = stackalloc byte[utf8Size];
            int bIdx = 0;
            for (int i = 0; i < 10000; i++)
            {
                bIdx = 0;
                obj.WriteJsonUtf8(byteSpan, ref bIdx);
            }

            long bMinAlloc = long.MaxValue;
            for (int retry = 0; retry < 10; retry++)
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                bIdx = 0;
                obj.WriteJsonUtf8(byteSpan, ref bIdx);
                long after = GC.GetAllocatedBytesForCurrentThread();
                long diff = after - before;
                if (diff < bMinAlloc) bMinAlloc = diff;
            }
            Assert.That(bMinAlloc, Is.EqualTo(0));
        }

        [Test]
        public void TestUriDictionary_Utf8EscapedSlashes()
        {
            var json = """{"UriDict":{"https:\/\/example.com\/api\/v1":42,"\/relative\/test":99}}""";
            var utf8Bytes = Encoding.UTF8.GetBytes(json);

            var modelUtf8 = UriDictionaryModel.FromJsonUtf8(utf8Bytes);
            Assert.That(modelUtf8, Is.Not.Null);
            var uri1 = new Uri("https://example.com/api/v1");
            var uri2 = new Uri("/relative/test", UriKind.Relative);
            Assert.That(modelUtf8!.UriDict.ContainsKey(uri1), Is.True);
            Assert.That(modelUtf8.UriDict[uri1], Is.EqualTo(42));
            Assert.That(modelUtf8.UriDict.ContainsKey(uri2), Is.True);
            Assert.That(modelUtf8.UriDict[uri2], Is.EqualTo(99));

            var modelChar = UriDictionaryModel.FromJson(json);
            Assert.That(modelChar, Is.Not.Null);
            Assert.That(modelChar!.UriDict.ContainsKey(uri1), Is.True);
            Assert.That(modelChar.UriDict[uri1], Is.EqualTo(42));
            Assert.That(modelChar.UriDict.ContainsKey(uri2), Is.True);
            Assert.That(modelChar.UriDict[uri2], Is.EqualTo(99));
        }

        [Test]
        public void TestQuotedNumberParsing_CharParser()
        {
            int index = 0;
            Assert.That(GenJsonParser.TryParseLong("\"12345678901234\"", ref index, out long l), Is.True);
            Assert.That(l, Is.EqualTo(12345678901234L));
            Assert.That(index, Is.EqualTo(16));

            index = 0;
            Assert.That(GenJsonParser.TryParseLong("\"-9876543210\"", ref index, out long? nl), Is.True);
            Assert.That(nl, Is.EqualTo(-9876543210L));
            Assert.That(index, Is.EqualTo(13));

            index = 0;
            Assert.That(GenJsonParser.TryParseULong("\"18446744073709551615\"", ref index, out ulong ul), Is.True);
            Assert.That(ul, Is.EqualTo(ulong.MaxValue));
            Assert.That(index, Is.EqualTo(22));

            index = 0;
            Assert.That(GenJsonParser.TryParseULong("\"12345\"", ref index, out ulong? nul), Is.True);
            Assert.That(nul, Is.EqualTo(12345UL));

            index = 0;
            Assert.That(GenJsonParser.TryParseSByte("\"127\"", ref index, out sbyte sb), Is.True);
            Assert.That(sb, Is.EqualTo((sbyte)127));

            index = 0;
            Assert.That(GenJsonParser.TryParseSByte("\"-128\"", ref index, out sbyte? nsb), Is.True);
            Assert.That(nsb, Is.EqualTo((sbyte)-128));

            // Negative tests
            index = 0;
            Assert.That(GenJsonParser.TryParseLong("\"abc\"", ref index, out long _), Is.False);
            Assert.That(index, Is.EqualTo(0));

            index = 0;
            Assert.That(GenJsonParser.TryParseULong("\"-1\"", ref index, out ulong _), Is.False);
            Assert.That(index, Is.EqualTo(0));

            index = 0;
            Assert.That(GenJsonParser.TryParseSByte("\"200\"", ref index, out sbyte _), Is.False);
            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void TestUtf8TextEnum_SequenceEqual_ZeroArrayAllocations()
        {
            var json = """{"Value":"Two"}""";
            var utf8Bytes = Encoding.UTF8.GetBytes(json);

            // Warmup
            _ = DefaultAsText.FromJsonUtf8(utf8Bytes);

            long before = GC.GetAllocatedBytesForCurrentThread();
            var result = DefaultAsText.FromJsonUtf8(utf8Bytes);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value, Is.EqualTo(DefaultAsTextEnum.Two));
            // Only the DefaultAsText class instance itself is allocated (~24 bytes on 64-bit runtime).
            // No byte[] arrays are allocated for SequenceEqual.
            long allocated = after - before;
            Assert.That(allocated, Is.LessThanOrEqualTo(32));
        }
    }
}
