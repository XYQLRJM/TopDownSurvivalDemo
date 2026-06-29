using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 先显示刷怪预警，再按当前关卡配置生成怪物。
/// </summary>
public class MonsterSpawner : MonoBehaviour
{
    /// <summary>刷怪预警预制体在 Resources 中的路径。</summary>
    private const string TipPath = "Prefabs/Character/Monstertips";
    /// <summary>场上最多允许存在的小怪数量。</summary>
    private const int MaxMonsterCount = 100;
    /// <summary>找不到关卡配置时使用的默认刷怪间隔。</summary>
    private const float DefaultSpawnInterval = 4f;
    /// <summary>预警提示持续时间。</summary>
    private const float TipDuration = 2f;
    /// <summary>随机刷怪点距离玩家的最小安全半径。</summary>
    private const float PlayerSafeRadius = 3f;
    /// <summary>找不到关卡配置时使用的默认每波刷怪数量。</summary>
    private const int DefaultSpawnCountPerWave = 3;

    /// <summary>当前场上仍由刷怪器管理的小怪列表。</summary>
    private readonly List<MonsterController> monsters = new List<MonsterController>();
    /// <summary>从配置文件读取出的怪物配置。</summary>
    private MonsterConfig[] monsterConfigs;
    /// <summary>当前关卡的刷怪权重配置。</summary>
    private StageSpawnConfig currentStageConfig;
    /// <summary>当前玩家目标。</summary>
    private PlayerController2D player;
    /// <summary>玩家成长组件，用于传给怪物发放经验。</summary>
    private PlayerProgression playerProgression;
    /// <summary>地图边界，用于随机刷怪点。</summary>
    private Bounds mapBounds;
    /// <summary>已经显示预警但尚未生成的小怪数量。</summary>
    private int pendingSpawnCount;
    /// <summary>当前关卡编号。</summary>
    private int currentStage = 1;
    /// <summary>是否允许继续刷怪。</summary>
    private bool isSpawning;

    /// <summary>外部只读访问当前小怪列表。</summary>
    public IReadOnlyList<MonsterController> Monsters => monsters;

    /// <summary>初始化刷怪器依赖并启动刷怪循环。</summary>
    public void Init(PlayerController2D target, PlayerProgression progression, Bounds bounds)
    {
        player = target;
        playerProgression = progression;
        mapBounds = bounds;
        monsterConfigs = MonsterConfigLoader.LoadMonsters();
        currentStageConfig = StageSpawnConfigLoader.GetStage(currentStage);
        isSpawning = true;
        StartCoroutine(SpawnLoop());
    }

    /// <summary>开启或暂停自动刷怪。</summary>
    public void SetSpawning(bool value)
    {
        isSpawning = value;
    }

    /// <summary>更新刷怪器当前关卡。</summary>
    public void SetStage(int stage)
    {
        currentStage = Mathf.Max(1, stage);
        currentStageConfig = StageSpawnConfigLoader.GetStage(currentStage);
    }

    /// <summary>立即在指定位置生成指定 id 的怪物。</summary>
    public MonsterController SpawnMonsterImmediate(string monsterId, Vector3 pos)
    {
        MonsterConfig monsterConfig = FindMonsterConfig(monsterId);
        if (monsterConfig == null)
            return null;

        return SpawnMonster(monsterConfig, pos);
    }

    /// <summary>循环生成每波刷怪预警。</summary>
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            CleanupMonsterList();
            int availableCount = MaxMonsterCount - monsters.Count - pendingSpawnCount;
            if (isSpawning && monsterConfigs != null && monsterConfigs.Length > 0 && player != null && availableCount > 0)
            {
                int spawnCount = Mathf.Min(GetSpawnCountPerWave(), availableCount);
                for (int i = 0; i < spawnCount; ++i)
                {
                    pendingSpawnCount++;
                    StartCoroutine(SpawnOne(GetRandomSpawnPosition()));
                }
            }

