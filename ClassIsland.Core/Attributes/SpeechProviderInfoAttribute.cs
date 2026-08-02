namespace ClassIsland.Core.Attributes;

/// <summary>
/// 语音提供方信息
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SpeechProviderInfoAttribute : Attribute
{
    /// <summary>
    /// 语音提供方 ID
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// 语音提供方名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 语音提供方描述
    /// </summary>
    public string Description { get; } = "";

    /// <inheritdoc />
    public SpeechProviderInfoAttribute(string id, string name, string description = "")
    {
        Id = id;
        Name = name;
        Description = description;
    }
}
