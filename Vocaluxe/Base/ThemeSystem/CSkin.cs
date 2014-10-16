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

    abstract class CSkin
    {
        private const int _SkinSystemVersion = 4;
        protected static readonly List<string> _RequiredTextures = new List<string>();
        protected static readonly List<string> _RequiredVideos = new List<string>();
        protected static readonly List<string> _RequiredColors = new List<string>();

        private readonly string _Folder;
        private readonly string _FileName;
        protected readonly CTheme _Parent;

        private SInfo _Info;
        public String Name
        {
            get { return _Info.Name; }
        }

        protected readonly Dictionary<string, CTextureRef> _Textures = new Dictionary<string, CTextureRef>();
        protected readonly Dictionary<string, CVideoSkinElement> _Videos = new Dictionary<string, CVideoSkinElement>();
        protected readonly Dictionary<string, SColorF> _Colors = new Dictionary<string, SColorF>();

        protected bool _IsLoaded;

        public static bool InitRequiredElements()
        {
            string path = Path.Combine(CSettings.ProgramFolder, CSettings.FileNameRequiredSkinElements);
            CXMLReader xmlReader = CXMLReader.OpenFile(path);
            if (xmlReader == null)
                return false;
            bool ok = xmlReader.Read("//root/Textures", _RequiredTextures);
            ok &= xmlReader.Read("//root/Videos", _RequiredVideos);
            ok &= xmlReader.Read("//root/Colors", _RequiredColors);
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                string name = "Player" + i;
                if (!_RequiredColors.Contains(name))
                    _RequiredColors.Add(name);
            }
            return ok;
        }

        public static void Close()
        {
            _RequiredTextures.Clear();
            _RequiredVideos.Clear();
            _RequiredColors.Clear();
        }

        protected CSkin(string folder, string file, CTheme parent)
        {
            _Folder = folder;
            _FileName = file;
            _Parent = parent;
        }

        public override string ToString()
        {
            return _Parent.Name + ":" + Name;
        }

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
                CLog.LogError("Can't load skin file \"" + _FileName + "\". Invalid file!");
                return false;
            }

            return true;
        }

        public virtual bool Load()
        {
            if (_IsLoaded)
                return true;
            Debug.Assert(_Textures.Count == 0 && _Videos.Count == 0);
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
                _Textures.Add(name, texture);
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

            if (!_LoadColors(xmlReader))
                return false;

            _IsLoaded = true;
            return true;
        }

        public void Unload()
        {
            foreach (CTextureRef tex in _Textures.Values)
                tex.Dispose();

            foreach (CVideoSkinElement vsk in _Videos.Values)
                CVideo.Close(ref vsk.VideoStream);

            _Textures.Clear();
            _Videos.Clear();
            _Colors.Clear();

            _IsLoaded = false;
        }

        private bool _LoadColors(CXMLReader xmlReader)
        {
            List<string> names = xmlReader.GetNames("//root/Colors/*");
            foreach (string str in names)
            {
                SColorF color;
                if (!xmlReader.Read("//root/Colors/" + str, out color))
                    return false;
                _Colors.Add(str, color);
            }
            return true;
        }

        public virtual bool GetColor(string colorName, out SColorF color)
        {
            return _Colors.TryGetValue(colorName, out color);
        }

        public virtual CVideoStream GetVideo(string videoName, bool loop)
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

        public virtual CTextureRef GetTexture(string name)
        {
            CTextureRef texture;
            _Textures.TryGetValue(name, out texture);
            return texture;
        }
    }
}