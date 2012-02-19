using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Vocaluxe.Base;

namespace Vocaluxe.Base
{
    static class CBackgroundMusic
    {
        private static CHelper Helper = new CHelper();
        private static List<int> _BackgroundMusicStreams = new List<int>();

        public static void init()
        {
            List<string> files = new List<string>();
            files.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.mp3", true, true));
            files.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wav", true, true));
            files.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.ogg", true, true));
            files.AddRange(Helper.ListFiles(CSettings.sFolderBackgroundMusic, "*.wma", true, true));

            foreach (string file in files)
            {
                _BackgroundMusicStreams.Add(CSound.Load(file));
            }
        }

        public static void Play()
        {
            if (CConfig.BackgroundMusic == EOffOn.TR_CONFIG_ON)
            {
                foreach (int i in _BackgroundMusicStreams)
                {
                    CSound.Fade(i, CConfig.BackgroundMusicVolume, CConfig.FadeTime);
                    CSound.Play(i);
                }
            }
        }

        public static void Stop()
        {
            foreach (int i in _BackgroundMusicStreams)
            {
                CSound.FadeAndStop(i, CConfig.BackgroundMusicVolume, CConfig.FadeTime);
            }
        }

        public static void Pause()
        {
            foreach (int i in _BackgroundMusicStreams)
            {
                CSound.Pause(i);
            }
        }
    }
}
