using UnityEngine;

/// <summary>
/// 从 Resources/Configs/stage_spawns.json 加载关卡刷怪配置。
/// </summary>
public static class StageSpawnConfigLoader
{
    /// <summary>关卡刷怪配置文件在 Resources 中的路径。</summary>
    private const string ConfigPath = "Configs/stage_spawns";
    /// <summary>缓存后的关卡刷怪配置，避免重复解析 JSON。</summary>
    private static StageSpawnConfig[] cachedStages;

    /// <summary>读取全部关卡刷怪配置。</summary>
    public static StageSpawnConfig[] LoadStages()
    {
        if (cachedStages != null)
            return cachedStages;

        TextAsset json = Resources.Load<TextAsset>(ConfigPath);
        if (json == null)
        {
            Debug.LogError($"Stage spawn config not found: Resources/{ConfigPath}.json");
            cachedStages = new StageSpawnConfig[0];
            return cachedStages;
        }

        StageSpawnConfigList list = JsonUtility.FromJson<StageSpawnConfigList>(json.text);
        cachedStages = list != null && list.stages != null ? list.stages : new StageSpawnConfig[0];
        return cachedStages;
    }

    /// <summary>按关卡编号查找刷怪配置。</summary>
    public static StageSpawnConfig GetStage(int stage)
    {
        StageSpawnConfig[] stages = LoadStages();
        for (int i = 0; i < stages.Length; ++i)
        {
            if (stages[i].stage == stage)
                return stages[i];
        }

        return null;
    }
}
