# Issue #13：实现 MockMES 程序

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/13

| 字段 | 值 |
|------|----|
| 编号 | #13 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:12:56 UTC |
| 关闭 | 2026-06-22 07:50:00 UTC |
| 标签 | （无） |

> ⚠️ **已关闭：原独立 MockMES 程序方案，设计修正后并入进程内 Fake（#4）。** 保留归档以呈现设计演进过程（见 `doc/003-ISSUE_LIST.md` 开头的设计演进说明）。

---

## Issue 正文

## 描述
创建独立的 MockMES 程序，提供 HTTP API（端口 9003），模拟 MES 系统。

## 任务清单
- [ ] 创建 MockMES 项目（可用 ASP.NET Core Minimal API 或 WinForms + HttpListener）
- [ ] API 1：GET /api/capillary-type?workOrder=xxx&machineNo=xxx（查询劈刀类型）
- [ ] API 2：POST /api/pickup（上报领料）
- [ ] 界面：显示收到的请求、支持配置拒绝上报（测试异常流程）
- [ ] 预置几条工单数据（如 WO001 → CAP-TYPE-A）

## 验收标准
- [ ] 能响应 HttpMesClient 请求
- [ ] 返回 JSON 格式数据
- [ ] 界面友好，便于配置测试场景

## 参考
doc/002-DESIGN.md 第九节9.3（MockMES 设计，需补充）

---

## 评论（1）

### @Luoyuetong · 2026-06-22 07:50:00 UTC

聚焦教学，去掉系统管线复杂度：独立 Mock 程序（TCP/HTTP）与 TCP/HTTP 客户端改为 3 个进程内 Fake，合并入 #4。详见 doc/003-ISSUE_LIST.md 开头的设计演进说明。
