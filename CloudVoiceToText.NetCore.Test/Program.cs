using CloudVoiceToText.NetCore.Models;

namespace CloudVoiceToText.NetCore.Test;

public class Program
{
    [SMTest]
    public static async Task TestVTS()
    {
        var devices = VoiceToText.GetVoiceInputDevice();
        foreach (var voiceDevice in devices)
        {
            Console.WriteLine(voiceDevice);
        }
        var index = Console.ReadLine();
        var vtt = new VoiceToText();
        vtt.InitializeVtt("***REMOVED***", "***REMOVED***", "***REMOVED***", VoiceToText.GetRandomUuid(), 600, int.Parse(index ?? "-1"), EngineModelType.P16k_zh);
        vtt.SentenceReceived += (sender, sentence) =>
        {
            Console.WriteLine(sentence);
        };
        vtt.StartVtt();
        await Task.Delay(-1);
    }
}
