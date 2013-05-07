using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VocaluxeLib.Menu;

namespace VocaluxeLib.Profile
{
    public class CAvatar
    {
        public int ID;
        public string FileName;
        public STexture Texture;

        public CAvatar(int ID)
        {
            this.ID = ID;
            FileName = String.Empty;
            Texture = new STexture(-1);
        }

        public bool LoadFromFile(string NewFileName)
        {
            FileName = NewFileName;
            CBase.Drawing.RemoveTexture(ref Texture);
            Texture = CBase.Drawing.AddTexture(FileName);
            return (Texture.Index != -1);
        }

        public void Reload()
        {
            CBase.Drawing.RemoveTexture(ref Texture);
            Texture = CBase.Drawing.AddTexture(FileName);
        }

        public void Unload()
        {
            CBase.Drawing.RemoveTexture(ref Texture);
        }
    }
}
