using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VocaluxeLib.Utils.Player;
using VocaluxeLib.Draw;
using VocaluxeLib.Songs;

namespace Vocaluxe.Base
{
    public static class CPreviewPlayer
    {
        private static CSongPlayer _Player = new CSongPlayer(true);

        public static CTexture VideoTexture
        {
            get { return _Player.GetVideoTexture(); }
        }

        public static CTexture Cover
        {
            get { return _Player.Cover; }
        }

        public static bool IsPlaying
        {
            get { return _Player.IsPlaying; }
        }

        public static float Length
        {
            get { return _Player.Length; }
        }

        public static void Load(CSong song, float start = 0f)
        {
            if (song == null)
                return;

            if (start == 0f)
                start = song.Preview.StartTime;

            _Player.Load(song, start);
        }

        public static void Play(float start = -1)
        {
            if (start > -1)
                _Player.Position = start;
            _Player.Play();
        }

        public static void Pause()
        {
            _Player.TogglePause();
        }

        public static void Stop()
        {
            _Player.Stop();
        }

        public static void UpdateVolume()
        {
            _Player.Volume = CConfig.PreviewMusicVolume;
        }
    }
}
