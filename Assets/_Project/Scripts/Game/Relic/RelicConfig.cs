using System;

/// <summary>
/// 从 relics.json 读取的商店遗物配置。
/// </summary>
[Serializable]
public class RelicConfig
{
    /// <summary>遗物唯一 id。</summary>
    public string id;
    /// <summary>遗物显示名称。</summary>
    public string name;
    /// <summary>遗物稀有度。</summary>
    public string rarity;
    /// <summary>限定可使用角色，空字符串表示所有角色可用。</summary>
    public string exclusiveCharacter;
    /// <summary>遗物描述文本。</summary>
    public string description;
    /// <summary>遗物图标在 Resources 中的路径。</summary>
    public string spritePath;
    /// <summary>遗物包含的效果列表。</summary>
    public RelicEffectConfig[] effects;
}

/// <summary>
/// 遗物配置中的单个效果条目。
/// </summary>
[Serializable]
public class RelicEffectConfig
{
    /// <summary>效果类型。</summary>
    public string type;
    /// <summary>效果数值。</summary>
    public float value;
    /// <summary>周期类效果的触发间隔。</summary>
    public float interval;
    /// <summary>伤害倍率类效果的倍率。</summary>
    public float damageMultiplier;
    /// <summary>按最大生命百分比恢复时使用的比例。</summary>
    public float hpPercent;
    /// <summary>属性重排类效果使用的顺序配置。</summary>
    public string[] order;
}

/// <summary>
/// 用于让 JsonUtility 解析遗物数组的包装类型。
/// </summary>
[Serializable]
public class RelicConfigList
{
    /// <summary>全部遗物配置。</summary>
    public RelicConfig[] relics;
}
