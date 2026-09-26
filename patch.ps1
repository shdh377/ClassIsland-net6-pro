<#
.SYNOPSIS
    ClassIsland 天气逐小时 6h→12h 补丁部署脚本（适用于 Win7 生产机）。

.DESCRIPTION
    在生产环境对 ClassIsland 安装目录做「原地覆盖」更新：
      1. 自动检测或按参数定位安装目录（D 盘）；
      2. 停止正在运行的 ClassIsland（避免文件占用）；
      3. 将待更新文件备份到 <安装目录>\BackupPatches\<时间戳>\；
      4. 同路径、同文件名原地覆盖；
      5. 重新启动程序。
    安装路径与可执行文件名保持不变，因此 C 盘还原快照中
    启动文件夹里的 ClassIsland.lnk 指向不受任何影响，开机自启无需处理。

    使用前：把新编译产出的 ClassIsland.dll、ClassIsland.Core.dll、ClassIsland.Shared.dll、
    ClassIsland.Shared.IPC.dll 放到本脚本旁的 update\ 目录，或用 -SourceDir 指向编译输出目录
    （仓库 ClassIsland\bin\Release\net6.0-windows\）。

.EXAMPLE
    .\patch.ps1 -TargetDir "D:\ClassIsland"
.EXAMPLE
    .\patch.ps1 -TargetDir "D:\ClassIsland" -SourceDir "F:\Classisland-win7\ClassIsland-net6\ClassIsland\bin\Release\net6.0-windows"
.NOTES
    兼容 Windows PowerShell 5.1（Win7 自带）。
    回滚：把 BackupPatches\<时间戳>\ 里的文件复制回安装目录即可。
#>
[CmdletBinding()]
param(
    # ClassIsland 安装目录。省略时自动取自正在运行的 ClassIsland 进程。
    [string]$TargetDir,
    # 新编译文件所在目录。默认为本脚本旁的 update\ 目录（已预放示例文件）。
    [string]$SourceDir = (Join-Path $PSScriptRoot 'update'),
    # 需要覆盖的文件列表（#182/#989 修复涉及 Core/Shared/Shared.IPC，必须一并更新，避免混合程序集）。
    [string[]]$Files = @('ClassIsland.dll', 'ClassIsland.Core.dll', 'ClassIsland.Shared.dll', 'ClassIsland.Shared.IPC.dll'),
    # 更新后不自动启动程序。
    [switch]$NoRestart
)

$ErrorActionPreference = 'Stop'

function Fail([string]$msg) {
    Write-Host "[失败] $msg" -ForegroundColor Red
    exit 1
}

# ---------- 1. 定位安装目录 ----------
if (-not $TargetDir) {
    $proc = Get-Process -Name 'ClassIsland' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($proc -and $proc.Path) {
        $TargetDir = Split-Path -Parent $proc.Path
        Write-Host "[信息] 从运行中的进程检测到安装目录：$TargetDir"
    }
}
if (-not $TargetDir) {
    Fail '无法自动检测安装目录（程序未运行）。请用 -TargetDir 指定，例如：.\patch.ps1 -TargetDir "D:\ClassIsland"'
}
if (-not (Test-Path -LiteralPath $TargetDir)) {
    Fail "安装目录不存在：$TargetDir"
}
$TargetDir = (Resolve-Path -LiteralPath $TargetDir).Path

$targetExe = Join-Path $TargetDir 'ClassIsland.exe'
$targetDll = Join-Path $TargetDir 'ClassIsland.dll'
if (-not ((Test-Path -LiteralPath $targetExe) -or (Test-Path -LiteralPath $targetDll))) {
    Fail "$TargetDir 不像是 ClassIsland 安装目录（未找到 ClassIsland.exe / ClassIsland.dll）。已中止，未做任何修改。"
}

if (-not (Test-Path -LiteralPath $SourceDir)) {
    Fail "更新源目录不存在：$SourceDir"
}

# ---------- 2. 收集待更新文件 ----------
$sourceFiles = @()
foreach ($f in $Files) {
    $sp = Join-Path $SourceDir $f
    if (Test-Path -LiteralPath $sp) {
        $sourceFiles += $sp
    }
    else {
        Write-Host "[跳过] 源目录中不存在：$f"
    }
}
if ($sourceFiles.Count -eq 0) {
    Fail "源目录 $SourceDir 中没有任何待更新文件。请先把新编译的 ClassIsland.dll 放进去。"
}

# ---------- 3. 停止正在运行的程序 ----------
$wasRunning = $false
$running = Get-Process -Name 'ClassIsland' -ErrorAction SilentlyContinue
if ($running) {
    $wasRunning = $true
    Write-Host '[信息] 正在停止 ClassIsland …'
    $running | Stop-Process -Force
    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline) {
        $still = Get-Process -Name 'ClassIsland' -ErrorAction SilentlyContinue
        if (-not $still) { break }
        Start-Sleep -Milliseconds 500
    }
    if (Get-Process -Name 'ClassIsland' -ErrorAction SilentlyContinue) {
        Fail 'ClassIsland 未能在 30 秒内退出（可能被占用），已中止，未修改任何文件。'
    }
}

# ---------- 4. 备份 ----------
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupDir = Join-Path $TargetDir ("BackupPatches\" + $stamp)
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null
foreach ($sp in $sourceFiles) {
    $name = Split-Path -Leaf $sp
    $tp = Join-Path $TargetDir $name
    if (Test-Path -LiteralPath $tp) {
        Copy-Item -LiteralPath $tp -Destination (Join-Path $backupDir $name) -Force
    }
}
Write-Host "[信息] 已备份原文件到：$backupDir"

# ---------- 5. 原地覆盖 ----------
foreach ($sp in $sourceFiles) {
    $name = Split-Path -Leaf $sp
    Copy-Item -LiteralPath $sp -Destination (Join-Path $TargetDir $name) -Force
    Write-Host "[更新] $name"
}

# ---------- 6. 重启 ----------
if (-not $NoRestart -and (Test-Path -LiteralPath $targetExe)) {
    Write-Host '[信息] 正在启动 ClassIsland …'
    Start-Process -FilePath $targetExe -WorkingDirectory $TargetDir
}

Write-Host ''
Write-Host '[完成] 补丁已应用。' -ForegroundColor Green
Write-Host '  · 安装路径与文件名未变化 → C 盘快照里的开机自启快捷方式继续有效，无需任何操作。'
Write-Host '  · 配置与插件均在安装目录（D 盘），还原系统不会回滚。'
Write-Host "  · 回滚方法：退出程序，将 $backupDir 中的文件复制回安装目录。"
