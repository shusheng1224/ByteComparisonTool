using System.Globalization;
using Avalonia.Data.Converters;
using ByteComparisonTool.Services;

namespace ByteComparisonTool.Converters;

/// <summary>
/// 将配置中的四字节十六进制地址转换为设计稿使用的 0x 前缀显示格式
/// </summary>
public sealed class FourByteAddressDisplayConverter : IValueConverter
{
    /// <summary>
    /// 将最多四个地址字节合并为八位大写十六进制地址
    /// </summary>
    /// <param name="value">空格分隔的十六进制地址文本</param>
    /// <param name="targetType">绑定目标属性类型</param>
    /// <param name="parameter">未使用的转换参数</param>
    /// <param name="culture">绑定转换使用的区域信息</param>
    /// <returns>0x 前缀的四字节地址，格式异常时返回原文本</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string source = value as string ?? string.Empty;
        if (!HexDataService.TryParse(source, out byte[] bytes, out _) || bytes.Length > 4)
        {
            return source;
        }

        string hexadecimal = string.Concat(bytes.Select(item =>
            item.ToString("X2", CultureInfo.InvariantCulture)));
        return $"0x{hexadecimal.PadLeft(8, '0')}";
    }

    /// <summary>
    /// 地址显示格式不支持反向转换
    /// </summary>
    /// <param name="value">目标地址文本</param>
    /// <param name="targetType">绑定源属性类型</param>
    /// <param name="parameter">未使用的转换参数</param>
    /// <param name="culture">绑定转换使用的区域信息</param>
    /// <returns>始终抛出不支持异常</returns>
    /// <exception cref="NotSupportedException">所有调用均抛出</exception>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
