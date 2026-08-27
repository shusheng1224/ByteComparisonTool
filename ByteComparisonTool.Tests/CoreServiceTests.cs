using ByteComparisonTool.Models;
using ByteComparisonTool.Services;
using ByteComparisonTool.Infrastructure;
using ByteComparisonTool.ViewModels;
using Xunit;

namespace ByteComparisonTool.Tests;

/// <summary>
/// 覆盖十六进制解析、生成、校验和配置持久化的核心服务测试
/// </summary>
public sealed class CoreServiceTests
{
    /// <summary>
    /// 验证常见分隔符和前缀能够统一解析和格式化
    /// </summary>
    [Fact]
    public void HexDataService_NormalizesSupportedInput()
    {
        byte[] data = HexDataService.Parse("0x00, ff\r\n12-34");

        Assert.Equal(new byte[] { 0x00, 0xFF, 0x12, 0x34 }, data);
        Assert.Equal("00 FF 12 34", HexDataService.Format(data));
    }

    /// <summary>
    /// 验证十六进制格式化结果每行固定包含最多十六个字节
    /// </summary>
    [Fact]
    public void HexDataService_FormatsSixteenBytesPerLine()
    {
        byte[] data = Enumerable.Range(0, 17).Select(value => (byte)value).ToArray();

        string expected = string.Join(Environment.NewLine,
            "00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F", "10");

        Assert.Equal(expected, HexDataService.Format(data));
    }

    /// <summary>
    /// 验证奇数位和非法字符会被明确拒绝
    /// </summary>
    [Theory]
    [InlineData("ABC")]
    [InlineData("GG")]
    public void HexDataService_RejectsInvalidInput(string text)
    {
        Assert.False(HexDataService.TryParse(text, out byte[] data, out string error));
        Assert.Empty(data);
        Assert.NotEmpty(error);
    }

    /// <summary>
    /// 验证递增生成器在 FF 后按字节边界回绕
    /// </summary>
    [Fact]
    public void DataGenerator_IncrementingDataWrapsAtByteBoundary()
    {
        DataGenerationRule rule = new()
        {
            Type = GenerationType.Incrementing,
            Length = 4,
            StartValue = 0xFE
        };

        byte[] data = new DataGenerator().Generate(rule);

        Assert.Equal(new byte[] { 0xFE, 0xFF, 0x00, 0x01 }, data);
    }

    /// <summary>
    /// 验证相同随机种子在多次生成时产生完全相同的数据
    /// </summary>
    [Fact]
    public void DataGenerator_RandomDataIsRepeatableForConfiguredSeed()
    {
        DataGenerationRule rule = new()
        {
            Type = GenerationType.Random,
            Length = 32,
            Seed = 20260825
        };

        DataGenerator generator = new();
        byte[] first = generator.Generate(rule);
        byte[] second = generator.Generate(rule);

        Assert.Equal(first, second);
    }

