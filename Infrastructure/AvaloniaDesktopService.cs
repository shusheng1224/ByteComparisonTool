using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using ByteComparisonTool.Views;

namespace ByteComparisonTool.Infrastructure;

/// <summary>
/// 使用 Avalonia 桌面窗口实现文件、剪贴板和确认交互
/// </summary>
public sealed class AvaloniaDesktopService : IDesktopService
{
    /// <summary>
    /// 所有桌面交互使用的主窗口
    /// </summary>
    private readonly Window _owner;

    /// <summary>
    /// JSON 配置文件类型
    /// </summary>
    private static readonly FilePickerFileType JsonFileType = new("JSON 配置")
    {
        Patterns = ["*.json"],
        MimeTypes = ["application/json"]
    };

    /// <summary>
    /// 使用指定主窗口创建桌面交互服务
    /// </summary>
    /// <param name="owner">文件选择器和对话框的所有者窗口</param>
    public AvaloniaDesktopService(Window owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <inheritdoc />
    public async Task<string?> PickConfigurationToOpenAsync()
    {
        if (!_owner.StorageProvider.CanOpen)
        {
            throw new NotSupportedException("当前平台不支持打开文件选择器");
        }

        IReadOnlyList<IStorageFile> files = await _owner.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "加载测试配置",
                AllowMultiple = false,
                FileTypeFilter = [JsonFileType]
            });

        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task<string?> PickConfigurationToSaveAsync(string suggestedFileName)
    {
        if (!_owner.StorageProvider.CanSave)
        {
            throw new NotSupportedException("当前平台不支持保存文件选择器");
        }

        IStorageFile? file = await _owner.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "保存测试配置",
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "json",
                FileTypeChoices = [JsonFileType],
                ShowOverwritePrompt = true
            });

        return file?.TryGetLocalPath();
    }

    /// <inheritdoc />
    public async Task CopyTextAsync(string text)
    {
        IClipboard clipboard = _owner.Clipboard ??
            throw new NotSupportedException("当前平台不支持系统剪贴板");
        await clipboard.SetTextAsync(text);
    }

    /// <inheritdoc />
    public async Task<string?> GetClipboardTextAsync()
    {
        IClipboard clipboard = _owner.Clipboard ??
            throw new NotSupportedException("当前平台不支持系统剪贴板");
        return await clipboard.TryGetTextAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message)
    {
        ConfirmationDialog dialog = new(title, message);
        bool? result = await dialog.ShowDialog<bool?>(_owner);
        return result == true;
    }

    /// <inheritdoc />
    public async Task<bool> EditConfigurationAsync()
    {
        ConfigurationEditorDialog dialog = new()
        {
            DataContext = _owner.DataContext
        };
        bool? result = await dialog.ShowDialog<bool?>(_owner);
        return result == true;
    }
}
