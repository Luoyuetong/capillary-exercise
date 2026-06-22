# CLAUDE.md

本文件供 Claude Code 自动读取，是本项目的**稳定工作规约**（配置、约束、流程规范）。
当前进度、下一步等动态信息见 `handoff/` 目录。

---

## 项目是什么

`capillary-exercise` 是一个**流程驱动开发的练习项目**，以"劈刀（Capillary）自动存取管理系统"为案例，完整走一遍软件工程闭环：

> 需求 → 设计 → Issue 拆解 → 测试计划 → 代码规范/CI → 迭代开发

**本期范围**：只做**领料流程（Pickup Flow）**。存料、退料不在本期。

目的是体验"动手前先想清楚"的工程习惯，并演示 AI 如何在规范流程中协作。

起点素材在 `ref/劈刀发料机-参考设计文档.md`。

---

## 文档导航

| 路径 | 内容 |
|------|------|
| `doc/000-PROJECT_STARTUP.md` | 项目启动全流程记录 |
| `doc/001-REQUIREMENTS.md` | 需求（功能需求 FR-01~13 + 验收标准） |
| `doc/002-DESIGN.md` | 概要设计（架构、接口、流程、依赖注入） |
| `doc/003-ISSUE_LIST.md` | Issue 拆解清单（13 个，含依赖关系与开发顺序） |
| `doc/004-TEST_PLAN.md` | 测试计划（测试用例 TC-01~23） |
| `doc/005-CODING_STANDARD.md` | 代码规范（10 条核心） |
| `doc/teaching/` | 配套教学材料（理念讲解，开发时可不读） |
| `handoff/` | **会话交接文档（当前进度、下一步）—— 接手工作先读这里** |

---

## 迭代开发流程

按 `doc/003-ISSUE_LIST.md` 的顺序与依赖关系，**一个 Issue 一个 Issue 地做**。每个 Issue 走完整闭环：

```
1. 建分支       git checkout -b feature/issue-N-描述
2. 编码         遵守 doc/005-CODING_STANDARD.md
3. 本地测试     dotnet test（对应 doc/004-TEST_PLAN.md 的用例）
4. 提 PR        关联 Issue（Closes #N），写清做了什么、为什么
5. CI 验证      GitHub Actions 自动编译+测试（.github/workflows/build-test.yml）
6. 自审         切换"审查者"视角，对照设计和 Issue（见 teaching/006）
7. 合并         CI 通过 + 自审通过 → 合并 → 关闭 Issue
```

> 当前进行到哪个 Issue、下一步做什么，见 `handoff/`。

---

## 编码规范（必须遵守）

**所有代码必须遵守 `doc/005-CODING_STANDARD.md`。** 核心要点：
- 面向接口编程，依赖通过构造函数注入
- 异步用 async/await，方法名带 `Async` 后缀
- 硬件/外部调用的预期内失败用**返回值**（bool/可空）表达，不用异常
- 数据库一律参数化查询，禁止字符串拼接 SQL
- 业务逻辑（Service）与 UI（Form）分离；Service 用 `IProgress<string>` 报告进度
- public 成员写 XML 文档注释
- 每个公共方法配单元测试，AAA 结构，命名 `方法名_场景_预期结果`

---

## 技术栈与约束

- **语言/框架**：C# .NET 9 WinForms
- **平台**：AnyCPU（SQLite 原生库支持多架构，无需锁定位数）
- **数据库**：SQLite (.db)，`Microsoft.Data.Sqlite`，参数化查询
- **测试**：xUnit + NSubstitute（Mock 硬件接口做单元测试）
- **硬件**：进程内 Fake（`FakePlcController`/`FakeScanner`/`FakeMesService`）模拟硬件/MES，让 App 无需真实硬件即可运行
- **接口抽象**：业务只依赖 `IPlcController`/`IScanner`/`IMesService`，3 行切换 Fake/生产（见 002-DESIGN.md 第六节）

---

## Git 工作约定（重要）

- **只 commit，不 push**。需要推送时用户会主动要求——不要自动 `git push`。
- 默认分支 `main`。开发在 `feature/issue-N-描述` 分支上。
- 提交信息用祈使句，关联 Issue（`Closes #N` / `Part of #N`），结尾加：
  `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`
- `gh` CLI 在 `C:\Program Files\GitHub CLI\gh.exe`（Git Bash 的 PATH 里可能没有，用完整路径）。
- 仓库：https://github.com/Luoyuetong/capillary-exercise

---

## 环境提示

- Shell：Windows，PowerShell 主用；Bash 工具处理中文路径/文件名更稳（PowerShell 传中文易乱码）。
- GitHub 连接偶尔超时，push 失败时重试即可。
- 测试注意：SQLite 跨平台、无需额外驱动，数据库测试在 CI runner 上可原生运行。端到端测试（启动整套 Fake + 界面）若不适合在 CI 跑，可用 `[Trait("Category","E2E")]` 标记区分，CI 跑单元 + Repository 测试，本地跑全部。
