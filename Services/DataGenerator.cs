using ByteComparisonTool.Models;

namespace ByteComparisonTool.Services;

/// <summary>
/// 根据测试阶段的数据生成规则创建待发送字节
/// </summary>
public sealed class DataGenerator
{
    /// <summary>
    /// 根据测试阶段生成待发送数据
    /// </summary>
    /// <param name="stage">包含数据生成规则的测试阶段</param>
    /// <returns>新生成且由调用方拥有的字节数组</returns>
    public byte[] Generate(TestStage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        return Generate(stage.Input);
    }

    /// <summary>
    /// 根据指定规则生成待发送数据
    /// </summary>
    /// <param name="rule">固定、递增、随机或重复数据生成规则</param>
    /// <returns>新生成且由调用方拥有的字节数组</returns>
    /// <exception cref="ArgumentOutOfRangeException">动态数据长度为负数或生成类型不受支持时抛出</exception>
    /// <exception cref="ArgumentException">随机生成规则未指定种子时抛出</exception>
    public byte[] Generate(DataGenerationRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (rule.Type is GenerationType.Incrementing or GenerationType.Random && rule.Length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rule), "生成数据长度不能为负数");
        }

        return rule.Type switch
        {
            GenerationType.Fixed => HexDataService.Parse(rule.FixedData),
            GenerationType.Incrementing => GenerateIncrementing(rule.Length, rule.StartValue),
            GenerationType.Random => GenerateRandom(rule.Length, rule.Seed ??
                throw new ArgumentException("随机生成必须指定随机种子", nameof(rule))),
            GenerationType.Repeated => GenerateRepeated(rule.RepeatedData, rule.RepeatCount,
                rule.ByteLimit),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule.Type, "不支持的数据生成类型")
        };
    }

    /// <summary>
    /// 生成从指定字节开始且逐字节递增的数据
    /// </summary>
    /// <param name="length">需要生成的字节数量</param>
    /// <param name="startValue">第一个字节的值</param>
    /// <returns>在 255 后回绕到 0 的递增字节数组</returns>
    private static byte[] GenerateIncrementing(int length, byte startValue)
    {
        byte[] data = new byte[length];
        for (int index = 0; index < length; index++)
        {
            data[index] = unchecked((byte)(startValue + index));
        }

        return data;
    }

    /// <summary>
    /// 生成随机字节数据
    /// </summary>
    /// <param name="length">需要生成的字节数量</param>
    /// <param name="seed">用于产生可重复伪随机数据的固定种子</param>
    /// <returns>指定长度的随机字节数组</returns>
    private static byte[] GenerateRandom(int length, int seed)
    {
        byte[] data = new byte[length];
        new Random(seed).NextBytes(data);
        return data;
    }

    /// <summary>
    /// 将指定字节模式重复到次数或字节上限并在上限处截断
    /// </summary>
    /// <param name="patternText">需要循环写入的十六进制字节模式</param>
    /// <param name="repeatCount">字节模式允许写入的最大次数</param>
    /// <param name="byteLimit">结果允许包含的最大字节数量</param>
    /// <returns>长度为模式重复结果与字节上限较小值的字节数组</returns>
    /// <exception cref="ArgumentOutOfRangeException">重复次数或字节上限不大于零时抛出</exception>
    /// <exception cref="ArgumentException">重复字节模式为空时抛出</exception>
    private static byte[] GenerateRepeated(string? patternText, int repeatCount, int byteLimit)
    {
        if (repeatCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repeatCount), "重复次数必须大于零");
        }

        if (byteLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLimit), "字节上限必须大于零");
        }

        byte[] pattern = HexDataService.Parse(patternText);
        if (pattern.Length == 0)
        {
            throw new ArgumentException("重复数据不能为空", nameof(patternText));
        }

        int resultLength = (int)Math.Min((long)pattern.Length * repeatCount, byteLimit);
        byte[] result = new byte[resultLength];
        for (int offset = 0; offset < result.Length; offset += pattern.Length)
        {
            int copyLength = Math.Min(pattern.Length, result.Length - offset);
            pattern.AsSpan(0, copyLength).CopyTo(result.AsSpan(offset, copyLength));
        }

        return result;
    }
}
