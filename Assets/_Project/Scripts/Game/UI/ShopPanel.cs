using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 构建遗物商店，处理多选购买，并应用已购买遗物。
/// </summary>
public class ShopPanel : BasePanel
{
    /// <summary>商店槽位数量。</summary>
    private const int SlotCount = 6;
    /// <summary>六个商店槽位的数据。</summary>
    private readonly ShopSlot[] slots = new ShopSlot[SlotCount];
    /// <summary>选中槽位时价格文本颜色。</summary>
    private readonly Color selectedPriceColor = Color.green;
    /// <summary>遗物名称文本。</summary>
    private Text itemNameText;
    /// <summary>遗物描述文本。</summary>
    private Text itemDescText;
    /// <summary>购买失败提示文本。</summary>
    private Text tipText;
    /// <summary>当前玩家运行时属性。</summary>
    private PlayerRuntimeData runtimeData;
    /// <summary>当前玩家成长数据。</summary>
    private PlayerProgression progression;
    /// <summary>当前玩家遗物效果组件。</summary>
    private PlayerRelicEffects relicEffects;
    /// <summary>关闭商店后进入下一流程的回调。</summary>
    private Action onClose;

    /// <summary>单个商店槽位的运行时数据。</summary>
    private class ShopSlot
    {
        /// <summary>槽位根对象。</summary>
        public GameObject root;
        /// <summary>槽位按钮。</summary>
        public Button button;
        /// <summary>槽位遗物图标。</summary>
        public Image icon;
        /// <summary>槽位价格文本。</summary>
        public Text priceText;
        /// <summary>价格文本默认颜色。</summary>
        public Color normalPriceColor = Color.white;
        /// <summary>槽位出售的遗物。</summary>
        public RelicConfig relic;
        /// <summary>槽位价格。</summary>
        public int price;
        /// <summary>槽位是否被玩家选中。</summary>
        public bool selected;
        /// <summary>槽位是否已经购买。</summary>
        public bool sold;
    }

    /// <summary>缓存商店控件和槽位引用。</summary>
    protected override void Awake()
    {
        base.Awake();
        itemNameText = FindText("itemnametxt");
        itemDescText = FindText("itemdistxt");
        tipText = FindText("tiptxt");
        if (tipText != null)
            tipText.gameObject.SetActive(false);

        for (int i = 0; i < SlotCount; ++i)
            slots[i] = BuildSlot(i + 1);
    }

    /// <summary>绑定玩家数据并刷新商店商品。</summary>
    public void Bind(PlayerRuntimeData data, PlayerProgression playerProgression, PlayerRelicEffects effects, Action closedCallback)
    {
        runtimeData = data;
        progression = playerProgression;
        relicEffects = effects;
        onClose = closedCallback;
        FillSlots();
        SetInfo(null);
        if (tipText != null)
            tipText.gameObject.SetActive(false);
    }

    /// <summary>商店面板显示时暂无额外逻辑。</summary>
    public override void ShowMe()
    {
    }

    /// <summary>商店面板隐藏时暂无额外逻辑。</summary>
    public override void HideMe()
    {
    }

    /// <summary>处理购买、跳过和槽位点击。</summary>
    protected override void ClickBtn(string btnName)
    {
        if (btnName == "buybutton")
        {
            BuySelected();
            return;
        }

        if (btnName == "passbutton")
        {
            CloseShop();
            return;
        }

        int slotIndex = GetSlotIndex(btnName);
        if (slotIndex >= 0)
            ToggleSlot(slotIndex);
    }

    /// <summary>生成本次商店商品并刷新槽位 UI。</summary>
    private void FillSlots()
    {
        List<RelicConfig> relics = CreateShopRelics();
        for (int i = 0; i < SlotCount; ++i)
        {
            ShopSlot slot = slots[i];
            if (slot == null || slot.root == null)
                continue;

            slot.root.SetActive(i < relics.Count);
            if (i >= relics.Count)
                continue;

            slot.relic = relics[i];
            slot.price = GetPrice(slot.relic.rarity);
            slot.selected = false;
            slot.sold = false;

            if (slot.button != null)
                slot.button.interactable = true;
            if (slot.priceText != null)
            {
                slot.normalPriceColor = slot.priceText.color;
                slot.priceText.text = $"{slot.price}$";
                slot.priceText.color = slot.normalPriceColor;
            }
            if (slot.icon != null)
            {
                slot.icon.sprite = Resources.Load<Sprite>(slot.relic.spritePath);
                slot.icon.preserveAspect = true;
            }
        }
    }

    /// <summary>按 3:2:1 稀有度生成六个不重复遗物。</summary>
    private List<RelicConfig> CreateShopRelics()
    {
        List<RelicConfig> result = new List<RelicConfig>();
        AddRandomRelics(result, "common", 3);
        AddRandomRelics(result, "uncommon", 2);
        AddRandomRelics(result, "rare", 1);
        Shuffle(result);
        return result;
    }

    /// <summary>从指定稀有度池中随机添加遗物。</summary>
    private void AddRandomRelics(List<RelicConfig> result, string rarity, int count)
    {
        List<RelicConfig> pool = RelicConfigLoader.GetRelics(rarity, runtimeData != null ? runtimeData.Id : string.Empty);
        for (int i = pool.Count - 1; i >= 0; --i)
        {
            if (ContainsRelic(result, pool[i].id))
                pool.RemoveAt(i);
        }

        while (count > 0 && pool.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
            count--;
        }
    }

