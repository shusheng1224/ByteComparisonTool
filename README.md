# ByteComparisonTool

板卡数据校验测试工具，用于验证上位机生成的数据发送到板卡后，板卡返回结果是否符合预期。

项目基于 Avalonia 和 .NET 10 开发，当前版本面向调试阶段的人工测试流程：工具负责生成输入、展示预期结果、比较实际返回数据并记录日志，不直接连接或控制板卡。

## 主要功能

- 多配置管理：启动时自动加载固定目录中的全部 JSON 配置，可快速切换、新增和编辑配置
- 多阶段测试：支持阶段新增、复制、删除、排序以及通过后自动进入下一阶段
- 数据生成：支持固定、递增、固定种子随机和重复数据四种生成方式
- 十六进制查看：每行固定 16 字节，带 `00–0F` 列头和行偏移
- 地址信息：生成数据地址和预期返回地址均为 4 字节，支持快捷复制
- 返回数量：可复制预期返回数据的纯数字字节数，便于设置板卡读取长度
- 数据校验：支持长度检查、全量比较和指定区间比较
- 差异分析：显示差异偏移、期望字节和实际字节
- 测试重置：可清空当前配置的全部通过/失败状态并从第一阶段重新开始
- 测试日志：记录阶段、生成数据、地址、预期数据、实际数据和校验结果
- 窗口状态：记住上次关闭时的位置、尺寸和最大化状态
- 响应式布局：支持完整布局和紧凑布局

## 界面布局

主界面包含三个主要数据区域：

- 左侧：当前阶段的原始生成数据
- 右上：预期返回数据、返回地址、返回数量和校验规则
- 右下：实际返回数据输入与校验操作

配置编辑器集中管理配置名称、阶段名称、生成规则、地址、预期数据和校验规则，并实时显示当前参数对应的生成数据。

## 快速开始

### 环境要求

- .NET 10 SDK
- Windows、Linux 或 macOS（当前主要在 Windows 上验证）

### 运行

```powershell
dotnet restore
dotnet run --project ByteComparisonTool.csproj
```

### 测试

```powershell
dotnet test ByteComparisonTool.Tests/ByteComparisonTool.Tests.csproj
```

### 发布 Windows 版本

```powershell
dotnet publish ByteComparisonTool.csproj -c Release -r win-x64 --self-contained true
```

发布文件默认位于：

```text
bin/Release/net10.0/win-x64/publish/
```

## 使用流程

1. 从顶部下拉框选择测试配置
2. 选择需要执行的测试阶段
3. 复制生成数据和写入地址，将数据发送给板卡
4. 按预期返回地址和返回数量读取板卡数据
5. 将板卡返回的十六进制数据粘贴到“实际返回数据”区域
6. 点击“重新校验”查看校验结果和差异表
7. 校验通过后自动进入下一阶段
8. 需要重新执行整套测试时点击“重新开始”

## 配置文件

程序启动时会加载以下目录中的全部 `*.json`：

```text
%LocalAppData%\ByteComparisonTool\Configurations
```

首次运行且目录为空时，程序会自动创建默认配置。配置编辑器中的保存操作会将修改写回对应文件。

仓库中的示例配置位于 [Samples/board-tests.example.json](Samples/board-tests.example.json)，字段说明见 [Samples/README.md](Samples/README.md)。

## 本地数据目录

Windows 默认使用：

```text
%LocalAppData%\ByteComparisonTool\
├── Configurations\       # 测试配置
├── Logs\                 # JSON Lines 测试日志
├── Backups\              # 手工创建的配置备份
└── window-placement.json # 主窗口位置和尺寸
```

这些运行时数据不会提交到 Git 仓库。

## 项目结构

```text
ByteComparisonTool
├── Views/                 # Avalonia 主界面、配置编辑器和确认窗口
├── ViewModels/            # 页面状态、命令和测试流程编排
├── Models/                # 配置、阶段、生成和校验数据模型
├── Services/              # 数据生成、校验、配置、日志和窗口状态服务
├── Infrastructure/        # 桌面文件、剪贴板和对话框抽象
├── Converters/            # Avalonia 绑定转换器
├── Samples/               # 示例测试配置及说明
└── ByteComparisonTool.Tests/ # 核心服务和回归测试
```

## 当前边界

当前版本暂不包含：

- 串口、USB 或网络硬件连接
- 自动发送生成数据
- 自动读取板卡返回数据
- 无人值守执行全部测试阶段

这些能力可以在后续版本中接入现有的阶段管理、数据生成和校验服务。

## 许可证

仓库当前未声明开源许可证。未经许可，不代表允许复制、修改或分发。
