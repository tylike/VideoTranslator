using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VideoTranslator.SRT.Models
{
    #region Whisper JSON 数据模型

    public class WhisperJsonRoot
    {
        [JsonPropertyName("systeminfo")]
        public string SystemInfo { get; set; }

        [JsonPropertyName("model")]
        public ModelInfo Model { get; set; }

        [JsonPropertyName("params")]
        public ParamsInfo Params { get; set; }

        [JsonPropertyName("result")]
        public ResultInfo Result { get; set; }

        [JsonPropertyName("transcription")]
        public List<TranscriptionSegment> Transcription { get; set; }
    }

    public class ModelInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("multilingual")]
        public bool Multilingual { get; set; }

        [JsonPropertyName("vocab")]
        public int Vocab { get; set; }

        [JsonPropertyName("audio")]
        public AudioInfo Audio { get; set; }

        [JsonPropertyName("text")]
        public TextInfo Text { get; set; }

        [JsonPropertyName("mels")]
        public int Mels { get; set; }

        [JsonPropertyName("ftype")]
        public int Ftype { get; set; }
    }

    public class AudioInfo
    {
        [JsonPropertyName("ctx")]
        public int Ctx { get; set; }

        [JsonPropertyName("state")]
        public int State { get; set; }

        [JsonPropertyName("head")]
        public int Head { get; set; }

        [JsonPropertyName("layer")]
        public int Layer { get; set; }
    }

    public class TextInfo
    {
        [JsonPropertyName("ctx")]
        public int Ctx { get; set; }

        [JsonPropertyName("state")]
        public int State { get; set; }

        [JsonPropertyName("head")]
        public int Head { get; set; }

        [JsonPropertyName("layer")]
        public int Layer { get; set; }
    }

    public class ParamsInfo
    {
        [JsonPropertyName("model")]
        public string ModelPath { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }

        [JsonPropertyName("translate")]
        public bool Translate { get; set; }
    }

    public class ResultInfo
    {
        [JsonPropertyName("language")]
        public string Language { get; set; }
    }

    public class TranscriptionSegment
    {
        [JsonPropertyName("timestamps")]
        public TimestampInfo Timestamps { get; set; }

        [JsonPropertyName("offsets")]
        public OffsetInfo Offsets { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("tokens")]
        public List<TokenInfo> Tokens { get; set; }
    }

    public class TimestampInfo
    {
        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }
    }

    public class OffsetInfo
    {
        [JsonPropertyName("from")]
        public int From { get; set; }

        [JsonPropertyName("to")]
        public int To { get; set; }
    }

    public class TokenInfo
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("timestamps")]
        public TimestampInfo Timestamps { get; set; }

        [JsonPropertyName("offsets")]
        public OffsetInfo Offsets { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("p")]
        public double Probability { get; set; }

        [JsonPropertyName("t_dtw")]
        public int TDtw { get; set; }
    }

    #endregion
}
