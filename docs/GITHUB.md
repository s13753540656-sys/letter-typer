# 上传 GitHub 与发布说明

## 项目建议

- 仓库名称：`letter-typer`
- 仓库描述：`Windows 本机英文逐字输入工具，支持文本选择、测试确认、倒计时和暂停续输。`
- 初始提交说明：`发布 LetterTyper 1.0.0：完善逐字输入、隐私隔离与中文文档`
- Release 标题：`LetterTyper 1.0.0`

仅将包含本文件的项目根目录作为仓库，不要上传上级工作目录。公开或私有状态由仓库所有者确认。当前项目仓库为 https://github.com/s13753540656-sys/letter-typer ，上传时已核实为公开仓库。

## 本地检查

1. 阅读 `docs/VALIDATION.md`，不要把未验证的真实网站兼容性写成已通过。
2. 运行 `verify.cmd` 与 `package.cmd`。
3. 检查 `git status`、暂存区、配置、截图和待发布 ZIP。禁止提交个人文本、学生信息、账号、密码、Cookie、Token 或本地配置。
4. `.gitignore` 只影响尚未跟踪的文件；若历史中已有敏感数据，先处理历史并撤销相关凭据，不能只新增忽略规则。

在确认当前目录正确后，可执行以下本地命令：

```text
git init
git add .
git diff --cached --stat
git diff --cached
git commit -m "发布 LetterTyper 1.0.0：完善逐字输入、隐私隔离与中文文档"
```

提交需要用户自己的 Git 身份配置；不要把身份信息或凭据写入项目文件。

## 连接远程前必须确认

确认目标仓库的完整 HTTPS/SSH 地址、所有者以及公开/私有状态；如目标已有内容，还应检查目标分支和合并方式。地址或可见性不明确时，不添加远程、不推送。

身份认证由用户本机 Git Credential Manager、SSH 或 GitHub 官方认证完成，禁止在命令、仓库地址、文档或 ZIP 中嵌入 Token。

本地打包脚本不会连接远程。以后上传或切换仓库时，应重新确认目标及可见性；不要因文档中的示例地址直接向其他仓库推送。

## 发布附件

- `LetterTyper-1.0.0-source.zip`：源码、打包脚本、验证脚本及完整文档。
- `LetterTyper-1.0.0-portable.zip`：用户所需的启动器、随运行所需源码、公开示例、配置示例和使用文档。
- `SHA256SUMS.txt`：两个 ZIP 的 SHA-256。

两个包均需完整解压后双击 `start.cmd`；便携包也依赖系统 PowerShell 与 .NET，包含源码用于本地编译，不是单独 EXE。发布说明应包含此要求，以及当前真实网页登录输入尚未验证的边界。

当前下载文件置于仓库 `downloads/`，README 提供直接入口。这与 GitHub Releases 附件是两种分发方式；项目打包脚本不自动创建 Release，也不把下载目录嵌入 ZIP。
