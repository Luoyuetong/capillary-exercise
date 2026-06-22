# Issue #2：实现数据访问层

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/2

| 字段 | 值 |
|------|----|
| 编号 | #2 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:00:02 UTC |
| 关闭 | 2026-06-22 08:51:52 UTC |
| 标签 | （无） |

---

## Issue 正文

## 描述
实现 SQLite 数据库访问、CapillaryRepository 和 LogRepository。

> 设计修正：数据库由 Access/Jet 改为 SQLite（Microsoft.Data.Sqlite，AnyCPU），去掉 x86 锁。参数化查询规范不变。

## 任务清单
- [ ] DbHelper.cs - SQLite 数据库连接封装（Microsoft.Data.Sqlite）
- [ ] ICapillaryRepository + CapillaryRepository - FIFO 查询、状态更新
- [ ] ILogRepository + LogRepository - 日志记录
- [ ] 创建数据库初始化脚本（建表 SQL）

## 验收标准
- [ ] FIFO 查询能正确按 (CapillaryType, Status, StoredTime) 排序
- [ ] 状态更新能正确修改 Status 和 WorkOrder/MachineNo
- [ ] 单元测试覆盖主要方法（SQLite 临时库/内存库，CI 可跑）

## 参考
doc/002-DESIGN.md 第三节（数据模型）、第四节4.4
