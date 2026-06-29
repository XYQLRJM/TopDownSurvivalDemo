using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 保存已拥有遗物和仅运行时存在的遗物状态。
/// </summary>
public class PlayerRelicEffects : MonoBehaviour
{
    /// <summary>按购买顺序保存的已拥有遗物 id。</summary>
    private readonly List<string> ownedRelicIds = new List<string>();
    /// <summary>拥有这些遗物效果的玩家控制器。</summary>
    private PlayerController2D player;
    /// <summary>遗物效果会读取或修改的玩家属性。</summary>
    private PlayerRuntimeData runtimeData;
    /// <summary>遗物效果会修改的玩家等级、经验和金币数据。</summary>
    private PlayerProgression progression;
    /// <summary>周期回血使用的计时器。</summary>
    private float regenTimer;
    /// <summary>周期扣血使用的计时器。</summary>
    private float drainTimer;
    /// <summary>每次回血恢复的生命值。</summary>
    private int regenAmount;
    /// <summary>两次回血之间的秒数间隔。</summary>
    private float regenInterval;
    /// <summary>扣血遗物每秒损失的生命值。</summary>
    private int hpDrainPerSecond;
    /// <summary>一次性复活遗物是否仍可触发。</summary>
    private bool reviveAvailable;
    /// <summary>复活遗物恢复的最大生命比例。</summary>
    private float reviveHpPercent;

    /// <summary>当前玩家运行时数据。</summary>
    public PlayerRuntimeData RuntimeData => runtimeData;
    /// <summary>当前玩家成长数据。</summary>
    public PlayerProgression Progression => progression;
    /// <summary>暴击伤害倍率。</summary>
    public float CritDamageMultiplier { get; private set; } = 2f;
    /// <summary>弓手箭矢是否分裂为三支。</summary>
    public bool TripleShot { get; private set; }
    /// <summary>三箭分裂时每支箭的伤害倍率。</summary>
    public float TripleShotDamageMultiplier { get; private set; } = 1f;
    /// <summary>暴击伤害转换为治疗的比例。</summary>
    public float CritLifeStealRate { get; private set; }
    /// <summary>所有造成伤害转换为治疗的比例。</summary>
    public float LifeStealRate { get; private set; }
    /// <summary>攻击 Boss 时使用的伤害倍率。</summary>
    public float BossDamageMultiplier { get; private set; } = 1f;

    /// <summary>用拥有者玩家数据初始化遗物效果。</summary>
    public void Init(PlayerController2D owner, PlayerRuntimeData data, PlayerProgression playerProgression)
    {
        player = owner;
        runtimeData = data;
        progression = playerProgression;
    }

    /// <summary>更新周期性遗物效果。</summary>
    private void Update()
    {
        if (runtimeData == null || player == null)
            return;

        TickRegen();
        TickHpDrain();
    }

    /// <summary>应用一个遗物配置，并可选择是否记录为已拥有。</summary>
    public void ApplyRelic(RelicConfig relic, bool recordOwned = true)
    {
        if (relic == null || relic.effects == null)
            return;

        if (recordOwned)
            ownedRelicIds.Add(relic.id);

        RelicEffectContext context = new RelicEffectContext(this, runtimeData, progression);
        for (int i = 0; i < relic.effects.Length; ++i)
            RelicEffectHandlerRegistry.Apply(relic.effects[i], context);
    }

    /// <summary>玩家造成伤害后应用吸血治疗效果。</summary>
    public void OnDealDamage(int damage, bool isCrit)
    {
        if (runtimeData == null || damage <= 0)
            return;

        if (LifeStealRate > 0f)
            runtimeData.Heal(Mathf.RoundToInt(damage * LifeStealRate));

        if (isCrit && CritLifeStealRate > 0f)
            runtimeData.Heal(Mathf.RoundToInt(damage * CritLifeStealRate));
    }

    /// <summary>如果可用则消耗一次复活机会。</summary>
    public bool TryRevive()
    {
        if (!reviveAvailable || runtimeData == null)
            return false;

        reviveAvailable = false;
        runtimeData.SetCurrentHp(Mathf.Max(1, Mathf.RoundToInt(runtimeData.MaxHp * reviveHpPercent)));
        return true;
    }

    /// <summary>捕获已拥有遗物 id 作为存档数据。</summary>
    public PlayerRelicSaveData CaptureSaveData()
    {
        return new PlayerRelicSaveData
        {
            relicIds = ownedRelicIds.ToArray()
        };
    }

