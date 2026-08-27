# 测试配置说明

ByteComparisonTool 使用 JSON 文件描述一套按顺序执行的板卡测试阶段。程序启动时会自动加载固定配置目录中的全部 `*.json` 文件。

示例文件：[board-tests.example.json](board-tests.example.json)

## 顶层结构

```json
{
  "name": "板卡基础回归测试",
  "version": 1,
  "stages": []
}
```

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `name` | string | 配置显示名称 |
| `version` | integer | 配置格式版本，当前为 `1` |
| `stages` | array | 按执行顺序排列的测试阶段 |

## 阶段结构

```json
{
  "id": "basic-handshake",
  "name": "Stage 1 - 握手",
  "input": {},
  "generatedDataAddress": "00 00 10 00",
  "expectedData": "AA BB CC DD EE",
  "expectedDataAddress": "00 00 20 00",
  "validation": {}
}
```

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `id` | string | 阶段稳定标识；复制阶段时程序会生成新标识 |
| `name` | string | 阶段显示名称 |
| `input` | object | 生成数据规则 |
| `generatedDataAddress` | string | 生成数据写入地址，必须恰好为 4 字节 |
| `expectedData` | string | 预期返回的十六进制数据 |
| `expectedDataAddress` | string | 预期数据读取地址，必须恰好为 4 字节 |
| `validation` | object | 返回数据校验规则 |

十六进制字段支持空格、换行、逗号、分号、连字符、冒号、下划线和 `0x` 前缀。保存配置时程序会统一格式化为大写、空格分隔且每行最多 16 字节。

## 数据生成方式

### 固定数据 `fixed`

直接使用 `fixedData` 中的字节：

```json
{
  "type": "fixed",
  "fixedData": "00 FF 12 34"
}
```

### 递增数据 `incrementing`

从 `startValue` 开始逐字节递增，超过 `FF` 后回绕到 `00`：

```json
{
  "type": "incrementing",
  "length": 256,
  "startValue": 0
}
```

### 固定种子随机数据 `random`

使用固定种子生成可重复的伪随机数据。同一 `seed` 和 `length` 会得到相同结果：

```json
{
  "type": "random",
  "length": 256,
  "seed": 20260827
}
```

随机生成必须设置 `seed`。

### 重复数据 `repeated`

重复指定字节模式，结果长度取“模式长度 × 重复次数”和字节上限中的较小值：

```json
{
  "type": "repeated",
  "repeatedData": "AA 55",
  "repeatCount": 8,
  "byteLimit": 12
}
```

上例生成 12 字节：

```text
AA 55 AA 55 AA 55 AA 55 AA 55 AA 55
```

## 校验方式

### 全量比较 `full`

按顺序比较全部字节：

```json
{
  "mode": "full",
  "requireLengthMatch": true,
  "ranges": []
}
```

### 指定区间比较 `ranges`

只比较配置的零基字节区间：

```json
{
  "mode": "ranges",
  "requireLengthMatch": true,
  "ranges": [
    {
      "name": "Header",
      "offset": 0,
      "length": 2
    },
    {
      "name": "Payload",
      "offset": 4,
      "length": 4
    }
  ]
}
```

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `mode` | string | `full` 或 `ranges` |
| `requireLengthMatch` | boolean | 是否要求实际数据总长度与预期数据一致 |
| `ranges` | array | `ranges` 模式下需要比较的字段区间 |
| `ranges[].name` | string | 字段显示名称 |
| `ranges[].offset` | integer | 从 0 开始的字节偏移 |
| `ranges[].length` | integer | 需要比较的字节数量 |

## 配置管理建议

- 每套板卡版本或测试场景使用独立配置文件
- 阶段名称应描述测试目的，而不是只使用序号
- 随机数据始终使用固定种子，确保问题可以复现
- 地址统一写成 4 字节，例如 `00 00 20 00`
- 在配置编辑器中保存前检查生成数据和预期数据的行列预览
- 修改重要配置前备份 `%LocalAppData%\ByteComparisonTool\Configurations`
