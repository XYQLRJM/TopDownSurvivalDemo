using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 应用单个遗物效果时传给处理器的数据。
/// </summary>
public readonly struct RelicEffectContext
{
    /// <summary>保存特殊遗物状态的拥有者组件。</summary>
    public readonly PlayerRelicEffects Relics;
    /// <summary>会被属性类遗物影响的玩家战斗属性。</summary>
    public readonly PlayerRuntimeData RuntimeData;
    /// <summary>会被金币或成长类遗物影响的玩家成长数据。</summary>
    public readonly PlayerProgression Progression;

    /// <summary>创建一次遗物效果应用上下文。</summary>
    public RelicEffectContext(PlayerRelicEffects relics, PlayerRuntimeData runtimeData, PlayerProgression progression)
    {
        Relics = relics;
        RuntimeData = runtimeData;
        Progression = progression;
    }
}

/// <summary>
/// 应用一种配置好的遗物效果类型。
/// </summary>
public interface IRelicEffectHandler
{
    /// <summary>与 RelicEffectConfig.type 匹配的效果类型字符串。</summary>
    string Type { get; }
    /// <summary>将配置效果应用到当前玩家上下文。</summary>
    void Apply(RelicEffectConfig effect, RelicEffectContext context);
}

/// <summary>
/// 将遗物效果类型字符串映射到小型处理器类。
/// </summary>
public static class RelicEffectHandlerRegistry
{
    /// <summary>按效果类型索引的已注册处理器。</summary>
    private static readonly Dictionary<string, IRelicEffectHandler> handlers = new Dictionary<string, IRelicEffectHandler>();

    /// <summary>一次性注册所有内置效果处理器。</summary>
    static RelicEffectHandlerRegistry()
    {
        Register(new StatRelicEffectHandler("attack"));
        Register(new StatRelicEffectHandler("defense"));
        Register(new StatRelicEffectHandler("maxHp"));
        Register(new StatRelicEffectHandler("moveSpeed"));
        Register(new StatRelicEffectHandler("critRate"));
        Register(new RegenRelicEffectHandler());
        Register(new GoldRelicEffectHandler());
        Register(new CritDamageRelicEffectHandler());
        Register(new ConvertMoveSpeedRelicEffectHandler());
        Register(new TripleShotRelicEffectHandler());
        Register(new CritLifeStealRelicEffectHandler());
        Register(new ReorderStatsRelicEffectHandler());
        Register(new ReviveOnceRelicEffectHandler());
        Register(new LifeStealRelicEffectHandler());
        Register(new HpDrainRelicEffectHandler());
        Register(new BossDamageRelicEffectHandler());
    }

    /// <summary>如果效果类型已注册处理器，则应用该效果。</summary>
    public static void Apply(RelicEffectConfig effect, RelicEffectContext context)
    {
        if (effect == null || string.IsNullOrEmpty(effect.type))
            return;

        if (handlers.TryGetValue(effect.type, out IRelicEffectHandler handler))
            handler.Apply(effect, context);
    }

    /// <summary>按效果类型添加或替换处理器。</summary>
    private static void Register(IRelicEffectHandler handler)
    {
        handlers[handler.Type] = handler;
    }
}

/// <summary>
/// 处理攻击、防御、生命、移速、暴击等简单属性遗物。
/// </summary>
public class StatRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前实例处理的属性类型。</summary>
    public string Type { get; }

    /// <summary>为一种属性类型创建属性处理器。</summary>
    public StatRelicEffectHandler(string type)
    {
        Type = type;
    }

    /// <summary>将数值属性加成应用到玩家运行时数据。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context)
    {
        context.RuntimeData?.ApplyBuff(Type, Mathf.RoundToInt(effect.value));
    }
}

/// <summary>处理周期回血遗物。</summary>
public class RegenRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "regen";
    /// <summary>为玩家添加一个回血来源。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Relics.AddRegen(Mathf.RoundToInt(effect.value), effect.interval);
}

/// <summary>处理立即获得金币的遗物。</summary>
public class GoldRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "gold";
    /// <summary>给玩家成长数据增加金币。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Progression?.AddGold(Mathf.RoundToInt(effect.value));
}

/// <summary>处理暴击伤害倍率遗物。</summary>
public class CritDamageRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "critDamageMultiplier";
    /// <summary>提高玩家暴击伤害倍率。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Relics.AddCritDamageMultiplier(effect.value);
}

/// <summary>处理将移速转换为防御的遗物。</summary>
public class ConvertMoveSpeedRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "convertMoveSpeedToDefense";
    /// <summary>将当前移速转换为防御，并把移速设为 0。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Relics.ConvertMoveSpeedToDefense();
}

/// <summary>处理弓手三箭分裂遗物。</summary>
public class TripleShotRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "tripleShot";
    /// <summary>开启三箭分裂，并设置每支箭的伤害倍率。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Relics.EnableTripleShot(effect.damageMultiplier);
}

/// <summary>处理暴击吸血遗物。</summary>
public class CritLifeStealRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "critLifeSteal";
    /// <summary>根据暴击造成的伤害增加治疗。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Relics.AddCritLifeSteal(effect.value);
}

/// <summary>处理重排玩家属性数值的遗物。</summary>
public class ReorderStatsRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "reorderStats";
    /// <summary>按遗物规则重排玩家当前属性。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Relics.ReorderStats();
}

/// <summary>处理一次性复活遗物。</summary>
public class ReviveOnceRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "reviveOnce";
    /// <summary>按配置生命比例开启一次复活机会。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Relics.EnableReviveOnce(effect.hpPercent);
}

/// <summary>处理普通伤害吸血遗物。</summary>
public class LifeStealRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "lifeSteal";
    /// <summary>根据所有造成的伤害增加治疗。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Relics.AddLifeSteal(effect.value);
}

/// <summary>处理每秒扣血遗物。</summary>
public class HpDrainRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "hpDrainPerSecond";
    /// <summary>给玩家添加周期性自伤。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Relics.AddHpDrainPerSecond(Mathf.RoundToInt(effect.value));
}

/// <summary>处理提高对 Boss 伤害的遗物。</summary>
public class BossDamageRelicEffectHandler : IRelicEffectHandler
{
    /// <summary>当前处理器处理的效果类型。</summary>
    public string Type => "bossDamageMultiplier";
    /// <summary>提高玩家对 Boss 的伤害倍率。</summary>
    public void Apply(RelicEffectConfig effect, RelicEffectContext context) => context.Relics.AddBossDamageMultiplier(effect.value);
}
