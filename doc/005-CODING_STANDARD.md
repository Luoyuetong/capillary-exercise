# 代码规范（精简版·10 条核心）

> 配套：`002-DESIGN.md`（设计）
> 用途：约束人和 AI 的编码风格。**这是演示版，不求完美，可随项目迭代完善。**
> AI 协作：本规范会在 `CLAUDE.md` 中被引用，AI 生成代码须遵守。

---

## 约定优先级

> 规范是为了**减少决策、统一风格**，不是束缚。遇到规范没覆盖的情况，参照 [.NET 官方 C# 编码约定](https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions)，并保持与现有代码一致。

---

## 1. 命名：见名知意，遵循 C# 惯例

| 元素 | 规则 | 示例 |
|------|------|------|
| 接口 | `I` 前缀 + PascalCase | `IPlcController` |
| 类 / 方法 / 属性 | PascalCase | `PickupService`、`ExecuteAsync` |
| 私有字段 | `_` 前缀 + camelCase | `_scanner`、`_capRepo` |
| 局部变量 / 参数 | camelCase | `workOrder`、`barcode` |
| 常量 | PascalCase | `MaxRetryCount` |
| 异步方法 | 以 `Async` 结尾 | `ScanAsync`、`ConnectAsync` |

**禁止**：拼音命名、无意义缩写（`a`、`tmp`、`data1`）。

## 2. 面向接口编程

业务逻辑只依赖**接口**，不依赖具体实现。

```csharp
// ✓ 正确：依赖接口
public PickupService(IPlcController plc, IScanner scanner) { ... }

// ✗ 错误：依赖具体实现
public PickupService(FakePlcController plc) { ... }
```

**理由**：可测试（Mock 替身）、可替换（Fake/生产切换）。见 `003-...NSubstitute与手写Fake` 教学材料。

## 3. 依赖通过构造函数注入

依赖在构造函数传入并存入只读字段，不在方法内部 `new` 出依赖。

```csharp
private readonly IScanner _scanner;
public PickupService(IScanner scanner) => _scanner = scanner;
```

**禁止**：在类内部 `new FakeScanner(...)`（这会让依赖无法替换、无法测试）。

## 4. 异步用 async/await，方法名带 Async

所有 I/O 操作（网络、文件、数据库）用异步，避免阻塞。

```csharp
public async Task<bool> FetchFromSlotAsync(string face, int x, int y)
{
    return await SendCommandAsync($"CMD:FETCH:{face},{x},{y}");
}
```

**禁止**：`.Result`、`.Wait()`（会死锁）；忙等待自旋。

## 5. 硬件/外部调用失败用返回值，不用异常

硬件通信、外部服务的"预期内失败"（连接失败、读码失败、超时）通过**返回值**表达（`bool` 或可空类型），调用方显式判断。

```csharp
// 预期内失败：返回 false / null
Task<bool> FetchFromSlotAsync(...);      // 失败返回 false
Task<string?> ScanAsync(...);            // 失败返回 null
```

**异常只用于"非预期"错误**（如编程错误、配置缺失）。数据库操作的底层错误可向上抛。

**理由**：硬件失败是业务流程的正常分支（见设计 5.2 异常处理），不是异常情况。

## 6. 数据库一律用参数化查询

**禁止字符串拼接 SQL**（注入风险）。

```csharp
// ✓ 正确：参数化
cmd.CommandText = "SELECT * FROM CapillaryInfo WHERE Barcode = $barcode";
cmd.Parameters.AddWithValue("$barcode", barcode);

// ✗ 错误：拼接
cmd.CommandText = $"SELECT * FROM CapillaryInfo WHERE Barcode = '{barcode}'";
```

## 7. 业务逻辑与 UI 分离

Service 层封装业务流程��Form 只负责界面交互。

- **Form**：收集输入、调用 Service、显示结果/进度
- **Service**：编排流程、调用硬件和数据层、返回结果对象

**禁止**：在按钮事件里直接写业务逻辑、直接调用硬件或数据库。

## 8. 进度通过 IProgress 报告，不在 Service 里碰 UI

Service 用 `IProgress<string>` 报告进度，由 UI 层决定怎么显示。

```csharp
public async Task<PickupResult> ExecuteAsync(
    string workOrder, string machineNo,
    IProgress<string> progress, CancellationToken ct = default)
{
    progress.Report("MES: 查询劈刀类型...");
    // ...
}
```

**禁止**：在 Service 里直接操作 `TextBox`、`MessageBox`（破坏分层、无法测试）。

## 9. public 成员写 XML 文档注释

所有 public 接口、类、方法写 `///` 注释，说明用途；关键参数和返回值说明清楚。

```csharp
/// <summary>
/// 执行一次完整的领料流程。
/// </summary>
/// <param name="workOrder">工单号</param>
/// <returns>领料结果，含成功标志和说明</returns>
public async Task<PickupResult> ExecuteAsync(string workOrder, ...) { ... }
```

注释讲**为什么/做什么**，不复述代码字面。

## 10. 每个公共方法配单元测试，遵循 AAA 结构

业务逻辑（Service、Repository）的 public 方法须有单元测试，覆盖正常 + 异常分支。测试用 **Arrange-Act-Assert** 三段式，方法名表达场景。

```csharp
[Fact]
public async Task ExecuteAsync_ReadFails_ReturnsFailAndLocksSlot()
{
    // Arrange：准备 Mock 和数据
    // Act：调用被测方法
    // Assert：验证结果和交互
}
```

命名格式：`方法名_场景_预期结果`。测试用例对应 `004-TEST_PLAN.md`。

---

## 附：提交信息规范

- 用**祈使句**说明做了什么：`Add PickupService` 、`Fix FIFO ordering bug`
- 关联 Issue：在描述里写 `Closes #7` 或 `Part of #2`
- AI 协作标注：`Co-Authored-By: Claude ...`

---

## 这份规范怎么用

1. **写代码前**：扫一眼，心里有数
2. **AI 协作**：`CLAUDE.md` 引用本文件，AI 自动遵守
3. **Code Review**：对照检查（但别纠结格式细节，重点看 2/5/7 这类设计性规则）
4. **迭代完善**：发现新的共性问题，补充进来——它是活的文档
