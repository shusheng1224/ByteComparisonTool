namespace ByteComparisonTool.Models;

/// <summary>
/// 描述预期返回数据与实际返回数据的校验规则
/// </summary>
public sealed class ValidationRule
{
    /// <summary>
    /// 获取或设置内容比较方式
    /// </summary>
    public ValidationMode Mode { get; set; } = ValidationMode.Full;

    /// <summary>
    /// 获取或设置长度不一致时是否判定校验失败
    /// </summary>
    public bool RequireLengthMatch { get; set; } = true;

    /// <summary>
    /// 获取或设置区间比较方式使用的字节区间
    /// </summary>
    public List<ComparisonRange> Ranges { get; set; } = [];
}
