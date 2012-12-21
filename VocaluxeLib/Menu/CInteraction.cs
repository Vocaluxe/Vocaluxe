using System;
using System.Collections.Generic;
using System.Text;

namespace Vocaluxe.Menu
{
    public enum EType
    {
        TBackground,
        TButton,
        TSelectSlide,
        TText,
        TStatic,
        TSongMenu,
        TLyric,
        TSingNote,
        TNameSelection,
        TEqualizer,
        TPlaylist,
        TParticleEffect
    }

    class CInteraction
    {
        private int _Num;
        private EType _Type;

        public int Num
        {
            get { return _Num; }
        }

        public EType Type
        {
            get { return _Type; }
        }

        public bool ThemeEditorOnly
        {
            get
            {
                return (_Type == EType.TBackground ||
                    _Type == EType.TNameSelection ||
                    _Type == EType.TText ||
                    _Type == EType.TStatic ||
                    _Type == EType.TSongMenu ||
                    _Type == EType.TLyric ||
                    _Type == EType.TSingNote ||
                    _Type == EType.TEqualizer ||
                    _Type == EType.TPlaylist);
            }
        }

        public CInteraction(int num, EType type)
        {
            _Num = num;
            _Type = type;
        }
    }
}
