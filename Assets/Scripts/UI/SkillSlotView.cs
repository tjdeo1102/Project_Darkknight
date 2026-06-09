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
            if (Combat.skills.Count != slotContents.Length) return;

            var skills = Combat.skills;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i] != null && skills[i].CanUseSkill(Combat, m_mana, false))
                {
                    slotContents[i].Animator?.SetBool(animParam, true);
                    slotContents[i].SlotImage.color = Color.white;
                }
                else
                {
                    slotContents[i].Animator?.SetBool(animParam, false);
                    slotContents[i].SlotImage.color = new Color(0.5f, 0.5f, 0.5f);
                }
            }
        }
    }
}
