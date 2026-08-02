# ClassIsland（Win7 兼容 · .NET 6 移植版）

> ClassIsland 是一款 Windows 桌面课表/日程显示应用，可在课桌面上展示课程表、倒计时、天气与通知。本仓库为官方 ClassIsland 的 **Win7 兼容移植版**（基于 .NET 6 / `net6.0-windows`）（感谢mimov2.5pro deepseekv4flash，AI太好用了你知道吗），并内置了两个实用插件：**IslandCaller**（随机点名/抽取）与 **MiMoTTS**（小米 MiMo 语音合成）。

## ✨ 功能特性

- 📅 **课表与日程显示**：课程表、上下课倒计时、学期周次
- 🔔 **通知系统**：上课/下课提醒、遮罩展示、语音播报（支持系统 TTS / Edge TTS / GPT-SoVITS / MiMo TTS）
- 🎲 **IslandCaller 插件**：随机点名/抽取，带权重均衡算法（长期次数均衡 + 近期回避），支持多人数抽取、名单导入（txt / CSV / SecRandom JSON）
- 🗣️ **MiMoTTS 插件**：小米 MiMo TTS 语音合成（需 API Key）
- 🧳 **便携式数据**：所有运行数据保存在应用根目录（`Data\`、`Config\` 等），不依赖 `%AppData%` 与注册表，适配带还原系统的教室电脑
- 🪶 **精简**：仅内置简体中文语言包；默认字体 HarmonyOS Sans SC（华为开源字体）
- 🖥️ **Win7 兼容**：目标框架 `net6.0-windows` / `net6.0-windows7.0`，可在 Windows 7 SP1+ 运行

## 💻 系统要求

| 项目 | 要求 |
| --- | --- |
| 操作系统 | Windows 7 SP1 及以上（推荐 Win7 x64 / Win10 / Win11） |
| 运行时 | .NET 6 桌面运行时（6.0.36 及以上；Win7 需安装 .NET 6 运行时） |
| 网络 | 天气、插件市场、在线语音（Edge/MiMo）等需要网络；离线也可使用核心课表功能 |

> 若在 Win7 上运行，请确保已安装 .NET 6 运行时（framework-dependent 版本不含运行时）。

## 🚀 快速开始

1. 从 Release 下载 `ClassIsland-net6-win7-v1.1.0.zip`；
2. 解压到任意目录（**建议非临时目录**，数据将保存在应用目录内）；
3. 双击运行 `ClassIsland.exe`；
4. 首次启动会自动生成默认配置与演示名单，按提示完成初始设置。

## 🧩 插件说明

### IslandCaller（随机点名/抽取）

- **快速点名**：悬浮窗按钮一键点 1 人；自定义抽取窗口可选 1~5 人；
- **权重均衡**：被点次数低于全班均值的学生更容易被抽中，刚被点过的学生短期内不会被重复抽中；
- **名单管理**：设置页可直接编辑（姓名/手动权重），支持从文本、CSV、SecRandom JSON 导入名单；
- **数据位置**：`Data\IslandCaller\`（名单 `Profile\`、历史 `History\`、设置 `Settings.json`）。

### MiMoTTS（语音合成）

- 在 ClassIsland 设置 → 提醒 → 语音中选择「MiMo TTS」；
- 在 MiMo TTS 面板中配置 API Key；
- 缓存目录：`Config\Plugins\MiMoTTS\`。

## 📂 数据存储

本版本采用**便携式布局**，所有数据保存在应用根目录：

| 路径 | 内容 |
| --- | --- |
| `Settings.json` | 应用主设置 |
| `Profiles\` | 课表档案（课程表、时间布局、科目） |
| `Config\` | 自动化、插件配置、公告记录等 |
| `Data\IslandCaller\` | IslandCaller 名单 / 历史 / 设置 |
| `Data\Management\` | 集控（管理端）数据 |
| `Logs\` | 运行日志 |
| `Cache\` / `Temp\` | 缓存与临时文件 |
| `Backups\` | 自动备份 |

> 提示：数据不写入 `%AppData%`，重装系统 / 还原系统后数据仍在应用目录内，可直接迁移整个目录。

## 🔨 从源码构建

环境：Windows 10/11 开发机 + **.NET 8 SDK**（可编译 `net6.0` 目标，产物兼容 Win7）。

```bash
dotnet build ClassIsland.sln -c Release
```

构建产物位于：

```
ClassIsland\bin\Release\net6.0-windows\
```

插件（IslandCaller、MiMoTTS）会自动输出到产物 `Plugins\` 目录。

## 📄 许可证

本项目基于 [GPL-3.0](LICENSE.txt) 许可发布。

本仓库是 [HelloWRC/ClassIsland](https://github.com/HelloWRC/ClassIsland) 的 .NET 6 / Win7 兼容移植分支，包含对上游项目的修改与新增插件。请遵守 GPL-3.0 协议的相关要求。

## 🙏 致谢

- [HelloWRC/ClassIsland](https://github.com/HelloWRC/ClassIsland) —— 原版 ClassIsland 项目
- [lladlam/ClassIsland-MiMoTTS](https://github.com/lladlam/ClassIsland-MiMoTTS) ——mimotts插件项目
= [HickoryTrail/IslandCaller](https://github.com/HickoryTrail/IslandCaller) - 点名插件项目

## 🙏 声明
这个项目几乎都由AI完成，对github并不熟悉，如果有不足欢迎指出
