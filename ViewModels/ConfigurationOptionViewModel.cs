using ByteComparisonTool.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ByteComparisonTool.ViewModels;

/// <summary>
/// 表示配置下拉列表中的一套内存测试配置
/// </summary>
public partial class ConfigurationOptionViewModel : ViewModelBase
{
    /// <summary>
    /// 使用配置模型、显示名称和可选文件路径创建下拉项
    /// </summary>
    /// <param name="configuration">切换时需要应用的测试配置</param>
    /// <param name="displayName">下拉列表主要显示名称</param>
    /// <param name="sourceText">配置来源说明</param>
    /// <param name="filePath">配置对应的本地 JSON 文件路径，内置配置为空</param>
    public ConfigurationOptionViewModel(TestConfiguration configuration, string displayName,
        string sourceText, string? filePath)
    {
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _displayName = displayName;
        _sourceText = sourceText;
        _filePath = filePath;
    }

    /// <summary>
    /// 获取或设置切换时需要应用的测试配置
    /// </summary>
    public TestConfiguration Configuration { get; set; }

    /// <summary>
    /// 获取或设置下拉列表主要显示名称
    /// </summary>
    [ObservableProperty]
    private string _displayName;

    /// <summary>
    /// 获取配置名称首字符，用于配置编辑器中的识别标记
    /// </summary>
    public string Initial => string.IsNullOrWhiteSpace(DisplayName)
        ? "?"
        : DisplayName.Trim()[..1].ToUpperInvariant();

    /// <summary>
    /// 获取配置包含的阶段数量摘要
    /// </summary>
    public string EditorSummary => $"{Configuration.Stages.Count} 个阶段 · 已启用";

    /// <summary>
    /// 在配置显示名称变化后同步可持久化配置名称
    /// </summary>
    /// <param name="value">用户编辑后的配置名称</param>
    partial void OnDisplayNameChanged(string value)
    {
        Configuration.Name = value;
        OnPropertyChanged(nameof(Initial));
    }

    /// <summary>
    /// 获取或设置配置来源说明
    /// </summary>
    [ObservableProperty]
    private string _sourceText;

    /// <summary>
    /// 获取或设置配置对应的本地 JSON 文件路径
    /// </summary>
    [ObservableProperty]
    private string? _filePath;
}
