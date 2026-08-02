# ClassIsland-win7 项目进度总结

> 更新时间：2026-08-01

---

## 一、项目简介

**ClassIsland-win7** 是 ClassIsland（一款 Windows 课表/日程显示应用）的 **Win7 兼容 .NET 6 移植版**，并在此基础上定制了个人使用的功能插件：

- **核心框架**：ClassIsland-net6（.NET 6 WPF，目标框架 `net6.0-windows` / `net6.0-windows7.0`）
- **编译环境**：本机（Win10）用 .NET 8 SDK（C# 12）编译，产物 `net6.0` 兼容 Win7
- **源码镜像**：项目在 **E: 盘** 和 **F: 盘** 各存一份，改动需两盘同步
- **运行环境**：本机已安装系统级 .NET 6.0.36 运行时，framework-dependent 版本可直接运行

**项目结构：**
```
E:\Classisland-win7\   F:\Classisland-win7\   （源码镜像，两盘相同）
├── ClassIsland-net6\          ← 核心框架 + 主程序
│   ├── ClassIsland\           ← 主程序（含设置页、通知服务等）
│   ├── ClassIsland.Core\      ← 核心库（SpeechProviderRegistry 等）
│   ├── MiMoTTS\               ← 小米 MiMo 语音插件
│   └── IslandCaller\          ← 点名/随机抽取插件
├── ClassIsland\               （旧版参考）
├── IslandCaller\              （旧版参考）
└── _MiMoTTS_backup\  _IslandCaller_backup\   ← 修改前备份
```

**重要路径：**
- 构建：`F:\Classisland-win7\ClassIsland-net6\ClassIsland\bin\Release\net6.0-windows\`（app 输出，含 Plugins）
- 测试运行目录：**`F:\Classisland-win7\Release\`**（已解压的 release 测试版）
- 进度总结：`F:\Classisland-win7\项目进度总结.md`

---

## 二、用户工作流要求（2026-08-01 起执行）

### 1. 修改范围限制
- **只对 `E:\Classisland-win7` 和 `F:\Classisland-win7` 两个文件夹内的文件进行修改**
- 其余路径（如 `C:\Users\Administrator\Documents\` 等）**先询问，非必要不读写**
- 源码改动需同步两盘（E:F 镜像保持一致）

### 2. Release 测试流程
```
改源码 → 编译 → 更新 F:\Classisland-win7\Release\（解压版 release）
      → 用户测试 → 通过后 → 打包 release zip
