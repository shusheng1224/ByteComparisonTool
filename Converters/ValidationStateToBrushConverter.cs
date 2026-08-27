using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ByteComparisonTool.Models;

namespace ByteComparisonTool.Converters;

/// <summary>
/// 将校验语义状态转换为界面使用的状态颜色
/// </summary>
public sealed class ValidationStateToBrushConverter : IValueConverter
{
    /// <summary>
    /// 将校验状态映射为等待、通过或失败颜色
    /// </summary>
    /// <param name="value">需要转换的校验状态</param>
    /// <param name="targetType">目标绑定类型</param>
    /// <param name="parameter">保留的绑定参数</param>
    /// <param name="culture">当前绑定文化</param>
    /// <returns>与校验状态对应的纯色画刷</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ValidationState state
            ? state switch
            {
                ValidationState.Success => new SolidColorBrush(Color.Parse("#1A8C61")),
                ValidationState.Failure => new SolidColorBrush(Color.Parse("#DC2626")),
                _ => new SolidColorBrush(Color.Parse("#64748B"))
            }
            : new SolidColorBrush(Color.Parse("#64748B"));
    }

    /// <summary>
    /// 阻止将状态颜色反向转换为校验状态
    /// </summary>
    /// <param name="value">显示层传入的状态颜色</param>
    /// <param name="targetType">目标校验状态类型</param>
    /// <param name="parameter">保留的绑定参数</param>
    /// <param name="culture">当前绑定文化</param>
    /// <returns>不会返回值，此方法始终抛出异常</returns>
    /// <exception cref="NotSupportedException">此转换器仅用于单向显示时抛出</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException("校验状态颜色转换器不支持反向转换");
    }
}
