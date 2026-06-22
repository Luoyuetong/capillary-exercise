# Issue #7：实现 PickupService（领料业务逻辑）

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/7

| 字段 | 值 |
|------|----|
| 编号 | #7 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:00:48 UTC |
| 关闭 | 2026-06-22 09:34:05 UTC |
| 标签 | （无） |

---

## Issue 正文

## 描述
实现领料流程的核心业务逻辑编排。

## 任务清单
- [ ] PickupService.cs - 实现 ExecuteAsync 方法
- [ ] 步骤1：MES 查询劈刀类型
- [ ] 步骤2：FIFO 查找库存
- [ ] 步骤3：PLC 取料
- [ ] 步骤4：扫码验证
- [ ] 步骤5：MES 上报
- [ ] 步骤6：PLC 出料
- [ ] 步骤7：更新数据库和日志
- [ ] 异常处理：读码失败/MES拒绝 → 放回原位并锁定
- [ ] IProgress<string> 进度报告

## 验收标准
- [ ] 单元测试覆盖正常流程
- [ ] 单元测试覆盖异常流程（无库存、读码失败、MES拒绝）
- [ ] 所有硬件和数据访问通过 NSubstitute Mock 测试（TC-01~10）

## 依赖
Issues #2, #3, #4

## 参考
doc/002-DESIGN.md 第五节5.1
