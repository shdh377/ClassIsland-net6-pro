using System.IO;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;
using static IslandCaller.Services.ProfileService;

namespace IslandCaller.Helpers;

public class CsvParseHelper
{
    public ILogger<CsvParseHelper> logger = IAppHost.GetService<ILogger<CsvParseHelper>>();

    public List<Person> ParseCsvFile(string filePath, int nameRow)
    {
        logger.LogInformation("开始解析 CSV 名单，文件路径: {Path}，姓名列: {NameRow}", filePath, nameRow);
        string content = File.ReadAllText(filePath);
        var list = new List<Person>();
        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        logger.LogDebug("CSV 原始行数: {LineCount}", lines.Length);
        for (int i = 0; i < lines.Length; i++)
        {
            var columns = lines[i].Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length > nameRow)
            {
                string name = columns[nameRow].Trim();
                list.Add(new Person
                {
                    Id = i + 1,
                    Name = name,
                    ManualWeight = 1.0
                });
            }
        }
        logger.LogInformation("CSV 名单解析完成，成功导入 {Count} 人", list.Count);
        return list;
    }
}
