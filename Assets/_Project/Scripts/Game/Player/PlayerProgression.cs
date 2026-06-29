using UnityEngine;

/// <summary>
/// 跟踪玩家等级、经验和金币。
/// </summary>
public class PlayerProgression : MonoBehaviour
{
    /// <summary>玩家当前等级。</summary>
    public int Level { get; private set; }
    /// <summary>当前等级已获得经验。</summary>
    public int CurrentExp { get; private set; }
    /// <summary>升到下一级需要的经验。</summary>
    public int NeedExp { get; private set; } = 30;
    /// <summary>玩家当前金币。</summary>
    public int Gold { get; private set; }

    /// <summary>增加经验，经验溢出时自动升级。</summary>
    public void AddExperience(int amount)
    {
        CurrentExp += amount;
        while (CurrentExp >= NeedExp)
        {
            CurrentExp -= NeedExp;
            AddLevel();
        }
    }

    /// <summary>直接提升指定等级数。</summary>
    public void AddLevels(int amount)
    {
        for (int i = 0; i < amount; ++i)
            AddLevel();
    }

    /// <summary>增加金币。</summary>
    public void AddGold(int amount)
    {
        Gold += Mathf.Max(0, amount);
    }

    /// <summary>尝试花费金币，金币不足时返回 false。</summary>
    public bool SpendGold(int amount)
    {
        if (Gold < amount)
            return false;

        Gold -= amount;
        return true;
    }

    /// <summary>捕获当前等级、经验、金币等成长数据用于存档。</summary>
    public PlayerProgressionSaveData CaptureSaveData()
    {
        return new PlayerProgressionSaveData
        {
            level = Level,
            currentExp = CurrentExp,
            needExp = NeedExp,
            gold = Gold
        };
    }

    /// <summary>从存档数据恢复玩家成长状态。</summary>
    public void Restore(PlayerProgressionSaveData data)
    {
        if (data == null)
            return;

        Level = Mathf.Max(0, data.level);
        CurrentExp = Mathf.Max(0, data.currentExp);
        NeedExp = Mathf.Max(1, data.needExp);
        Gold = Mathf.Max(0, data.gold);
    }

    /// <summary>提升一级并增加下一等级所需经验。</summary>
    private void AddLevel()
    {
        Level++;
        NeedExp += 10;
    }
}
