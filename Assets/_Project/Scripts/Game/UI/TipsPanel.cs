using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 只有确认按钮的简单提示面板。
/// </summary>
public class TipsPanel : BasePanel
{
    /// <summary>提示内容文本。</summary>
    private Text tipText;

    /// <summary>缓存提示文本控件。</summary>
    protected override void Awake()
    {
        base.Awake();
        tipText = GetControl<Text>("tiptxt");
    }

    /// <summary>设置提示面板显示的文字。</summary>
    public void SetText(string text)
    {
        if (tipText != null)
            tipText.text = text;
    }

    /// <summary>显示提示文本，并在指定时间后自动关闭。</summary>
    public void ShowAutoClose(string text, float duration)
    {
        SetText(text);
        StopAllCoroutines();
        StartCoroutine(AutoClose(duration));
    }

    /// <summary>面板显示时暂无额外逻辑。</summary>
    public override void ShowMe()
    {
    }

    /// <summary>面板隐藏时暂无额外逻辑。</summary>
    public override void HideMe()
    {
    }

    /// <summary>点击确认按钮时关闭提示面板。</summary>
    protected override void ClickBtn(string btnName)
    {
        if (btnName == "Confirmbtn")
            UIMgr.Instance.HidePanel<TipsPanel>(true);
    }

    /// <summary>等待指定时间后关闭提示面板。</summary>
    private IEnumerator AutoClose(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);
        UIMgr.Instance.HidePanel<TipsPanel>(true);
    }
}
