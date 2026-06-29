using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 控制 ChooseScene 中的角色切换、预览生成和属性文本刷新。
/// </summary>
public class ChooseCharacterPanel : BasePanel
{
    /// <summary>可选角色配置列表。</summary>
    private CharacterConfig[] characters;
    /// <summary>当前选择的角色索引。</summary>
    private int currentIndex;
    /// <summary>角色预览生成位置。</summary>
    private Transform characterPos;
    /// <summary>当前角色预览对象。</summary>
    private GameObject currentPreview;
    /// <summary>角色属性展示文本。</summary>
    private Text infoText;

    /// <summary>缓存选角界面所需控件。</summary>
    protected override void Awake()
    {
        base.Awake();
        characterPos = GameObject.Find("characterpos")?.transform;
        infoText = GetControl<Text>("infotxt");
    }

    /// <summary>加载角色配置并显示第一个角色。</summary>
    private void Start()
    {
        characters = CharacterConfigLoader.LoadCharacters();
        if (characters.Length == 0)
        {
            Debug.LogError("No character data loaded.");
            return;
        }

        ShowCharacter(0);
    }

    /// <summary>选角面板显示时暂无额外逻辑。</summary>
    public override void ShowMe()
    {
    }

    /// <summary>选角面板隐藏时暂无额外逻辑。</summary>
    public override void HideMe()
    {
    }

    /// <summary>响应左右切换和开始游戏按钮。</summary>
    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "leftbtn":
                SwitchCharacter(-1);
                break;
            case "rightbtn":
                SwitchCharacter(1);
                break;
            case "Startbtn":
                StartGame();
                break;
        }
    }

    /// <summary>按照偏移量循环切换角色。</summary>
    private void SwitchCharacter(int offset)
    {
        if (characters == null || characters.Length == 0)
            return;

        int nextIndex = (currentIndex + offset + characters.Length) % characters.Length;
        ShowCharacter(nextIndex);
    }

    /// <summary>显示指定索引的角色预览和信息。</summary>
    private void ShowCharacter(int index)
    {
        currentIndex = index;
        CharacterConfig config = characters[currentIndex];

        RefreshPreview(config);
        RefreshInfo(config);
    }

    /// <summary>刷新场景中的角色预览预制体。</summary>
    private void RefreshPreview(CharacterConfig config)
    {
        if (characterPos == null)
        {
            Debug.LogError("ChooseScene missing characterpos.");
            return;
        }

        if (currentPreview != null)
            Destroy(currentPreview);

        for (int i = characterPos.childCount - 1; i >= 0; --i)
            Destroy(characterPos.GetChild(i).gameObject);

        GameObject prefab = Resources.Load<GameObject>(config.prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Character prefab not found: Resources/{config.prefabPath}.prefab");
            return;
        }

        currentPreview = Instantiate(prefab, characterPos);
        currentPreview.transform.localPosition = Vector3.zero;
        currentPreview.transform.localRotation = Quaternion.identity;
        currentPreview.transform.localScale = Vector3.one;

        Animator animator = currentPreview.GetComponent<Animator>();
        if (animator != null)
            animator.Play(0, 0, 0f);
    }

    /// <summary>刷新角色属性文本。</summary>
    private void RefreshInfo(CharacterConfig config)
    {
        if (infoText == null)
            return;

        infoText.text =
            $"角色：{config.name}\n" +
            $"生命：{config.maxHp}\n" +
            $"攻击：{config.attack}\n" +
            $"防御：{config.defense}\n" +
            $"移速：{config.moveSpeed}\n" +
            $"暴击率：{config.critRate}";
    }

    /// <summary>保存当前角色选择并进入游戏场景。</summary>
    private void StartGame()
    {
        if (characters == null || characters.Length == 0)
            return;

        SelectedCharacterData.SetSelectedCharacter(characters[currentIndex].id);
        SceneMgr.Instance.LoadScene(SceneNameConfig.GameScene);
    }

}
