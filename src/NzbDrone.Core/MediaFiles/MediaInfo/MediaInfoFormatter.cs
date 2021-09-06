using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NLog.Fluent;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Instrumentation.Extensions;

namespace NzbDrone.Core.MediaFiles.MediaInfo
{
    public static class MediaInfoFormatter
    {
        private const string ValidHdrColourPrimaries = "bt2020";
        private const string VideoDynamicRangeHdr = "HDR";
        private static readonly string[] ValidHdrTransferFunctions = { "PQ", "HLG", "smpte2084" };
        private static readonly string[] DolbyVisionCodecIds = { "dvhe", "dvh1" };

        private static readonly Regex PositionRegex = new Regex(@"(?<position>^\d\.\d)", RegexOptions.Compiled);

        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(MediaInfoFormatter));

        public static decimal FormatAudioChannels(MediaInfoModel mediaInfo)
        {
            var audioChannels = FormatAudioChannelsFromAudioChannelPositions(mediaInfo);

            if (audioChannels == null || audioChannels == 0.0m)
            {
                audioChannels = FormatAudioChannelsFromAudioChannels(mediaInfo);
            }

            return audioChannels ?? 0;
        }

        public static string FormatAudioCodec(MediaInfoModel mediaInfo, string sceneName)
        {
            if (mediaInfo.AudioFormat == null)
            {
                return null;
            }

            var audioFormat = mediaInfo.AudioFormat.Trim().Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
            var audioCodecID = mediaInfo.AudioCodecID ?? string.Empty;
            var audioProfile = mediaInfo.AudioProfile ?? string.Empty;
            var audioCodecLibrary = mediaInfo.AudioCodecLibrary ?? string.Empty;

            if (audioFormat.Empty())
            {
                return string.Empty;
            }

            if (audioCodecID.ContainsIgnoreCase("thd+"))
            {
                return "TrueHD Atmos";
            }

            if (audioFormat.ContainsIgnoreCase("truehd"))
            {
                return "TrueHD";
            }

            if (audioFormat.ContainsIgnoreCase("flac"))
            {
                return "FLAC";
            }

            if (audioFormat.ContainsIgnoreCase("dts"))
            {
                if (audioProfile == "DTS:X")
                {
                    return "DTS-X";
                }

                if (audioProfile == "DTS-HD MA")
                {
                    return "DTS-HD MA";
                }

                if (audioProfile == "DTS-ES")
                {
                    return "DTS-ES";
                }

                if (audioProfile == "DTS-HD HRA")
                {
                    return "DTS-HD HRA";
                }

                if (audioProfile == "DTS Express")
                {
                    return "DTS Express";
                }

                if (audioProfile == "DTS 96/24")
                {
                    return "DTS 96/24";
                }

                return "DTS";
            }

            if (audioCodecID.ContainsIgnoreCase("ec+3"))
            {
                return "EAC3 Atmos";
            }

            if (audioFormat.ContainsIgnoreCase("eac3"))
            {
                return "EAC3";
            }

            if (audioFormat.ContainsIgnoreCase("ac3"))
            {
                return "AC3";
            }

            if (audioFormat.ContainsIgnoreCase("aac"))
            {
                if (audioCodecID == "A_AAC/MPEG4/LC/SBR")
                {
                    return "HE-AAC";
                }

                return "AAC";
            }

            if (audioFormat.ContainsIgnoreCase("mp3"))
            {
                return "MP3";
            }

            if (audioFormat.ContainsIgnoreCase("mp2"))
            {
                return "MP2";
            }

            if (audioFormat.ContainsIgnoreCase("opus"))
            {
                return "Opus";
            }

            if (audioFormat.ContainsIgnoreCase("pcm"))
            {
                return "PCM";
            }

            if (audioFormat.ContainsIgnoreCase("adpcm"))
            {
                return "PCM";
            }

            if (audioFormat.ContainsIgnoreCase("vorbis"))
            {
                return "Vorbis";
            }

            if (audioFormat.ContainsIgnoreCase("wmav2"))
            {
                return "WMA";
            }

            if (audioFormat.ContainsIgnoreCase("A_QUICKTIME"))
            {
                return "";
            }

            Logger.Debug()
                  .Message("Unknown audio format: '{0}' in '{1}'.", string.Join(", ", mediaInfo.AudioFormat, audioCodecID, audioProfile, audioCodecLibrary), sceneName)
                  .WriteSentryWarn("UnknownAudioFormatFFProbe", mediaInfo.ContainerFormat, mediaInfo.AudioFormat, audioCodecID)
                  .Write();

            return mediaInfo.AudioFormat;
        }

