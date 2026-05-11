using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using CloudVoiceToText.NetCore.Models;
using NAudio.Wave;

namespace CloudVoiceToText.NetCore;

/// <summary>
/// ASR实时转写类
/// </summary>
public class VoiceToText
{
    private int _maxTime = 600;
    private int _deviceNumber;
    private bool _isFinal = true;
    private bool _forceHotKey;
    private string _appid = string.Empty;
    private string _sId = string.Empty;
    private string _sKey = string.Empty;
    private string _uuid = string.Empty;
    private string _hotWordId = string.Empty;
    private EngineModelType _engineModelType;
    private VttSentence? _currentMessage;
    private ClientWebSocket? _clientWebSocket = new();
    private object? _waveInWay;
    /// <summary>
    /// 现在返回的消息（非稳定消息，消息可能随时变化）
    /// </summary>
    public event EventHandler<VttSentence>? SentenceReceived;
    /// <summary>
    /// 现在返回的稳定消息
    /// </summary>
    public event EventHandler<VttSentence>? StaticSentenceReceived;
    /// <summary>
    /// 初始化实时转写
    /// </summary>
    /// <param name="aPpid">应用的APPID</param>
    /// <param name="sId">应用的SecretID</param>
    /// <param name="sKey">应用的SecretKey</param>
    /// <param name="uUid">Voice的唯一标识，可以是随机数也可以是用户的UID</param>
    /// <param name="maxTime">单次转写的最大持续时间</param>
    /// <param name="engineModelType">使用的语言模型</param>
    /// <param name="deviceNumber">音频输入设备的编号</param>
    /// <param name="hotWordId">热词ID</param>
    /// <param name="forceHotKey">是否强制使用热词</param>
    /// <returns></returns>
    public void InitializeVtt(string aPpid, string sId, string sKey, string uUid, int maxTime, int deviceNumber, EngineModelType engineModelType, string hotWordId = "", bool forceHotKey = false)
    {
        _appid = aPpid;
        _sId = sId;
        _sKey = sKey;
        _uuid = uUid;
        _maxTime = maxTime;
        _engineModelType = engineModelType;
        _deviceNumber = deviceNumber;
        _hotWordId = hotWordId;
        _forceHotKey = forceHotKey;
    }
    /// <summary>
    /// 关闭远程服务器实时转写通讯
    /// </summary>
    public void CloseVtt()
    {
        _isFinal = true;
    }
    /// <summary>
    /// 获取实时转写的状态是否为结束状态
    /// </summary>
    public bool GetEndState()
    {
        return _isFinal;
    }
    /// <summary>
    /// 获取现在时刻的返回的消息
    /// </summary>
    /// <returns>VVTMessage消息类型</returns>
    public VttSentence? CurrentReceivedSentence() => _currentMessage;
    /// <summary>
    /// 获取当前的音频输入设备列表
    /// </summary>
    /// <returns>音频设备列表</returns>
    public static List<VoiceDevice> GetVoiceInputDevice()
    {
        var inputDeviceCount = WaveInEvent.DeviceCount;
        var deviceList = new List<VoiceDevice> { new VoiceDeviceImpl(-1, "录制系统扬声器声音") };
        for (var i = 0; i < inputDeviceCount; i++)
        {
            var capabilities = WaveInEvent.GetCapabilities(i);
            deviceList.Add(new VoiceDeviceImpl(i, capabilities.ProductName));
        }
        return deviceList;
    }
    /// <summary>
    /// 生成随机的UUID
    /// </summary>
    /// <returns>随机UUID</returns>
    public static string GetRandomUuid()
    {
        var r = new Random();
        return $"{r.Next(1000, 1000000)}{r.Next(1000, 1000000)}";
    }
    /// <summary>
    /// 开始实时转写
    /// </summary>
    public async void StartVtt()
    {
        _isFinal = false;
        var modelType = _engineModelType switch
        {
            EngineModelType.P16k_zh => "16k_zh",
            EngineModelType.P16k_zh_PY => "16k_zh-PY",
            EngineModelType.P16k_zh_TW => "16k_zh-TW",
            EngineModelType.P16k_zh_edu => "16k_zh_edu",
            EngineModelType.P16k_zh_medical => "16k_zh_medical",
            EngineModelType.P16k_zh_court => "16k_zh_court",
            EngineModelType.P16k_en => "16k_en",
            EngineModelType.P16k_en_game => "16k_en_game",
            EngineModelType.P16k_en_edu => "16k_en_edu",
            EngineModelType.P16k_ko => "16k_ko",
            EngineModelType.P16k_ja => "16k_ja",
            EngineModelType.P16k_th => "16k_th",
            EngineModelType.P16k_id => "16k_id",
            EngineModelType.P16k_vi => "16k_vi",
            EngineModelType.P16k_ms => "16k_ms",
            EngineModelType.P16k_fil => "16k_fil",
            EngineModelType.P16k_ca => "16k_ca",
            EngineModelType.P16k_zh_dialect => "16k_zh_dialect",
            _ => string.Empty
        };
        var tp = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);//设置当前的UNIX时间戳
        var forceHotKey = 0;
        if (_forceHotKey)
        {
            forceHotKey = 1;
        }
        //拼接未加密的签名串
        var ultSign = $"asr.cloud.tencent.com/asr/v2/{_appid}?engine_model_type={modelType}&expired={Convert.ToInt64(tp.TotalSeconds) + _maxTime}&hotword_id={_hotWordId}&needvad=1&nonce={Convert.ToInt64(tp.TotalSeconds)}&reinforce_hotword={forceHotKey}&secretid={_sId}&timestamp={Convert.ToInt64(tp.TotalSeconds)}&voice_format=1&voice_id={_uuid}";
        #region 生成加密签名
        using var hmacsha1 = new HMACSHA1();
        hmacsha1.Key = Encoding.UTF8.GetBytes(_sKey);
        var sign = Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(ultSign)));
        #endregion
        var signature = HttpUtility.UrlEncode(sign);//签名进行Uri编码
        if (_clientWebSocket is
            {
                State: WebSocketState.None or WebSocketState.Closed
            })
        {
            _clientWebSocket = new ClientWebSocket();
            _clientWebSocket.ConnectAsync(new Uri($"wss://asr.cloud.tencent.com/asr/v2/{_appid}?engine_model_type={modelType}&expired={Convert.ToInt64(tp.TotalSeconds) + _maxTime}&hotword_id={_hotWordId}&needvad=1&nonce={Convert.ToInt64(tp.TotalSeconds)}&reinforce_hotword={forceHotKey}&secretid={_sId}&timestamp={Convert.ToInt64(tp.TotalSeconds)}&voice_format=1&voice_id={_uuid}&signature={signature}"), CancellationToken.None).Wait();
        }
        if (_clientWebSocket is
            {
                State: WebSocketState.Open
            })
        {
            var buffer = new byte[1024];
            var bBytes = new ArraySegment<byte>(buffer);
            _clientWebSocket.ReceiveAsync(bBytes, CancellationToken.None).Wait();
            var backMessage = new VttSentenceImpl(Encoding.UTF8.GetString(bBytes.Array ?? []));
            int backCode = backMessage.Code;
            if ( _clientWebSocket.State == WebSocketState.Open)
            {
                //判断是否录制系统声音
                if (_deviceNumber == -1)
                {
                    var capture = new WasapiLoopbackCapture();
                    _waveInWay = capture;
                    capture.WaveFormat = new WaveFormat(16000, 16, 1);
                    // 添加事件处理程序来处理音频数据
                    capture.DataAvailable += async (sender, aArgs) =>
                    {
                        switch (_clientWebSocket.State)
                        {
                            case WebSocketState.Open when _isFinal:
                                await _clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"type\": \"end\"}")), WebSocketMessageType.Text, false, CancellationToken.None);
                                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "{\"type\": \"end\"}", CancellationToken.None);
                                _clientWebSocket.Abort();
                                capture.StopRecording();
                                break;
                            case WebSocketState.Open when !_isFinal:
                                await _clientWebSocket.SendAsync(new ArraySegment<byte>(aArgs.Buffer, 0, aArgs.BytesRecorded), WebSocketMessageType.Binary, true, CancellationToken.None);
                                break;
                            default:
                                {
                                    if (!_isFinal)
                                    {
                                        capture.StopRecording();
                                        Console.WriteLine("失去Web连接");
                                    }
                                    break;
                                }
                        }
                    };
                    // 开始录制
                    capture.StartRecording();
                }
                else
                {
                    var waveIn = new WaveInEvent();
                    _waveInWay = waveIn;
                    waveIn.WaveFormat = new WaveFormat(16000, 16, 1);
                    waveIn.DeviceNumber = _deviceNumber;
                    // 添加事件处理程序来处理音频数据
                    waveIn.DataAvailable += async (sender, aArgs) =>
                    {
                        switch (_clientWebSocket.State)
                        {
                            case WebSocketState.Open when _isFinal:
                                await _clientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"type\": \"end\"}")), WebSocketMessageType.Text, false, CancellationToken.None);
                                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "{\"type\": \"end\"}", CancellationToken.None);
                                _clientWebSocket.Abort();
                                waveIn.StopRecording();
                                break;
                            case WebSocketState.Open when !_isFinal:
                                await _clientWebSocket.SendAsync(new ArraySegment<byte>(aArgs.Buffer, 0, aArgs.BytesRecorded), WebSocketMessageType.Binary, true, CancellationToken.None);
                                break;
                            default:
                                {
                                    if (!_isFinal)
                                    {
                                        waveIn.StopRecording();
                                        Console.WriteLine("失去Web连接");
                                    }
                                    break;
                                }
                        }
                    };
                    // 开始录制
                    waveIn.StartRecording();
                }
                while (_clientWebSocket.State == WebSocketState.Open)
                {
                    var recBytes = new byte[1024];
                    try
                    {
                        await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(recBytes, 0, recBytes.Length), CancellationToken.None);
                        var message = Encoding.UTF8.GetString(recBytes);
                        if (VttSentence.Initialize)
                        {
                            VttSentence.Initialize = false;
                            _currentMessage = new VttSentenceImpl(message);
                        }
                        if (_currentMessage is
                            {
                                IsEmpty: false
                            })
                        {
                            _currentMessage = new VttSentenceImpl(message);
                        }
                        else
                        {
                            _currentMessage?.AppendSentence(message);
                        }
                        if (_currentMessage is
                            {
                                IsEmpty: false
                            })
                        {
                            SentenceReceived?.Invoke(this, _currentMessage);
                            if (_currentMessage.NowSentenceState == SentenceState.End)
                            {
                                StaticSentenceReceived?.Invoke(this, _currentMessage);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _currentMessage = new VttSentenceImpl(ex.Message);
                        SentenceReceived?.Invoke(this, _currentMessage);
                        StaticSentenceReceived?.Invoke(this, _currentMessage);
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
            Console.WriteLine("握手失败！状态为：" + _clientWebSocket?.State);
        }
    }
    /// <summary>
    /// 释放本实例所使用的资源
    /// </summary>
    public void Dispose()
    {
        _isFinal = true;
        _clientWebSocket?.Dispose();
        if (_waveInWay?.GetType().Name == "WaveInEvent")
        {
            (_waveInWay as WaveInEvent)?.Dispose();
        }
        else if (_waveInWay?.GetType().Name == "WasapiLoopbackCapture")
        {
            (_waveInWay as WasapiLoopbackCapture)?.Dispose();
        }
    }
}
