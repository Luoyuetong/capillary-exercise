# Issue #6：实现 IMesService 和 HttpMesClient

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/6

| 字段 | 值 |
|------|----|
| 编号 | #6 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:00:10 UTC |
| 关闭 | 2026-06-22 07:49:51 UTC |
| 标签 | （无） |

> ⚠️ **已关闭：原 HTTP MES 客户端方案，设计修正后并入进程内 Fake（#4）。** 保留归档以呈现设计演进过程（见 `doc/003-ISSUE_LIST.md` 开头的设计演进说明）。

---

## Issue 正文

## 描述
实现 MES 服务接口和 HTTP 客户端，连接 MockMES（HTTP API）。

## 任务清单
- [ ] IMesService.cs - 定义 QueryCapillaryTypeAsync, ReportPickupAsync
- [ ] HttpMesClient.cs - 实现 HTTP 调用
- [ ] 超时处理（5秒）

## 验收标准
- [ ] 能调用 MockMES API
- [ ] 单元测试（用 Mock HTTP 或真实 MockMES）

## 依赖
需要 MockMES 程序

## 参考
doc/002-DESIGN.md 第四节4.3

---

## 评论（1）

### @Luoyuetong · 2026-06-22 07:49:50 UTC

聚焦教学，去掉系统管线复杂度：独立 Mock 程序（TCP/HTTP）与 TCP/HTTP 客户端改为 3 个进程内 Fake，合并入 #4。详见 doc/003-ISSUE_LIST.md 开头的设计演进说明。
