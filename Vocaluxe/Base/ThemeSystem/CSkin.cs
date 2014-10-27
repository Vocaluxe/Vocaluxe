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

        protected SSkin _Data;
        public String Name
        {
            get { return _Data.Info.Name; }
        }

        protected readonly Dictionary<string, CTextureRef> _Textures = new Dictionary<string, CTextureRef>();
        protected readonly Dictionary<string, CVideoSkinElement> _Videos = new Dictionary<string, CVideoSkinElement>();

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
            try
            {
                var xml = new CXmlSerializer();
                _Data = xml.Deserialize<SSkin>(Path.Combine(_Folder, _FileName));
                if (_Data.SkinSystemVersion != _SkinSystemVersion)
                {
                    string errorMsg = _Data.SkinSystemVersion < _SkinSystemVersion ? "the file ist outdated!" : "the file is for newer program versions!";
                    errorMsg += " Current Version is " + _SkinSystemVersion;
                    throw new Exception(errorMsg);
                }
            }
            catch (Exception e)
            {
                CLog.LogError("Can't load skin file \"" + _FileName + "\". Invalid file!", false, false, e);
                return false;
            }

            return true;
        }

        public virtual bool Load()
        {
            if (_IsLoaded)
                return true;
            Debug.Assert(_Textures.Count == 0 && _Videos.Count == 0);

            // load skins/textures
            foreach (KeyValuePair<string, string> kvp in _Data.Skins)
            {
                CTextureRef texture = CDraw.AddTexture(Path.Combine(_Folder, kvp.Value));
                if (texture == null)
                {
                    CLog.LogError("Error on loading texture \"" + kvp.Key + "\": " + kvp.Value, true);
                    return false;
                }
                _Textures.Add(kvp.Key, texture);
            }

            // load videos
            foreach (KeyValuePair<string, string> kvp in _Data.Videos)
            {
                CVideoSkinElement sk = new CVideoSkinElement {FileName = kvp.Value};
                if (!File.Exists(Path.Combine(_Folder, sk.FileName)))
                {
                    CLog.LogError("Video \"" + kvp.Key + "\": (" + sk.FileName + ") not found!", true);
                    return false;
                }
                _Videos.Add(kvp.Key, sk);
            }

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

            _IsLoaded = false;
        }

        public virtual bool GetColor(string colorName, out SColorF color)
        {
            return _Data.Colors.TryGetValue(colorName, out color);
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