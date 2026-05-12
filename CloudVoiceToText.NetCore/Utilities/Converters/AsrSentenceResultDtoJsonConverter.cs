using CloudVoiceToText.NetCore.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudVoiceToText.NetCore.Utilities.Converters;

/// <summary>
/// 转换器
/// </summary>
public sealed class AsrSentenceResultDtoJsonConverter : JsonConverter<AsrSentenceResultDto?>
{
    /// <inheritdoc/>
    public override AsrSentenceResultDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => JsonSerializer.Deserialize<AsrSentenceResultDtoImpl?>(ref reader, options);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, AsrSentenceResultDto? value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, (AsrSentenceResultDtoImpl?)value, options);
}