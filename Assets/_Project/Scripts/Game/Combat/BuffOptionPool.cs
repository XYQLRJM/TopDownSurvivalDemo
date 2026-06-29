using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 为不同角色提供随机升级强化选项。
/// </summary>
public static class BuffOptionPool
{
    /// <summary>战士可随机到的强化池。</summary>
    private static readonly BuffOption[] WarriorBuffs =
    {
        new BuffOption("maxHp", 6, "心脏强化\n生命+6"),
        new BuffOption("attack", 3, "手部强化\n攻击+3"),
        new BuffOption("defense", 3, "护甲强化\n防御+3"),
        new BuffOption("moveSpeed", 2, "腿部强化\n移速+2"),
        new BuffOption("critRate", 3, "眼部强化\n暴击+3%")
    };

    /// <summary>弓手可随机到的强化池。</summary>
    private static readonly BuffOption[] ArcherBuffs =
    {
        new BuffOption("maxHp", 4, "心脏强化\n生命+4"),
        new BuffOption("attack", 4, "手部强化\n攻击+4"),
        new BuffOption("defense", 2, "护甲强化\n防御+2"),
        new BuffOption("moveSpeed", 3, "腿部强化\n移速+3"),
        new BuffOption("critRate", 4, "眼部强化\n暴击+4%")
    };

    /// <summary>按照角色 id 随机取出指定数量的强化选项。</summary>
    public static BuffOption[] GetRandomOptions(string characterId, int count)
    {
        BuffOption[] source = characterId == "archer" ? ArcherBuffs : WarriorBuffs;
        List<BuffOption> pool = new List<BuffOption>(source);
        List<BuffOption> result = new List<BuffOption>();

        while (result.Count < count && pool.Count > 0)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result.ToArray();
    }
}
