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

    private void Start()
    {
        var elements = GetComponentsInChildren<UIElementBase>(true);

        UIElements = new();

        foreach (var element in elements)
        {
            element.Controller = this;
            UIElements[element.Type] = element;
        }

        var instance = InGameLoop.Instance;
        if (instance != null)
        {
            instance.KillCount.OnValueChanged += OnKillChanged;
            OnKillChanged(instance.KillCount.Value);
        }
        IsReady = true;
    }
    public override void StartInit()
    {
        base.StartInit();
        Player.input.actions["RadialMenu"].performed += OnRadialMenu;
        Player.input.actions["RadialMenu"].canceled += OnRadialMenu;
        Player.input.actions["Exit"].performed += OnExitPanel;
        Player.model.Health.OnChangeStat += OnHPChanged;
        Player.model.Mana.OnChangeStat += OnMPChanged;
        Player.model.Money.OnChangeStat += OnMoneyChanged;
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
            Player.model.Money.OnChangeStat -= OnMoneyChanged;
        }

        if (InGameLoop.Instance != null)
        {
            InGameLoop.Instance.KillCount.OnValueChanged -= OnKillChanged;
        }
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

            // 원래 색상 저장 및 알파값 0으로 설정
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

            // 모든 요소 duration동안 동시에 Fade
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

            // 완료후 버튼 활성화
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

        // 만약, ui전용 패널이 열릴 경우 ui키입력으로 상태 전환
        
        if (state == UIState.None)
        {
            Time.timeScale = 1f;
            foreach (var element in UIElements.Values)
            {
                if (element.DependencyOnChangeState)
                    element.gameObject.SetActive(false);
            }
            Player.SetActionMap(InputActionMap.Player);
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

            Player.SetActionMap(state == UIState.Dialog ? InputActionMap.Dialog : InputActionMap.UI);
        }
    }
}
