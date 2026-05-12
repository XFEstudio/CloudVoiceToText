namespace CloudVoiceToText.NetCore.Models;

/// <summary>
/// ASR实时语音识别事件参数
/// </summary>
public class AsrServiceEventArgs : EventArgs
{
    /// <summary>
    /// 是否识别成功
    /// </summary>
    public bool Success { get; set; }
    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// ASR实时语音识别结果
    /// </summary>
    public AsrSentence? AsrSentence { get; set; }
}
