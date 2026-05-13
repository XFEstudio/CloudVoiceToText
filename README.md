# CloudVoiceToText

[![NuGet Version](https://img.shields.io/nuget/v/CloudVoiceToText.NetCore?style=flat-square&logo=nuget&label=NuGet)](https://www.nuget.org/packages/CloudVoiceToText.NetCore)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CloudVoiceToText.NetCore?style=flat-square&logo=nuget&label=Downloads)](https://www.nuget.org/packages/CloudVoiceToText.NetCore)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0--windows-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

**[中文文档](README.zh-CN.md)**

A .NET library that connects to [Tencent Cloud ASR](https://cloud.tencent.com/product/asr) (Automatic Speech Recognition) to provide real-time voice-to-text transcription.

## Features

- Real-time streaming speech recognition via WebSocket
- Supports microphone input or system audio loopback (WASAPI)
- 18 engine model types covering Chinese, English, and other languages
- Events for both intermediate (non-stable) and final sentences
- Simple API: create an `AsrService`, subscribe to events, call `StartAsr()`

## Requirements

- .NET 10.0 Windows
- A [Tencent Cloud](https://cloud.tencent.com) account with ASR enabled (APPID, SecretId, SecretKey)

## Installation

Install the NuGet package:

```shell
dotnet add package CloudVoiceToText.NetCore
```

Or via the NuGet Package Manager in Visual Studio:

```
Install-Package CloudVoiceToText.NetCore
```

## Quick Start

```csharp
using CloudVoiceToText.NetCore.Models;

// List available audio input devices
var devices = AsrService.GetVoiceInputDevice();
foreach (var device in devices)
{
    Console.WriteLine(device); // [Index: N]  Device Name: ...
}

// Create the ASR service
// deviceIndex = -1 captures system speaker (loopback); 0, 1, ... for microphones
var asrService = new AsrService(
    appId:           "YOUR_APP_ID",
    secretId:        "YOUR_SECRET_ID",
    secretKey:       "YOUR_SECRET_KEY",
    uuid:            AsrService.GetRandomUuid(),
    maxTime:         600,               // max session length in seconds
    deviceIndex:     0,                 // microphone device index
    engineModelType: EngineModelType.P16k_zh  // Chinese Mandarin model
);

// Subscribe to real-time (possibly changing) results
asrService.SentenceReceived += (_, args) =>
{
    if (args.Success)
        Console.Write($"\r{args.AsrSentence}");
    else
        Console.WriteLine($"[ERROR] {args.Message}");
};

// Subscribe to finalized sentences only
asrService.CompleteSentenceReceived += (_, args) =>
{
    if (args.Success)
        Console.WriteLine($"\n[Final] {args.AsrSentence}");
};

// Start recognition (blocks until StopAsr() is called or connection closes)
await asrService.StartAsr();
```

## API Reference

### `AsrService`

| Member | Description |
|--------|-------------|
| `AsrService(appId, secretId, secretKey, uuid, maxTime, deviceIndex, engineModelType, hotWordId, hotKey)` | Constructor — configure the service |
| `Task StartAsr()` | Start real-time ASR; returns when the session ends |
| `void StopAsr()` | Signal the service to stop recording and close the connection |
| `void Dispose()` | Release all resources |
| `static List<VoiceDevice> GetVoiceInputDevice()` | Return a list of available audio input devices |
| `static string GetRandomUuid()` | Generate a random UUID for the session |
| `event SentenceReceived` | Fires on every intermediate recognition result |
| `event CompleteSentenceReceived` | Fires when a complete sentence is recognized |

### `EngineModelType`

| Value | Language |
|-------|----------|
| `P16k_zh` | Chinese Mandarin (General) |
| `P16k_zh_PY` | Chinese / English / Cantonese mixed |
| `P16k_zh_TW` | Traditional Chinese |
| `P16k_zh_edu` | Chinese Education |
| `P16k_zh_medical` | Chinese Medical |
| `P16k_zh_court` | Chinese Legal / Court |
| `P16k_zh_dialect` | Chinese Multi-dialect |
| `P16k_en` | English (General) |
| `P16k_en_game` | English Gaming |
| `P16k_en_edu` | English Education |
| `P16k_ko` | Korean |
| `P16k_ja` | Japanese |
| `P16k_th` | Thai |
| `P16k_id` | Indonesian |
| `P16k_vi` | Vietnamese |
| `P16k_ms` | Malay |
| `P16k_fil` | Filipino |
| `P16k_ca` | Cantonese |

### `AsrServiceEventArgs`

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the recognition succeeded |
| `Message` | `string` | Raw JSON message from the server |
| `AsrSentence` | `AsrSentence?` | Parsed sentence (available when `Success` is `true`) |

### `AsrSentence`

| Property | Type | Description |
|----------|------|-------------|
| `Text` | `string` | Recognized text |
| `SentenceState` | `SentenceState` | `Start`, `Middle`, or `End` |
| `Response` | `AsrSentenceDto` | Full deserialized response object |

### `VoiceDevice`

| Member | Description |
|--------|-------------|
| `DeviceIndex` | Device index (`-1` = system speaker loopback) |
| `DeviceName` | Human-readable device name |
| `GetIndexAndName()` | Returns a formatted `[Index: N]  Name` string |

## License

This project is licensed under the [MIT License](LICENSE).