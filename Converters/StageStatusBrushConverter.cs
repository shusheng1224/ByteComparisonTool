using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ByteComparisonTool.Converters;

/// <summary>
/// 将阶段状态文本转换为设计稿规定的背景色或前景色
/// </summary>
public sealed class StageStatusBrushConverter : IValueConverter
{
    /// <summary>
    /// 根据阶段状态及转换参数返回背景色或文字颜色
    /// </summary>
    /// <param name="value">阶段状态文本</param>
    /// <param name="targetType">绑定目标属性类型</param>
    /// <param name="parameter">Background 或 Foreground 颜色用途</param>
    /// <param name="culture">绑定转换使用的区域信息</param>
    /// <returns>与阶段状态对应的颜色画刷</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string status = value as string ?? string.Empty;
        bool foreground = string.Equals(parameter as string, "Foreground",
            StringComparison.OrdinalIgnoreCase);

        if (status.Contains("通过", StringComparison.Ordinal))
        {
            return Brush.Parse(foreground ? "#FFFFFF" : "#1A8C61");
        }

        if (status.Contains("失败", StringComparison.Ordinal))
        {
            return Brush.Parse(foreground ? "#FFFFFF" : "#DC2626");
        }

        return Brush.Parse(foreground ? "#66738A" : "#F8FAFC");
    }

    /// <summary>
    /// 阶段状态颜色不支持反向转换
    /// </summary>
    /// <param name="value">目标颜色值</param>
    /// <param name="targetType">绑定源属性类型</param>
    /// <param name="parameter">转换用途参数</param>
    /// <param name="culture">绑定转换使用的区域信息</param>
    /// <returns>始终抛出不支持异常</returns>
    /// <exception cref="NotSupportedException">所有调用均抛出</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