    /// <summary>从存档恢复遗物，能找到配置时按 id 重新应用。</summary>
    public void Restore(PlayerRelicSaveData data)
    {
        ResetRelicStates();
        if (data == null)
            return;

        if (data.relicIds == null || data.relicIds.Length == 0)
            return;

        for (int i = 0; i < data.relicIds.Length; ++i)
        {
            RelicConfig relic = RelicConfigLoader.GetRelic(data.relicIds[i]);
            ApplyRelic(relic);
        }
    }

    /// <summary>添加周期回血效果。</summary>
    public void AddRegen(int amount, float interval)
    {
        regenAmount += Mathf.Max(1, amount);
        regenInterval = interval > 0f ? interval : 3f;
    }

    /// <summary>提高暴击伤害倍率。</summary>
    public void AddCritDamageMultiplier(float value)
    {
        CritDamageMultiplier = Mathf.Max(CritDamageMultiplier, value);
    }

    /// <summary>为远程攻击开启三箭分裂。</summary>
    public void EnableTripleShot(float damageMultiplier)
    {
        TripleShot = true;
        TripleShotDamageMultiplier = damageMultiplier > 0f ? damageMultiplier : 0.6f;
    }

    /// <summary>根据暴击伤害添加治疗效果。</summary>
    public void AddCritLifeSteal(float value)
    {
        CritLifeStealRate += Mathf.Max(0f, value);
    }

    /// <summary>开启一次性复活效果。</summary>
    public void EnableReviveOnce(float hpPercent)
    {
        reviveAvailable = true;
        reviveHpPercent = hpPercent > 0f ? hpPercent : 0.5f;
    }

    /// <summary>根据所有造成的伤害添加治疗效果。</summary>
    public void AddLifeSteal(float value)
    {
        LifeStealRate += Mathf.Max(0f, value);
    }

    /// <summary>添加周期性自伤效果。</summary>
    public void AddHpDrainPerSecond(int value)
    {
        hpDrainPerSecond += Mathf.Max(0, value);
    }

    /// <summary>提高对 Boss 造成的伤害。</summary>
    public void AddBossDamageMultiplier(float value)
    {
        BossDamageMultiplier = Mathf.Max(BossDamageMultiplier, value);
    }

    /// <summary>将移速转换为防御，并把移速设为 0。</summary>
    public void ConvertMoveSpeedToDefense()
    {
        if (runtimeData == null)
            return;

        runtimeData.SetStats(
            runtimeData.MaxHp,
            runtimeData.Attack,
            runtimeData.Defense + Mathf.RoundToInt(runtimeData.MoveSpeed),
            0f,
            runtimeData.CritRate);
    }

    /// <summary>按特殊遗物规则重排当前属性数值。</summary>
    public void ReorderStats()
    {
        if (runtimeData == null)
            return;

        List<int> values = new List<int>
        {
            runtimeData.MaxHp,
            runtimeData.Attack,
            runtimeData.Defense,
            Mathf.RoundToInt(runtimeData.MoveSpeed),
            runtimeData.CritRate
        };
        values.Sort((a, b) => b.CompareTo(a));
        runtimeData.SetStats(values[4], values[0], values[3], values[2], values[1]);
    }

    /// <summary>执行周期回血。</summary>
    private void TickRegen()
    {
        if (regenAmount <= 0 || regenInterval <= 0f || runtimeData.CurrentHp <= 0)
            return;

        regenTimer += Time.deltaTime;
        if (regenTimer < regenInterval)
            return;

        regenTimer = 0f;
        runtimeData.Heal(regenAmount);
    }

    /// <summary>执行周期扣血。</summary>
    private void TickHpDrain()
    {
        if (hpDrainPerSecond <= 0 || runtimeData.CurrentHp <= 0)
            return;

        drainTimer += Time.deltaTime;
        if (drainTimer < 1f)
            return;

        drainTimer = 0f;
        player.TakeDamage(hpDrainPerSecond);
    }

    /// <summary>重新应用存档遗物前清空所有特殊遗物状态。</summary>
    private void ResetRelicStates()
    {
        ownedRelicIds.Clear();
        regenTimer = 0f;
        drainTimer = 0f;
        regenAmount = 0;
        regenInterval = 0f;
        hpDrainPerSecond = 0;
        reviveAvailable = false;
        reviveHpPercent = 0f;
        CritDamageMultiplier = 2f;
        TripleShot = false;
        TripleShotDamageMultiplier = 1f;
        CritLifeStealRate = 0f;
        LifeStealRate = 0f;
        BossDamageMultiplier = 1f;
    }

}
