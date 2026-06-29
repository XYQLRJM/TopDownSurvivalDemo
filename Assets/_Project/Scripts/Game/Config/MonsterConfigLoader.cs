using UnityEngine;

/// <summary>
/// 从 Resources/Configs/monsters.json 加载怪物属性。
/// </summary>
public static class MonsterConfigLoader
{
    /// <summary>怪物配置文件在 Resources 中的路径。</summary>
    private const string ConfigPath = "Configs/monsters";

    /// <summary>读取全部怪物和 Boss 配置。</summary>
    public static MonsterConfig[] LoadMonsters()
    {
        TextAsset json = Resources.Load<TextAsset>(ConfigPath);
        if (json == null)
        {
            Debug.LogError($"Monster config not found: Resources/{ConfigPath}.json");
            return new MonsterConfig[0];
        }

        MonsterConfigList configList = JsonUtility.FromJson<MonsterConfigList>(json.text);
        return configList != null && configList.monsters != null
            ? configList.monsters
            : new MonsterConfig[0];
    }

    /// <summary>按照 id 查找一个怪物或 Boss 配置。</summary>
    public static MonsterConfig GetMonster(string id)
    {
        foreach (MonsterConfig config in LoadMonsters())
        {
            if (config.id == id)
                return config;
        }

        return null;
    }
}
