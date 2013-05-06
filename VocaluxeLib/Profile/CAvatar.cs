using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VocaluxeLib.Profile
{
    public class CAvatar
    {
        private int _ID;

        public int ID
        {
            get { return _ID; }
        }

        public string FileName { get; set; }
        public STexture Texture;

        public CAvatar(int ID)
        {
            _ID = ID;
            FileName = String.Empty;
            Texture = new STexture(-1);
        }
    }
}
