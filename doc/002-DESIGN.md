# 概要设计文档 - 领料流程

## 一、设计目标

基于 `001-REQUIREMENTS.md` 的需求，设计领料流程的技术实现方案。

**核心原则**：
- **分层清晰**：UI、业务逻辑、数据访问、硬件通信各司其职
- **接口抽象**：硬件（PLC/扫码器/MES）通过接口抽象，支持 Mock/真实切换
- **可测试性**：业务逻辑独立于 UI 和硬件，便于单元测试

---

## 二、系统架构

### 2.1 分层架构

```
┌─────────────────────────────────────────┐
│          UI Layer (WinForms)           │  PickupForm.cs
│  - 用户输入工单号、机台号                  │
│  - 显示进度和结果                         │
└─────────────────────────────────────────┘
              ↓ 调用
┌─────────────────────────────────────────┐
│         Service Layer                  │  PickupService.cs
│  - 编排领料流程                          │
│  - 协调硬件、MES、数据库                  │
└─────────────────────────────────────────┘
       ↓              ↓              ↓
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│  Hardware    │ │  MES         │ │  Data        │
│  Interfaces  │ │  Interface   │ │  Access      │
├──────────────┤ ├──────────────┤ ├──────────────┤
│ IPlcController│ │ IMesService  │ │ Repository   │
│ IScanner     │ │              │ │              │
└──────────────┘ └──────────────┘ └──────────────┘
       ↓              ↓              ↓
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ FakePlc      │ │ FakeMes      │ │ SQLite DB    │
│ Controller   │ │ Service      │ │ (.db)        │
└──────────────┘ └──────────────┘ └──────────────┘
```

### 2.2 模块职责

| 模块 | 职责 |
|------|------|
| **PickupForm** | 接收用户输入，显示进度和结果 |
| **PickupService** | 领料流程编排：查MES → 找库存 → PLC取料 → 读码 → 上报MES → 出料 → 更新DB |
| **IPlcController** | PLC 控制接口：取料、出料、放回等动作 |
| **IScanner** | 扫码器接口：触发扫码、获取条码 |
| **IMesService** | MES 接口：查询劈刀类型、上报领料 |
| **CapillaryRepository** | 劈刀数据 CRUD：查询库存、更新状态 |
| **LogRepository** | 日志记录 |

---

## 三、数据模型

### 3.1 CapillaryInfo（劈刀信息表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int (AutoNumber) | 主键 |
| Barcode | string(50) | 条码（唯一索引） |
| CapillaryType | string(50) | 类型 |
| Face | string(1) | 仓位面 (A/B/C) |
| PosX | int | X 坐标 (1-18) |
| PosY | int | Y 坐标 (1-18) |
| StoredTime | DateTime | 入库时间（FIFO 排序依据） |
| Status | int | 0=在库，1=已领出，2=锁定 |
| WorkOrder | string(50) | 关联工单号 |
| MachineNo | string(50) | 关联机台号 |

**索引**：
- 唯一索引：Barcode
- 复合索引：(CapillaryType, Status, StoredTime) — 用于 FIFO 查询

### 3.2 OperationLog（操作日志表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int (AutoNumber) | 主键 |
| OperationType | string(20) | "Pickup" |
| Barcode | string(50) | 劈刀条码 |
| CapillaryType | string(50) | 劈刀类型 |
| Face | string(1) | 仓位面 |
| PosX | int | X 坐标 |
| PosY | int | Y 坐标 |
| WorkOrder | string(50) | 工单号 |
| MachineNo | string(50) | 机台号 |
| Result | string(20) | "Success" / "Fail" |
| Message | string(200) | 失败原因或备注 |
| Timestamp | DateTime | 时间戳 |

---

## 四、接口设计

### 4.1 IPlcController（PLC 控制接口）

```csharp
public interface IPlcController
{
    Task<bool> ConnectAsync();
    void Disconnect();
    bool IsConnected { get; }

    // 从仓位取出劈刀至读码位
    Task<bool> FetchFromSlotAsync(string face, int x, int y);
    
    // 将劈刀放入出料口
    Task<bool> OutputToPickupPortAsync();
    
    // 将劈刀放回原仓位（读码失败或MES拒绝时）
    Task<bool> ReturnToSlotAsync(string face, int x, int y);
    
    // PLC 状态变化事件
    event Action<string>? OnStatusChanged;
}
```

**实现类**：
- `FakePlcController` — 进程内模拟 PLC，按指令返回成功/失败，无需真实硬件
- 生产环境可替换为 `FinsPlcClient`（FINS 协议）

### 4.2 IScanner（扫码器接口）

