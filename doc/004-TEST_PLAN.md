# 测试计划 - 领料流程

> 配套文档：`001-REQUIREMENTS.md`（需求）、`002-DESIGN.md`（设计）
> 教学呼应：`teaching/030-为什么建议提前写测试.md`

本文档在编码**之前**完成，目的是先定义"什么算做对"，把验收标准锁成约定。

---

## 一、测试策略

### 1.1 测试分层

| 层次 | 测试对象 | 方式 | 是否需要硬件 |
|------|---------|------|------------|
| **单元测试** | PickupService 业务逻辑 | xUnit + NSubstitute（Mock 所有依赖） | 否 |
| **单元测试** | Repository 数据访问 | xUnit + 测试数据库 | 否（用临时 SQLite .db） |
| **单元测试** | Fake 实现 | xUnit 直接验证 Fake 行为 | 否（进程内） |
| **端到端测试** | 完整领料流程 | 真实 Service + Fake + SQLite 全程内串联 | 否（全进程内） |

### 1.2 测试重点

**核心是 PickupService**——它编排了整个领料流程，包含所有业务判断和异常处理。这是单元测试的主战场。

**关键原则**：用 NSubstitute Mock 掉 IPlcController / IScanner / IMesService / Repository，**单独测试业务逻辑的每一条分支**，不依赖真实硬件。

### 1.3 覆盖率目标

| 模块 | 目标 |
|------|------|
| PickupService | ≥ 90%（核心逻辑，必须高覆盖） |
| Repository | ≥ 80% |
| Fake 实现 | ≥ 60%（逻辑简单，端到端测试也会覆盖） |
| 整体 | ≥ 75% |

---

## 二、PickupService 单元测试用例

PickupService 的流程有 7 步，每一步都可能成功或失败。测试用例覆盖**正常路径 + 每个失败分支**。

### 2.1 正常流程

**TC-01：完整领料成功**
- **前置**：MES 返回类型 "CAP-A"；库存有 1 个 CAP-A（条码 BC001，仓位 A,5,10）；PLC 取料成功；扫码返回 BC001（匹配）；MES 上报成功；PLC 出料成功
- **操作**：ExecuteAsync("WO001", "M01", progress)
- **预期**：
  - 返回 Success
  - 劈刀状态更新为 1（已领出），记录 WorkOrder=WO001, MachineNo=M01
  - 日志记录一条 Pickup / Success
  - progress 依次报告 7 个步骤
- **对应需求**：FR-04~FR-10、验收标准 4.1

### 2.2 MES 查询失败分支

**TC-02：MES 查询返回 null（工单无效）**
- **前置**：MES.QueryCapillaryTypeAsync 返回 null
- **预期**：
  - 返回 Fail，原因含"MES查询失败"或"工单无效"
  - **不调用任何 PLC 动作**（硬件未动）
  - 日志记录 Fail（可选）
- **对应需求**：FR-04 异常

### 2.3 库存查找失败分支

**TC-03：无库存**
- **前置**：MES 返回 "CAP-A"；Repository.FindOldestByType("CAP-A") 返回 null
- **预期**：
  - 返回 Fail，原因含"库存不足"
  - **不调用任何 PLC 动作**
- **对应需求**：FR-06 异常

**TC-04：FIFO 顺序正确性**
- **前置**：库存有 3 个 CAP-A，入库时间不同
- **预期**：选中的是 **StoredTime 最早**的那一个
- **对应需求**：FR-06（FIFO）
- **备注**：这条更适合放在 Repository 测试（见 3.2），此处验证 Service 正确使用了返回值

### 2.4 PLC 取料失败分支

**TC-05：PLC 取料失败**
- **前置**：MES 返回类型；有库存；PLC.FetchFromSlotAsync 返回 false
- **预期**：
  - 返回 Fail，原因含"PLC取料失败"
  - **不更新数据库**（劈刀状态保持在库）
  - 不调用扫码、MES上报、出料
- **对应需求**：FR-08 异常

### 2.5 读码失败分支（重点）

