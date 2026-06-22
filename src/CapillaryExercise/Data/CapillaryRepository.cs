using System.Globalization;
using CapillaryExercise.Models;
using Microsoft.Data.Sqlite;

namespace CapillaryExercise.Data;

/// <summary>
/// 基于 SQLite 的劈刀数据访问实现。全部使用参数化查询。
/// </summary>
public class CapillaryRepository : ICapillaryRepository
{
    private readonly DbHelper _db;

    /// <summary>
    /// 用 DbHelper 构造，数据库连接由其统一提供。
    /// </summary>
    /// <param name="db">数据库帮助类。</param>
    public CapillaryRepository(DbHelper db) => _db = db;

    /// <inheritdoc />
    public CapillaryInfo? FindOldestByType(string capillaryType)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        // 只取在库(Status=0)，按入库时间升序取最早一条；StoredTime 以 ISO-8601 存储，字典序即时间序。
        command.CommandText = """
            SELECT Id, Barcode, CapillaryType, Face, PosX, PosY, StoredTime, Status, WorkOrder, MachineNo
            FROM CapillaryInfo
            WHERE CapillaryType = $type AND Status = 0
            ORDER BY StoredTime ASC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$type", capillaryType);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapToCapillary(reader) : null;
    }

    /// <inheritdoc />
    public CapillaryInfo? GetByBarcode(string barcode)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Barcode, CapillaryType, Face, PosX, PosY, StoredTime, Status, WorkOrder, MachineNo
            FROM CapillaryInfo
            WHERE Barcode = $barcode
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$barcode", barcode);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapToCapillary(reader) : null;
    }

    /// <inheritdoc />
    public void UpdateStatus(int id, int status, string? workOrder, string? machineNo)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE CapillaryInfo
            SET Status = $status, WorkOrder = $workOrder, MachineNo = $machineNo
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$workOrder", (object?)workOrder ?? DBNull.Value);
        command.Parameters.AddWithValue("$machineNo", (object?)machineNo ?? DBNull.Value);
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// 将当前行映射为 CapillaryInfo 实体。
    /// </summary>
    private static CapillaryInfo MapToCapillary(SqliteDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Barcode = reader.GetString(1),
        CapillaryType = reader.GetString(2),
        Face = reader.GetString(3),
        PosX = reader.GetInt32(4),
        PosY = reader.GetInt32(5),
        StoredTime = DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind),
        Status = reader.GetInt32(7),
        WorkOrder = reader.IsDBNull(8) ? null : reader.GetString(8),
        MachineNo = reader.IsDBNull(9) ? null : reader.GetString(9)
    };
}
