namespace ClassIsland.Core.Attributes;

/// <summary>
/// 提醒提供方信息
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class NotificationProviderInfoAttribute : Attribute
{
    /// <summary>
    /// 提醒提供方 GUID
    /// </summary>
    public Guid Guid { get; }

    /// <summary>
    /// 提醒提供方名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 提醒提供方描述
    /// </summary>
    public string Description { get; } = "";

    /// <summary>
    /// 提醒提供方设置界面类型
    /// </summary>
    public Type? SettingsType { get; internal set; }

    /// <summary>
    /// 提醒提供方类型
    /// </summary>
    public Type? ProviderType { get; internal set; }

    /// <summary>
    /// 是否注册了设置类型
    /// </summary>
    public bool HasSettings { get; internal set; }

    /// <inheritdoc />
    public NotificationProviderInfoAttribute(string guid, string name, string description = "")
    {
        Guid = Guid.Parse(guid);
        Name = name;
        Description = description;
    }

    /// <inheritdoc />
    public NotificationProviderInfoAttribute(string guid, string name, string iconGlyph, string description = "")
    {
        Guid = Guid.Parse(guid);
        Name = name;
        Description = description;
    }
}
