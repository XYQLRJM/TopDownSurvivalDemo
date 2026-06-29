using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 显示关卡结算结果，并执行传入的确认回调。
/// </summary>
public class ResultPanel : BasePanel
{
    /// <summary>结算结果文本。</summary>
    private Text resultText;
    /// <summary>点击确定后执行的回调。</summary>
    private Action confirmCallback;

    /// <summary>缓存结算文本控件。</summary>
    protected override void Awake()
    {
        base.Awake();
        resultText = FindText("Restxt");
    }

    /// <summary>按胜负设置默认结算文本。</summary>
    public void SetResult(bool isWin)
    {
        SetResultText(isWin ? "游戏胜利" : "游戏失败", null);
    }

    /// <summary>设置结算文本和确定按钮回调。</summary>
    public void SetResultText(string text, Action onConfirm)
    {
        confirmCallback = onConfirm;
        if (resultText != null)
            resultText.text = text;
    }

    /// <summary>结算面板显示时暂无额外逻辑。</summary>
    public override void ShowMe()
    {
    }

    /// <summary>结算面板隐藏时暂无额外逻辑。</summary>
    public override void HideMe()
    {
    }

    /// <summary>响应确定按钮，优先执行绑定回调。</summary>
    protected override void ClickBtn(string btnName)
    {
        if (btnName != "Confimbtn" && btnName != "Confirmbtn")
            return;

        if (confirmCallback != null)
        {
            Action callback = confirmCallback;
            confirmCallback = null;
            UIMgr.Instance.HidePanel<ResultPanel>(true);
            callback.Invoke();
            return;
        }

        ReturnToMenu();
    }

    /// <summary>默认返回开始界面。</summary>
    private void ReturnToMenu()
    {
        Time.timeScale = 1f;
        MusicMgr.Instance.ClearSound();
        PoolMgr.Instance.ClearGameObjectPools();
        UIMgr.Instance.HidePanel<ResultPanel>(true);
        UIMgr.Instance.HidePanel<GamePanel>(true);
        SceneMgr.Instance.LoadSceneAsyn(SceneNameConfig.BeginScene);
    }

    /// <summary>按名字在当前面板内查找 Text 控件。</summary>
    private Text FindText(string textName)
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; ++i)
        {
            if (texts[i].name == textName)
                return texts[i];
        }

        return null;
    }
}