**TC-06：扫码返回 null（读码失败）**
- **前置**：取料成功；Scanner.ScanAsync 返回 null
- **预期**：
  - 调用 PLC.ReturnToSlotAsync（放回原位）
  - 劈刀状态更新为 2（锁定）
  - 返回 Fail，原因含"读码失败"
  - 日志记录 Fail / "读码失败"
- **对应需求**：FR-09、FR-11、验收标准 4.2

**TC-07：扫码条码与库存不匹配**
- **前置**：取料成功；库存条码 BC001；Scanner 返回 BC999（不匹配）
- **预期**：同 TC-06（放回 + 锁定 + Fail）
- **对应需求**：FR-09、FR-11

### 2.6 MES 上报拒绝分支（重点）

**TC-08：MES 上报被拒绝**
- **前置**：取料成功；读码匹配；MES.ReportPickupAsync 返回 false
- **预期**：
  - 调用 PLC.ReturnToSlotAsync（放回原位）
  - 劈刀状态更新为 2（锁定）
  - 返回 Fail，原因含"MES拒绝"
  - 日志记录 Fail / "MES拒绝"
- **对应需求**：FR-05、FR-12、验收标准 4.2

### 2.7 PLC 出料失败分支

**TC-09：PLC 出料失败**
- **前置**：取料成功；读码匹配；MES 上报成功；PLC.OutputToPickupPortAsync 返回 false
- **预期**：
  - 返回 Fail，原因含"PLC出料失败"
  - **数据库不更新为已领出**（因为劈刀还在机器里，见设计 5.2）
  - 日志记录 Fail
- **对应需求**：FR-10、设计决策 9.4

### 2.8 进度报告验证

**TC-10：progress 报告内容**
- **前置**：正常流程
- **预期**：IProgress.Report 被依次调用，内容覆盖各步骤（MES查询 → 查库存 → 取料 → 读码 → 上报 → 出料）
- **对应需求**：FR-02、NFR-07

---

## 三、Repository 单元测试用例

### 3.1 测试环境
- 使用临时 SQLite 文件（每个测试用例前重建，保证隔离）
- 或使用 SQLite 内存数据库（`Data Source=:memory:`，更快）

### 3.2 CapillaryRepository

**TC-11：FindOldestByType 返回最早入库的在库劈刀**
- **前置**：插入 3 条 CAP-A（StoredTime 不同，Status=0）
- **预期**：返回 StoredTime 最早的那条
- **对应需求**：FR-06

**TC-12：FindOldestByType 忽略非在库状态**
- **前置**：CAP-A 有 2 条，一条 Status=0，一条 Status=1（已领出，且更早）
- **预期**：返回 Status=0 的那条（跳过已领出的）
- **对应需求**：FR-06

**TC-13：FindOldestByType 忽略锁定仓位**
- **前置**：CAP-A 有 2 条，一条 Status=0，一条 Status=2（锁定，且更早）
- **预期**：返回 Status=0 的那条（跳过锁定的）
- **对应需求**：FR-11（锁定仓位不参与 FIFO）

**TC-14：FindOldestByType 无匹配返回 null**
- **前置**：库存无 CAP-A
- **预期**：返回 null
- **对应需求**：FR-06 异常

**TC-15：UpdateStatus 正确更新状态和关联工单**
- **前置**：插入一条劈刀
- **操作**：UpdateStatus(id, 1, "WO001", "M01")
- **预期**：该记录 Status=1, WorkOrder=WO001, MachineNo=M01
- **对应需求**：FR-07

### 3.3 LogRepository

**TC-16：Insert 正确写入日志**
- **操作**：Insert 一条 OperationLog
- **预期**：数据库中能查到该条日志，字段一致
- **对应需求**：FR-13

---

## 四、Fake 实现单元测试用例

### 4.1 前置
进程内直接 new 出 Fake，无需启动任何外部程序。

**TC-17：FakePlcController 取料返回成功**
- **操作**：ConnectAsync → FetchFromSlotAsync("A", 5, 10)
- **预期**：连接成功，返回 true（可配置为模拟失败时返回 false）
- **对应需求**：FR-08

