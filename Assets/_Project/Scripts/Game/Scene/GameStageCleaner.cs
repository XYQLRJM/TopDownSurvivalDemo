using UnityEngine;

/// <summary>
/// 清理关卡中的对象池物体、音效、Boss 引用和界面。
/// </summary>
public static class GameStageCleaner
{
    /// <summary>清理当前关卡对象并解除 Boss 血条绑定。</summary>
    public static void CleanupStage(ref BossController boss, GamePanel panel)
    {
        MusicMgr.Instance.ClearSound();
        PoolMgr.Instance.ClearGameObjectPools();
        DestroyBoss(ref boss);
        panel?.BindBoss(null);
    }

    /// <summary>清理游戏场景运行时对象并返回开始界面。</summary>
    public static void ReturnToMenu(ref BossController boss)
    {
        Time.timeScale = 1f;
        MusicMgr.Instance.ClearSound();
        PoolMgr.Instance.ClearGameObjectPools();
        DestroyBoss(ref boss);
        UIMgr.Instance.HidePanel<ResultPanel>(true);
        UIMgr.Instance.HidePanel<ChooseBuffPanel>(true);
        UIMgr.Instance.HidePanel<ShopPanel>(true);
        UIMgr.Instance.HidePanel<GamePanel>(true);
        SceneMgr.Instance.LoadSceneAsyn(SceneNameConfig.BeginScene);
    }

    /// <summary>销毁当前 Boss 对象并清空引用。</summary>
    private static void DestroyBoss(ref BossController boss)
    {
        if (boss != null)
            Object.Destroy(boss.gameObject);
        boss = null;
    }
}
