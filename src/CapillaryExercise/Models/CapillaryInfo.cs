namespace CapillaryExercise.Models;

/// <summary>
/// 劈刀信息，对应数据库 CapillaryInfo 表的一行记录。
/// </summary>
public class CapillaryInfo
{
    /// <summary>主键（自增）。</summary>
    public int Id { get; set; }

    /// <summary>条码，全局唯一。</summary>
    public string Barcode { get; set; } = string.Empty;

    /// <summary>劈刀类型。</summary>
    public string CapillaryType { get; set; } = string.Empty;

    /// <summary>仓位面（A/B/C）。</summary>
    public string Face { get; set; } = string.Empty;

    /// <summary>仓位 X 坐标（1-18）。</summary>
    public int PosX { get; set; }

    /// <summary>仓位 Y 坐标（1-18）。</summary>
    public int PosY { get; set; }

    /// <summary>入库时间，作为 FIFO 领料的排序依据。</summary>
    public DateTime StoredTime { get; set; }

    /// <summary>状态：0=在库，1=已领出，2=锁定。</summary>
    public int Status { get; set; }

    /// <summary>关联工单号；在库时为空。</summary>
    public string? WorkOrder { get; set; }

    /// <summary>关联机台号；在库时为空。</summary>
    public string? MachineNo { get; set; }
}
