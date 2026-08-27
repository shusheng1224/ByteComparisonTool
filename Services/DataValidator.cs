using ByteComparisonTool.Models;

namespace ByteComparisonTool.Services;

/// <summary>
/// 按长度、全部内容或指定区间比较板卡返回数据
/// </summary>
public sealed class DataValidator
{
    /// <summary>
    /// 解析十六进制文本并按指定规则完成校验
    /// </summary>
    /// <param name="expectedText">预期返回数据的十六进制文本</param>
    /// <param name="actualText">实际返回数据的十六进制文本</param>
    /// <param name="rule">长度和内容比较规则</param>
    /// <returns>包含长度状态、全部差异和展示摘要的校验结果</returns>
    public ValidationResult Validate(string? expectedText, string? actualText, ValidationRule rule)
    {
        return Validate(HexDataService.Parse(expectedText), HexDataService.Parse(actualText), rule);
    }

    /// <summary>
    /// 按指定规则校验预期字节与实际字节
    /// </summary>
    /// <param name="expected">预期返回数据</param>
    /// <param name="actual">实际返回数据</param>
    /// <param name="rule">长度和内容比较规则</param>
    /// <returns>包含长度状态、全部差异和展示摘要的校验结果</returns>
    public ValidationResult Validate(IReadOnlyList<byte> expected, IReadOnlyList<byte> actual,
        ValidationRule rule)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(rule);

        bool lengthMatches = expected.Count == actual.Count;
        IReadOnlyList<ByteDifference> differences = rule.Mode switch
        {
            ValidationMode.Full => CompareFull(expected, actual, rule.RequireLengthMatch),
            ValidationMode.Ranges => CompareRanges(expected, actual, rule.Ranges),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule.Mode, "不支持的校验模式")
        };

        bool success = (!rule.RequireLengthMatch || lengthMatches) && differences.Count == 0;
        return new ValidationResult
        {
            IsSuccess = success,
            LengthMatches = lengthMatches,
            ExpectedLength = expected.Count,
            ActualLength = actual.Count,
            Differences = differences,
            Message = BuildMessage(success, lengthMatches, expected.Count, actual.Count, differences)
        };
    }

    /// <summary>
    /// 比较完整字节内容
    /// </summary>
    /// <param name="expected">预期返回数据</param>
    /// <param name="actual">实际返回数据</param>
    /// <param name="includeMissingBytes">是否将长度差产生的缺失字节记录为差异</param>
    /// <returns>按偏移升序排列的字节差异</returns>
    private static IReadOnlyList<ByteDifference> CompareFull(IReadOnlyList<byte> expected,
        IReadOnlyList<byte> actual, bool includeMissingBytes)
    {
        int comparisonLength = includeMissingBytes
            ? Math.Max(expected.Count, actual.Count)
            : Math.Min(expected.Count, actual.Count);
        List<ByteDifference> differences = [];

        for (int offset = 0; offset < comparisonLength; offset++)
        {
            byte? expectedValue = offset < expected.Count ? expected[offset] : null;
            byte? actualValue = offset < actual.Count ? actual[offset] : null;
            if (expectedValue != actualValue)
            {
                differences.Add(new ByteDifference(offset, expectedValue, actualValue));
            }
        }

        return differences;
    }

    /// <summary>
    /// 比较规则中配置的全部字节区间
    /// </summary>
    /// <param name="expected">预期返回数据</param>
    /// <param name="actual">实际返回数据</param>
    /// <param name="ranges">需要比较的连续字节区间</param>
    /// <returns>去重后按偏移升序排列的字节差异</returns>
    private static IReadOnlyList<ByteDifference> CompareRanges(IReadOnlyList<byte> expected,
        IReadOnlyList<byte> actual, IEnumerable<ComparisonRange> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);
        SortedDictionary<int, ByteDifference> differences = [];

        foreach (ComparisonRange range in ranges)
        {
            ArgumentNullException.ThrowIfNull(range);
            if (range.Offset < 0 || range.Length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ranges), "比较区间偏移不能为负数且长度必须大于零");
            }

            if ((long)range.Offset + range.Length > expected.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(ranges),
                    $"比较区间 {range.Name} 超出预期数据长度");
            }

            for (int offset = range.Offset; offset < range.Offset + range.Length; offset++)
            {
                byte expectedValue = expected[offset];
                byte? actualValue = offset < actual.Count ? actual[offset] : null;
                if (expectedValue != actualValue)
                {
                    differences[offset] = new ByteDifference(offset, expectedValue, actualValue);
                }
            }
        }

        return differences.Values.ToArray();
    }

    /// <summary>
    /// 构建适合在结果区域直接展示的校验摘要
    /// </summary>
    /// <param name="success">校验是否通过</param>
    /// <param name="lengthMatches">数据长度是否一致</param>
    /// <param name="expectedLength">预期字节长度</param>
    /// <param name="actualLength">实际字节长度</param>
    /// <param name="differences">按偏移升序排列的字节差异</param>
    /// <returns>中文校验结果摘要</returns>
    private static string BuildMessage(bool success, bool lengthMatches, int expectedLength,
        int actualLength, IReadOnlyList<ByteDifference> differences)
    {
        if (success)
        {
            return "校验通过";
        }

        if (!lengthMatches && differences.Count > 0)
        {
            return $"长度不一致（期望 {expectedLength} 字节，实际 {actualLength} 字节），" +
                $"共 {differences.Count} 处差异；详情见下表";
        }

        if (!lengthMatches)
        {
            return $"长度不一致（期望 {expectedLength} 字节，实际 {actualLength} 字节）";
        }

        return $"共 {differences.Count} 处差异；详情见下表";
    }
}
