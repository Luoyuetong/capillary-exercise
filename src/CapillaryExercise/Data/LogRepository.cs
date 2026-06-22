using System.Globalization;
using CapillaryExercise.Models;

namespace CapillaryExercise.Data;

/// <summary>
/// 基于 SQLite 的操作日志记录实现。使用参数化查询。
/// </summary>
public class LogRepository : ILogRepository
{
    private readonly DbHelper _db;

    /// <summary>
    /// 用 DbHelper 构造，数据库连接由其统一提供。
    /// </summary>
    /// <param name="db">数据库帮助类。</param>
    public LogRepository(DbHelper db) => _db = db;

    /// <inheritdoc />
    public void Insert(OperationLog log)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO OperationLog
                (OperationType, Barcode, CapillaryType, Face, PosX, PosY,
                 WorkOrder, MachineNo, Result, Message, Timestamp)
            VALUES
                ($operationType, $barcode, $capillaryType, $face, $posX, $posY,
                 $workOrder, $machineNo, $result, $message, $timestamp);
            """;
        command.Parameters.AddWithValue("$operationType", log.OperationType);
        command.Parameters.AddWithValue("$barcode", log.Barcode);
        command.Parameters.AddWithValue("$capillaryType", log.CapillaryType);
        command.Parameters.AddWithValue("$face", log.Face);
        command.Parameters.AddWithValue("$posX", log.PosX);
        command.Parameters.AddWithValue("$posY", log.PosY);
        command.Parameters.AddWithValue("$workOrder", log.WorkOrder);
        command.Parameters.AddWithValue("$machineNo", log.MachineNo);
        command.Parameters.AddWithValue("$result", log.Result);
        command.Parameters.AddWithValue("$message", log.Message);
        // ISO-8601 往返格式存储，读出时可无歧义还原。
        command.Parameters.AddWithValue("$timestamp",
            log.Timestamp.ToString("o", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }
}
