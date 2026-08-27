namespace ByteComparisonTool.Models;

/// <summary>
/// 表示一次阶段校验需要持久记录的信息
/// </summary>
public sealed class TestLogEntry
{
    /// <summary>
    /// 获取或设置校验发生时间
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 获取或设置阶段名称
    /// </summary>
    public string StageName { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置已格式化的生成数据
    /// </summary>
    public string GeneratedData { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置生成数据写入板卡的地址
    /// </summary>
    public string GeneratedDataAddress { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置已格式化的预期数据
    /// </summary>
    public string ExpectedData { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置预期返回数据对应的板卡地址
    /// </summary>
    public string ExpectedDataAddress { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置已格式化的实际数据
    /// </summary>
    public string ActualData { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置校验是否通过
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 获取或设置校验结果摘要
    /// </summary>
    public string ResultMessage { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置校验发现的字节差异
    /// </summary>
    public List<ByteDifference> Differences { get; set; } = [];
}
