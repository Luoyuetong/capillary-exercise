using CapillaryExercise.Data;
using CapillaryExercise.Demo;
using CapillaryExercise.Forms;
using CapillaryExercise.Hardware;
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
        // seeder 同时供界面"重置演示数据"按钮复用，使会改库的场景可反复演示。
        var seeder = new DemoDataSeeder(db);
        seeder.SeedIfEmpty();

        // 硬件层：进程内 Fake，无需真实硬件（见 002-DESIGN.md 第六节）。
        // 生产环境只需把这三行换成 FINS/串口/HTTP 实现，业务逻辑零改动。
        IPlcController plc = new FakePlcController();
        IScanner scanner = new FakeScanner(DemoDataSeeder.DemoBarcode);
        IMesService mes = new FakeMesService()
            .WithType("WO001", "CAP-A")   // 有库存 + 扫码匹配 → 领料成功
            .WithType("WO002", "CAP-B")   // 有库存但扫码不匹配 → 读码失败、仓位锁定
            .WithType("WO003", "CAP-C");  // 无库存 → 提示库存不足
                                          // 其它工单号 → MES 查询失败

        // 业务层 + UI 层：依赖通过构造函数注入。
        // 内置演示案例与 MES/库存配置一一对应，点选即自动填好工单、复现分支。
        var pickupService = new PickupService(plc, scanner, mes, capRepo, logRepo);
        Application.Run(new PickupForm(pickupService, BuildDemoScenarios(), seeder.Reset));
    }

    /// <summary>
    /// 构建内置演示案例列表，与 <see cref="Main"/> 里的 MES/库存配置一一对应。
    /// 顺序按"先成功后失败"排列，便于现场演示。
    /// </summary>
    private static IReadOnlyList<DemoScenario> BuildDemoScenarios() =>
    [
        new("✅ 成功领料（WO001）", "WO001", "M1", "完整流程跑通，结果绿色：领料成功"),
        new("❌ 库存不足（WO003）", "WO003", "M1", "MES 能查到类型 CAP-C，但库里无货 → 库存不足"),
        new("❌ 读码失败·仓位锁定（WO002）", "WO002", "M1", "取出 CAP-B 但扫码不符 → 放回原位并锁定仓位"),
        new("❌ 工单无效（WO999）", "WO999", "M1", "MES 查不到此工单 → 第一步即被拦下"),
    ];
}
