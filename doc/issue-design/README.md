# Issue 设计文档归档

> 本目录把 GitHub 上 13 个 Issue 的原始内容归档到本地，便于教学时离线查阅、对照 `doc/003-ISSUE_LIST.md` 看「拆解 → 实现」的对应关系。

每个文件 = 一个 Issue 的原样内容（正文 + 评论 + 元信息）。**#5/#6/#11/#12/#13 是设计修正前的方案，已并入 #4**，保留归档以呈现工程演进（详见教学材料 `teaching/030`）。

## 清单

| 编号 | 标题 | 状态 | 备注 |
|------|------|------|------|
| [#1](./issue-01-搭建项目基础结构.md) | 搭建项目基础结构 | CLOSED |  |
| [#2](./issue-02-实现数据访问层.md) | 实现数据访问层 | CLOSED |  |
| [#3](./issue-03-定义硬件接口（IPlcController,%20IScanner）.md) | 定义硬件接口（IPlcController, IScanner） | CLOSED |  |
| [#4](./issue-04-实现%203%20个进程内%20Fake（FakePlcController-FakeScanner-FakeMesService）.md) | 实现 3 个进程内 Fake（FakePlcController/FakeScanner/FakeMesService） | CLOSED |  |
| [#5](./issue-05-实现%20TcpScannerClient（Mock%20扫码器）.md) | 实现 TcpScannerClient（Mock 扫码器） | CLOSED | 已并入 #4 |
| [#6](./issue-06-实现%20IMesService%20和%20HttpMesClient.md) | 实现 IMesService 和 HttpMesClient | CLOSED | 已并入 #4 |
| [#7](./issue-07-实现%20PickupService（领料业务逻辑）.md) | 实现 PickupService（领料业务逻辑） | CLOSED |  |
| [#8](./issue-08-实现%20PickupForm（领料界面）.md) | 实现 PickupForm（领料界面） | CLOSED |  |
| [#9](./issue-09-端到端测试：领料流程（Fake%20+%20SQLite%20进程内）.md) | 端到端测试：领料流程（Fake + SQLite 进程内） | CLOSED |  |
| [#10](./issue-10-搭建%20GitHub%20Actions%20CI.md) | 搭建 GitHub Actions CI | CLOSED |  |
| [#11](./issue-11-实现%20MockPLC%20程序.md) | 实现 MockPLC 程序 | CLOSED | 已并入 #4 |
| [#12](./issue-12-实现%20MockScanner%20程序.md) | 实现 MockScanner 程序 | CLOSED | 已并入 #4 |
| [#13](./issue-13-实现%20MockMES%20程序.md) | 实现 MockMES 程序 | CLOSED | 已并入 #4 |

## 实际开发的 8 个 Issue

设计修正后，领料流程实际落地的是 **#1–#4、#7–#10**（共 8 个），每个都走了完整的「分支 → 编码 → 测试 → PR → CI → 自审 → 合并」闭环。对应关系见 `doc/003-ISSUE_LIST.md`。
