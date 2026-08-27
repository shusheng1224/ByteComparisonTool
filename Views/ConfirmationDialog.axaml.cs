using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ByteComparisonTool.Views;

/// <summary>
/// 显示是否进入下一测试阶段的模态确认窗口
/// </summary>
public partial class ConfirmationDialog : Window
{
    /// <summary>
    /// 为 XAML 加载器创建确认窗口
    /// </summary>
    public ConfirmationDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 使用指定标题和消息创建确认窗口
    /// </summary>
    /// <param name="title">窗口标题和内容标题</param>
    /// <param name="message">需要用户确认的消息</param>
    public ConfirmationDialog(string title, string message) : this()
    {
        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
    }

    /// <summary>
    /// 关闭窗口并返回用户已确认
    /// </summary>
    /// <param name="sender">触发事件的确认按钮</param>
    /// <param name="e">点击事件参数</param>
    private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    /// <summary>
    /// 关闭窗口并返回用户已取消
    /// </summary>
    /// <param name="sender">触发事件的取消按钮</param>
    /// <param name="e">点击事件参数</param>
    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
