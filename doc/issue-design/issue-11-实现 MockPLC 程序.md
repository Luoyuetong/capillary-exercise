# Issue #11：实现 MockPLC 程序

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/11

| 字段 | 值 |
|------|----|
| 编号 | #11 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:12:53 UTC |
| 关闭 | 2026-06-22 07:49:54 UTC |
| 标签 | （无） |

> ⚠️ **已关闭：原独立 MockPLC 程序方案，设计修正后并入进程内 Fake（#4）。** 保留归档以呈现设计演进过程（见 `doc/003-ISSUE_LIST.md` 开头的设计演进说明）。

---

## Issue 正文

## 描述
创建独立的 MockPLC 程序，提供 TCP Server（端口 9002），模拟 PLC 控制器。

## 任务清单
- [ ] 创建 MockPLC WinForms 项目
- [ ] 实现 TCP Server，监听 127.0.0.1:9002
- [ ] 解析命令：CMD:FETCH:{face},{x},{y}、CMD:OUTPUT、CMD:RETURN:{face},{x},{y}
- [ ] 返回响应：OK 或 FAIL
- [ ] 界面显示：连接状态、收到的命令、通信日志
- [ ] 支持手动设置模拟失败（用于测试异常流程）

## 验收标准
- [ ] 能接受 TcpPlcClient 连接
- [ ] 正确解析和响应各类命令
- [ ] 界面友好，便于调试

## 参考
doc/002-DESIGN.md 第九节9.2（MockPLC 设计）

---

## 评论（1）

### @Luoyuetong · 2026-06-22 07:49:54 UTC

聚焦教学，去掉系统管线复杂度：独立 Mock 程序（TCP/HTTP）与 TCP/HTTP 客户端改为 3 个进程内 Fake，合并入 #4。详见 doc/003-ISSUE_LIST.md 开头的设计演进说明。
