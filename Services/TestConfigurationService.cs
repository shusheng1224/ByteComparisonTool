using System.Text.Json;
using System.Text.Json.Serialization;
using ByteComparisonTool.Models;

namespace ByteComparisonTool.Services;

/// <summary>
/// 负责测试配置的 JSON 加载、校验与保存
/// </summary>
public sealed class TestConfigurationService
{
    /// <summary>
    /// 动态生成数据允许的最大字节数量
    /// </summary>
    private const int MaximumGeneratedDataLength = 1_048_576;

    /// <summary>
    /// 从 JSON 文件异步加载测试配置
    /// </summary>
    /// <param name="filePath">配置文件路径</param>
    /// <param name="cancellationToken">取消异步读取操作的令牌</param>
    /// <returns>已经过结构和十六进制数据校验的测试配置</returns>
    /// <exception cref="InvalidDataException">JSON 为空或配置内容无效时抛出</exception>
    public async Task<TestConfiguration> LoadAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        await using FileStream stream = File.OpenRead(filePath);
        TestConfiguration? configuration = await JsonSerializer.DeserializeAsync<TestConfiguration>(
            stream, CreateJsonOptions(false), cancellationToken);

        if (configuration is null)
        {
            throw new InvalidDataException("配置文件未包含有效的测试配置");
        }

