using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Unity.VisualScripting;

public interface IUIElements
{
    public UIController Controller { get; set; }
}

public class UIController : MonoBehaviour
{
    public static UIController Instance;

    public PlayerController Player;

    public RadialMenu RadialMenu;
    public InventorySystem Inventory;
    public SkillTreeSystem SkillTree;
    public Slider HPSlider;
    public Slider MPSlider;

    private UIState m_currentState;
    private List<IUIElements> m_elements;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        RadialMenu = GetComponentInChildren<RadialMenu>(true);
        Inventory = GetComponentInChildren<InventorySystem>(true);
        SkillTree = GetComponentInChildren<SkillTreeSystem>(true);

        m_elements = new()
        {
            RadialMenu, Inventory, SkillTree
        };

        foreach (var element in m_elements)
        {
            element.Controller = this;
        }
    }

    private void OnEnable()
    {
        Player.input.actions["RadialMenu"].performed += OnRadialMenu;
        Player.input.actions["RadialMenu"].canceled += OnRadialMenu;
        Player.input.actions["Exit"].performed += OnExitPanel;
        Player.model.Health.OnChangeStat += OnHPChanged;
        Player.model.Mana.OnChangeStat += OnMPChanged;
    }

    private void OnDisable()
    {
        if (Player.IsDestroyed() == false)
        {
            Player.input.actions["RadialMenu"].performed -= OnRadialMenu;
            Player.input.actions["RadialMenu"].canceled -= OnRadialMenu;
            Player.input.actions["Exit"].performed -= OnExitPanel;
            Player.model.Health.OnChangeStat -= OnHPChanged;
            Player.model.Mana.OnChangeStat -= OnMPChanged;
        }
    }

    public void OnRadialMenu(InputAction.CallbackContext context)
    {
        if (m_currentState != UIState.None) return;

        if (context.performed)
            RadialMenu.gameObject.SetActive(true);
        else if (context.canceled)
            RadialMenu.gameObject.SetActive(false);
    }

    public void OnExitPanel(InputAction.CallbackContext context)
    {
        if (m_currentState == UIState.None) return;
        foreach (var item in m_elements)
        {
            if (item is MonoBehaviour mono)
            {
                mono.gameObject.SetActive(false);
            }
        }
    }

    public void OnHPChanged()
    {
        var hp = Player.model.Health;
        HPSlider.value = Mathf.Clamp(hp.TotalValue / hp.maxValue, 0f, 1f);
    }

    public void OnMPChanged()
    {
        var mp = Player.model.Mana;
        MPSlider.value = Mathf.Clamp(mp.TotalValue / mp.maxValue, 0f, 1f);
    }

    public void ChangeState(UIState state)
    {
        m_currentState = state;

        // 만약, ui전용 패널이 열릴 경우 ui키입력으로 상태 전환
        Player.input.SwitchCurrentActionMap(state == UIState.None ? InputActionMap.Player.ToString() : InputActionMap.UI.ToString());
    }
}
