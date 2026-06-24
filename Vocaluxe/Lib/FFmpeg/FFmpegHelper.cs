using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Vocaluxe.Base;
using VocaluxeLib.Log;

namespace Vocaluxe.Lib
{
    internal static class FFmpegHelper
    {
        public const int IOBufferSize = 4096;
        public const string FFmpegBinariesRepositoryUri = "https://github.com/GyanD/codexffmpeg";

        internal static void PrepareFFmpegBinaries()
        {
           if(_InitFromCustomPath())
           {
               return;
           }

           _InitFromDownloadedBinaries();
        }

        private static void _InitFromDownloadedBinaries()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new InvalidOperationException(
                    "FFmpeg auto download is available only on Windows platform. You need to specify explicitly FFmpeg binaries path in your config");
            }

            var versionToDownload = CConfig.Config.FFmpeg.VersionToDownload;
            if (string.IsNullOrEmpty(versionToDownload))
            {
                throw new ArgumentException("Version to download is not specified");
            }

            var ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "libs", "unmanaged", "FFmpeg");
            if (Directory.Exists(ffmpegPath))
            {
                ffmpeg.RootPath = ffmpegPath;
                var existingVersion = _TryGetVersion();
                if (!string.IsNullOrEmpty(existingVersion) && existingVersion.StartsWith(versionToDownload, StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }

                Directory.Delete(ffmpegPath, true);
            }

            Directory.CreateDirectory(ffmpegPath);

            var archiveName = $"ffmpeg-{versionToDownload}-full_build-shared.zip";
            var downloadUri = $"{FFmpegBinariesRepositoryUri}/releases/download/{versionToDownload}/{archiveName}";
            CLog.Information($"Will download FFmpeg version {versionToDownload} from {downloadUri}");

            var archivePath = Path.Combine(ffmpegPath, archiveName);
            using var httpClient = new HttpClient();
            File.WriteAllBytes(archivePath, httpClient.GetByteArrayAsync(downloadUri).Result);

            using (var archive = ZipFile.OpenRead(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.Name.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }

                    var dllExtractPath = Path.Combine(ffmpegPath, entry.Name);
                    entry.ExtractToFile(dllExtractPath, overwrite: true);
                    CLog.Information($"Extracted FFmpeg DLL {entry.Name}");
                }
            }

            File.Delete(archivePath);
            ffmpeg.RootPath = ffmpegPath;
            var downloadedVersion = _TryGetVersion();
            if (string.IsNullOrEmpty(downloadedVersion))
            {
                throw new InvalidOleVariantTypeException($"Unable to download FFmpeg {versionToDownload}");
            }

            CLog.Information($"FFmpeg version {downloadedVersion} downloaded");
        }

        private static bool _InitFromCustomPath()
        {
            var ffmpegPath = CConfig.Config.FFmpeg.FFmpegPath;
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                return false;
            }

            ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), ffmpegPath);
            ffmpeg.RootPath = ffmpegPath;
            var version = _TryGetVersion();
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException($"Unable to find the FFmpeg binaries in custom specified folder: {ffmpegPath}");
            }

            CLog.Information($"FFmpeg binaries version {version} found in: {ffmpegPath}");
            return true;
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
