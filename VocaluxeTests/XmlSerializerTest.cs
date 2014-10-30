using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vocaluxe.Base;
using VocaluxeLib;
using VocaluxeLib.Menu;
using VocaluxeLib.Xml;

namespace VocaluxeTests
{
    [TestClass]
    public class CXmlSerializerTest
    {
        private const string _Head = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n";
        private const string _Empty = _Head + @"<root />";

#pragma warning disable 649
#pragma warning disable 169
        private struct SBasic
        {
            public int I;
            public string S;
            public float F;
            public double D;
        }

        [XmlType("Entry")]
        private struct SEntry
        {
            public int I;
        }

        private struct SList
        {
            [XmlArray] public List<SEntry> Ints;
        }

        private struct SArray
        {
            [XmlArray] public SEntry[] Ints;
        }

        private struct SListEmb
        {
            public int I;
            [XmlElement("Entry")] public List<SEntry> Ints;
            public int J;
        }

        private struct SArrayEmb
        {
            public int I;
            [XmlElement("Entry")] public SEntry[] Ints;
            public int J;
        }

        private struct SProperty
        {
            [XmlIgnore] public int Private;
            // ReSharper disable UnusedMember.Local
            public int Public
            {
                get { return Private - 1; }
                set { Private = value + 1; }
            }
            // ReSharper restore UnusedMember.Local
            // ReSharper disable UnusedAutoPropertyAccessor.Local
            public int Auto { get; set; }
            // ReSharper restore UnusedAutoPropertyAccessor.Local
        }

        private struct SBytes
        {
            public byte[] B;
        }

        private struct SIgnore
        {
            [XmlIgnore] public int I;
            public int J;
        }

        private class CIgnore
        {
            [XmlIgnore] public int I;
            public int J;
        }
#pragma warning restore 169
#pragma warning restore 649

        private static void _AssertFail<T>(Action test) where T : Exception
        {
            try
            {
                test.Invoke();
                Assert.Fail("Exception " + typeof(T).Name + " not thrown!");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(T));
            }
        }

        private static void _AssertFail<T, T2>(String xmlString) where T2 : Exception where T : new()
        {
            var xml = new CXmlSerializer();
            _AssertFail<T2>(() => xml.DeserializeString<T>(xmlString));
        }

        private static T _AssertSerDeserMatch<T>(string xmlString) where T : new()
        {
            var xml = new CXmlSerializer();
            T foo = xml.DeserializeString<T>(xmlString);
            string xmlNew = xml.Serialize(foo);
            Assert.AreEqual(xmlString, xmlNew);
            return foo;
        }

        [TestMethod]
        public void TestBasic()
        {
            const string s = @"<root>
  <I>1</I>
  <S>2</S>
  <F>3</F>
  <D>4</D>
</root>";
            const string sUnordered = @"<root>
  <D>4</D>
  <I>1</I>
  <F>3</F>
  <S>2</S>
</root>";
            var xml = new CXmlSerializer();
            SBasic foo = xml.DeserializeString<SBasic>(s);
            Assert.AreEqual(1, foo.I);
            Assert.AreEqual("2", foo.S);
            Assert.AreEqual(3, foo.F, 0.0001);
            Assert.AreEqual(4, foo.D, 0.0001);
            foo = xml.DeserializeString<SBasic>(sUnordered);
            Assert.AreEqual(1, foo.I);
            Assert.AreEqual("2", foo.S);
            Assert.AreEqual(3, foo.F, 0.0001);
            Assert.AreEqual(4, foo.D, 0.0001);
        }

