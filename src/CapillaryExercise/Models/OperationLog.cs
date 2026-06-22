namespace CapillaryExercise.Models;

/// <summary>
/// 操作日志，对应数据库 OperationLog 表的一行记录。
/// 记录每次领料（及失败原因），供追溯使用。
/// </summary>
public class OperationLog
{
    /// <summary>主键（自增）。</summary>
    public int Id { get; set; }

    /// <summary>操作类型，本期固定为 "Pickup"。</summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>劈刀条码。</summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>劈刀类型。</summary>
    public string CapillaryType { get; set; } = string.Empty;

    /// <summary>仓位面（A/B/C）。</summary>
    public string Face { get; set; } = string.Empty;

    /// <summary>仓位 X 坐标。</summary>
    public int PosX { get; set; }

    /// <summary>仓位 Y 坐标。</summary>
    public int PosY { get; set; }

    /// <summary>工单号。</summary>
    public string WorkOrder { get; set; } = string.Empty;

    /// <summary>机台号。</summary>
    public string MachineNo { get; set; } = string.Empty;

    /// <summary>结果："Success" / "Fail"。</summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>失败原因或备注。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>操作时间戳。</summary>
    public DateTime Timestamp { get; set; }
}