```csharp
public interface IScanner
{
    Task<bool> ConnectAsync();
    void Disconnect();
    bool IsConnected { get; }

    // 触发一次扫码，返回条码；失败返回 null
    Task<string?> ScanAsync(CancellationToken ct = default);
    
    // 被动推送模式事件（可选）
    event Action<string>? OnBarcodeReceived;
}
```

**实现类**：
- `FakeScanner` — 进程内模拟扫码器，返回预置条码（可配置为模拟失败）
- 生产环境可替换为 `SerialScannerClient`（串口）

### 4.3 IMesService（MES 接口）

```csharp
public interface IMesService
{
    // 查询工单所需劈刀类型
    Task<string?> QueryCapillaryTypeAsync(string workOrder, string machineNo);
    
    // 上报领料信息，返回 MES 是否确认
    Task<bool> ReportPickupAsync(string workOrder, string machineNo, 
                                  string barcode, string capillaryType);
}
```

**实现类**：
- `FakeMesService` — 进程内模拟 MES，返回预置工单→类型映射（可配置为拒绝上报）
- 生产环境可替换为 `HttpMesClient`（调用真实 MES HTTP API，只需改 URL）

### 4.4 ICapillaryRepository（劈刀数据访问接口）

```csharp
public interface ICapillaryRepository
{
    // FIFO 查找：根据类型，查询状态=在库，按入库时间排序，取第一条
    CapillaryInfo? FindOldestByType(string capillaryType);
    
    // 根据条码查询
    CapillaryInfo? GetByBarcode(string barcode);
    
    // 更新状态（领出/锁定）
    void UpdateStatus(int id, int status, string? workOrder, string? machineNo);
}
```

### 4.5 ILogRepository（日志记录接口）

```csharp
public interface ILogRepository
{
    void Insert(OperationLog log);
}
```

---

## 五、核心流程设计

### 5.1 PickupService.ExecuteAsync() 流程

```csharp
public class PickupService
{
    private readonly IPlcController _plc;
    private readonly IScanner _scanner;
    private readonly IMesService _mes;
    private readonly ICapillaryRepository _capRepo;
    private readonly ILogRepository _logRepo;

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
            return Fail("MES查询失败或工单无效");

        // 2. FIFO 查找库存
        progress.Report($"查找库存: {capType}...");
        var cap = _capRepo.FindOldestByType(capType);
        if (cap == null)
            return Fail($"库存不足: {capType}");

        // 3. PLC 取料
        progress.Report($"PLC: 从 {cap.Face}{cap.PosX:D2}{cap.PosY:D2} 取料...");
        if (!await _plc.FetchFromSlotAsync(cap.Face, cap.PosX, cap.PosY))
            return Fail("PLC取料失败");

        // 4. 扫码验证
        progress.Report("扫码器: 读码验证...");
        var scannedBarcode = await _scanner.ScanAsync(ct);
        if (scannedBarcode == null || scannedBarcode != cap.Barcode)
        {
            progress.Report("读码失败，放回原位并锁定...");
            await _plc.ReturnToSlotAsync(cap.Face, cap.PosX, cap.PosY);
            _capRepo.UpdateStatus(cap.Id, 2, null, null); // 锁定
            LogOperation("Pickup", cap.Barcode, cap.CapillaryType, 
                         cap.Face, cap.PosX, cap.PosY, 
                         workOrder, machineNo, "Fail", "读码失败");
            return Fail("读码失败，仓位已锁定");
        }

        // 5. MES 上报
        progress.Report("MES: 上报领料信息...");
        if (!await _mes.ReportPickupAsync(workOrder, machineNo, cap.Barcode, cap.CapillaryType))
        {
            progress.Report("MES拒绝，放回原位并锁定...");
            await _plc.ReturnToSlotAsync(cap.Face, cap.PosX, cap.PosY);
            _capRepo.UpdateStatus(cap.Id, 2, null, null); // 锁定
            LogOperation("Pickup", cap.Barcode, cap.CapillaryType, 
                         cap.Face, cap.PosX, cap.PosY, 
                         workOrder, machineNo, "Fail", "MES拒绝");
            return Fail("MES拒绝，仓位已锁定");
        }

        // 6. PLC 出料
        progress.Report("PLC: 出料...");
        if (!await _plc.OutputToPickupPortAsync())
            return Fail("PLC出料失败");

        // 7. 更新数据库
        _capRepo.UpdateStatus(cap.Id, 1, workOrder, machineNo); // 已领出
        LogOperation("Pickup", cap.Barcode, cap.CapillaryType, 
                     cap.Face, cap.PosX, cap.PosY, 
                     workOrder, machineNo, "Success", "");

        return Success($"领料成功: {cap.Barcode}");
    }
}
```

### 5.2 异常处理策略

