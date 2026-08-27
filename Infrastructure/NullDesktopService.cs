namespace ByteComparisonTool.Infrastructure;

/// <summary>
/// 为设计器和无桌面宿主场景提供无副作用的交互实现
/// </summary>
public sealed class NullDesktopService : IDesktopService
{
    /// <inheritdoc />
    public Task<string?> PickConfigurationToOpenAsync()
    {
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task<string?> PickConfigurationToSaveAsync(string suggestedFileName)
    {
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task CopyTextAsync(string text)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<string?> GetClipboardTextAsync()
    {
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message)
    {
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<bool> EditConfigurationAsync()
    {
        return Task.FromResult(false);
    }
}
