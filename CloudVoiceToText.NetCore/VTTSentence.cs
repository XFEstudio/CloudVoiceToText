using XFEExtension.NetCore.AutoImplement;
using XFEExtension.NetCore.StringExtension;

namespace CloudVoiceToText.NetCore
{
    /// <summary>
    /// 包含了CVTT输出句子的解析
    /// </summary>
    [CreateImpl]
    public abstract class VttSentence
    {
        internal static bool Initialize { get; set; } = true;
        /// <summary>
        /// 当前句子的状态（一句话是否识别完毕）
        /// </summary>
        public SentenceState NowSentenceState { get; private set; }
        /// <summary>
        /// 该消息是否是空消息
        /// </summary>
        public bool IsEmpty { get; private set; }
        /// <summary>
        /// 状态码，0代表正常，非0值表示发生错误
        /// </summary>
        public int Code { get; private set; }
        /// <summary>
        /// 当前一段话结果在整个音频流中的起始时间
        /// </summary>
        public int StartTime { get; private set; }
        /// <summary>
        /// 当前一段话结果在整个音频流中的结束时间
        /// </summary>
        public int EndTime { get; private set; }
        /// <summary>
        /// 当前一段话结果在整个音频流中的序号，从0条开始逐句递增
        /// </summary>
        public int SentenceIndex { get; private set; }
        /// <summary>
        /// 消息中的所有信息，带格式
        /// </summary>
        public string AllMessage { get; private set; }
        /// <summary>
        /// 当前一段话文本结果，编码为 UTF8
        /// </summary>
        public string Text { get; private set; } = string.Empty;
        /// <summary>
        /// 消息中的所有信息去除双引号后的内容
        /// </summary>
        public string ValidMessage { get; private set; } = string.Empty;
        /// <summary>
        /// 本 message 唯一 id
        /// </summary>
        public string MessageId { get; private set; } = string.Empty;
        /// <summary>
        /// 音频流唯一 id，由客户端在握手阶段生成并赋值在调用参数中
        /// </summary>
        public string VoiceId { get; private set; } = string.Empty;
        /// <summary>
        /// 错误说明，发生错误时显示这个错误发生的具体原因，随着业务发展或体验优化，此文本可能会经常保持变更或更新
        /// </summary>
        public string BackMessage { get; private set; } = string.Empty;

        /// <summary>
        /// 创建VTTSentence消息
        /// </summary>
        /// <param name="allMessage">总消息</param>
        protected VttSentence(string allMessage)
        {
            AllMessage = allMessage;
            AnalyzeSentence();
        }

        private static string RemoveOtherString(string str) => str.Replace("\"", string.Empty);

        private protected void AnalyzeSentence()
        {
            ValidMessage = RemoveOtherString(AllMessage); //获取有效消息
            if (ValidMessage.Count(c => c == '{') == ValidMessage.Count(c => c == '}'))
            {
                IsEmpty = false;
                Text = GetStringBetweenTwoString(AllMessage, "voice_text_str\":\"", "\","); //获取正文消息
                BackMessage = GetStringBetweenTwoString(ValidMessage, "message\":\"", "\","); //获取返回信息
                VoiceId = GetStringBetweenTwoString(ValidMessage, "voice_id:", ","); //获取该次声音的ID
                MessageId = GetStringBetweenTwoString(ValidMessage, "message_id:", ","); //获取该次消息的ID
                //获取起始时间
                if (int.TryParse(GetStringBetweenTwoString(ValidMessage, "start_time:", ","), out var startTime))
                {
                    this.StartTime = startTime;
                }
                else
                {
                    this.StartTime = -1;
                }
                //获取终止时间
                if (int.TryParse(GetStringBetweenTwoString(ValidMessage, "end_time:", ","), out var endTime))
                {
                    this.EndTime = endTime;
                }
                else
                {
                    this.EndTime = -1;
                }
                //获取起始序号
                if (int.TryParse(GetStringBetweenTwoString(ValidMessage, "index:", ","), out var startIndex))
                {
                    SentenceIndex = startIndex;
                }
                else
                {
                    SentenceIndex = -1;
                }
                //获取CodeBlock返回值
                if (int.TryParse(GetStringBetweenTwoString(ValidMessage, "code:", ","), out var intCode))
                {
                    Code = intCode;
                }
                else
                {
                    Code = -1;
                }
                //获取句子状态
                if (!int.TryParse(GetStringBetweenTwoString(ValidMessage, "slice_type:", ","), out var state))
                    return;
                NowSentenceState = state switch
                {
                    0 => SentenceState.Begin,
                    1 => SentenceState.Continue,
                    2 => SentenceState.End,
                    _ => NowSentenceState
                };
            }
            else
            {
                IsEmpty = true;
            }
        }

        internal void AppendSentence(string sentence)
        {
            AllMessage += sentence;
            AnalyzeSentence();
        }

        /// <summary>
        /// 获取所有消息
        /// </summary>
        /// <returns>该消息的所有字符串</returns>
        public override string ToString()
        {
            return AllMessage;
        }

        /// <summary>
        /// 根据给定的开头和末尾返回查找到的第一个匹配的字符串（全匹配）
        /// </summary>
        /// <param name="str">被匹配的字符串</param>
        /// <param name="beginStr">匹配开头字符串</param>
        /// <param name="endStr">匹配结尾字符串</param>
        /// <returns>返回夹在开头和末尾中间的字符串</returns>
        public static string GetStringBetweenTwoString(string str, string beginStr, string endStr)
        {
            if (!str.NullOrWhiteSpace)
                return string.Empty;
            var beginIndex = str.IndexOf(beginStr, StringComparison.Ordinal);
            if (beginIndex is -1 or 0)
            {
                return string.Empty;
            }
            var endIndex = str.IndexOf(endStr, beginIndex, StringComparison.Ordinal);
            return endIndex is -1 or 0 ? string.Empty : str.Substring(beginIndex + beginStr.Length, endIndex - beginIndex - beginStr.Length);
        }

        /// <summary>
        /// 通过给定的文本格式查找对应的字段
        /// </summary>
        /// <param name="form">查找的文本格式</param>
        /// <returns></returns>
        public string GetTextByForm(string form) => GetStringBetweenTwoString(ValidMessage, form + ":", ",");
    }
}
