using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string sHead = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n";
        private const string sEmpty = sHead + @"<root />";

        private struct SBasic
        {
            public int I;
            public string S;
            public float F;
            public double D;
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
            var xml = new CXmlSerializer();
            SBasic foo = xml.DeserializeString<SBasic>(s);
            Assert.AreEqual(1, foo.I);
            Assert.AreEqual("2", foo.S);
            Assert.AreEqual(3, foo.F, 0.0001);
            Assert.AreEqual(4, foo.D, 0.0001);
        }

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
            var xml = new CXmlSerializer();
            foreach (string s1 in s)
            {
                string sTmp = s1;
                _AssertFail<XmlException>(() => xml.DeserializeString<SBasic>(sTmp));
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
            var xml = new CXmlSerializer();
            foreach (string s1 in s)
            {
                string sTmp = s1;
                _AssertFail<XmlException>(() => xml.DeserializeString<SBasic>(sTmp));
            }
        }

        [TestMethod]
        public void TestBasicSerialization()
        {
            const string s = sHead + @"<root>
  <I>1</I>
  <S>2</S>
  <F>3</F>
  <D>4</D>
</root>";
            var xml = new CXmlSerializer();
            SBasic foo = xml.DeserializeString<SBasic>(s);
            string s2 = xml.Serialize(foo);
            Assert.AreEqual(s, s2);
        }

        private readonly string[] _XMLList = new string[]
            {
                sHead + @"<root>
  <Ints>
    <Entry>
      <I>1</I>
    </Entry>
    <Entry>
      <I>1</I>
    </Entry>
  </Ints>
</root>",
                sHead + @"<root>
  <Ints>
  </Ints>
</root>",
                sHead + @"<root>
  <Ints />
</root>"
            };

        [XmlType("Entry")]
        private struct SEntry
        {
            public int I;
        }

        private struct SList
        {
            [XmlArray] public List<SEntry> Ints;
        }

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
            _AssertFail<XmlException>(() => xml.DeserializeString<SList>(sEmpty));
        }

        private struct SArray
        {
            [XmlArray] public SEntry[] Ints;
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
            _AssertFail<XmlException>(() => xml.DeserializeString<SArray>(sEmpty));
        }

        private const string _XMLListEmb = sHead + @"<root>
  <I>2</I>
  <Entry>
    <I>1</I>
  </Entry>
  <J>3</J>
</root>";
        private const string _XMLListEmb2 = sHead + @"<root>
  <I>2</I>
  <Entry>
    <I>1</I>
  </Entry>
  <Entry>
    <I>2</I>
  </Entry>
  <J>3</J>
</root>";
        private const string _XMLListEmb3 = sHead + @"<root>
  <I>2</I>
  <J>3</J>
</root>";

        private struct SListEmb
        {
            public int I;
            [XmlElement("Entry")] public List<SEntry> Ints;
            public int J;
        }

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
            _AssertFail<XmlException>(() => xml.DeserializeString<SListEmb>(sEmpty));
        }

        private struct SArrayEmb
        {
            public int I;
            [XmlElement("Entry")] public SEntry[] Ints;
            public int J;
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
            _AssertFail<XmlException>(() => xml.DeserializeString<SArrayEmb>(sEmpty));
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
                object foo = xml.GetType().GetMethod("Deserialize").MakeGenericMethod(new Type[] {type}).Invoke(xml, new object[] {xmlPath});
                Assert.IsInstanceOfType(foo, type);
                string newXml = xml.Serialize(foo);
                string oldXml = File.ReadAllText(xmlPath);
                Assert.AreEqual(oldXml, newXml, "Error in " + type.Name);
            }
        }
    }
}