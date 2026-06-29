using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从 Resources 加载并筛选遗物配置。
/// </summary>
public static class RelicConfigLoader
{
    /// <summary>遗物配置文件在 Resources 中的路径。</summary>
    private const string ConfigPath = "Configs/relics";
    /// <summary>缓存后的遗物配置，避免重复解析 JSON。</summary>
    private static RelicConfig[] cachedRelics;

    /// <summary>读取全部遗物配置。</summary>
    public static RelicConfig[] LoadRelics()
    {
        if (cachedRelics != null)
            return cachedRelics;

        TextAsset json = Resources.Load<TextAsset>(ConfigPath);
        if (json == null)
        {
            Debug.LogError($"Relic config not found: Resources/{ConfigPath}.json");
            cachedRelics = new RelicConfig[0];
            return cachedRelics;
        }

        RelicConfigList list = JsonUtility.FromJson<RelicConfigList>(json.text);
        cachedRelics = list != null && list.relics != null ? list.relics : new RelicConfig[0];
        return cachedRelics;
    }

    /// <summary>按稀有度和角色限定筛选可出售遗物。</summary>
    public static List<RelicConfig> GetRelics(string rarity, string characterId)
    {
        List<RelicConfig> result = new List<RelicConfig>();
        RelicConfig[] relics = LoadRelics();
        for (int i = 0; i < relics.Length; ++i)
        {
            RelicConfig relic = relics[i];
            if (relic.rarity != rarity)
                continue;

            if (!string.IsNullOrEmpty(relic.exclusiveCharacter) && relic.exclusiveCharacter != characterId)
                continue;

            result.Add(relic);
        }

        return result;
    }

    /// <summary>按遗物 id 查找单个遗物配置。</summary>
    public static RelicConfig GetRelic(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        RelicConfig[] relics = LoadRelics();
        for (int i = 0; i < relics.Length; ++i)
        {
            if (relics[i].id == id)
                return relics[i];
        }

        return null;
    }
}
