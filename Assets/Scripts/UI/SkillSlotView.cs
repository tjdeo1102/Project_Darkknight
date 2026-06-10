using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotView : MonoBehaviour
{
    [Serializable]
    public struct SlotContent
    {
        public Image SlotImage;
        public Animator Animator;
    }

    public SlotContent[] slotContents;
    public PlayerCombat Combat;
    private Stat m_mana;
    private readonly int animParam = Animator.StringToHash("CanUse");

    void Start()
    {
        if (InGameLoop.Instance != null && 
            InGameLoop.Instance.Player != null)
        {
            var Ctrl = InGameLoop.Instance.Player;
            Combat = Ctrl.combat;
            m_mana = Ctrl.model.Mana;
        }
    }

    // Update is called once per frame
    void Update()
    {
        RefreshSkillState();
    }

    public void RefreshSkillState()
    {
        if (Combat != null)
        {
            var skills = Combat.skills;
            for (int i = 0; i < slotContents.Length; i++)
            {
                var skill = i < skills.Count ? skills[i] : null;
                var canUse = skill != null &&
                             Combat.CanUseEquippedSkill(skill) &&
                             skill.CanUseSkill(Combat, m_mana, false);

                if (canUse)
                {
                    slotContents[i].Animator?.SetBool(animParam, true);
                    if (slotContents[i].SlotImage != null)
                        slotContents[i].SlotImage.color = Color.white;
                }
                else
                {
                    slotContents[i].Animator?.SetBool(animParam, false);
                    if (slotContents[i].SlotImage != null)
                        slotContents[i].SlotImage.color = new Color(0.5f, 0.5f, 0.5f);
                }
            }
        }
    }
}
