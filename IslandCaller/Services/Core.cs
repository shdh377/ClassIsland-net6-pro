using System.IO;
using Microsoft.Extensions.Logging;

namespace IslandCaller.Services;

public class CoreService(ProfileService profileService, HistoryService historyService, ILogger<CoreService> logger, Status status)
{
    private readonly ProfileService profileService = profileService;
    private readonly HistoryService historyService = historyService;
    private readonly ILogger<CoreService> logger = logger;
    private readonly Status status = status;
    Random rand = new Random();

    internal class Person
    {
        internal int Id { get; set; }
        internal string Name { get; set; }
        internal double ManualWeight { get; set; } = 1.0;
        internal double Weight { get; set; }
    }

    internal List<Person> Persons { get; set; } = new List<Person>();

    internal void InitializeCore()
    {
        logger.LogInformation("初始化 Core 模块，加载学生信息...");
        RebuildPersons();
        status.CoreServiceInitialized = true;
    }

    /// <summary>
    /// 轻量级更新权重：仅根据当前名单重建内存中的学生权重，
    /// 不做文件 I/O、不修改 Status 标志。用于编辑档案时即时生效。
    /// </summary>
    internal void ApplyMemberWeights()
    {
        RebuildPersons();
    }

    private void RebuildPersons()
    {
        Persons.Clear();
        foreach (var person in profileService.Members)
        {
            Persons.Add(new Person
            {
                Id = person.Id,
                Name = person.Name,
                ManualWeight = person.ManualWeight,
                Weight = 0.0
            });
        }
        ComputeWeightsForAllStudents();
    }

    private double ComputeSingleWeight(
                            double manualWeight,
                            int lastHitStep,
                            int nHist,
                            double avgHist)
    {
        const double fMin = 0;
        const double beta = 0.54;

        int deltaS = lastHitStep;
        if (deltaS < 0) deltaS = 15;

        double F_session = 1 - (1 - fMin) * Math.Exp(-beta * deltaS);

        const double eps = 1.0;
        const double gamma = 0.9;
        const double rMin = 0.6;
        const double rMax = 1.6;

        double ratio = (manualWeight * avgHist + eps) / (nHist + eps);
        double F_history = Math.Pow(ratio, gamma);
        F_history = Math.Max(rMin, Math.Min(rMax, F_history));

        return manualWeight * F_session * F_history;
    }

    private void ComputeWeightsForAllStudents()
    {
        double avgHist = historyService.GetAverageLongTermCount();
        logger.LogTrace("计算全班历史平均被点次数: {AvgHist}", avgHist);
        foreach (var person in Persons)
        {
            int nHist = historyService.GetLongTermCount(person.Name);
            int lastHitStep = historyService.GetLastCallIndex(person.Name);
            double weight = ComputeSingleWeight(
                                    person.ManualWeight,
                                    lastHitStep,
                                    nHist,
                                    avgHist);
            person.Weight = weight;
            logger.LogTrace("计算权重 - 学生: {Name}, ManualWeight: {ManualWeight}, LastHitStep: {LastHitStep}, nHist: {NHist}, Weight: {Weight}",
                person.Name, person.ManualWeight, lastHitStep, nHist, weight);
        }
    }

    internal string GetRandomStudent()
    {
        double totalWeight = Persons.Sum(p => p.Weight);
        logger.LogTrace("计算权重总和: {TotalWeight}", totalWeight);
        if (totalWeight == 0) return "Error";
        double r = rand.NextDouble() * totalWeight;
        logger.LogTrace("生成随机数: {R} (范围: [0, {TotalWeight}))", r, totalWeight);
        double cumulative = 0;
        foreach (var person in Persons)
        {
            cumulative += person.Weight;
            if (r < cumulative)
            {
                historyService.Add(person.Name);
                logger.LogTrace("抽取到学生：{Name}", person.Name);
                ComputeWeightsForAllStudents();
                return person.Name;
            }
        }
        logger.LogWarning("随机选择学生时发生了意外情况，权重总和: {TotalWeight}, 随机数: {R}", totalWeight, r);
        return "Error";
    }
}
