using System.Text.Json.Serialization;
using XFEExtension.NetCore.AutoImplement;

namespace CloudVoiceToText.NetCore.Models;

/// <summary>
/// 转文本的响应结果
/// </summary>
[CreateImpl]
public abstract class VttSentenceResultDto
{
    /// <summary>
    /// 片段类型
    /// </summary>
    [JsonPropertyName("slice_type")]
    public int SliceType { get; set; }
    /// <summary>
    /// 起始时间
    /// </summary>
    [JsonPropertyName("start_time")]
    public long StartTime { get; set; }
    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("end_time")]
    public long EndTime { get; set; }
    /// <summary>
    /// 音频文本
    /// </summary>
    [JsonPropertyName("voice_text_str")]
    public string VoiceText { get; set; } = string.Empty;
    /// <summary>
    /// 词组大小
    /// </summary>
    [JsonPropertyName("word_size")]
    public int WordSize { get; set; }
    /// <summary>
    /// 词组列表
    /// </summary>
    [JsonPropertyName("word_list")]
    public List<string> WordList { get; set; } = [];
    /// <summary>
    /// 情感类型
    /// </summary>
    [JsonPropertyName("emotion_type")]
    public int? EmotionType { get; set; } = null;
    /// <summary>
    /// 讲话者信息
    /// </summary>
    [JsonPropertyName("speaker_info")]
    public string? SpeakerInfo { get; set; } = null;
}
