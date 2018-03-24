#region license
// This file is part of Vocaluxe.
// 
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using NUnit.Framework;
using VocaluxeLib;
using VocaluxeLib.Xml;
using Vocaluxe.Base;

namespace Tests.VocaluxeLib.XML
{
    [TestFixture]
    public class CXmlSerializerTest
    {
        private const string _Head = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n";
        private const string _Empty = _Head + @"<root />";

        #region Tests

        [Test]
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
            var xml = new CXmlDeserializer();
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

        [Test]
        public void TestMissingXmlElement()
        {
            string[] s = new string[] { @"<root>
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
</root>" };
            foreach (string s1 in s)
            {
                string sTmp = s1;
                _AssertFail<SBasic, CXmlException>(sTmp);
            }
        }

        [Test]
        public void TestMissingStructElement()
        {
            string[] s = new string[] { @"<root>
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
</root>" };
            foreach (string s1 in s)
            {
                string sTmp = s1;
                _AssertFail<SBasic, CXmlException>(sTmp);
            }
        }

        [Test]
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

        private readonly string[] _XmlList = new string[]
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

        [Test]
        public void TestList()
        {
            var xml = new CXmlDeserializer();
            var ser = new CXmlSerializer();
            SList foo = xml.DeserializeString<SList>(_XmlList[0]);
            Assert.AreEqual(foo.Ints.Count, 2, "Deserialization failed");
            Assert.AreEqual(foo.Ints[0].I, 1, "Deserialization failed");
            Assert.AreEqual(foo.Ints[1].I, 1, "Deserialization failed");
            string res = ser.Serialize(foo);
            Assert.AreEqual(_XmlList[0], res, "Serialization failed");
            foo = xml.DeserializeString<SList>(_XmlList[1]);
            Assert.AreEqual(foo.Ints.Count, 0, "Deserialization2 failed");
            res = ser.Serialize(foo);
            Assert.AreEqual(_XmlList[2], res, "Serialization2 failed");
            foo = xml.DeserializeString<SList>(_XmlList[2]);
            Assert.AreEqual(foo.Ints.Count, 0, "Deserialization2 failed");
            _AssertSerDeserMatch<SList>(_XmlList[2]);
            res = ser.Serialize(foo);
            Assert.AreEqual(_XmlList[2], res, "Serialization2 failed");
            _AssertFail<SList, CXmlException>(_Empty);
        }

        [Test]
        public void TestArray()
        {
            var xml = new CXmlDeserializer();
            var ser = new CXmlSerializer();
            SArray foo = xml.DeserializeString<SArray>(_XmlList[0]);
            Assert.AreEqual(foo.Ints.Length, 2, "Deserialization failed");
            Assert.AreEqual(foo.Ints[0].I, 1, "Deserialization failed");
            Assert.AreEqual(foo.Ints[1].I, 1, "Deserialization failed");
            string res = ser.Serialize(foo);
            Assert.AreEqual(_XmlList[0], res, "Serialization failed");
            foo = xml.DeserializeString<SArray>(_XmlList[1]);
            Assert.AreEqual(foo.Ints.Length, 0, "Deserialization2 failed");
            res = ser.Serialize(foo);
            Assert.AreEqual(_XmlList[2], res, "Serialization2 failed");
            foo = xml.DeserializeString<SArray>(_XmlList[2]);
            Assert.AreEqual(foo.Ints.Length, 0, "Deserialization2 failed");
            res = ser.Serialize(foo);
            Assert.AreEqual(_XmlList[2], res, "Serialization2 failed");
            _AssertFail<SArray, CXmlException>(_Empty);
        }

        private const string _XmlListEmb = _Head + @"<root>
  <I>2</I>
  <Entry>
    <I>1</I>
  </Entry>
  <J>3</J>
</root>";
        private const string _XmlListEmb2 = _Head + @"<root>
  <I>2</I>
  <Entry>
    <I>1</I>
  </Entry>
  <Entry>
    <I>2</I>
  </Entry>
  <J>3</J>
</root>";
        private const string _XmlListEmb3 = _Head + @"<root>
  <I>2</I>
  <J>3</J>
</root>";
        private const string _XmlListEmb4 = _Head + @"<root>
  <I>2</I>
  <Entry />
  <J>3</J>
</root>";

        [Test]
        public void TestListEmbedded()
        {
            var xml = new CXmlDeserializer();
            var ser = new CXmlSerializer();
            SListEmb foo = xml.DeserializeString<SListEmb>(_XmlListEmb);
            Assert.AreEqual(foo.Ints.Count, 1, "Deserialization failed");
            Assert.AreEqual(foo.Ints[0].I, 1, "Deserialization failed");
            string res = ser.Serialize(foo);
            Assert.AreEqual(_XmlListEmb, res, "Serialization failed");
            foo = xml.DeserializeString<SListEmb>(_XmlListEmb2);
            Assert.AreEqual(foo.Ints.Count, 2, "Deserialization failed");
            Assert.AreEqual(foo.Ints[1].I, 2, "Deserialization failed");
            res = ser.Serialize(foo);
            Assert.AreEqual(_XmlListEmb2, res, "Serialization failed");
            foo = xml.DeserializeString<SListEmb>(_XmlListEmb3);
            Assert.AreEqual(foo.Ints.Count, 0, "Deserialization failed");
            res = ser.Serialize(foo);
            Assert.AreEqual(_XmlListEmb3, res, "Serialization failed");
            ser = new CXmlSerializer(true);
            foo.Ints.Clear();
            res = ser.Serialize(foo);
            Assert.AreEqual(_XmlListEmb4, res, "Serialization failed");
            _AssertFail<SListEmb, CXmlException>(_Empty);
        }

        [Test]
        public void TestArrayEmbedded()
        {
            var xml = new CXmlDeserializer();
            var ser = new CXmlSerializer();
            SArrayEmb foo = xml.DeserializeString<SArrayEmb>(_XmlListEmb);
            Assert.AreEqual(foo.Ints.Length, 1, "Deserialization failed");
            Assert.AreEqual(foo.Ints[0].I, 1, "Deserialization failed");
            string res = ser.Serialize(foo);
            Assert.AreEqual(_XmlListEmb, res, "Serialization failed");
            foo = xml.DeserializeString<SArrayEmb>(_XmlListEmb2);
            Assert.AreEqual(foo.Ints.Length, 2, "Deserialization failed");
            Assert.AreEqual(foo.Ints[1].I, 2, "Deserialization failed");
            res = ser.Serialize(foo);
            Assert.AreEqual(_XmlListEmb2, res, "Serialization failed");
            foo = xml.DeserializeString<SArrayEmb>(_XmlListEmb3);
            Assert.AreEqual(foo.Ints.Length, 0, "Deserialization failed");
            res = ser.Serialize(foo);
            Assert.AreEqual(_XmlListEmb3, res, "Serialization failed");
            ser = new CXmlSerializer(true);
            foo.Ints = new SEntry[0];
            res = ser.Serialize(foo);
            Assert.AreEqual(_XmlListEmb4, res, "Serialization failed");
            _AssertFail<SArrayEmb, CXmlException>(_Empty);
        }

        [Test]
        public void TestProperty()
        {
            const string xmlString = _Head + @"<root>
  <Public>2</Public>
  <Auto>3</Auto>
</root>";
            SProperty foo = _AssertSerDeserMatch<SProperty>(xmlString);
            Assert.AreEqual(3, foo.Private);
            Assert.AreEqual(3, foo.Auto);
        }

        [Test]
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

        [Test]
        public void TestIgnore()
        {
            _AssertSerDeserMatch<SIgnore>(_XmlIgnore);
            _AssertFail<SIgnore, CXmlException>(_XmlIgnore2);
        }

        [Test]
        public void TestExisting()
        {
            var foo = new SIgnore { I = 1 };
            var xml = new CXmlDeserializer();
            var ser = new CXmlSerializer();
            SIgnore bar = xml.DeserializeString(_XmlIgnore, foo);
            Assert.AreEqual(1, bar.I);
            Assert.AreEqual(_XmlIgnore, ser.Serialize(bar));

            var foo2 = new CIgnore { I = 1 };
            CIgnore bar2 = xml.DeserializeString(_XmlIgnore, foo2);
            Assert.AreEqual(1, bar2.I);
            Assert.AreEqual(foo2.J, bar2.J, "Original classes should be modified by the deserialization");
            Assert.AreEqual(_XmlIgnore, ser.Serialize(bar2));
        }

        [Test]
        public void TestOnlyList()
        {
            const string s = _Head + @"<root>
  <String>2</String>
  <String>3</String>
</root>";
            _AssertSerDeserMatch<List<string>>(s);
        }

        [Test]
        public void TestOnlyDict()
        {
            const string s = _Head + @"<root>
  <String name=""val1"">2</String>
  <String name=""val2"">3</String>
</root>";
            _AssertSerDeserMatch<Dictionary<string, string>>(s);
        }

        [Test]
        public void TestDefaultValues()
        {
            const string s = _Head + @"<root>
  <I>1</I>
  <S>2</S>
  <F>3</F>
  <D>4</D>
  <Sub>
    <I>22</I>
  </Sub>
</root>";
            _AssertSerDeserMatch<SDefault>(s);
            var xml = new CXmlDeserializer(new CXmlErrorHandler(exception => { }));
            SDefault foo = xml.DeserializeString<SDefault>(@"<root />");
            Assert.AreEqual(foo.I, 1337);
            Assert.AreEqual(foo.F, null);
            Assert.AreEqual(foo.S, "Foo");
            Assert.AreEqual(foo.D, 666);
            Assert.AreEqual(foo.Sub.I, 111);
            string newXml = new CXmlSerializer().Serialize(foo);
            Assert.AreEqual(_Head + @"<root>
  <Sub />
</root>", newXml);
        }

        [Test]
        public void TestRealFiles([Values(typeof(SThemeCover), typeof(CConfig.SConfig), /*typeof(SThemeScreen),*/ typeof(SDefaultFonts), typeof(SSkin), typeof(STheme), typeof(Dictionary<string, string>))] Type type)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "VocaluxeLib", "XML", "TestFiles");

            string xmlPath = Path.Combine(filePath, type.Name + ".xml");
            var deser = new CXmlDeserializer();

            object foo = deser.Deserialize(xmlPath, Activator.CreateInstance(type));

            Assert.IsInstanceOf(type, foo, "Wrong type with " + type.Name);
            var ser = new CXmlSerializer(type == typeof(CConfig.SConfig));
            string newXml = ser.Serialize(foo, type == typeof(Dictionary<string, string>) ? "resources" : null);
            // Typename will be uppercase but input is lowercase
            newXml = newXml.Replace("<String", "<string").Replace("</String", "</string");
            string oldXml = File.ReadAllText(xmlPath);
            Assert.AreEqual(oldXml, newXml, "Recontructed XML has differences.");
        }

        #endregion

        #region Helper

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
            [XmlArray]
            public List<SEntry> Ints;
        }

