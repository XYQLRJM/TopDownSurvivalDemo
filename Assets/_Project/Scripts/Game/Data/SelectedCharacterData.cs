/// <summary>
/// 从选角场景传递到游戏场景的临时选角数据。
/// </summary>
public static class SelectedCharacterData
{
    /// <summary>从选角界面传递到游戏场景的角色 id。</summary>
    public static string SelectedCharacterId { get; private set; }

    /// <summary>保存玩家选择的角色 id。</summary>
    public static void SetSelectedCharacter(string characterId)
    {
        SelectedCharacterId = characterId;
    }
}
