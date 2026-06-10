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
    private InputAction m_radialMenuAction;
    private InputAction m_exitAction;
    private CanvasGroup m_noticeGroup;
    private TextMeshProUGUI m_noticeText;
    private DG.Tweening.Sequence m_noticeSequence;
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

        ChangeState(UIState.None);
        InitializeNotice();
        IsReady = true;
    }
    public override void StartInit()
    {
        base.StartInit();
        if (m_isInitialized) return;
        if (Player == null || Player.input == null || Player.input.actions == null || Player.model == null) return;

        m_radialMenuAction = Player.input.actions.FindAction("Player/RadialMenu");
        m_exitAction = Player.input.actions.FindAction("UI/Exit");
        if (m_radialMenuAction == null || m_exitAction == null)
        {
            Debug.LogError("Required UI input actions are missing.", this);
            return;
        }

        m_radialMenuAction.performed += OnRadialMenu;
        m_radialMenuAction.canceled += OnRadialMenu;
        m_exitAction.performed += OnExitPanel;
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
        if (m_isInitialized && Player != null && Player.IsDestroyed() == false)
        {
            if (m_radialMenuAction != null)
            {
                m_radialMenuAction.performed -= OnRadialMenu;
                m_radialMenuAction.canceled -= OnRadialMenu;
            }
            if (m_exitAction != null)
                m_exitAction.performed -= OnExitPanel;
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
            {
                var selectedState = UIState.None;
                var hasSelection = element is RadialMenu radialMenu &&
                                   radialMenu.TryGetSelectedState(out selectedState);
                element.gameObject.SetActive(false);

                if (hasSelection)
                {
                    ChangeState(selectedState);
                }
            }
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

    public void ShowRequiredWeaponMessage(WeaponType weaponType)
    {
        var weaponName = weaponType switch
        {
            WeaponType.Sword => "검",
            WeaponType.Knife => "단검",
            _ => "알맞은 무기",
        };

        ShowNotice($"{weaponName}을 장착해야 스킬을 사용할 수 있습니다.");
    }

    private void InitializeNotice()
    {
        if (m_noticeGroup != null) return;

        var noticeObject = new GameObject(
            "SkillNotice",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image));
        noticeObject.transform.SetParent(transform, false);
        noticeObject.transform.SetAsLastSibling();

        var noticeRect = noticeObject.GetComponent<RectTransform>();
        noticeRect.anchorMin = new Vector2(0.5f, 0.22f);
        noticeRect.anchorMax = new Vector2(0.5f, 0.22f);
        noticeRect.pivot = new Vector2(0.5f, 0.5f);
        noticeRect.anchoredPosition = Vector2.zero;
        noticeRect.sizeDelta = new Vector2(500f, 48f);

        var background = noticeObject.GetComponent<Image>();
        background.color = new Color(0.05f, 0.05f, 0.05f, 0.82f);
        background.raycastTarget = false;

        m_noticeGroup = noticeObject.GetComponent<CanvasGroup>();
        m_noticeGroup.alpha = 0f;
        m_noticeGroup.interactable = false;
        m_noticeGroup.blocksRaycasts = false;

        var textObject = new GameObject(
            "Text",
            typeof(RectTransform),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(noticeObject.transform, false);

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 4f);
        textRect.offsetMax = new Vector2(-16f, -4f);

        m_noticeText = textObject.GetComponent<TextMeshProUGUI>();
        m_noticeText.font = MoneyText != null ? MoneyText.font : KillText?.font;
        m_noticeText.alignment = TextAlignmentOptions.Center;
        m_noticeText.color = Color.white;
        m_noticeText.fontSize = 22f;
        m_noticeText.enableAutoSizing = true;
        m_noticeText.fontSizeMin = 14f;
        m_noticeText.fontSizeMax = 22f;
        m_noticeText.raycastTarget = false;
    }

    private void ShowNotice(string message)
    {
        InitializeNotice();
        if (m_noticeGroup == null || m_noticeText == null) return;

        m_noticeText.text = message;
        m_noticeGroup.transform.SetAsLastSibling();
        m_noticeSequence?.Kill();
        m_noticeGroup.alpha = 0f;

        m_noticeSequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(m_noticeGroup.DOFade(1f, 0.12f))
            .AppendInterval(1.25f)
            .Append(m_noticeGroup.DOFade(0f, 0.22f));
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
        if (UIElements == null) return;
        if (state != UIState.None && UIElements.ContainsKey(state) == false)
            state = UIState.None;

        m_currentState = state;
        foreach (var pair in UIElements)
        {
            var element = pair.Value;
            if (element == null || element.DependencyOnChangeState == false) continue;

            var shouldBeActive = state != UIState.None && pair.Key == state;
            if (element.gameObject.activeSelf != shouldBeActive)
                element.gameObject.SetActive(shouldBeActive);
        }

        Time.timeScale = state == UIState.None ? 1f : 0f;
        var inputMap = state switch
        {
            UIState.None => InputActionMap.Player,
            UIState.Dialog => InputActionMap.Dialog,
            _ => InputActionMap.UI,
        };
        Player?.SetActionMap(inputMap);
    }
}
