using VocaluxeLib;
using VocaluxeLib.Draw;

namespace Vocaluxe.Base.ThemeSystem
{
    class CPartySkin : CSkin
    {
        private CSkin _BaseSkin;

        public CPartySkin(string folder, string file, CTheme parent) : base(folder, file, parent) {}

        public override bool Load()
        {
            _BaseSkin = CThemes.CurrentThemes[-1].CurrentSkin;
            if (!base.Load())
                return false;
            for (int i = 1; i <= CSettings.MaxNumPlayer; i++)
            {
                if (!_Colors.ContainsKey("Player" + i))
                    continue;
                CLog.LogDebug("Party themes cannot contain player colors. They will be ignored!");
                break;
            }
            return true;
        }

        public override bool GetColor(string name, out SColorF color)
        {
            if (_Parent.Name == "Default" && !_RequiredColors.Contains(name))
                CLog.LogDebug("Non-Default color: " + name);
            return base.GetColor(name, out color) || _BaseSkin.GetColor(name, out color);
        }

        public override CVideoStream GetVideo(string name, bool loop)
        {
            if (_Parent.Name == "Default" && !_RequiredVideos.Contains(name))
                CLog.LogDebug("Non-Default color: " + name);
            return base.GetVideo(name, loop) ?? _BaseSkin.GetVideo(name, loop);
        }

        public override CTextureRef GetTexture(string name)
        {
            if (_Parent.Name == "Default" && !_RequiredTextures.Contains(name))
                CLog.LogDebug("Non-Default color: " + name);
            return base.GetTexture(name) ?? _BaseSkin.GetTexture(name);
        }
    }
}