using System.Collections.ObjectModel;
using System.ComponentModel;
using ByteComparisonTool.Infrastructure;
using ByteComparisonTool.Models;
using ByteComparisonTool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ByteComparisonTool.ViewModels;

/// <summary>
/// 编排测试配置、阶段数据生成、人工返回数据校验和日志记录
/// </summary>
public partial class MainViewModel : ViewModelBase, IAsyncInitializable, IDisposable
{
    /// <summary>
    /// 配置编辑器为保证响应速度最多渲染的生成预览字节数
    /// </summary>
    private const int GeneratedPreviewByteLimit = 4096;

    /// <summary>
    /// 桌面文件、剪贴板和确认交互服务
    /// </summary>
    private readonly IDesktopService _desktopService;

    /// <summary>
    /// 测试输入数据生成服务
    /// </summary>
    private readonly DataGenerator _dataGenerator;

    /// <summary>
    /// 板卡返回数据校验服务
    /// </summary>
    private readonly DataValidator _dataValidator;

    /// <summary>
    /// 测试配置加载、保存和校验服务
    /// </summary>
    private readonly TestConfigurationService _configurationService;

    /// <summary>
    /// 自动发现、导入和保存 JSON 配置的固定目录
    /// </summary>
    private readonly string _configurationDirectory;

    /// <summary>
    /// 可选的结构化测试日志写入器
    /// </summary>
    private readonly TestLogger? _logger;

    /// <summary>
    /// 当前正在编辑和执行的测试配置
    /// </summary>
    private TestConfiguration _configuration;

    /// <summary>
    /// 最近一次生成且用于当前阶段的字节数据
    /// </summary>
    private byte[] _generatedBytes = [];

    /// <summary>
    /// 阶段加载过程中禁止选择变化重复刷新
    /// </summary>
    private bool _isLoadingStage;

    /// <summary>
    /// 配置列表重建期间禁止选择变化触发配置切换
    /// </summary>
    private bool _isSwitchingConfiguration;

    /// <summary>
    /// 指示当前视图模型是否已经释放
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// 当前订阅参数变化以刷新生成预览的规则
    /// </summary>
    private DataGenerationRule? _observedGenerationRule;

    /// <summary>
    /// 获取供左侧导航显示的阶段集合
    /// </summary>
    public ObservableCollection<StageListItemViewModel> Stages { get; } = [];

    /// <summary>
    /// 获取启动后可通过下拉列表快速切换的配置集合
    /// </summary>
    public ObservableCollection<ConfigurationOptionViewModel> Configurations { get; } = [];

    /// <summary>
    /// 获取本次校验中最多前一百个字节差异
    /// </summary>
    public ObservableCollection<DifferenceItemViewModel> Differences { get; } = [];

    /// <summary>
    /// 获取配置编辑窗口中当前阶段的字段比较区间
    /// </summary>
    public ObservableCollection<ComparisonRange> ComparisonRanges { get; } = [];

    /// <summary>
    /// 获取配置编辑窗口可选择的数据生成方式
    /// </summary>
    public IReadOnlyList<GenerationType> GenerationTypes { get; } = Enum.GetValues<GenerationType>();

    /// <summary>
    /// 获取十六进制数据区域的列标题文本
    /// </summary>
    public string HexColumnHeaderText { get; } = "00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F";

    /// <summary>
    /// 获取配置编辑窗口可选择的数据校验方式
    /// </summary>
    public IReadOnlyList<ValidationMode> ValidationModes { get; } = Enum.GetValues<ValidationMode>();

    /// <summary>
    /// 获取或设置左侧当前选中的阶段
    /// </summary>
    [ObservableProperty]
    private StageListItemViewModel? _selectedStage;

    /// <summary>
    /// 获取或设置配置编辑器当前阶段的数据生成方式
    /// </summary>
    [ObservableProperty]
    private GenerationType _selectedGenerationType = GenerationType.Fixed;

    /// <summary>
    /// 获取或设置配置下拉列表当前选择项
    /// </summary>
    [ObservableProperty]
    private ConfigurationOptionViewModel? _selectedConfiguration;

    /// <summary>
    /// 获取阶段数量说明文本
    /// </summary>
    [ObservableProperty]
    private string _stageCountText = "0 个阶段";

    /// <summary>
    /// 获取当前阶段进度文本
    /// </summary>
    [ObservableProperty]
    private string _stageProgressText = "0 / 0";

    /// <summary>
    /// 获取当前阶段进度百分比
    /// </summary>
    [ObservableProperty]
    private double _stageProgressPercent;

    /// <summary>
    /// 获取当前阶段标题
    /// </summary>
    [ObservableProperty]
    private string _currentStageTitle = "未选择阶段";

    /// <summary>
    /// 获取当前数据生成方式说明
    /// </summary>
    [ObservableProperty]
    private string _generatorTypeText = "未配置";

    /// <summary>
    /// 获取最近生成数据的规范十六进制文本
    /// </summary>
    [ObservableProperty]
    private string _generatedData = string.Empty;

    /// <summary>
    /// 获取生成数据的行偏移标题文本
    /// </summary>
    [ObservableProperty]
    private string _generatedRowHeaders = string.Empty;

    /// <summary>
    /// 获取当前生成数据写入板卡地址的规范十六进制文本
    /// </summary>
    [ObservableProperty]
    private string _generatedDataAddress = string.Empty;

    /// <summary>
    /// 获取生成数据的字节数量文本
    /// </summary>
    [ObservableProperty]
    private string _generatedByteCountText = "0 字节";

    /// <summary>
    /// 获取配置编辑器中当前生成规则的数据预览
    /// </summary>
    [ObservableProperty]
    private string _generatedPreviewData = string.Empty;

    /// <summary>
    /// 获取配置编辑器生成预览的行偏移标题
    /// </summary>
    [ObservableProperty]
    private string _generatedPreviewRowHeaders = string.Empty;

    /// <summary>
    /// 获取配置编辑器生成预览的总长度和截断状态
    /// </summary>
    [ObservableProperty]
    private string _generatedPreviewStatusText = "0 字节";

    /// <summary>
    /// 获取或设置当前阶段的预期返回数据
    /// </summary>
    [ObservableProperty]
    private string _expectedData = string.Empty;

    /// <summary>
    /// 获取预期返回数据的行偏移标题文本
    /// </summary>
    [ObservableProperty]
    private string _expectedRowHeaders = string.Empty;

    /// <summary>
    /// 获取当前预期返回数据对应板卡地址的规范十六进制文本
    /// </summary>
    [ObservableProperty]
    private string _expectedDataAddress = string.Empty;

    /// <summary>
    /// 获取预期数据的字节数量文本
    /// </summary>
    [ObservableProperty]
    private string _expectedByteCountText = "0 字节";

