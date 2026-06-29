using System;

/// <summary>
/// 从 monsters.json 读取的怪物或 Boss 属性。
/// </summary>
[Serializable]
public class MonsterConfig
{
    /// <summary>怪物唯一 id。</summary>
    public string id;
    /// <summary>怪物显示名称。</summary>
    public string name;
    /// <summary>怪物最大生命。</summary>
    public int maxHp;
    /// <summary>怪物攻击力。</summary>
    public int attack;
    /// <summary>怪物防御力。</summary>
    public int defense;
    /// <summary>怪物移动速度配置值。</summary>
    public int moveSpeed;
    /// <summary>怪物行为类型，用来区分近战、远程、Boss 等逻辑。</summary>
    public string behaviorType;
    /// <summary>怪物预制体在 Resources 中的路径。</summary>
    public string prefabPath;
}

/// <summary>
/// 用于让 JsonUtility 解析怪物数组的包装类型。
/// </summary>
[Serializable]
public class MonsterConfigList
{
    /// <summary>全部怪物和 Boss 配置。</summary>
    public MonsterConfig[] monsters;
}
