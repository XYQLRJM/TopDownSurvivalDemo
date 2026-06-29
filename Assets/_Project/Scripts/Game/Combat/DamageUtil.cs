using UnityEngine;

/// <summary>
/// 伤害计算结果，包含最终伤害和是否暴击。
/// </summary>
public readonly struct DamageResult
{
    /// <summary>最终造成的伤害。</summary>
    public readonly int Damage;
    /// <summary>本次伤害是否暴击。</summary>
    public readonly bool IsCrit;

    /// <summary>创建一次伤害计算结果。</summary>
    public DamageResult(int damage, bool isCrit)
    {
        Damage = damage;
        IsCrit = isCrit;
    }
}

/// <summary>
/// 统一处理伤害与暴击计算。
/// </summary>
public static class DamageUtil
{
    /// <summary>计算最终伤害，只返回伤害数值。</summary>
    public static int CalculateDamage(int attack, int defense, int critRate)
    {
        return CalculateDamageResult(attack, defense, critRate).Damage;
    }

    /// <summary>计算最终伤害和是否暴击。</summary>
    public static DamageResult CalculateDamageResult(
        int attack,
        int defense,
        int critRate,
        float critDamageMultiplier = 2f,
        float damageMultiplier = 1f)
    {
        int damage = Mathf.Max(1, attack - defense);
        bool isCrit = Random.Range(0, 100) < critRate;
        if (isCrit)
            damage = Mathf.RoundToInt(damage * Mathf.Max(1f, critDamageMultiplier));

        damage = Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Max(0f, damageMultiplier)));
        return new DamageResult(damage, isCrit);
    }
}
