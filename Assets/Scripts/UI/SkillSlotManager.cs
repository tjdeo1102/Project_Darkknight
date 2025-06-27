using UnityEngine;
using UnityEngine.UI;

public class SkillSlotManager : MonoBehaviour
{
    public Image[] EquipSkillSlots;
    public Image[] BackgroundSkillSlots;

    private void OnEnable()
    {
        if (EquipSkillSlots.Length < (int)InputSkill.Size
            || BackgroundSkillSlots.Length < (int)InputSkill.Size)
            gameObject.SetActive(false);
    }

    public void RefreshSkillSlot(SkillBase skill, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > (int)InputSkill.SkillE) return;

        EquipSkillSlots[slotIndex].sprite = skill.Icon;
        BackgroundSkillSlots[slotIndex].sprite = skill.Icon;
    }
}
