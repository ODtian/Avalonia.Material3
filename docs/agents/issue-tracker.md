# Issue tracker: GitHub

本仓库的规格与任务位于 https://github.com/ODtian/Avalonia.Material3/issues 。

## 操作约定

- 使用 gh CLI，并明确传入 --repo ODtian/Avalonia.Material3。
- 创建 Issue：正文先写入 UTF-8 文件，再通过 --body-file 提交。
- 读取 Issue 时检查正文、标签、评论和依赖。
- 完整规格与各实施任务分别创建 Issue，规格使用 ready-for-agent 标签。
- 跨仓库依赖使用完整 GitHub Issue 链接与对应版本契约。
- 本地 docs/SPEC.md 是规格来源，记录发布地址；规格变更同步到关联 Issue。
- 同名 Issue 与标签先读取当前记录，再进行相应更新。

## Pull requests as a triage surface

PRs as a request surface: no.

## 规格发布与任务读取

技能要求发布到追踪器时创建对应 GitHub Issue。
技能要求读取任务时使用 gh issue view，并读取标签及评论。

## 任务依赖

优先记录 GitHub 原生依赖关系；适用时使用完整 Issue 链接表示跨仓库依赖。
每个可实施任务具有明确范围、验收条件和所属规格。
