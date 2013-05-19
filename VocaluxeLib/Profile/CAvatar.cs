using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VocaluxeLib.Draw;
using VocaluxeLib.Menu;

namespace VocaluxeLib.Profile
{
    public class CAvatar
    {
        public int ID;
        public string FileName = "";
        public CTexture Texture;

        public CAvatar(int ID)
        {
            this.ID = ID;
        }

        public bool LoadFromFile(string NewFileName)
        {
            FileName = NewFileName;
            return Reload();
        }

        public bool Reload()
        {
            CBase.Drawing.RemoveTexture(ref Texture);
            Texture = CBase.Drawing.AddTexture(FileName);

            return Texture != null;
        }

        public void Unload()
        {
            CBase.Drawing.RemoveTexture(ref Texture);
        }
    }
}