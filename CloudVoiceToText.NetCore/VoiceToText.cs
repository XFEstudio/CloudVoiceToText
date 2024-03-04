using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Web;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace CloudVoiceToText.NetCore
{
    /// <summary>
    /// ASR实时转写类
    /// </summary>
    public partial class VoiceToText
    {
        private int MaxTime = 600;
        private int DeviceNumber = 0;
        private bool IsFinal = true;
        private bool ForceHotKey;
        private string APPID;
        private string sID;
        private string sKey;
        private string UUID;
        private string HotWordId;
        private EngineModelType engineModelType;
        private VTTSentence CurrentMessage;
        private ClientWebSocket clien = new ClientWebSocket();
        private object WaveInWay;
        /// <summary>
        /// 初始化实时转写
        /// </summary>
        /// <param name="aPPID">应用的APPID</param>
        /// <param name="sID">应用的SecretID</param>
        /// <param name="sKey">应用的SecertKey</param>
        /// <param name="uUID">Voice的唯一标识，可以是随机数也可以是用户的UID</param>
        /// <param name="maxTime">单次转写的最大持续时间</param>
        /// <param name="engineModelType">使用的语言模型</param>
        /// <param name="deviceNumber">音频输入设备的编号</param>
        /// <param name="hotWordId">热词ID</param>
        /// <param name="forceHotKey">是否强制使用热词</param>
        /// <returns></returns>
        public void InitializeVTT(string aPPID, string sID, string sKey, string uUID, int maxTime, int deviceNumber, EngineModelType engineModelType, string hotWordId = "", bool forceHotKey = false)
        {
            this.APPID = aPPID;
            this.sID = sID;
            this.sKey = sKey;
            this.UUID = uUID;
            this.MaxTime = maxTime;
            this.engineModelType = engineModelType;
            this.DeviceNumber = deviceNumber;
            this.HotWordId = hotWordId;
            this.ForceHotKey = forceHotKey;
        }
        /// <summary>
        /// 关闭远程服务器实时转写通讯
        /// </summary>
        public void CloseVTT()
        {
            IsFinal = true;
        }
        /// <summary>
        /// 获取实时转写的状态是否为结束状态
        /// </summary>
        public bool GetEndState()
        {
            return IsFinal;
        }
        /// <summary>
        /// 获取现在时刻的返回的消息
        /// </summary>
        /// <returns>VVTMessage消息类型</returns>
        public VTTSentence CurrentReceivedSentence()
        {
            return CurrentMessage;
        }
        /// <summary>
        /// 现在返回的消息（非稳定消息，消息可能随时变化）
        /// </summary>
        public event EventHandler<VTTSentence> SentenceReceived;
        /// <summary>
        /// 现在返回的稳定消息
        /// </summary>
        public event EventHandler<VTTSentence> StaticSentenceReceived;
        /// <summary>
        /// 获取当前的音频输入设备列表
        /// </summary>
        /// <returns>音频设备列表</returns>
        public static List<VoiceDevice> GetVoiceInputDevice()
        {
            var inputDeviceCount = WaveInEvent.DeviceCount;
            var DeviceList = new List<VoiceDevice> { new VoiceDeviceImpl(-1, "录制系统扬声器声音") };
            for (int i = 0; i < inputDeviceCount; i++)
            {
                var capabilities = WaveInEvent.GetCapabilities(i);
                DeviceList.Add(new VoiceDeviceImpl(i, capabilities.ProductName));
            }
            return DeviceList;
        }
        /// <summary>
        /// 生成随机的UUID
        /// </summary>
        /// <returns>随机UUID</returns>
        public static string GetRandomUUID()
        {
            Random r = new Random();
            return $"{r.Next(1000, 1000000)}{r.Next(1000, 1000000)}";
        }
        /// <summary>
        /// 开始实时转写
        /// </summary>
        public async void StartCVTT()
        {
            IsFinal = false;
            string ModelType = string.Empty;
            switch (engineModelType)
            {
                case EngineModelType.P16k_zh:
                    ModelType = "16k_zh";
                    break;
                case EngineModelType.P16k_zh_PY:
                    ModelType = "16k_zh-PY";
                    break;
                case EngineModelType.P16k_zh_TW:
                    ModelType = "16k_zh-TW";
                    break;
                case EngineModelType.P16k_zh_edu:
                    ModelType = "16k_zh_edu";
                    break;
                case EngineModelType.P16k_zh_medical:
                    ModelType = "16k_zh_medical";
                    break;
                case EngineModelType.P16k_zh_court:
                    ModelType = "16k_zh_court";
                    break;
                case EngineModelType.P16k_en:
                    ModelType = "16k_en";
                    break;
                case EngineModelType.P16k_en_game:
                    ModelType = "16k_en_game";
                    break;
                case EngineModelType.P16k_en_edu:
                    ModelType = "16k_en_edu";
                    break;
                case EngineModelType.P16k_ko:
                    ModelType = "16k_ko";
                    break;
                case EngineModelType.P16k_ja:
                    ModelType = "16k_ja";
                    break;
                case EngineModelType.P16k_th:
                    ModelType = "16k_th";
                    break;
                case EngineModelType.P16k_id:
                    ModelType = "16k_id";
                    break;
                case EngineModelType.P16k_vi:
                    ModelType = "16k_vi";
                    break;
                case EngineModelType.P16k_ms:
                    ModelType = "16k_ms";
                    break;
                case EngineModelType.P16k_fil:
                    ModelType = "16k_fil";
                    break;
                case EngineModelType.P16k_ca:
                    ModelType = "16k_ca";
                    break;
                case EngineModelType.P16k_zh_dialect:
                    ModelType = "16k_zh_dialect";
                    break;
            }
            TimeSpan tp = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);//设置当前的UNIX时间戳
            int forceHotKey = 0;
            if (ForceHotKey)
            {
                forceHotKey = 1;
            }
            //拼接未加密的签名串
            string UltSign = $"asr.cloud.tencent.com/asr/v2/{APPID}?engine_model_type={ModelType}&expired={Convert.ToInt64(tp.TotalSeconds) + MaxTime}&hotword_id={HotWordId}&needvad=1&nonce={Convert.ToInt64(tp.TotalSeconds)}&reinforce_hotword={forceHotKey}&secretid={sID}&timestamp={Convert.ToInt64(tp.TotalSeconds)}&voice_format=1&voice_id={UUID}";
            #region 生成加密签名
            HMACSHA1 hMACSHA1 = new HMACSHA1();
            hMACSHA1.Key = Encoding.UTF8.GetBytes(sKey);
            string Sign = Convert.ToBase64String(hMACSHA1.ComputeHash(Encoding.UTF8.GetBytes(UltSign)));
            #endregion
            string signature = HttpUtility.UrlEncode(Sign);//签名进行Uri编码
            if (clien.State == WebSocketState.None || clien.State == WebSocketState.Closed)
            {
                clien = new ClientWebSocket();
                clien.ConnectAsync(new Uri($"wss://asr.cloud.tencent.com/asr/v2/{APPID}?engine_model_type={ModelType}&expired={Convert.ToInt64(tp.TotalSeconds) + MaxTime}&hotword_id={HotWordId}&needvad=1&nonce={Convert.ToInt64(tp.TotalSeconds)}&reinforce_hotword={forceHotKey}&secretid={sID}&timestamp={Convert.ToInt64(tp.TotalSeconds)}&voice_format=1&voice_id={UUID}&signature={signature}"), CancellationToken.None).Wait();
            }
            if (clien.State == WebSocketState.Open)
            {
                byte[] buffer = new byte[1024];
                ArraySegment<byte> bBytes = new ArraySegment<byte>(buffer);
                clien.ReceiveAsync(bBytes, CancellationToken.None).Wait();
                var backMessage = new VTTSentenceImpl(Encoding.UTF8.GetString(bBytes.Array));
                string validMessage = backMessage.ValidMessage;
                int backCode = backMessage.Code;
                if (backCode == 0 && clien.State == WebSocketState.Open)
                {
                    //判断是否录制系统声音
                    if (DeviceNumber == -1)
                    {
                        WasapiLoopbackCapture capture = new WasapiLoopbackCapture();
                        WaveInWay = capture;
                        capture.WaveFormat = new WaveFormat(16000, 16, 1);
                        // 添加事件处理程序来处理音频数据
                        capture.DataAvailable += async (sender, aArgs) =>
                        {
                            if (clien.State == WebSocketState.Open && IsFinal)
                            {
                                await clien.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"type\": \"end\"}")), WebSocketMessageType.Text, false, CancellationToken.None);
                                await clien.CloseAsync(WebSocketCloseStatus.NormalClosure, "{\"type\": \"end\"}", CancellationToken.None);
                                clien.Abort();
                                capture.StopRecording();
                            }
                            else if (clien.State == WebSocketState.Open && !IsFinal)
                            {
                                await clien.SendAsync(new ArraySegment<byte>(aArgs.Buffer, 0, aArgs.BytesRecorded), WebSocketMessageType.Binary, true, CancellationToken.None);
                            }
                            else if (!IsFinal)
                            {
                                capture.StopRecording();
                                Console.WriteLine("失去Web连接");
                            }
                        };
                        // 开始录制
                        capture.StartRecording();
                    }
                    else
                    {
                        WaveInEvent waveIn = new WaveInEvent();
                        WaveInWay = waveIn;
                        waveIn.WaveFormat = new WaveFormat(16000, 16, 1);
                        waveIn.DeviceNumber = DeviceNumber;
                        // 添加事件处理程序来处理音频数据
                        waveIn.DataAvailable += async (sender, aArgs) =>
                        {
                            if (clien.State == WebSocketState.Open && IsFinal)
                            {
                                await clien.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"type\": \"end\"}")), WebSocketMessageType.Text, false, CancellationToken.None);
                                await clien.CloseAsync(WebSocketCloseStatus.NormalClosure, "{\"type\": \"end\"}", CancellationToken.None);
                                clien.Abort();
                                waveIn.StopRecording();
                            }
                            else if (clien.State == WebSocketState.Open && !IsFinal)
                            {
                                await clien.SendAsync(new ArraySegment<byte>(aArgs.Buffer, 0, aArgs.BytesRecorded), WebSocketMessageType.Binary, true, CancellationToken.None);
                            }
                            else if (!IsFinal)
                            {
                                waveIn.StopRecording();
                                Console.WriteLine("失去Web连接");
                            }
                        };
                        // 开始录制
                        waveIn.StartRecording();
                    }
                    while (clien.State == WebSocketState.Open)
                    {
                        byte[] recBytes = new byte[1024];
                        try
                        {
                            await clien.ReceiveAsync(new ArraySegment<byte>(recBytes, 0, recBytes.Length), CancellationToken.None);
                            string message = Encoding.UTF8.GetString(recBytes);
                            if (VTTSentence.Initialize)
                            {
                                VTTSentence.Initialize = false;
                                CurrentMessage = new VTTSentenceImpl(message);
                            }
                            if (!CurrentMessage.IsEmpty)
                            {
                                CurrentMessage = new VTTSentenceImpl(message);
                            }
                            else
                            {
                                CurrentMessage.AppendSentence(message);
                            }
                            if (!CurrentMessage.IsEmpty)
                            {
                                SentenceReceived?.Invoke(this, CurrentMessage);
                                if (CurrentMessage.NowSentenceState == SentenceState.End)
                                {
                                    StaticSentenceReceived?.Invoke(this, CurrentMessage);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            CurrentMessage = new VTTSentenceImpl(ex.Message);
                            SentenceReceived?.Invoke(this, CurrentMessage);
                            StaticSentenceReceived?.Invoke(this, CurrentMessage);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("出现错误！错误代码：" + backCode);
                    Console.WriteLine(backMessage.BackMessage);
                }
            }
            else
            {
                Console.WriteLine("握手失败！状态为：" + clien.State);
            }
        }
        /// <summary>
        /// 释放本实例所使用的资源
        /// </summary>
        public void Dispose()
        {
            IsFinal = true;
            clien.Dispose();
            if (WaveInWay.GetType().Name == "WaveInEvent")
            {
                (WaveInWay as WaveInEvent).Dispose();
            }
            else if (WaveInWay.GetType().Name == "WasapiLoopbackCapture")
            {
                (WaveInWay as WasapiLoopbackCapture).Dispose();
            }
        }
    }
}
