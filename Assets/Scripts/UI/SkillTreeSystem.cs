using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkillTreeSystem : MonoBehaviour, IUIElements
{
    public GameObject DescriptionWindow;
    public TextMeshProUGUI DescriptionText;
    public Image DescriptionImage;
    public SkillSlotManager SkillSlots;
    public SkillTreeSlot[] SkillTreeSlots;

    public Button EquipBtn;
    public Button EnforceBtn;
    public TextMeshProUGUI EnforceBtnText;

    // key: 선행스킬 value: 선행스킬을 요구하는 스킬들
    public Dictionary<SkillBase, List<SkillBase>> preRequireSkillDic;
    private SkillTreeSlot m_clickedSkillSlot;

    public UIController Controller { get; set; }

    private void Start()
    {
        preRequireSkillDic = new();
        foreach (var slot in SkillTreeSlots)
        {
            foreach (var skill in slot.SlotSkill.RequireSkill)
            {
                if (!preRequireSkillDic.ContainsKey(skill))
                {
                    preRequireSkillDic[skill] = new List<SkillBase>();
                }
                preRequireSkillDic[skill].Add(slot.SlotSkill);
            }
        }
    }

    private void OnEnable()
    {
        Controller.ChangeState(UIState.SkillTree);
        EquipBtn.onClick.AddListener(EquipSkill);
        EnforceBtn.onClick.AddListener(EnforceSkill);
        Controller.Player.input.actions["SkillEquip"].performed += OnSkillEquip;
    }

    private void OnDisable()
    {
        if (Controller.Player.IsDestroyed() == false)
        {
            Controller.Player.input.actions["SkillEquip"].performed -= OnSkillEquip;
        }
        EquipBtn.onClick.RemoveListener(EquipSkill);
        EnforceBtn.onClick.RemoveListener(EnforceSkill);
        Controller.ChangeState(UIState.None);

    }
    public void ClickDescriptionWindow (SkillTreeSlot slot, bool isActive)
    {
        var skill = slot.SlotSkill;
        if (skill.CanUnlock == false) return;

        if (skill == null)
        {
            DescriptionWindow.SetActive(false);
            return;
        }
        if (isActive)
        {
            DescriptionText.text = skill.Description;
            DescriptionImage.sprite = skill.Icon;
            EnforceBtnText.text = (skill.EnforceBaseCost * (skill.EnforceLevel + 1) * skill.EnforceCostFactor).ToString();
            m_clickedSkillSlot = slot;
            // 착용 ui는 비활성화
            SkillSlots.gameObject.SetActive(false);
        }
        else m_clickedSkillSlot = null;
        DescriptionWindow.SetActive(isActive);
    }

    public void EnforceSkill()
    {
        // 비활성화 여부에 따라 / 1. 활성화 / 2. 강화
        var skill = m_clickedSkillSlot.SlotSkill;
        if (skill == null || skill.CanUnlock == false) return;

        var money = Controller.Player.model.Money;
        var cost = skill.EnforceBaseCost * (skill.EnforceLevel + 1) * skill.EnforceCostFactor;
        if (money.TotalValue - cost < 0f) return;

        if (skill.CanActive == false)
        {
            // 돈이 있는 경우 비용 지불과 동시에 활성화
            skill.CanActive = true;
            skill.EnforceLevel = 1;
            m_clickedSkillSlot.SlotIcon.color = Color.white;
            money.AddModifier(new StatModifier(-cost, 0), StatModifyType.Perment);

            // 선행스킬로 등록된 스킬들 검사
            if (preRequireSkillDic.TryGetValue(skill,out var items))
            {
                // 리스트 내 스킬들의 각각의 선행스킬을 검사하여, Unlock여부 갱신
                foreach (var item in items)
                {
                    item.UnlockRequireSkill(skill);
                }
            }

            // 모든 검사가 끝나면 슬롯 업데이트
            foreach (var item in SkillTreeSlots)
            {
                item.RefreshSlot();
            }

            EnforceBtnText.text = (skill.EnforceBaseCost * (skill.EnforceLevel + 1) * skill.EnforceCostFactor).ToString();
        }
        else
        {
            skill.EnforceLevel++;
        }
    }

    public void EquipSkill()
    {
        if (m_clickedSkillSlot.SlotSkill == null ||
            m_clickedSkillSlot.SlotSkill.CanActive == false) return;
        SkillSlots.gameObject.SetActive(true);
    }

    public void OnSkillEquip(InputAction.CallbackContext context)
    {
        var player = Controller.Player;

        if (player.InputSkillDic.ContainsKey(context.control.name) == false
            || SkillSlots.gameObject.activeSelf == false) return;

        int skillNum = (int)player.InputSkillDic[context.control.name];
        int targetCount = player.InputSkillDic.Count;
        int currentCount = player.combat.skills.Count;

        if (currentCount < targetCount)
        {
            int diff = targetCount - currentCount;
            player.combat.skills.AddRange(Enumerable.Repeat<SkillBase>(null, diff));
        }
        player.combat.skills[skillNum] = m_clickedSkillSlot.SlotSkill;
        
        SkillSlots.RefreshSkillSlot(m_clickedSkillSlot.SlotSkill, skillNum);
        SkillSlots.gameObject.SetActive(false);

    }
}
