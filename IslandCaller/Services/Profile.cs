using System.IO;
using ClassIsland.Shared;
using Microsoft.Extensions.Logging;
using System.Text;
using IslandCaller.Shared;

namespace IslandCaller.Services;

public class ProfileService
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Gender { get; set; }
        public double ManualWeight { get; set; } = 1.0;
    }

    public List<Person> Members { get; set; } = new List<Person>();

    private string GetBasePath()
    {
        return AppPaths.ProfileFolder;
    }

    private string GetFilePath(Guid guid)
    {
        return Path.Combine(GetBasePath(), $"{guid}.csv");
    }

    public void LoadSelectedProfile(Guid guid)
    {
        var logger = IAppHost.GetService<ILogger<ProfileService>>();
        var status = IAppHost.GetService<Status>();
        status.ProfileServiceInitialized = false;
        try
        {
            string filePath = GetFilePath(guid);

            if (!File.Exists(filePath))
            {
                logger.LogError("找不到对应的名单文件: {FilePath}", filePath);
                throw new FileNotFoundException($"找不到对应的名单文件: {filePath}");
            }

            string[] lines = File.ReadAllLines(filePath);

            if (lines.Length == 0)
            {
                logger.LogError("CSV 文件为空");
                throw new Exception("CSV 文件为空");
            }

            string header = lines[0].Trim();
            if (header != "id,name,gender,manualweight")
            {
                logger.LogError("CSV 标题格式错误，必须为: id,name,gender,manualweight");
                throw new Exception("CSV 标题格式错误，必须为: id,name,gender,manualweight");
            }

            Members.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(',');

                if (parts.Length != 4)
                {
                    logger.LogWarning("第 {Line} 行格式错误: {Content}", i + 1, line);
                    continue;
                }

                Members.Add(new Person
                {
                    Id = Convert.ToInt32(parts[0]),
                    Name = parts[1],
                    Gender = Convert.ToInt32(parts[2]),
                    ManualWeight = Convert.ToDouble(parts[3])
                });
            }
            Members = Members.OrderBy(x => x.Id).ToList();
        }
        finally
        {
            status.ProfileServiceInitialized = true;
        }
    }

    public List<Person> GetMembers(Guid guid)
    {
        var logger = IAppHost.GetService<ILogger<ProfileService>>();
        string filePath = GetFilePath(guid);

        if (!File.Exists(filePath))
        {
            logger.LogError("找不到对应的名单文件: {FilePath}", filePath);
            throw new FileNotFoundException($"找不到对应的名单文件: {filePath}");
        }

        string[] lines = File.ReadAllLines(filePath);

        if (lines.Length == 0)
        {
            logger.LogError("CSV 文件为空");
            throw new Exception("CSV 文件为空");
        }

        if (lines[0].Trim() != "id,name,gender,manualweight")
        {
            logger.LogError("CSV 标题格式错误，必须为: id,name,gender,manualweight");
            throw new Exception("CSV 标题格式错误，必须为: id,name,gender,manualweight");
        }

        List<Person> members = new List<Person>();

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(',');

            if (parts.Length != 4)
            {
                logger.LogWarning("第 {Line} 行格式错误: {Content}", i + 1, line);
                continue;
            }

            members.Add(new Person
            {
                Id = Convert.ToInt32(parts[0]),
                Name = parts[1],
                Gender = Convert.ToInt32(parts[2]),
                ManualWeight = Convert.ToDouble(parts[3])
            });
        }
        members = members.OrderBy(x => x.Id).ToList();
        return members;
    }

    public void SaveProfile(Guid guid, List<Person> members)
    {
        var logger = IAppHost.GetService<ILogger<ProfileService>>();
        members = members.OrderBy(x => x.Id).ToList();
        string basePath = GetBasePath();

        if (!Directory.Exists(basePath))
        {
            Directory.CreateDirectory(basePath);
        }

        string filePath = GetFilePath(guid);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("id,name,gender,manualweight");

        foreach (var person in members)
        {
            sb.AppendLine($"{person.Id},{person.Name},{person.Gender},{person.ManualWeight}");
        }

        try
        {
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "写入名单文件失败: {FilePath}", filePath);
            throw new Exception($"写入名单文件失败: {filePath}", ex);
        }
    }

    public void CreateDemoProfile(Guid guid)
    {
        List<Person> members = new List<Person>();
        members.Add(new Person
        {
            Id = 1,
            Gender = 0,
            Name = "小明",
            ManualWeight = 1.0
        });
        members.Add(new Person
        {
            Id = 2,
            Gender = 0,
            Name = "李明",
            ManualWeight = 1.0
        });
        members.Add(new Person
        {
            Id = 3,
            Gender = 1,
            Name = "李华",
            ManualWeight = 1.0
        });
        SaveProfile(guid, members);
    }
}
