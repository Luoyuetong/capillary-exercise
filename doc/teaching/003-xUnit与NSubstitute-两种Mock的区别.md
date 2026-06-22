# 测试工具：xUnit 与 NSubstitute，以及它和"手写 Fake"的区别

> 教学材料 · 测试篇配套（承接 002-为什么建议提前写测试）

上一篇讲了"为什么提前写测试"。这一篇讲**用什么工具写测试**，并澄清一个最容易混淆的点：

> **我们项目里有两种"假实现"：NSubstitute 自动生成的替身，和我们手写的 Fake。它们用途不同。**

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

> 注意：上面这个手写的 `FakeScanner` 不是白写的——它正是我们项目里 **Fake（手写假实现）** 的雏形。第四节会讲，本项目用这种手写 Fake 来驱动整个程序运行（替代真实硬件），它和 NSubstitute 替身分工不同。

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

## 四、最关键的澄清：NSubstitute 替身 vs 手写 Fake

这是本篇的核心。我们项目里有**两种"假实现"**，都用来顶替接口，但用途不同：

### 甲：NSubstitute 生成的"测试替身"
- **是什么**：内存里的假对象，由 NSubstitute 在测试代码里临时生成（不用手写类）
- **活在哪**：测试进程内部，跟着测试用例生灭
- **替谁**：替 `IPlcController`、`IScanner` 等接口
- **干什么用**：**单元测试**——隔离 PickupService，单独验证它的业务逻辑对不对；每个用例可临时编排不同行为（这次返回成功、下次返回失败）

### 乙：手写的 Fake 实现（FakePlcController / FakeScanner / FakeMesService）
- **是什么**：我们自己写的类，正经实现接口（如第二节的 `FakeScanner`），有一份稳定、可复用的行为
- **活在哪**：是主程序的一部分（`Hardware/`、`Services/` 下），随 App 一起运行
- **替谁**：替**真实硬件**（真的 PLC、真的扫码器、真的 MES）
- **干什么用**：
  1. **让程序真正跑起来**——教学/演示环境没有真硬件，靠这些 Fake 让主程序能实际运行
  2. **端到端测试**——用真实的 Service + Fake + SQLite 串起完整流程，验证整体跑通

### 对比一张表

| 维度 | 甲（NSubstitute 替身） | 乙（手写 Fake） |
|------|--------------------------|------------------------|
| 形态 | 内存对象，自动生成 | 手写的类，正经实现接口 |
| 在哪 | 只在测试代码里 | 主程序的一部分，随 App 运行 |
| 行为 | 每个用例临时编排 | 一份稳定行为（可带配置） |
| 替代对象 | 接口（为隔离被测对象） | 真实硬件（为让程序能跑） |
| 用途 | 单元测试（测逻辑分支） | 跑程序 + 端到端测试 |
| 生命周期 | 每个测试用例生灭 | 跟主程序一样长 |
| 对应测试 | TC-01~TC-16 | TC-17~TC-23 |

### 一句话区分

> **甲（NSubstitute）问的是："我的业务逻辑写对了吗？"**——为每个用例临时造假货，快、灵活、隔离。
> **乙（手写 Fake）问的是："整个程序能跑通吗？"**——一份稳定的假硬件，让 App 在没有真实硬件时也能运行和演示。

---

## 五、为什么要有两种？不能只用一种吗？

这是个好问题，答案体现了"测什么"和"跑什么"的分工。

**只用甲（NSubstitute）行不行？**
- 测业务逻辑够用，但**没法让程序真正跑起来**演示——NSubstitute 替身只活在测试进程里，主程序拿不到它。
- 演示环境需要一份"插上去就能用"的假硬件，这正是手写 Fake 的活。

**只用乙（手写 Fake）行不行？**
- 跑程序够用，但用它做单元测试很别扭：每次想测一个分支（比如"读码失败时是否锁定仓位"），都得改 Fake 的代码或加配置开关，**远不如 NSubstitute 一行 `Returns(...)` 来得快**。
- 单元测试要的是"每个用例独立编排行为"，这正是 NSubstitute 的强项。

**所以分工**：
```
单元测试（甲 NSubstitute） → 大量、快速，每个用例临时编排    ← 测"逻辑对不��"
       ↓
端到端（乙 手写 Fake）     → 几个，真实 Service+Fake+DB 串联  ← 测"程序跑得通吗"
```

这正是 `004-TEST_PLAN.md` 第一节的测试策略——**不同目的，用不同的"假货"。**

---

## 六、和"生产环境"的关系

最后串一下完整图景。`IPlcController` 这个接口，在不同场景下有**三种实现**：

| 场景 | IPlcController 的实现 | 说明 |
|------|---------------------|------|
| 单元测试 | NSubstitute 生成的替身（甲） | 内存假货，测逻辑 |
| 教学/演示 | `FakePlcController`（乙，手写 Fake） | 进程内假硬件，让程序能跑 |
| 生产环境 | `FinsPlcClient` → 连**真实 PLC** | 真硬件，真通信 |

**接口抽象的威力就在这里**：业务逻辑只依赖 `IPlcController` 接口，底下换成哪种实现，它都不用改。
- 测试时换 NSubstitute 替身
- 演示时换手写 Fake
- 生产时换真实硬件

（这呼应了设计文档 `002-DESIGN.md` 第六节"3 行代码切换 Fake/生产"。）

---

## 七、小结

> - **xUnit**：测试框架，负责组织运行测试、判断对错。
> - **NSubstitute**：Mocking 库，负责造接口的假实现，隔离被测对象。两者配合写单元测试。
> - **两种假实现别混淆**：
>   - NSubstitute 替身（自动生成、只在测试里）→ 单元测试，问"逻辑对吗"
>   - 手写 Fake（正经实现、随程序运行）→ 跑程序 + 端到端，问"程序通吗"
> - **接口抽象**让同一套业务逻辑，在测试/演示/生产三种场景下，分别对接三种实现而无需改动。

---

**教学材料 · capillary-exercise 项目**