        public static string FormatVideoCodec(MediaInfoModel mediaInfo, string sceneName)
        {
            if (mediaInfo.VideoFormat == null)
            {
                return null;
            }

            var videoFormat = mediaInfo.VideoFormat.Trim().Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
            var videoCodecID = mediaInfo.VideoCodecID ?? string.Empty;
            var videoProfile = mediaInfo.VideoProfile ?? string.Empty;
            var videoCodecLibrary = mediaInfo.VideoCodecLibrary ?? string.Empty;

            var result = mediaInfo.VideoFormat.Trim();

            if (videoFormat.Empty())
            {
                return result;
            }

            if (videoFormat.ContainsIgnoreCase("h264"))
            {
                return "x264";
            }

            if (videoFormat.ContainsIgnoreCase("avc") || videoFormat.ContainsIgnoreCase("V.MPEG4/ISO/AVC"))
            {
                if (videoCodecLibrary.StartsWithIgnoreCase("x264"))
                {
                    return "x264";
                }

                return GetSceneNameMatch(sceneName, "AVC", "x264", "h264");
            }

            if (videoFormat.ContainsIgnoreCase("hevc") || videoFormat.ContainsIgnoreCase("V_MPEGH/ISO/HEVC"))
            {
                if (videoCodecLibrary.StartsWithIgnoreCase("x265"))
                {
                    return "x265";
                }

                return GetSceneNameMatch(sceneName, "HEVC", "x265", "h265");
            }

            if (videoFormat.ContainsIgnoreCase("mpegvideo"))
            {
                return "MPEG2";
            }

            if (videoFormat.ContainsIgnoreCase("m4v"))
            {
                if (videoCodecID.ContainsIgnoreCase("XVID") ||
                    videoCodecLibrary.StartsWithIgnoreCase("XviD"))
                {
                    return "XviD";
                }

                if (videoCodecID.ContainsIgnoreCase("DIV3") ||
                    videoCodecID.ContainsIgnoreCase("DIVX") ||
                    videoCodecID.ContainsIgnoreCase("DX50") ||
                    videoCodecLibrary.StartsWithIgnoreCase("DivX"))
                {
                    return "DivX";
                }
            }

            if (videoFormat.ContainsIgnoreCase("m4v"))
            {
                result = GetSceneNameMatch(sceneName, "XviD", "DivX", "");
                if (result.IsNotNullOrWhiteSpace())
                {
                    return result;
                }

                if (videoCodecLibrary.Contains("Lavc"))
                {
                    return ""; // libavcodec mpeg-4
                }

                if (videoCodecLibrary.Contains("em4v"))
                {
                    return ""; // NeroDigital
                }

                if (videoCodecLibrary.Contains("Intel(R) IPP"))
                {
                    return ""; // Intel(R) IPP
                }

                if (videoCodecLibrary.Contains("ZJMedia") ||
                    videoCodecLibrary.Contains("DigiArty"))
                {
                    return ""; // Other
                }

                if (string.IsNullOrEmpty(videoCodecLibrary))
                {
                    return ""; // Unknown mp4v
                }
            }

            if (videoFormat.ContainsIgnoreCase("vc1"))
            {
                return "VC1";
            }

            if (videoFormat.ContainsIgnoreCase("av1"))
            {
                return "AV1";
            }

            if (videoFormat.ContainsIgnoreCase("VP6") || videoFormat.ContainsIgnoreCase("VP7") ||
                videoFormat.ContainsIgnoreCase("VP8") || videoFormat.ContainsIgnoreCase("VP9"))
            {
                return videoFormat.First().ToUpperInvariant();
            }

            if (videoFormat.ContainsIgnoreCase("WMV1") || videoFormat.ContainsIgnoreCase("WMV2"))
            {
                return "WMV";
            }

            if (videoFormat.ContainsIgnoreCase("DivX") || videoFormat.ContainsIgnoreCase("div3"))
            {
                return "DivX";
            }

            if (videoFormat.ContainsIgnoreCase("XviD"))
            {
                return "XviD";
            }

            if (videoFormat.ContainsIgnoreCase("V_QUICKTIME") ||
                videoFormat.ContainsIgnoreCase("RealVideo 4"))
            {
                return "";
            }

            if (videoFormat.ContainsIgnoreCase("mp42") ||
                videoFormat.ContainsIgnoreCase("mp43"))
            {
                // MS old DivX competitor
                return "";
            }

            Logger.Debug()
                  .Message("Unknown video format: '{0}' in '{1}'.", string.Join(", ", mediaInfo.VideoFormat, videoCodecID, videoProfile, videoCodecLibrary), sceneName)
                  .WriteSentryWarn("UnknownVideoFormatFFProbe", mediaInfo.ContainerFormat, mediaInfo.VideoFormat, videoCodecID)
                  .Write();

            return result;
        }

