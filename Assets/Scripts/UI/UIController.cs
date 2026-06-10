using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using DG.Tweening;
using TMPro;

public class UIController : ManagerBase<UIController>
{
    public PlayerController Player;

    public Dictionary<UIState,UIElementBase> UIElements;
    public Slider HPSlider;
    public Slider MPSlider;
    public TextMeshProUGUI KillText;
    public TextMeshProUGUI MoneyText;

    private UIState m_currentState;
    private bool m_isInitialized;
    public UIState CurrentState => m_currentState;

    private void Start()
    {
        var elements = GetComponentsInChildren<UIElementBase>(true);

        UIElements = new();

        foreach (var element in elements)
        {
            element.Controller = this;
            UIElements[element.Type] = element;
        }

        IsReady = true;
    }
    public override void StartInit()
    {
        base.StartInit();
        if (m_isInitialized) return;
        if (Player == null || Player.input == null || Player.input.actions == null || Player.model == null) return;

        Player.input.actions["RadialMenu"].performed += OnRadialMenu;
        Player.input.actions["RadialMenu"].canceled += OnRadialMenu;
        Player.input.actions["Exit"].performed += OnExitPanel;
        Player.model.Health.OnChangeStat += OnHPChanged;
        Player.model.Mana.OnChangeStat += OnMPChanged;
        Player.model.Money.OnChangeStat += OnMoneyChanged;

        var instance = InGameLoop.Instance;
        if (instance?.KillCount != null)
        {
            instance.KillCount.OnValueChanged += OnKillChanged;
            OnKillChanged(instance.KillCount.Value);
        }

        m_isInitialized = true;
        OnHPChanged();
        OnMPChanged();
        OnMoneyChanged();
    }
    private void OnDisable()
    {
        if (Player != null && Player.IsDestroyed() == false)
        {
            Player.input.actions["RadialMenu"].performed -= OnRadialMenu;
            Player.input.actions["RadialMenu"].canceled -= OnRadialMenu;
            Player.input.actions["Exit"].performed -= OnExitPanel;
            Player.model.Health.OnChangeStat -= OnHPChanged;
            Player.model.Mana.OnChangeStat -= OnMPChanged;
            Player.model.Money.OnChangeStat -= OnMoneyChanged;
        }

        if (InGameLoop.Instance?.KillCount != null)
        {
            InGameLoop.Instance.KillCount.OnValueChanged -= OnKillChanged;
        }
        m_isInitialized = false;
    }

    public void OnRadialMenu(InputAction.CallbackContext context)
    {
        if (m_currentState != UIState.None || m_currentState == UIState.Die) return;

        if (UIElements.TryGetValue(UIState.RadialMenu, out var element))
        {
            if (context.performed)
                element.gameObject.SetActive(true);
            else if (context.canceled)
                element.gameObject.SetActive(false);
        }
    }

    public void OnExitPanel(InputAction.CallbackContext context)
    {
        if (m_currentState == UIState.None || m_currentState == UIState.Die) return;
        ChangeState(UIState.None);
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
    public void OnMoneyChanged()
    {
        if (Player == null || Player.model == null || MoneyText == null) return;

        var money = Player.model.Money;
        MoneyText.text = $"{money.TotalValue} $";
    }
    public void OnKillChanged(int newKill)
    {
        KillText.text = $"{newKill} Kill";
    }
    public void DeadUI(float duration)
    {
        ChangeState(UIState.Die);
        if (UIElements.TryGetValue(UIState.Die, out var element))
        {
            element.gameObject.SetActive(true);
            var imgs = element.GetComponentsInChildren<Image>(true);
            var texts = element.GetComponentsInChildren<TextMeshProUGUI>(true);
            var btns = element.GetComponentsInChildren<Button>(true);

            // Store original colors and start fully transparent.
            Color[] imgsOriginalAlphas = new Color[imgs.Length];
            Color[] textsOriginalAlphas = new Color[texts.Length];
            float[] btnsOriginalAlphas = new float[btns.Length];

            for (int i = 0; i < imgs.Length; i++)
            {
                imgsOriginalAlphas[i] = imgs[i].color;
                imgs[i].color = new Color(imgsOriginalAlphas[i].r, imgsOriginalAlphas[i].g, imgsOriginalAlphas[i].b, 0f);
            }

            for (int i = 0; i < texts.Length; i++)
            {
                textsOriginalAlphas[i] = texts[i].color;
                texts[i].color = new Color(textsOriginalAlphas[i].r, textsOriginalAlphas[i].g, textsOriginalAlphas[i].b, 0f);
            }

            for (int i = 0; i < btns.Length; i++)
            {
                var c = btns[i].image.color;
                btnsOriginalAlphas[i] = c.a;
                btns[i].image.color = new Color(c.r, c.g, c.b, 0f);
                btns[i].interactable = false;
            }

            // Fade all elements together.
            DG.Tweening.Sequence seq = DOTween.Sequence();
            seq.SetUpdate(true);

            for (int i = 0; i < imgs.Length; i++)
            {
                seq.Join(imgs[i].DOColor(imgsOriginalAlphas[i], duration));
            }
            for (int i = 0; i < texts.Length; i++)
            {
                seq.Join(texts[i].DOColor(textsOriginalAlphas[i], duration));
            }

            // Enable buttons after the fade completes.
            seq.OnComplete(() =>
            {
                for (int i = 0; i < btns.Length; i++)
                {
                    btns[i].interactable = true;
                }
            });

            seq.Play();
        }
    }

    public void ChangeState(UIState state)
    {
        m_currentState = state;

        // Switch input maps while a UI-only panel is open.
        
        if (state == UIState.None)
        {
            Time.timeScale = 1f;
            foreach (var element in UIElements.Values)
            {
                if (element.DependencyOnChangeState)
                    element.gameObject.SetActive(false);
            }
            Player?.SetActionMap(InputActionMap.Player);
        }
        else
        {
            Time.timeScale = 0f;
            if (UIElements.TryGetValue(state, out var element))
            {
                if (element.DependencyOnChangeState)
                    element.gameObject.SetActive(true);
            }
            else ChangeState(UIState.None);

            Player?.SetActionMap(state == UIState.Dialog ? InputActionMap.Dialog : InputActionMap.UI);
        }
    }
}
