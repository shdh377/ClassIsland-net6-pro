using System.IO;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static IslandCaller.Services.ProfileService;

namespace IslandCaller.Helpers;

public class SecRandomParseHelper
{
    public ILogger<SecRandomParseHelper> logger = IAppHost.GetService<ILogger<SecRandomParseHelper>>();

    public List<Person> ParseSecRandomProfile(string filePath)
    {
        logger.LogInformation("开始解析 SecRandom 名单，文件路径: {Path}", filePath);
        string content = File.ReadAllText(filePath);
        using var jsonDoc = JsonDocument.Parse(content);
        var list = new List<Person>();
        int i = 0;
        int rawCount = 0;
        foreach (var person in jsonDoc.RootElement.EnumerateObject())
        {
            rawCount++;
            i++;
            list.Add(new Person
            {
                Id = i,
                Name = person.Name,
                ManualWeight = 1.0
            });
        }
        logger.LogDebug("SecRandom 原始人数: {PersonCount}", rawCount);
        logger.LogInformation("SecRandom 名单解析完成，成功导入 {Count} 人", list.Count);
        return list;
    }
}
