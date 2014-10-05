using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Vocaluxe.Lib.Video;
using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base
{
    class CSkinElement
    {
        public string Value;
        public CTextureRef Texture;
    }

    class CVideoSkinElement
    {
        public string Value;
        public CVideoStream VideoStream;
    }

    class CSkin
    {
        private const int _SkinSystemVersion = 3;

        private readonly string _Folder;
        private readonly string _FileName;
        public readonly int PartyModeID;

        public string Name;
        private string _Author;
        private int _SkinVersionMajor;
        private int _SkinVersionMinor;

        public readonly Dictionary<string, CSkinElement> SkinList = new Dictionary<string, CSkinElement>();
        public readonly Dictionary<string, CVideoSkinElement> VideoList = new Dictionary<string, CVideoSkinElement>();

        private readonly Dictionary<string, SColorF> _ColorSchemes = new Dictionary<string, SColorF>();
        private SColorF?[] _PlayerColors;

        public CSkin(string folder, string file, int partyModeId)
        {
            _Folder = folder;
            _FileName = file;
            PartyModeID = partyModeId;
        }

        #region Load/Save
        public bool Init()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(_Folder, _FileName));
            if (xmlReader == null)
                return false;

            int version = 0;
            if (!xmlReader.TryGetIntValue("//root/SkinSystemVersion", ref version) || version != _SkinSystemVersion)
            {
                string msg = "Can't load Skin \"" + _FileName + "\", ";
                if (version < _SkinSystemVersion)
                    msg += "the file ist outdated! ";
                else
                    msg += "the file is for newer program versions! ";

                msg += "Current SkinSystemVersion is " + _SkinSystemVersion;
                CLog.LogError(msg);
                return false;
            }

            bool ok = xmlReader.GetValue("//root/Info/Name", out Name);
            ok &= xmlReader.GetValue("//root/Info/Author", out _Author, String.Empty);
            ok &= xmlReader.TryGetIntValue("//root/Info/SkinVersionMajor", ref _SkinVersionMajor);
            ok &= xmlReader.TryGetIntValue("//root/Info/SkinVersionMinor", ref _SkinVersionMinor);

            if (!ok)
            {
                CLog.LogError("Can't load skin \"" + _FileName + "\". Invalid file!");
                return false;
            }

            return true;
        }

        public bool Load()
        {
            Debug.Assert(SkinList.Count == 0 && VideoList.Count == 0);
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(_Folder, _FileName));
            if (xmlReader == null)
                return false;

            // load skins/textures
            List<string> names = xmlReader.GetNames("//root/Skins/*");
            foreach (string name in names)
            {
                CSkinElement sk = new CSkinElement();
                xmlReader.GetValue("//root/Skins/" + name, out sk.Value);
                sk.Texture = CDraw.AddTexture(Path.Combine(_Folder, sk.Value));
                if (sk.Texture == null)
                {
                    CLog.LogError("Error on loading texture \"" + name + "\": " + sk.Value, true);
                    return false;
                }
                SkinList.Add(name, sk);
            }

            // load videos
            names = xmlReader.GetNames("//root/Videos/*");
            foreach (string name in names)
            {
                CVideoSkinElement sk = new CVideoSkinElement();
                xmlReader.GetValue("//root/Videos/" + name, out sk.Value);
                VideoList.Add(name, sk);
            }

            // load colors
            _LoadColors(xmlReader);
            return true;
        }

        public void Unload()
        {
            foreach (CSkinElement sk in SkinList.Values)
                CDraw.RemoveTexture(ref sk.Texture);

            foreach (CVideoSkinElement vsk in VideoList.Values)
            {
                CVideo.Close(ref vsk.VideoStream);
            }
            SkinList.Clear();
            VideoList.Clear();
            _PlayerColors = null;
            _ColorSchemes.Clear();
        }

        private void _LoadColors(CXMLReader xmlReader)
        {
            if (PartyModeID == -1)
            {
                _PlayerColors = new SColorF?[CSettings.MaxNumPlayer];

                for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                {
                    SColorF color;
                    if (xmlReader.TryGetColorFromRGBA("//root/Colors/Player" + (i + 1), out color))
                        _PlayerColors[i] = color;
                    else
                        _PlayerColors[i] = null;
                }
            }

            List<string> names = xmlReader.GetNames("//root/ColorSchemes/*");
            foreach (string str in names)
            {
                SColorF color;
                if (xmlReader.TryGetColorFromRGBA("//root/ColorSchemes/" + str, out color))
                    _ColorSchemes.Add(str, color);
            }
        }

        public void SaveSkin()
        {
            using (XmlWriter writer = XmlWriter.Create(Path.Combine(_Folder, _FileName), CConfig.XMLSettings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("root");

                // ThemeSystemVersion
                writer.WriteElementString("SkinSystemVersion", _SkinSystemVersion.ToString());

                writer.WriteStartElement("Info");

                writer.WriteElementString("Name", Name);
                writer.WriteElementString("Author", _Author);
                writer.WriteElementString("SkinVersionMajor", _SkinVersionMajor.ToString());
                writer.WriteElementString("SkinVersionMinor", _SkinVersionMinor.ToString());

                writer.WriteEndElement();

                // save colors
                _SaveColors(writer);

                writer.WriteStartElement("Skins");

                foreach (KeyValuePair<string, CSkinElement> element in SkinList)
                    writer.WriteElementString(element.Key, element.Value.Value);
                writer.WriteEndElement();

                writer.WriteStartElement("Videos");

                foreach (KeyValuePair<string, CVideoSkinElement> element in VideoList)
                    writer.WriteElementString(element.Key, element.Value.Value);
                writer.WriteEndElement();

                // End of File
                writer.WriteEndElement(); //end of root
                writer.WriteEndDocument();

                writer.Flush();
            }
        }

        private void _SaveColors(XmlWriter writer)
        {
            if (PartyModeID == -1)
            {
                writer.WriteStartElement("Colors");
                for (int i = 0; i < _PlayerColors.Length; i++)
                {
                    SColorF? color = _PlayerColors[i];
                    if (color == null)
                        continue;
                    writer.WriteStartElement("Player" + (i + 1));

                    writer.WriteElementString("R", color.Value.R.ToString("#0.000"));
                    writer.WriteElementString("G", color.Value.G.ToString("#0.000"));
                    writer.WriteElementString("B", color.Value.B.ToString("#0.000"));
                    writer.WriteElementString("A", color.Value.A.ToString("#0.000"));

                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

            writer.WriteStartElement("ColorSchemes");
            foreach (KeyValuePair<string, SColorF> kvp in _ColorSchemes)
            {
                writer.WriteStartElement(kvp.Key);

                writer.WriteElementString("R", kvp.Value.R.ToString("#0.000"));
                writer.WriteElementString("G", kvp.Value.G.ToString("#0.000"));
                writer.WriteElementString("B", kvp.Value.B.ToString("#0.000"));
                writer.WriteElementString("A", kvp.Value.A.ToString("#0.000"));

                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
        #endregion Load/Save

        public bool GetColor(string colorName, out SColorF color)
        {
            return _ColorSchemes.TryGetValue(colorName, out color) || GetPlayerColor(colorName, out color);
        }

        public bool GetPlayerColor(string playerNrString, out SColorF color)
        {
            color = new SColorF();
            if (playerNrString == null)
                return false;
            if (playerNrString.StartsWith("Player"))
                playerNrString = playerNrString.Substring(6);
            int playerNr;
            if (!int.TryParse(playerNrString, out playerNr))
                return false;

            return GetPlayerColor(playerNr, out color);
        }

        public bool GetPlayerColor(int playerNr, out SColorF color)
        {
            if (_PlayerColors != null && playerNr >= 1 && playerNr <= _PlayerColors.Length)
            {
                SColorF? colorTmp = _PlayerColors[playerNr - 1];
                if (colorTmp != null)
                {
                    color = colorTmp.Value;
                    return true;
                }
            }

            color = new SColorF();
            return false;
        }

        public CVideoStream GetVideo(string videoName, bool loop = true)
        {
            CVideoSkinElement sk;
            if (VideoList.TryGetValue(videoName, out sk))
            {
                if (sk.VideoStream == null || sk.VideoStream.IsClosed())
                {
                    sk.VideoStream = CVideo.Load(Path.Combine(_Folder, sk.Value));
                    CVideo.SetLoop(sk.VideoStream, loop);
                }
                return sk.VideoStream;
            }
            return null;
        }
    }
}