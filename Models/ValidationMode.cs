namespace ByteComparisonTool.Models;

/// <summary>
/// 预期数据与实际数据的内容比较方式
/// </summary>
public enum ValidationMode
{
    /// <summary>
    /// 比较全部字节
    /// </summary>
    Full,

    /// <summary>
    /// 仅比较配置的字节区间
    /// </summary>
    Ranges
}
