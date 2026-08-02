using System.IO;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;
using static IslandCaller.Services.ProfileService;

namespace IslandCaller.Helpers;

public class TextFileParseHelper
{
    public ILogger<TextFileParseHelper> Logger = IAppHost.GetService<ILogger<TextFileParseHelper>>();

    public List<Person> ParseTextFile(string filePath)
    {
        Logger.LogInformation("开始解析文本名单，文件路径: {Path}", filePath);
        string content = File.ReadAllText(filePath);
        var list = new List<Person>();
        var names = content.Split(new[] { ' ', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        Logger.LogDebug("文本名单拆分后姓名数量: {NameCount}", names.Length);
        for (int i = 0; i < names.Length; i++)
        {
            list.Add(new Person
            {
                Id = i + 1,
                Name = names[i],
                ManualWeight = 1.0
            });
        }
        Logger.LogInformation("文本名单解析完成，成功导入 {Count} 人", list.Count);
        return list;
    }
}
