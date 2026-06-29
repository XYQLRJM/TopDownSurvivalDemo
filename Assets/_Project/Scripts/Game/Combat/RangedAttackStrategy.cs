using UnityEngine;

/// <summary>
/// 弓手远程攻击：向射程内最近的怪物发射箭矢。
/// </summary>
public class RangedAttackStrategy : IPlayerAttackStrategy
{
    /// <summary>弓手自动索敌射程。</summary>
    private const float AttackRange = 5.5f;
    /// <summary>箭矢飞行速度。</summary>
    private const float ArrowSpeed = 14f;
    /// <summary>箭矢生成点相对玩家中心的偏移。</summary>
    private const float MuzzleOffset = 0.45f;
    /// <summary>箭矢音效从音频中的起播时间。</summary>
    private const float ArrowSoundStartTime = 1.5f;

    /// <summary>尝试向射程内最近目标发射箭矢。</summary>
    public bool TryAttack(PlayerAttackContext context)
    {
        MonsterController target = FindNearestMonster(context);
        BossController bossTarget = FindBossInRange(context);
        if (target == null && bossTarget == null)
            return false;

        Vector2 origin = context.Player.transform.position;
        Vector2 targetPosition = GetNearestTargetPosition(origin, target, bossTarget);
        Vector2 dir = (targetPosition - origin).normalized;
        context.Player.TriggerAttack();
        if (context.RelicEffects != null && context.RelicEffects.TripleShot)
        {
            SpawnArrow(context, dir);
            SpawnArrow(context, Rotate(dir, 30f));
            SpawnArrow(context, Rotate(dir, -30f));
        }
        else
        {
            ArrowProjectile.Spawn(context.Player.transform.position + (Vector3)(dir * MuzzleOffset), dir, context.RuntimeData.Attack, context.RuntimeData.CritRate, ArrowSpeed, context.RelicEffects);
        }

        MusicMgr.Instance.PlaySoundFrom("arrow", ArrowSoundStartTime);
        return true;
    }

    /// <summary>在小怪和 Boss 中选择离玩家最近的射击目标位置。</summary>
    private Vector2 GetNearestTargetPosition(Vector2 origin, MonsterController monster, BossController boss)
    {
        if (monster == null)
            return boss.transform.position;
        if (boss == null)
            return monster.transform.position;

        float monsterSqr = ((Vector2)monster.transform.position - origin).sqrMagnitude;
        float bossSqr = ((Vector2)boss.transform.position - origin).sqrMagnitude;
        return bossSqr < monsterSqr ? boss.transform.position : monster.transform.position;
    }

    /// <summary>按照方向生成一支箭。</summary>
    private void SpawnArrow(PlayerAttackContext context, Vector2 dir)
    {
        ArrowProjectile.Spawn(
            context.Player.transform.position + (Vector3)(dir.normalized * MuzzleOffset),
            dir,
            context.RuntimeData.Attack,
            context.RuntimeData.CritRate,
            ArrowSpeed,
            context.RelicEffects,
            context.RelicEffects.TripleShotDamageMultiplier);
    }

    /// <summary>将方向向量旋转指定角度，用于三箭分裂。</summary>
    private Vector2 Rotate(Vector2 dir, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(dir.x * cos - dir.y * sin, dir.x * sin + dir.y * cos).normalized;
    }

    /// <summary>查找射程内最近的小怪。</summary>
    private MonsterController FindNearestMonster(PlayerAttackContext context)
    {
        MonsterController nearest = null;
        float nearestSqr = float.MaxValue;
        Vector2 origin = context.Player.transform.position;
        float rangeSqr = AttackRange * AttackRange;

        foreach (MonsterController monster in context.Spawner.Monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            float sqr = ((Vector2)monster.transform.position - origin).sqrMagnitude;
            if (sqr > rangeSqr || sqr >= nearestSqr)
                continue;

            nearestSqr = sqr;
            nearest = monster;
        }

        return nearest;
    }

    /// <summary>查找射程内可攻击的 Boss。</summary>
    private BossController FindBossInRange(PlayerAttackContext context)
    {
        BossController boss = Object.FindObjectOfType<BossController>();
        if (boss == null || !boss.IsAlive)
            return null;

        float sqr = ((Vector2)boss.transform.position - (Vector2)context.Player.transform.position).sqrMagnitude;
        return sqr <= AttackRange * AttackRange ? boss : null;
    }
}
