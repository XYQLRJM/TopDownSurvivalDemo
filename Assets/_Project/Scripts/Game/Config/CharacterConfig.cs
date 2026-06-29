using System;

/// <summary>
/// 从 characters.json 读取的角色初始属性。
/// </summary>
[Serializable]
public class CharacterConfig
{
    /// <summary>角色唯一 id。</summary>
    public string id;
    /// <summary>角色显示名称。</summary>
    public string name;
    /// <summary>攻击类型，melee 表示近战，ranged 表示远程。</summary>
    public string attackType;
    /// <summary>角色初始等级。</summary>
    public int level;
    /// <summary>角色初始最大生命。</summary>
    public int maxHp;
    /// <summary>角色初始攻击力。</summary>
    public int attack;
    /// <summary>角色初始防御力。</summary>
    public int defense;
    /// <summary>角色初始移动速度。</summary>
    public int moveSpeed;
    /// <summary>角色初始暴击率。</summary>
    public int critRate;
    /// <summary>角色预制体在 Resources 中的路径。</summary>
    public string prefabPath;
    /// <summary>动画片段名称前缀，例如 P001 或 P002。</summary>
    public string animPrefix;
}

/// <summary>
/// 用于让 JsonUtility 解析角色数组的包装类型。
/// </summary>
[Serializable]
public class CharacterConfigList
{
    /// <summary>全部角色配置。</summary>
    public CharacterConfig[] characters;
}