        private static decimal? FormatAudioChannelsFromAudioChannelPositions(MediaInfoModel mediaInfo)
        {
            if (mediaInfo.AudioChannelPositions == null)
            {
                return 0;
            }

            var match = PositionRegex.Match(mediaInfo.AudioChannelPositions);
            if (match.Success)
            {
                return decimal.Parse(match.Groups["position"].Value);
            }

            return 0;
        }

        private static decimal? FormatAudioChannelsFromAudioChannels(MediaInfoModel mediaInfo)
        {
            var audioChannelsStream = mediaInfo.AudioChannelsStream;

            var audioFormat = (mediaInfo.AudioFormat ?? string.Empty).Trim().Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);

            // FLAC 6 channels is likely 5.1
            if (audioFormat.ContainsIgnoreCase("flac") && audioChannelsStream == 6)
            {
                return 5.1m;
            }

            return audioChannelsStream;
        }

        private static string GetSceneNameMatch(string sceneName, params string[] tokens)
        {
            sceneName = sceneName.IsNotNullOrWhiteSpace() ? Parser.Parser.RemoveFileExtension(sceneName) : string.Empty;

            foreach (var token in tokens)
            {
                if (sceneName.ContainsIgnoreCase(token))
                {
                    return token;
                }
            }

            // Last token is the default.
            return tokens.Last();
        }

        public static string FormatVideoDynamicRange(MediaInfoModel mediaInfo)
        {
            if (DolbyVisionCodecIds.ContainsIgnoreCase(mediaInfo.VideoCodecID))
            {
                // Dolby vision
                return VideoDynamicRangeHdr;
            }

            if (mediaInfo.VideoBitDepth >= 10 &&
                mediaInfo.VideoColourPrimaries.IsNotNullOrWhiteSpace() &&
                mediaInfo.VideoTransferCharacteristics.IsNotNullOrWhiteSpace())
            {
                // Other HDR
                if (mediaInfo.VideoColourPrimaries.EqualsIgnoreCase(ValidHdrColourPrimaries) &&
                    ValidHdrTransferFunctions.Any(mediaInfo.VideoTransferCharacteristics.Contains))
                {
                    return VideoDynamicRangeHdr;
                }
            }

            return "";
        }
    }
}
