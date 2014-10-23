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
using System.Linq;

namespace VocaluxeLib.Xml
{
    public abstract class CXmlDeserializer : CXmlReaderBase
    {
        public abstract string FilePath { get; }

        public bool Read(string xPath, out SThemeFont el)
        {
            el = new SThemeFont();
            bool ok = true;
            ok &= GetValue(xPath + "/Name", out el.Name);
            ok &= TryGetFloatValue(xPath + "/Size", ref el.Size);
            ok &= TryGetEnumValue(xPath + "/Style", ref el.Style);
            return ok;
        }

        public bool Read(string xPath, out SMargin el)
        {
            el = new SMargin();
            bool ok = true;
            string val;
            bool defOk = GetAttribute(xPath, "Default", out val) && int.TryParse(val, out el.Default);
            ok &= TryGetIntValue(xPath + "/Left", ref el.Left, el.Default);
            ok &= TryGetIntValue(xPath + "/Right", ref el.Right, el.Default);
            ok &= TryGetIntValue(xPath + "/Top", ref el.Top, el.Default);
            ok &= TryGetIntValue(xPath + "/Bottom", ref el.Bottom, el.Default);
            return ok || defOk;
        }

        public bool Read(string xPath, out SThemeCoverGeneratorText el)
        {
            el = new SThemeCoverGeneratorText();
            bool ok = true;
            ok &= GetValue(xPath + "/Text", out el.Text);
            ok &= Read(xPath + "/Font", out el.Font);
            ok &= Read(xPath + "/Color", out el.Color);
            ok &= Read(xPath + "/Margin", out el.Margin);
            if (ok)
                TryGetIntValue(xPath + "/Indent", ref el.Indent, 6 * (el.Margin.Left + el.Margin.Right) / 2);
            return ok;
        }

        public bool Read(string xPath, out SThemeColor el)
        {
            el = new SThemeColor();
            float val = 0;
            if (TryGetNormalizedFloatValue(xPath + "/R", ref val))
                el.R = val;
            else
                el.R = null;
            if (TryGetNormalizedFloatValue(xPath + "/G", ref val))
                el.G = val;
            else
                el.G = null;
            if (TryGetNormalizedFloatValue(xPath + "/B", ref val))
                el.B = val;
            else
                el.B = null;
            if (TryGetNormalizedFloatValue(xPath + "/A", ref val))
                el.A = val;
            else
                el.A = null;
            GetAttribute(xPath, "Name", out el.Name);
            return true;
        }

        public bool Read(string xPath, out SThemeCoverGenerator el)
        {
            el = new SThemeCoverGenerator();
            bool ok = true;
            string val;
            ok &= GetAttribute(xPath, "Type", out val) && Enum.TryParse(val, out el.Type);
            ok &= Read(xPath + "/Text", out el.Text);
            ok &= Read(xPath + "/BackgroundColor", out el.BackgroundColor);
            ok &= GetValue(xPath + "/Image", out el.Image);
            TryGetNormalizedFloatValue(xPath + "/ImageAlpha", ref el.ImageAlpha, 0.5f);
            TryGetBoolValue(xPath + "/ShowFirstCover", out el.ShowFirstCover);
            return ok;
        }

        public bool Read(string xPath, out SInfo el)
        {
            el = new SInfo();
            bool ok = GetValue(xPath + "/Name", out el.Name);
            ok &= GetValue(xPath + "/Author", out el.Author);
            ok &= TryGetIntValue(xPath + "/VersionMajor", ref el.VersionMajor);
            ok &= TryGetIntValue(xPath + "/VersionMinor", ref el.VersionMinor);
            return ok;
        }

        public bool Read(string xPath, out SColorF value)
        {
            value = new SColorF();
            bool result = true;
            result &= TryGetNormalizedFloatValue(xPath + "/R", ref value.R);
            result &= TryGetNormalizedFloatValue(xPath + "/G", ref value.G);
            result &= TryGetNormalizedFloatValue(xPath + "/B", ref value.B);
            result &= TryGetNormalizedFloatValue(xPath + "/A", ref value.A);
            return result;
        }

        public bool Read(string xPath, List<string> value)
        {
            Debug.Assert(value != null);
            string val;
            if (!GetValue(xPath, out val))
                return false;
            string[] entries = val.Split(new string[] {"\r\n", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            value.AddRange(entries.Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)));
            return true;
        }

        public bool CheckVersion(string xPath, int reqVersion)
        {
            int version = 0;
            string errorMsg;
            if (!TryGetIntValue(xPath, ref version))
                errorMsg = "Version missing";
            else if (version != reqVersion)
            {
                errorMsg = version < reqVersion ? "the file ist outdated!" : "the file is for newer program versions!";
                errorMsg += " Current Version is " + reqVersion;
            }
            else
                return true;
            CBase.Log.LogError("Cannot load " + FilePath + ": " + errorMsg);
            return false;
        }
    }
}