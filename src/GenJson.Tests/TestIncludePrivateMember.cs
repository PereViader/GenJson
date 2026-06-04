using NUnit.Framework;
using System;

namespace GenJson.Tests
{
    [TestFixture]
    public class TestIncludePrivateMember
    {
        [Test]
        public void TestIndividualPrivateMembers()
        {
            var obj = new ClassWithIndividualPrivateMembers();
            obj.SetPrivateStringField("hello");
            obj.SetPrivateIntProperty(123);

            var expected = """{"_privateStringField":"hello","PrivateIntProperty":123}""";
            var json = obj.ToJson();
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = ClassWithIndividualPrivateMembers.FromJson(expected)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.GetPrivateStringField(), Is.EqualTo("hello"));
            Assert.That(deserialized.GetPrivateIntProperty(), Is.EqualTo(123));
            Assert.That(deserialized.GetIgnoredPrivateField(), Is.EqualTo(3.14)); // Should not be affected

            var utf8Json = obj.ToJsonUtf8();
            var utf8Deserialized = ClassWithIndividualPrivateMembers.FromJsonUtf8(utf8Json)!;
            Assert.That(utf8Deserialized, Is.Not.Null);
            Assert.That(utf8Deserialized.GetPrivateStringField(), Is.EqualTo("hello"));
            Assert.That(utf8Deserialized.GetPrivateIntProperty(), Is.EqualTo(123));
        }

        [Test]
        public void TestClassWithAllPrivateMembers()
        {
            var obj = new ClassWithAllPrivateMembers();
            obj.SetField1("test1");
            obj.SetField2(555);
            obj.SetAutoProp("propVal");

            // Order of fields in C# class depends on metadata order.
            // Usually, fields and properties are returned in definition order:
            // _field1, _field2, AutoProp (backing field is skipped, AutoProp itself is serialized).
            var expected = """{"_field1":"test1","_field2":555,"AutoProp":"propVal"}""";
            var json = obj.ToJson();
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = ClassWithAllPrivateMembers.FromJson(expected)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.GetField1(), Is.EqualTo("test1"));
            Assert.That(deserialized.GetField2(), Is.EqualTo(555));
            Assert.That(deserialized.GetAutoProp(), Is.EqualTo("propVal"));

