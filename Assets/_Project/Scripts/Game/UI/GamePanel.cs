using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 绑定游戏内生命、经验、金币、等级、时间、关卡和 Boss 血条 UI。
/// </summary>
public class GamePanel : BasePanel
{
    /// <summary>玩家生命条图片。</summary>
    private Image hpImage;
    /// <summary>玩家经验条图片。</summary>
    private Image expImage;
    /// <summary>Boss 生命条图片。</summary>
    private Image bossHpImage;
    /// <summary>金币数量文本。</summary>
    private Text goldText;
    /// <summary>等级文本。</summary>
    private Text levelText;
    /// <summary>倒计时文本。</summary>
    private Text timeText;
    /// <summary>关卡进度文本。</summary>
    private Text countText;
    /// <summary>玩家生命条 RectTransform，用于兼容缩放式血条。</summary>
    private RectTransform hpBar;
    /// <summary>玩家经验条 RectTransform，用于兼容缩放式经验条。</summary>
    private RectTransform expBar;
    /// <summary>Boss 血条 RectTransform。</summary>
    private RectTransform bossHpBar;
    /// <summary>Boss 血条根节点。</summary>
    private GameObject bossHpRoot;
    /// <summary>绑定的玩家运行时属性。</summary>
    private PlayerRuntimeData playerData;
    /// <summary>绑定的玩家成长数据。</summary>
    private PlayerProgression progression;
    /// <summary>当前绑定的 Boss。</summary>
    private BossController boss;

    /// <summary>缓存游戏主界面中的 UI 控件。</summary>
    protected override void Awake()
    {
        base.Awake();
        hpImage = FindBarImage("Hp", "hp");
        expImage = FindBarImage("Exp", "exp");
        bossHpRoot = FindObject("BossHp");
        bossHpImage = bossHpRoot != null ? FindBarImageIn(bossHpRoot.transform, "hp") : null;
        goldText = FindText("goldtxt");
        levelText = FindText("leveltxt");
        timeText = FindText("timetxt");
        countText = FindText("counttxt");
        hpBar = hpImage != null ? hpImage.rectTransform : null;
        expBar = expImage != null ? expImage.rectTransform : null;
        bossHpBar = bossHpImage != null ? bossHpImage.rectTransform : null;
        SetBossHpVisible(false);
    }

    /// <summary>绑定玩家属性和成长数据。</summary>
    public void Bind(PlayerRuntimeData data, PlayerProgression playerProgression)
    {
        playerData = data;
        progression = playerProgression;
        Refresh();
    }

    /// <summary>绑定或解绑 Boss 血条。</summary>
    public void BindBoss(BossController bossController)
    {
        boss = bossController;
        SetBossHpVisible(boss != null);
        Refresh();
    }

    /// <summary>更新倒计时显示。</summary>
    public void SetRemainingTime(int seconds)
    {
        if (timeText != null)
            timeText.text = $"{Mathf.Max(0, seconds)} s";
    }

    /// <summary>更新当前关卡进度显示。</summary>
    public void SetStageText(int stage, int maxStage)
    {
        if (countText != null)
            countText.text = $"第{stage}/{maxStage}关";
    }

    /// <summary>每帧刷新血条、经验、金币、等级和 Boss 血条。</summary>
    private void Update()
    {
        Refresh();
    }

    /// <summary>游戏主面板显示时暂无额外逻辑。</summary>
    public override void ShowMe()
    {
    }

    /// <summary>游戏主面板隐藏时暂无额外逻辑。</summary>
    public override void HideMe()
    {
    }

    /// <summary>响应退出按钮和玩家信息按钮。</summary>
    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "exitBtn":
                UIMgr.Instance.ShowPanel<ExitPanel>(E_UILayer.Top, null, true);
                break;
            case "playerbtn":
                UIMgr.Instance.ShowPanel<PlayerInfoPanel>(E_UILayer.Top, panel =>
                {
                    panel.Bind(playerData, progression);
                }, true);
                break;
        }
    }

    /// <summary>根据绑定数据刷新全部动态 UI。</summary>
    private void Refresh()
    {
        if (playerData != null && hpImage != null)
            SetBar(hpImage, hpBar, playerData.MaxHp > 0 ? (float)playerData.CurrentHp / playerData.MaxHp : 0f);

        if (progression != null && expImage != null)
            SetBar(expImage, expBar, progression.NeedExp > 0 ? (float)progression.CurrentExp / progression.NeedExp : 0f);

        if (progression != null && goldText != null)
            goldText.text = progression.Gold.ToString();

        if (progression != null && levelText != null)
            levelText.text = $"Level：{progression.Level}";

        if (boss != null && bossHpImage != null)
            SetBar(bossHpImage, bossHpBar, boss.MaxHp > 0 ? (float)boss.CurrentHp / boss.MaxHp : 0f);
    }

    /// <summary>显示或隐藏 Boss 血条。</summary>
    private void SetBossHpVisible(bool visible)
    {
        if (bossHpRoot != null)
            bossHpRoot.SetActive(visible);
    }

    /// <summary>按根节点名和条节点名查找血条图片。</summary>
    private Image FindBarImage(string rootName, string barName)
    {
        GameObject root = FindObject(rootName);
        return root != null ? FindBarImageIn(root.transform, barName) : FindBarImageIn(transform, barName);
    }

    /// <summary>在指定节点下查找血条图片。</summary>
    private Image FindBarImageIn(Transform root, string barName)
    {
        Image[] images = root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; ++i)
        {
            if (images[i].name == barName)
                return images[i];
        }

        return null;
    }

    /// <summary>设置血条填充比例，同时兼容 fillAmount 和横向缩放。</summary>
    private void SetBar(Image image, RectTransform bar, float value)
    {
        float percent = Mathf.Clamp01(value);
        image.fillAmount = percent;

        if (bar != null)
        {
            Vector3 scale = bar.localScale;
            scale.x = percent;
            bar.localScale = scale;
        }
    }

    /// <summary>按名字在当前面板中查找对象。</summary>
    private GameObject FindObject(string objectName)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; ++i)
        {
            if (transforms[i].name == objectName)
                return transforms[i].gameObject;
        }

        return null;
    }

    /// <summary>按名字在当前面板中查找 Text 控件。</summary>
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
