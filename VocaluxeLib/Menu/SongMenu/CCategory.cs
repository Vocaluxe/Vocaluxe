using System.Collections.Generic;

namespace VocaluxeLib.Menu.SongMenu
{
    public class CCategory
    {
        public readonly string Name;
        private STexture _CoverTextureSmall = new STexture(-1);
        private STexture _CoverTextureBig = new STexture(-1);
        private bool _CoverBigLoaded;
        public readonly List<CSongPointer> Songs = new List<CSongPointer>();

        public CCategory(string name)
        {
            Name = name;
        }

        public STexture CoverTextureSmall
        {
            get { return _CoverTextureSmall; }

            set { _CoverTextureSmall = value; }
        }

        public STexture CoverTextureBig
        {
            get { return _CoverBigLoaded ? _CoverTextureBig : _CoverTextureSmall; }
            set
            {
                if (value.Index == -1)
                    return;
                _CoverTextureBig = value;
                _CoverBigLoaded = true;
            }
        }

        public CCategory(string name, STexture coverSmall, STexture coverBig)
        {
            Name = name;
            CoverTextureSmall = coverSmall;
            CoverTextureBig = coverBig;
        }

        public CCategory(CCategory cat)
        {
            Name = cat.Name;
            CoverTextureSmall = cat.CoverTextureSmall;
            CoverTextureBig = cat.CoverTextureBig;
        }
    }
}