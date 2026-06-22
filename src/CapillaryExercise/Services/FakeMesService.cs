namespace CapillaryExercise.Services;

/// <summary>
/// 进程内模拟 MES，实现 <see cref="IMesService"/>，让 App 无需真实 MES 即可运行并支撑端到端测试。
/// 类型查询走可配置的工单→类型映射 <see cref="TypeMap"/>；领料上报由 <see cref="ShouldApprovePickup"/> 决定放行或拒绝。
/// 生产环境可替换为 HTTP 调用真实 MES，业务逻辑零改动。
/// </summary>
public class FakeMesService : IMesService
{
    /// <summary>工单号 → 劈刀类型的预置映射。查询命中返回对应类型，未命中返回 null。</summary>
    public Dictionary<string, string> TypeMap { get; } = new();

    /// <summary>上报领料时 MES 是否放行。设为 false 模拟 MES 拒绝。默认 true。</summary>
    public bool ShouldApprovePickup { get; set; } = true;

    /// <summary>
    /// 预置一条工单→类型映射，便于链式配置。
    /// </summary>
    /// <param name="workOrder">工单号。</param>
    /// <param name="capillaryType">该工单所需劈刀类型。</param>
    /// <returns>当前实例，支持链式调用。</returns>
    public FakeMesService WithType(string workOrder, string capillaryType)
    {
        TypeMap[workOrder] = capillaryType;
        return this;
    }

    /// <inheritdoc />
    public Task<string?> QueryCapillaryTypeAsync(string workOrder, string machineNo)
    {
        // 命中预置映射返回类型，否则返回 null 表示工单无效/查询失败。
        var type = TypeMap.GetValueOrDefault(workOrder);
        return Task.FromResult(type);
    }

    /// <inheritdoc />
    public Task<bool> ReportPickupAsync(string workOrder, string machineNo,
                                        string barcode, string capillaryType)
        => Task.FromResult(ShouldApprovePickup);
}
