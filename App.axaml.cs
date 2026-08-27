using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ByteComparisonTool.Infrastructure;
using ByteComparisonTool.Services;
using ByteComparisonTool.ViewModels;
using ByteComparisonTool.Views;

namespace ByteComparisonTool;

/// <summary>
/// 配置并启动板卡数据校验桌面应用
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 需要在应用退出时释放的主视图模型
    /// </summary>
    private MainViewModel? _mainViewModel;

    /// <summary>
    /// 加载应用级 Avalonia XAML 资源
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 创建桌面主窗口并注入测试和平台服务
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            MainWindow mainWindow = new();
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ByteComparisonTool", "Logs", $"validation-{DateTime.Now:yyyyMMdd}.jsonl");
            string configurationDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ByteComparisonTool", "Configurations");
            _mainViewModel = new MainViewModel(
                new AvaloniaDesktopService(mainWindow),
                new DataGenerator(),
                new DataValidator(),
                new TestConfigurationService(),
                configurationDirectory,
                new TestLogger(logPath));
            mainWindow.DataContext = _mainViewModel;
            desktop.MainWindow = mainWindow;
            desktop.Exit += Desktop_OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 在桌面应用退出时释放日志同步资源
    /// </summary>
    /// <param name="sender">触发退出事件的桌面生命周期</param>
    /// <param name="e">应用退出事件参数</param>
    private void Desktop_OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _mainViewModel?.Dispose();
    }
}
