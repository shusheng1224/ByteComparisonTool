using ByteComparisonTool.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ByteComparisonTool.ViewModels;

/// <summary>
/// 表示左侧阶段导航中的一个可观察条目
/// </summary>
public partial class StageListItemViewModel : ViewModelBase
{
    /// <summary>
    /// 使用阶段模型和当前顺序创建导航项
    /// </summary>
    /// <param name="model">对应的测试阶段模型</param>
    /// <param name="index">阶段在当前配置中的零基索引</param>
    public StageListItemViewModel(TestStage model, int index)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Index = index;
        _name = model.Name;
    }

    /// <summary>
    /// 获取对应的测试阶段模型
    /// </summary>
    public TestStage Model { get; }

    /// <summary>
    /// 获取阶段在当前配置中的零基索引
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 获取供导航徽标显示的一基序号
    /// </summary>
    public int Number => Index + 1;

    /// <summary>
    /// 获取或设置阶段显示名称
    /// </summary>
    [ObservableProperty]
    private string _name;

    /// <summary>
    /// 获取阶段生成和校验方式摘要
    /// </summary>
    public string Description => $"{GetGenerationDescription(Model.Input.Type)} · " +
        (Model.Validation.Mode == ValidationMode.Full ? "全量比较" : "字段比较");

    /// <summary>
    /// 通知阶段摘要发生变化以刷新阶段表格
    /// </summary>
    public void RefreshDescription()
    {
        OnPropertyChanged(nameof(Description));
    }

    /// <summary>
    /// 获取或设置阶段最近一次校验状态
    /// </summary>
    [ObservableProperty]
    private string _statusText = "待测试";

    /// <summary>
    /// 在阶段显示名称变化后同步可持久化阶段模型
    /// </summary>
    /// <param name="value">用户编辑后的阶段名称</param>
    partial void OnNameChanged(string value)
    {
        Model.Name = value;
    }

    /// <summary>
    /// 将数据生成枚举转换为导航摘要
    /// </summary>
    /// <param name="type">阶段数据生成方式</param>
    /// <returns>用于阶段导航的中文生成方式</returns>
    private static string GetGenerationDescription(GenerationType type)
    {
        return type switch
        {
            GenerationType.Fixed => "固定",
            GenerationType.Incrementing => "递增",
            GenerationType.Random => "随机",
            GenerationType.Repeated => "重复",
            _ => "未知"
        };
    }
}
