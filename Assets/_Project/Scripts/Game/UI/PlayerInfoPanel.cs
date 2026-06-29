using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 显示当前玩家属性，并在下一次点击时关闭。
/// </summary>
public class PlayerInfoPanel : BasePanel
{
    /// <summary>玩家信息展示文本。</summary>
    private Text infoText;
    /// <summary>绑定的玩家运行时属性。</summary>
    private PlayerRuntimeData runtimeData;
    /// <summary>绑定的玩家成长数据。</summary>
    private PlayerProgression progression;
    /// <summary>是否允许点击关闭，避免打开面板的同一次点击立刻关闭。</summary>
    private bool canClose;

    /// <summary>缓存玩家信息文本控件。</summary>
    protected override void Awake()
    {
        base.Awake();
        infoText = FindText("infotxt");
    }

    /// <summary>绑定玩家数据并立即刷新显示。</summary>
    public void Bind(PlayerRuntimeData data, PlayerProgression playerProgression)
    {
        runtimeData = data;
        progression = playerProgression;
        RefreshInfo();
    }

    /// <summary>实时刷新属性，并在下一次点击时关闭面板。</summary>
    private void Update()
    {
        RefreshInfo();

        if (!canClose)
        {
            canClose = true;
            return;
        }

        if (Input.GetMouseButtonDown(0))
            UIMgr.Instance.HidePanel<PlayerInfoPanel>(true);
    }

    /// <summary>面板显示时重置关闭保护。</summary>
    public override void ShowMe()
    {
        canClose = false;
        RefreshInfo();
    }

    /// <summary>面板隐藏时暂无额外逻辑。</summary>
    public override void HideMe()
    {
    }

    /// <summary>刷新玩家属性展示文本。</summary>
    private void RefreshInfo()
    {
        if (runtimeData == null || infoText == null)
            return;

        int level = progression != null ? progression.Level : runtimeData.Level;
        infoText.text =
            $"角色：{runtimeData.DisplayName}\n" +
            $"等级：{level}\n" +
            $"生命：{runtimeData.CurrentHp}/{runtimeData.MaxHp}\n" +
            $"攻击：{runtimeData.Attack}\n" +
            $"防御：{runtimeData.Defense}\n" +
            $"移速：{runtimeData.MoveSpeed}\n" +
            $"暴击率：{runtimeData.CritRate}";
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