    /// <summary>
    /// 验证重复生成器按次数循环字节模式并在字节上限处截断
    /// </summary>
    [Fact]
    public void DataGenerator_RepeatedDataStopsAtByteLimit()
    {
        DataGenerationRule rule = new()
        {
            Type = GenerationType.Repeated,
            RepeatedData = "AA BB CC",
            RepeatCount = 4,
            ByteLimit = 8
        };

        byte[] data = new DataGenerator().Generate(rule);

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xAA, 0xBB, 0xCC, 0xAA, 0xBB }, data);
    }

    /// <summary>
    /// 验证全量比较报告长度差异和缺失尾字节
    /// </summary>
    [Fact]
    public void DataValidator_FullComparisonReportsLengthAndTailDifference()
    {
        ValidationResult result = new DataValidator().Validate(
            [0xAA, 0xBB, 0xCC], [0xAA, 0xBB], new ValidationRule());

        Assert.False(result.IsSuccess);
        Assert.False(result.LengthMatches);
        ByteDifference difference = Assert.Single(result.Differences);
        Assert.Equal(2, difference.Offset);
        Assert.Equal((byte)0xCC, difference.Expected);
        Assert.Null(difference.Actual);
        Assert.DoesNotContain(Environment.NewLine, result.Message);
        Assert.Contains("长度不一致", result.Message);
        Assert.Contains("共 1 处差异", result.Message);
    }

    /// <summary>
    /// 验证区间比较忽略区间外差异并保留长度规则
    /// </summary>
    [Fact]
    public void DataValidator_RangeComparisonOnlyChecksConfiguredBytes()
    {
        ValidationRule rule = new()
        {
            Mode = ValidationMode.Ranges,
            Ranges = [new ComparisonRange { Name = "Payload", Offset = 1, Length = 2 }]
        };

        ValidationResult result = new DataValidator().Validate(
            [0x00, 0x11, 0x22, 0x33], [0xFF, 0x11, 0x22, 0x44], rule);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Differences);
    }

    /// <summary>
    /// 验证配置保存和重新加载保持阶段顺序及规则
    /// </summary>
    /// <returns>表示配置文件往返测试的任务</returns>
    [Fact]
    public async Task TestConfigurationService_RoundTripsConfiguration()
    {
        TestConfigurationService service = new();
        TestConfiguration original = service.CreateBuiltInTemplate();
        original.Stages[0].GeneratedDataAddress = "00 00 10 00";
        original.Stages[0].ExpectedDataAddress = "00 00 20 00";
        original.Stages[0].Input = new DataGenerationRule
        {
            Type = GenerationType.Repeated,
            RepeatedData = "AA 55",
            RepeatCount = 8,
            ByteLimit = 12
        };
        string directoryPath = Path.Combine(Path.GetTempPath(), $"ByteComparisonTool-{Guid.NewGuid():N}");
        string filePath = Path.Combine(directoryPath, "tests.json");

        try
        {
            await service.SaveAsync(filePath, original);
            TestConfiguration loaded = await service.LoadAsync(filePath);

            Assert.Equal(original.Name, loaded.Name);
            Assert.Equal(original.Stages.Select(stage => stage.Name),
                loaded.Stages.Select(stage => stage.Name));
            Assert.Equal(GenerationType.Repeated, loaded.Stages[0].Input.Type);
            Assert.Equal("00 00 10 00", loaded.Stages[0].GeneratedDataAddress);
            Assert.Equal("00 00 20 00", loaded.Stages[0].ExpectedDataAddress);
            Assert.Equal("AA 55", loaded.Stages[0].Input.RepeatedData);
            Assert.Equal(8, loaded.Stages[0].Input.RepeatCount);
            Assert.Equal(12, loaded.Stages[0].Input.ByteLimit);
            Assert.Equal(GenerationType.Incrementing, loaded.Stages[1].Input.Type);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
    }

    /// <summary>
    /// 验证启动初始化会加载固定目录中的全部 JSON 配置
    /// </summary>
    /// <returns>表示固定目录配置发现测试的任务</returns>
    [Fact]
    public async Task MainViewModel_InitializeLoadsAllConfigurationsFromFixedDirectory()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(),
            $"ByteComparisonTool-Configs-{Guid.NewGuid():N}");
        TestConfigurationService service = new();
        TestConfiguration first = service.CreateBuiltInTemplate();
        TestConfiguration second = service.CreateBuiltInTemplate();
        first.Name = "配置 A";
        second.Name = "配置 B";

        try
        {
            await service.SaveAsync(Path.Combine(directoryPath, "a.json"), first);
            await service.SaveAsync(Path.Combine(directoryPath, "b.json"), second);
            using MainViewModel viewModel = new(new NullDesktopService(), new DataGenerator(),
                new DataValidator(), service, directoryPath, null);

            await viewModel.InitializeAsync();

            Assert.Equal(2, viewModel.Configurations.Count);
            Assert.Equal(["配置 A", "配置 B"],
                viewModel.Configurations.Select(option => option.DisplayName));
            Assert.Equal("配置 A", viewModel.SelectedConfiguration?.DisplayName);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
    }

    /// <summary>
    /// 验证配置编辑器切换生成方式会立即同步阶段模型和摘要
    /// </summary>
    /// <returns>表示生成方式通知测试的任务</returns>
    [Fact]
    public async Task MainViewModel_GenerationTypeChangeUpdatesStageImmediately()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(),
            $"ByteComparisonTool-GenerationType-{Guid.NewGuid():N}");

        try
        {
            using MainViewModel viewModel = new(new NullDesktopService(), new DataGenerator(),
                new DataValidator(), new TestConfigurationService(), directoryPath, null);

            await viewModel.InitializeAsync();

            viewModel.SelectedGenerationType = GenerationType.Repeated;
            viewModel.SelectedStage!.Model.Input.RepeatedData = "AA 55";
            viewModel.SelectedStage.Model.Input.RepeatCount = 3;
            viewModel.SelectedStage.Model.Input.ByteLimit = 5;

            Assert.Equal(GenerationType.Repeated, viewModel.SelectedStage?.Model.Input.Type);
            Assert.Contains("重复", viewModel.GeneratorTypeText);
            Assert.Contains("重复", viewModel.SelectedStage?.Description);
            Assert.Equal("AA 55 AA 55 AA", viewModel.GeneratedPreviewData);
            Assert.Equal("0000", viewModel.GeneratedPreviewRowHeaders);
            Assert.Equal("5 字节", viewModel.GeneratedPreviewStatusText);
            Assert.Equal("0000", viewModel.GeneratedRowHeaders);
            Assert.Equal("0000", viewModel.ExpectedRowHeaders);

            viewModel.ExpectedData = string.Join(',', Enumerable.Range(0, 17)
                .Select(value => value.ToString("X2")));
            viewModel.FormatExpectedDataCommand.Execute(null);

            Assert.Equal(HexDataService.Format(Enumerable.Range(0, 17)
                .Select(value => (byte)value)), viewModel.ExpectedData);
            Assert.Equal(string.Join(Environment.NewLine, "0000", "0010"),
                viewModel.ExpectedRowHeaders);
            Assert.Equal("17 字节", viewModel.ExpectedByteCountText);

            viewModel.ActualData = HexDataService.Format(Enumerable.Range(0, 17)
                .Select(value => (byte)value));

            Assert.Equal(string.Join(Environment.NewLine, "0000", "0010"),
                viewModel.ActualRowHeaders);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
    }

    /// <summary>
    /// 验证阶段校验通过后会自动切换到下一阶段且不再弹出确认
    /// </summary>
    /// <returns>表示自动阶段切换测试的任务</returns>
    [Fact]
    public async Task MainViewModel_ValidationSuccessAdvancesToNextStageAutomatically()
    {
        using MainViewModel viewModel = new(new NullDesktopService(), new DataGenerator(),
            new DataValidator(), new TestConfigurationService(),
            Path.Combine(Path.GetTempPath(), $"ByteComparisonTool-AutoNext-{Guid.NewGuid():N}"),
            null);

        viewModel.ActualData = viewModel.ExpectedData;

        await viewModel.ValidateDataCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.SelectedStage?.Index);
        Assert.Contains("已自动进入", viewModel.StatusMessage);
    }

    /// <summary>
    /// 验证重新开始会清空全部阶段结果并返回第一阶段
    /// </summary>
    /// <returns>表示校验进度重置测试的任务</returns>
    [Fact]
    public async Task MainViewModel_RestartValidationClearsAllStageResults()
    {
        using MainViewModel viewModel = new(new NullDesktopService(), new DataGenerator(),
            new DataValidator(), new TestConfigurationService(),
            Path.Combine(Path.GetTempPath(), $"ByteComparisonTool-Restart-{Guid.NewGuid():N}"),
            null);

        viewModel.ActualData = viewModel.ExpectedData;
        await viewModel.ValidateDataCommand.ExecuteAsync(null);
        viewModel.ActualData = "00";
        await viewModel.ValidateDataCommand.ExecuteAsync(null);

        Assert.Contains(viewModel.Stages, stage => stage.StatusText == "通过");
        Assert.Contains(viewModel.Stages, stage => stage.StatusText == "失败");

        viewModel.RestartValidationCommand.Execute(null);

        Assert.All(viewModel.Stages, stage => Assert.Equal("待测试", stage.StatusText));
        Assert.Equal(0, viewModel.SelectedStage?.Index);
        Assert.Empty(viewModel.ActualData);
        Assert.Empty(viewModel.Differences);
        Assert.Equal(ValidationState.Idle, viewModel.ValidationState);
        Assert.Equal("最近校验：尚未校验", viewModel.LastValidationTimeText);
    }

    /// <summary>
    /// 验证固定目录首次为空时会创建并加载默认配置文件
    /// </summary>
    /// <returns>表示首次配置目录初始化测试的任务</returns>
    [Fact]
    public async Task MainViewModel_InitializeCreatesDefaultConfigurationForEmptyDirectory()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(),
            $"ByteComparisonTool-EmptyConfigs-{Guid.NewGuid():N}");

        try
        {
            using MainViewModel viewModel = new(new NullDesktopService(), new DataGenerator(),
                new DataValidator(), new TestConfigurationService(), directoryPath, null);

            await viewModel.InitializeAsync();

            Assert.Single(viewModel.Configurations);
            Assert.True(File.Exists(Path.Combine(directoryPath, "default-board-tests.json")));

            await viewModel.AddConfigurationCommand.ExecuteAsync(null);

            Assert.Equal(2, viewModel.Configurations.Count);
            Assert.Equal("新配置 1", viewModel.SelectedConfiguration?.DisplayName);
            Assert.Equal(2, Directory.GetFiles(directoryPath, "*.json").Length);

            viewModel.SelectedConfiguration!.DisplayName = "重命名配置";
            viewModel.SelectedStage!.Name = "重命名阶段";
            viewModel.AddComparisonRangeCommand.Execute(null);
            viewModel.ComparisonRanges[0].Name = "Header";
            viewModel.ComparisonRanges[0].Length = 2;
            string sourceStageId = viewModel.SelectedStage.Model.Id;
            viewModel.DuplicateStageCommand.Execute(null);

            Assert.Equal("重命名配置", viewModel.SelectedConfiguration.Configuration.Name);
            Assert.Equal(2, viewModel.Stages.Count);
            Assert.Equal("重命名阶段 - 副本", viewModel.SelectedStage?.Name);
            Assert.NotEqual(sourceStageId, viewModel.SelectedStage?.Model.Id);
            Assert.NotSame(viewModel.Stages[0].Model.Input, viewModel.Stages[1].Model.Input);
            Assert.NotSame(viewModel.Stages[0].Model.Validation.Ranges[0],
                viewModel.Stages[1].Model.Validation.Ranges[0]);

            await viewModel.SaveConfigCommand.ExecuteAsync(null);
            TestConfiguration saved = await new TestConfigurationService().LoadAsync(
                viewModel.SelectedConfiguration.FilePath!);

            Assert.Equal("重命名配置", saved.Name);
            Assert.Equal(["重命名阶段", "重命名阶段 - 副本"],
                saved.Stages.Select(stage => stage.Name));

            await viewModel.EditConfigurationCommand.ExecuteAsync(null);
            Assert.NotNull(viewModel.SelectedStage);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
    }

    /// <summary>
    /// 验证切换到其他配置后取消编辑不会把原配置快照写入其他配置
    /// </summary>
    /// <returns>表示跨配置取消编辑回滚测试的任务</returns>
    [Fact]
    public async Task MainViewModel_CancelEditorDoesNotOverwriteAnotherConfiguration()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(),
            $"ByteComparisonTool-EditorCancel-{Guid.NewGuid():N}");
        TestConfigurationService configurationService = new();
        TestConfiguration first = configurationService.CreateBuiltInTemplate();
        TestConfiguration second = configurationService.CreateBuiltInTemplate();
        first.Name = "配置 A";
        second.Name = "配置 B";
        MainViewModel? viewModel = null;
        CallbackDesktopService desktopService = new(() =>
        {
            viewModel!.SelectedConfiguration = viewModel.Configurations[1];
            return Task.FromResult(false);
        });

        try
        {
            await configurationService.SaveAsync(Path.Combine(directoryPath, "a.json"), first);
            await configurationService.SaveAsync(Path.Combine(directoryPath, "b.json"), second);
            viewModel = new MainViewModel(desktopService, new DataGenerator(), new DataValidator(),
                configurationService, directoryPath, null);
            await viewModel.InitializeAsync();

            await viewModel.EditConfigurationCommand.ExecuteAsync(null);

            Assert.Equal("配置 A", viewModel.SelectedConfiguration?.DisplayName);
            Assert.Equal("配置 A", viewModel.Configurations[0].Configuration.Name);
            Assert.Equal("配置 B", viewModel.Configurations[1].Configuration.Name);
        }
        finally
        {
            viewModel?.Dispose();
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }
        }
    }

    /// <summary>
    /// 验证配置校验拒绝空预期数据和无效动态生成长度
    /// </summary>
    [Fact]
    public void TestConfigurationService_RejectsEmptyExpectedDataAndInvalidLength()
    {
        TestConfigurationService service = new();
        TestConfiguration configuration = service.CreateBuiltInTemplate();
        configuration.Stages[0].ExpectedData = string.Empty;

        Assert.Throws<InvalidDataException>(() => service.Validate(configuration));

        configuration = service.CreateBuiltInTemplate();
        configuration.Stages[1].Input.Length = 0;

        Assert.Throws<InvalidDataException>(() => service.Validate(configuration));
    }

    /// <summary>
    /// 验证配置校验拒绝无效的重复数据参数
    /// </summary>
    [Fact]
    public void TestConfigurationService_RejectsInvalidRepeatedDataParameters()
    {
        TestConfigurationService service = new();
        TestConfiguration configuration = service.CreateBuiltInTemplate();
        configuration.Stages[0].Input = new DataGenerationRule
        {
            Type = GenerationType.Repeated,
            RepeatedData = "AA BB",
            RepeatCount = 0,
            ByteLimit = 64
        };

        Assert.Throws<InvalidDataException>(() => service.Validate(configuration));
    }

    /// <summary>
    /// 验证配置校验拒绝空地址和包含非法字符的地址
    /// </summary>
    [Fact]
    public void TestConfigurationService_RejectsInvalidBoardAddresses()
    {
        TestConfigurationService service = new();
        TestConfiguration configuration = service.CreateBuiltInTemplate();
        configuration.Stages[0].GeneratedDataAddress = string.Empty;

        Assert.Throws<InvalidDataException>(() => service.Validate(configuration));

        configuration = service.CreateBuiltInTemplate();
        configuration.Stages[0].ExpectedDataAddress = "0xNOT-HEX";

        Assert.Throws<InvalidDataException>(() => service.Validate(configuration));
    }

    /// <summary>
    /// 验证配置校验拒绝未指定种子的随机生成规则
    /// </summary>
    [Fact]
    public void TestConfigurationService_RejectsRandomRuleWithoutSeed()
    {
        TestConfigurationService service = new();
        TestConfiguration configuration = service.CreateBuiltInTemplate();
        configuration.Stages[2].Input.Seed = null;

        Assert.Throws<InvalidDataException>(() => service.Validate(configuration));
    }

    /// <summary>
    /// 验证加载旧配置时会将空随机种子迁移为确定性的零种子
    /// </summary>
    /// <returns>表示旧配置加载与迁移测试的任务</returns>
    [Fact]
    public async Task TestConfigurationService_LoadMigratesLegacyRandomSeed()
    {
        string filePath = Path.Combine(Path.GetTempPath(),
            $"ByteComparisonTool-Legacy-{Guid.NewGuid():N}.json");
        const string json = """
            {
              "name": "旧随机配置",
              "version": 1,
              "stages": [
                {
                  "id": "legacy-random",
                  "name": "随机阶段",
                  "input": {
                    "type": "random",
                    "length": 4,
                    "seed": null
                  },
                  "expectedData": "AA",
                  "validation": {
                    "mode": "full",
                    "requireLengthMatch": true,
                    "ranges": []
                  }
                }
              ]
            }
            """;

        try
        {
            await File.WriteAllTextAsync(filePath, json);
            TestConfiguration configuration =
                await new TestConfigurationService().LoadAsync(filePath);

            Assert.Equal(0, configuration.Stages[0].Input.Seed);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    /// <summary>
    /// 为视图模型测试提供可编排的配置编辑回调和无副作用桌面能力
    /// </summary>
    private sealed class CallbackDesktopService : IDesktopService
    {
        /// <summary>
        /// 打开配置编辑器时执行的测试回调
        /// </summary>
        private readonly Func<Task<bool>> _editConfigurationAsync;

        /// <summary>
        /// 创建使用指定配置编辑回调的测试桌面服务
        /// </summary>
        /// <param name="editConfigurationAsync">模拟编辑器交互并返回保存结果的回调</param>
        public CallbackDesktopService(Func<Task<bool>> editConfigurationAsync)
        {
            _editConfigurationAsync = editConfigurationAsync;
        }

        /// <inheritdoc />
        public Task<string?> PickConfigurationToOpenAsync()
        {
            return Task.FromResult<string?>(null);
        }

        /// <inheritdoc />
        public Task<string?> PickConfigurationToSaveAsync(string suggestedFileName)
        {
            return Task.FromResult<string?>(null);
        }

        /// <inheritdoc />
        public Task CopyTextAsync(string text)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<string?> GetClipboardTextAsync()
        {
            return Task.FromResult<string?>(null);
        }

        /// <inheritdoc />
        public Task<bool> ConfirmAsync(string title, string message)
        {
            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<bool> EditConfigurationAsync()
        {
            return _editConfigurationAsync();
        }
    }
}
