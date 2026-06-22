# Issue #3：定义硬件接口（IPlcController, IScanner）

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/3

| 字段 | 值 |
|------|----|
| 编号 | #3 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:00:06 UTC |
| 关闭 | 2026-06-22 08:52:15 UTC |
| 标签 | （无） |

---

## Issue 正文

## 描述
定义 PLC 和扫码器的抽象接口，为后续 Mock 和真实实现打基础。

## 任务清单
- [ ] IPlcController.cs - 定义 FetchFromSlot, OutputToPickupPort, ReturnToSlot 等方法
- [ ] IScanner.cs - 定义 ScanAsync 方法
- [ ] 接口文档注释完整

## 验收标准
- [ ] 接口定义清晰，方法签名与设计文档一致
- [ ] 编译通过

## 参考
doc/002-DESIGN.md 第四节4.1、4.2
