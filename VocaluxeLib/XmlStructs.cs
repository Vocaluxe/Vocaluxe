using System.ComponentModel;
using System.Xml.Serialization;

namespace VocaluxeLib
{
    [XmlType("Song")]
    public struct SPlaylistSong
    {
        public string Artist, Title;
        [DefaultValue(EGameMode.TR_GAMEMODE_NORMAL)] public EGameMode GameMode;
    }
}