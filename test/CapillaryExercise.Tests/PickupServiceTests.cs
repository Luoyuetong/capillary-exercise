using CapillaryExercise.Data;
using CapillaryExercise.Hardware;
using CapillaryExercise.Models;
using CapillaryExercise.Services;
using NSubstitute;

namespace CapillaryExercise.Tests;

/// <summary>
/// PickupService 领料流程业务逻辑单元测试（TC-01~TC-10）。
/// 用 NSubstitute Mock 掉 PLC/扫码器/MES/Repository，单独验证每条分支，不依赖真实硬件。
/// </summary>
public class PickupServiceTests
{
    private readonly IPlcController _plc = Substitute.For<IPlcController>();
    private readonly IScanner _scanner = Substitute.For<IScanner>();
    private readonly IMesService _mes = Substitute.For<IMesService>();
    private readonly ICapillaryRepository _capRepo = Substitute.For<ICapillaryRepository>();
    private readonly ILogRepository _logRepo = Substitute.For<ILogRepository>();
    private readonly IProgress<string> _progress = Substitute.For<IProgress<string>>();
    private readonly PickupService _service;

    public PickupServiceTests()
    {
        _service = new PickupService(_plc, _scanner, _mes, _capRepo, _logRepo);
    }

    /// <summary>
    /// 构造一条在库劈刀，默认 BC001 / CAP-A / 仓位 A,5,10。
    /// </summary>
    private static CapillaryInfo SampleCapillary() => new()
    {
        Id = 42,
        Barcode = "BC001",
        CapillaryType = "CAP-A",
        Face = "A",
        PosX = 5,
        PosY = 10,
        StoredTime = new DateTime(2026, 1, 1),
        Status = 0
    };

    /// <summary>
    /// 把全部依赖配置成"正常流程一路成功"，个别测试再覆盖某一步使其失败。
    /// </summary>
    private CapillaryInfo ArrangeHappyPath()
    {
        var cap = SampleCapillary();
        _mes.QueryCapillaryTypeAsync("WO001", "M01").Returns("CAP-A");
        _capRepo.FindOldestByType("CAP-A").Returns(cap);
        _plc.FetchFromSlotAsync(cap.Face, cap.PosX, cap.PosY).Returns(true);
        _scanner.ScanAsync(Arg.Any<CancellationToken>()).Returns(cap.Barcode);
        _mes.ReportPickupAsync("WO001", "M01", cap.Barcode, cap.CapillaryType).Returns(true);
        _plc.OutputToPickupPortAsync().Returns(true);
        return cap;
    }

    // ---- TC-01：完整领料成功 ----

    [Fact]
    public async Task ExecuteAsync_HappyPath_ReturnsSuccessAndMarksPickedOut()
    {
        // Arrange
        var cap = ArrangeHappyPath();

        // Act
        var result = await _service.ExecuteAsync("WO001", "M01", _progress);

        // Assert：成功，且状态更新为已领出(1)、关联工单/机台。
        Assert.True(result.IsSuccess);
        Assert.Contains("BC001", result.Message);
        _capRepo.Received(1).UpdateStatus(cap.Id, 1, "WO001", "M01");
        _logRepo.Received(1).Insert(Arg.Is<OperationLog>(
            log => log.Result == "Success" && log.Barcode == "BC001"));
    }

    // ---- TC-02：MES 查询返回 null（工单无效） ----

