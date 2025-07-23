using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkillTreeSystem : UIElementBase
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
        Controller.Player.input.actions["Skill"].performed += OnSkillEquip;
    }

    protected override void OnDisable()
    {
        if (Controller.Player.IsDestroyed() == false)
        {
            Controller.Player.input.actions["Skill"].performed -= OnSkillEquip;
        }
        EquipBtn.onClick.RemoveListener(EquipSkill);
        EnforceBtn.onClick.RemoveListener(EnforceSkill);
        base.OnDisable();
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
            m_clickedSkillSlot = slot;
            UpdateDescription();
            RefreshEnforceText();
            // 착용 ui는 비활성화
            SkillSlots.gameObject.SetActive(false);
        }
        else m_clickedSkillSlot = null;
        DescriptionWindow.SetActive(isActive);
    }


    private void UpdateDescription()
    {
        var skill = m_clickedSkillSlot.SlotSkill;
        if (DescriptionImage == null || DescriptionText == null || skill == null) return;
        StringBuilder sb = new();
        sb.AppendLine($"<color=#D0E8F2>[{skill.SkillName}]</color>");
        sb.AppendLine(skill.Description);
        sb.AppendLine();
        foreach (var stat in skill.SkillStats)
        {
            sb.AppendLine($"{stat.StatType.ToString()}:");
            sb.AppendLine($"  + Flat: {stat.StatModifier.FixedValue:F1}");
            sb.AppendLine($"  + Percent: {stat.StatModifier.PercentValue:F1}");
            sb.AppendLine($"  + Duration: {stat.Duration:F1}");
        }
        DescriptionText.text = sb.ToString();
        DescriptionImage.sprite = skill.Icon;
    }
    public void EnforceSkill()
    {
        // 비활성화 여부에 따라 / 1. 활성화 / 2. 강화
        var skill = m_clickedSkillSlot.SlotSkill;
        if (skill == null || skill.CanUnlock == false) return;

        var money = Controller.Player.model.Money;
        var cost = skill.EnforceBaseCost * (1 + skill.EnforceLevel * skill.EnforceCostFactor);
        if (money.TotalValue - cost < 0f) return;

        if (skill.CanActive == false)
        {
            // 돈이 있는 경우 비용 지불과 동시에 활성화
            skill.CanActive = true;
            skill.EnforceLevel = 1;
            m_clickedSkillSlot.SlotIcon.color = Color.white;
            money.AddModifier(new StatModifier(-cost, 0), StatModifyType.Permanent);

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
            RefreshEnforceText();
        }
        else
        {
            if (skill.EnforceCostFactor < 0) return;
            skill.EnforceLevel++;
            RefreshEnforceText();
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
        var size = (int)InputSkill.Size;
        int bindingIndex = context.action.GetBindingIndexForControl(context.control);
        if (bindingIndex < 0 || bindingIndex >= size || SkillSlots.gameObject.activeSelf == false) return;

        int currentCount = player.combat.skills.Count;
        // 부족한 만큼 null값 추가하여 리스트 크기 맞추기
        if (currentCount < size)
        {
            int diff = (int)InputSkill.Size - currentCount;
            player.combat.skills.AddRange(Enumerable.Repeat<SkillBase>(null, diff));
        }
        player.combat.skills[bindingIndex] = m_clickedSkillSlot.SlotSkill;
        
        SkillSlots.RefreshSkillSlot(m_clickedSkillSlot.SlotSkill, bindingIndex);
        SkillSlots.gameObject.SetActive(false);

    }
    public void RefreshEnforceText()
    {
        if (m_clickedSkillSlot == null) return;
        var skill = m_clickedSkillSlot.SlotSkill;
        if (EnforceBtnText == null || skill == null) return;
        // 해금되었으면서 강화 비용 없는 경우 Max
        if (skill.CanActive && skill.EnforceCostFactor < 0) EnforceBtnText.text = "Max";
        else EnforceBtnText.text = (skill.EnforceBaseCost * (1 + skill.EnforceLevel * skill.EnforceCostFactor)).ToString();
    }
}
