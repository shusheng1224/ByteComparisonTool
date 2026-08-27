namespace ByteComparisonTool.Models;

/// <summary>
/// 表示测试流程中的一个独立阶段
/// </summary>
public sealed class TestStage
{
    /// <summary>
    /// 获取或设置阶段的稳定标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 获取或设置阶段显示名称
    /// </summary>
    public string Name { get; set; } = "New Stage";

    /// <summary>
    /// 获取或设置待发送数据的生成规则
    /// </summary>
    public DataGenerationRule Input { get; set; } = new();

    /// <summary>
    /// 获取或设置生成数据写入板卡地址的十六进制文本
    /// </summary>
    public string GeneratedDataAddress { get; set; } = "00 00 00 00";

    /// <summary>
    /// 获取或设置板卡预期返回数据的十六进制文本
    /// </summary>
    public string ExpectedData { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置预期返回数据对应板卡地址的十六进制文本
    /// </summary>
    public string ExpectedDataAddress { get; set; } = "00 00 00 00";

    /// <summary>
    /// 获取或设置返回数据校验规则
    /// </summary>
    public ValidationRule Validation { get; set; } = new();
}
