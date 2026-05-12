using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using CloudVoiceToText.NetCore.Models;
using NAudio.Wave;

namespace CloudVoiceToText.NetCore;

/// <summary>
/// ASR 实时语音识别
/// </summary>
/// <param name="appId">应用的APPID</param>
/// <param name="secretId">应用的SecretID</param>
/// <param name="secretKey">应用的SecretKey</param>
/// <param name="uuid">Voice的唯一标识，可以是随机数也可以是用户的UID</param>
/// <param name="maxTime">单次转写的最大持续时间</param>
/// <param name="engineModelType">使用的语言模型</param>
/// <param name="deviceIndex">音频输入设备的编号</param>
/// <param name="hotWordId">热词ID</param>
/// <param name="hotKey">是否强制使用热词</param>
/// <returns></returns>
public class AsrService(string appId, string secretId, string secretKey, string uuid, int maxTime, int deviceIndex, EngineModelType engineModelType, string hotWordId = "", bool hotKey = false)
{
    private object? _waveInWay;
    private ClientWebSocket? _clientWebSocket = new();
    /// <summary>
    /// 是否关闭
    /// </summary>
    public bool Closed { get; private set; } = true;
    /// <summary>
    /// 现在返回的消息（非完整消息，消息可能随时变化）
    /// </summary>
    public event EventHandler<AsrServiceEventArgs>? SentenceReceived;
    /// <summary>
    /// 现在返回的完整消息
    /// </summary>
    public event EventHandler<AsrServiceEventArgs>? CompleteSentenceReceived;

    /// <summary>
    /// 获取当前的音频输入设备列表
    /// </summary>
    /// <returns>音频设备列表</returns>
    public static List<VoiceDevice> GetVoiceInputDevice()
    {
        var inputDeviceCount = WaveInEvent.DeviceCount;
        var deviceList = new List<VoiceDevice>
        {
            new VoiceDeviceImpl(-1, "录制系统扬声器声音")
        };
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
        var random = new Random();
        return $"{random.Next(1000, 1000000)}{random.Next(1000, 1000000)}";
    }

    /// <summary>
    /// 关闭远程服务器实时语音识别通讯
    /// </summary>
    public void StopAsr() => Closed = true;

