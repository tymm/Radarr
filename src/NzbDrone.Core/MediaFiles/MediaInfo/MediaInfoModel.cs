using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using FFMpegCore;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.MediaFiles.MediaInfo
{
    public class MediaInfoModel : IEmbeddedDocument
    {
        public MediaFormat Format { get; set; }
        public List<VideoStream> VideoStreams { get; set; }
        public List<AudioStream> AudioStreams { get; set; }
        public List<SubtitleStream> SubtitleStreams { get; set; }

        [JsonIgnore]
        public AudioStream PrimaryAudioStream => AudioStreams?.OrderBy(stream => stream.Index).FirstOrDefault();
        [JsonIgnore]
        public VideoStream PrimaryVideoStream => VideoStreams?.OrderBy(stream => stream.Index).FirstOrDefault();
        [JsonIgnore]
        public SubtitleStream PrimarySubtitleStream => SubtitleStreams?.OrderBy(stream => stream.Index).FirstOrDefault();

        public string VideoFormat { get; set; }
        public string VideoCodecID { get; set; }
        public string VideoProfile { get; set; }
        public int VideoBitrate { get; set; }
        public int VideoBitDepth { get; set; }
        public int VideoMultiViewCount { get; set; }
        public string VideoColourPrimaries { get; set; }
        public string VideoTransferCharacteristics { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string AudioFormat { get; set; }
        public string AudioCodecID { get; set; }
        public int AudioBitrate { get; set; }
        public TimeSpan RunTime { get; set; }
        public int AudioStreamCount { get; set; }
        public int AudioChannels { get; set; }
        public string AudioChannelPositions { get; set; }
        public string AudioProfile { get; set; }
        public decimal VideoFps { get; set; }
        public string AudioLanguages { get; set; }
        public string Subtitles { get; set; }
        public string ScanType { get; set; }
        public int SchemaRevision { get; set; }
    }
}
