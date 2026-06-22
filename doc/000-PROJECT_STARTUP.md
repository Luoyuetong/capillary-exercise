# 项目启动全流程记录

本文档记录了 `capillary-exercise` 项目从零开始的完整启动过程，展示"需求 → 设计 → Issue 拆解"的工程实践。

---

## 一、项目背景

### 1.1 目标
以劈刀（Capillary）自动存取管理系统为案例，体验**流程驱动开发**：
- 设计先于编码
- 测试驱动思维
- 用 Git 流程（Issue / PR / CI）倒逼开发习惯
- AI 全程协作（从设计到测试到编码）

### 1.2 范围
本期聚焦**领料流程（Pickup Flow）**：
1. 输入工单号 + 机台号
2. MES 查询所需劈刀类型
3. 系统 FIFO 查找库存
4. PLC 取出劈刀并读码验证
5. 上报 MES
6. 出料

### 1.3 参考资料
基于 [002-XinJi_AI_Demo](https://github.com/Luoyuetong/002-XinJi_AI_Demo) 的 `doc/design.md` 提炼需求，但从零实现。

---

## 二、环境准备

### 2.1 工具安装
1. **Git**：已配置用户 `Yuetong Luo <ytluo@hfut.edu.cn>`
2. **GitHub CLI (gh)**：通过 `winget install GitHub.cli` 安装
3. **认证**：`gh auth login` 完成 GitHub 认证（账号 `@Luoyuetong`）

### 2.2 工作区创建
```powershell
cd F:\
mkdir capillary-exercise
cd capillary-exercise
git init
git config user.name "Yuetong Luo"
git config user.email "ytluo@hfut.edu.cn"
```

### 2.3 GitHub 仓库创建
```powershell
gh repo create capillary-exercise --public \
  --description "Capillary management system - pickup flow exercise" \
  --source=. --remote=origin
```

仓库地址：https://github.com/Luoyuetong/capillary-exercise

---

## 三、第一步：搭建目录结构

### 3.1 创建目录
```
capillary-exercise/
├── doc/               # 文档（编号：001-XXX.md, 002-XXX.md...）
├── src/               # 源代码
├── test/              # 测试代码
├── .github/workflows/ # CI/CD 配置
└── README.md
```

### 3.2 文档编号规则
- `001-REQUIREMENTS.md` — 需求文档
- `002-DESIGN.md` — 概要设计
- `003-ISSUE_LIST.md` — Issue 拆解清单
- `004-TEST_PLAN.md` — 测试计划（待完成）
- `005-CODING_STANDARD.md` — 代码规范（待完成）

### 3.3 初始提交
```bash
git add README.md
git commit -m "Initial commit: project setup"
git branch -M main
git push -u origin main
```

**Commit**: `02928d7` - Initial commit

---

## 四、第二步：整理需求文档

### 4.1 素材来源
从 `002-XinJi_AI_Demo/doc/design.md` 中提取领料流程的业务需求，剥离技术实现细节。

### 4.2 需求文档结构
```
doc/001-REQUIREMENTS.md
├── 一、业务背景
├── 二、功能需求（FR-01 ~ FR-13）
│   ├── 2.1 领料操作界面
│   ├── 2.2 MES 集成
│   ├── 2.3 库存管理
│   ├── 2.4 硬件交互
│   └── 2.5 异常处理
├── 三、非功能需求（NFR-01 ~ NFR-10）
├── 四、验收标准
├── 五、边界与约束
└── 六、术语表
```

### 4.3 核心需求
- **13 条功能需求**：覆盖 UI、MES 集成、库存管理、硬件交互、异常处理
- **10 条非功能需求**：性能、可靠性、可用性、可维护性
- **验收标准**：正常流程 + 3 类异常流程（无库存、读码失败、MES 拒绝）+ 数据完整性

### 4.4 提交
```bash
git add doc/001-REQUIREMENTS.md
git commit -m "Add requirements document for pickup flow"
git push
```

**Commit**: `880c40b` - Add requirements document

---

## 五、第三步：编写概要设计

### 5.1 设计目标
基于需求文档，设计技术实现方案：
- **分层架构**：UI → Service → Hardware/MES/Data
- **接口抽象**：硬件通过接口抽象，支持 Mock/真实切换
- **可测试性**：业务逻辑独立于 UI 和硬件

### 5.2 设计文档结构
```
doc/002-DESIGN.md
├── 二、系统架构（分层架构图）
├── 三、数据模型（CapillaryInfo, OperationLog）
├── 四、接口设计
│   ├── 4.1 IPlcController
│   ├── 4.2 IScanner
│   ├── 4.3 IMesService
│   ├── 4.4 ICapillaryRepository
│   └── 4.5 ILogRepository
├── 五、核心流程设计（PickupService.ExecuteAsync）
├── 六、依赖注入与组装
├── 七、技术选型
├── 八、项目结构（代码层）
└── 九、关键设计决策
```

### 5.3 核心设计要点
1. **分层架构**：UI (WinForms) → Service → Hardware/MES/Data
2. **5 个核心接口**：IPlcController, IScanner, IMesService, ICapillaryRepository, ILogRepository
3. **PickupService 流程**：
   - MES 查询 → FIFO 查库存 → PLC 取料 → 读码验证 → MES 上报 → PLC 出料 → 更新 DB
   - 异常处理：读码失败/MES 拒绝 → 放回原位并锁定
4. **依赖注入**：3 行代码切换 Mock/生产环境
5. **项目结构**：`src/CapillaryExercise/` 主程序 + `MockPLC/MockScanner/MockMES/` 独立程序 + `test/` 测试

### 5.4 提交
```bash
git add doc/002-DESIGN.md
git commit -m "Add design document for pickup flow"
git push
```

**Commit**: `21fd72b` - Add design document

---

## 六、第四步：拆解 GitHub Issues

### 6.1 拆解原则
- 每个 Issue 对应一个可独立完成、验收、合并的模块
- 按依赖关系排序（先底层后上层）
- 每个 Issue 包含：功能描述、任务清单、验收标准、关联的设计章节

### 6.2 初始拆解（10 个 Issues）
```
阶段一：基础设施
  #1: 搭建项目基础结构
  #2: 实现数据访问层

阶段二：接口与 Mock 实现
  #3: 定义硬件接口（IPlcController, IScanner）
  #4: 实现 TcpPlcClient
  #5: 实现 TcpScannerClient
  #6: 实现 IMesService 和 HttpMesClient

阶段三：业务逻辑与界面
  #7: 实现 PickupService
  #8: 实现 PickupForm

阶段四：集成与 CI
  #9: 集成测试：端到端流程
  #10: 搭建 GitHub Actions CI
```

### 6.3 优化：拆分 Mock 程序（新增 3 个 Issues）
**发现问题**：#4-#6 客户端开发时无法测试（依赖 Mock 程序），但 Mock 程序在 #9 才实现。

**解决方案**：将 Mock 程序独立为 #11-#13，提前到第二批开发。

```
阶段二：Mock 程序（优先）
  #11: 实现 MockPLC 程序
  #12: 实现 MockScanner 程序
  #13: 实现 MockMES 程序

阶段三：接口与客户端实现
  #3: 定义硬件接口
  #4: 实现 TcpPlcClient（依赖 #11）
  #5: 实现 TcpScannerClient（依赖 #12）
  #6: 实现 HttpMesClient（依赖 #13）
```

### 6.4 最终 Issue 清单（13 个）
| 阶段 | Issues | 说明 |
|------|--------|------|
| 基础设施 | #1-#2 | 项目结构、数据访问 |
| Mock 程序 | #11-#13 | 优先实现，便于客户端测试 |
| 接口与客户端 | #3-#6 | 可用真实 Mock 测试 |
| 业务与界面 | #7-#8 | 领料服务、界面 |
| 集成与 CI | #9-#10 | E2E 测试、CI |

### 6.5 提交
```bash
gh issue create --title "..." --body "..."  # 创建 13 个 Issues
git add doc/003-ISSUE_LIST.md
git commit -m "Add issue breakdown list"
git push
```

**Commits**:
- `747935d` - Add issue breakdown list (10 issues)
- `aaf6bb9` - Update issue list: add Mock program issues (13 issues)

---

## 七、开发顺序建议

### 第一批（可并行）
- #1: 搭建项目结构
- #2: 数据访问层
- #3: 定义硬件接口

### 第二批（依赖第一批）
- #11: MockPLC 程序
- #12: MockScanner 程序
- #13: MockMES 程序

### 第三批（依赖第二批）
- #4: TcpPlcClient（可用真实 MockPLC 测试）
- #5: TcpScannerClient（可用真实 MockScanner 测试）
- #6: HttpMesClient（可用真实 MockMES 测试）

### 第四批（依赖第三批）
- #7: PickupService

### 第五批（依赖第四批）
- #8: PickupForm

### 第六批（依赖所有）
- #9: 集成测试
- #10: CI 配置

**预估工作量**：13-18 小时（单人 + AI 辅助）

---

## 八、关键决策与经验

### 8.1 为什么先写需求、设计，再拆 Issue？
- **需求**：明确"做什么"，与"怎么做"分离
- **设计**：技术方案、接口、流程，为 Issue 拆解提供依据
- **Issue**：可独立开发的任务单元，基于设计拆分

**顺序很重要**：需求 → 设计 → Issue，不能跳过中间环节。

### 8.2 Issue 与模块的区别
- **模块**：代码组织单位（如 `PickupService` 类），可独立测试（单元测试）
- **Issue**：工作任务单位，必须有验收标准，可独立交付

一个 Issue 可能包含多个模块，也可能只是一个模块的一部分。

### 8.3 为什么把 Mock 程序独立为 Issue？
- **原因**：客户端开发时需要真实 Mock 测试，不能等到集成阶段才发现问题
- **效果**：Mock 程序提前到第二批，客户端开发时就能测试，符合 TDD 思想

### 8.4 目录结构与文档编号的价值
- **规范的目录**：`doc/`、`src/`、`test/`、`.github/workflows/`，清晰易维护
- **文档编号**：`001-XXX.md`、`002-XXX.md`，便于引用和排序

### 8.5 Git 提交习惯
- **每个阶段独立提交**：需求一个 commit、设计一个 commit、Issue 清单一个 commit
- **提交信息清晰**：说明做了什么、为什么做
- **Co-Authored-By**：标注 AI 协作（`Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`）

---

## 九、待完成的工作

### 下一步计划
1. **测试设计**：`doc/004-TEST_PLAN.md`（测试策略、用例清单、覆盖率目标）
2. **代码规范**：`doc/005-CODING_STANDARD.md`（10 条核心规范）
3. **CI 配置**：`.github/workflows/build-test.yml`（自动编译 + 测试）
4. **迭代开发**：按 Issue 顺序逐个实现、测试、合并

### 预估剩余工作量
- 测试设计 + 规范底座：2-3h
- 迭代开发（13 个 Issues）：13-18h
- **总计**：15-21h

---

## 十、项目关键链接

- **GitHub 仓库**：https://github.com/Luoyuetong/capillary-exercise
- **Issues 列表**：https://github.com/Luoyuetong/capillary-exercise/issues
- **需求文档**：[doc/001-REQUIREMENTS.md](https://github.com/Luoyuetong/capillary-exercise/blob/main/doc/001-REQUIREMENTS.md)
- **设计文档**：[doc/002-DESIGN.md](https://github.com/Luoyuetong/capillary-exercise/blob/main/doc/002-DESIGN.md)
- **Issue 清单**：[doc/003-ISSUE_LIST.md](https://github.com/Luoyuetong/capillary-exercise/blob/main/doc/003-ISSUE_LIST.md)

---

## 十一、总结与反思

### 11.1 完成的工作
- ✅ 工作区 + GitHub 仓库搭建
- ✅ 需求文档（13 条功能需求 + 10 条非功能需求 + 验收标准）
- ✅ 概要设计（分层架构 + 5 个核心接口 + 流程设计）
- ✅ Issue 拆解（13 个任务，依赖关系清晰）

### 11.2 核心价值
1. **设计先于编码**：需求 → 设计 → Issue，建立全局观
2. **流程倒逼习惯**：每一步都有产出物和验收标准
3. **AI 全程协作**：从需求整理到设计到 Issue 拆解，AI 是思考伙伴，但人主导决策
4. **文档化思考**：把思考过程写下来，可追溯、可复用

### 11.3 经验教训
- **Mock 程序要提前**：测试驱动的关键是"可测试性"，Mock 不能等到最后
- **Issue 粒度要合适**：太大无法并行，太小频繁切换
- **文档编号很有用**：便于引用和维护
- **Git 习惯要养成**：每个阶段独立提交，提交信息清晰

### 11.4 下一步重点
继续走**测试设计 → 规范底座 → 迭代开发**的流程，体验完整的工程闭环。

---

## 附录：设计演进记录（2026-06-22）

> 本文档记录的是**项目启动当时**的决策与拆解，忠实保留原貌。进入迭代开发（第 7 步）后，为聚焦工程流程教学，做了两处调整。此处追加说明，不改写上文历史——"留痕"本身也是工程纪律的一部分。

1. **数据库 Access → SQLite**：原选 Access(.mdb) 继承自参考项目，导致强制 x86、CI 跑不了数据库测试。改用 SQLite(.db) 后 AnyCPU、跨平台、CI 可原生跑库测试，去掉了与教学无关的环境包袱。
2. **独立 Mock 程序 + TCP 客户端 → 进程内 Fake**：原计划 3 个独立 Mock 程序（#11-#13，TCP/HTTP）+ 3 个 TCP/HTTP 客户端（#4-#6）。TCP/HTTP + 独立进程更偏"系统管线"而非"工程流程"，改为 3 个进程内 Fake（合并为单个 Issue #4），App 仍可运行、DI 切换演示仍在。原 #5/#6/#11/#12/#13 关闭，13 Issue 精简为 8 个。

> 这次调整本身也是教学点：**工程会演进，设计可以修正**——关键是把"为什么改"记录清楚（详见 `003-ISSUE_LIST.md` 开头的设计演进说明）。上文 11.3 "Mock 程序要提前"的经验在新方案下变为"Fake 实现要随接口尽早就位"，道理一致：可测试性、可运行性不能等到最后。
