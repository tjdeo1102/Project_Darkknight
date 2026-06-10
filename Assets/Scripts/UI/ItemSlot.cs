using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class ItemSlot : MonoBehaviour, IPointerDownHandler,IPointerEnterHandler, IPointerExitHandler
{
    private const float clickDelay = 0.3f;
    private int clickCount = 0;
    private float clickTime = 0f;

    [SerializeField] private InventoryItem slotItem;
    public InventoryItem SlotItem
    {
        get { return slotItem; }
        set
        {
            slotItem = value;
            if (SlotIcon != null)
            {
                SlotIcon.sprite = value != null ? value.Icon : originIcon;
            }
        }
    }
    private Sprite originIcon;

    public ItemType SlotType;
    public Image SlotIcon;
    public InventorySystem System;

    private void Start()
    {
        originIcon = SlotIcon != null ? SlotIcon.sprite : null;
        if (SlotItem != null) SlotItem = SlotItem;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Time.unscaledTime - clickTime > clickDelay) clickCount = 0;
        clickCount++;
        clickTime = Time.unscaledTime;
        // 슬롯 아이템 스왑 (SlotType에 따라, Equip, Unequip 구분)
        if (clickCount > 1)
        {
            System.SwapItem(this);
            System.HoverEffect(SlotItem, true, eventData.position);
            clickCount = 0;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        System.HoverEffect(SlotItem, true, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        System.HoverEffect(SlotItem, false, eventData.position);
    }
}
