# CloudVoiceToText

[![NuGet 版本](https://img.shields.io/nuget/v/CloudVoiceToText.NetCore?style=flat-square&logo=nuget&label=NuGet)](https://www.nuget.org/packages/CloudVoiceToText.NetCore)
[![NuGet 下载量](https://img.shields.io/nuget/dt/CloudVoiceToText.NetCore?style=flat-square&logo=nuget&label=下载量)](https://www.nuget.org/packages/CloudVoiceToText.NetCore)
[![开源协议：MIT](https://img.shields.io/badge/协议-MIT-blue.svg?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0--windows-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/zh-cn/download/dotnet/10.0)

**[English Documentation](README.md)**

一个对接[腾讯云 ASR](https://cloud.tencent.com/product/asr)（语音识别）的 .NET 库，提供实时语音转文字功能。

## 功能特性

- 通过 WebSocket 实现实时流式语音识别
- 支持麦克风输入或系统音频回环录制（WASAPI）
- 支持 18 种引擎模型，涵盖中文、英文及多种语言
- 提供中间结果（非稳定）和最终句子两种事件
- 简洁 API：创建 `AsrService`，订阅事件，调用 `StartAsr()` 即可

## 环境要求

- .NET 10.0 Windows
- [腾讯云](https://cloud.tencent.com) 账号，已开通语音识别服务（需 APPID、SecretId、SecretKey）

## 安装

通过 NuGet 安装：

```shell
dotnet add package CloudVoiceToText.NetCore
```

或通过 Visual Studio NuGet 包管理器：

```
Install-Package CloudVoiceToText.NetCore
```

## 快速开始

```csharp
using CloudVoiceToText.NetCore.Models;

// 列出可用的音频输入设备
var devices = AsrService.GetVoiceInputDevice();
foreach (var device in devices)
{
    Console.WriteLine(device); // [编号：N]  设备名称：...
}

// 创建 ASR 服务
// deviceIndex = -1 表示录制系统扬声器（回环）；0、1、... 表示麦克风
var asrService = new AsrService(
    appId:           "YOUR_APP_ID",
    secretId:        "YOUR_SECRET_ID",
    secretKey:       "YOUR_SECRET_KEY",
    uuid:            AsrService.GetRandomUuid(),
    maxTime:         600,               // 单次会话最大持续时间（秒）
    deviceIndex:     0,                 // 麦克风设备索引
    engineModelType: EngineModelType.P16k_zh  // 中文普通话模型
);

// 订阅实时（可能变化的）识别结果
asrService.SentenceReceived += (_, args) =>
{
    if (args.Success)
        Console.Write($"\r{args.AsrSentence}");
    else
        Console.WriteLine($"[错误] {args.Message}");
};

// 订阅最终稳定句子
asrService.CompleteSentenceReceived += (_, args) =>
{
    if (args.Success)
        Console.WriteLine($"\n[最终结果] {args.AsrSentence}");
};

// 开始识别（阻塞直到调用 StopAsr() 或连接断开）
await asrService.StartAsr();
```

## API 参考

### `AsrService`

| 成员 | 说明 |
|------|------|
| `AsrService(appId, secretId, secretKey, uuid, maxTime, deviceIndex, engineModelType, hotWordId, hotKey)` | 构造函数，配置服务参数 |
| `Task StartAsr()` | 启动实时语音识别；会话结束后返回 |
| `void StopAsr()` | 通知服务停止录制并关闭连接 |
| `void Dispose()` | 释放所有资源 |
| `static List<VoiceDevice> GetVoiceInputDevice()` | 返回可用音频输入设备列表 |
| `static string GetRandomUuid()` | 生成随机 UUID 用于本次会话 |
| `event SentenceReceived` | 每次收到中间识别结果时触发 |
| `event CompleteSentenceReceived` | 识别出完整句子时触发 |

### `EngineModelType`

| 枚举值 | 语言 |
|--------|------|
| `P16k_zh` | 中文普通话（通用） |
| `P16k_zh_PY` | 中英粤混合 |
| `P16k_zh_TW` | 中文繁体 |
| `P16k_zh_edu` | 中文教育 |
| `P16k_zh_medical` | 中文医疗 |
| `P16k_zh_court` | 中文法庭 |
| `P16k_zh_dialect` | 中文多方言 |
| `P16k_en` | 英文（通用） |
| `P16k_en_game` | 英文游戏 |
| `P16k_en_edu` | 英文教育 |
| `P16k_ko` | 韩语 |
| `P16k_ja` | 日语 |
| `P16k_th` | 泰语 |
| `P16k_id` | 印度尼西亚语 |
| `P16k_vi` | 越南语 |
| `P16k_ms` | 马来语 |
| `P16k_fil` | 菲律宾语 |
| `P16k_ca` | 粤语 |

### `AsrServiceEventArgs`

| 属性 | 类型 | 说明 |
|------|------|------|
| `Success` | `bool` | 识别是否成功 |
| `Message` | `string` | 来自服务器的原始 JSON 消息 |
| `AsrSentence` | `AsrSentence?` | 已解析的句子对象（`Success` 为 `true` 时有效） |

### `AsrSentence`

| 属性 | 类型 | 说明 |
|------|------|------|
| `Text` | `string` | 识别到的文本 |
| `SentenceState` | `SentenceState` | `Start`（开始）、`Middle`（中间）或 `End`（结束） |
| `Response` | `AsrSentenceDto` | 完整的反序列化响应对象 |

### `VoiceDevice`

| 成员 | 说明 |
|------|------|
| `DeviceIndex` | 设备索引（`-1` 表示系统扬声器回环） |
| `DeviceName` | 设备名称 |
| `GetIndexAndName()` | 返回格式化的 `[编号：N]  设备名称：...` 字符串 |

## 开源协议

本项目采用 [MIT 协议](LICENSE) 开源。
