using System.Text.Json;

namespace ByteComparisonTool.Services;

/// <summary>
/// 从本地设置文件读取和保存主窗口位置
/// </summary>
internal sealed class WindowPlacementService
{
    /// <summary>
    /// 窗口状态文件的完整路径
    /// </summary>
    private readonly string _filePath;

    /// <summary>
    /// 创建使用指定状态文件的窗口位置服务
    /// </summary>
    /// <param name="filePath">窗口状态文件的完整路径</param>
    public WindowPlacementService(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// 读取上次成功保存的窗口位置
    /// </summary>
    /// <returns>有效的窗口位置记录，文件缺失或损坏时返回 <see langword="null"/></returns>
    public WindowPlacement? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<WindowPlacement>(json);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// 将当前窗口位置写入本地设置文件
    /// </summary>
    /// <param name="placement">需要持久化的窗口位置记录</param>
    public void Save(WindowPlacement placement)
    {
        try
        {
            string? directoryPath = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string json = JsonSerializer.Serialize(placement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // 窗口关闭不能因非关键的界面状态保存失败而中断
        }
    }
}

/// <summary>
/// 表示可在下次启动时恢复的主窗口状态
/// </summary>
internal sealed class WindowPlacement
{
    /// <summary>
    /// 窗口左上角的屏幕横坐标（物理像素）
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// 窗口左上角的屏幕纵坐标（物理像素）
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// 正常状态下的窗口宽度（逻辑像素）
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// 正常状态下的窗口高度（逻辑像素）
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// 上次关闭时窗口是否处于最大化状态
    /// </summary>
    public bool IsMaximized { get; set; }
}
