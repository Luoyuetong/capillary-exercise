# Issue #12：实现 MockScanner 程序

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/12

| 字段 | 值 |
|------|----|
| 编号 | #12 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:12:55 UTC |
| 关闭 | 2026-06-22 07:49:57 UTC |
| 标签 | （无） |

> ⚠️ **已关闭：原独立 MockScanner 程序方案，设计修正后并入进程内 Fake（#4）。** 保留归档以呈现设计演进过程（见 `doc/003-ISSUE_LIST.md` 开头的设计演进说明）。

---

## Issue 正文

## 描述
创建独立的 MockScanner 程序，提供 TCP Server（端口 9001），模拟扫码器。

## 任务清单
- [ ] 创建 MockScanner WinForms 项目
- [ ] 实现 TCP Server，监听 127.0.0.1:9001
- [ ] 解析命令：SCAN\n
- [ ] 返回响应：OK:{barcode}\n 或 FAIL\n
- [ ] 界面：条码输入框、发送按钮、模拟失败按钮、随机生成条码按钮
- [ ] 通信日志显示

## 验收标准
- [ ] 能接受 TcpScannerClient 连接
- [ ] 正确响应 SCAN 命令
- [ ] 界面友好，便于手动输入条码测试

## 参考
doc/002-DESIGN.md 第九节9.1（MockScanner 设计）

---

## 评论（1）

### @Luoyuetong · 2026-06-22 07:49:57 UTC

聚焦教学，去掉系统管线复杂度：独立 Mock 程序（TCP/HTTP）与 TCP/HTTP 客户端改为 3 个进程内 Fake，合并入 #4。详见 doc/003-ISSUE_LIST.md 开头的设计演进说明。