        private struct SArray
        {
            [XmlArray]
            public SEntry[] Ints;
        }

        private struct SListEmb
        {
            public int I;
            [XmlElement("Entry")]
            public List<SEntry> Ints;
            public int J;
        }

        private struct SArrayEmb
        {
            public int I;
            [XmlElement("Entry")]
            public SEntry[] Ints;
            public int J;
        }

        private struct SProperty
        {
            [XmlIgnore]
            public int Private;
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
            [XmlIgnore]
            public int I;
            public int J;
        }

        private class CIgnore
        {
            [XmlIgnore]
            public int I;
            public int J;
        }

        private struct SDefaultSub
        {
            [DefaultValue(111)]
            public int I;
        }

        private struct SDefault
        {
            [DefaultValue(1337)]
            public int I;
            [DefaultValue("Foo")]
            public string S;
            public float? F;
            [DefaultValue(666.0)]
            public double D;
            public SDefaultSub Sub;
        }

#pragma warning restore 169
#pragma warning restore 649

        private static void _AssertFail<T, T2>(String xmlString) where T2 : Exception where T : new()
        {
            var deserializer = new CXmlDeserializer();

            Exception exception =
                Assert.Catch(() => deserializer.DeserializeString<T>(xmlString));
            Assert.IsInstanceOf(typeof(T2), exception);
        }

        private static T _AssertSerDeserMatch<T>(string xmlString) where T : new()
        {
            var deserializer = new CXmlDeserializer();
            var serializer = new CXmlSerializer();
            T foo = deserializer.DeserializeString<T>(xmlString);
            string xmlNew = serializer.Serialize(foo);
            Assert.AreEqual(xmlString, xmlNew);
            return foo;
        }

        #endregion
    }
}
