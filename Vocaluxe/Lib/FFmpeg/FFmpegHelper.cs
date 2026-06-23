using System;
using System.IO;
using FFmpeg.AutoGen;
using Vocaluxe.Base;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib.FFmpeg
{
    internal static class FFmpegHelper
    {
        public const int IOBufferSize = 4096;

        internal static void PrepareFFmpegBinaries()
        {
            var ffmpegPath = CConfig.Config.FFmpeg.FFmpegPath;
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                throw new ArgumentException("FFmpeg path was not provided in configuration");
            }

            ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), ffmpegPath);
            ffmpeg.RootPath = ffmpegPath;
            var version = _TryGetVersion();
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException($"Unable to find FFmpeg binaries in {ffmpegPath}");
            }

            CLog.Information($"FFmpeg binaries version {version} found in {ffmpegPath}");
        }

        private static string _TryGetVersion()
        {
            try
            {
                return ffmpeg.av_version_info();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