    /// <summary>
    /// 开始实时语音识别
    /// </summary>
    public async Task StartAsr()
    {
        Closed = false;
        if (_clientWebSocket is
            {
                State: WebSocketState.None or WebSocketState.Closed
            })
        {
            _clientWebSocket = new ClientWebSocket();
            await _clientWebSocket.ConnectAsync(new Uri(SummonSignature(GetEngineModelTypeString(), hotKey ? 1 : 0)), CancellationToken.None);
        }
        if (_clientWebSocket is
            {
                State: WebSocketState.Open
            })
        {
            var buffer = new byte[1024];
            var response= await _clientWebSocket.ReceiveAsync(buffer, CancellationToken.None);
            var responseString = Encoding.UTF8.GetString(buffer, 0, response.Count);
            var startResponseObject = JsonSerializer.Deserialize<AsrStartDtoImpl>(responseString);
            var code = startResponseObject?.Code ?? -1;
            if (_clientWebSocket.State == WebSocketState.Open)
            {
                //判断是否录制系统声音
                if (deviceIndex == -1)
                {
                    var capture = new WasapiLoopbackCapture();
                    _waveInWay = capture;
                    capture.WaveFormat = new WaveFormat(16000, 16, 1);
                    // 添加事件处理程序来处理音频数据
                    capture.DataAvailable += async (sender, aArgs) =>
                    {
                        switch (_clientWebSocket.State)
                        {
                            case WebSocketState.Open when Closed:
                                await _clientWebSocket.SendAsync(new ArraySegment<byte>("{\"type\": \"end\"}"u8.ToArray()), WebSocketMessageType.Text, false, CancellationToken.None);
                                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "{\"type\": \"end\"}", CancellationToken.None);
                                _clientWebSocket.Abort();
                                capture.StopRecording();
                                break;
                            case WebSocketState.Open when !Closed:
                                await _clientWebSocket.SendAsync(new ArraySegment<byte>(aArgs.Buffer, 0, aArgs.BytesRecorded), WebSocketMessageType.Binary, true, CancellationToken.None);
                                break;
                            case WebSocketState.None:
                            case WebSocketState.Connecting:
                            case WebSocketState.CloseSent:
                            case WebSocketState.CloseReceived:
                            case WebSocketState.Closed:
                            case WebSocketState.Aborted:
                            default:
                                {
                                    if (!Closed)
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
                    waveIn.DeviceNumber = deviceIndex;
                    // 添加事件处理程序来处理音频数据
                    waveIn.DataAvailable += async (sender, aArgs) =>
                    {
                        switch (_clientWebSocket.State)
                        {
                            case WebSocketState.Open when Closed:
                                await _clientWebSocket.SendAsync(new ArraySegment<byte>("{\"type\": \"end\"}"u8.ToArray()), WebSocketMessageType.Text, false, CancellationToken.None);
                                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "{\"type\": \"end\"}", CancellationToken.None);
                                _clientWebSocket.Abort();
                                waveIn.StopRecording();
                                break;
                            case WebSocketState.Open when !Closed:
                                await _clientWebSocket.SendAsync(new ArraySegment<byte>(aArgs.Buffer, 0, aArgs.BytesRecorded), WebSocketMessageType.Binary, true, CancellationToken.None);
                                break;
                            case WebSocketState.None:
                            case WebSocketState.Connecting:
                            case WebSocketState.CloseSent:
                            case WebSocketState.CloseReceived:
                            case WebSocketState.Closed:
                            case WebSocketState.Aborted:
                            default:
                                {
                                    if (!Closed)
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
                    try
                    {
                        response = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), CancellationToken.None);
                        responseString = Encoding.UTF8.GetString(buffer, 0, response.Count);
                        var responseObject = JsonSerializer.Deserialize<AsrSentenceDtoImpl>(responseString);
                        if (responseObject is not null)
                        {
                            var result = new AsrSentenceImpl(responseObject);
                            SentenceReceived?.Invoke(this, new AsrServiceEventArgs
                            {
                                Success = true,
                                AsrSentence = result,
                                Message = responseString
                            });
                            if (result.SentenceState == SentenceState.End)
                            {
                                CompleteSentenceReceived?.Invoke(this, new AsrServiceEventArgs
                                {
                                    Success = true,
                                    AsrSentence = result,
                                    Message = responseString
                                });
                            }
                        }
                        else
                        {
                            SentenceReceived?.Invoke(this, new AsrServiceEventArgs
                            {
                                Success = false,
                                Message = responseString
                            });
                            CompleteSentenceReceived?.Invoke(this, new AsrServiceEventArgs
                            {
                                Success = false,
                                Message = responseString
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        SentenceReceived?.Invoke(this, new AsrServiceEventArgs
                        {
                            Success = false,
                            Message = ex.Message
                        });
                        CompleteSentenceReceived?.Invoke(this, new AsrServiceEventArgs
                        {
                            Success = false,
                            Message = ex.Message
                        });
                    }
                }
            }
            else
            {
                Console.WriteLine("出现错误！错误代码：" + code);
                Console.WriteLine(responseString);
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
        Closed = true;
        _clientWebSocket?.Dispose();
        switch (_waveInWay?.GetType().Name)
        {
            case "WaveInEvent":
                (_waveInWay as WaveInEvent)?.Dispose();
                break;
            case "WasapiLoopbackCapture":
                (_waveInWay as WasapiLoopbackCapture)?.Dispose();
                break;
        }
    }

    private string SummonSignature(string modelType, int forceHotKey)
    {
        var timeStamp = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        var ultSign = $"asr.cloud.tencent.com/asr/v2/{appId}?engine_model_type={modelType}&expired={Convert.ToInt64(timeStamp.TotalSeconds) + maxTime}&hotword_id={hotWordId}&needvad=1&nonce={Convert.ToInt64(timeStamp.TotalSeconds)}&reinforce_hotword={forceHotKey}&secretid={secretId}&timestamp={Convert.ToInt64(timeStamp.TotalSeconds)}&voice_format=1&voice_id={uuid}";
        using var hmacsha1 = new HMACSHA1();
        hmacsha1.Key = Encoding.UTF8.GetBytes(secretKey);
        return $"wss://asr.cloud.tencent.com/asr/v2/{appId}?engine_model_type={modelType}&expired={Convert.ToInt64(timeStamp.TotalSeconds) + maxTime}&hotword_id={hotWordId}&needvad=1&nonce={Convert.ToInt64(timeStamp.TotalSeconds)}&reinforce_hotword={forceHotKey}&secretid={secretId}&timestamp={Convert.ToInt64(timeStamp.TotalSeconds)}&voice_format=1&voice_id={uuid}&signature={HttpUtility.UrlEncode(Convert.ToBase64String(hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(ultSign))))}";
    }

    private string GetEngineModelTypeString() => engineModelType switch
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
}
