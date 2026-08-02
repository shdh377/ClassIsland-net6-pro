using System.IO;
using IslandCaller.Models;
using System.Text;
using IslandCaller.Shared;

namespace IslandCaller.Services;

public class HistoryService(ProfileService profileService, Status status)
{
    private Dictionary<string, int> historyDict = new();
    private List<string> top20List = new();
    private ProfileService profileService = profileService;
    private Status Status = status;

    private string GetBasePath()
    {
        return AppPaths.HistoryFolder;
    }

    private string GetFilePath(Guid guid)
    {
        return Path.Combine(GetBasePath(), $"{guid}.txt");
    }

    public void Load(Guid guid)
    {
        Status.HistoryServiceInitialized = false;
        try
        {
            historyDict.Clear();

            string filePath = GetFilePath(guid);

            var csvDict = new Dictionary<string, int>();

            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);

                foreach (var line in lines)
                {
                    var parts = line.Split(',');

                    if (parts.Length != 2)
                        continue;

                    string name = parts[0].Trim();

                    if (int.TryParse(parts[1], out int count))
                    {
                        csvDict[name] = count;
                    }
                }
            }

            foreach (var person in profileService.Members)
            {
                string name = person.Name;

                if (csvDict.TryGetValue(name, out int count))
                {
                    historyDict[name] = count;
                }
                else
                {
                    historyDict[name] = 0;
                }
            }
        }
        finally
        {
            Status.HistoryServiceInitialized = true;
        }
    }

    public void Add(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (historyDict.ContainsKey(name))
            historyDict[name]++;
        else
            historyDict[name] = 1;

        top20List.Insert(0, name);

        if (top20List.Count > 20)
            top20List.RemoveAt(top20List.Count - 1);

        Guid guid = Settings.Instance.Profile.DefaultProfile;
        Save(guid);
    }

    private void Save(Guid guid)
    {
        string basePath = GetBasePath();
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        string filePath = GetFilePath(guid);

        StringBuilder sb = new();

        foreach (var pair in historyDict)
        {
            sb.AppendLine($"{pair.Key},{pair.Value}");
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    public int GetLongTermCount(string name)
    {
        if (historyDict.TryGetValue(name, out int count))
            return count;

        return 0;
    }

    public int GetLastCallIndex(string name)
    {
        int index = top20List.IndexOf(name);
        return index;
    }

    public double GetAverageLongTermCount()
    {
        if (historyDict.Count == 0)
            return 0;
        return historyDict.Values.Average();
    }

    public void ClearLongTermHistory()
    {
        historyDict.Clear();
        Save(Settings.Instance.Profile.DefaultProfile);
    }

    public void ClearThisLessonHistory()
    {
        top20List.Clear();
    }
}
