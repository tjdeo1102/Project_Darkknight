using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySystem : UIElementBase
{
    public List<ItemSlot> EquipSlots;
    public List<ItemSlot> UnequipSlots;
    public TextMeshProUGUI StatText;

    public GameObject DescriptionWindow;
    public TextMeshProUGUI DescriptionText;
    public Image DescriptionImage;
    private Dictionary<ItemType, ItemSlot> equipDic = new();
    private Dictionary<StatType, Action> listeners = new();

    private Dictionary<StatType, string> statTexts = new();
    private Button m_closeButton;

    private void OnEnable()
    {
        if (Controller == null || Controller.Player == null) return;
        EnsureCloseButton();
        var model = Controller.Player.model;
        if (model != null)
        {
            foreach (var stat in model.Stats)
            {
                Action listen = () => UpdateStat(stat.Key, stat.Value);
                listeners.Add(stat.Key, listen);
                stat.Value.OnChangeStat += listen;
                listen.Invoke();
            }
        }
    }

    protected override void OnDisable()
    {
        if (Controller == null || Controller.Player == null)
        {
            base.OnDisable();
            return;
        }

        var model = Controller.Player.model;
        if (model != null)
        {
            foreach (var stat in model.Stats)
            {
                if (listeners.ContainsKey(stat.Key) == false) continue;
                stat.Value.OnChangeStat -= listeners[stat.Key];
                listeners.Remove(stat.Key);
            }
        }
        base.OnDisable();
    }

    private void Start()
    {
        EnsureCloseButton();
        foreach (ItemSlot slot in EquipSlots)
        {
            equipDic.Add(slot.SlotType, slot);
            slot.System = this;
        }

        foreach (var slot in UnequipSlots)
        {
            slot.System = this;
        }
    }

    private void EnsureCloseButton()
    {
        if (m_closeButton != null) return;

        var buttonObject = new GameObject(
            "CloseButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(transform, false);
        buttonObject.transform.SetAsLastSibling();

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-38f, -32f);
        rect.sizeDelta = new Vector2(42f, 42f);

        m_closeButton = buttonObject.GetComponent<Button>();
        m_closeButton.targetGraphic = buttonObject.GetComponent<Image>();
        m_closeButton.onClick.AddListener(() => Controller?.CloseCurrentPanel());
        EasternFantasyUI.StyleButton(m_closeButton);

        var label = EasternFantasyUI.CreateLabel(
            buttonObject.transform,
            "Label",
            StatText);
        label.text = "X";
        label.fontSize = 24f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 24f;

        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    public void UpdateStat(StatType type ,Stat newStat)
    {
        statTexts[type] = $"{type.ToString()} : {newStat.TotalValue:F1} <color=green>(+{newStat.GetTotalModifier(StatModifyType.Equipment):F1})</color>";

        StringBuilder sb = new StringBuilder();
        foreach (var item in statTexts)
        {
            sb.AppendLine(item.Value);
        }
        sb.AppendLine();
        StatText.text = sb.ToString();
    }

    public void HoverEffect(InventoryItem item, bool isActive, Vector2 hoverPos)
    {
        if (item == null)
        {
            DescriptionWindow.SetActive(false);
            return;
        }
        if (isActive)
        {
            UpdateDescription(item);
        }
        DescriptionWindow.GetComponent<RectTransform>().position = hoverPos;
        DescriptionWindow.SetActive(isActive);
    }

    public void SwapItem(ItemSlot from)
    {
        var model = Controller.Player.model;
        // Unequip -> Equip로 아이템 전송
        if (from.SlotType == ItemType.None)
        {
            // 같은 아이템 타입의 Equip 슬롯과 Swap (타입 별 Equip 슬롯은 하나)

            if (from.SlotItem!= null && equipDic.ContainsKey(from.SlotItem.ItemType))
            {
                var equipSlot = equipDic[from.SlotItem.ItemType];
                // 스탯 갱신
                if (equipSlot.SlotItem != null)
                {
                    foreach (var modifyStat in equipSlot.SlotItem.ModifierStats)
                    {
                        // 기존 장착된 무기의 스탯의 값은 빼기
                        model.Stats[modifyStat.StatType].AddModifier(-modifyStat.StatModifier, StatModifyType.Equipment);
                    }
                }
                if (from.SlotItem != null)
                {
                    foreach (var modifyStat in from.SlotItem.ModifierStats)
                    {
                        // 새로 장착할 무기의 스탯의 값은 더하기
                        model.Stats[modifyStat.StatType].AddModifier(modifyStat.StatModifier, StatModifyType.Equipment);
                    }

                }
                // 스왑
                (from.SlotItem, equipSlot.SlotItem) = (equipSlot.SlotItem, from.SlotItem);
            }
        }
        // Equip -> Unequip로 아이템 전송 (장착 해제)
        else
        {
            if (from.SlotItem == null) return;

            var res = UnequipSlots.FirstOrDefault(slot => slot.SlotItem == null);
            if (res == null) return;

            // 스탯 갱신
            if (from.SlotItem.ModifierStats != null)
            {
                foreach (var modifyStat in from.SlotItem.ModifierStats)
                {
                    // 기존 장착된 무기의 스탯의 값은 빼기
                    model.Stats[modifyStat.StatType].AddModifier(-modifyStat.StatModifier, StatModifyType.Equipment);
                }
            }

            (from.SlotItem, res.SlotItem) = (res.SlotItem, from.SlotItem);
        }

        // Unequip 슬롯 정렬
        var sortedItems = UnequipSlots
            .Select(slot => slot.SlotItem)
            .OrderBy(item => item == null)
            .ToList();

        for (int i = 0; i < sortedItems.Count; i++)
        {
            UnequipSlots[i].SlotItem = sortedItems[i];
        }
    }

    public bool TryAddItem(InventoryItem item)
    {
        if (item == null) return false;

        foreach (var slot in UnequipSlots)
        {
            if (slot.SlotItem == null)
            {
                slot.SlotItem = item;
                return true;
            }
        }
        return false;
    }

    public bool HasEmptySlot()
    {
        return UnequipSlots.Any(slot => slot != null && slot.SlotItem == null);
    }

    public bool CanPurchaseProgressionItem(
        InventoryItem item,
        out int requiredLevel)
    {
        requiredLevel = 0;
        if (item == null ||
            item.ItemType == ItemType.None ||
            item.ProgressionLevel <= 1)
        {
            return true;
        }

        requiredLevel = item.ProgressionLevel - 1;
        var predecessorLevel = requiredLevel;
        return EnumerateOwnedItems().Any(ownedItem =>
            ownedItem.ItemType == item.ItemType &&
            ownedItem.ProgressionLevel == predecessorLevel &&
            IsSameProgressionSeries(ownedItem, item));
    }

    private static bool IsSameProgressionSeries(
        InventoryItem ownedItem,
        InventoryItem targetItem)
    {
        if (ownedItem.ProgressionSeries < 0 || targetItem.ProgressionSeries < 0)
        {
            return true;
        }

        return ownedItem.ProgressionSeries == targetItem.ProgressionSeries;
    }

    private IEnumerable<InventoryItem> EnumerateOwnedItems()
    {
        var equippedSlots = EquipSlots ?? Enumerable.Empty<ItemSlot>();
        var unequippedSlots = UnequipSlots ?? Enumerable.Empty<ItemSlot>();

        return equippedSlots
            .Concat(unequippedSlots)
            .Where(slot => slot != null && slot.SlotItem != null)
            .Select(slot => slot.SlotItem);
    }

    private void UpdateDescription(InventoryItem item)
    {
        if (DescriptionImage ==null || DescriptionText ==null) return;
        StringBuilder sb = new();
        sb.AppendLine($"<color=#D0E8F2>[{item.ItemType.ToString()}]</color>");
        sb.AppendLine(item.Description);
        sb.AppendLine();
        foreach (var stat in item.ModifierStats)
        {
            sb.AppendLine($"{stat.StatType.ToString()}:");
            sb.AppendLine($"  + Flat: {stat.StatModifier.FixedValue:F1}");
            sb.AppendLine($"  + Percent: {stat.StatModifier.PercentValue:F1}");
        }
        DescriptionText.text = sb.ToString();
        DescriptionImage.sprite = item.Icon;
    }
}
