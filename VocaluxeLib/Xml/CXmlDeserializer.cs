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
            bool defOk = TryGetIntValue(xPath + "/Default", ref el.Default);
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
            bool ok = true;
            ok &= TryGetNormalizedFloatValue(xPath + "/R", ref el.Color.R);
            ok &= TryGetNormalizedFloatValue(xPath + "/G", ref el.Color.G);
            ok &= TryGetNormalizedFloatValue(xPath + "/B", ref el.Color.B);
            ok &= TryGetNormalizedFloatValue(xPath + "/A", ref el.Color.A);
            if (!ok)
                ok = GetValue(xPath, out el.Name) && !el.Name.Contains('<');
            return ok;
        }

        public bool Read(string xPath, out SThemeCoverGenerator el)
        {
            el = new SThemeCoverGenerator();
            bool ok = true;
            ok &= TryGetEnumValue(xPath + "/Type", ref el.Type);
            ok &= Read(xPath + "/Text", out el.Text);
            ok &= Read(xPath + "/BackgroundColor", out el.BackgroundColor);
            ok &= GetValue(xPath + "/Image", out el.Image);
            TryGetNormalizedFloatValue(xPath + "/ImageAlpha", ref el.ImageAlpha, 0.5f);
            TryGetBoolValue(xPath + "/ShowFirstCover", out el.ShowFirstCover);
            return ok;
        }

        public bool Read(string xPath, out SInfo el)
        {
            el=new SInfo();
            bool ok = GetValue(xPath+"/Name", out el.Name);
            ok &= GetValue(xPath + "/Author", out el.Author);
            ok &= TryGetIntValue(xPath + "/VersionMajor", ref el.VersionMajor);
            ok &= TryGetIntValue(xPath + "/VersionMinor", ref el.VersionMinor);
            return ok;
        }

        public bool CheckVersion(string xPath, int reqVersion)
        {
            int version=0;
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
            CBase.Log.LogError("Cannot load "+FilePath+": "+errorMsg);
            return false;
        }
    }
}