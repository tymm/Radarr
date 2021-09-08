using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using FFMpegCore;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.MediaFiles.MediaInfo
{
    public class MediaInfoModel : IEmbeddedDocument
    {
        private IMediaAnalysis _analysis;

        public MediaInfoModel()
        {
        }

        private class Stream
        {
            public int Index { get; set; }
            public string Codec_type { get; set;  }
            public string Codec_name { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public int VideoMultiViewCount { get; set; }
            public string Bits_per_raw_sample { get; set; }
            public string Color_primaries { get; set; }
            public string Color_transfer { get; set; }
            public int Channels { get; set; }
            public string Channel_layout { get; set; }
            public Dictionary<string, string> Tags { get; set; }
        }

        public MediaInfoModel(IEnumerable<string> audioLanguages = null,
            IEnumerable<string> subtitleLanguages = null,
            string videoFormat = "avc",
            int width = 960,
            int height = 720,
            int bitDepth = 8,
            int videoMultiViewCount = 1,
            string colorPrimaries = "bt709",
            string colorTransfer = "bt709",
            string audioFormat = "dts",
            int audioChannels = 6,
            string audioChannelPositions = "5.1")
        {
            audioLanguages ??= new[] { "eng" };
            subtitleLanguages ??= new[] { "eng" };

            var streams = new List<Stream>
            {
                new Stream
                {
                    Index = 0,
                    Codec_type = "video",
                    Codec_name = videoFormat,
                    Width = width,
                    Height = height,
                    VideoMultiViewCount = videoMultiViewCount,
                    Bits_per_raw_sample = bitDepth.ToString(),
                    Color_primaries = colorPrimaries,
                    Color_transfer = colorTransfer
                }
            };

            var i = 1;

            foreach (var lang in audioLanguages)
            {
                streams.Add(new Stream
                {
                    Index = i++,
                    Codec_type = "audio",
                    Codec_name = audioFormat,
                    Channels = audioChannels,
                    Channel_layout = audioChannelPositions,
                    Tags = new Dictionary<string, string>
                    {
                        { "language", lang }
                    }
                });
            }

            foreach (var lang in subtitleLanguages)
            {
                streams.Add(new Stream()
                {
                    Index = i++,
                    Codec_type = "subtitle",
                    Codec_name = "hdmv_pgs_subtitle",
                    Tags = new Dictionary<string, string>
                    {
                        { "language", lang }
                    }
                });
            }

            RawData = new
            {
                Streams = streams,
                Format = new
                {
                    Nb_streams = i
                }
            }.ToJson();
        }

        public string RawData { get; set; }
        public int SchemaRevision { get; set; }

        [JsonIgnore]
        public IMediaAnalysis Analysis
        {
            get
            {
                _analysis = FFProbe.Analyse(RawData);
                return _analysis;
            }
        }

        [JsonIgnore]
        public string VideoFormat => Analysis.PrimaryVideoStream?.CodecName;
        [JsonIgnore]
        public string VideoCodecID => Analysis.PrimaryVideoStream?.CodecTagString;
        [JsonIgnore]
        public string VideoProfile => Analysis.PrimaryVideoStream?.Profile;
        [JsonIgnore]
        public int VideoBitrate => Analysis.PrimaryVideoStream?.BitRate ?? 0;
        [JsonIgnore]
        public int VideoBitDepth => Analysis.PrimaryVideoStream?.BitsPerRawSample ?? 0;
        [JsonIgnore]
        public int VideoMultiViewCount => 1;
        [JsonIgnore]
        public string VideoColourPrimaries => Analysis.PrimaryVideoStream?.ColorPrimaries;
        [JsonIgnore]
        public string VideoTransferCharacteristics => Analysis.PrimaryVideoStream?.ColorTransfer;
        [JsonIgnore]
        public int Height => Analysis.PrimaryVideoStream?.Height ?? 0;
        [JsonIgnore]
        public int Width => Analysis.PrimaryVideoStream?.Width ?? 0;
        [JsonIgnore]
        public string AudioFormat => Analysis.PrimaryAudioStream?.CodecName;
        [JsonIgnore]
        public string AudioCodecID => Analysis.PrimaryAudioStream?.CodecTagString;
        [JsonIgnore]
        public string AudioProfile => Analysis.PrimaryAudioStream?.Profile;
        [JsonIgnore]
        public int AudioBitrate => Analysis.PrimaryAudioStream?.BitRate ?? 0;
        [JsonIgnore]
        public TimeSpan RunTime => GetBestRuntime(Analysis.PrimaryAudioStream?.Duration, Analysis.PrimaryVideoStream.Duration, Analysis.Format.Duration);
        [JsonIgnore]
        public int AudioStreamCount => Analysis.AudioStreams.Count;
        [JsonIgnore]
        public int AudioChannels => Analysis.PrimaryAudioStream?.Channels ?? 0;
        [JsonIgnore]
        public string AudioChannelPositions => Analysis.PrimaryAudioStream?.ChannelLayout;
        [JsonIgnore]
        public decimal VideoFps => Analysis.PrimaryVideoStream?.FrameRate ?? 0;
        [JsonIgnore]
        public string AudioLanguages => Analysis.AudioStreams?.Select(x => x.Language).Where(l => l.IsNotNullOrWhiteSpace()).ConcatToString("/") ?? string.Empty;
        [JsonIgnore]
        public string Subtitles => Analysis.SubtitleStreams?.Select(x => x.Language).Where(l => l.IsNotNullOrWhiteSpace()).ConcatToString("/") ?? string.Empty;
        [JsonIgnore]
        public string ScanType => "Progressive";

        private static TimeSpan GetBestRuntime(TimeSpan? audio, TimeSpan? video, TimeSpan general)
        {
            if (!video.HasValue || video.Value.TotalMilliseconds == 0)
            {
                if (!audio.HasValue || audio.Value.TotalMilliseconds == 0)
                {
                    return general;
                }

                return audio.Value;
            }

            return video.Value;
        }
    }
}
