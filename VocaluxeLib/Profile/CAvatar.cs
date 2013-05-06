using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VocaluxeLib.Menu;

namespace VocaluxeLib.Profile
{
    public class CAvatar
    {
        private int _ID;

        public int ID
        {
            get { return _ID; }
        }

        public string FileName;
        public STexture Texture;

        public CAvatar(int ID)
        {
            _ID = ID;
            FileName = String.Empty;
            Texture = new STexture(-1);
        }

        public void LoadFromFile(string NewFileName)
        {
            FileName = NewFileName;
            CBase.Drawing.RemoveTexture(ref Texture);
            Texture = CBase.Drawing.AddTexture(FileName);
        }

        public void Reload()
        {
            CBase.Drawing.RemoveTexture(ref Texture);
            Texture = CBase.Drawing.AddTexture(FileName);
        }
    }
}
