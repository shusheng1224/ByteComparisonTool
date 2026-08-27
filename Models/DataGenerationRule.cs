using CommunityToolkit.Mvvm.ComponentModel;

namespace ByteComparisonTool.Models;

/// <summary>
/// 描述一个测试阶段如何生成待发送数据
/// </summary>
public sealed partial class DataGenerationRule : ObservableObject
{
    /// <summary>
    /// 获取或设置数据生成方式
    /// </summary>
    [ObservableProperty]
    private GenerationType _type = GenerationType.Fixed;

    /// <summary>
    /// 获取或设置固定数据生成方式使用的十六进制文本
    /// </summary>
    [ObservableProperty]
    private string _fixedData = string.Empty;

    /// <summary>
    /// 获取或设置递增或随机数据的字节长度
    /// </summary>
    [ObservableProperty]
    private int _length = 256;

    /// <summary>
    /// 获取或设置递增数据的起始字节
    /// </summary>
    [ObservableProperty]
    private byte _startValue;

    /// <summary>
    /// 获取或设置用于产生可重复随机数据的固定种子
    /// </summary>
    [ObservableProperty]
    private int? _seed = 0;

    /// <summary>
    /// 获取或设置重复数据生成方式使用的十六进制字节模式
    /// </summary>
    [ObservableProperty]
    private string _repeatedData = "00";

    /// <summary>
    /// 获取或设置字节模式的最大重复次数
    /// </summary>
    [ObservableProperty]
    private int _repeatCount = 1;

    /// <summary>
    /// 获取或设置重复数据生成结果允许的最大字节数量
    /// </summary>
    [ObservableProperty]
    private int _byteLimit = 1024;
}
