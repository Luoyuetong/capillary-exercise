# Issue 拆解清单

基于 `002-DESIGN.md`，将领料流程拆解为可独立开发的 GitHub Issues。

> **设计演进（聚焦教学）**：原计划用 3 个独立 Mock 程序（TCP/HTTP，原 #11-#13）+ 3 个 TCP/HTTP 客户端（原 #4-#6）。为聚焦工程流程、去掉系统管线的复杂度，改为 **3 个进程内 Fake**（`FakePlcController`/`FakeScanner`/`FakeMesService`），合并为单个 Issue #4；原 #5/#6/#11/#12/#13 关闭。数据库也由 Access/Jet 改为 SQLite（去掉 x86 锁）。

---

## Issue 列表

### 阶段一：基础设施（#1-#2）

| Issue | 标题 | 依赖 | 说明 |
|-------|------|------|------|
| [#1](https://github.com/Luoyuetong/capillary-exercise/issues/1) | 搭建项目基础结构 | 无 | 创建 .NET 9 解决方案、主项目、测试项目 |
| [#2](https://github.com/Luoyuetong/capillary-exercise/issues/2) | 实现数据访问层 | #1 | DbHelper(SQLite), CapillaryRepository, LogRepository |

### 阶段二：接口与 Fake 实现（#3-#4）

| Issue | 标题 | 依赖 | 说明 |
|-------|------|------|------|
| [#3](https://github.com/Luoyuetong/capillary-exercise/issues/3) | 定义硬件接口 | #1 | IPlcController, IScanner, IMesService |
| [#4](https://github.com/Luoyuetong/capillary-exercise/issues/4) | 实现 3 个进程内 Fake | #3 | FakePlcController, FakeScanner, FakeMesService |

### 阶段三：业务逻辑与界面（#7-#8）

| Issue | 标题 | 依赖 | 说明 |
|-------|------|------|------|
| [#7](https://github.com/Luoyuetong/capillary-exercise/issues/7) | 实现 PickupService | #2, #3, #4 | 领料流程编排 + 异常处理 |
| [#8](https://github.com/Luoyuetong/capillary-exercise/issues/8) | 实现 PickupForm | #7 | WinForms 界面 + 进度显示 |

### 阶段四：集成与 CI（#9-#10）

| Issue | 标题 | 依赖 | 说明 |
|-------|------|------|------|
| [#9](https://github.com/Luoyuetong/capillary-exercise/issues/9) | 端到端测试：领料流程 | #1-#8 | 真实 Service + Fake + SQLite 全程内串联 |
| [#10](https://github.com/Luoyuetong/capillary-exercise/issues/10) | 搭建 GitHub Actions CI | #1-#9 | 自动编译 + 测试 |

> **已关闭**：原 #5（TcpScannerClient）、#6（HttpMesClient）、#11/#12/#13（独立 Mock 程序）——并入 #4 的进程内 Fake。

---

## 开发顺序建议

### 第一批（可并行）
- #1: 搭建项目结构
- #2: 数据访问层（SQLite）
- #3: 定义硬件接口

### 第二批（依赖 #3）
- #4: 实现 3 个进程内 Fake

### 第三批（依赖第二批）
- #7: PickupService

### 第四批（依赖第三批）
- #8: PickupForm

### 第五批（依赖所有）
- #9: 端到端测试
- #10: CI 配置

---

## Issue 与设计文档映射

| Issue | 对应设计章节 |
|-------|-------------|
| #1 | 第八节：项目结构 |
| #2 | 第三节：数据模型 + 第四节4.4-4.5 |
| #3 | 第四节4.1-4.3（三个接口） |
| #4 | 第四节4.1-4.3（Fake 实现） |
| #7 | 第五节5.1 |
| #8 | 第二节2.1（需求） |
| #9 | 第六节：依赖注入（端到端组装） |
| #10 | 第六节：依赖注入 |

---

## 验收标准汇总

每个 Issue 完成时：
- [ ] 功能实现符合设计文档
- [ ] 单元测试覆盖核心逻辑
- [ ] 代码遵守规范（待定义 `004-CODING_STANDARD.md`）
- [ ] 通过 CI 编译和测试
- [ ] PR 经过 Code Review（自审）

---

## 预估工作量

| 阶段 | Issues | 预估 |
|------|--------|------|
| 基础设施 | #1-#2 | 2-3h |
| 接口与 Fake | #3-#4 | 2-3h |
| 业务与界面 | #7-#8 | 3-4h |
| 集成与 CI | #9-#10 | 2-3h |
| **总计** | 8 Issues | **9-13h** |

> 注：预估基于单人开发、使用 AI 辅助的情况。原 13 Issue 因聚焦教学（进程内 Fake 替代独立 Mock 程序）精简为 8 个。
