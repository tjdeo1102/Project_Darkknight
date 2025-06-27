using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class InventorySystem : MonoBehaviour, IUIElements
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

    public UIController Controller { get; set; }

    private void OnEnable()
    {
        Controller.ChangeState(UIState.Inventory);
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

    private void OnDisable()
    {
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
        Controller.ChangeState(UIState.None);
    }

    private void Start()
    {
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

    public void UpdateStat(StatType type ,Stat newStat)
    {
        statTexts[type] = $"{type.ToString()} : {newStat.TotalValue:F1} <color=green>(+{newStat.addValue:F1})</color>";

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
            DescriptionText.text = item.Description;
            DescriptionImage.sprite = item.Icon;
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
            if (equipDic.ContainsKey(from.SlotItem.ItemType))
            {
                var equipSlot = equipDic[from.SlotItem.ItemType];
                // 스탯 갱신
                if (equipSlot.SlotItem != null)
                {
                    foreach (var modifyStat in equipSlot.SlotItem.ModifierStats)
                    {
                        // 기존 장착된 무기의 스탯의 값은 빼기
                        model.Stats[modifyStat.type].AddModifier(-modifyStat.modifier, StatModifyType.Equipment);
                    }
                }
                if (from.SlotItem != null)
                {
                    foreach (var modifyStat in from.SlotItem.ModifierStats)
                    {
                        // 새로 장착할 무기의 스탯의 값은 더하기
                        model.Stats[modifyStat.type].AddModifier(modifyStat.modifier, StatModifyType.Equipment);
                    }

                }

                // 스왑
                (from.SlotItem, equipSlot.SlotItem) = (equipSlot.SlotItem, from.SlotItem);
            }
        }
        // Equip -> Unequip로 아이템 전송 (장착 해제)
        else
        {
            var res = UnequipSlots.First(slot => slot.SlotItem == null);

            // 스탯 갱신
            if (from.SlotItem.ModifierStats != null)
            {
                foreach (var modifyStat in from.SlotItem.ModifierStats)
                {
                    // 기존 장착된 무기의 스탯의 값은 빼기
                    model.Stats[modifyStat.type].AddModifier(-modifyStat.modifier, StatModifyType.Equipment);
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
}
