# Codex Usage Widget

一个简洁的 Windows 任务栏 Codex 用量组件。通过本机已登录的 Codex 后台程序读取账号额度，显示 5 小时和每周用量、进度及重置时间。

使用 C#、WPF、WinForms 和 Windows 自带的 .NET Framework 4.8。无需 Node.js、Visual Studio、WinUI 或 Windows App SDK；编译出的主程序约 127 KB，不捆绑运行库。

## 外观

![七种任务栏样式，使用模拟数据](images/styles.png)

支持圆角进度条、分段进度条、双行卡片、点阵、渐变、环形仪表和数字胶囊。可以分别调整两类额度、文字、背景、中余量、低余量及过期数据颜色，支持配色预设、选色器和十六进制输入。

“剩余 / 已用”双选开关同时作用于任务栏和详细窗口。两行前缀可以自定义；`{mode}` 自动替换为“余 / 用”，留空隐藏。支持字号、宽度、左右位置、偏移和背景不透明度调整，设置中有即时预览。

![设置窗口，使用模拟数据](images/settings.png)

## 环境

- Windows 10 22H2 或 Windows 11，x64。
- .NET Framework 4.8。
- 本机已登录的 Codex CLI，或包含可调用 Codex 后台组件的官方桌面应用。

目前实际验证在 Windows 11 的 125% 缩放下进行；100%、125%、150%、200% 绘制也有测试。Windows 10 和跨显示器 DPI 切换仍需实机确认。任务栏嵌入使用 Explorer 窗口关系，第三方任务栏工具或系统更新可能影响布局。

## 构建与启动

在项目目录打开 Windows PowerShell：

```powershell
.\build.ps1
.\dist\CodexUsageWidget-Windows.exe
```

脚本使用 Windows 自带的 C# 编译器，无需下载依赖。若当前 PowerShell 执行策略禁止脚本，可以仅对本次进程运行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

默认隐藏详细窗口，只显示任务栏组件。右键可以刷新、打开设置或退出，双击显示详细窗口。关闭或最小化详细窗口会隐藏它并继续刷新；右键“退出”才关闭整个程序。

```powershell
.\dist\CodexUsageWidget-Windows.exe --settings
.\dist\CodexUsageWidget-Windows.exe --window
```

## 用量来源

设置中的 Codex 路径非空时，优先使用指定的完整 EXE 路径；留空时依次检查：

1. npm 安装位置和 PATH 中的 `codex.exe`。
2. `%LOCALAPPDATA%\OpenAI\Codex\bin\*\codex.exe`。
3. 当前用户注册的已知官方商店包内的 Codex 后台程序。

商店包信息使用 Windows 原生接口查询，不依赖 `Get-AppxPackage`。自动候选需通过 `app-server --help` 验证。已知包族为 `OpenAI.Codex`、`OpenAI.ChatGPT-Desktop`、`OpenAI.ChatGPT`，发布者 ID 为 `2p2nqsd0c76g0`。

程序通过 `account/rateLimits/read` 获取额度，默认每 60 秒刷新，收到更新通知时再查询。倒计时每秒更新，用量按服务端返回值更新。不会执行模型任务，也不会擅自把到期额度设为 100%。配置写入 EXE 同目录的 `settings.json`；程序不保存登录令牌。

显示的是选中后台程序所登录账号的额度。普通 OpenAI API Key 用量与 ChatGPT 套餐额度不同，本程序没有直接 HTTP API 回退。只有旧商店包且没有可调用后台组件时，需安装并登录 Codex CLI。

## 颜色规则

颜色始终按剩余量判定，切换到已用量显示不会改变判色规则。默认低阈值为 20%、中阈值为 50%：剩余 ≤20% 为低余量色，20%＜剩余≤50% 为中余量色，其余使用正常额度色。过期色优先。中阈值不得低于低阈值。

## 验证

```powershell
# 基础检查，无需登录或联网
.\test.ps1

# 外观、DPI 和实际设置窗口检查，会短暂打开测试窗口
.\test.ps1 -Appearance

# 读取本机真实用量，无模型请求
.\test.ps1 -Live
.\test.ps1 -Live -StoreOnly
```

检查结果和测试图片写入 `dist/`。外观检查覆盖旧配置迁移、配色、阈值优先级、七种样式、显示开关、自定义前缀、字号和 DPI 预览。

## 源码

`src/` 包含窗口、设置、任务栏挂载与绘制、CLI 定位和用量协议代码；`tests/AppearanceChecks.cs` 是外观与设置集成检查。运行时配置、真实用量报告、编译产物和本机缓存均不纳入 Git。

本项目是独立的个人工具，与 OpenAI 官方产品无隶属关系。
