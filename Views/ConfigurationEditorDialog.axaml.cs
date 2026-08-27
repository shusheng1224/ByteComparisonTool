using Avalonia.Controls;
using Avalonia.Interactivity;
using ByteComparisonTool.Models;
using ByteComparisonTool.ViewModels;

namespace ByteComparisonTool.Views;

/// <summary>
/// 集中编辑配置名称、阶段、生成规则和校验规则的窗口
/// </summary>
public partial class ConfigurationEditorDialog : Window
{
    /// <summary>
    /// 创建并加载集中配置编辑窗口
    /// </summary>
    public ConfigurationEditorDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 删除按钮所属行对应的字段比较区间
    /// </summary>
    /// <param name="sender">携带比较区间作为 Tag 的删除按钮</param>
    /// <param name="e">按钮点击事件参数</param>
    private void DeleteRange_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ComparisonRange range } && DataContext is MainViewModel viewModel)
        {
            viewModel.DeleteComparisonRangeCommand.Execute(range);
        }
    }

    /// <summary>
    /// 在预期数据编辑框失去焦点时自动整理为每行十六字节
    /// </summary>
    /// <param name="sender">失去焦点的预期数据编辑框</param>
    /// <param name="e">焦点离开事件参数</param>
    private void ExpectedDataEditor_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.FormatExpectedDataCommand.Execute(null);
        }
    }

    /// <summary>
    /// 保存当前配置并在保存成功后关闭编辑窗口
    /// </summary>
    /// <param name="sender">触发保存操作的按钮</param>
    /// <param name="e">按钮点击事件参数</param>
    private async void SaveAndCloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel &&
            await viewModel.SaveCurrentConfigurationAsync())
        {
            Close(true);
        }
    }

    /// <summary>
    /// 放弃本次集中编辑并关闭配置编辑窗口
    /// </summary>
    /// <param name="sender">触发取消操作的按钮</param>
    /// <param name="e">按钮点击事件参数</param>
    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
