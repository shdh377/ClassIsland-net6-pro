# WebKiosk · 定时全屏播放（实验性）

> 本插件是 `WebOpener`（定时打开网页）的独立分支，两者可并存安装，由你决定保留哪一个。
> 新增的核心能力：**kiosk 全屏 + 放开自动播放**——例如每周日自动全屏播放央视《新闻周刊》最新一期。

## 与 WebOpener 的差异

| 能力 | 说明 |
| --- | --- |
| 全屏自动播放 | 规则可勾选「全屏自动播放（kiosk）」：用 `chrome/edge --kiosk <网址> --autoplay-policy=no-user-gesture-required` 启动，浏览器窗口占满屏幕且视频**允许带声音自动播放** |
| 独立浏览器配置 | 使用插件专属 `user-data-dir`，不干扰用户日常浏览器配置与已打开的窗口 |
| 关闭全屏 | 设置页提供「关闭全屏窗口」按钮（按启动时记录的 PID 精确关闭，只关本插件开的窗口） |
| 未装浏览器时 | 自动探测 Chrome / Edge（含 Program Files、LocalAppData、Win7 时代的 x86 路径），都找不到则回退为默认浏览器普通打开并记日志 |
| 其余功能 | 与 WebOpener 一致：课前/每周定时、打开本地文件、图片全屏、全局静默时段、确认对话框 |

## 适用条件（重要）

- 目标机需安装 **Chrome 或 Edge**。Windows 7 上两者最后支持版本为 **109**，上述启动参数可用，但均已于 2023 年初停止安全更新；
- 「自动播放」是放开浏览器策略后由**网站播放器自身**执行的：若栏目页打开后默认不自动播最新一期，kiosk 只能保证全屏，首次仍需点一次播放；
- 已实测：`tv.cctv.com/lm/xwzk/tongyong/index.shtml`（《新闻周刊》通用播放页）的节目列表为 JS 动态加载，静态 HTML 无最新一期直链，故本插件直接全屏打开该页，不解析直链。

## 安装

软件内「设置 → 插件 → 从本地安装」选择 `cipx/WebKiosk.cipx`，重启生效。
与 WebOpener 同时安装时，两者的设置页与配置文件（`Config/Plugins/WebKiosk/`）完全独立，互不覆盖。

## 配置位置

`{安装目录}/Config/Plugins/WebKiosk/rules.json`
