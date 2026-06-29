/// <summary>
/// 玩家攻击策略共用的上下文数据。
/// </summary>
public readonly struct PlayerAttackContext
{
    /// <summary>执行攻击的玩家控制器。</summary>
    public readonly PlayerController2D Player;
    /// <summary>玩家当前运行时属性。</summary>
    public readonly PlayerRuntimeData RuntimeData;
    /// <summary>当前关卡刷怪器，用来读取场上怪物。</summary>
    public readonly MonsterSpawner Spawner;
    /// <summary>玩家当前拥有的遗物效果。</summary>
    public readonly PlayerRelicEffects RelicEffects;

    /// <summary>创建一次攻击所需的上下文数据。</summary>
    public PlayerAttackContext(PlayerController2D player, PlayerRuntimeData runtimeData, MonsterSpawner spawner, PlayerRelicEffects relicEffects)
    {
        Player = player;
        RuntimeData = runtimeData;
        Spawner = spawner;
        RelicEffects = relicEffects;
    }
}
