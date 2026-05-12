using System.Text.Json;
using CloudVoiceToText.NetCore.Models;

namespace CloudVoiceToText.NetCore.Test;

public class Program
{
    // [SMTest]
    public static AsrStartDto? TestJsonSerialize() => JsonSerializer.Deserialize<AsrStartDtoImpl>("""
                                                                                                      {
                                                                                                        "code" : 0,
                                                                                                        "message" : "success",
                                                                                                        "voice_id" : "910381956841"
                                                                                                      }
                                                                                                      """);
    [SMTest]
    public static async Task TestAsr()
    {
        var devices = AsrService.GetVoiceInputDevice();
        foreach (var voiceDevice in devices)
        {
            Console.WriteLine(voiceDevice);
        }
        var index = Console.ReadLine();
        var asrService = new AsrService("***REMOVED***", "***REMOVED***", "***REMOVED***", AsrService.GetRandomUuid(), 600, int.Parse(index ?? "-1"), EngineModelType.P16k_zh);
        asrService.SentenceReceived += (_, args) =>
        {
            if (args.Success)
            {
                Console.WriteLine(args.AsrSentence);
            }
            else
            {
                Console.WriteLine($"[ERROR] {args.Message}");
            }
        };
        await asrService.StartAsr();
    }
}