```
- **F:\Classisland-win7\Release\** 为测试用解压版 release（framework-dependent，本机系统 .NET 6.0.36 可直接运行）
- 每次对源码作出修改后，**先更新该目录**供用户测试，**不要牵扯其他盘符**
- 用户测试通过后，再进行 zip 打包（如 `安分ci.zip`）

### 3. 备份策略
- 备份仅保留**上一次的修改**（删除更早的备份层）
- 源码修改前先备份到 `_MiMoTTS_backup\` / `_IslandCaller_backup\`（按日期命名）

---

## 三、已完成的工作

### 1. .NET 8 → .NET 6 移植
- IslandCaller 和 MiMoTTS 插件从 .NET 8 移植到 .NET 6（目标框架 `net6.0-windows7.0`）
- 核心框架 ClassIsland-net6 本身已是 .NET 6，插件需匹配

### 2. JIT 内联导致栈回溯失败修复
- **问题**：`NotificationHostService.ShowNotification` 使用 `StackTrace.GetFrames()` 验证调用者是否实现 `INotificationProvider`，Release 模式下 JIT 内联移除了栈帧，导致 `ArgumentException: 此方法只能由 INotificationProvider 调用`
- **修复**：在 `IslandCallerNotificationProvider.RandomCall` 方法上加 `[MethodImpl(MethodImplOptions.NoInlining)]`，阻止 JIT 内联，保留完整栈帧

### 3. MiMoTTS 语音提供者设置页面
- 扩展 `SpeechProviderRegistry`：新增 `_settingsControlTypes` 字典、`RegisterProvider<TSpeechService, TSettingsControl>()`、`GetSettingsControlType()` 方法
- 在 `NotificationSettingsPage` 中通过 `ContentControl` + `Loaded` 事件动态实例化 MiMoTTS 设置控件（跨程序集插件场景）
- 调用入口：应用设置 → 提醒 → 高级设置 → SpeechSource==3 的 MiMoTTS 面板

### 4. HoverFluent 悬浮工具条改造
| 功能 | 实现方式 |
|------|----------|
| 拖拽移动 | Button2（"..."按钮）按住左键拖拽，`PreviewMouseLeftButtonDown/Move/Up` 隧道事件 + 4px 距离阈值区分拖拽/点击 |
| 点击行为 | Button2 单击打开 `PersonalCall` 自定义抽取窗口 |
| 自适应布局 | `Grid` 两列布局：Button1 `Width="*"` 填充剩余空间，Button2 `Width="Auto"` + `MinWidth="40"` |
| 窗口缩放 | `ResizeMode="CanResizeWithGrip"`，`MinWidth="120" MinHeight="50"` |
| 透明度 | `Opacity="0.92"`，轻微透明不影响可读性 |
| 置顶保持 | `WindowTopmostHelper` + `EnsureNoActivate` + 3000ms 定时置顶循环 |

### 5. PersonalCall 自定义抽取窗口
- `WindowStyle="None"` + `AllowsTransparency="True"` + 圆角 `Border`
- Slider 选择抽取人数（1-5）
- 支持拖拽移动（`Border_MouseLeftButtonDown`）和调整大小（`ResizeMode="CanResizeWithGrip"`）

### 6. 删除 CallerPopupWindow
- 删除点名后显示结果的弹窗，改为使用 ClassIsland 内置通知系统（`MaskContent` + `MaskDuration` + `IsSpeechEnabled`）

### 7. 【2026-08-01】MiMoTTS API Key 无法保存修复
- **问题**：在应用设置-提醒-高级设置中配置 MiMo 的 API Key，输入后短时间内再打开设置页面 API 为空
- **根因排查**：单控件隔离测试中保存逻辑本身有效，问题集中在真实运行环境的并发/静默失败路径
- **修复**（`MiMoSpeechServiceSettingsControl.xaml.cs` + `MiMoSpeechService.cs`）：
  - **API Key 输入即保存**：`PasswordChanged` 事件里直接调用 `SaveSettings()` 立即写盘，不再依赖属性变更链
  - **FileShare.ReadWrite**：控件保存（`FileMode.Create`）与服务 `ReloadConfig` 读取（`FileMode.Open`）均改用 `FileShare.ReadWrite`，避免写入/读取互锁冲突导致写入失败
  - **目录预创建**：保存前先 `Directory.CreateDirectory(GlobalConstants.PluginConfigFolder)`
  - **错误日志**：保存失败不再静默吞掉，改为 `_logger.LogError` 记录
  - **防抖保存**：其他字段属性变更走 500ms `DispatcherTimer` 防抖（`ScheduleSave()`），避免频繁写盘
  - **日志容错**：`_logger` 用 try/catch 初始化为可空，app host 未初始化时允许无日志运行

### 8. Release 构建、部署与打包流程
- 构建：`dotnet build ClassIsland.sln -c Release`（.NET 8 SDK 全路径）→ **0 错误**
- **完整包**（app + 插件，非仅插件）：
  - app 顶层（framework-dependent）：ClassIsland.dll、Core/Shared DLL、deps.json、runtimes、本地化
  - `Plugins\MiMoTTS\`、`Plugins\IslandCaller\`（按部署规则清理，不含 core DLLs）
- **插件部署规则**：插件目录**不得**包含 core DLLs（ClassIsland.Core.dll、ClassIsland.Shared.dll、ClassIsland.Shared.IPC.dll）——会导致 AsyncBox InvalidCastException；只含 plugin DLL + ClassIsland.PluginSdk.dll + xml + icon + manifest + 唯一依赖（NAudio）
- 产物校验：`F:\ClassIsland-win7\Release\`（测试版）+ `安分ci.zip`（正式打包版）

### 9. 已解压测试版 Release 目录（2026-08-01 建立）
- 路径：**`F:\ClassIsland-win7\Release\`**
- 内容：app 顶层 125 文件 + Plugins（MiMoTTS 45568B / IslandCaller 73728B，与构建产物 MD5 一致）+ runtimes + 本地化
- 已实测用系统 .NET 6.0.36 成功启动运行

---

## 四、当前状态

| 项目 | 状态 |
|------|------|
| 编译 | ✅ 0 错误（约 2000+ 警告，多为 .NET 6 兼容性警告） |
| MiMoTTS API Key 保存 | ✅ 已修复（输入即存 + FileShare.ReadWrite） |
| F:\Classisland-win7\Release 测试目录 | ✅ 已建立并实测可启动 |
| HoverFluent 自适应布局 | ✅ 已实现，待验证 |
| HoverFluent 透明度 | ✅ 0.92 |
| 拖拽/点击分离 | ✅ 已实现 |
| 通知系统集成 | ✅ 已实现 |
| MiMoTTS 设置页面 | ✅ 已实现 |

---

## 五、待完成 / 待验证任务

### 待用户测试
- [ ] **MiMoTTS API Key 输入后保存并重开设置页不丢失**（本次修复的核心验证点）
- [ ] MiMo TTS 语音实际朗读是否正常（之前日志有 401 Unauthorized）
- [ ] HoverFluent 拖拽调整大小后，界面控件是否正确自适应（Grid 布局验证）
- [ ] Button2 拖拽移动是否流畅，4px 阈值是否合适
- [ ] Button2 单击打开 PersonalCall 是否有误触发（拖拽后松手不应弹出）
- [ ] 透明度 0.92 是否符合审美预期
- [ ] 语音朗读功能是否正常（`IsSpeechEnabled=true`）

### 待完成
- [ ] 用户测试通过后，重新打包 release zip
- [ ] 同步更新较旧的 release zips（`E:\Classisland-win7\ClassIsland-Release-v1.0.0.zip`）

### 潜在改进
- [ ] HoverFluent 缩放手柄视觉提示（当前仅系统默认 grip）
- [ ] 拖拽时的视觉反馈（如半透明跟随）
- [ ] PersonalCall 窗口大小联动 HoverFluent 位置

---

## 六、已知问题

1. **构建警告多**（约 2000+ 个）— 大部分为 .NET 6 兼容性警告，非功能性问题
2. **ClassIsland 进程锁 DLL** — 每次构建前需先 `taskkill //F //IM ClassIsland.exe`
3. **IslandCaller 输出路径** — 为 `net6.0-windows7.0`（非 `net6.0-windows`），复制 DLL 时需注意路径差异
4. **中文路径/编码** — PowerShell 脚本中硬编码中文路径可能被按系统代码页错误编码，路径应通过 `param()` 传入；对比文件时注意 BOM 与 `/` `\` 分隔符差异

---

## 七、关键技术栈

- **框架**：.NET 6 WPF（`net6.0-windows` / `net6.0-windows7.0`），.NET 8 SDK 编译
- **UI**：XAML + MaterialDesign 风格（部分引用）
- **架构**：插件系统，`NotificationProviderBase` 抽象类 + `INotificationProvider` 接口；`SpeechProviderRegistry` 注册语音提供者
- **服务**：DI 容器（`IAppHost.GetService<T>()`），`ILogger` 日志
- **设置模型**：CommunityToolkit.Mvvm `ObservableRecipient`（继承并序列化 `IsActive` 属性）
- **窗口管理**：Win32 `WS_EX_NOACTIVATE`，`WindowTopmostHelper` 置顶工具
- **构建命令**：`C:\Users\Administrator\AppData\Local\Microsoft\dotnet\dotnet.exe build ClassIsland.sln -c Release`
