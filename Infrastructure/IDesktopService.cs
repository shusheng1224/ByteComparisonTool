namespace ByteComparisonTool.Infrastructure;

/// <summary>
/// 抽象桌面文件选择、剪贴板和确认交互能力
/// </summary>
public interface IDesktopService
{
    /// <summary>
    /// 选择需要加载的 JSON 配置文件
    /// </summary>
    /// <returns>用户选择的本地文件路径，取消时为空</returns>
    Task<string?> PickConfigurationToOpenAsync();

    /// <summary>
    /// 选择当前配置的 JSON 保存位置
    /// </summary>
    /// <param name="suggestedFileName">建议的文件名</param>
    /// <returns>用户选择的本地文件路径，取消时为空</returns>
    Task<string?> PickConfigurationToSaveAsync(string suggestedFileName);

    /// <summary>
    /// 将文本复制到系统剪贴板
    /// </summary>
    /// <param name="text">需要复制的文本</param>
    /// <returns>表示复制操作的任务</returns>
    Task CopyTextAsync(string text);

    /// <summary>
    /// 读取系统剪贴板中的文本
    /// </summary>
    /// <returns>剪贴板文本，不可用或没有文本时为空</returns>
    Task<string?> GetClipboardTextAsync();

    /// <summary>
    /// 显示带所有者窗口的确认对话框
    /// </summary>
    /// <param name="title">对话框标题</param>
    /// <param name="message">需要用户确认的消息</param>
    /// <returns>用户明确确认时为 true，否则为 false</returns>
    Task<bool> ConfirmAsync(string title, string message);

    /// <summary>
    /// 显示集中编辑当前配置和全部阶段的窗口
    /// </summary>
    /// <returns>用户保存配置并关闭时为 true，取消编辑时为 false</returns>
    Task<bool> EditConfigurationAsync();
}