| 异常场景 | 处理方式 |
|---------|---------|
| MES 查询失败 | 返回失败，不动硬件 |
| 无库存 | 返回失败，不动硬件 |
| PLC 取料失败 | 返回失败，记录日志 |
| 读码失败 | 劈刀放回原位，仓位锁定，记录日志 |
| MES 拒绝 | 劈刀放回原位，仓位锁定，记录日志 |
| PLC 出料失败 | 返回失败，但数据库**不更新**（因为劈刀还在机器里） |

---

## 六、依赖注入与组装

在 `Program.cs` 中手动组装依赖：

```csharp
static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 数据层
        var dbPath = Path.Combine(Application.StartupPath, "CapillaryData.db");
        var db = new DbHelper(dbPath);
        var capRepo = new CapillaryRepository(db);
        var logRepo = new LogRepository(db);

        // 硬件层（进程内 Fake）
        IPlcController plc = new FakePlcController();
        IScanner scanner = new FakeScanner();
        IMesService mes = new FakeMesService();

        // 业务层
        var pickupService = new PickupService(plc, scanner, mes, capRepo, logRepo);

        // UI 层
        var mainForm = new MainForm(pickupService, plc, scanner);

        // 启动
        Application.Run(mainForm);
    }
}
```

**生产环境切换**：只需改 3 行：
```csharp
IPlcController plc = new FinsPlcClient("192.168.1.10", 9600);
IScanner scanner = new SerialScannerClient("COM3", 9600);
IMesService mes = new HttpMesClient("http://mes.company.com/api");
```
---

## 七、技术选型

| 项目 | 选择 | 说明 |
|------|------|------|
| 开发语言 | C# .NET 9 | 与参考项目保持一致 |
| UI 框架 | WinForms | 桌面应用，简单直观 |
| 数据库 | SQLite (.db) | 跨平台、零配置、单文件，教学友好 |
| PLC 通信 | 进程内 Fake | 生产可换 FINS 协议 |
| 扫码器 | 进程内 Fake | 生产可换串口 |
| MES | 进程内 Fake | 生产可换 HTTP 调用真实 MES |
| 测试框架 | xUnit + NSubstitute | 单元测试 + Mock |

---

## 八、项目结构（代码层）

```
src/
├── CapillaryExercise/              # 主程序
│   ├── Models/
│   │   ├── CapillaryInfo.cs
│   │   ├── OperationLog.cs
│   │   └── PickupResult.cs
│   ├── Hardware/
│   │   ├── IPlcController.cs
│   │   ├── FakePlcController.cs
│   │   ├── IScanner.cs
│   │   └── FakeScanner.cs
│   ├── Services/
│   │   ├── IMesService.cs
│   │   ├── FakeMesService.cs
│   │   └── PickupService.cs
│   ├── Data/
│   │   ├── DbHelper.cs
│   │   ├── ICapillaryRepository.cs
│   │   ├── CapillaryRepository.cs
│   │   ├── ILogRepository.cs
│   │   └── LogRepository.cs
│   ├── Forms/
│   │   ├── MainForm.cs
│   │   └── PickupForm.cs
│   └── Program.cs
│
└── （硬件/MES 由进程内 Fake 实现，无独立 Mock 程序）

test/
└── CapillaryExercise.Tests/
    ├── PickupServiceTests.cs       # 领料服务单元测试
    └── ...
```

---

## 九、关键设计决策

### 9.1 为什么用接口抽象硬件？
- **可测试性**：Service 层测试时，用 NSubstitute Mock 硬件接口，无需真实硬件
- **可替换性**：教学/演示环境用进程内 Fake，生产环境换成 FINS/串口/HTTP，业务逻辑零改动

### 9.2 为什么 Service 层接收 IProgress<string>？
- UI 层传入 `IProgress<string>`，Service 每一步调用 `progress.Report("xxx")`
- UI 收到进度后更新界面，业务逻辑与 UI 解耦
- 测试时可传入 Mock Progress，验证进度报告是否正确

### 9.3 为什么读码失败要锁定仓位？
- 读码失败可能是条码损坏或贴错
- 锁定后不再参与 FIFO，避免反复取到坏劈刀
- 需人工介入检查和修复

### 9.4 为什么 PLC 出料失败不更新数据库？
- 出料失败说明劈刀还在机器里（可能卡住）
- 如果标记为"已领出"，会导致库存不准
- 应保持"在库"状态，记录日志，等人工处理

---

## 十、下一步

基于本设计文档，后续工作：
1. **Issue 拆解**：将设计切分为可独立开发的 GitHub Issues
2. **测试设计**：编写测试计划和测试用例（`003-TEST_PLAN.md`）
3. **代码规范**：制定编码规范（`004-CODING_STANDARD.md`）
4. **迭代开发**：按 Issue 逐个实现、测试、合并