    /// <summary>
    /// 获取或设置用户输入的板卡实际返回数据
    /// </summary>
    [ObservableProperty]
    private string _actualData = string.Empty;

    /// <summary>
    /// 获取实际返回数据的行偏移标题文本
    /// </summary>
    [ObservableProperty]
    private string _actualRowHeaders = string.Empty;

    /// <summary>
    /// 获取实际数据的字节数量或格式状态文本
    /// </summary>
    [ObservableProperty]
    private string _actualByteCountText = "0 字节";

    /// <summary>
    /// 获取当前校验结果的语义状态
    /// </summary>
    [ObservableProperty]
    private ValidationState _validationState = ValidationState.Idle;

    /// <summary>
    /// 获取校验结果图标字符
    /// </summary>
    [ObservableProperty]
    private string _resultIcon = "·";

    /// <summary>
    /// 获取校验结果标题
    /// </summary>
    [ObservableProperty]
    private string _resultTitle = "等待校验";

    /// <summary>
    /// 获取校验结果摘要
    /// </summary>
    [ObservableProperty]
    private string _resultSummary = "粘贴板卡返回数据后点击开始校验";

    /// <summary>
    /// 获取最近一次校验时间文本
    /// </summary>
    [ObservableProperty]
    private string _lastValidationTimeText = "最近校验：尚未校验";

    /// <summary>
    /// 获取当前配置文件名或内置模板名称
    /// </summary>
    [ObservableProperty]
    private string _configFileName = "内置示例配置";

    /// <summary>
    /// 获取底部状态栏消息
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "已加载内置示例配置";

    /// <summary>
    /// 为 Avalonia 设计器创建使用无副作用桌面服务的视图模型
    /// </summary>
    public MainViewModel() : this(new NullDesktopService(), new DataGenerator(),
        new DataValidator(), new TestConfigurationService(),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ByteComparisonTool", "Configurations"), null)
    {
    }

    /// <summary>
    /// 使用应用服务创建主视图模型并载入内置示例配置
    /// </summary>
    /// <param name="desktopService">桌面文件、剪贴板和确认交互服务</param>
    /// <param name="dataGenerator">测试输入数据生成服务</param>
    /// <param name="dataValidator">板卡返回数据校验服务</param>
    /// <param name="configurationService">测试配置加载、保存和校验服务</param>
    /// <param name="configurationDirectory">自动加载和保存 JSON 配置的固定目录</param>
    /// <param name="logger">可选的结构化测试日志写入器</param>
    public MainViewModel(IDesktopService desktopService, DataGenerator dataGenerator,
        DataValidator dataValidator, TestConfigurationService configurationService,
        string configurationDirectory, TestLogger? logger)
    {
        _desktopService = desktopService ?? throw new ArgumentNullException(nameof(desktopService));
        _dataGenerator = dataGenerator ?? throw new ArgumentNullException(nameof(dataGenerator));
        _dataValidator = dataValidator ?? throw new ArgumentNullException(nameof(dataValidator));
        _configurationService = configurationService ??
            throw new ArgumentNullException(nameof(configurationService));
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationDirectory);
        _configurationDirectory = Path.GetFullPath(configurationDirectory);
        _logger = logger;
        _configuration = _configurationService.CreateBuiltInTemplate();
        ConfigurationOptionViewModel builtInOption = new(
            _configuration, "内置示例配置", "内置模板", null);
        Configurations.Add(builtInOption);
        _isSwitchingConfiguration = true;
        SelectedConfiguration = builtInOption;
        _isSwitchingConfiguration = false;
        ApplyConfiguration(_configuration, 0);
    }

    /// <summary>
    /// 创建固定配置目录并加载其中的全部 JSON 配置项
    /// </summary>
    /// <returns>表示首次配置发现和界面初始化操作的任务</returns>
    public async Task InitializeAsync()
    {
        try
        {
            Directory.CreateDirectory(_configurationDirectory);
            string[] filePaths = Directory.GetFiles(_configurationDirectory, "*.json")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (filePaths.Length == 0)
            {
                string defaultPath = Path.Combine(_configurationDirectory,
                    "default-board-tests.json");
                await _configurationService.SaveAsync(defaultPath, _configuration);
                filePaths = [defaultPath];
            }

            List<ConfigurationOptionViewModel> loadedOptions = [];
            int invalidCount = 0;
            foreach (string filePath in filePaths)
            {
                try
                {
                    TestConfiguration configuration =
                        await _configurationService.LoadAsync(filePath);
                    loadedOptions.Add(CreateConfigurationOption(filePath, configuration));
                }
                catch
                {
                    invalidCount++;
                }
            }

            if (loadedOptions.Count == 0)
            {
                string fallbackPath = Path.Combine(_configurationDirectory,
                    "default-board-tests.json");
                await _configurationService.SaveAsync(fallbackPath, _configuration);
                loadedOptions.Add(CreateConfigurationOption(fallbackPath, _configuration));
            }

            _isSwitchingConfiguration = true;
            Configurations.Clear();
            foreach (ConfigurationOptionViewModel option in loadedOptions)
            {
                Configurations.Add(option);
            }

            SelectedConfiguration = Configurations[0];
            _isSwitchingConfiguration = false;
            ApplyConfiguration(SelectedConfiguration.Configuration, 0);
            ConfigFileName = SelectedConfiguration.DisplayName;
            StatusMessage = invalidCount == 0
                ? $"已从固定目录加载 {Configurations.Count} 套配置"
                : $"已加载 {Configurations.Count} 套配置，忽略 {invalidCount} 个无效文件";
        }
        catch (Exception exception)
        {
            _isSwitchingConfiguration = false;
            SetOperationError("自动加载配置失败", exception);
        }
    }

    /// <summary>
    /// 在选择变化后载入对应测试阶段
    /// </summary>
    /// <param name="value">新选择的阶段导航项</param>
    partial void OnSelectedStageChanged(StageListItemViewModel? value)
    {
        if (_isLoadingStage || value is null)
        {
            return;
        }

        LoadStage(value.Index);
    }

    /// <summary>
    /// 在配置编辑器生成方式变化后同步阶段模型和界面摘要
    /// </summary>
    /// <param name="value">用户选择的数据生成方式</param>
    partial void OnSelectedGenerationTypeChanged(GenerationType value)
    {
        if (SelectedStage is null)
        {
            return;
        }

        SelectedStage.Model.Input.Type = value;
    }

    /// <summary>
    /// 在配置下拉选择变化后立即切换配置并载入第一阶段
    /// </summary>
    /// <param name="value">新选择的配置下拉项</param>
    partial void OnSelectedConfigurationChanged(ConfigurationOptionViewModel? value)
    {
        if (_isSwitchingConfiguration || value is null)
        {
            return;
        }

        ApplyConfiguration(value.Configuration, 0);
        ConfigFileName = value.DisplayName;
        StatusMessage = $"已切换配置：{value.DisplayName}";
    }

