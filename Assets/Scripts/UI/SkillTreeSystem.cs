using System.Linq;
using System.Text;
using TMPro;
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

    private SkillTreeSlot m_clickedSkillSlot;
    private PlayerCombat m_combat;
    private InputAction m_skillEquipAction;
    private bool m_runtimeEquipListener;
    private bool m_runtimeEnforceListener;

    private void Start()
    {
        ResolveCombat();
        RefreshAllSlots();
    }

    private void OnEnable()
    {
        if (Controller == null || Controller.Player == null) return;

        ResolveCombat();
        m_runtimeEquipListener = AddListenerWhenMissing(EquipBtn, nameof(EquipSkill), EquipSkill);
        m_runtimeEnforceListener = AddListenerWhenMissing(EnforceBtn, nameof(EnforceSkill), EnforceSkill);
        if (Controller.Player.input?.actions != null)
        {
            m_skillEquipAction = Controller.Player.input.actions.FindAction("UI/Skill");
            if (m_skillEquipAction != null)
                m_skillEquipAction.performed += OnSkillEquip;
        }
        RefreshAllSlots();
    }

    protected override void OnDisable()
    {
        if (Controller != null &&
            Controller.Player != null &&
            Controller.Player.input?.actions != null &&
            m_skillEquipAction != null)
        {
            m_skillEquipAction.performed -= OnSkillEquip;
        }

        if (m_runtimeEquipListener)
            EquipBtn?.onClick.RemoveListener(EquipSkill);
        if (m_runtimeEnforceListener)
            EnforceBtn?.onClick.RemoveListener(EnforceSkill);
        m_runtimeEquipListener = false;
        m_runtimeEnforceListener = false;
        base.OnDisable();
    }

    private bool AddListenerWhenMissing(Button button, string methodName, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return false;

        for (var index = 0; index < button.onClick.GetPersistentEventCount(); index++)
        {
            if (button.onClick.GetPersistentTarget(index) == this &&
                button.onClick.GetPersistentMethodName(index) == methodName)
            {
                return false;
            }
        }

        button.onClick.AddListener(action);
        return true;
    }

    public void ClickDescriptionWindow(SkillTreeSlot slot, bool isActive)
    {
        var skill = slot?.SlotSkill;
        if (skill == null || IsSkillUnlocked(skill) == false)
        {
            DescriptionWindow?.SetActive(false);
            return;
        }

        if (isActive)
        {
            m_clickedSkillSlot = slot;
            UpdateDescription();
            RefreshEnforceText();
            SkillSlots?.gameObject.SetActive(false);
        }
        else
        {
            m_clickedSkillSlot = null;
        }

        DescriptionWindow?.SetActive(isActive);
    }

    private void UpdateDescription()
    {
        var skill = m_clickedSkillSlot?.SlotSkill;
        if (DescriptionImage == null || DescriptionText == null || skill == null) return;

        var builder = new StringBuilder();
        builder.AppendLine($"<color=#D0E8F2>[{skill.SkillName}]</color>");
        builder.AppendLine(skill.Description);
        builder.AppendLine();

        foreach (var stat in skill.SkillStats ?? System.Array.Empty<StatChange>())
        {
            builder.AppendLine($"{stat.StatType}:");
            builder.AppendLine($"  + Flat: {stat.StatModifier.FixedValue:F1}");
            builder.AppendLine($"  + Percent: {stat.StatModifier.PercentValue:F1}");
            builder.AppendLine($"  + Duration: {stat.Duration:F1}");
        }

        DescriptionText.text = builder.ToString();
        DescriptionImage.sprite = skill.Icon;
    }

    public void EnforceSkill()
    {
        var skill = m_clickedSkillSlot?.SlotSkill;
        if (skill == null || IsSkillUnlocked(skill) == false || ResolveCombat() == false) return;

        var state = m_combat.GetSkillState(skill);
        if (state.IsActive && skill.EnforceCostFactor < 0f) return;

        var cost = skill.GetEnforceCost(state);
        var money = Controller?.Player?.model?.Money;
        if (money == null) return;
        if (money.TotalValue < cost) return;

        money.AddModifier(new StatModifier(-cost, 0), StatModifyType.Permanent);
        if (state.IsActive)
        {
            state.EnforceLevel++;
        }
        else
        {
            state.IsActive = true;
            state.EnforceLevel = Mathf.Max(1, state.EnforceLevel);
        }

        RefreshAllSlots();
        RefreshEnforceText();
    }

    public void EquipSkill()
    {
        var skill = m_clickedSkillSlot?.SlotSkill;
        if (skill == null || ResolveCombat() == false || m_combat.GetSkillState(skill).IsActive == false) return;

        SkillSlots?.gameObject.SetActive(true);
    }

    public void OnSkillEquip(InputAction.CallbackContext context)
    {
        if (ResolveCombat() == false || SkillSlots == null || SkillSlots.gameObject.activeSelf == false) return;

        var size = (int)InputSkill.Size;
        var bindingIndex = PlayerCombat.GetSkillSlotIndex(context);
        if (bindingIndex < 0 || bindingIndex >= size) return;

        var selectedSkill = m_clickedSkillSlot?.SlotSkill;
        if (selectedSkill == null || m_combat.GetSkillState(selectedSkill).IsActive == false) return;

        if (m_combat.skills.Count < size)
        {
            m_combat.skills.AddRange(Enumerable.Repeat<SkillBase>(null, size - m_combat.skills.Count));
        }

        for (var index = 0; index < size; index++)
        {
            if (index == bindingIndex || m_combat.skills[index] != selectedSkill) continue;

            m_combat.skills[index] = null;
            SkillSlots.RefreshSkillSlot(null, index);
        }

        m_combat.skills[bindingIndex] = selectedSkill;
        SkillSlots.RefreshSkillSlot(selectedSkill, bindingIndex);
        SkillSlots.gameObject.SetActive(false);
    }

    public void RefreshEnforceText()
    {
        var skill = m_clickedSkillSlot?.SlotSkill;
        if (EnforceBtnText == null || skill == null || ResolveCombat() == false) return;

        var state = m_combat.GetSkillState(skill);
        EnforceBtnText.text = state.IsActive && skill.EnforceCostFactor < 0f
            ? "Max"
            : skill.GetEnforceCost(state).ToString();
    }

    public SkillRuntimeState GetSkillState(SkillBase skill)
    {
        return ResolveCombat() ? m_combat.GetSkillState(skill) : null;
    }

    public bool IsSkillUnlocked(SkillBase skill)
    {
        return ResolveCombat() && m_combat.IsSkillUnlocked(skill);
    }

    private bool ResolveCombat()
    {
        if (m_combat == null)
        {
            m_combat = Controller?.Player?.combat;
        }

        return m_combat != null;
    }

    private void RefreshAllSlots()
    {
        if (SkillTreeSlots == null) return;

        foreach (var slot in SkillTreeSlots)
        {
            slot?.RefreshSlot();
        }
    }
}
