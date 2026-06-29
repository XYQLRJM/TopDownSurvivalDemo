using System;

/// <summary>
/// 单个怪物在关卡刷怪表中的权重配置。
/// </summary>
[Serializable]
public class StageMonsterWeightConfig
{
    /// <summary>怪物配置 id。</summary>
    public string monsterId;
    /// <summary>随机刷出该怪物的权重。</summary>
    public int weight;
}

/// <summary>
/// 单个关卡的刷怪配置。
/// </summary>
[Serializable]
public class StageSpawnConfig
{
    /// <summary>关卡编号。</summary>
    public int stage;
    /// <summary>每波生成的预警点数量。</summary>
    public int spawnCount;
    /// <summary>两波刷怪之间的间隔。</summary>
    public float spawnInterval;
    /// <summary>本关可刷出的怪物及其权重。</summary>
    public StageMonsterWeightConfig[] monsters;
}

/// <summary>
/// 用于让 JsonUtility 解析关卡刷怪数组的包装类型。
/// </summary>
[Serializable]
public class StageSpawnConfigList
{
    /// <summary>全部关卡刷怪配置。</summary>
    public StageSpawnConfig[] stages;
}
