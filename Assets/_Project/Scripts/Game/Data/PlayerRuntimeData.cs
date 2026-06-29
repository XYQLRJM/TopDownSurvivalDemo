using UnityEngine;

/// <summary>
/// 从角色配置复制出的玩家运行时数据，后续升级和道具会修改这一份实例。
/// </summary>
public class PlayerRuntimeData : MonoBehaviour
{
    /// <summary>角色唯一 id。</summary>
    public string Id { get; private set; }
    /// <summary>角色显示名称。</summary>
    public string DisplayName { get; private set; }
    /// <summary>角色攻击类型。</summary>
    public string AttackType { get; private set; }
    /// <summary>玩家当前等级。</summary>
    public int Level { get; private set; }
    /// <summary>玩家最大生命。</summary>
    public int MaxHp { get; private set; }
    /// <summary>玩家当前生命。</summary>
    public int CurrentHp { get; private set; }
    /// <summary>玩家攻击力。</summary>
    public int Attack { get; private set; }
    /// <summary>玩家防御力。</summary>
    public int Defense { get; private set; }
    /// <summary>玩家移动速度。</summary>
    public float MoveSpeed { get; private set; }
    /// <summary>玩家暴击率。</summary>
    public int CritRate { get; private set; }

    /// <summary>用角色初始配置初始化玩家运行时属性。</summary>
    public void Init(CharacterConfig config)
    {
        Id = config.id;
        DisplayName = config.name;
        AttackType = config.attackType;
        Level = config.level;
        MaxHp = config.maxHp;
        CurrentHp = MaxHp;
        Attack = config.attack;
        Defense = config.defense;
        MoveSpeed = config.moveSpeed;
        CritRate = config.critRate;
    }

    /// <summary>捕获当前玩家战斗属性用于存档。</summary>
    public PlayerRuntimeSaveData CaptureSaveData()
    {
        return new PlayerRuntimeSaveData
        {
            id = Id,
            displayName = DisplayName,
            attackType = AttackType,
            level = Level,
            maxHp = MaxHp,
            currentHp = CurrentHp,
            attack = Attack,
            defense = Defense,
            moveSpeed = MoveSpeed,
            critRate = CritRate
        };
    }

    /// <summary>从存档数据恢复玩家战斗属性。</summary>
    public void Restore(PlayerRuntimeSaveData data)
    {
        if (data == null)
            return;

        Id = data.id;
        DisplayName = data.displayName;
        AttackType = data.attackType;
        Level = data.level;
        MaxHp = Mathf.Max(1, data.maxHp);
        CurrentHp = Mathf.Clamp(data.currentHp, 0, MaxHp);
        Attack = Mathf.Max(0, data.attack);
        Defense = Mathf.Max(0, data.defense);
        MoveSpeed = Mathf.Max(0f, data.moveSpeed);
        CritRate = Mathf.Max(0, data.critRate);
    }

    /// <summary>扣除玩家生命，最低降到 0。</summary>
    public void TakeDamage(int damage)
    {
        CurrentHp = Mathf.Max(0, CurrentHp - damage);
    }

    /// <summary>将玩家生命恢复到最大值。</summary>
    public void HealFull()
    {
        CurrentHp = MaxHp;
    }

    /// <summary>治疗玩家，但不会超过最大生命。</summary>
    public void Heal(int amount)
    {
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + Mathf.Max(0, amount));
    }

    /// <summary>直接设置当前生命，并限制在合法范围。</summary>
    public void SetCurrentHp(int value)
    {
        CurrentHp = Mathf.Clamp(value, 0, MaxHp);
    }

    /// <summary>按照强化类型修改玩家属性。</summary>
    public void ApplyBuff(string buffType, int value)
    {
        switch (buffType)
        {
            case "maxHp":
                MaxHp += value;
                CurrentHp += value;
                break;
            case "attack":
                Attack += value;
                break;
            case "defense":
                Defense += value;
                break;
            case "moveSpeed":
                MoveSpeed += value;
                break;
            case "critRate":
                CritRate += value;
                break;
        }
    }

    /// <summary>直接设置一组属性，常用于特殊遗物重排属性。</summary>
    public void SetStats(int maxHp, int attack, int defense, float moveSpeed, int critRate)
    {
        MaxHp = Mathf.Max(1, maxHp);
        Attack = Mathf.Max(0, attack);
        Defense = Mathf.Max(0, defense);
        MoveSpeed = Mathf.Max(0f, moveSpeed);
        CritRate = Mathf.Max(0, critRate);
        CurrentHp = Mathf.Clamp(CurrentHp, 0, MaxHp);
    }
}
