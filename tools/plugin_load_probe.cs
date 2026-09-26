#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

// 复刻 ClassIsland PluginService.InitializePlugins 的入口识别逻辑，
// 验证插件在剥离宿主程序集后能否正确绑定到宿主的 ClassIsland.Core。
class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    public PluginLoadContext(string pluginPath) : base("ClassIsland.PluginLoadContext")
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }
}

class Probe
{
    static int Main(string[] args)
    {
        var hostDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var corePath = Path.Combine(hostDir, "ClassIsland.Core.dll");
        if (!File.Exists(corePath))
        {
            Console.WriteLine("PROBE FAIL: host Core not found at " + corePath);
            return 2;
        }

        var hostCore = AssemblyLoadContext.Default.LoadFromAssemblyPath(corePath);
        var pluginBaseType = hostCore.GetType("ClassIsland.Core.Abstractions.PluginBase", throwOnError: true)!;
        var entranceAttrType = hostCore.GetType("ClassIsland.Core.Attributes.PluginEntrance", throwOnError: true)!;

        var failed = 0;
        foreach (var pluginDir in args)
        {
            var asmName = Path.GetFileNameWithoutExtension(pluginDir.TrimEnd(Path.DirectorySeparatorChar));
            try
            {
                var dll = Directory.GetFiles(pluginDir, "*.dll")
                    .First(f => Path.GetFileName(f) != "ClassIsland.PluginSdk.dll"
                             && !Path.GetFileName(f).StartsWith("ClassIsland."));
                asmName = Path.GetFileNameWithoutExtension(dll);
                var alc = new PluginLoadContext(dll);
                var asm = alc.LoadFromAssemblyName(new AssemblyName(asmName));

                var entrance = asm.ExportedTypes.FirstOrDefault(x =>
                    x.BaseType == pluginBaseType ||
                    x.GetCustomAttributes().FirstOrDefault(a => a.GetType() == entranceAttrType) != null);

                if (entrance == null)
                {
                    Console.WriteLine($"{asmName}: NOT FOUND (入口识别失败)");
                    failed++;
                    continue;
                }

                var coreOfEntrance = entrance.BaseType!.Assembly;
                var boundToHost = ReferenceEquals(coreOfEntrance, hostCore);
                Console.WriteLine($"{asmName}: FOUND entrance={entrance.FullName} " +
                    $"baseCore={coreOfEntrance.GetName().Name}@{coreOfEntrance.Location} " +
                    $"boundToHost={boundToHost}");
                if (!boundToHost) failed++;

                // 模拟 Activator 创建入口实例（不执行 Initialize）
                var instance = Activator.CreateInstance(entrance);
                Console.WriteLine($"{asmName}: instantiate={(instance != null)}");
                if (instance == null) failed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{asmName ?? pluginDir}: EXCEPTION {ex.GetType().Name}: {ex.Message}");
                failed++;
            }
        }
        return failed == 0 ? 0 : 1;
    }
}
