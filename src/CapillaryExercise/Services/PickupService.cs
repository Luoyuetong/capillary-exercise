using CapillaryExercise.Data;
using CapillaryExercise.Hardware;
using CapillaryExercise.Models;

namespace CapillaryExercise.Services;

/// <summary>
/// 领料流程的核心业务编排：查 MES → FIFO 找库存 → PLC 取料 → 扫码验证
/// → MES 上报 → PLC 出料 → 更新数据库与日志。
/// 仅依赖接口（硬件/MES/数据访问），便于 Mock 测试与 Fake/生产切换。
/// 异常处理策略见 002-DESIGN.md 第 5.2 节。
/// </summary>
public class PickupService
{
    private readonly IPlcController _plc;
    private readonly IScanner _scanner;
    private readonly IMesService _mes;
    private readonly ICapillaryRepository _capRepo;
    private readonly ILogRepository _logRepo;

    /// <summary>
    /// 通过构造函数注入全部依赖，均以接口形式传入。
    /// </summary>
    /// <param name="plc">PLC 控制接口。</param>
    /// <param name="scanner">扫码器接口。</param>
    /// <param name="mes">MES 接口。</param>
    /// <param name="capRepo">劈刀数据访问接口。</param>
    /// <param name="logRepo">操作日志记录接口。</param>
    public PickupService(
        IPlcController plc,
        IScanner scanner,
        IMesService mes,
        ICapillaryRepository capRepo,
        ILogRepository logRepo)
    {
        _plc = plc;
        _scanner = scanner;
        _mes = mes;
        _capRepo = capRepo;
        _logRepo = logRepo;
    }

    /// <summary>
    /// 执行一次完整的领料流程。每一步的预期内失败都通过返回值判断，
    /// 并按 5.2 策略决定是否回退（放回原位 + 锁定）或更新数据库。
    /// </summary>
    /// <param name="workOrder">工单号。</param>
    /// <param name="machineNo">机台号。</param>
    /// <param name="progress">进度报告通道，逐步上报当前动作供 UI 展示。</param>
    /// <param name="ct">取消令牌，传递给扫码等可取消的等待。</param>
    /// <returns>领料结果，含成功标志和说明文字。</returns>
    public async Task<PickupResult> ExecuteAsync(
        string workOrder,
        string machineNo,
        IProgress<string> progress,
        CancellationToken ct = default)
    {
        // 1. MES 查询劈刀类型
        progress.Report("MES: 查询劈刀类型...");
        var capType = await _mes.QueryCapillaryTypeAsync(workOrder, machineNo);
        if (capType == null)
        {
            return PickupResult.Fail("MES查询失败或工单无效");
        }

        // 2. FIFO 查找库存
        progress.Report($"查找库存: {capType}...");
        var cap = _capRepo.FindOldestByType(capType);
        if (cap == null)
        {
            return PickupResult.Fail($"库存不足: {capType}");
        }

        // 3. PLC 取料
        progress.Report($"PLC: 从 {cap.Face}{cap.PosX:D2}{cap.PosY:D2} 取料...");
        if (!await _plc.FetchFromSlotAsync(cap.Face, cap.PosX, cap.PosY))
        {
            // 取料失败：劈刀仍在原仓位，数据库不更新，仅记录日志。
            LogOperation(cap, workOrder, machineNo, "Fail", "PLC取料失败");
            return PickupResult.Fail("PLC取料失败");
        }

        // 4. 扫码验证
        progress.Report("扫码器: 读码验证...");
        var scannedBarcode = await _scanner.ScanAsync(ct);
        if (scannedBarcode == null || scannedBarcode != cap.Barcode)
        {
            // 读码失败或条码不匹配：放回原位并锁定仓位，不再参与 FIFO。
            progress.Report("读码失败，放回原位并锁定...");
            await _plc.ReturnToSlotAsync(cap.Face, cap.PosX, cap.PosY);
            _capRepo.UpdateStatus(cap.Id, 2, null, null);
            LogOperation(cap, workOrder, machineNo, "Fail", "读码失败");
            return PickupResult.Fail("读码失败，仓位已锁定");
        }

        // 5. MES 上报
        progress.Report("MES: 上报领料信息...");
        if (!await _mes.ReportPickupAsync(workOrder, machineNo, cap.Barcode, cap.CapillaryType))
        {
            // MES 拒绝：放回原位并锁定仓位。
            progress.Report("MES拒绝，放回原位并锁定...");
            await _plc.ReturnToSlotAsync(cap.Face, cap.PosX, cap.PosY);
            _capRepo.UpdateStatus(cap.Id, 2, null, null);
            LogOperation(cap, workOrder, machineNo, "Fail", "MES拒绝");
            return PickupResult.Fail("MES拒绝，仓位已锁定");
        }

        // 6. PLC 出料
        progress.Report("PLC: 出料...");
        if (!await _plc.OutputToPickupPortAsync())
        {
            // 出料失败：劈刀还在机器里，数据库不更新为已领出（见 9.4），仅记录日志。
            LogOperation(cap, workOrder, machineNo, "Fail", "PLC出料失败");
            return PickupResult.Fail("PLC出料失败");
        }

        // 7. 更新数据库和日志
        _capRepo.UpdateStatus(cap.Id, 1, workOrder, machineNo);
        LogOperation(cap, workOrder, machineNo, "Success", string.Empty);

        return PickupResult.Success($"领料成功: {cap.Barcode}");
    }

    /// <summary>
    /// 按劈刀信息和结果写入一条操作日志。集中拼装，避免每个分支重复填字段。
    /// </summary>
    private void LogOperation(
        CapillaryInfo cap, string workOrder, string machineNo, string result, string message)
    {
        _logRepo.Insert(new OperationLog
        {
            OperationType = "Pickup",
            Barcode = cap.Barcode,
            CapillaryType = cap.CapillaryType,
            Face = cap.Face,
            PosX = cap.PosX,
            PosY = cap.PosY,
            WorkOrder = workOrder,
            MachineNo = machineNo,
            Result = result,
            Message = message,
            Timestamp = DateTime.Now
        });
    }
}
