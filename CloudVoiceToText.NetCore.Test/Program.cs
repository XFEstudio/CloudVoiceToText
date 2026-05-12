using CloudVoiceToText.NetCore.Models;

namespace CloudVoiceToText.NetCore.Test;

public class Program
{
    [SMTest]
    public static async Task TestAsr()
    {
        var devices = AsrService.GetVoiceInputDevice();
        foreach (var voiceDevice in devices)
        {
            Console.WriteLine(voiceDevice);
        }
        var index = Console.ReadLine();
        var vtt = new AsrService("***REMOVED***", "***REMOVED***", "***REMOVED***", AsrService.GetRandomUuid(), 600, int.Parse(index ?? "-1"), EngineModelType.P16k_zh);
        vtt.SentenceReceived += (sender, sentence) =>
        {
            Console.WriteLine(sentence);
        };
        await vtt.StartAsr();
    }
}
