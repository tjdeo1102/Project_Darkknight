using UnityEngine;
using UnityEngine.UI;
using static SkillSlotView;

public class SkillSlotManager : MonoBehaviour
{
    public Image[] EquipSkillSlots;
    public SkillSlotView SlotView;

    private void OnEnable()
    {
        if (SlotView == null
            || EquipSkillSlots.Length < (int)InputSkill.Size
            || SlotView.slotContents.Length < (int)InputSkill.Size)
            gameObject.SetActive(false);
    }

    public void RefreshSkillSlot(SkillBase skill, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > (int)InputSkill.SkillE) return;

        EquipSkillSlots[slotIndex].sprite = skill.Icon;
        var viewSlots = SlotView.slotContents;
        viewSlots[slotIndex].SlotImage.sprite = skill.Icon;
    }
}
