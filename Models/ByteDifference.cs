namespace ByteComparisonTool.Models;

/// <summary>
/// 表示一个字节位置上的预期值与实际值差异
/// </summary>
/// <param name="Offset">从零开始的字节偏移</param>
/// <param name="Expected">预期字节，超出预期数据长度时为空</param>
/// <param name="Actual">实际字节，超出实际数据长度时为空</param>
public sealed record ByteDifference(int Offset, byte? Expected, byte? Actual);
