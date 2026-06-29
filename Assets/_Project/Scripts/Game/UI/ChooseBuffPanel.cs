using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 显示三个随机强化选项，并回传玩家选择。
/// </summary>
public class ChooseBuffPanel : BasePanel
{
    /// <summary>三个强化按钮上的文本。</summary>
    private readonly Text[] buffTexts = new Text[3];
    /// <summary>当前展示的三个强化选项。</summary>
    private BuffOption[] options;
    /// <summary>玩家选择或跳过后的回调。</summary>
    private Action<BuffOption> onSelected;
    /// <summary>是否已经做出选择，防止重复点击。</summary>
    private bool hasSelected;

    /// <summary>缓存强化文本控件。</summary>
    protected override void Awake()
    {
        base.Awake();
        buffTexts[0] = FindText("buff1txt");
        buffTexts[1] = FindText("buff2txt");
        buffTexts[2] = FindText("buff3txt");
    }

    /// <summary>绑定玩家数据并随机生成三个强化选项。</summary>
    public void Bind(PlayerRuntimeData data, Action<BuffOption> selectedCallback)
    {
        hasSelected = false;
        onSelected = selectedCallback;
        options = BuffOptionPool.GetRandomOptions(data != null ? data.Id : "warrior", 3);

        for (int i = 0; i < buffTexts.Length; ++i)
        {
            if (buffTexts[i] != null && options != null && i < options.Length)
                buffTexts[i].text = options[i].description;
        }
    }

    /// <summary>面板显示时重置选择状态。</summary>
    public override void ShowMe()
    {
        hasSelected = false;
    }

    /// <summary>面板隐藏时暂无额外逻辑。</summary>
    public override void HideMe()
    {
    }

    /// <summary>处理强化按钮和跳过按钮点击。</summary>
    protected override void ClickBtn(string btnName)
    {
        if (hasSelected)
            return;

        if (btnName == "PassBtn")
        {
            hasSelected = true;
            onSelected?.Invoke(null);
            return;
        }

        int index = GetButtonIndex(btnName);
        if (index < 0 || options == null || index >= options.Length)
            return;

        hasSelected = true;
        onSelected?.Invoke(options[index]);
    }

    /// <summary>把按钮名转换为强化选项索引。</summary>
    private int GetButtonIndex(string btnName)
    {
        switch (btnName)
        {
            case "buff1btn":
                return 0;
            case "buff2btn":
                return 1;
            case "buff3btn":
                return 2;
            default:
                return -1;
        }
    }

    /// <summary>在当前面板内按名字查找 Text 控件。</summary>
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
