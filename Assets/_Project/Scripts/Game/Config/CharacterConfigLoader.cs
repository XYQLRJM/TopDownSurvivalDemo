using UnityEngine;

/// <summary>
/// 从 Resources/Configs/characters.json 加载角色初始数据。
/// </summary>
public static class CharacterConfigLoader
{
    /// <summary>角色配置文件在 Resources 中的路径。</summary>
    private const string ConfigPath = "Configs/characters";

    /// <summary>读取全部角色初始配置。</summary>
    public static CharacterConfig[] LoadCharacters()
    {
        TextAsset json = Resources.Load<TextAsset>(ConfigPath);
        if (json == null)
        {
            Debug.LogError($"Character config not found: Resources/{ConfigPath}.json");
            return new CharacterConfig[0];
        }

        CharacterConfigList configList = JsonUtility.FromJson<CharacterConfigList>(json.text);
        return configList != null && configList.characters != null
            ? configList.characters
            : new CharacterConfig[0];
    }
}
