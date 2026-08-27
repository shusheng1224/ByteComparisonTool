using System.Globalization;
using ByteComparisonTool.Models;

namespace ByteComparisonTool.ViewModels;

/// <summary>
/// 表示校验结果区域中的一个字节差异条目
/// </summary>
public sealed class DifferenceItemViewModel
{
    /// <summary>
    /// 使用字节差异模型创建展示条目
    /// </summary>
    /// <param name="difference">包含偏移及预期和实际字节的差异</param>
    public DifferenceItemViewModel(ByteDifference difference)
    {
        OffsetText = $"Offset 0x{difference.Offset.ToString("X4", CultureInfo.InvariantCulture)}";
        ExpectedText = $"期望 {FormatByte(difference.Expected)}";
        ActualText = $"实际 {FormatByte(difference.Actual)}";
    }

    /// <summary>
    /// 获取大写十六进制偏移文本
    /// </summary>
    public string OffsetText { get; }

    /// <summary>
    /// 获取预期字节文本
    /// </summary>
    public string ExpectedText { get; }

    /// <summary>
    /// 获取实际字节文本
    /// </summary>
    public string ActualText { get; }

    /// <summary>
    /// 将可空字节转换为两位大写十六进制文本
    /// </summary>
    /// <param name="value">需要格式化的可空字节</param>
    /// <returns>两位十六进制文本或缺失标记</returns>
    private static string FormatByte(byte? value)
    {
        return value?.ToString("X2", CultureInfo.InvariantCulture) ?? "--";
    }
}
