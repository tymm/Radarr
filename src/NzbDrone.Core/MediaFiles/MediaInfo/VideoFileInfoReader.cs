using System;
using System.IO;
using FFMpegCore;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.MediaFiles.MediaInfo
{
    public interface IVideoFileInfoReader
    {
        MediaInfoModel GetMediaInfo(string filename);
        TimeSpan? GetRunTime(string filename);
    }

    public class VideoFileInfoReader : IVideoFileInfoReader
    {
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public const int MINIMUM_MEDIA_INFO_SCHEMA_REVISION = 4;
        public const int CURRENT_MEDIA_INFO_SCHEMA_REVISION = 7;

        public VideoFileInfoReader(IDiskProvider diskProvider, Logger logger)
        {
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public MediaInfoModel GetMediaInfo(string filename)
        {
            if (!_diskProvider.FileExists(filename))
            {
                throw new FileNotFoundException("Media file does not exist: " + filename);
            }

            IMediaAnalysis mediaInfo = null;

            // TODO: Cache media info by path, mtime and length so we don't need to read files multiple times
            try
            {
                _logger.Debug("Getting media info from {0}", filename);
                mediaInfo = FFProbe.Analyse(filename);

                if (mediaInfo != null)
                {
                    int width = mediaInfo.PrimaryVideoStream.Width;
                    int height = mediaInfo.PrimaryVideoStream.Height;
                    int videoBitRate = mediaInfo.PrimaryVideoStream.BitRate;
                    int audioBitRate = mediaInfo.PrimaryAudioStream.BitRate;
                    var audioRuntime = mediaInfo.PrimaryAudioStream.Duration;
                    var videoRuntime = mediaInfo.PrimaryVideoStream.Duration;
                    var generalRuntime = mediaInfo.Format.Duration;
                    int streamCount = mediaInfo.AudioStreams.Count;
                    int audioChannels = mediaInfo.PrimaryAudioStream.Channels;
                    int videoBitDepth = mediaInfo.PrimaryVideoStream.BitsPerRawSample;
                    decimal videoFrameRate = (decimal)mediaInfo.PrimaryVideoStream.FrameRate;
                    int videoMultiViewCount = mediaInfo.VideoStreams.Count;

                    string subtitles = mediaInfo.SubtitleStreams.SelectList(x => x.Language).ConcatToString();
                    string scanType = "Progressive";

                    var audioChannelPositions = mediaInfo.PrimaryAudioStream.ChannelLayout;
                    string audioLanguages = mediaInfo.AudioStreams.SelectList(x => x.Language).ConcatToString();

                    string videoProfile = mediaInfo.PrimaryVideoStream.Profile;
                    string audioProfile = mediaInfo.PrimaryAudioStream.Profile;

                    var mediaInfoModel = new MediaInfoModel
                    {
                        ContainerFormat = mediaInfo.Format.FormatLongName,
                        VideoFormat = mediaInfo.PrimaryVideoStream.CodecName,
                        VideoCodecID = mediaInfo.PrimaryVideoStream.CodecTagString,
                        VideoProfile = videoProfile,
                        VideoCodecLibrary = "",
                        VideoBitrate = videoBitRate,
                        VideoBitDepth = videoBitDepth,
                        VideoMultiViewCount = videoMultiViewCount,
                        VideoColourPrimaries = "todo",
                        VideoTransferCharacteristics = "todo",
                        VideoHdrFormat = "todo",
                        VideoHdrFormatCompatibility = "todo",
                        Height = height,
                        Width = width,
                        AudioFormat = mediaInfo.PrimaryAudioStream.CodecName,
                        AudioCodecID = mediaInfo.PrimaryAudioStream.CodecTagString,
                        AudioProfile = audioProfile,
                        AudioCodecLibrary = "",
                        AudioAdditionalFeatures = "",
                        AudioBitrate = audioBitRate,
                        RunTime = GetBestRuntime(audioRuntime, videoRuntime, generalRuntime),
                        AudioStreamCount = streamCount,
                        AudioChannelsContainer = 0,
                        AudioChannelsStream = audioChannels,
                        AudioChannelPositions = audioChannelPositions,
                        VideoFps = videoFrameRate,
                        AudioLanguages = audioLanguages,
                        Subtitles = subtitles,
                        ScanType = scanType,
                        SchemaRevision = CURRENT_MEDIA_INFO_SCHEMA_REVISION
                    };

                    return mediaInfoModel;
                }
                else
                {
                    _logger.Warn("Unable to open media info from file: " + filename);
                }
            }
            catch (DllNotFoundException ex)
            {
                _logger.Error(ex, "mediainfo is required but was not found");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to parse media info from file: {0}", filename);
            }

            return null;
        }

        public TimeSpan? GetRunTime(string filename)
        {
            var info = GetMediaInfo(filename);

            return info?.RunTime;
        }

        private TimeSpan GetBestRuntime(TimeSpan audio, TimeSpan video, TimeSpan general)
        {
            if (video.TotalMilliseconds == 0)
            {
                if (audio.TotalMilliseconds == 0)
                {
                    return general;
                }

                return audio;
            }

            return video;
        }
    }
}
