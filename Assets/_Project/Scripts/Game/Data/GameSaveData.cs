using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 每关开始时记录的完整存档快照。
/// </summary>
[Serializable]
public class GameSaveData
{
    /// <summary>已选择的角色 id。</summary>
    public string characterId;
    /// <summary>继续游戏时重新开始的关卡数。</summary>
    public int stage;
    /// <summary>玩家战斗属性快照。</summary>
    public PlayerRuntimeSaveData runtimeData;
    /// <summary>玩家等级、经验和金币快照。</summary>
    public PlayerProgressionSaveData progressionData;
    /// <summary>已拥有遗物快照。</summary>
    public PlayerRelicSaveData relicData;
}

/// <summary>
/// 可序列化的玩家战斗属性。
/// </summary>
[Serializable]
public class PlayerRuntimeSaveData
{
    /// <summary>角色 id。</summary>
    public string id;
    /// <summary>角色显示名称。</summary>
    public string displayName;
    /// <summary>攻击策略 id。</summary>
    public string attackType;
    /// <summary>配置或运行时等级值。</summary>
    public int level;
    /// <summary>最大生命值。</summary>
    public int maxHp;
    /// <summary>当前生命值。</summary>
    public int currentHp;
    /// <summary>攻击属性。</summary>
    public int attack;
    /// <summary>防御属性。</summary>
    public int defense;
    /// <summary>移动速度属性。</summary>
    public float moveSpeed;
    /// <summary>暴击率。</summary>
    public int critRate;
}

/// <summary>
/// 可序列化的玩家成长数据。
/// </summary>
[Serializable]
public class PlayerProgressionSaveData
{
    /// <summary>玩家当前等级。</summary>
    public int level;
    /// <summary>当前等级内已有经验。</summary>
    public int currentExp;
    /// <summary>升到下一级需要的经验。</summary>
    public int needExp;
    /// <summary>当前金币数量。</summary>
    public int gold;
}

/// <summary>
/// 可序列化的遗物拥有数据。
/// </summary>
[Serializable]
public class PlayerRelicSaveData
{
    /// <summary>读档时重新应用的已拥有遗物 id。</summary>
    public string[] relicIds;
}

/// <summary>
/// 使用 json 文件持久化的存档槽。
/// </summary>
public static class GameSaveStore
{
    /// <summary>Application.persistentDataPath 下的存档文件名。</summary>
    private const string SaveFileName = "save.json";
    /// <summary>从 json 文件读取后缓存的存档数据。</summary>
    private static GameSaveData savedData;
    /// <summary>下一次加载 GameScene 时是否使用存档。</summary>
    private static bool loadSavedGame;

    /// <summary>当前是否存在存档。</summary>
    public static bool HasSave => GetSavedData() != null;
    /// <summary>当前存档数据，按需延迟读取。</summary>
    public static GameSaveData SavedData => GetSavedData();

    /// <summary>将数据保存到内存和 json 文件。</summary>
    public static void Save(GameSaveData data)
    {
        savedData = data;
        if (data == null)
        {
            Clear();
            return;
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data));
    }

    /// <summary>清除内存和磁盘中的存档。</summary>
    public static void Clear()
    {
        savedData = null;
        loadSavedGame = false;
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    /// <summary>请求下一次 GameScene 加载时使用存档。</summary>
    public static void RequestLoadSavedGame()
    {
        loadSavedGame = true;
    }

    /// <summary>消费并清除待处理的继续游戏请求。</summary>
    public static bool ConsumeLoadSavedGameRequest()
    {
        bool value = loadSavedGame;
        loadSavedGame = false;
        return value && GetSavedData() != null;
    }

    /// <summary>返回缓存存档，若无缓存则从磁盘读取。</summary>
    private static GameSaveData GetSavedData()
    {
        if (savedData != null)
            return savedData;

        if (!File.Exists(SavePath))
            return null;

        string json = File.ReadAllText(SavePath);
        if (string.IsNullOrEmpty(json))
            return null;

        savedData = JsonUtility.FromJson<GameSaveData>(json);
        return savedData;
    }

    /// <summary>json 存档文件的绝对路径。</summary>
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
}