        [TestMethod]
        public void TestMissingXmlElement()
        {
            string[] s = new string[] {@"<root>
  <S>2</S>
  <F>3</F>
  <D>4</D>
</root>", @"<root>
  <I>1</I>
  <F>3</F>
  <D>4</D>
</root>", @"<root>
  <I>1</I>
  <S>2</S>
  <F>3</F>
</root>"};
            foreach (string s1 in s)
            {
                string sTmp = s1;
                _AssertFail<SBasic, XmlException>(sTmp);
            }
        }

        [TestMethod]
        public void TestMissingStructElement()
        {
            string[] s = new string[] {@"<root>
  <Foo>1</Foo>
  <I>1</I>
  <S>2</S>
  <F>3</F>
  <D>4</D>
</root>", @"<root>
  <I>1</I>
  <Foo>1</Foo>
  <S>2</S>
  <F>3</F>
  <D>4</D>
</root>", @"<root>
  <I>1</I>
  <S>2</S>
  <F>3</F>
  <D>4</D>
  <Foo>1</Foo>
</root>"};
            foreach (string s1 in s)
            {
                string sTmp = s1;
                _AssertFail<SBasic, XmlException>(sTmp);
            }
        }

        [TestMethod]
        public void TestBasicSerialization()
        {
            const string s = _Head + @"<root>
  <I>1</I>
  <S>2</S>
  <F>3</F>
  <D>4</D>
</root>";
            _AssertSerDeserMatch<SBasic>(s);
        }

        private readonly string[] _XMLList = new string[]
            {
                _Head + @"<root>
  <Ints>
    <Entry>
      <I>1</I>
    </Entry>
    <Entry>
      <I>1</I>
    </Entry>
  </Ints>
</root>",
                _Head + @"<root>
  <Ints>
  </Ints>
</root>",
                _Head + @"<root>
  <Ints />
</root>"
            };

        [TestMethod]
        public void TestList()
        {
            var xml = new CXmlSerializer();
            SList foo = xml.DeserializeString<SList>(_XMLList[0]);
            Assert.AreEqual(foo.Ints.Count, 2, "Deserialization failed");
            Assert.AreEqual(foo.Ints[0].I, 1, "Deserialization failed");
            Assert.AreEqual(foo.Ints[1].I, 1, "Deserialization failed");
            string res = xml.Serialize(foo);
            Assert.AreEqual(_XMLList[0], res, "Serialization failed");
            foo = xml.DeserializeString<SList>(_XMLList[1]);
            Assert.AreEqual(foo.Ints.Count, 0, "Deserialization2 failed");
            res = xml.Serialize(foo);
            Assert.AreEqual(_XMLList[2], res, "Serialization2 failed");
            foo = xml.DeserializeString<SList>(_XMLList[2]);
            Assert.AreEqual(foo.Ints.Count, 0, "Deserialization2 failed");
            res = xml.Serialize(foo);
            Assert.AreEqual(_XMLList[2], res, "Serialization2 failed");
            _AssertFail<SList, XmlException>(_Empty);
        }

        [TestMethod]
        public void TestArray()
        {
            var xml = new CXmlSerializer();
            SArray foo = xml.DeserializeString<SArray>(_XMLList[0]);
            Assert.AreEqual(foo.Ints.Length, 2, "Deserialization failed");
            Assert.AreEqual(foo.Ints[0].I, 1, "Deserialization failed");
            Assert.AreEqual(foo.Ints[1].I, 1, "Deserialization failed");
            string res = xml.Serialize(foo);
            Assert.AreEqual(_XMLList[0], res, "Serialization failed");
            foo = xml.DeserializeString<SArray>(_XMLList[1]);
            Assert.AreEqual(foo.Ints.Length, 0, "Deserialization2 failed");
            res = xml.Serialize(foo);
            Assert.AreEqual(_XMLList[2], res, "Serialization2 failed");
            foo = xml.DeserializeString<SArray>(_XMLList[2]);
            Assert.AreEqual(foo.Ints.Length, 0, "Deserialization2 failed");
            res = xml.Serialize(foo);
            Assert.AreEqual(_XMLList[2], res, "Serialization2 failed");
            _AssertFail<SArray, XmlException>(_Empty);
        }

        private const string _XMLListEmb = _Head + @"<root>
  <I>2</I>
  <Entry>
    <I>1</I>
  </Entry>
  <J>3</J>
</root>";
        private const string _XMLListEmb2 = _Head + @"<root>
  <I>2</I>
  <Entry>
    <I>1</I>
  </Entry>
  <Entry>
    <I>2</I>
  </Entry>
  <J>3</J>
</root>";
        private const string _XMLListEmb3 = _Head + @"<root>
  <I>2</I>
  <J>3</J>
</root>";

        [TestMethod]
        public void TestListEmbedded()
        {
            var xml = new CXmlSerializer();
            SListEmb foo = xml.DeserializeString<SListEmb>(_XMLListEmb);
            Assert.AreEqual(foo.Ints.Count, 1, "Deserialization failed");
            Assert.AreEqual(foo.Ints[0].I, 1, "Deserialization failed");
            string res = xml.Serialize(foo);
            Assert.AreEqual(_XMLListEmb, res, "Serialization failed");
            foo = xml.DeserializeString<SListEmb>(_XMLListEmb2);
            Assert.AreEqual(foo.Ints.Count, 2, "Deserialization failed");
            Assert.AreEqual(foo.Ints[1].I, 2, "Deserialization failed");
            res = xml.Serialize(foo);
            Assert.AreEqual(_XMLListEmb2, res, "Serialization failed");
            foo = xml.DeserializeString<SListEmb>(_XMLListEmb3);
            Assert.AreEqual(foo.Ints.Count, 0, "Deserialization failed");
            res = xml.Serialize(foo);
            Assert.AreEqual(_XMLListEmb3, res, "Serialization failed");
            _AssertFail<SListEmb, XmlException>(_Empty);
        }

        [TestMethod]
        public void TestArrayEmbedded()
        {
            var xml = new CXmlSerializer();
            SArrayEmb foo = xml.DeserializeString<SArrayEmb>(_XMLListEmb);
            Assert.AreEqual(foo.Ints.Length, 1, "Deserialization failed");
            Assert.AreEqual(foo.Ints[0].I, 1, "Deserialization failed");
            string res = xml.Serialize(foo);
            Assert.AreEqual(_XMLListEmb, res, "Serialization failed");
            foo = xml.DeserializeString<SArrayEmb>(_XMLListEmb2);
            Assert.AreEqual(foo.Ints.Length, 2, "Deserialization failed");
            Assert.AreEqual(foo.Ints[1].I, 2, "Deserialization failed");
            res = xml.Serialize(foo);
            Assert.AreEqual(_XMLListEmb2, res, "Serialization failed");
            foo = xml.DeserializeString<SArrayEmb>(_XMLListEmb3);
            Assert.AreEqual(foo.Ints.Length, 0, "Deserialization failed");
            res = xml.Serialize(foo);
            Assert.AreEqual(_XMLListEmb3, res, "Serialization failed");
            _AssertFail<SArrayEmb, XmlException>(_Empty);
        }

        [TestMethod]
        public void TestProperty()
        {
            const string xmlString = _Head + @"<root>
  <Public>2</Public>
  <Auto>3</Auto>
</root>";
            var xml = new CXmlSerializer();
            SProperty foo = xml.DeserializeString<SProperty>(xmlString);
            Assert.AreEqual(3, foo.Private);
            Assert.AreEqual(3, foo.Auto);
            string newXml = xml.Serialize(foo);
            Assert.AreEqual(xmlString, newXml);
        }

        [TestMethod]
        public void TestByteArray()
        {
            const string xmlString = _Head + @"<root>
  <B>MTMzNw==</B>
</root>";
            _AssertSerDeserMatch<SBytes>(xmlString);
        }

        private const string _XmlIgnore = _Head + @"<root>
  <J>2</J>
</root>";
        private const string _XmlIgnore2 = _Head + @"<root>
  <I>2</I>
  <J>2</J>
</root>";

        [TestMethod]
        public void TestIgnore()
        {
            _AssertSerDeserMatch<SIgnore>(_XmlIgnore);
            _AssertFail<SIgnore, XmlException>(_XmlIgnore2);
        }

        [TestMethod]
        public void TestExisting()
        {
            var foo = new SIgnore {I = 1};
            var xml = new CXmlSerializer();
            SIgnore bar = xml.DeserializeString(_XmlIgnore, foo);
            Assert.AreEqual(1, bar.I);
            Assert.AreEqual(_XmlIgnore, xml.Serialize(bar));

            var foo2 = new CIgnore {I = 1};
            CIgnore bar2 = xml.DeserializeString(_XmlIgnore, foo2);
            Assert.AreEqual(1, bar2.I);
            Assert.AreEqual(foo2.J, bar2.J, "Original classes should be modified by the deserialization");
            Assert.AreEqual(_XmlIgnore, xml.Serialize(bar2));
        }

        [TestMethod]
        public void TestOnlyList()
        {
            const string s = _Head + @"<root>
  <String>2</String>
  <String>3</String>
</root>";
            _AssertSerDeserMatch<List<string>>(s);
        }

        [TestMethod]
        public void TestOnlyDict()
        {
            const string s = _Head + @"<root>
  <String name=""val1"">2</String>
  <String name=""val2"">3</String>
</root>";
            _AssertSerDeserMatch<Dictionary<string, string>>(s);
        }

        [TestMethod]
        public void TestRealFiles()
        {
            Type[] types = new Type[] {typeof(SThemeCover), typeof(CConfig.SConfig), typeof(SThemeScreen), typeof(SDefaultFonts), typeof(SSkin), typeof(STheme)};
            string filePath = Path.Combine(new string[] {AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TestXmlFiles"});
            foreach (Type type in types)
            {
                string xmlPath = Path.Combine(filePath, type.Name + ".xml");
                var xml = new CXmlSerializer(type == typeof(CConfig.SConfig));
                object foo = null;
                try
                {
                    foo = xml.Deserialize(xmlPath, Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    Assert.Fail("Exception with " + type.Name + ": {0}", new object[] {e.Message});
                }
                Assert.IsInstanceOfType(foo, type, "Wrong type with " + type.Name);
                string newXml = xml.Serialize(foo);
                string oldXml = File.ReadAllText(xmlPath);
                Assert.AreEqual(oldXml, newXml, "Error with " + type.Name);
            }
        }
    }
}