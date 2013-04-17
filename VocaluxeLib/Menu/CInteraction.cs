namespace VocaluxeLib.Menu
{
    public enum EType
    {
        Background,
        Button,
        SelectSlide,
        Text,
        Static,
        SongMenu,
        Lyric,
        SingNote,
        NameSelection,
        Equalizer,
        Playlist,
        ParticleEffect
    }

    class CInteraction
    {
        private readonly int _Num;
        private readonly EType _Type;

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
                return _Type == EType.Background ||
                       _Type == EType.NameSelection ||
                       _Type == EType.Text ||
                       _Type == EType.Static ||
                       _Type == EType.SongMenu ||
                       _Type == EType.Lyric ||
                       _Type == EType.SingNote ||
                       _Type == EType.Equalizer ||
                       _Type == EType.Playlist;
            }
        }

        public CInteraction(int num, EType type)
        {
            _Num = num;
            _Type = type;
        }
    }
}