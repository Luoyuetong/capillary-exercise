using CapillaryExercise.Models;

namespace CapillaryExercise.Data;

/// <summary>
/// 劈刀数据访问接口。封装库存查询与状态更新。
/// </summary>
public interface ICapillaryRepository
{
    /// <summary>
    /// FIFO 查找：在指定类型、状态为"在库"(0) 的劈刀中，返回入库时间最早的一条。
    /// 已领出(1) 和锁定(2) 的劈刀不参与，无匹配时返回 null。
    /// </summary>
    /// <param name="capillaryType">劈刀类型。</param>
    /// <returns>入库最早的在库劈刀，无则返回 null。</returns>
    CapillaryInfo? FindOldestByType(string capillaryType);

    /// <summary>
    /// 根据条码查询劈刀，无匹配时返回 null。
    /// </summary>
    /// <param name="barcode">劈刀条码。</param>
    /// <returns>匹配的劈刀，无则返回 null。</returns>
    CapillaryInfo? GetByBarcode(string barcode);

    /// <summary>
    /// 更新劈刀状态及关联工单/机台。
    /// </summary>
    /// <param name="id">劈刀主键。</param>
    /// <param name="status">新状态：0=在库，1=已领出，2=锁定。</param>
    /// <param name="workOrder">关联工单号；锁定时可为 null。</param>
    /// <param name="machineNo">关联机台号；锁定时可为 null。</param>
    void UpdateStatus(int id, int status, string? workOrder, string? machineNo);
}
