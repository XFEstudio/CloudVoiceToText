using System.Text.Json.Serialization;
using XFEExtension.NetCore.AutoImplement;

namespace CloudVoiceToText.NetCore.Models;

/// <summary>
/// Asr实时转写开始
/// </summary>
[CreateImpl(Modifiers = ["public"])]
public abstract class AsrStartDto
{
    /// <summary>
    /// 状态码，0代表正常，非0值表示发生错误
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }
    /// <summary>
    /// 消息中的所有信息，带格式
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// 音频流唯一 id，由客户端在握手阶段生成并赋值在调用参数中
    /// </summary>
    [JsonPropertyName("voice_id")]
    public string VoiceId { get; set; } = string.Empty;
}
