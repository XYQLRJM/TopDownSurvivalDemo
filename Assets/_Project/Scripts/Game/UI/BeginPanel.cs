using UnityEngine;

/// <summary>
/// 处理开始菜单上的按钮。
/// </summary>
public class BeginPanel : BasePanel
{
    /// <summary>开始面板显示时暂无额外逻辑。</summary>
    public override void ShowMe()
    {
    }

    /// <summary>开始面板隐藏时暂无额外逻辑。</summary>
    public override void HideMe()
    {
    }

    /// <summary>响应开始、继续和退出按钮。</summary>
    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "StartGameBtn":
                GameSaveStore.Clear();
                SceneMgr.Instance.LoadScene(SceneNameConfig.ChooseScene);
                break;
            case "ContinueGameBtn":
                ContinueGame();
                break;
            case "ExitBtn":
                QuitGame();
                break;
        }
    }

    /// <summary>退出游戏，编辑器中停止播放模式。</summary>
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>继续游戏；没有存档时显示自动关闭提示。</summary>
    private void ContinueGame()
    {
        if (!GameSaveStore.HasSave)
        {
            UIMgr.Instance.ShowPanel<TipsPanel>(E_UILayer.Top, panel =>
            {
                panel.ShowAutoClose("没有正在进行的游戏", 1f);
            }, true);
            return;
        }

        GameSaveStore.RequestLoadSavedGame();
        SceneMgr.Instance.LoadScene(SceneNameConfig.GameScene);
    }
}
