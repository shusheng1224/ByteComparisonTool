namespace ByteComparisonTool.Models;

/// <summary>
/// 表示可加载和保存的一套板卡测试配置
/// </summary>
public sealed class TestConfiguration
{
    /// <summary>
    /// 获取或设置配置显示名称
    /// </summary>
    public string Name { get; set; } = "Board Validation Tests";

    /// <summary>
    /// 获取或设置配置格式版本
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// 获取或设置按执行顺序排列的测试阶段
    /// </summary>
    public List<TestStage> Stages { get; set; } = [];
}
