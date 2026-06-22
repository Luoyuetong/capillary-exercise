# Issue #4：实现 3 个进程内 Fake（FakePlcController/FakeScanner/FakeMesService）

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/4

| 字段 | 值 |
|------|----|
| 编号 | #4 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:00:07 UTC |
| 关闭 | 2026-06-22 09:23:05 UTC |
| 标签 | （无） |

---

## Issue 正文

## 描述
实现 3 个进程内 Fake，模拟硬件/MES，让 App 无需真实硬件即可运行，并支撑端到端测试。

> 设计修正：原"实现 TcpPlcClient（连 MockPLC）"。为聚焦工程流程教学，独立 Mock 程序（原 #11-#13，TCP/HTTP）+ TCP/HTTP 客户端（原 #5/#6）改为进程内 Fake，合并入本 Issue。详见 doc/003-ISSUE_LIST.md 开头的设计演进说明。

## 任务清单
- [ ] FakePlcController.cs - 实现 IPlcController，FetchFromSlotAsync/OutputToPickupPortAsync/ReturnToSlotAsync 返回成功（可配置模拟失败）
- [ ] FakeScanner.cs - 实现 IScanner，ScanAsync 返回预置条码（可配置返回 null 模拟读码失败）
- [ ] FakeMesService.cs - 实现 IMesService，QueryCapillaryTypeAsync 按预置映射返回类型，ReportPickupAsync 返回确认（可配置拒绝）
- [ ] Fake 行为可配置（预置条码、工单→类型映射、成功/失败开关）

## 验收标准
- [ ] 三个 Fake 均实现对应接口，App 能用它们组装并运行
- [ ] 单元测试覆盖 Fake 行为（TC-17/18/19）

## 依赖
#3（硬件接口定义）

## 参考
doc/002-DESIGN.md 第四节 4.1-4.3、第六节（依赖注入组装）
