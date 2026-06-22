# Issue #9：端到端测试：领料流程（Fake + SQLite 进程内）

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/9

| 字段 | 值 |
|------|----|
| 编号 | #9 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:00:52 UTC |
| 关闭 | 2026-06-22 10:03:41 UTC |
| 标签 | （无） |

---

## Issue 正文

## 描述
用真实 PickupService + 进程内 Fake + SQLite 串联完整领料流程，完成端到端测试。

> 设计修正：原计划搭 3 个独立 Mock 程序做集成测试。改为进程内 Fake 后，端到端测试全程在进程内完成，无需启动外部程序。

## 任务清单
- [ ] 组装：真实 PickupService + FakePlcController/FakeScanner/FakeMesService + SQLite
- [ ] 预置测试数据（数据库插入几条劈刀记录 + 配置 Fake 行为）
- [ ] 端到端测试：输入工单号 → 完整领料流程 → 验证结果（TC-20~23）

## 验收标准
- [ ] 正常流程能走通
- [ ] 异常流程（无库存、读码失败、MES拒绝）能正确处理
- [ ] 数据库状态正确更新
- [ ] 日志记录完整

## 依赖
Issues #1-#8

## 参考
doc/002-DESIGN.md 第六节（依赖注入组装）、doc/004-TEST_PLAN.md 第五节
