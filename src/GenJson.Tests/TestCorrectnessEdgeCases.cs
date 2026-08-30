using System;
using System.Collections.Generic;
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
    }
}
