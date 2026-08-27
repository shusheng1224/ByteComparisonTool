namespace ByteComparisonTool.Models;

/// <summary>
/// 表示一次返回数据校验的完整结果
/// </summary>
public sealed class ValidationResult
{
    /// <summary>
    /// 获取校验是否通过
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 获取预期数据与实际数据的长度是否一致
    /// </summary>
    public bool LengthMatches { get; init; }

    /// <summary>
    /// 获取预期数据字节长度
    /// </summary>
    public int ExpectedLength { get; init; }

    /// <summary>
    /// 获取实际数据字节长度
    /// </summary>
    public int ActualLength { get; init; }

    /// <summary>
    /// 获取按偏移升序排列的字节差异
    /// </summary>
    public IReadOnlyList<ByteDifference> Differences { get; init; } = [];

    /// <summary>
    /// 获取适合界面展示的校验结果摘要
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
