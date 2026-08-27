namespace ByteComparisonTool.Models;

/// <summary>
/// 测试输入数据的生成方式
/// </summary>
public enum GenerationType
{
    /// <summary>
    /// 使用配置中保存的固定十六进制数据
    /// </summary>
    Fixed,

    /// <summary>
    /// 从起始字节开始逐字节递增并在 255 后回绕
    /// </summary>
    Incrementing,

    /// <summary>
    /// 生成指定长度的随机字节
    /// </summary>
    Random,

    /// <summary>
    /// 重复指定字节模式并按总字节上限截断
    /// </summary>
    Repeated
}