    [Fact]
    public async Task ExecuteAsync_MesQueryReturnsNull_ReturnsFailAndTouchesNoHardware()
    {
        // Arrange：MES 查询失败。
        _mes.QueryCapillaryTypeAsync("WO999", "M01").Returns((string?)null);

        // Act
        var result = await _service.ExecuteAsync("WO999", "M01", _progress);

        // Assert：失败，且未触碰任何 PLC 动作、未查库存。
        Assert.False(result.IsSuccess);
        Assert.Contains("MES查询失败", result.Message);
        _capRepo.DidNotReceive().FindOldestByType(Arg.Any<string>());
        await _plc.DidNotReceive().FetchFromSlotAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>());
    }

    // ---- TC-03：无库存 ----

    [Fact]
    public async Task ExecuteAsync_NoStock_ReturnsFailAndTouchesNoHardware()
    {
        // Arrange：MES 返回类型，但库存查不到。
        _mes.QueryCapillaryTypeAsync("WO001", "M01").Returns("CAP-A");
        _capRepo.FindOldestByType("CAP-A").Returns((CapillaryInfo?)null);

        // Act
        var result = await _service.ExecuteAsync("WO001", "M01", _progress);

        // Assert：失败含"库存不足"，未触碰 PLC。
        Assert.False(result.IsSuccess);
        Assert.Contains("库存不足", result.Message);
        await _plc.DidNotReceive().FetchFromSlotAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>());
    }

    // ---- TC-04：FIFO 顺序正确性（Service 正确使用 Repository 返回值） ----

    [Fact]
    public async Task ExecuteAsync_UsesCapillaryReturnedByFifoLookup()
    {
        // Arrange：Repository 返回特定仓位的劈刀。
        var cap = ArrangeHappyPath();

        // Act
        await _service.ExecuteAsync("WO001", "M01", _progress);

        // Assert：取料动作使用的是 Repository 返回的那条的仓位坐标。
        await _plc.Received(1).FetchFromSlotAsync(cap.Face, cap.PosX, cap.PosY);
    }

    // ---- TC-05：PLC 取料失败 ----

    [Fact]
    public async Task ExecuteAsync_FetchFails_ReturnsFailAndDoesNotProceed()
    {
        // Arrange：取料失败。
        var cap = ArrangeHappyPath();
        _plc.FetchFromSlotAsync(cap.Face, cap.PosX, cap.PosY).Returns(false);

        // Act
        var result = await _service.ExecuteAsync("WO001", "M01", _progress);

        // Assert：失败；不读码、不上报、不出料；不更新劈刀状态。
        Assert.False(result.IsSuccess);
        Assert.Contains("PLC取料失败", result.Message);
        await _scanner.DidNotReceive().ScanAsync(Arg.Any<CancellationToken>());
        await _mes.DidNotReceive().ReportPickupAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _plc.DidNotReceive().OutputToPickupPortAsync();
        _capRepo.DidNotReceive().UpdateStatus(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>());
    }

    // ---- TC-06：扫码返回 null（读码失败） ----

    [Fact]
    public async Task ExecuteAsync_ScanReturnsNull_ReturnsToSlotAndLocks()
    {
        // Arrange：取料成功，但读码返回 null。
        var cap = ArrangeHappyPath();
        _scanner.ScanAsync(Arg.Any<CancellationToken>()).Returns((string?)null);

        // Act
        var result = await _service.ExecuteAsync("WO001", "M01", _progress);

        // Assert：放回原位、锁定(2)、失败、日志记录读码失败。
        Assert.False(result.IsSuccess);
        Assert.Contains("读码失败", result.Message);
        await _plc.Received(1).ReturnToSlotAsync(cap.Face, cap.PosX, cap.PosY);
        _capRepo.Received(1).UpdateStatus(cap.Id, 2, null, null);
        _logRepo.Received(1).Insert(Arg.Is<OperationLog>(
            log => log.Result == "Fail" && log.Message == "读码失败"));
        await _mes.DidNotReceive().ReportPickupAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    // ---- TC-07：扫码条码与库存不匹配 ----

    [Fact]
    public async Task ExecuteAsync_ScanMismatch_ReturnsToSlotAndLocks()
    {
        // Arrange：取料成功，但扫到的条码与库存不符。
        var cap = ArrangeHappyPath();
        _scanner.ScanAsync(Arg.Any<CancellationToken>()).Returns("BC999");

        // Act
        var result = await _service.ExecuteAsync("WO001", "M01", _progress);

        // Assert：同读码失败处理——放回 + 锁定 + 失败。
        Assert.False(result.IsSuccess);
        Assert.Contains("读码失败", result.Message);
        await _plc.Received(1).ReturnToSlotAsync(cap.Face, cap.PosX, cap.PosY);
        _capRepo.Received(1).UpdateStatus(cap.Id, 2, null, null);
    }

    // ---- TC-08：MES 上报被拒绝 ----

    [Fact]
    public async Task ExecuteAsync_MesRejects_ReturnsToSlotAndLocks()
    {
        // Arrange：取料、读码成功，但 MES 上报被拒绝。
        var cap = ArrangeHappyPath();
        _mes.ReportPickupAsync("WO001", "M01", cap.Barcode, cap.CapillaryType).Returns(false);

        // Act
        var result = await _service.ExecuteAsync("WO001", "M01", _progress);

        // Assert：放回原位、锁定(2)、失败、日志记录 MES拒绝；不出料。
        Assert.False(result.IsSuccess);
        Assert.Contains("MES拒绝", result.Message);
        await _plc.Received(1).ReturnToSlotAsync(cap.Face, cap.PosX, cap.PosY);
        _capRepo.Received(1).UpdateStatus(cap.Id, 2, null, null);
        _logRepo.Received(1).Insert(Arg.Is<OperationLog>(
            log => log.Result == "Fail" && log.Message == "MES拒绝"));
        await _plc.DidNotReceive().OutputToPickupPortAsync();
    }

    // ---- TC-09：PLC 出料失败 ----

    [Fact]
    public async Task ExecuteAsync_OutputFails_ReturnsFailAndKeepsInStock()
    {
        // Arrange：前序全部成功，仅出料失败。
        var cap = ArrangeHappyPath();
        _plc.OutputToPickupPortAsync().Returns(false);

        // Act
        var result = await _service.ExecuteAsync("WO001", "M01", _progress);

        // Assert：失败；不标记为已领出（劈刀还在机器里，见 9.4）；记录 Fail 日志。
        Assert.False(result.IsSuccess);
        Assert.Contains("PLC出料失败", result.Message);
        _capRepo.DidNotReceive().UpdateStatus(cap.Id, 1, Arg.Any<string?>(), Arg.Any<string?>());
        _logRepo.Received(1).Insert(Arg.Is<OperationLog>(log => log.Result == "Fail"));
    }

    // ---- TC-10：progress 报告内容 ----

    [Fact]
    public async Task ExecuteAsync_HappyPath_ReportsEachStep()
    {
        // Arrange
        ArrangeHappyPath();
        var reports = new List<string>();
        var progress = new SyncProgress(reports.Add);

        // Act
        await _service.ExecuteAsync("WO001", "M01", progress);

        // Assert：依次覆盖 MES查询 → 查库存 → 取料 → 读码 → 上报 → 出料。
        Assert.Contains(reports, m => m.Contains("查询劈刀类型"));
        Assert.Contains(reports, m => m.Contains("查找库存"));
        Assert.Contains(reports, m => m.Contains("取料"));
        Assert.Contains(reports, m => m.Contains("读码"));
        Assert.Contains(reports, m => m.Contains("上报"));
        Assert.Contains(reports, m => m.Contains("出料"));
    }

    /// <summary>
    /// 同步执行回调的 IProgress 实现。默认 Progress&lt;T&gt; 依赖
    /// SynchronizationContext 异步投递，在无 UI 上下文的测试里会丢报告，
    /// 故用同步版本确保 Report 立即记录。
    /// </summary>
    private sealed class SyncProgress : IProgress<string>
    {
        private readonly Action<string> _handler;
        public SyncProgress(Action<string> handler) => _handler = handler;
        public void Report(string value) => _handler(value);
    }
}
