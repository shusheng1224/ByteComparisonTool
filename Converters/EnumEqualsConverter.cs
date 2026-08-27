using System.Globalization;
using Avalonia.Data.Converters;

namespace ByteComparisonTool.Converters;

/// <summary>
/// 判断枚举绑定值是否与转换参数中的成员名称一致
/// </summary>
public sealed class EnumEqualsConverter : IValueConverter
{
    /// <summary>
    /// 将枚举值与作为字符串传入的目标成员名称进行比较
    /// </summary>
    /// <param name="value">需要判断的枚举值</param>
    /// <param name="targetType">目标绑定类型</param>
    /// <param name="parameter">期望匹配的枚举成员名称</param>
    /// <param name="culture">当前绑定文化</param>
    /// <returns>枚举名称与目标名称一致时为 true，否则为 false</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Enum && parameter is string expectedName &&
            string.Equals(value.ToString(), expectedName, StringComparison.Ordinal);
    }

    /// <summary>
    /// 阻止将可见状态反向转换为枚举值
    /// </summary>
    /// <param name="value">显示层传入的可见状态</param>
    /// <param name="targetType">目标枚举类型</param>
    /// <param name="parameter">期望匹配的枚举成员名称</param>
    /// <param name="culture">当前绑定文化</param>
    /// <returns>不会返回值，此方法始终抛出异常</returns>
    /// <exception cref="NotSupportedException">此转换器仅用于单向显示时抛出</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException("枚举相等转换器不支持反向转换");
    }
}
