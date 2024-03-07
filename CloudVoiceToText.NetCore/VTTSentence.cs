using System;
using System.Linq;
using XFEExtension.NetCore.ImplExtension;

namespace CloudVoiceToText.NetCore
{
    /// <summary>
    /// 包含了CVTT输出句子的解析
    /// </summary>
    [CreateImpl]
    public abstract class VTTSentence
    {
        #region 内部私有变量
        private protected SentenceState nowSentenceState;
        private protected bool isEmpty = true;
        private protected int code;
        private protected int startTime;
        private protected int endTime;
        private protected int sentenceIndex;
        private protected string allMessage;
        private protected string text;
        private protected string validMessage;
        private protected string messageId;
        private protected string voiceId;
        private protected string backMessage;
        #endregion
        internal static bool Initialize { get; set; } = true;
        /// <summary>
        /// 当前句子的状态（一句话是否识别完毕）
        /// </summary>
        public SentenceState NowSentenceState { get { return nowSentenceState; } }
        /// <summary>
        /// 该消息是否是空消息
        /// </summary>
        public bool IsEmpty { get { return isEmpty; } }
        /// <summary>
        /// 状态码，0代表正常，非0值表示发生错误
        /// </summary>
        public int Code { get { return code; } }
        /// <summary>
        /// 当前一段话结果在整个音频流中的起始时间
        /// </summary>
        public int StartTime { get { return startTime; } }
        /// <summary>
        /// 当前一段话结果在整个音频流中的结束时间
        /// </summary>
        public int EndTime { get { return endTime; } }
        /// <summary>
        /// 当前一段话结果在整个音频流中的序号，从0开始逐句递增
        /// </summary>
        public int SentenceIndex { get { return sentenceIndex; } }
        /// <summary>
        /// 消息中的所有信息，带格式
        /// </summary>
        public string AllMessage { get { return allMessage; } }
        /// <summary>
        /// 当前一段话文本结果，编码为 UTF8
        /// </summary>
        public string Text { get { return text; } }
        /// <summary>
        /// 消息中的所有信息去除双引号后的内容
        /// </summary>
        public string ValidMessage { get { return validMessage; } }
        /// <summary>
        /// 本 message 唯一 id
        /// </summary>
        public string MessageID { get { return messageId; } }
        /// <summary>
        /// 音频流唯一 id，由客户端在握手阶段生成并赋值在调用参数中
        /// </summary>
        public string VoiceID { get { return voiceId; } }
        /// <summary>
        /// 错误说明，发生错误时显示这个错误发生的具体原因，随着业务发展或体验优化，此文本可能会经常保持变更或更新
        /// </summary>
        public string BackMessage { get { return backMessage; } }
        private static string RemoveOtherStr(string str)
        {
            return str.Replace("\"", string.Empty);
        }
        private protected void AnalyzeSentence()
        {
            validMessage = RemoveOtherStr(AllMessage);//获取有效消息
            if (validMessage.Count(c => c == '{') == validMessage.Count(c => c == '}'))
            {
                isEmpty = false;
                text = GetStringBetweenTwoString(AllMessage, "voice_text_str\":\"", "\",");//获取正文消息
                backMessage = GetStringBetweenTwoString(ValidMessage, "message\":\"", "\",");//获取返回信息
                voiceId = GetStringBetweenTwoString(ValidMessage, "voice_id:", ",");//获取该次声音的ID
                messageId = GetStringBetweenTwoString(ValidMessage, "message_id:", ",");//获取该次消息的ID
                                                                                        //获取起始时间
                if (int.TryParse(GetStringBetweenTwoString(ValidMessage, "start_time:", ","), out int startTime))
                {
                    this.startTime = startTime;
                }
                else
                {
                    this.startTime = -1;
                }
                //获取终止时间
                if (int.TryParse(GetStringBetweenTwoString(ValidMessage, "end_time:", ","), out int endTime))
                {
                    this.endTime = endTime;
                }
                else
                {
                    this.endTime = -1;
                }
                //获取起始序号
                if (int.TryParse(GetStringBetweenTwoString(ValidMessage, "index:", ","), out int startIndex))
                {
                    sentenceIndex = startIndex;
                }
                else
                {
                    sentenceIndex = -1;
                }
                //获取CodeBlock返回值
                if (int.TryParse(GetStringBetweenTwoString(ValidMessage, "code:", ","), out int intCode))
                {
                    code = intCode;
                }
                else
                {
                    code = -1;
                }
                //获取句子状态
                if (int.TryParse(GetStringBetweenTwoString(ValidMessage, "slice_type:", ","), out int state))
                {
                    switch (state)
                    {
                        case 0: nowSentenceState = SentenceState.Begin; break;
                        case 1: nowSentenceState = SentenceState.Continue; break;
                        case 2: nowSentenceState = SentenceState.End; break;
                    }
                }
            }
            else
            {
                isEmpty = true;
            }
        }
        internal void AppendSentence(string sentence)
        {
            allMessage += sentence;
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
            if (str != string.Empty && str != null)
            {
                int beginIndex = str.IndexOf(beginStr, StringComparison.Ordinal);
                if (beginIndex == -1 || beginIndex == 0)
                {
                    return string.Empty;
                }
                int endIndex = str.IndexOf(endStr, beginIndex, StringComparison.Ordinal);
                if (endIndex == -1 || endIndex == 0)
                {
                    return string.Empty;
                }
                return str.Substring(beginIndex + beginStr.Length, endIndex - beginIndex - beginStr.Length);
            }
            else
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// 通过给定的文本格式查找对应的字段
        /// </summary>
        /// <param name="form">查找的文本格式</param>
        /// <returns></returns>
        public string GetTextByForm(string form)
        {
            return GetStringBetweenTwoString(ValidMessage, form + ":", ",");
        }
        /// <summary>
        /// 创建VTTSentence消息
        /// </summary>
        /// <param name="allMessage">总消息</param>
        public VTTSentence(string allMessage)
        {
            this.allMessage = allMessage;
            AnalyzeSentence();
        }
    }
}
