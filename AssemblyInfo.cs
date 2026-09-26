using System.Reflection;
using System.Runtime.Versioning;

// 本仓库发布标签带 v 前缀（如 v1.1.2），AssemblyVersion 只接受数字格式；此处固定为数字版本，git 信息保留在 InformationalVersion。
[assembly: AssemblyVersion("1.1.2")]
[assembly: AssemblyInformationalVersion($"{ThisAssembly.Git.BaseTag}+{ThisAssembly.Git.Sha}")]
[assembly: AssemblyTitle("ClassIsland")]
[assembly: AssemblyProduct("ClassIsland")]
#if NETCOREAPP
[assembly: SupportedOSPlatform("Windows")]
#endif