using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using VocaluxeLib;

namespace nshToXmlConverter
{
    public static class CConverter
    {
        public static bool ConvertNshToXml(string file)
        {
            string xmlFile = Path.ChangeExtension(file, "xml");

            XmlWriter writer = null;
            StreamReader sr = null;
            try
            {
                writer = XmlWriter.Create(xmlFile);
                sr = new StreamReader(file);

                string language = Path.GetFileNameWithoutExtension(file);
                switch (language)
                {
                    case "Dutch":
                        language = "Nederlands";
                        break;

                    case "English":
                        language = "English";
                        break;

                    case "French":
                        language = "Français";
                        break;

                    case "German":
                        language = "Deutsch";
                        break;

                    case "Hungarian":
                        language = "Magyar";
                        break;

                    case "Spanish":
                        language = "Español";
                        break;

                    case "Turkish":
                        language = "Türkçe";
                        break;

                    default:
                        return false;
                }

                writer.WriteStartDocument();
                writer.WriteStartElement("resources");
                writer.WriteStartElement("string");
                writer.WriteAttributeString("name", "language");
                writer.WriteValue(language);
                writer.WriteEndElement();
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    line = line.Trim();
                    line = line.Substring(11);
                    int namePos = line.IndexOf(' ');
                    string name = line.Substring(0, namePos);
                    line = line.Substring(namePos + 1);
                    int valuePos = line.IndexOf(' ');
                    string value = line.Substring(valuePos + 2);
                    value = value.Substring(0, value.Length - 1);
                    if (value != "" && name != "")
                    {
                        writer.WriteStartElement("string");
                        writer.WriteAttributeString("name", name);
                        writer.WriteValue(value);
                        writer.WriteEndElement();
                    }
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
                writer = null;
                sr.Close();
                sr.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool ConvertXmlToNsh(string file)
        {
            StreamWriter sw = null;
            try
            {
                CXMLReader reader = CXMLReader.OpenFile(file);

                string language = "";
                reader.GetValue("//resources/string[@name='language']", out language, "");

                if (language != "")
                {
                    sw = new StreamWriter(Path.ChangeExtension(file, "nsh"));
                    IEnumerable<string> names = reader.GetAttributes("resources", "name");
                    foreach (string name in names)
                    {
                        string value;
                        if (!reader.GetValue("//resources/string[@name='" + name + "']", out value, ""))
                            continue;
                        sw.WriteLine("LangString " + name + " ${LANG_" + language.ToUpper() + "} \"" + value + "\"");
                    }
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                }
                else
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
