using System;
using UnityEngine;

/// <summary>
/// 创建并跟踪 Boss 关卡。
/// </summary>
public class GameBossStageService
{
    /// <summary>Boss 关卡追踪和攻击的玩家。</summary>
    private readonly PlayerController2D player;
    /// <summary>Boss 关仍然使用的小怪刷怪器。</summary>
    private readonly MonsterSpawner monsterSpawner;
    /// <summary>用于绑定 Boss 血条的游戏主界面。</summary>
    private readonly GamePanel gamePanel;
    /// <summary>Boss 死亡后通知关卡管理器的回调。</summary>
    private readonly Action<BossController> onBossDead;

    /// <summary>当前关卡正在生效的 Boss。</summary>
    public BossController CurrentBoss { get; private set; }

    /// <summary>创建 Boss 关卡服务并保存依赖对象。</summary>
    public GameBossStageService(PlayerController2D player, MonsterSpawner monsterSpawner, GamePanel gamePanel, Action<BossController> onBossDead)
    {
        this.player = player;
        this.monsterSpawner = monsterSpawner;
        this.gamePanel = gamePanel;
        this.onBossDead = onBossDead;
    }

    /// <summary>判断指定关卡是否为 Boss 关。</summary>
    public static bool IsBossStage(int stage)
    {
        return stage == 5 || stage == 10;
    }

    /// <summary>启动 Boss 关卡的小怪刷怪和 Boss 生成。</summary>
    public void StartBossStage(int stage)
    {
        monsterSpawner?.SetStage(stage);
        monsterSpawner?.SetSpawning(true);
        SpawnBoss(stage);
    }

    /// <summary>绑定当前 Boss 并刷新 Boss 血条 UI。</summary>
    public void BindBoss(BossController boss)
    {
        CurrentBoss = boss;
        gamePanel?.BindBoss(CurrentBoss);
    }

    /// <summary>清空当前 Boss 引用并隐藏 Boss 血条。</summary>
    public void ClearBoss()
    {
        CurrentBoss = null;
        gamePanel?.BindBoss(null);
    }

    /// <summary>销毁当前 Boss 对象并清空 UI 绑定。</summary>
    public void DestroyBoss()
    {
        if (CurrentBoss != null)
            UnityEngine.Object.Destroy(CurrentBoss.gameObject);
        ClearBoss();
    }

    /// <summary>按关卡配置生成对应 Boss。</summary>
    private void SpawnBoss(int stage)
    {
        MonsterConfig config = GetBossConfig(stage);
        GameObject prefab = config != null ? Resources.Load<GameObject>(config.prefabPath) : null;
        if (prefab == null || player == null)
        {
            Debug.LogError($"Boss prefab not found for stage {stage}: {config?.prefabPath}");
            return;
        }

        GameObject bossObj = UnityEngine.Object.Instantiate(prefab, GetRandomBossPosition(), Quaternion.identity);
        bossObj.name = config.id;
        BossController boss = bossObj.GetComponent<BossController>();
        if (boss == null)
            boss = bossObj.AddComponent<BossController>();

        boss.Init(config, player, monsterSpawner, GetMapBounds(), onBossDead);
        BindBoss(boss);
    }

    /// <summary>按关卡编号读取 Boss 配置。</summary>
    private MonsterConfig GetBossConfig(int stage)
    {
        if (stage == 5)
            return MonsterConfigLoader.GetMonster("boss_1");
        if (stage == 10)
            return MonsterConfigLoader.GetMonster("boss_2");

        return null;
    }

    /// <summary>读取地图边界，找不到地图时使用默认范围。</summary>
    private Bounds GetMapBounds()
    {
        SpriteRenderer mapRenderer = GameObject.Find("Map1")?.GetComponent<SpriteRenderer>();
        return mapRenderer != null ? mapRenderer.bounds : new Bounds(Vector3.zero, new Vector3(10f, 10f, 0f));
    }

    /// <summary>随机生成一个离玩家较远的 Boss 出生点。</summary>
    private Vector3 GetRandomBossPosition()
    {
        Bounds bounds = GetMapBounds();
        for (int i = 0; i < 20; ++i)
        {
            Vector3 pos = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                0f);

            if (player == null || Vector2.Distance(pos, player.transform.position) > 5f)
                return pos;
        }

        return bounds.center;
    }
}