            yield return new WaitForSeconds(GetSpawnInterval());
        }
    }

    /// <summary>显示预警，等待结束后生成一只随机怪物。</summary>
    private IEnumerator SpawnOne(Vector3 pos)
    {
        GameObject tip = PoolMgr.Instance.GetObj(TipPath);
        EnsurePoolObj(tip, 30);
        tip.transform.position = pos;
        yield return new WaitForSeconds(TipDuration);

        CleanupMonsterList();
        pendingSpawnCount = Mathf.Max(0, pendingSpawnCount - 1);
        if (tip != null)
            PoolMgr.Instance.PushObj(tip);

        if (!isSpawning || player == null || monsters.Count >= MaxMonsterCount)
            yield break;

        MonsterConfig monsterConfig = GetRandomMonsterConfig();
        if (monsterConfig == null)
            yield break;

        SpawnMonster(monsterConfig, pos);
    }

    /// <summary>从对象池取出怪物并初始化。</summary>
    private MonsterController SpawnMonster(MonsterConfig monsterConfig, Vector3 pos)
    {
        GameObject monsterObj = PoolMgr.Instance.GetObj(monsterConfig.prefabPath);
        EnsurePoolObj(monsterObj, MaxMonsterCount);
        monsterObj.transform.position = pos;

        MonsterController controller = monsterObj.GetComponent<MonsterController>();
        if (controller == null)
            controller = monsterObj.AddComponent<MonsterController>();

        controller.Init(monsterConfig, player, playerProgression);
        monsters.Add(controller);
        return controller;
    }

    /// <summary>按当前关卡配置的权重随机选择怪物配置。</summary>
    private MonsterConfig GetRandomMonsterConfig()
    {
        StageMonsterWeightConfig[] weights = currentStageConfig != null ? currentStageConfig.monsters : null;
        if (weights == null || weights.Length == 0)
            return null;

        int totalWeight = 0;
        for (int i = 0; i < weights.Length; ++i)
            totalWeight += Mathf.Max(0, weights[i].weight);

        if (totalWeight <= 0)
            return null;

        int randomWeight = Random.Range(0, totalWeight);
        for (int i = 0; i < weights.Length; ++i)
        {
            int weight = Mathf.Max(0, weights[i].weight);
            if (randomWeight < weight)
                return FindMonsterConfig(weights[i].monsterId);

            randomWeight -= weight;
        }

        return FindMonsterConfig(weights[0].monsterId);
    }

    /// <summary>读取当前关卡每波刷怪数量。</summary>
    private int GetSpawnCountPerWave()
    {
        return currentStageConfig != null && currentStageConfig.spawnCount > 0
            ? currentStageConfig.spawnCount
            : DefaultSpawnCountPerWave;
    }

    /// <summary>读取当前关卡刷怪间隔。</summary>
    private float GetSpawnInterval()
    {
        return currentStageConfig != null && currentStageConfig.spawnInterval > 0f
            ? currentStageConfig.spawnInterval
            : DefaultSpawnInterval;
    }

    /// <summary>按照 id 在配置表中查找怪物配置。</summary>
    private MonsterConfig FindMonsterConfig(string id)
    {
        if (monsterConfigs == null || string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < monsterConfigs.Length; ++i)
        {
            if (monsterConfigs[i].id == id)
                return monsterConfigs[i];
        }

        return null;
    }

    /// <summary>随机取一个地图内且离玩家较远的刷怪点。</summary>
    private Vector3 GetRandomSpawnPosition()
    {
        for (int i = 0; i < 20; ++i)
        {
            Vector3 pos = new Vector3(
                Random.Range(mapBounds.min.x, mapBounds.max.x),
                Random.Range(mapBounds.min.y, mapBounds.max.y),
                0f);

            if (player == null || Vector2.Distance(pos, player.transform.position) >= PlayerSafeRadius)
                return pos;
        }

        return mapBounds.center;
    }

    /// <summary>清理已经死亡或失活的小怪引用。</summary>
    private void CleanupMonsterList()
    {
        for (int i = monsters.Count - 1; i >= 0; --i)
        {
            if (monsters[i] == null || !monsters[i].gameObject.activeSelf || !monsters[i].IsAlive)
                monsters.RemoveAt(i);
        }
    }

    /// <summary>确保池化对象有 PoolObj 标记并设置容量。</summary>
    private void EnsurePoolObj(GameObject obj, int maxNum)
    {
        if (obj == null)
            return;

        PoolObj poolObj = obj.GetComponent<PoolObj>();
        if (poolObj == null)
            poolObj = obj.AddComponent<PoolObj>();
        if (poolObj.maxNum <= 0)
            poolObj.maxNum = maxNum;
    }
}
