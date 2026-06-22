# Issue #8：实现 PickupForm（领料界面）

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/8

| 字段 | 值 |
|------|----|
| 编号 | #8 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:00:50 UTC |
| 关闭 | 2026-06-22 09:52:14 UTC |
| 标签 | （无） |

---

## Issue 正文

## 描述
实现领料操作的 WinForms 界面。

## 任务清单
- [ ] PickupForm.cs - 界面设计（工单号、机台号输入框，开始按钮）
- [ ] 进度显示区域（ListBox 或 TextBox）
- [ ] 结果显示区域（成功/失败提示）
- [ ] 调用 PickupService.ExecuteAsync
- [ ] 实现 IProgress<string> 更新 UI

## 验收标准
- [ ] 界面布局清晰，一屏显示
- [ ] 进度实时更新
- [ ] 异常时显示友好提示

## 依赖
Issue #7

## 参考
doc/001-REQUIREMENTS.md 第二节2.1
