using UnityEngine;
using UnityEngine.UI;
using static SkillSlotView;

public class SkillSlotManager : MonoBehaviour
{
    public Image[] EquipSkillSlots;
    public SkillSlotView SlotView;

    private Sprite[] m_defaultEquipSprites;
    private Sprite[] m_defaultViewSprites;

    private void Awake()
    {
        m_defaultEquipSprites = CaptureSprites(EquipSkillSlots);

        var viewSlots = SlotView != null ? SlotView.slotContents : null;
        m_defaultViewSprites = new Sprite[viewSlots?.Length ?? 0];
        for (var index = 0; index < m_defaultViewSprites.Length; index++)
        {
            m_defaultViewSprites[index] = viewSlots[index].SlotImage?.sprite;
        }
    }

    private void OnEnable()
    {
        if (SlotView == null
            || EquipSkillSlots.Length < (int)InputSkill.Size
            || SlotView.slotContents.Length < (int)InputSkill.Size)
            gameObject.SetActive(false);
    }

    public void RefreshSkillSlot(SkillBase skill, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= (int)InputSkill.Size) return;

        var icon = skill != null ? skill.Icon : GetDefaultSprite(m_defaultEquipSprites, slotIndex);
        if (EquipSkillSlots[slotIndex] != null)
            EquipSkillSlots[slotIndex].sprite = icon;

        var viewSlots = SlotView.slotContents;
        if (viewSlots[slotIndex].SlotImage != null)
        {
            viewSlots[slotIndex].SlotImage.sprite = skill != null
                ? skill.Icon
                : GetDefaultSprite(m_defaultViewSprites, slotIndex);
        }
    }

    private static Sprite[] CaptureSprites(Image[] images)
    {
        var sprites = new Sprite[images?.Length ?? 0];
        for (var index = 0; index < sprites.Length; index++)
        {
            sprites[index] = images[index]?.sprite;
        }
        return sprites;
    }

    private static Sprite GetDefaultSprite(Sprite[] sprites, int index)
    {
        return sprites != null && index >= 0 && index < sprites.Length
            ? sprites[index]
            : null;
    }
}
