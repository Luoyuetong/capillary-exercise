using System.Globalization;
using CapillaryExercise.Data;

namespace CapillaryExercise.Demo;

/// <summary>
/// 演示数据预置与重置：把成功 / 读码失败 / 无库存等分支所需的库存劈刀写入数据库，
/// 让 App 无需手工建库即可跑通各场景。抽成独立类（而非塞在 <c>Program</c> 里）有两个目的：
/// 一是供界面上的"重置演示数据"按钮复用，二是可独立单元测试（见编码规范 §8）。
/// </summary>
public class DemoDataSeeder
{
    /// <summary>演示用的匹配条码（最早入库的 CAP-A）。扫码器预置此码以演示"读码匹配成功"。</summary>
    public const string DemoBarcode = "BC-A-001";

    private readonly DbHelper _db;

    /// <summary>
    /// 用 DbHelper 构造，数据库连接由其统一提供。
    /// </summary>
    /// <param name="db">数据库帮助类。</param>
    public DemoDataSeeder(DbHelper db) => _db = db;

    /// <summary>
    /// 库存为空时预置演示劈刀；已有数据则跳过，避免重复启动时唯一索引冲突。
    /// </summary>
    public void SeedIfEmpty()
    {
        if (!IsCapillaryEmpty())
        {
            return;
        }
        SeedCapillaries();
    }

    /// <summary>
    /// 重置演示数据：清空劈刀与日志两张表后重新预置。
    /// 让"成功领料""读码失败锁仓"等会改库的场景可以反复演示，每次都从同一干净状态开始。
    /// </summary>
    public void Reset()
    {
        using (var connection = _db.CreateConnection())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                DELETE FROM CapillaryInfo;
                DELETE FROM OperationLog;
                """;
            command.ExecuteNonQuery();
        }
        SeedCapillaries();
    }

    /// <summary>
    /// 预置演示库存：覆盖成功 / 读码失败 / 无库存三类分支。
    /// </summary>
    private void SeedCapillaries()
    {
        // CAP-A 两条：最早的 BC-A-001 用于演示成功领料（FIFO 命中最早一条，扫码器认这个码）。
        Insert(DemoBarcode, "CAP-A", "A", 5, 10, new DateTime(2026, 1, 1, 8, 0, 0));
        Insert("BC-A-002", "CAP-A", "A", 5, 11, new DateTime(2026, 1, 2, 8, 0, 0));
        // CAP-B 一条：扫码器只预置了 CAP-A 的条码，取它会读码不符 → 演示锁定分支。
        Insert("BC-B-001", "CAP-B", "B", 3, 7, new DateTime(2026, 1, 1, 9, 0, 0));
        // CAP-C 不预置库存 → 演示"库存不足"。
    }

    /// <summary>
    /// 判断劈刀表是否为空（无任何记录）。
    /// </summary>
    private bool IsCapillaryEmpty()
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM CapillaryInfo);";
        return Convert.ToInt64(command.ExecuteScalar()) == 0;
    }

    /// <summary>
    /// 插入一条在库（Status=0）劈刀，参数化写入。
    /// </summary>
    private void Insert(string barcode, string type, string face, int x, int y, DateTime storedTime)
    {
        using var connection = _db.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO CapillaryInfo
                (Barcode, CapillaryType, Face, PosX, PosY, StoredTime, Status, WorkOrder, MachineNo)
            VALUES
                ($barcode, $type, $face, $posX, $posY, $storedTime, 0, NULL, NULL);
            """;
        command.Parameters.AddWithValue("$barcode", barcode);
        command.Parameters.AddWithValue("$type", type);
        command.Parameters.AddWithValue("$face", face);
        command.Parameters.AddWithValue("$posX", x);
        command.Parameters.AddWithValue("$posY", y);
        // ISO-8601 往返格式，字典序即时间序，与 FIFO 排序约定一致。
        command.Parameters.AddWithValue("$storedTime", storedTime.ToString("o", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }
}
