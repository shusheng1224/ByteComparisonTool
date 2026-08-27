namespace ByteComparisonTool.Models;

/// <summary>
/// 表示当前校验结果所处的语义状态
/// </summary>
public enum ValidationState
{
    /// <summary>
    /// 尚未执行校验
    /// </summary>
    Idle,

    /// <summary>
    /// 最近一次校验通过
    /// </summary>
    Success,

    /// <summary>
    /// 最近一次校验失败或输入无效
    /// </summary>
    Failure
}
