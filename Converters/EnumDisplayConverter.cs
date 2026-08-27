using System.Globalization;
using Avalonia.Data.Converters;
using ByteComparisonTool.Models;

namespace ByteComparisonTool.Converters;

/// <summary>
/// 将配置枚举值转换为面向用户的中文文本
/// </summary>
public sealed class EnumDisplayConverter : IValueConverter
{
    /// <summary>
    /// 将生成方式或校验方式枚举转换为中文显示文本
    /// </summary>
    /// <param name="value">需要显示的枚举值</param>
    /// <param name="targetType">目标绑定类型</param>
    /// <param name="parameter">未使用的转换参数</param>
    /// <param name="culture">当前绑定文化</param>
    /// <returns>对应的中文显示文本，未知值使用原始文本</returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            GenerationType.Fixed => "固定数据",
            GenerationType.Incrementing => "递增数据",
            GenerationType.Random => "随机数据",
            GenerationType.Repeated => "重复数据",
            ValidationMode.Full => "全量比较",
            ValidationMode.Ranges => "指定字段比较",
            _ => value?.ToString()
        };
    }

    /// <summary>
    /// 阻止将显示文本反向转换为枚举值
    /// </summary>
    /// <param name="value">显示层传入的值</param>
    /// <param name="targetType">目标枚举类型</param>
    /// <param name="parameter">未使用的转换参数</param>
    /// <param name="culture">当前绑定文化</param>
    /// <returns>不会返回值，此方法始终抛出异常</returns>
    /// <exception cref="NotSupportedException">此转换器仅用于单向显示时抛出</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("枚举显示转换器不支持反向转换");
    }
}
