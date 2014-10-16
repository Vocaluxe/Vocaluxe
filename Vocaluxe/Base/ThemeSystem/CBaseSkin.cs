using System;
using System.Collections.Generic;

namespace Vocaluxe.Base.ThemeSystem
{
    class CBaseSkin : CSkin
    {
        public CBaseSkin(string folder, string file, CTheme parent) : base(folder, file, parent) {}

        public override bool Load()
        {
            if (!base.Load())
                return false;

            if (!_CheckRequiredElements())
            {
                _IsLoaded = false;
                return false;
            }

            return true;
        }

        private bool _CheckRequiredElements()
        {
            List<string> missingTextures = _RequiredTextures.FindAll(name => !_Textures.ContainsKey(name));
            List<string> missingVideos = _RequiredVideos.FindAll(name => !_Videos.ContainsKey(name));
            List<string> missingColors = _RequiredColors.FindAll(name => !_Colors.ContainsKey(name));
            if (missingTextures.Count + missingVideos.Count + missingColors.Count == 0)
                return true;
            string msg = "The skin \"" + this + "\" is missing the following elements: ";
            if (missingTextures.Count > 0)
                msg += Environment.NewLine + "Textures: " + String.Join(", ", missingTextures);
            if (missingVideos.Count > 0)
                msg += Environment.NewLine + "Videos: " + String.Join(", ", missingVideos);
            if (missingColors.Count > 0)
                msg += Environment.NewLine + "Colors: " + String.Join(", ", missingColors);
            CLog.LogError(msg);
            return false;
        }
    }
}