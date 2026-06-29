using UnityEngine;

/// <summary>
/// 玩家战斗驱动，负责冷却并把具体攻击行为交给策略类。
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    /// <summary>玩家自动攻击间隔。</summary>
    [SerializeField] private float attackInterval = 0.65f;

    /// <summary>攻击策略执行时需要的上下文。</summary>
    private PlayerAttackContext context;
    /// <summary>当前角色使用的攻击策略。</summary>
    private IPlayerAttackStrategy attackStrategy;
    /// <summary>下一次允许攻击的时间。</summary>
    private float nextAttackTime;

    /// <summary>初始化玩家战斗组件并根据攻击类型创建策略。</summary>
    public void Init(PlayerController2D owner, PlayerRuntimeData data, MonsterSpawner monsterSpawner)
    {
        context = new PlayerAttackContext(owner, data, monsterSpawner, owner.GetComponent<PlayerRelicEffects>());
        attackStrategy = CreateStrategy(data.AttackType);
    }

    /// <summary>冷却结束后尝试自动攻击。</summary>
    private void Update()
    {
        if (!IsReady())
            return;

        if (attackStrategy.TryAttack(context))
            nextAttackTime = Time.time + attackInterval;
    }

    /// <summary>判断当前是否满足攻击条件。</summary>
    private bool IsReady()
    {
        return context.Player != null
            && context.RuntimeData != null
            && context.Spawner != null
            && attackStrategy != null
            && Time.time >= nextAttackTime;
    }

    /// <summary>根据角色攻击类型创建近战或远程策略。</summary>
    private IPlayerAttackStrategy CreateStrategy(string attackType)
    {
        switch (attackType)
        {
            case "ranged":
                return new RangedAttackStrategy();
            case "melee":
            default:
                return new MeleeAttackStrategy();
        }
    }
}
