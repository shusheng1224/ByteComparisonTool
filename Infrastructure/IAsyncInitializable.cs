namespace ByteComparisonTool.Infrastructure;

/// <summary>
/// 表示可在关联视图首次显示时异步加载初始状态的对象
/// </summary>
public interface IAsyncInitializable
{
    /// <summary>
    /// 异步完成对象的初始数据加载
    /// </summary>
    /// <returns>表示初始化过程的任务</returns>
    Task InitializeAsync();
}
