# 测试工具：xUnit 与 NSubstitute，以及它们和"真 Mock 程序"的关系

> 教学材料 · 测试篇配套（承接 002-为什么建议提前写测试）

上一篇讲了"为什么提前写测试"。这一篇讲**用什么工具写测试**，并澄清一个最容易混淆的点：

> **我们项目里有两种东西都叫 "Mock"，但它们是完全不同的两回事。**

---

## 一、xUnit 是什么

**xUnit 是一个测试框架（NuGet 工具库）。**

它解决的问题是：**怎么把"测试"这件事，变成可以自动运行、自动判断对错的代码。**

没有测试框架时，你想验证一个函数，得自己写个 `Main` 函数打印结果，再用眼睛看对不对。xUnit 把这件事标准化了：

```csharp
public class CalculatorTests
{
    [Fact]                              // 标记：这是一个测试用例
    public void Add_TwoNumbers_ReturnsSum()
    {
        var result = 2 + 3;
        Assert.Equal(5, result);        // 断言：期望等于 5，不等就报错
    }
}
```

xUnit 提供三样核心东西：
- **`[Fact]` / `[Theory]`**：标记哪些方法是测试用例
- **`Assert.xxx`**：断言——声明"我期望的结果是什么"，不符合就判定测试失败
- **测试运行器**：自动找出所有测试、批量运行、汇总报告（多少通过、多少失败）

> 一句话：**xUnit 负责"组织和运行测试，自动判断对错"。**

.NET 里同类的还有 MSTest、NUnit，作用类似。我们选 xUnit（版本 2.9.2）。

---

## 二、NSubstitute 是什么

**NSubstitute 是一个 Mocking 库（NuGet 工具库），帮你自动生成接口的"假实现"。**

它解决的问题是：**测试一个类时，怎么把它依赖的其他东西"换成假的"，从而单独测试它。**

### 为什么需要它

测试 `PickupService` 时，它依赖 5 个东西：PLC、扫码器、MES、两个 Repository。但测业务逻辑时，我们**不想真的连硬件、连数据库**（慢、不稳定、还要启动别的程序）。

于是用 NSubstitute 造一批"假货"顶上去，**而且我们能指定假货的行为**：

```csharp
// 让 NSubstitute 自动生成一个假的扫码器（不用手写类）
var scanner = Substitute.For<IScanner>();

// 编排它的行为：被调用时返回 "BC001"
scanner.ScanAsync(Arg.Any<CancellationToken>()).Returns("BC001");

// 想测"读码失败"？换个编排，让它返回 null
scanner.ScanAsync(Arg.Any<CancellationToken>()).Returns((string?)null);
```

### 不用它会怎样

你得**自己手写**一个假类，实现接口的每个方法：

```csharp
public class FakeScanner : IScanner
{
    public Task<string?> ScanAsync(CancellationToken ct = default)
        => Task.FromResult<string?>("BC001");   // 写死
    // ... 还要实现 ConnectAsync、Disconnect、IsConnected、事件 ...
}
```

每个接口、每种场景都写一个类，非常啰嗦。NSubstitute 让你**一行生成、随时改行为**。

> 一句话：**NSubstitute 负责"造假货替身，隔离被测对象的依赖"。**

.NET 里同类的还有 Moq、FakeItEasy。我们选 NSubstitute（版本 5.3.0），因为语法简洁易读。

---

## 三、两者的关系：配合使用

xUnit 和 NSubstitute 不是竞争关系，是**搭档**：

| 工具 | 角色 | 类比 |
|------|------|------|
| **xUnit** | 组织、运行测试，判断对错 | 考试的"考场和评分规则" |
| **NSubstitute** | 造假的依赖，隔离被测对象 | 考试用的"模拟道具" |

一个典型的 PickupService 单元测试，两者一起上场：

```csharp
[Fact]                                          // xUnit：标记测试
public async Task Pickup_ReadFails_LocksSlot()
{
    // 准备：NSubstitute 造假货
    var plc = Substitute.For<IPlcController>();
    var scanner = Substitute.For<IScanner>();
    var mes = Substitute.For<IMesService>();
    var capRepo = Substitute.For<ICapillaryRepository>();
    var logRepo = Substitute.For<ILogRepository>();

    // 编排：MES 返回类型，库存有货，但扫码失败
    mes.QueryCapillaryTypeAsync("WO001", "M01").Returns("CAP-A");
    capRepo.FindOldestByType("CAP-A").Returns(new CapillaryInfo { /* ... */ });
    plc.FetchFromSlotAsync(...).Returns(true);
    scanner.ScanAsync(Arg.Any<CancellationToken>()).Returns((string?)null);  // 读码失败

    var service = new PickupService(plc, scanner, mes, capRepo, logRepo);

    // 执行
    var result = await service.ExecuteAsync("WO001", "M01", progress);

    // 验证：xUnit 断言
    Assert.False(result.Success);                       // 应该失败
    await plc.Received().ReturnToSlotAsync(...);         // 应该调用了"放回原位"
    capRepo.Received().UpdateStatus(Arg.Any<int>(), 2, null, null);  // 应该锁定
}
```

