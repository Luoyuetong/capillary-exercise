using CapillaryExercise.Hardware;
using CapillaryExercise.Services;

namespace CapillaryExercise.Tests;

/// <summary>
/// 三个进程内 Fake 的行为测试（TC-17/18/19）。
/// 进程内直接 new 出 Fake，无需启动任何外部程序；覆盖默认成功与可配置失败两类分支。
/// </summary>
public class FakeTests
{
    // ---- TC-17：FakePlcController 取料返回成功 ----

    [Fact]
    public async Task FetchFromSlotAsync_AfterConnect_ReturnsTrue()
    {
        // Arrange
        var plc = new FakePlcController();

        // Act
        var connected = await plc.ConnectAsync();
        var fetched = await plc.FetchFromSlotAsync("A", 5, 10);

        // Assert：连接成功，取料返回 true。
        Assert.True(connected);
        Assert.True(plc.IsConnected);
        Assert.True(fetched);
    }

    [Fact]
    public async Task FetchFromSlotAsync_ConfiguredToFail_ReturnsFalse()
    {
        // Arrange：配置为模拟取料失败。
        var plc = new FakePlcController { FetchShouldSucceed = false };

        // Act
        var fetched = await plc.FetchFromSlotAsync("A", 5, 10);

        // Assert
        Assert.False(fetched);
    }

    [Fact]
    public async Task ConnectAsync_ConfiguredToFail_ReturnsFalseAndStaysDisconnected()
    {
        // Arrange：配置为模拟连接失败。
        var plc = new FakePlcController { ConnectShouldSucceed = false };

        // Act
        var connected = await plc.ConnectAsync();

        // Assert
        Assert.False(connected);
        Assert.False(plc.IsConnected);
    }

    [Fact]
    public async Task FakePlcController_RaisesStatusChangedOnActions()
    {
        // Arrange
        var plc = new FakePlcController();
        var messages = new List<string>();
        plc.OnStatusChanged += messages.Add;

        // Act
        await plc.ConnectAsync();
        await plc.FetchFromSlotAsync("A", 5, 10);

        // Assert：每个动作都对外报告状态。
        Assert.Equal(2, messages.Count);
    }

    // ---- TC-18：FakeScanner 返回预置条码 ----

    [Fact]
    public async Task ScanAsync_PresetBarcode_ReturnsThatBarcode()
    {
        // Arrange：预置条码 BC001。
        var scanner = new FakeScanner("BC001");

        // Act
        var barcode = await scanner.ScanAsync();

        // Assert
        Assert.Equal("BC001", barcode);
    }

    [Fact]
    public async Task ScanAsync_NoPresetBarcode_ReturnsNull()
    {
        // Arrange：未预置条码，模拟读码失败。
        var scanner = new FakeScanner();

        // Act
        var barcode = await scanner.ScanAsync();

        // Assert
        Assert.Null(barcode);
    }

    [Fact]
    public async Task ScanAsync_PresetBarcode_RaisesOnBarcodeReceived()
    {
        // Arrange
        var scanner = new FakeScanner("BC001");
        string? received = null;
        scanner.OnBarcodeReceived += b => received = b;

        // Act
        await scanner.ScanAsync();

        // Assert：读到条码时触发被动推送事件。
        Assert.Equal("BC001", received);
    }

    // ---- TC-19：FakeMesService 查询类型 ----

    [Fact]
    public async Task QueryCapillaryTypeAsync_PresetMapping_ReturnsType()
    {
        // Arrange：预置 WO001 → CAP-A。
        var mes = new FakeMesService().WithType("WO001", "CAP-A");

        // Act
        var type = await mes.QueryCapillaryTypeAsync("WO001", "M01");

        // Assert
        Assert.Equal("CAP-A", type);
    }

    [Fact]
    public async Task QueryCapillaryTypeAsync_UnknownWorkOrder_ReturnsNull()
    {
        // Arrange：未预置任何映射。
        var mes = new FakeMesService();

        // Act
        var type = await mes.QueryCapillaryTypeAsync("WO999", "M01");

        // Assert
        Assert.Null(type);
    }

    [Fact]
    public async Task ReportPickupAsync_Default_ReturnsTrue()
    {
        // Arrange：默认放行。
        var mes = new FakeMesService();

        // Act
        var approved = await mes.ReportPickupAsync("WO001", "M01", "BC001", "CAP-A");

        // Assert
        Assert.True(approved);
    }

    [Fact]
    public async Task ReportPickupAsync_ConfiguredToReject_ReturnsFalse()
    {
        // Arrange：配置为模拟 MES 拒绝。
        var mes = new FakeMesService { ShouldApprovePickup = false };

        // Act
        var approved = await mes.ReportPickupAsync("WO001", "M01", "BC001", "CAP-A");

        // Assert
        Assert.False(approved);
    }
}
