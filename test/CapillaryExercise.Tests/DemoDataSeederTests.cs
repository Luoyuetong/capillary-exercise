using CapillaryExercise.Data;
using CapillaryExercise.Demo;
using Microsoft.Data.Sqlite;

namespace CapillaryExercise.Tests;

/// <summary>
/// DemoDataSeeder 的预置与重置行为测试。
/// 每个用例使用独立的临时 SQLite 文件，保证隔离。
/// </summary>
public class DemoDataSeederTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbHelper _db;
    private readonly CapillaryRepository _capRepo;
    private readonly LogRepository _logRepo;
    private readonly DemoDataSeeder _seeder;

    public DemoDataSeederTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cap-seed-test-{Guid.NewGuid():N}.db");
        _db = new DbHelper(_dbPath);
        _db.InitializeSchema();
        _capRepo = new CapillaryRepository(_db);
        _logRepo = new LogRepository(_db);
        _seeder = new DemoDataSeeder(_db);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    // ---- SeedIfEmpty 在空库时预置演示数据 ----

    [Fact]
    public void SeedIfEmpty_EmptyDb_SeedsDemoCapillaries()
    {
        // Act
        _seeder.SeedIfEmpty();

        // Assert：成功场景的 CAP-A（FIFO 最早一条）与读码失败场景的 CAP-B 均已就位，CAP-C 不预置。
        var capA = _capRepo.FindOldestByType("CAP-A");
        Assert.NotNull(capA);
        Assert.Equal(DemoDataSeeder.DemoBarcode, capA!.Barcode);
        Assert.NotNull(_capRepo.FindOldestByType("CAP-B"));
        Assert.Null(_capRepo.FindOldestByType("CAP-C"));
    }

    // ---- SeedIfEmpty 幂等：已有数据时不重复插入 ----

    [Fact]
    public void SeedIfEmpty_AlreadySeeded_DoesNotDuplicate()
    {
        // Arrange
        _seeder.SeedIfEmpty();

        // Act：再次调用应跳过（否则唯一索引会冲突抛异常）。
        _seeder.SeedIfEmpty();

        // Assert：CAP-A 仍能查到，未抛异常即说明未重复插入。
        Assert.NotNull(_capRepo.FindOldestByType("CAP-A"));
    }

    // ---- Reset 把改动过的库恢复到干净的初始演示状态 ----

    [Fact]
    public void Reset_AfterDataMutated_RestoresInitialDemoState()
    {
        // Arrange：预置后模拟一次"成功领料"——把最早的 CAP-A 标记为已领出，并写一条日志。
        _seeder.SeedIfEmpty();
        var picked = _capRepo.FindOldestByType("CAP-A");
        Assert.NotNull(picked);
        _capRepo.UpdateStatus(picked!.Id, 1, "WO001", "M1");
        _logRepo.Insert(new Models.OperationLog
        {
            OperationType = "Pickup", Barcode = picked.Barcode, CapillaryType = "CAP-A",
            Face = "A", PosX = 5, PosY = 10, WorkOrder = "WO001", MachineNo = "M1",
            Result = "Success", Message = string.Empty, Timestamp = DateTime.Now
        });

        // 领走后最早的在库 CAP-A 变成了 BC-A-002。
        Assert.Equal("BC-A-002", _capRepo.FindOldestByType("CAP-A")!.Barcode);

        // Act
        _seeder.Reset();

        // Assert：最早的在库 CAP-A 又回到 BC-A-001（演示成功场景可重复），日志表清空。
        Assert.Equal(DemoDataSeeder.DemoBarcode, _capRepo.FindOldestByType("CAP-A")!.Barcode);
        Assert.Equal(0, CountLogs());
    }

    /// <summary>统计操作日志条数。</summary>
    private int CountLogs()
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM OperationLog;";
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
