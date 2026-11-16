# 运行与开发依赖

| 组件 | 要求/用途 | 安装与来源 | 是否随包分发 |
| --- | --- | --- | --- |
| Windows 10/11 交互桌面 | Win32 窗口与键盘 API、桌面界面 | 用户自己的 Windows 系统 | 否 |
| Windows PowerShell 5.1 | `start.cmd` 调用；FullLanguage、允许 Add-Type | Windows 自带；[Microsoft 文档](https://learn.microsoft.com/en-us/powershell/scripting/windows-powershell/overview) | 否 |
| .NET Framework 4.8 | C# 编译器及 System、Windows.Forms、Drawing、Web.Extensions | [Microsoft 安装指南](https://learn.microsoft.com/en-us/dotnet/framework/install/guide-for-developers) | 否 |
| System.IO.Compression / FileSystem | 打包脚本创建和读取 ZIP | .NET Framework 系统程序集 | 否 |
| Microsoft YaHei UI 字体 | 中文界面；缺失时可能字体回退 | Windows 随附字体 | 否 |
| 桌面浏览器 | 实际接受输入的目标窗口 | 用户自行安装；匹配名单见 README | 否 |
| Git（可选） | 本地仓库和上传 GitHub | [Git 官方网站](https://git-scm.com/) | 否 |

没有 npm、pip、NuGet 依赖，无模型、浏览器插件、第三方图标、云端 API Key 或环境变量配置要求。`System.Web.Extensions` 只用于本地 JSON 解析，不启动 Web 服务。

启动器使用 PowerShell `Add-Type` 在本机编译源码，没有安装全局程序或后台服务。除正常启动和用户选文件外，不需要项目目录写权限；生成截图和发布 ZIP 时需要写入 `docs/images/` 与 `dist/`。运行环境可能产生系统编译缓存或临时文件，这些不属于发行物。

本项目没有把 Windows PowerShell 5.1 的系统版本与开源 PowerShell 7 的许可混同；系统组件继续适用用户安装的 Microsoft 产品许可。没有从系统目录复制程序集或字体。
