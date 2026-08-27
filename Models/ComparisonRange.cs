namespace ByteComparisonTool.Models;

/// <summary>
/// 描述需要比较的连续字节区间
/// </summary>
public sealed class ComparisonRange
{
    /// <summary>
    /// 获取或设置便于展示的字段名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置从零开始的字节偏移
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// 获取或设置需要比较的字节数量
    /// </summary>
    public int Length { get; set; }
}
