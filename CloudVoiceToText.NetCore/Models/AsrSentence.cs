using XFEExtension.NetCore.AutoImplement;
using XFEExtension.NetCore.StringExtension;

namespace CloudVoiceToText.NetCore.Models;

/// <summary>
/// 包含了CVTT输出句子的解析
/// </summary>
[CreateImpl]
public abstract class AsrSentence(AsrSentenceDto sentenceDto)
{
    /// <summary>
    /// 识别结果
    /// </summary>
    public string Text { get; set; } = sentenceDto.Result?.VoiceText ?? string.Empty;
    /// <summary>
    /// 识别状态
    /// </summary>
    public SentenceState SentenceState { get; set; } = (SentenceState)(sentenceDto.Result?.SliceType ?? 3);
    /// <summary>
    /// 响应结果
    /// </summary>
    public AsrSentenceDto Response { get; set; } = sentenceDto;

    /// <summary>
    /// 获取识别文本
    /// </summary>
    /// <returns>识别到的文本字符串</returns>
    public override string ToString() => Response.Result?.VoiceText ?? string.Empty;
}
