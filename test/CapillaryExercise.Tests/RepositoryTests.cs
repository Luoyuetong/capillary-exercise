using System.Globalization;
using CapillaryExercise.Data;
using CapillaryExercise.Models;
using Microsoft.Data.Sqlite;

namespace CapillaryExercise.Tests;

/// <summary>
/// CapillaryRepository 与 LogRepository 的数据访问测试（TC-11~TC-16）。
/// 每个测试用例使用独立的临时 SQLite 文件，保证隔离。
/// </summary>
public class RepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbHelper _db;
    private readonly CapillaryRepository _capRepo;
    private readonly LogRepository _logRepo;

    public RepositoryTests()
    {
        // 每个测试一个独立临时文件，互不干扰。
        _dbPath = Path.Combine(Path.GetTempPath(), $"cap-test-{Guid.NewGuid():N}.db");
        _db = new DbHelper(_dbPath);
        _db.InitializeSchema();
        _capRepo = new CapillaryRepository(_db);
        _logRepo = new LogRepository(_db);
    }

    public void Dispose()
    {
        // 释放连接池对文件的句柄后再删除，避免 Windows 上文件占用。
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    // ---- TC-11：FindOldestByType 返回最早入库的在库劈刀 ----

    [Fact]
    public void FindOldestByType_MultipleInStock_ReturnsEarliestStored()
    {
        // Arrange：插入 3 条 CAP-A，入库时间不同，均在库。
        SeedCapillary("BC001", "CAP-A", storedTime: new DateTime(2026, 1, 3), status: 0);
        SeedCapillary("BC002", "CAP-A", storedTime: new DateTime(2026, 1, 1), status: 0);
        SeedCapillary("BC003", "CAP-A", storedTime: new DateTime(2026, 1, 2), status: 0);

        // Act
        var result = _capRepo.FindOldestByType("CAP-A");

        // Assert：取 StoredTime 最早(2026-01-01)的那条。
        Assert.NotNull(result);
        Assert.Equal("BC002", result!.Barcode);
    }

    // ---- TC-12：FindOldestByType 忽略非在库状态 ----

    [Fact]
    public void FindOldestByType_EarlierIsPickedOut_ReturnsInStockOne()
    {
        // Arrange：更早的一条已领出(Status=1)，较晚的一条在库(Status=0)。
        SeedCapillary("BC001", "CAP-A", storedTime: new DateTime(2026, 1, 1), status: 1);
        SeedCapillary("BC002", "CAP-A", storedTime: new DateTime(2026, 1, 2), status: 0);

        // Act
        var result = _capRepo.FindOldestByType("CAP-A");

        // Assert：跳过已领出，返回在库的 BC002。
        Assert.NotNull(result);
        Assert.Equal("BC002", result!.Barcode);
    }

    // ---- TC-13：FindOldestByType 忽略锁定仓位 ----

    [Fact]
    public void FindOldestByType_EarlierIsLocked_ReturnsInStockOne()
    {
        // Arrange：更早的一条锁定(Status=2)，较晚的一条在库(Status=0)。
        SeedCapillary("BC001", "CAP-A", storedTime: new DateTime(2026, 1, 1), status: 2);
        SeedCapillary("BC002", "CAP-A", storedTime: new DateTime(2026, 1, 2), status: 0);

        // Act
        var result = _capRepo.FindOldestByType("CAP-A");

        // Assert：跳过锁定，返回在库的 BC002。
        Assert.NotNull(result);
        Assert.Equal("BC002", result!.Barcode);
    }

    // ---- TC-14：FindOldestByType 无匹配返回 null ----

    [Fact]
    public void FindOldestByType_NoMatch_ReturnsNull()
    {
        // Arrange：库存中没有 CAP-A。
        SeedCapillary("BC001", "CAP-B", storedTime: new DateTime(2026, 1, 1), status: 0);

        // Act
        var result = _capRepo.FindOldestByType("CAP-A");

        // Assert
        Assert.Null(result);
    }

    // ---- TC-15：UpdateStatus 正确更新状态和关联工单 ----

    [Fact]
    public void UpdateStatus_GivenId_UpdatesStatusAndWorkOrder()
    {
        // Arrange
        SeedCapillary("BC001", "CAP-A", storedTime: new DateTime(2026, 1, 1), status: 0);
        var seeded = _capRepo.GetByBarcode("BC001");
        Assert.NotNull(seeded);

        // Act：标记为已领出，关联工单/机台。
        _capRepo.UpdateStatus(seeded!.Id, 1, "WO001", "M01");

        // Assert
        var updated = _capRepo.GetByBarcode("BC001");
        Assert.NotNull(updated);
        Assert.Equal(1, updated!.Status);
        Assert.Equal("WO001", updated.WorkOrder);
        Assert.Equal("M01", updated.MachineNo);
    }

    // ---- TC-16：LogRepository.Insert 正确写入日志 ----

    [Fact]
    public void Insert_GivenLog_PersistsAllFields()
    {
        // Arrange
        var log = new OperationLog
        {
            OperationType = "Pickup",
            Barcode = "BC001",
            CapillaryType = "CAP-A",
            Face = "A",
            PosX = 5,
            PosY = 10,
            WorkOrder = "WO001",
            MachineNo = "M01",
            Result = "Fail",
            Message = "读码失败",
            Timestamp = new DateTime(2026, 6, 22, 14, 30, 0)
        };

        // Act
        _logRepo.Insert(log);

        // Assert：从数据库读回，逐字段比对。
        var stored = ReadSingleLog();
        Assert.Equal("Pickup", stored.OperationType);
        Assert.Equal("BC001", stored.Barcode);
        Assert.Equal("CAP-A", stored.CapillaryType);
        Assert.Equal("A", stored.Face);
        Assert.Equal(5, stored.PosX);
        Assert.Equal(10, stored.PosY);
        Assert.Equal("WO001", stored.WorkOrder);
        Assert.Equal("M01", stored.MachineNo);
        Assert.Equal("Fail", stored.Result);
        Assert.Equal("读码失败", stored.Message);
        Assert.Equal(log.Timestamp, stored.Timestamp);
    }

    // ---- 测试辅助 ----

    /// <summary>
    /// 向 CapillaryInfo 表插入一条记录（参数化）。Repository 本身不提供插入，故测试直接写库。
    /// </summary>
    private void SeedCapillary(string barcode, string type, DateTime storedTime, int status)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO CapillaryInfo
                (Barcode, CapillaryType, Face, PosX, PosY, StoredTime, Status, WorkOrder, MachineNo)
            VALUES
                ($barcode, $type, 'A', 5, 10, $storedTime, $status, NULL, NULL);
            """;
        command.Parameters.AddWithValue("$barcode", barcode);
        command.Parameters.AddWithValue("$type", type);
        command.Parameters.AddWithValue("$storedTime", storedTime.ToString("o", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$status", status);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 读回 OperationLog 表中唯一一条记录，映射为实体供断言。
    /// </summary>
    private OperationLog ReadSingleLog()
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT OperationType, Barcode, CapillaryType, Face, PosX, PosY,
                   WorkOrder, MachineNo, Result, Message, Timestamp
            FROM OperationLog
            LIMIT 1;
            """;
        using var reader = command.ExecuteReader();
        Assert.True(reader.Read(), "日志表中应存在一条记录");
        return new OperationLog
        {
            OperationType = reader.GetString(0),
            Barcode = reader.GetString(1),
            CapillaryType = reader.GetString(2),
            Face = reader.GetString(3),
            PosX = reader.GetInt32(4),
            PosY = reader.GetInt32(5),
            WorkOrder = reader.GetString(6),
            MachineNo = reader.GetString(7),
            Result = reader.GetString(8),
            Message = reader.GetString(9),
            Timestamp = DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind)
        };
    }
}
