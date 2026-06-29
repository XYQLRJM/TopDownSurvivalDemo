using UnityEngine;

/// <summary>
/// 游戏内点击 X 按钮打开的暂停菜单。
/// </summary>
public class ExitPanel : BasePanel
{
    /// <summary>打开退出面板时暂停游戏。</summary>
    public override void ShowMe()
    {
        Time.timeScale = 0f;
    }

    /// <summary>隐藏退出面板时暂无额外逻辑。</summary>
    public override void HideMe()
    {
    }

    /// <summary>响应确认返回主菜单和取消按钮。</summary>
    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "confirmbtn":
                ReturnToMenu();
                break;
            case "exitbtn":
                ResumeGame();
                break;
        }
    }

    /// <summary>清理运行对象并返回开始界面。</summary>
    private void ReturnToMenu()
    {
        Time.timeScale = 1f;
        MusicMgr.Instance.ClearSound();
        PoolMgr.Instance.ClearGameObjectPools();
        UIMgr.Instance.HidePanel<ExitPanel>(true);
        UIMgr.Instance.HidePanel<PlayerInfoPanel>(true);
        UIMgr.Instance.HidePanel<GamePanel>(true);
        SceneMgr.Instance.LoadSceneAsyn(SceneNameConfig.BeginScene);
    }

    /// <summary>关闭退出面板并恢复游戏。</summary>
    private void ResumeGame()
    {
        Time.timeScale = 1f;
        UIMgr.Instance.HidePanel<ExitPanel>(true);
    }
}