        UpgradeLegacyConfiguration(configuration);
        Validate(configuration);
        return configuration;
    }

    /// <summary>
    /// 将测试配置异步保存为便于人工编辑的 JSON 文件
    /// </summary>
    /// <param name="filePath">目标配置文件路径</param>
    /// <param name="configuration">需要保存的测试配置</param>
    /// <param name="cancellationToken">取消异步写入操作的令牌</param>
    /// <returns>表示保存操作的任务</returns>
    public async Task SaveAsync(string filePath, TestConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(configuration);
        Validate(configuration);

        string fullPath = Path.GetFullPath(filePath);
        string? directoryPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using FileStream stream = new(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, configuration, CreateJsonOptions(true),
            cancellationToken);
    }

    /// <summary>
    /// 验证配置结构、生成规则、预期数据和校验区间
    /// </summary>
    /// <param name="configuration">需要验证的测试配置</param>
    /// <exception cref="InvalidDataException">配置缺少必要内容或包含无效规则时抛出</exception>
    public void Validate(TestConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (string.IsNullOrWhiteSpace(configuration.Name))
        {
            throw new InvalidDataException("配置名称不能为空");
        }

        if (configuration.Version <= 0)
        {
            throw new InvalidDataException("配置版本必须大于零");
        }

        if (configuration.Stages is null || configuration.Stages.Count == 0)
        {
            throw new InvalidDataException("配置必须至少包含一个测试阶段");
        }

        HashSet<string> stageIds = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < configuration.Stages.Count; index++)
        {
            TestStage? stage = configuration.Stages[index];
            ValidateStage(stage, index, stageIds);
        }
    }

    /// <summary>
    /// 创建可直接运行和另存为文件的内置示例配置
    /// </summary>
    /// <returns>包含固定、递增和随机数据阶段的全新配置</returns>
    public TestConfiguration CreateBuiltInTemplate()
    {
        return new TestConfiguration
        {
            Name = "板卡数据校验示例",
            Stages =
            [
                new TestStage
                {
                    Name = "Stage 1 - 固定数据",
                    Input = new DataGenerationRule
                    {
                        Type = GenerationType.Fixed,
                        FixedData = "00 FF 12 34 56 78"
                    },
                    ExpectedData = "AA BB CC DD EE"
                },
                new TestStage
                {
                    Name = "Stage 2 - 递增数据",
                    Input = new DataGenerationRule
                    {
                        Type = GenerationType.Incrementing,
                        Length = 16,
                        StartValue = 0
                    },
                    ExpectedData = "00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F"
                },
                new TestStage
                {
                    Name = "Stage 3 - 随机数据",
                    Input = new DataGenerationRule
                    {
                        Type = GenerationType.Random,
                        Length = 256,
                        Seed = 20260825
                    },
                    ExpectedData = "5A A5"
                }
            ]
        };
    }

    /// <summary>
    /// 验证单个测试阶段及其嵌套规则
    /// </summary>
    /// <param name="stage">需要验证的测试阶段</param>
    /// <param name="index">阶段在配置中的零基索引</param>
    /// <param name="stageIds">用于检查重复项的阶段标识集合</param>
    private static void ValidateStage(TestStage? stage, int index, ISet<string> stageIds)
    {
        if (stage is null)
        {
            throw new InvalidDataException($"第 {index + 1} 个测试阶段不能为空");
        }

        if (string.IsNullOrWhiteSpace(stage.Id) || !stageIds.Add(stage.Id))
        {
            throw new InvalidDataException($"阶段 {index + 1} 的标识为空或重复");
        }

        if (string.IsNullOrWhiteSpace(stage.Name))
        {
            throw new InvalidDataException($"阶段 {index + 1} 的名称不能为空");
        }

        if (stage.Input is null)
        {
            throw new InvalidDataException($"阶段 {stage.Name} 缺少数据生成规则");
        }

        ValidateGenerationRule(stage.Name, stage.Input);
        byte[] generatedDataAddress = ParseConfiguredHex(stage.Name, "生成数据地址",
            stage.GeneratedDataAddress);
        if (generatedDataAddress.Length == 0)
        {
            throw new InvalidDataException($"阶段 {stage.Name} 的生成数据地址不能为空");
        }

        byte[] expected = ParseConfiguredHex(stage.Name, "预期数据", stage.ExpectedData);
        if (expected.Length == 0)
        {
            throw new InvalidDataException($"阶段 {stage.Name} 的预期数据不能为空");
        }

        byte[] expectedDataAddress = ParseConfiguredHex(stage.Name, "预期返回数据地址",
            stage.ExpectedDataAddress);
        if (expectedDataAddress.Length == 0)
        {
            throw new InvalidDataException($"阶段 {stage.Name} 的预期返回数据地址不能为空");
        }

        if (stage.Validation is null)
        {
            throw new InvalidDataException($"阶段 {stage.Name} 缺少校验规则");
        }

        ValidateValidationRule(stage.Name, stage.Validation, expected.Length);
    }

    /// <summary>
    /// 验证数据生成规则的长度和十六进制参数格式
    /// </summary>
    /// <param name="stageName">规则所属的阶段名称</param>
    /// <param name="rule">需要验证的数据生成规则</param>
    private static void ValidateGenerationRule(string stageName, DataGenerationRule rule)
    {
        if (!Enum.IsDefined(rule.Type))
        {
            throw new InvalidDataException($"阶段 {stageName} 使用了不支持的数据生成类型");
        }

        if (rule.Type == GenerationType.Fixed)
        {
            if (ParseConfiguredHex(stageName, "固定生成数据", rule.FixedData).Length == 0)
            {
                throw new InvalidDataException($"阶段 {stageName} 的固定生成数据不能为空");
            }
        }
        else if (rule.Type == GenerationType.Repeated)
        {
            if (ParseConfiguredHex(stageName, "重复数据", rule.RepeatedData).Length == 0)
            {
                throw new InvalidDataException($"阶段 {stageName} 的重复数据不能为空");
            }

            if (rule.RepeatCount <= 0)
            {
                throw new InvalidDataException($"阶段 {stageName} 的重复次数必须大于零");
            }

            if (rule.ByteLimit <= 0 || rule.ByteLimit > MaximumGeneratedDataLength)
            {
                throw new InvalidDataException(
                    $"阶段 {stageName} 的字节上限必须在 1 到 {MaximumGeneratedDataLength} 字节之间");
            }
        }
        else if (rule.Type == GenerationType.Random && !rule.Seed.HasValue)
        {
            throw new InvalidDataException($"阶段 {stageName} 的随机生成必须指定随机种子");
        }
        else if (rule.Length <= 0 || rule.Length > MaximumGeneratedDataLength)
        {
            throw new InvalidDataException(
                $"阶段 {stageName} 的生成长度必须在 1 到 {MaximumGeneratedDataLength} 字节之间");
        }
    }

    /// <summary>
    /// 为旧配置中的随机生成规则补充确定性的默认随机种子
    /// </summary>
    /// <param name="configuration">从配置文件反序列化得到的配置</param>
    private static void UpgradeLegacyConfiguration(TestConfiguration configuration)
    {
        if (configuration.Stages is null)
        {
            return;
        }

        foreach (TestStage? stage in configuration.Stages)
        {
            if (stage?.Input?.Type == GenerationType.Random && !stage.Input.Seed.HasValue)
            {
                stage.Input.Seed = 0;
            }
        }
    }

    /// <summary>
    /// 验证内容比较模式和全部比较区间
    /// </summary>
    /// <param name="stageName">规则所属的阶段名称</param>
    /// <param name="rule">需要验证的校验规则</param>
    /// <param name="expectedLength">预期返回数据的字节长度</param>
    private static void ValidateValidationRule(string stageName, ValidationRule rule,
        int expectedLength)
    {
        if (!Enum.IsDefined(rule.Mode))
        {
            throw new InvalidDataException($"阶段 {stageName} 使用了不支持的校验模式");
        }

        if (rule.Mode != ValidationMode.Ranges)
        {
            return;
        }

        if (rule.Ranges is null || rule.Ranges.Count == 0)
        {
            throw new InvalidDataException($"阶段 {stageName} 的区间校验规则至少需要一个比较区间");
        }

        foreach (ComparisonRange? range in rule.Ranges)
        {
            if (range is null || range.Offset < 0 || range.Length <= 0)
            {
                throw new InvalidDataException($"阶段 {stageName} 包含无效的比较区间");
            }

            if ((long)range.Offset + range.Length > expectedLength)
            {
                throw new InvalidDataException($"阶段 {stageName} 的比较区间 {range.Name} 超出预期数据长度");
            }
        }
    }

    /// <summary>
    /// 解析配置中的十六进制文本并将格式错误转换为配置异常
    /// </summary>
    /// <param name="stageName">文本所属的阶段名称</param>
    /// <param name="fieldName">文本所属的配置字段名称</param>
    /// <param name="text">需要解析的十六进制文本</param>
    /// <returns>解析得到的字节数组</returns>
    private static byte[] ParseConfiguredHex(string stageName, string fieldName, string? text)
    {
        try
        {
            return HexDataService.Parse(text);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException($"阶段 {stageName} 的{fieldName}无效：{exception.Message}", exception);
        }
    }

    /// <summary>
    /// 创建支持字符串枚举、注释和尾随逗号的 JSON 序列化选项
    /// </summary>
    /// <param name="writeIndented">是否使用缩进输出 JSON</param>
    /// <returns>用于测试配置读写的 JSON 序列化选项</returns>
    private static JsonSerializerOptions CreateJsonOptions(bool writeIndented)
    {
        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = writeIndented,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
