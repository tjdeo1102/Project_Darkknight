using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillTreeSlot : MonoBehaviour, IPointerDownHandler
{
    public SkillBase SlotSkill;
    public Image SlotIcon;
    public SkillTreeSystem System;

    private void Start()
    {
        if (SlotIcon != null) SlotIcon.sprite = SlotSkill.Icon;

        RefreshSlot();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        System.ClickDescriptionWindow(this,true);
    }

    public void RefreshSlot()
    {
        // 비활성화 스킬은 색 어둡게
        if (SlotSkill.CanActive == false) SlotIcon.color = new Color(0.5f, 0.5f, 0.5f);
        // 구매 불가능 스킬은 검정색
        if (SlotSkill.CanUnlock == false) SlotIcon.color = Color.black;
    }
}
