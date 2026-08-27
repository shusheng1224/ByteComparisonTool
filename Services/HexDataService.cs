using System.Globalization;
using System.Text;

namespace ByteComparisonTool.Services;

/// <summary>
/// 提供十六进制字节文本的解析和统一格式化能力
/// </summary>
public static class HexDataService
{
    /// <summary>
    /// 每行最多显示的十六进制字节数量
    /// </summary>
    private const int BytesPerLine = 16;

    /// <summary>
    /// 将十六进制文本解析为字节数组
    /// </summary>
    /// <param name="text">可包含空白、常用分隔符和 0x 前缀的十六进制文本</param>
    /// <returns>按输入顺序解析得到的字节数组</returns>
    /// <exception cref="FormatException">文本包含非十六进制字符或不完整字节时抛出</exception>
    public static byte[] Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        StringBuilder digits = new(text.Length);
        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];
            if (current == '0' && index + 1 < text.Length &&
                (text[index + 1] == 'x' || text[index + 1] == 'X'))
            {
                index++;
                continue;
            }

            if (IsSeparator(current))
            {
                continue;
            }

            if (!Uri.IsHexDigit(current))
            {
                throw new FormatException($"位置 {index} 包含无效的十六进制字符 '{current}'");
            }

            digits.Append(current);
        }

        if (digits.Length % 2 != 0)
        {
            throw new FormatException("十六进制数据必须由完整的两个字符字节组成");
        }

        byte[] data = new byte[digits.Length / 2];
        for (int index = 0; index < data.Length; index++)
        {
            data[index] = byte.Parse(digits.ToString(index * 2, 2),
                NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        }

        return data;
    }

    /// <summary>
    /// 尝试将十六进制文本解析为字节数组
    /// </summary>
    /// <param name="text">可包含空白、常用分隔符和 0x 前缀的十六进制文本</param>
    /// <param name="data">解析成功时输出字节数组，失败时输出空数组</param>
    /// <param name="errorMessage">解析失败时输出错误原因，成功时输出空字符串</param>
    /// <returns>文本合法并已成功解析时为 true，否则为 false</returns>
    public static bool TryParse(string? text, out byte[] data, out string errorMessage)
    {
        try
        {
            data = Parse(text);
            errorMessage = string.Empty;
            return true;
        }
        catch (FormatException exception)
        {
            data = [];
            errorMessage = exception.Message;
            return false;
        }
    }

    /// <summary>
    /// 将字节序列格式化为大写、每行十六个字节且以空格分隔的十六进制文本
    /// </summary>
    /// <param name="data">需要格式化的字节序列</param>
    /// <returns>按每行十六个字节换行的十六进制文本</returns>
    public static string Format(IEnumerable<byte> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return string.Join(Environment.NewLine,
            data.Select(value => value.ToString("X2", CultureInfo.InvariantCulture))
                .Chunk(BytesPerLine)
                .Select(line => string.Join(' ', line)));
    }

    /// <summary>
    /// 判断字符是否为解析时可忽略的常用分隔符
    /// </summary>
    /// <param name="value">需要检查的字符</param>
    /// <returns>字符属于空白、逗号、分号、连字符、冒号或下划线时为 true</returns>
    private static bool IsSeparator(char value)
    {
        return char.IsWhiteSpace(value) || value is ',' or ';' or '-' or ':' or '_';
    }
}
