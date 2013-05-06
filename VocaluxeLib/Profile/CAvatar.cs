using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocaluxeLib.Profile
{
    public class CAvatar
    {
        public string FileName;
        public STexture Texture;

        // ReSharper disable UnusedParameter.Local
        public CAvatar(int dummy)
            // ReSharper restore UnusedParameter.Local
        {
            FileName = String.Empty;
            Texture = new STexture(-1);
        }
    }
}
