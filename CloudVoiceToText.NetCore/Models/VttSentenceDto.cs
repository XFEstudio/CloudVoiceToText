using System.Text.Json.Serialization;
using XFEExtension.NetCore.AutoImplement;

namespace CloudVoiceToText.NetCore.Models;

/// <summary>
/// 转文本的响应
/// </summary>
[CreateImpl]
public abstract class VttSentenceDto
{
    /// <summary>
    /// 状态码，0代表正常，非0值表示发生错误
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; private set; }
    /// <summary>
    /// 消息中的所有信息，带格式
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; private set; } = string.Empty;
    /// <summary>
    /// 本 message 唯一 id
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; private set; } = string.Empty;
    /// <summary>
    /// 音频流唯一 id，由客户端在握手阶段生成并赋值在调用参数中
    /// </summary>
    [JsonPropertyName("voice_id")]
    public string VoiceId { get; private set; } = string.Empty;
    /// <summary>
    /// 识别结果
    /// </summary>
    [JsonPropertyName("result")]
    public VttSentenceResultDto? Result { get; set; } = null;
}
