# Issue #10：搭建 GitHub Actions CI

> 本文件由 GitHub Issue 原样归档，供教学查阅。源：
> https://github.com/Luoyuetong/capillary-exercise/issues/10

| 字段 | 值 |
|------|----|
| 编号 | #10 |
| 状态 | CLOSED |
| 创建 | 2026-06-21 09:00:53 UTC |
| 关闭 | 2026-06-22 10:14:13 UTC |
| 标签 | （无） |

---

## Issue 正文

## 描述
配置 GitHub Actions，自动编译和测试。

## 任务清单
- [ ] .github/workflows/build-test.yml
- [ ] 触发条件：push 和 pull_request
- [ ] 步骤：checkout → setup dotnet → restore → build → test
- [ ] 测试覆盖率报告（可选）

## 验收标准
- [ ] 每次 push 自动触发 CI
- [ ] CI 通过表示编译和测试都成功

## 参考
doc/002-DESIGN.md 第六节（依赖注入）