    /// <summary>
    /// 在预期数据编辑后同步当前阶段并更新字节数量
    /// </summary>
    /// <param name="value">当前预期数据文本</param>
    partial void OnExpectedDataChanged(string value)
    {
        if (!_isLoadingStage && SelectedStage is not null)
        {
            SelectedStage.Model.ExpectedData = value;
        }

        ExpectedByteCountText = GetByteCountText(value);
        ExpectedRowHeaders = GetRowHeaders(value);
    }

    /// <summary>
    /// 在生成数据变化后刷新对应的行偏移标题
    /// </summary>
    /// <param name="value">当前生成数据文本</param>
    partial void OnGeneratedDataChanged(string value)
    {
        GeneratedRowHeaders = GetRowHeaders(value);
    }

    /// <summary>
    /// 在实际数据变化后更新格式状态和字节数量
    /// </summary>
    /// <param name="value">当前实际返回数据文本</param>
    partial void OnActualDataChanged(string value)
    {
        ActualByteCountText = GetByteCountText(value);
        ActualRowHeaders = GetRowHeaders(value);
    }

    /// <summary>
    /// 将用户选择的 JSON 文件导入固定配置目录
    /// </summary>
    /// <returns>表示加载及界面刷新操作的任务</returns>
    [RelayCommand]
    private async Task LoadConfigAsync()
    {
        try
        {
            string? sourcePath = await _desktopService.PickConfigurationToOpenAsync();
            if (string.IsNullOrEmpty(sourcePath))
            {
                StatusMessage = "已取消导入配置";
                return;
            }

            TestConfiguration loaded = await _configurationService.LoadAsync(sourcePath);
            Directory.CreateDirectory(_configurationDirectory);
            string targetPath = CreateImportTargetPath(sourcePath);
            if (!string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(targetPath),
                StringComparison.OrdinalIgnoreCase))
            {
                await _configurationService.SaveAsync(targetPath, loaded);
            }

            ConfigurationOptionViewModel option = AddOrUpdateConfiguration(targetPath, loaded);
            if (ReferenceEquals(SelectedConfiguration, option))
            {
                ApplyConfiguration(loaded, 0);
            }
            else
            {
                SelectedConfiguration = option;
            }
            StatusMessage = $"配置已导入固定目录：{targetPath}";
        }
        catch (Exception exception)
        {
            SetOperationError("导入配置失败", exception);
        }
    }

    /// <summary>
    /// 将当前测试配置保存到固定配置目录
    /// </summary>
    /// <returns>表示配置校验和保存操作的任务</returns>
    [RelayCommand]
    private async Task SaveConfigAsync()
    {
        await SaveCurrentConfigurationAsync();
    }

    /// <summary>
    /// 校验并将当前测试配置保存到固定配置目录
    /// </summary>
    /// <returns>配置验证及文件写入全部成功时为 true，否则为 false</returns>
    public async Task<bool> SaveCurrentConfigurationAsync()
    {
        try
        {
            NormalizeConfigurationBeforeSave();
            Directory.CreateDirectory(_configurationDirectory);
            string filePath = SelectedConfiguration?.FilePath ?? Path.Combine(
                _configurationDirectory, CreateSafeConfigurationFileName(_configuration.Name));
            await _configurationService.SaveAsync(filePath, _configuration);
            ConfigurationOptionViewModel option = AddOrUpdateConfiguration(filePath, _configuration);
            if (!ReferenceEquals(SelectedConfiguration, option))
            {
                _isSwitchingConfiguration = true;
                SelectedConfiguration = option;
                _isSwitchingConfiguration = false;
            }

            StatusMessage = $"配置已保存：{filePath}";
            return true;
        }
        catch (Exception exception)
        {
            SetOperationError("保存配置失败", exception);
            return false;
        }
    }

    /// <summary>
    /// 打开集中配置编辑窗口并在关闭后刷新测试界面
    /// </summary>
    /// <returns>表示配置编辑窗口显示和界面刷新操作的任务</returns>
    [RelayCommand]
    private async Task EditConfigurationAsync()
    {
        if (SelectedConfiguration is null)
        {
            return;
        }

        ConfigurationOptionViewModel originalOption = SelectedConfiguration;
        int originalStageIndex = SelectedStage?.Index ?? 0;
        Dictionary<ConfigurationOptionViewModel, TestConfiguration> snapshots =
            Configurations.ToDictionary(option => option,
                option => CloneConfiguration(option.Configuration));
        bool saved = await _desktopService.EditConfigurationAsync();

        if (saved)
        {
            if (SelectedConfiguration is null)
            {
                return;
            }

            int selectedStageIndex = SelectedStage?.Index ?? 0;
            _configuration = SelectedConfiguration.Configuration;
            RebuildStageItems(Math.Min(selectedStageIndex, _configuration.Stages.Count - 1));
            ConfigFileName = SelectedConfiguration.DisplayName;
            StatusMessage = "配置修改已保存并应用";
            return;
        }

        foreach ((ConfigurationOptionViewModel option, TestConfiguration snapshot) in snapshots)
        {
            option.Configuration = snapshot;
            option.DisplayName = snapshot.Name;
        }

        _isSwitchingConfiguration = true;
        SelectedConfiguration = originalOption;
        _isSwitchingConfiguration = false;
        _configuration = originalOption.Configuration;
        RebuildStageItems(Math.Min(originalStageIndex, _configuration.Stages.Count - 1));
        ConfigFileName = originalOption.DisplayName;
        StatusMessage = "已取消配置修改";
    }

    /// <summary>
    /// 在固定目录创建包含一个默认阶段的新配置并立即切换
    /// </summary>
    /// <returns>表示新配置保存和界面切换操作的任务</returns>
    [RelayCommand]
    private async Task AddConfigurationAsync()
    {
        try
        {
            Directory.CreateDirectory(_configurationDirectory);
            int suffix = 1;
            string configurationName;
            string filePath;
            do
            {
                configurationName = $"新配置 {suffix}";
                filePath = Path.Combine(_configurationDirectory,
                    CreateSafeConfigurationFileName(configurationName));
                suffix++;
            }
            while (File.Exists(filePath) || Configurations.Any(option =>
                string.Equals(option.DisplayName, configurationName,
                    StringComparison.OrdinalIgnoreCase)));

            TestConfiguration configuration = new()
            {
                Name = configurationName,
                Stages =
                [
                    new TestStage
                    {
                        Name = "Stage 1",
                        Input = new DataGenerationRule
                        {
                            Type = GenerationType.Fixed,
                            FixedData = "00 01 02 03"
                        },
                        ExpectedData = "00 01 02 03"
                    }
                ]
            };

            await _configurationService.SaveAsync(filePath, configuration);
            ConfigurationOptionViewModel option = AddOrUpdateConfiguration(filePath, configuration);
            SelectedConfiguration = option;
            StatusMessage = $"已新建配置：{configurationName}";
        }
        catch (Exception exception)
        {
            SetOperationError("新建配置失败", exception);
        }
    }

    /// <summary>
    /// 在当前配置末尾新增并选择一个固定数据阶段
    /// </summary>
    [RelayCommand]
    private void AddStage()
    {
        TestStage stage = new()
        {
            Name = $"Stage {_configuration.Stages.Count + 1}",
            Input = new DataGenerationRule
            {
                Type = GenerationType.Fixed,
                FixedData = "00 01 02 03"
            },
            ExpectedData = "00 01 02 03"
        };
        _configuration.Stages.Add(stage);
        RebuildStageItems(_configuration.Stages.Count - 1);
        StatusMessage = $"已新增阶段：{stage.Name}";
    }

    /// <summary>
    /// 删除当前阶段并选择相邻阶段
    /// </summary>
    [RelayCommand]
    private void DeleteStage()
    {
        if (SelectedStage is null)
        {
            return;
        }

        if (_configuration.Stages.Count == 1)
        {
            StatusMessage = "配置至少需要保留一个测试阶段";
            return;
        }

        int index = SelectedStage.Index;
        string name = SelectedStage.Name;
        _configuration.Stages.RemoveAt(index);
        RebuildStageItems(Math.Min(index, _configuration.Stages.Count - 1));
        StatusMessage = $"已删除阶段：{name}";
    }

    /// <summary>
    /// 深度复制当前阶段并将副本插入原阶段之后
    /// </summary>
    [RelayCommand]
    private void DuplicateStage()
    {
        if (SelectedStage is null)
        {
            return;
        }

        TestStage duplicate = CloneStage(SelectedStage.Model, false);
        string baseName = $"{SelectedStage.Name} - 副本";
        string candidateName = baseName;
        int suffix = 2;
        while (_configuration.Stages.Any(stage =>
            string.Equals(stage.Name, candidateName, StringComparison.OrdinalIgnoreCase)))
        {
            candidateName = $"{baseName} {suffix}";
            suffix++;
        }

        duplicate.Name = candidateName;
        int targetIndex = SelectedStage.Index + 1;
        _configuration.Stages.Insert(targetIndex, duplicate);
        RebuildStageItems(targetIndex);
        StatusMessage = $"已复制阶段：{candidateName}";
    }

    /// <summary>
    /// 为当前阶段添加一个默认字段比较区间
    /// </summary>
    [RelayCommand]
    private void AddComparisonRange()
    {
        if (SelectedStage is null)
        {
            return;
        }

        ComparisonRange range = new()
        {
            Name = $"Field {ComparisonRanges.Count + 1}",
            Offset = 0,
            Length = 1
        };
        SelectedStage.Model.Validation.Ranges.Add(range);
        ComparisonRanges.Add(range);
    }

    /// <summary>
    /// 从当前阶段删除指定字段比较区间
    /// </summary>
    /// <param name="range">需要从校验规则删除的字段比较区间</param>
    [RelayCommand]
    private void DeleteComparisonRange(ComparisonRange? range)
    {
        if (SelectedStage is null || range is null)
        {
            return;
        }

        SelectedStage.Model.Validation.Ranges.Remove(range);
        ComparisonRanges.Remove(range);
    }

    /// <summary>
    /// 将当前阶段向前移动一个位置
    /// </summary>
    [RelayCommand]
    private void MoveStageUp()
    {
        MoveSelectedStage(-1);
    }

    /// <summary>
    /// 将当前阶段向后移动一个位置
    /// </summary>
    [RelayCommand]
    private void MoveStageDown()
    {
        MoveSelectedStage(1);
    }

    /// <summary>
    /// 选择当前阶段之前的测试阶段
    /// </summary>
    [RelayCommand]
    private void PreviousStage()
    {
        if (SelectedStage is not null && SelectedStage.Index > 0)
        {
            SelectStage(SelectedStage.Index - 1);
        }
    }

    /// <summary>
    /// 选择当前阶段之后的测试阶段
    /// </summary>
    [RelayCommand]
    private void NextStage()
    {
        if (SelectedStage is not null && SelectedStage.Index < Stages.Count - 1)
        {
            SelectStage(SelectedStage.Index + 1);
        }
    }

    /// <summary>
    /// 将当前生成数据复制到系统剪贴板
    /// </summary>
    /// <returns>表示剪贴板写入操作的任务</returns>
    [RelayCommand]
    private async Task CopyGeneratedDataAsync()
    {
        await CopyTextAsync(GeneratedData, "生成数据已复制");
    }

    /// <summary>
    /// 按配置编辑器当前参数生成完整数据并复制到系统剪贴板
    /// </summary>
    /// <returns>表示数据生成和剪贴板写入操作的任务</returns>
    [RelayCommand]
    private async Task CopyGeneratedPreviewDataAsync()
    {
        if (SelectedStage is null)
        {
            return;
        }

        try
        {
            byte[] bytes = _dataGenerator.Generate(SelectedStage.Model.Input);
            await CopyTextAsync(HexDataService.Format(bytes),
                $"生成数据已复制，共 {bytes.Length} 字节");
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
        {
            SetOperationError("复制生成数据失败", exception);
        }
    }

    /// <summary>
    /// 将生成数据写入地址复制到系统剪贴板
    /// </summary>
    /// <returns>表示剪贴板写入操作的任务</returns>
    [RelayCommand]
    private async Task CopyGeneratedDataAddressAsync()
    {
        await CopyTextAsync(GeneratedDataAddress, "生成数据地址已复制");
    }

    /// <summary>
    /// 将当前预期数据复制到系统剪贴板
    /// </summary>
    /// <returns>表示剪贴板写入操作的任务</returns>
    [RelayCommand]
    private async Task CopyExpectedDataAsync()
    {
        await CopyTextAsync(ExpectedData, "预期数据已复制");
    }

    /// <summary>
    /// 将预期返回数据地址复制到系统剪贴板
    /// </summary>
    /// <returns>表示剪贴板写入操作的任务</returns>
    [RelayCommand]
    private async Task CopyExpectedDataAddressAsync()
    {
        await CopyTextAsync(ExpectedDataAddress, "预期返回数据地址已复制");
    }

    /// <summary>
    /// 将预期返回数据的纯数字字节数量复制到系统剪贴板
    /// </summary>
    /// <returns>表示字节数量解析和剪贴板写入操作的任务</returns>
    [RelayCommand]
    private async Task CopyExpectedByteCountAsync()
    {
        if (!HexDataService.TryParse(ExpectedData, out byte[] bytes, out string errorMessage))
        {
            StatusMessage = $"无法复制返回数量：{errorMessage}";
            return;
        }

        await CopyTextAsync(bytes.Length.ToString(), $"预期返回数量已复制：{bytes.Length}");
    }

    /// <summary>
    /// 从系统剪贴板读取文本并填入实际返回数据区域
    /// </summary>
    /// <returns>表示剪贴板读取操作的任务</returns>
    [RelayCommand]
    private async Task PasteActualDataAsync()
    {
        try
        {
            string? text = await _desktopService.GetClipboardTextAsync();
            if (text is null)
            {
                StatusMessage = "剪贴板中没有文本";
                return;
            }

            ActualData = text;
            FormatActualData();
        }
        catch (Exception exception)
        {
            SetOperationError("粘贴失败", exception);
        }
    }

    /// <summary>
    /// 将合法实际返回数据规范化为大写空格分隔格式
    /// </summary>
    [RelayCommand]
    private void FormatActualData()
    {
        if (!HexDataService.TryParse(ActualData, out byte[] bytes, out string errorMessage))
        {
            ShowInputError(errorMessage);
            return;
        }

        ActualData = HexDataService.Format(bytes);
        StatusMessage = $"实际返回数据已格式化，共 {bytes.Length} 字节";
    }

    /// <summary>
    /// 清空实际返回数据和最近校验结果
    /// </summary>
    [RelayCommand]
    private void ClearActualData()
    {
        ClearValidationState(true);
        StatusMessage = "已清空实际返回数据";
    }

    /// <summary>
    /// 将配置编辑器中的预期返回数据整理为每行十六字节
    /// </summary>
    [RelayCommand]
    private void FormatExpectedData()
    {
        if (!HexDataService.TryParse(ExpectedData, out byte[] bytes, out string errorMessage))
        {
            StatusMessage = $"预期返回数据格式错误：{errorMessage}";
            return;
        }

        ExpectedData = HexDataService.Format(bytes);
        StatusMessage = $"预期返回数据已格式化，共 {bytes.Length} 字节";
    }

    /// <summary>
    /// 清空当前配置的全部阶段结果并从第一阶段重新开始
    /// </summary>
    [RelayCommand]
    private void RestartValidation()
    {
        foreach (StageListItemViewModel stage in Stages)
        {
            stage.StatusText = "待测试";
        }

        if (Stages.Count > 0)
        {
            LoadStage(0);
        }
        else
        {
            ClearValidationState(true);
        }

        StatusMessage = "已清空全部校验结果，请从第一阶段重新开始";
    }

    /// <summary>
    /// 校验当前实际返回数据、记录日志并在通过后自动进入下一阶段
    /// </summary>
    /// <returns>表示校验后日志写入和阶段切换操作的任务</returns>
    [RelayCommand]
    private async Task ValidateDataAsync()
    {
        if (SelectedStage is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ActualData))
        {
            await RecordInvalidInputAsync("实际返回数据不能为空");
            return;
        }

        if (!HexDataService.TryParse(ActualData, out byte[] actualBytes, out string actualError))
        {
            await RecordInvalidInputAsync(actualError);
            return;
        }

        if (!HexDataService.TryParse(ExpectedData, out byte[] expectedBytes, out string expectedError))
        {
            await RecordInvalidInputAsync($"预期数据格式错误：{expectedError}");
            return;
        }

        ValidationResult result;
        try
        {
            result = _dataValidator.Validate(expectedBytes, actualBytes, SelectedStage.Model.Validation);
        }
        catch (Exception exception)
        {
            await RecordInvalidInputAsync(exception.Message);
            return;
        }

        ActualData = HexDataService.Format(actualBytes);
        ExpectedData = HexDataService.Format(expectedBytes);
        ShowValidationResult(result);
        SelectedStage.StatusText = result.IsSuccess ? "通过" : "失败";
        await AppendLogAsync(result.IsSuccess, result.Message, result.Differences);

        if (!result.IsSuccess)
        {
            return;
        }

        if (SelectedStage.Index >= Stages.Count - 1)
        {
            StatusMessage = "全部测试阶段已完成";
            ResultSummary = $"{result.Message}，全部测试阶段已完成";
            return;
        }

        string completedStageName = SelectedStage.Name;
        SelectStage(SelectedStage.Index + 1);
        StatusMessage = $"{completedStageName} 校验通过，已自动进入 {SelectedStage?.Name}";
    }

    /// <summary>
    /// 释放日志写入器及其同步资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ObserveGenerationRule(null);
        _logger?.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// 应用一套完整配置并选择指定阶段
    /// </summary>
    /// <param name="configuration">需要显示和执行的配置</param>
    /// <param name="selectedIndex">加载后选择的阶段索引</param>
    private void ApplyConfiguration(TestConfiguration configuration, int selectedIndex)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        RebuildStageItems(selectedIndex);
    }

    /// <summary>
    /// 按文件路径新增配置下拉项或更新已经存在的配置下拉项
    /// </summary>
    /// <param name="filePath">配置对应的本地 JSON 文件路径</param>
    /// <param name="configuration">已经加载或保存的测试配置</param>
    /// <returns>可直接选择的新增或更新后配置下拉项</returns>
    private ConfigurationOptionViewModel AddOrUpdateConfiguration(string filePath,
        TestConfiguration configuration)
    {
        string fullPath = Path.GetFullPath(filePath);
        ConfigurationOptionViewModel? existing = Configurations.FirstOrDefault(option =>
            string.Equals(option.FilePath, fullPath, StringComparison.OrdinalIgnoreCase));
        string displayName = string.IsNullOrWhiteSpace(configuration.Name)
            ? Path.GetFileNameWithoutExtension(fullPath)
            : configuration.Name;

        if (existing is not null)
        {
            existing.Configuration = configuration;
            existing.DisplayName = displayName;
            existing.SourceText = Path.GetFileName(fullPath);
            existing.FilePath = fullPath;
            ConfigFileName = existing.DisplayName;
            return existing;
        }

        ConfigurationOptionViewModel option = new(configuration, displayName,
            Path.GetFileName(fullPath), fullPath);
        Configurations.Add(option);
        ConfigFileName = option.DisplayName;
        return option;
    }

    /// <summary>
    /// 根据文件和配置内容创建配置下拉项
    /// </summary>
    /// <param name="filePath">配置对应的本地 JSON 文件路径</param>
    /// <param name="configuration">已经完成校验的测试配置</param>
    /// <returns>包含显示名称、文件来源和配置模型的下拉项</returns>
    private static ConfigurationOptionViewModel CreateConfigurationOption(string filePath,
        TestConfiguration configuration)
    {
        string fullPath = Path.GetFullPath(filePath);
        string displayName = string.IsNullOrWhiteSpace(configuration.Name)
            ? Path.GetFileNameWithoutExtension(fullPath)
            : configuration.Name;
        return new ConfigurationOptionViewModel(configuration, displayName,
            Path.GetFileName(fullPath), fullPath);
    }

    /// <summary>
    /// 深度复制阶段生成规则、预期数据和校验字段并生成新标识
    /// </summary>
    /// <param name="source">需要复制的源测试阶段</param>
    /// <param name="preserveId">是否保留源阶段标识，编辑快照需要保留标识</param>
    /// <returns>具有独立嵌套模型和新稳定标识的测试阶段副本</returns>
    private static TestStage CloneStage(TestStage source, bool preserveId)
    {
        return new TestStage
        {
            Id = preserveId ? source.Id : Guid.NewGuid().ToString("N"),
            Name = source.Name,
            Input = new DataGenerationRule
            {
                Type = source.Input.Type,
                FixedData = source.Input.FixedData,
                Length = source.Input.Length,
                StartValue = source.Input.StartValue,
                Seed = source.Input.Seed,
                RepeatedData = source.Input.RepeatedData,
                RepeatCount = source.Input.RepeatCount,
                ByteLimit = source.Input.ByteLimit
            },
            GeneratedDataAddress = source.GeneratedDataAddress,
            ExpectedData = source.ExpectedData,
            ExpectedDataAddress = source.ExpectedDataAddress,
            Validation = new ValidationRule
            {
                Mode = source.Validation.Mode,
                RequireLengthMatch = source.Validation.RequireLengthMatch,
                Ranges = source.Validation.Ranges.Select(range => new ComparisonRange
                {
                    Name = range.Name,
                    Offset = range.Offset,
                    Length = range.Length
                }).ToList()
            }
        };
    }

    /// <summary>
    /// 深度复制一套配置及其全部阶段和嵌套规则
    /// </summary>
    /// <param name="source">需要建立编辑快照的源配置</param>
    /// <returns>可在取消编辑时独立恢复的配置副本</returns>
    private static TestConfiguration CloneConfiguration(TestConfiguration source)
    {
        return new TestConfiguration
        {
            Name = source.Name,
            Version = source.Version,
            Stages = source.Stages.Select(stage => CloneStage(stage, true)).ToList()
        };
    }

    /// <summary>
    /// 清理名称和十六进制文本并在保存前执行完整配置校验
    /// </summary>
    private void NormalizeConfigurationBeforeSave()
    {
        _configuration.Name = _configuration.Name.Trim();
        foreach (TestStage stage in _configuration.Stages)
        {
            stage.Name = stage.Name.Trim();
            if (stage.Input.Type == GenerationType.Fixed)
            {
                stage.Input.FixedData = HexDataService.Format(
                    HexDataService.Parse(stage.Input.FixedData));
            }
            else if (stage.Input.Type == GenerationType.Repeated)
            {
                stage.Input.RepeatedData = HexDataService.Format(
                    HexDataService.Parse(stage.Input.RepeatedData));
            }

            stage.GeneratedDataAddress = HexDataService.Format(
                HexDataService.Parse(stage.GeneratedDataAddress));
            stage.ExpectedData = HexDataService.Format(HexDataService.Parse(stage.ExpectedData));
            stage.ExpectedDataAddress = HexDataService.Format(
                HexDataService.Parse(stage.ExpectedDataAddress));
        }

        _configurationService.Validate(_configuration);
    }

    /// <summary>
    /// 将配置名称转换为固定目录中可用的 JSON 文件名
    /// </summary>
    /// <param name="configurationName">当前配置显示名称</param>
    /// <returns>不包含非法文件名字符且以 json 结尾的文件名</returns>
    private static string CreateSafeConfigurationFileName(string? configurationName)
    {
        string source = string.IsNullOrWhiteSpace(configurationName)
            ? "board-tests"
            : configurationName.Trim();
        HashSet<char> invalidCharacters = [.. Path.GetInvalidFileNameChars()];
        string safeName = new(source.Select(character =>
            invalidCharacters.Contains(character) ? '_' : character).ToArray());
        return safeName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? safeName
            : $"{safeName}.json";
    }

    /// <summary>
    /// 为导入配置选择不会静默覆盖已有文件的固定目录路径
    /// </summary>
    /// <param name="sourcePath">用户选择的源配置文件路径</param>
    /// <returns>源文件已位于固定目录时返回原路径，否则返回唯一目标路径</returns>
    private string CreateImportTargetPath(string sourcePath)
    {
        string fullSourcePath = Path.GetFullPath(sourcePath);
        string initialPath = Path.Combine(_configurationDirectory, Path.GetFileName(sourcePath));
        if (string.Equals(fullSourcePath, Path.GetFullPath(initialPath),
            StringComparison.OrdinalIgnoreCase) || !File.Exists(initialPath))
        {
            return initialPath;
        }

        string baseName = Path.GetFileNameWithoutExtension(initialPath);
        string extension = Path.GetExtension(initialPath);
        int suffix = 2;
        string candidatePath;
        do
        {
            candidatePath = Path.Combine(_configurationDirectory,
                $"{baseName} ({suffix}){extension}");
            suffix++;
        }
        while (File.Exists(candidatePath));

        return candidatePath;
    }

    /// <summary>
    /// 根据当前配置重建左侧阶段导航集合
    /// </summary>
    /// <param name="selectedIndex">重建后选择的阶段索引</param>
    private void RebuildStageItems(int selectedIndex)
    {
        ObserveGenerationRule(null);
        _isLoadingStage = true;
        try
        {
            Stages.Clear();
            for (int index = 0; index < _configuration.Stages.Count; index++)
            {
                Stages.Add(new StageListItemViewModel(_configuration.Stages[index], index));
            }

            StageCountText = $"{Stages.Count} 个阶段";
            if (Stages.Count == 0)
            {
                SelectedStage = null;
                SelectedGenerationType = GenerationType.Fixed;
                StageProgressText = "0 / 0";
                StageProgressPercent = 0;
                CurrentStageTitle = "未选择阶段";
                GeneratorTypeText = "未配置";
                GeneratedData = string.Empty;
                GeneratedRowHeaders = string.Empty;
                GeneratedByteCountText = "0 字节";
                GeneratedPreviewData = string.Empty;
                GeneratedPreviewRowHeaders = string.Empty;
                GeneratedPreviewStatusText = "0 字节";
                GeneratedDataAddress = string.Empty;
                ExpectedData = string.Empty;
                ExpectedRowHeaders = string.Empty;
                ExpectedByteCountText = "0 字节";
                ExpectedDataAddress = string.Empty;
                ComparisonRanges.Clear();
                ClearValidationState(true);
                return;
            }

            StageListItemViewModel stageToLoad =
                Stages[Math.Clamp(selectedIndex, 0, Stages.Count - 1)];
            SelectedStage = stageToLoad;
        }
        finally
        {
            _isLoadingStage = false;
        }

        LoadStage(Stages[Math.Clamp(selectedIndex, 0, Stages.Count - 1)].Index);
    }

    /// <summary>
    /// 选择并载入指定索引的阶段
    /// </summary>
    /// <param name="index">阶段在当前配置中的零基索引</param>
    private void SelectStage(int index)
    {
        if (index < 0 || index >= Stages.Count)
        {
            return;
        }

        SelectedStage = Stages[index];
    }

    /// <summary>
    /// 将指定阶段载入数据编辑和校验区域
    /// </summary>
    /// <param name="index">阶段在当前配置中的零基索引</param>
    private void LoadStage(int index)
    {
        if (index < 0 || index >= Stages.Count)
        {
            return;
        }

        StageListItemViewModel item = Stages[index];
        ObserveGenerationRule(item.Model.Input);
        _isLoadingStage = true;
        try
        {
            if (!ReferenceEquals(SelectedStage, item))
            {
                SelectedStage = item;
            }

            CurrentStageTitle = item.Name;
            SelectedGenerationType = item.Model.Input.Type;
            GeneratorTypeText = GetGeneratorTypeText(item.Model.Input);
            GeneratedDataAddress = NormalizeConfiguredHex(item.Model.GeneratedDataAddress);
            item.Model.GeneratedDataAddress = GeneratedDataAddress;
            ExpectedData = NormalizeConfiguredHex(item.Model.ExpectedData);
            item.Model.ExpectedData = ExpectedData;
            ExpectedDataAddress = NormalizeConfiguredHex(item.Model.ExpectedDataAddress);
            item.Model.ExpectedDataAddress = ExpectedDataAddress;
            ComparisonRanges.Clear();
            foreach (ComparisonRange range in item.Model.Validation.Ranges)
            {
                ComparisonRanges.Add(range);
            }

            StageProgressText = $"{index + 1} / {Stages.Count}";
            StageProgressPercent = (index + 1d) / Stages.Count * 100d;
            _generatedBytes = _dataGenerator.Generate(item.Model);
            GeneratedData = HexDataService.Format(_generatedBytes);
            GeneratedByteCountText = $"{_generatedBytes.Length} 字节";
            UpdateGeneratedDataPreview(_generatedBytes);
            ClearValidationState(true);
            StatusMessage = $"当前阶段：{item.Name}";
        }
        catch (Exception exception)
        {
            SetOperationError("载入阶段失败", exception);
        }
        finally
        {
            _isLoadingStage = false;
        }
    }

    /// <summary>
    /// 将当前选中阶段按指定方向移动一位
    /// </summary>
    /// <param name="direction">负一表示上移，正一表示下移</param>
    private void MoveSelectedStage(int direction)
    {
        if (SelectedStage is null)
        {
            return;
        }

        int sourceIndex = SelectedStage.Index;
        int targetIndex = sourceIndex + direction;
        if (targetIndex < 0 || targetIndex >= _configuration.Stages.Count)
        {
            StatusMessage = direction < 0 ? "当前阶段已经位于首位" : "当前阶段已经位于末位";
            return;
        }

        TestStage stage = _configuration.Stages[sourceIndex];
        _configuration.Stages.RemoveAt(sourceIndex);
        _configuration.Stages.Insert(targetIndex, stage);
        RebuildStageItems(targetIndex);
        StatusMessage = $"已调整阶段顺序：{stage.Name}";
    }

    /// <summary>
    /// 清除实际输入并将校验展示恢复为等待状态
    /// </summary>
    /// <param name="clearActualData">是否同时清空实际返回数据</param>
    private void ClearValidationState(bool clearActualData)
    {
        if (clearActualData)
        {
            ActualData = string.Empty;
        }

        Differences.Clear();
        ValidationState = ValidationState.Idle;
        ResultIcon = "·";
        ResultTitle = "等待校验";
        ResultSummary = "粘贴板卡返回数据后点击开始校验";
        LastValidationTimeText = "最近校验：尚未校验";
    }

    /// <summary>
    /// 切换当前监听的生成规则并管理属性变化订阅
    /// </summary>
    /// <param name="rule">需要监听的当前阶段生成规则，传入空值表示取消监听</param>
    private void ObserveGenerationRule(DataGenerationRule? rule)
    {
        if (ReferenceEquals(_observedGenerationRule, rule))
        {
            return;
        }

        if (_observedGenerationRule is not null)
        {
            _observedGenerationRule.PropertyChanged -= GenerationRule_OnPropertyChanged;
        }

        _observedGenerationRule = rule;
        if (_observedGenerationRule is not null)
        {
            _observedGenerationRule.PropertyChanged += GenerationRule_OnPropertyChanged;
        }
    }

    /// <summary>
    /// 在配置编辑器修改任一生成参数后刷新规则摘要和数据预览
    /// </summary>
    /// <param name="sender">发生变化的当前阶段生成规则</param>
    /// <param name="e">包含变化属性名称的通知参数</param>
    private void GenerationRule_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoadingStage || SelectedStage is null ||
            !ReferenceEquals(sender, SelectedStage.Model.Input))
        {
            return;
        }

        SelectedStage.RefreshDescription();
        GeneratorTypeText = GetGeneratorTypeText(SelectedStage.Model.Input);
        RefreshGeneratedDataPreview();
    }

    /// <summary>
    /// 使用当前阶段规则重新生成配置编辑器中的实时数据预览
    /// </summary>
    private void RefreshGeneratedDataPreview()
    {
        if (SelectedStage is null)
        {
            GeneratedPreviewData = string.Empty;
            GeneratedPreviewRowHeaders = string.Empty;
            GeneratedPreviewStatusText = "0 字节";
            return;
        }

        try
        {
            byte[] generatedBytes = _dataGenerator.Generate(SelectedStage.Model.Input);
            UpdateGeneratedDataPreview(generatedBytes);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException or OverflowException)
        {
            GeneratedPreviewData = string.Empty;
            GeneratedPreviewRowHeaders = string.Empty;
            GeneratedPreviewStatusText = $"预览不可用 · {exception.Message}";
        }
    }

    /// <summary>
    /// 将生成结果转换为带长度和截断说明的编辑器预览文本
    /// </summary>
    /// <param name="generatedBytes">当前规则生成的完整字节数据</param>
    private void UpdateGeneratedDataPreview(byte[] generatedBytes)
    {
        int previewLength = Math.Min(generatedBytes.Length, GeneratedPreviewByteLimit);
        GeneratedPreviewData = HexDataService.Format(generatedBytes.AsSpan(0, previewLength).ToArray());
        GeneratedPreviewRowHeaders = GetRowHeaders(GeneratedPreviewData);
        GeneratedPreviewStatusText = generatedBytes.Length > previewLength
            ? $"共 {generatedBytes.Length} 字节 · 显示前 {previewLength} 字节"
            : $"{generatedBytes.Length} 字节";
    }

    /// <summary>
    /// 将校验服务结果转换为界面状态和差异项
    /// </summary>
    /// <param name="result">完整字节校验结果</param>
    private void ShowValidationResult(ValidationResult result)
    {
        Differences.Clear();
        foreach (ByteDifference difference in result.Differences.Take(100))
        {
            Differences.Add(new DifferenceItemViewModel(difference));
        }

        ValidationState = result.IsSuccess ? ValidationState.Success : ValidationState.Failure;
        ResultIcon = result.IsSuccess ? "✓" : "×";
        ResultTitle = result.IsSuccess ? "校验通过" : "校验失败";
        ResultSummary = result.Differences.Count > 100
            ? $"{result.Message}；界面仅显示前 100 处差异"
            : result.Message;
        LastValidationTimeText = $"最近校验：{DateTime.Now:HH:mm:ss}";
        StatusMessage = result.IsSuccess ? "当前阶段校验通过" : "当前阶段校验失败";
    }

    /// <summary>
    /// 显示非法十六进制输入错误
    /// </summary>
    /// <param name="message">具体格式错误原因</param>
    private void ShowInputError(string message)
    {
        Differences.Clear();
        ValidationState = ValidationState.Failure;
        ResultIcon = "!";
        ResultTitle = "输入格式错误";
        ResultSummary = message;
        LastValidationTimeText = $"最近校验：{DateTime.Now:HH:mm:ss}";
        StatusMessage = $"输入格式错误：{message}";
    }

    /// <summary>
    /// 展示并记录一次未进入字节比较的非法输入
    /// </summary>
    /// <param name="message">阻止校验的输入错误</param>
    /// <returns>表示日志追加操作的任务</returns>
    private async Task RecordInvalidInputAsync(string message)
    {
        ShowInputError(message);
        if (SelectedStage is not null)
        {
            SelectedStage.StatusText = "失败";
        }

        await AppendLogAsync(false, $"输入格式错误：{message}", []);
    }

    /// <summary>
    /// 追加当前阶段的一次结构化测试日志
    /// </summary>
    /// <param name="isSuccess">校验是否通过</param>
    /// <param name="message">校验或输入错误摘要</param>
    /// <param name="differences">校验发现的字节差异</param>
    /// <returns>表示日志追加操作的任务</returns>
    private async Task AppendLogAsync(bool isSuccess, string message,
        IReadOnlyList<ByteDifference> differences)
    {
        if (_logger is null || SelectedStage is null)
        {
            return;
        }

        try
        {
            await _logger.AppendAsync(new TestLogEntry
            {
                Timestamp = DateTimeOffset.Now,
                StageName = SelectedStage.Name,
                GeneratedData = GeneratedData,
                GeneratedDataAddress = GeneratedDataAddress,
                ExpectedData = ExpectedData,
                ExpectedDataAddress = ExpectedDataAddress,
                ActualData = ActualData,
                IsSuccess = isSuccess,
                ResultMessage = message,
                Differences = [.. differences]
            });
        }
        catch (Exception exception)
        {
            StatusMessage = $"校验已完成，但日志写入失败：{exception.Message}";
        }
    }

    /// <summary>
    /// 将指定文本复制到剪贴板并更新操作状态
    /// </summary>
    /// <param name="text">需要复制的文本</param>
    /// <param name="successMessage">复制成功后显示的状态消息</param>
    /// <returns>表示剪贴板写入操作的任务</returns>
    private async Task CopyTextAsync(string text, string successMessage)
    {
        try
        {
            await _desktopService.CopyTextAsync(text);
            StatusMessage = successMessage;
        }
        catch (Exception exception)
        {
            SetOperationError("复制失败", exception);
        }
    }

    /// <summary>
    /// 将操作异常转换为非阻塞界面状态
    /// </summary>
    /// <param name="operation">失败的操作名称</param>
    /// <param name="exception">包含失败原因的异常</param>
    private void SetOperationError(string operation, Exception exception)
    {
        ValidationState = ValidationState.Failure;
        ResultIcon = "!";
        ResultTitle = operation;
        ResultSummary = exception.Message;
        StatusMessage = $"{operation}：{exception.Message}";
    }

    /// <summary>
    /// 将生成规则转换为简短中文说明
    /// </summary>
    /// <param name="rule">当前阶段的数据生成规则</param>
    /// <returns>固定、递增、随机或重复数据说明</returns>
    private static string GetGeneratorTypeText(DataGenerationRule rule)
    {
        return rule.Type switch
        {
            GenerationType.Fixed => "固定数据",
            GenerationType.Incrementing => $"递增 · {rule.Length} 字节",
            GenerationType.Random => $"随机 · {rule.Length} 字节 · 种子 {rule.Seed}",
            GenerationType.Repeated => $"重复 · {rule.RepeatCount} 次 · 上限 {rule.ByteLimit} 字节",
            _ => "未知生成方式"
        };
    }

    /// <summary>
    /// 计算十六进制文本的字节数量展示
    /// </summary>
    /// <param name="text">需要解析的十六进制文本</param>
    /// <returns>字节数量或格式错误文本</returns>
    private static string GetByteCountText(string? text)
    {
        return HexDataService.TryParse(text, out byte[] bytes, out _)
            ? $"{bytes.Length} 字节"
            : "格式错误";
    }

    /// <summary>
    /// 根据十六进制文本的实际换行生成按十六字节递增的行偏移标题
    /// </summary>
    /// <param name="text">需要生成行标题的十六进制文本</param>
    /// <returns>以换行分隔的四位十六进制行偏移文本</returns>
    private static string GetRowHeaders(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        int rowCount = text.Replace("\r\n", "\n").Split('\n').Length;

        return string.Join(Environment.NewLine,
            Enumerable.Range(0, rowCount).Select(row => (row * 16).ToString("X4")));
    }

    /// <summary>
    /// 在载入阶段时规范化配置中的合法十六进制文本
    /// </summary>
    /// <param name="text">配置中的十六进制文本</param>
    /// <returns>规范格式文本，解析失败时保留原文本</returns>
    private static string NormalizeConfiguredHex(string? text)
    {
        return HexDataService.TryParse(text, out byte[] bytes, out _)
            ? HexDataService.Format(bytes)
            : text ?? string.Empty;
    }
}