**TC-18：FakeScanner 返回预置条码**
- **前置**：Fake 预置条码 BC001
- **操作**：ScanAsync()
- **预期**：返回 "BC001"（可配置为模拟失败时返回 null）
- **对应需求**：FR-09

**TC-19：FakeMesService 查询类型**
- **前置**：Fake 预置 WO001 → CAP-A
- **操作**：QueryCapillaryTypeAsync("WO001", "M01")
- **预期**：返回 "CAP-A"
- **对应需求**：FR-04

---

## 五、端到端测试用例

### 5.1 前置
进程内组装真实 PickupService + Fake + SQLite，预置数据库与 Fake 行为。

**TC-20：端到端 - 正常领料**
- **前置**：FakeMes 预置 WO001→CAP-A；数据库有 CAP-A（BC001, A,5,10）；FakeScanner 预置 BC001
- **操作**：界面输入 WO001 / M01 → 开始领料
- **预期**：流程走通，界面显示成功，数据库状态更新，日志记录
- **对应需求**：验收标准 4.1

**TC-21：端到端 - 读码失败**
- **前置**：同上，但 FakeScanner 设为"模拟失败"或返回错误条码
- **预期**：劈刀放回，仓位锁定，界面提示读码失败
- **对应需求**：验收标准 4.2

**TC-22：端到端 - MES 拒绝**
- **前置**：同 TC-20，但 FakeMes 设为"拒绝上报"
- **预期**：劈刀放回，仓位锁定，界面提示 MES 拒绝
- **对应需求**：验收标准 4.2

**TC-23：端到端 - 无库存**
- **前置**：FakeMes 预置 WO002→CAP-B；数据库无 CAP-B
- **预期**：界面提示库存不足，硬件不动作
- **对应需求**：验收标准 4.2

---

## 六、测试用例与需求/Issue 映射

| 测试用例 | 验证需求 | 关联 Issue |
|---------|---------|-----------|
| TC-01 | FR-04~10, 验收4.1 | #7 |
| TC-02 | FR-04 异常 | #7 |
| TC-03, TC-04 | FR-06 | #7 |
| TC-05 | FR-08 异常 | #7 |
| TC-06, TC-07 | FR-09, FR-11 | #7 |
| TC-08 | FR-05, FR-12 | #7 |
| TC-09 | FR-10, 设计9.4 | #7 |
| TC-10 | FR-02, NFR-07 | #7 |
| TC-11~TC-15 | FR-06, FR-07, FR-11 | #2 |
| TC-16 | FR-13 | #2 |
| TC-17 | FR-08 | #4 |
| TC-18 | FR-09 | #4 |
| TC-19 | FR-04 | #4 |
| TC-20~TC-23 | 验收4.1, 4.2 | #9 |

**覆盖检查**：13 条功能需求（FR-01~FR-13）均有测试用例覆盖。
> FR-01（输入工单/机台）、FR-03（显示结果）属 UI 交互，主要靠端到端 TC-20 和手工测试验证。

---

## 七、测试用例汇总

| 类别 | 用例数 | 编号 |
|------|--------|------|
| PickupService 单元测试 | 10 | TC-01 ~ TC-10 |
| Repository 单元测试 | 6 | TC-11 ~ TC-16 |
| Fake 实现单元测试 | 3 | TC-17 ~ TC-19 |
| 端到端测试 | 4 | TC-20 ~ TC-23 |
| **总计** | **23** | |

---

## 八、测试先行的体现

本测试计划在**任何业务代码编写之前**完成，价值：

1. **定义了"做对的标准"**：23 个用例就是 23 条"完成"的精确定义
2. **暴露了设计盲区**：写用例时确认了"出料失败不更新数据库"（TC-09）这类边界——逼着把设计 9.4 想清楚
3. **锁成约定**：开发 #7（PickupService）时，必须让 TC-01~TC-10 全过才算完成
4. **指导开发顺序**：TC 映射到 Issue，每个 Issue 完成的标准明确

> 这正是 `teaching/030-为什么建议提前写测试.md` 讲的：测试先行，是思考工具，也是约定。
