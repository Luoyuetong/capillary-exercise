using System.Globalization;
using CapillaryExercise.Data;
using CapillaryExercise.Forms;
using CapillaryExercise.Hardware;
using CapillaryExercise.Models;
using CapillaryExercise.Services;

namespace CapillaryExercise;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 数据层：SQLite 单文件，放在程序目录下，启动即建表。
        var dbPath = Path.Combine(Application.StartupPath, "CapillaryData.db");
        var db = new DbHelper(dbPath);
        db.InitializeSchema();

        var capRepo = new CapillaryRepository(db);
        var logRepo = new LogRepository(db);

        // 首次启动预置演示数据，让 App 无需手工建库即可跑通各分支。
        var demoBarcode = SeedDemoDataIfEmpty(db, capRepo);

        // 硬件层：进程内 Fake，无需真实硬件（见 002-DESIGN.md 第六节）。
        // 生产环境只需把这三行换成 FINS/串口/HTTP 实现，业务逻辑零改动。
        IPlcController plc = new FakePlcController();
        IScanner scanner = new FakeScanner(demoBarcode);
        IMesService mes = new FakeMesService()
            .WithType("WO001", "CAP-A")   // 有库存 + 扫码匹配 → 领料成功
            .WithType("WO002", "CAP-B")   // 有库存但扫码不匹配 → 读码失败、仓位锁定
            .WithType("WO003", "CAP-C");  // 无库存 → 提示库存不足
                                          // 其它工单号 → MES 查询失败

        // 业务层 + UI 层：依赖通过构造函数注入。
        var pickupService = new PickupService(plc, scanner, mes, capRepo, logRepo);
        Application.Run(new PickupForm(pickupService));
    }

    /// <summary>
    /// 库存为空时预置几条演示劈刀，覆盖成功 / 读码失败 / 无库存等分支。
    /// 返回最早入库的 CAP-A 条码，供 FakeScanner 预置以演示"读码匹配成功"。
    /// </summary>
    /// <param name="db">数据库帮助类。</param>
    /// <param name="capRepo">劈刀数据访问，用于判断库存是否已存在。</param>
    /// <returns>演示用的匹配条码（最早入库的 CAP-A）。</returns>
    private static string SeedDemoDataIfEmpty(DbHelper db, ICapillaryRepository capRepo)
    {
        const string demoBarcode = "BC-A-001";

        // 已有 CAP-A 库存说明此前已预置，跳过，避免唯一索引冲突。
        if (capRepo.FindOldestByType("CAP-A") is not null)
        {
            return demoBarcode;
        }

        // CAP-A 两条：最早的 BC-A-001 用于演示成功领料（FIFO 命中最早一条）。
        InsertCapillary(db, demoBarcode, "CAP-A", "A", 5, 10, new DateTime(2026, 1, 1, 8, 0, 0));
        InsertCapillary(db, "BC-A-002", "CAP-A", "A", 5, 11, new DateTime(2026, 1, 2, 8, 0, 0));
        // CAP-B 一条：扫码器只预置了 CAP-A 的条码，取它会读码不符 → 演示锁定分支。
        InsertCapillary(db, "BC-B-001", "CAP-B", "B", 3, 7, new DateTime(2026, 1, 1, 9, 0, 0));
        // CAP-C 不预置库存 → 演示"库存不足"。

        return demoBarcode;
    }

    /// <summary>
    /// 插入一条在库（Status=0）劈刀。仅用于演示数据预置，参数化写入。
    /// </summary>
    private static void InsertCapillary(
        DbHelper db, string barcode, string type, string face, int x, int y, DateTime storedTime)
    {
        using var connection = db.CreateConnection();
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