这正是测试计划 `004-TEST_PLAN.md` 里的 **TC-06**。

---

## 四、最关键的澄清：两种 "Mock" 是不同的东西

这是本篇的核心。我们项目里，"Mock" 这个词指了**两个完全不同层次的东西**：

### Mock 甲：NSubstitute 生成的"测试替身"
- **是什么**：内存里的假对象，由 NSubstitute 在测试代码里临时生成
- **活在哪**：测试进程内部，跟着测试用例生灭
- **替谁**：替 `IPlcController`、`IScanner` 等接口
- **干什么用**：**单元测试**——隔离 PickupService，单独验证它的业务逻辑对不对

### Mock 乙：独立的 Mock 程序（MockPLC / MockScanner / MockMES）
- **是什么**：独立的可执行程序（.exe），有真实的 TCP/HTTP 服务端
- **活在哪**：独立进程，自己启动、自己运行，有界面
- **替谁**：替**真实硬件**（真的 PLC、真的扫码器、真的 MES 服务器）
- **干什么用**：
  1. **集成/端到端测试**——验证客户端和服务端的真实通信路径通不通
  2. **跑起整个程序**——教学/演示环境没有真硬件，靠它们让主程序能实际运行

### 对比一张表

| 维度 | Mock 甲（NSubstitute 替身） | Mock 乙（独立 Mock 程序） |
|------|--------------------------|------------------------|
| 形态 | 内存对象 | 独立 .exe 程序 |
| 进程 | 测试进程内 | 独立进程 |
| 通信 | 直接方法调用 | 真实 TCP / HTTP |
| 替代对象 | 接口（代码层） | 真实硬件（系统层） |
| 用途 | 单元测试（测逻辑） | 集成/端到端测试 + 运行程序 |
| 速度 | 极快（无网络） | 较慢（走网络） |
| 生命周期 | 每个测试用例生灭 | 手动启动，长期运行 |
| 对应测试 | TC-01~TC-16 | TC-17~TC-23 |

### 一句话区分

> **Mock 甲（NSubstitute）问的是："我的业务逻辑写对了吗？"**——在代码层面造假，快、隔离。
> **Mock 乙（独立程序）问的是："整个系统连起来能跑通吗？"**——在系统层面替代硬件，验证真实通信，还能让程序实际运行。

---

## 五、为什么要有两种？不能只用一种吗？

这是个好问题，答案体现了测试分层的思想。

**只用 Mock 甲（NSubstitute）行不行？**
- 不行。它只能验证"业务逻辑对"，但**永远测不到真实的网络通信**——TcpPlcClient 发的命令格式对不对、MockPLC 解析得对不对，它一概不知道。
- 而且它没法让你**把程序真正跑起来**演示。

**只用 Mock 乙（独立程序）行不行？**
- 也不行。每次测一个小逻辑分支都要启动三个程序、连网络，**又慢又脆**。
- 而且"测 PickupService 在读码失败时是否锁定仓位"这种逻辑分支，用独立程序很难精确控制和复现。

**所以分层**：
```
单元测试（Mock 甲）  → 大量、快速，覆盖每条业务分支     ← 测"逻辑对不对"
       ↓
集成测试（Mock 乙）  → 少量，验证真实通信路径           ← 测"通信通不通"
       ↓
端到端（Mock 乙）    → 几个，验证整体流程跑通           ← 测"系统连得起来"
```

这正是 `004-TEST_PLAN.md` 第一节的测试策略——**不同层次的测试，用不同的"假货"，回答不同的问题。**

---

## 六、和"生产环境"的关系

最后串一下完整图景。`IPlcController` 这个接口，在不同场景下有**三种实现**：

| 场景 | IPlcController 的实现 | 说明 |
|------|---------------------|------|
| 单元测试 | NSubstitute 生成的替身（Mock 甲） | 内存假货，测逻辑 |
| 教学/演示 | `TcpPlcClient` → 连 **MockPLC**（Mock 乙） | 真通信，但对端是模拟程序 |
| 生产环境 | `FinsPlcClient` → 连**真实 PLC** | 真通信，真硬件 |

**接口抽象的威力就在这里**：业务逻辑只依赖 `IPlcController` 接口，底下换成哪种实现，它都不用改。
- 测试时换 Mock 甲
- 演示时换 Mock 乙
- 生产时换真实硬件

（这呼应了设计文档 `002-DESIGN.md` 第六节"3 行代码切换 Mock/生产"。）

---

## 七、小结

> - **xUnit**：测试框架，负责组织运行测试、判断对错。
> - **NSubstitute**：Mocking 库，负责造接口的假实现，隔离被测对象。两者配合写单元测试。
> - **两种 Mock 别混淆**：
>   - NSubstitute 替身（代码层假货）→ 单元测试，问"逻辑对吗"
>   - 独立 Mock 程序（系统层假硬件）→ 集成/端到端 + 跑程序，问"系统通吗"
> - **接口抽象**让同一套业务逻辑，在测试/演示/生产三种场景下，分别对接三种实现而无需改动。

---

**教学材料 · capillary-exercise 项目**
