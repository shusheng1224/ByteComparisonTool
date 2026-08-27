using System.Text;
using System.Text.Json;
using ByteComparisonTool.Models;

namespace ByteComparisonTool.Services;

/// <summary>
/// 将每次阶段校验以一行一个 JSON 对象的形式追加到日志文件
/// </summary>
public sealed class TestLogger : IDisposable
{
    /// <summary>
    /// 日志文件的完整路径
    /// </summary>
    private readonly string _logFilePath;

    /// <summary>
    /// 串行化同一日志实例上的并发写入
    /// </summary>
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>
    /// 指示当前实例是否已经释放
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 使用目标日志文件路径创建日志写入器
    /// </summary>
    /// <param name="logFilePath">需要追加校验记录的文件路径</param>
    public TestLogger(string logFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);
        _logFilePath = Path.GetFullPath(logFilePath);
    }

    /// <summary>
    /// 获取日志文件的完整路径
    /// </summary>
    public string LogFilePath => _logFilePath;

    /// <summary>
    /// 将一条校验记录异步追加到日志文件
    /// </summary>
    /// <param name="entry">包含阶段、数据和校验结果的日志记录</param>
    /// <param name="cancellationToken">取消等待或写入操作的令牌</param>
    /// <returns>表示追加操作的任务</returns>
    public async Task AppendAsync(TestLogEntry entry, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(entry);
        string json = JsonSerializer.Serialize(entry, CreateJsonOptions());

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            string? directoryPath = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await using FileStream stream = new(_logFilePath, FileMode.Append, FileAccess.Write,
                FileShare.Read, 4096, FileOptions.Asynchronous);
            await using StreamWriter writer = new(stream, new UTF8Encoding(false));
            await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 释放日志写入同步资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _writeLock.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// 创建日志序列化使用的紧凑 JSON 选项
    /// </summary>
    /// <returns>使用 camelCase 属性名的 JSON 序列化选项</returns>
    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
}
