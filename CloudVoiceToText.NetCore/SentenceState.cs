namespace CloudVoiceToText.NetCore
{
    /// <summary>
    /// 句子被识别的状态
    /// </summary>
    public enum SentenceState
    {
        /// <summary>
        /// 一段话开始识别
        /// </summary>
        Begin = 0,
        /// <summary>
        /// 一段话识别中，句子为非稳态结果(该段识别结果还可能变化)
        /// </summary>
        Continue = 1,
        /// <summary>
        /// 一段话识别结束，句子为稳态结果(该段识别结果不再变化)
        /// </summary>
        End = 2
    }
}
