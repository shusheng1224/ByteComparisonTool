using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using ByteComparisonTool.Infrastructure;
using ByteComparisonTool.Services;

namespace ByteComparisonTool.Views;

/// <summary>
/// 承载阶段导航、数据编辑和校验结果的主窗口
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 进入 Figma 紧凑布局的客户端宽度阈值
    /// </summary>
    private const double CompactWidthBreakpoint = 1240;

    /// <summary>
    /// 进入 Figma 紧凑布局的客户端高度阈值
    /// </summary>
    private const double CompactHeightBreakpoint = 720;

    /// <summary>
    /// 窗口状态持久化服务
    /// </summary>
    private readonly WindowPlacementService _windowPlacementService;

    /// <summary>
    /// 最近一次正常状态下的窗口位置和大小
    /// </summary>
    private WindowPlacement? _normalWindowPlacement;

    /// <summary>
    /// 指示窗口是否已经完成首次状态恢复
    /// </summary>
    private bool _hasRestoredWindowPlacement;

    /// <summary>
    /// 创建并加载板卡数据校验主窗口
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        string placementPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ByteComparisonTool", "window-placement.json");
        _windowPlacementService = new WindowPlacementService(placementPath);
        SizeChanged += MainWindow_OnSizeChanged;
        PositionChanged += MainWindow_OnPositionChanged;
    }

    /// <summary>
    /// 在窗口首次显示时异步加载固定目录中的全部配置
    /// </summary>
    /// <param name="e">窗口打开事件参数</param>
    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        RestoreWindowPlacement();
        UpdateResponsiveLayout();
        if (DataContext is IAsyncInitializable initializable)
        {
            await initializable.InitializeAsync();
        }
    }

    /// <summary>
    /// 根据窗口客户端尺寸切换完整布局与 Figma 紧凑布局
    /// </summary>
    /// <param name="sender">触发尺寸变化的主窗口</param>
    /// <param name="e">包含新客户端尺寸的事件参数</param>
    private void MainWindow_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        CaptureNormalWindowPlacement();
        UpdateResponsiveLayout();
    }

    /// <summary>
    /// 在正常状态下移动窗口时更新待保存的位置
    /// </summary>
    /// <param name="sender">触发位置变化的主窗口</param>
    /// <param name="e">窗口的新屏幕位置</param>
    private void MainWindow_OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        CaptureNormalWindowPlacement();
    }

    /// <summary>
    /// 在窗口关闭前保存正常尺寸、位置和最大化状态
    /// </summary>
    /// <param name="e">窗口关闭事件参数</param>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        CaptureNormalWindowPlacement();
        if (_normalWindowPlacement is not null)
        {
            _normalWindowPlacement.IsMaximized = WindowState == WindowState.Maximized;
            _windowPlacementService.Save(_normalWindowPlacement);
        }

        base.OnClosing(e);
    }

    /// <summary>
    /// 在紧凑窗口内展开或收起差异表
    /// </summary>
    /// <param name="sender">触发切换的按钮</param>
    /// <param name="e">按钮点击事件参数</param>
    private void ToggleCompactDifferencesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SetCompactDifferenceVisibility(!CompactDifferencePanel.IsVisible);
    }

    /// <summary>
    /// 应用当前窗口尺寸对应的响应式布局可见性
    /// </summary>
    private void UpdateResponsiveLayout()
    {
        var useCompactLayout = ClientSize.Width < CompactWidthBreakpoint ||
                               ClientSize.Height < CompactHeightBreakpoint;

        if (!useCompactLayout)
        {
            SetCompactDifferenceVisibility(false);
        }

        CompactLayout.IsVisible = useCompactLayout;
        FullLayout.IsVisible = !useCompactLayout;
    }

    /// <summary>
    /// 更新紧凑差异表及其切换按钮的显示状态
    /// </summary>
    /// <param name="isVisible">是否显示紧凑差异表</param>
    private void SetCompactDifferenceVisibility(bool isVisible)
    {
        CompactDifferencePanel.IsVisible = isVisible;
        CompactDifferenceToggleButton.Content = isVisible ? "收起差异表  ↑" : "查看差异表  →";
    }

    /// <summary>
    /// 从本地设置恢复窗口尺寸、位置和最大化状态
    /// </summary>
    private void RestoreWindowPlacement()
    {
        WindowPlacement? placement = _windowPlacementService.Load();
        if (placement is null || !IsValidPlacement(placement))
        {
            _hasRestoredWindowPlacement = true;
            CaptureNormalWindowPlacement();
            return;
        }

        Screen? targetScreen = FindTargetScreen(placement) ?? Screens.Primary;
        if (targetScreen is not null)
        {
            ApplyPlacementWithinScreen(placement, targetScreen);
        }
        else
        {
            Width = Math.Max(MinWidth, placement.Width);
            Height = Math.Max(MinHeight, placement.Height);
            Position = new PixelPoint(placement.X, placement.Y);
        }

        _normalWindowPlacement = placement;
        _hasRestoredWindowPlacement = true;
        if (placement.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    /// <summary>
    /// 在窗口处于正常状态时记住当前位置和客户区尺寸
    /// </summary>
    private void CaptureNormalWindowPlacement()
    {
        if (!_hasRestoredWindowPlacement || WindowState != WindowState.Normal ||
            ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        _normalWindowPlacement = new WindowPlacement
        {
            X = Position.X,
            Y = Position.Y,
            Width = Width,
            Height = Height
        };
    }

    /// <summary>
    /// 判断持久化记录是否包含可恢复的有限尺寸
    /// </summary>
    /// <param name="placement">待检查的窗口位置记录</param>
    /// <returns>尺寸有效且不小于主窗口最小尺寸时返回 <see langword="true"/></returns>
    private bool IsValidPlacement(WindowPlacement placement)
    {
        return double.IsFinite(placement.Width) && double.IsFinite(placement.Height) &&
               placement.Width >= MinWidth && placement.Height >= MinHeight;
    }

    /// <summary>
    /// 查找仍包含已保存窗口标题栏位置的显示器
    /// </summary>
    /// <param name="placement">已保存的窗口位置记录</param>
    /// <returns>包含窗口标题栏位置的显示器，未找到时返回 <see langword="null"/></returns>
    private Screen? FindTargetScreen(WindowPlacement placement)
    {
        const int visibleTitleBarOffset = 32;
        int probeX = placement.X + visibleTitleBarOffset;
        int probeY = placement.Y + visibleTitleBarOffset;
        return Screens.All.FirstOrDefault(screen =>
        {
            PixelRect area = screen.WorkingArea;
            return probeX >= area.X && probeX < area.Right &&
                   probeY >= area.Y && probeY < area.Bottom;
        });
    }

    /// <summary>
    /// 将保存的窗口状态限制在目标显示器的可用工作区内
    /// </summary>
    /// <param name="placement">需要校正的窗口位置记录</param>
    /// <param name="screen">承载恢复窗口的目标显示器</param>
    private void ApplyPlacementWithinScreen(WindowPlacement placement, Screen screen)
    {
        PixelRect area = screen.WorkingArea;
        double maxWidth = area.Width / screen.Scaling;
        double maxHeight = area.Height / screen.Scaling;
        double width = Math.Clamp(placement.Width, MinWidth, Math.Max(MinWidth, maxWidth));
        double height = Math.Clamp(placement.Height, MinHeight, Math.Max(MinHeight, maxHeight));
        int pixelWidth = (int)Math.Ceiling(width * screen.Scaling);
        int pixelHeight = (int)Math.Ceiling(height * screen.Scaling);
        int x = Math.Clamp(placement.X, area.X, Math.Max(area.X, area.Right - pixelWidth));
        int y = Math.Clamp(placement.Y, area.Y, Math.Max(area.Y, area.Bottom - pixelHeight));

        Width = width;
        Height = height;
        Position = new PixelPoint(x, y);
        placement.X = x;
        placement.Y = y;
        placement.Width = width;
        placement.Height = height;
    }
}
