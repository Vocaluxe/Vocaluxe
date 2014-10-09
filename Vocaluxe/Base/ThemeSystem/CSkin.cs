using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Xml;

namespace Vocaluxe.Base.ThemeSystem
{
    class CVideoSkinElement
    {
        public string FileName;
        public CVideoStream VideoStream;
    }

    class CSkin
    {
        private const int _SkinSystemVersion = 3;

        private readonly string _Folder;
        private readonly string _FileName;
        public readonly int PartyModeID;

        private SInfo _Info;
        public String Name
        {
            get { return _Info.Name; }
        }

        public readonly Dictionary<string, CTextureRef> Textures = new Dictionary<string, CTextureRef>();
        private readonly Dictionary<string, CVideoSkinElement> _Videos = new Dictionary<string, CVideoSkinElement>();

        private readonly Dictionary<string, SColorF> _ColorSchemes = new Dictionary<string, SColorF>();
        private SColorF[] _PlayerColors;

        private bool _IsLoaded;

        public CSkin(string folder, string file, int partyModeId)
        {
            _Folder = folder;
            _FileName = file;
            PartyModeID = partyModeId;
        }

        public override string ToString()
        {
            return Name;
        }

        #region Load
        public bool Init()
        {
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(_Folder, _FileName));
            if (xmlReader == null)
                return false;

            if (!xmlReader.CheckVersion("//root/SkinSystemVersion", _SkinSystemVersion))
                return false;

            bool ok = xmlReader.Read("//root/Info", out _Info);

            if (!ok)
            {
                CLog.LogError("Can't load skin \"" + _FileName + "\". Invalid file!");
                return false;
            }

            return true;
        }

        public bool Load()
        {
            if (_IsLoaded)
                return true;
            Debug.Assert(Textures.Count == 0 && _Videos.Count == 0);
            CXMLReader xmlReader = CXMLReader.OpenFile(Path.Combine(_Folder, _FileName));
            if (xmlReader == null)
                return false;

            // load skins/textures
            List<string> names = xmlReader.GetNames("//root/Skins/*");
            foreach (string name in names)
            {
                string fileName;
                xmlReader.GetValue("//root/Skins/" + name, out fileName);
                CTextureRef texture = CDraw.AddTexture(Path.Combine(_Folder, fileName));
                if (texture == null)
                {
                    CLog.LogError("Error on loading texture \"" + name + "\": " + fileName, true);
                    return false;
                }
                Textures.Add(name, texture);
            }

            // load videos
            names = xmlReader.GetNames("//root/Videos/*");
            foreach (string name in names)
            {
                CVideoSkinElement sk = new CVideoSkinElement();
                xmlReader.GetValue("//root/Videos/" + name, out sk.FileName);
                if (!File.Exists(Path.Combine(_Folder, sk.FileName)))
                {
                    CLog.LogError("Video \"" + name + "\": (" + sk.FileName + ") not found!", true);
                    return false;
                }
                _Videos.Add(name, sk);
            }

            // load colors
            if (!_LoadColors(xmlReader))
                return false;

            _IsLoaded = true;
            return true;
        }

        public void Unload()
        {
            foreach (CTextureRef tex in Textures.Values)
                tex.Dispose();

            foreach (CVideoSkinElement vsk in _Videos.Values)
                CVideo.Close(ref vsk.VideoStream);

            Textures.Clear();
            _Videos.Clear();
            _PlayerColors = null;
            _ColorSchemes.Clear();

            _IsLoaded = false;
        }

        private bool _LoadColors(CXMLReader xmlReader)
        {
            if (PartyModeID == -1)
            {
                _PlayerColors = new SColorF[CSettings.MaxNumPlayer];

                for (int i = 0; i < CSettings.MaxNumPlayer; i++)
                {
                    SColorF color;
                    if (!xmlReader.Read("//root/Colors/Player" + (i + 1), out color))
                        return false;
                    _PlayerColors[i] = color;
                }
            }

            List<string> names = xmlReader.GetNames("//root/ColorSchemes/*");
            foreach (string str in names)
            {
                SColorF color;
                if (!xmlReader.Read("//root/ColorSchemes/" + str, out color))
                    return false;
                _ColorSchemes.Add(str, color);
            }
            return true;
        }
        #endregion Load

        public bool GetColor(string colorName, out SColorF color)
        {
            return _ColorSchemes.TryGetValue(colorName, out color) || _GetPlayerColor(colorName, out color);
        }

        private bool _GetPlayerColor(string playerNrString, out SColorF color)
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
                color = _PlayerColors[playerNr - 1];
                return true;
            }

            color = new SColorF();
            return false;
        }

        public CVideoStream GetVideo(string videoName, bool loop = true)
        {
            CVideoSkinElement sk;
            if (_Videos.TryGetValue(videoName, out sk))
            {
                if (sk.VideoStream == null || sk.VideoStream.IsClosed())
                {
                    sk.VideoStream = CVideo.Load(Path.Combine(_Folder, sk.FileName));
                    CVideo.SetLoop(sk.VideoStream, loop);
                }
                return sk.VideoStream;
            }
            return null;
        }

        public CTextureRef GetTexture(string name)
        {
            CTextureRef texture;
            Textures.TryGetValue(name, out texture);
            return texture;
        }
    }
}