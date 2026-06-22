namespace CapillaryExercise.Services;

/// <summary>
/// MES 接口。封装工单劈刀类型查询与领料上报。
/// 教学/演示用 <c>FakeMesService</c> 进程内模拟；生产可换 HTTP 调用真实 MES，业务逻辑零改动。
/// </summary>
public interface IMesService
{
    /// <summary>
    /// 查询指定工单/机台所需的劈刀类型。
    /// </summary>
    /// <param name="workOrder">工单号。</param>
    /// <param name="machineNo">机台号。</param>
    /// <returns>所需劈刀类型；查询失败或工单无效时返回 null。</returns>
    Task<string?> QueryCapillaryTypeAsync(string workOrder, string machineNo);

    /// <summary>
    /// 上报领料信息，由 MES 确认是否放行。
    /// </summary>
    /// <param name="workOrder">工单号。</param>
    /// <param name="machineNo">机台号。</param>
    /// <param name="barcode">劈刀条码。</param>
    /// <param name="capillaryType">劈刀类型。</param>
    /// <returns>MES 确认放行返回 true，拒绝返回 false。</returns>
    Task<bool> ReportPickupAsync(string workOrder, string machineNo,
                                 string barcode, string capillaryType);
}