    /// <summary>判断列表中是否已经包含指定遗物。</summary>
    private bool ContainsRelic(List<RelicConfig> relics, string id)
    {
        for (int i = 0; i < relics.Count; ++i)
        {
            if (relics[i].id == id)
                return true;
        }

        return false;
    }

    /// <summary>切换槽位选中状态并刷新详情。</summary>
    private void ToggleSlot(int index)
    {
        ShopSlot slot = slots[index];
        if (slot == null || slot.sold || slot.relic == null)
            return;

        slot.selected = !slot.selected;
        if (slot.priceText != null)
            slot.priceText.color = slot.selected ? selectedPriceColor : slot.normalPriceColor;

        SetInfo(slot.selected ? slot.relic : null);
    }

    /// <summary>购买所有选中槽位，金币不足时全部不购买。</summary>
    private void BuySelected()
    {
        int totalPrice = GetSelectedTotalPrice();
        if (totalPrice <= 0)
            return;

        if (progression == null || progression.Gold < totalPrice)
        {
            ShowTip("金币不足");
            return;
        }

        progression.SpendGold(totalPrice);
        for (int i = 0; i < slots.Length; ++i)
        {
            ShopSlot slot = slots[i];
            if (slot == null || !slot.selected || slot.sold || slot.relic == null)
                continue;

            relicEffects?.ApplyRelic(slot.relic);
            slot.sold = true;
            slot.selected = false;
            slot.root.SetActive(false);
        }

        SetInfo(null);
    }

    /// <summary>计算当前所有选中遗物的总价。</summary>
    private int GetSelectedTotalPrice()
    {
        int total = 0;
        for (int i = 0; i < slots.Length; ++i)
        {
            ShopSlot slot = slots[i];
            if (slot != null && slot.selected && !slot.sold && slot.relic != null)
                total += slot.price;
        }

        return total;
    }

    /// <summary>显示购买失败提示。</summary>
    private void ShowTip(string text)
    {
        if (tipText == null)
            return;

        StopCoroutine(nameof(HideTipAfterDelay));
        tipText.text = text;
        tipText.gameObject.SetActive(true);
        StartCoroutine(HideTipAfterDelay());
    }

    /// <summary>一秒后隐藏提示文本。</summary>
    private IEnumerator HideTipAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1f);
        if (tipText != null)
            tipText.gameObject.SetActive(false);
    }

    /// <summary>关闭商店并进入下一关流程。</summary>
    private void CloseShop()
    {
        Action callback = onClose;
        onClose = null;
        UIMgr.Instance.HidePanel<ShopPanel>(true);
        callback?.Invoke();
    }

    /// <summary>刷新右侧遗物信息文本。</summary>
    private void SetInfo(RelicConfig relic)
    {
        if (itemNameText != null)
            itemNameText.text = relic != null ? relic.name : string.Empty;
        if (itemDescText != null)
            itemDescText.text = relic != null ? relic.description : string.Empty;
    }

    /// <summary>根据稀有度随机生成价格。</summary>
    private int GetPrice(string rarity)
    {
        switch (rarity)
        {
            case "rare":
                return UnityEngine.Random.Range(95, 106);
            case "uncommon":
                return UnityEngine.Random.Range(50, 61);
            default:
                return UnityEngine.Random.Range(20, 26);
        }
    }

    /// <summary>把槽位按钮名转换为槽位索引。</summary>
    private int GetSlotIndex(string btnName)
    {
        if (!btnName.StartsWith("pos") || !btnName.EndsWith("btn"))
            return -1;

        string numberText = btnName.Substring(3, btnName.Length - 6);
        if (!int.TryParse(numberText, out int number))
            return -1;

        int index = number - 1;
        return index >= 0 && index < SlotCount ? index : -1;
    }

    /// <summary>打乱列表顺序。</summary>
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; ++i)
        {
            int index = UnityEngine.Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[index];
            list[index] = temp;
        }
    }

    /// <summary>构建指定编号的槽位引用。</summary>
    private ShopSlot BuildSlot(int number)
    {
        GameObject root = FindObject($"itempos{number}");
        Button button = FindButton($"pos{number}btn");
        Text price = root != null ? FindTextIn(root.transform, "price") : null;

        return new ShopSlot
        {
            root = root,
            button = button,
            icon = button != null ? button.GetComponent<Image>() : null,
            priceText = price,
            normalPriceColor = price != null ? price.color : Color.white
        };
    }

    /// <summary>按名字在商店面板内查找对象。</summary>
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

    /// <summary>按名字在商店面板内查找按钮。</summary>
    private Button FindButton(string buttonName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; ++i)
        {
            if (buttons[i].name == buttonName)
                return buttons[i];
        }

        return null;
    }

    /// <summary>按名字在商店面板内查找文本。</summary>
    private Text FindText(string textName)
    {
        return FindTextIn(transform, textName);
    }

    /// <summary>在指定节点下按名字查找文本。</summary>
    private Text FindTextIn(Transform root, string textName)
    {
        Text[] texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; ++i)
        {
            if (texts[i].name == textName)
                return texts[i];
        }

        return null;
    }
}
