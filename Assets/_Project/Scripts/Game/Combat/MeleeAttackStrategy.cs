using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战士近战攻击：先选中面向方向目标，再对 120 度扇形内目标造成伤害。
/// </summary>
public class MeleeAttackStrategy : IPlayerAttackStrategy
{
    /// <summary>战士近战攻击半径。</summary>
    private const float AttackRange = 2.6f;
    /// <summary>扇形攻击左右各自的角度。</summary>
    private const float HalfAngle = 60f;

    /// <summary>尝试以玩家面向方向发动一次近战扇形攻击。</summary>
    public bool TryAttack(PlayerAttackContext context)
    {
        MonsterController centerTarget = FindNearestFacingMonster(context);
        BossController bossTarget = FindFacingBoss(context);
        Vector2? centerPosition = GetNearestCenterPosition(context, centerTarget, bossTarget);
        if (!centerPosition.HasValue)
            return false;

        context.Player.TriggerAttack();
        MusicMgr.Instance.PlaySound("sword-stab-body-hit");
        DamageSector(context, centerPosition.Value);
        return true;
    }

    /// <summary>查找玩家面向方向上最近的小怪。</summary>
    private MonsterController FindNearestFacingMonster(PlayerAttackContext context)
    {
        MonsterController nearest = null;
        float nearestSqr = float.MaxValue;
        Vector2 origin = context.Player.transform.position;
        Vector2 facing = context.Player.LookDirection;

        foreach (MonsterController monster in context.Spawner.Monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            Vector2 toMonster = (Vector2)monster.transform.position - origin;
            float sqr = toMonster.sqrMagnitude;
            if (sqr > AttackRange * AttackRange)
                continue;

            if (Vector2.Dot(facing, toMonster.normalized) <= 0f)
                continue;

            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = monster;
            }
        }

        return nearest;
    }

    /// <summary>查找玩家面向方向上的 Boss。</summary>
    private BossController FindFacingBoss(PlayerAttackContext context)
    {
        BossController boss = Object.FindObjectOfType<BossController>();
        if (boss == null || !boss.IsAlive)
            return null;

        Vector2 origin = context.Player.transform.position;
        Vector2 toBoss = (Vector2)boss.transform.position - origin;
        if (toBoss.sqrMagnitude > AttackRange * AttackRange)
            return null;

        return Vector2.Dot(context.Player.LookDirection, toBoss.normalized) > 0f ? boss : null;
    }

    /// <summary>在小怪和 Boss 中选择离玩家最近的攻击中心。</summary>
    private Vector2? GetNearestCenterPosition(PlayerAttackContext context, MonsterController monster, BossController boss)
    {
        if (monster == null && boss == null)
            return null;
        if (monster == null)
            return boss.transform.position;
        if (boss == null)
            return monster.transform.position;

        Vector2 origin = context.Player.transform.position;
        float monsterSqr = ((Vector2)monster.transform.position - origin).sqrMagnitude;
        float bossSqr = ((Vector2)boss.transform.position - origin).sqrMagnitude;
        return bossSqr < monsterSqr ? boss.transform.position : monster.transform.position;
    }

    /// <summary>以目标方向为中心，对扇形范围内的小怪和 Boss 结算伤害。</summary>
    private void DamageSector(PlayerAttackContext context, Vector2 centerTargetPosition)
    {
        Vector2 origin = context.Player.transform.position;
        Vector2 centerDir = (centerTargetPosition - origin).normalized;
        List<MonsterController> targets = new List<MonsterController>();

        foreach (MonsterController monster in context.Spawner.Monsters)
        {
            if (monster == null || !monster.IsAlive)
                continue;

            Vector2 toMonster = (Vector2)monster.transform.position - origin;
            if (toMonster.sqrMagnitude > AttackRange * AttackRange)
                continue;

            if (Vector2.Angle(centerDir, toMonster.normalized) <= HalfAngle)
                targets.Add(monster);
        }

        foreach (MonsterController target in targets)
        {
            float critMultiplier = context.RelicEffects != null ? context.RelicEffects.CritDamageMultiplier : 2f;
            DamageResult result = DamageUtil.CalculateDamageResult(context.RuntimeData.Attack, target.Defense, context.RuntimeData.CritRate, critMultiplier);
            target.TakeDamage(result.Damage, origin, result.IsCrit);
            context.RelicEffects?.OnDealDamage(result.Damage, result.IsCrit);
        }

        BossController boss = Object.FindObjectOfType<BossController>();
        if (boss == null || !boss.IsAlive)
            return;

        Vector2 toBoss = (Vector2)boss.transform.position - origin;
        if (toBoss.sqrMagnitude > AttackRange * AttackRange)
            return;

        if (Vector2.Angle(centerDir, toBoss.normalized) > HalfAngle)
            return;

        float bossMultiplier = context.RelicEffects != null ? context.RelicEffects.BossDamageMultiplier : 1f;
        float bossCritMultiplier = context.RelicEffects != null ? context.RelicEffects.CritDamageMultiplier : 2f;
        DamageResult bossResult = DamageUtil.CalculateDamageResult(context.RuntimeData.Attack, boss.Defense, context.RuntimeData.CritRate, bossCritMultiplier, bossMultiplier);
        boss.TakeDamage(bossResult.Damage, origin, bossResult.IsCrit);
        context.RelicEffects?.OnDealDamage(bossResult.Damage, bossResult.IsCrit);
    }
}