            var utf8Json = obj.ToJsonUtf8();
            var utf8Deserialized = ClassWithAllPrivateMembers.FromJsonUtf8(utf8Json)!;
            Assert.That(utf8Deserialized, Is.Not.Null);
            Assert.That(utf8Deserialized.GetField1(), Is.EqualTo("test1"));
            Assert.That(utf8Deserialized.GetField2(), Is.EqualTo(555));
            Assert.That(utf8Deserialized.GetAutoProp(), Is.EqualTo("propVal"));
        }

        [Test]
        public void TestStructWithAllPrivateMembers()
        {
            var obj = new StructWithAllPrivateMembers("str", 77);
            var expected = """{"_structField":"str","_structProp":77}""";
            var json = obj.ToJson();
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = StructWithAllPrivateMembers.FromJson(expected)!.Value;
            Assert.That(deserialized.GetField(), Is.EqualTo("str"));
            Assert.That(deserialized.GetProp(), Is.EqualTo(77));
        }

        [Test]
        public void TestRecordWithAllPrivateMembers()
        {
            var obj = new RecordWithAllPrivateMembers();
            obj.SetField("recordTest");

            var expected = """{"_recordField":"recordTest"}""";
            var json = obj.ToJson();
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = RecordWithAllPrivateMembers.FromJson(expected)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.GetField(), Is.EqualTo("recordTest"));
        }

        [Test]
        public void TestInheritanceWithPrivateAndProtected()
        {
            var obj = new ChildPrivateClass();
            // Note: _parentPrivate is NOT serialized because it's private to base class ParentPrivateClass,
            // but _parentProtected is serialized because it is protected and accessible to ChildPrivateClass.
            // _childPrivate is serialized because it is private to ChildPrivateClass.
            var expected = """{"_parentProtected":"protected","_childPrivate":"child"}""";
            var json = obj.ToJson();
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = ChildPrivateClass.FromJson(expected)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.GetParentProtected(), Is.EqualTo("protected"));
            Assert.That(deserialized.GetChildPrivate(), Is.EqualTo("child"));
            // _parentPrivate should remain default since it wasn't serialized/deserialized
            Assert.That(deserialized.GetParentPrivate(), Is.EqualTo("parent"));
        }

        [Test]
        public void TestPrivateReadonlyAndConstFields()
        {
            var obj = new ClassWithPrivateReadonlyAndConst(42);
            // _readonlyFieldCtor is serialized (constructor arg).
            // _readonlyFieldNoCtor is ignored (readonly & not constructor arg).
            // _constField is ignored (const is static).
            var expected = """{"_readonlyFieldCtor":42}""";
            var json = obj.ToJson();
            Assert.That(json, Is.EqualTo(expected));

            var deserialized = ClassWithPrivateReadonlyAndConst.FromJson(expected)!;
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.GetReadonlyFieldCtor(), Is.EqualTo(42));
            Assert.That(deserialized.GetReadonlyFieldNoCtor(), Is.EqualTo(999));
        }
    }

    [GenJson]
    public partial class ClassWithIndividualPrivateMembers
    {
        [GenJsonIncludePrivateMember]
        private string _privateStringField = "default_field";

        [GenJsonIncludePrivateMember]
        private int PrivateIntProperty { get; set; } = 42;

        // This one should be ignored/not-serialized because it lacks the attribute
        private double _ignoredPrivateField = 3.14;

        public void SetPrivateStringField(string val) => _privateStringField = val;
        public string GetPrivateStringField() => _privateStringField;

        public void SetPrivateIntProperty(int val) => PrivateIntProperty = val;
        public int GetPrivateIntProperty() => PrivateIntProperty;

        public double GetIgnoredPrivateField() => _ignoredPrivateField;
    }

    [GenJson]
    [GenJsonIncludePrivateMember]
    public partial class ClassWithAllPrivateMembers
    {
        private string _field1 = "f1";
        private int _field2 = 100;
        
        private string AutoProp { get; set; } = "auto";

        public void SetField1(string val) => _field1 = val;
        public string GetField1() => _field1;
        public void SetField2(int val) => _field2 = val;
        public int GetField2() => _field2;
        public void SetAutoProp(string val) => AutoProp = val;
        public string GetAutoProp() => AutoProp;
    }

    [GenJson]
    [GenJsonIncludePrivateMember]
    public partial struct StructWithAllPrivateMembers
    {
        private string _structField;
        private int _structProp { get; set; }

        public StructWithAllPrivateMembers(string f, int p)
        {
            _structField = f;
            _structProp = p;
        }

        public string GetField() => _structField;
        public int GetProp() => _structProp;
    }

    [GenJson]
    [GenJsonIncludePrivateMember]
    public partial record RecordWithAllPrivateMembers
    {
        private string _recordField = "rf";
        
        public string GetField() => _recordField;
        public void SetField(string val) => _recordField = val;
    }

    [GenJson]
    [GenJsonIncludePrivateMember]
    public partial class ParentPrivateClass
    {
        private string _parentPrivate = "parent";
        protected string _parentProtected = "protected";

        public string GetParentPrivate() => _parentPrivate;
        public string GetParentProtected() => _parentProtected;
    }

    [GenJson]
    [GenJsonIncludePrivateMember]
    public partial class ChildPrivateClass : ParentPrivateClass
    {
        private string _childPrivate = "child";

        public string GetChildPrivate() => _childPrivate;
    }

    [GenJson]
    [GenJsonIncludePrivateMember]
    public partial class ClassWithPrivateReadonlyAndConst
    {
        private readonly int _readonlyFieldCtor;
        private readonly int _readonlyFieldNoCtor = 999;
        private const int _constField = 12345;

        public ClassWithPrivateReadonlyAndConst(int _readonlyFieldCtor)
        {
            this._readonlyFieldCtor = _readonlyFieldCtor;
        }

        public int GetReadonlyFieldCtor() => _readonlyFieldCtor;
        public int GetReadonlyFieldNoCtor() => _readonlyFieldNoCtor;
        public int GetConstField() => _constField;
    }
}
