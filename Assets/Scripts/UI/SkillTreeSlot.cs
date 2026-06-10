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
        if (SlotIcon != null && SlotSkill != null) SlotIcon.sprite = SlotSkill.Icon;

        RefreshSlot();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        System.ClickDescriptionWindow(this,true);
    }

    public void RefreshSlot()
    {
        if (SlotSkill == null || SlotIcon == null || System == null) return;

        var state = System.GetSkillState(SlotSkill);
        if (System.IsSkillUnlocked(SlotSkill) == false)
        {
            SlotIcon.color = Color.black;
            return;
        }

        SlotIcon.color = state != null && state.IsActive
            ? Color.white
            : new Color(0.5f, 0.5f, 0.5f);
    }
}
