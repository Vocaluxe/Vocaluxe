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
using System.Xml.Serialization;
using VocaluxeLib;
using VocaluxeLib.Draw;
using VocaluxeLib.Xml;
using System.Linq;

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

        protected struct SRequiredElements
        {
            [XmlIgnore] public List<string> Textures, Videos, Colors;
            [XmlElement("Textures")]
            public string TexturesStr
            {
                get { return string.Join("\r\n", Textures); }
                set
                {
                    string[] entries = value.Split(new string[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
                    Textures = entries.Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
            }
            [XmlElement("Videos")]
            public string VideosStr
            {
                get { return string.Join("\r\n", Videos); }
                set
                {
                    string[] entries = value.Split(new string[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
                    Videos = entries.Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
            }
            [XmlElement("Colors")]
            public string ColorsStr
            {
                get { return string.Join("\r\n", Colors); }
                set
                {
                    string[] entries = value.Split(new string[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
                    Colors = entries.Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
            }
        }

        protected static SRequiredElements _Required;

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
            var xml = new CXmlDeserializer();
            try
            {
                _Required = xml.Deserialize<SRequiredElements>(path);
            }
            catch (CXmlException e)
            {
                CLog.LogError("Error reading required elements: " + e);
                return false;
            }
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                string name = "Player" + i;
                if (!_Required.Colors.Contains(name))
                    _Required.Colors.Add(name);
            }
            return true;
        }

        public static void Close()
        {
            if (_Required.Textures == null)
                return;
            _Required.Textures.Clear();
            _Required.Videos.Clear();
            _Required.Colors.Clear();
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
                var xml = new CXmlDeserializer();
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
                CLog.LogError("Can't load skin file \"" + Path.Combine(_Folder, _FileName) + "\". Invalid file!", false, false, e);
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
                    CLog.LogError("Video \"" + kvp.Key + "\": (" + sk.FileName + ") not found!");
                    continue;
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