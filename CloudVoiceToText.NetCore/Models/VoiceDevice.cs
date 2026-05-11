using XFEExtension.NetCore.AutoImplement;

namespace CloudVoiceToText.NetCore.Models;

/// <summary>
/// 音频设备类
/// </summary>
[CreateImpl]
public abstract class VoiceDevice(int deviceIndex, string deviceName)
{
    /// <summary>
    /// 当前设备的索引
    /// </summary>
    public int DeviceIndex { get; private protected set; } = deviceIndex;
    /// <summary>
    /// 当前设备的名称
    /// </summary>
    public string DeviceName { get; private protected set; } = deviceName;
    
    /// <summary>
    /// 获取用于展示的索引+名称描述文本
    /// </summary>
    /// <returns>索引和名称的string</returns>
    public string GetIndexAndName()
    {
        return $"[编号：{DeviceIndex}]\t设备名称：{DeviceName}";
    }
    
    /// <summary>
    /// 获取字符串名称
    /// </summary>
    /// <returns>字符串名称</returns>
    public override string ToString()
    {
        return GetIndexAndName();
    }
}
