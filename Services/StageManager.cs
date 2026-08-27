using ByteComparisonTool.Models;

namespace ByteComparisonTool.Services;

/// <summary>
/// 管理当前测试配置及其阶段导航状态
/// </summary>
public sealed class StageManager
{
    /// <summary>
    /// 当前使用的测试配置
    /// </summary>
    private TestConfiguration? _configuration;

    /// <summary>
    /// 获取当前阶段的零基索引，无配置时为 -1
    /// </summary>
    public int CurrentIndex { get; private set; } = -1;

    /// <summary>
    /// 获取当前测试配置
    /// </summary>
    public TestConfiguration? Configuration => _configuration;

    /// <summary>
    /// 获取当前测试阶段，无活动配置时为空
    /// </summary>
    public TestStage? CurrentStage => CurrentIndex >= 0 && _configuration is not null
        ? _configuration.Stages[CurrentIndex]
        : null;

    /// <summary>
    /// 获取当前阶段之前是否还有阶段
    /// </summary>
    public bool CanMovePrevious => CurrentIndex > 0;

    /// <summary>
    /// 获取当前阶段之后是否还有阶段
    /// </summary>
    public bool CanMoveNext => _configuration is not null &&
        CurrentIndex >= 0 && CurrentIndex < _configuration.Stages.Count - 1;

    /// <summary>
    /// 当前配置或阶段发生改变时触发
    /// </summary>
    public event EventHandler? CurrentStageChanged;

    /// <summary>
    /// 设置活动配置并选择第一个阶段
    /// </summary>
    /// <param name="configuration">至少包含一个阶段的测试配置</param>
    public void SetConfiguration(TestConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.Stages is null || configuration.Stages.Count == 0)
        {
            throw new ArgumentException("测试配置必须至少包含一个阶段", nameof(configuration));
        }

        _configuration = configuration;
        CurrentIndex = 0;
        OnCurrentStageChanged();
    }

    /// <summary>
    /// 选择指定索引的测试阶段
    /// </summary>
    /// <param name="index">阶段在当前配置中的零基索引</param>
    public void Select(int index)
    {
        if (_configuration is null)
        {
            throw new InvalidOperationException("尚未加载测试配置");
        }

        if (index < 0 || index >= _configuration.Stages.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (CurrentIndex == index)
        {
            return;
        }

        CurrentIndex = index;
        OnCurrentStageChanged();
    }

    /// <summary>
    /// 尝试切换到下一个测试阶段
    /// </summary>
    /// <returns>成功切换到下一阶段时为 true，已经位于末尾时为 false</returns>
    public bool MoveNext()
    {
        if (!CanMoveNext)
        {
            return false;
        }

        CurrentIndex++;
        OnCurrentStageChanged();
        return true;
    }

    /// <summary>
    /// 尝试切换到上一个测试阶段
    /// </summary>
    /// <returns>成功切换到上一阶段时为 true，已经位于开头时为 false</returns>
    public bool MovePrevious()
    {
        if (!CanMovePrevious)
        {
            return false;
        }

        CurrentIndex--;
        OnCurrentStageChanged();
        return true;
    }

    /// <summary>
    /// 清除活动配置和阶段选择
    /// </summary>
    public void Clear()
    {
        if (_configuration is null)
        {
            return;
        }

        _configuration = null;
        CurrentIndex = -1;
        OnCurrentStageChanged();
    }

    /// <summary>
    /// 通知订阅方当前配置或阶段已经改变
    /// </summary>
    private void OnCurrentStageChanged()
    {
        CurrentStageChanged?.Invoke(this, EventArgs.Empty);
    }
}
