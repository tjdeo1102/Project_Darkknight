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
    private readonly List<GameObject> m_hudObjects = new();
    private UIState m_overlayReturnState = UIState.None;
    private Image m_menuBackdrop;
    public UIState CurrentState => m_currentState;
    public bool IsChangingState { get; private set; }
    public bool HasOverlayReturn => m_overlayReturnState != UIState.None;

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
        InitializePresentation();
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
        CloseCurrentPanel();
    }

    public void CloseCurrentPanel()
    {
        if (m_currentState == UIState.None || m_currentState == UIState.Die) return;
        if (m_overlayReturnState != UIState.None)
        {
            CloseOverlay();
            return;
        }

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

        m_overlayReturnState = UIState.None;
        IsChangingState = true;
        m_currentState = state;
        foreach (var pair in UIElements)
        {
            var element = pair.Value;
            if (element == null || element.DependencyOnChangeState == false) continue;

            var shouldBeActive = state != UIState.None && pair.Key == state;
            if (element.gameObject.activeSelf != shouldBeActive)
                element.gameObject.SetActive(shouldBeActive);
        }
        IsChangingState = false;

        Time.timeScale = state == UIState.None ? 1f : 0f;
        ApplyInputMap(state);
        UpdatePresentation(state);
        AnimateStateElement(state);
    }

    public void OpenOverlay(UIState overlayState, UIState returnState)
    {
        if (UIElements == null ||
            UIElements.TryGetValue(overlayState, out var overlay) == false ||
            UIElements.TryGetValue(returnState, out var returnElement) == false)
        {
            return;
        }

        IsChangingState = true;
        m_overlayReturnState = returnState;
        m_currentState = overlayState;
        returnElement.gameObject.SetActive(true);
        overlay.gameObject.SetActive(true);
        overlay.transform.SetAsLastSibling();
        IsChangingState = false;

        Time.timeScale = 0f;
        ApplyInputMap(overlayState);
        UpdatePresentation(overlayState);
        AnimateStateElement(overlayState);
    }

    public void CloseOverlay()
    {
        if (m_overlayReturnState == UIState.None || UIElements == null) return;

        var returnState = m_overlayReturnState;
        IsChangingState = true;
        if (UIElements.TryGetValue(m_currentState, out var overlay))
            overlay.gameObject.SetActive(false);
        if (UIElements.TryGetValue(returnState, out var returnElement))
        {
            returnElement.gameObject.SetActive(true);
            returnElement.transform.SetAsLastSibling();
            RestoreElementInteraction(returnElement);
        }
        m_currentState = returnState;
        m_overlayReturnState = UIState.None;
        IsChangingState = false;

        Time.timeScale = 0f;
        ApplyInputMap(returnState);
        UpdatePresentation(returnState);
        AnimateStateElement(returnState);
    }

    private void ApplyInputMap(UIState state)
    {
        if (Player == null) return;

        var inputMap = state switch
        {
            UIState.None => InputActionMap.Player,
            UIState.Dialog => InputActionMap.Dialog,
            _ => InputActionMap.UI,
        };

        Player.SetActionMap(inputMap);
        SetUiActionMapEnabled(state == UIState.Dialog);
    }

    private void SetUiActionMapEnabled(bool isEnabled)
    {
        var uiMap = Player?.input?.actions?.FindActionMap(InputActionMap.UI.ToString(), false);
        if (uiMap == null) return;

        if (isEnabled)
            uiMap.Enable();
        else if (Player.input.currentActionMap != uiMap)
            uiMap.Disable();
    }

    private static void RestoreElementInteraction(UIElementBase element)
    {
        if (element == null) return;

        var group = element.GetComponent<CanvasGroup>();
        if (group == null) return;

        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void InitializePresentation()
    {
        CacheHudObject("BasePanel");
        CacheHudObject("Minimap");
        CacheHudObject("SkillSlots");
        CacheHudObject("KillBox");
        CacheHudObject("Money");

        var backdropObject = new GameObject(
            "MenuBackdrop",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        backdropObject.transform.SetParent(transform, false);
        backdropObject.transform.SetAsFirstSibling();

        var rect = backdropObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        m_menuBackdrop = backdropObject.GetComponent<Image>();
        m_menuBackdrop.color = new Color(0.018f, 0.028f, 0.024f, 0.88f);
        m_menuBackdrop.raycastTarget = true;
        backdropObject.SetActive(false);

        StylePlayerBars();
        StyleMenuElement(UIState.Inventory);
        StyleMenuElement(UIState.SkillTree);
    }

    private void CacheHudObject(string objectName)
    {
        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            if (child == transform || child.name != objectName) continue;
            if (child.GetComponentInParent<UIElementBase>(true) != null) continue;
            if (m_hudObjects.Contains(child.gameObject) == false)
                m_hudObjects.Add(child.gameObject);
        }
    }

    private void UpdatePresentation(UIState state)
    {
        var showHud = state == UIState.None || state == UIState.RadialMenu;
        foreach (var hudObject in m_hudObjects)
        {
            if (hudObject != null)
                hudObject.SetActive(showHud);
        }

        if (m_menuBackdrop == null) return;

        var showBackdrop = state == UIState.Inventory ||
                           state == UIState.SkillTree ||
                           state == UIState.Dialog ||
                           state == UIState.Setting;
        m_menuBackdrop.gameObject.SetActive(showBackdrop);
        if (showBackdrop)
        {
            m_menuBackdrop.color = state == UIState.Dialog
                ? new Color(0.012f, 0.018f, 0.016f, 0.64f)
                : new Color(0.018f, 0.028f, 0.024f, 0.88f);
            m_menuBackdrop.transform.SetAsFirstSibling();
        }
    }

    private void StylePlayerBars()
    {
        StyleSlider(HPSlider, EasternFantasyUI.Vermilion);
        StyleSlider(MPSlider, EasternFantasyUI.Jade);
    }

    private static void StyleSlider(Slider slider, Color fillColor)
    {
        if (slider == null) return;

        if (slider.fillRect != null)
        {
            var fill = slider.fillRect.GetComponent<Image>();
            if (fill == null)
                fill = slider.fillRect.GetComponentInChildren<Image>(true);
            if (fill != null) fill.color = fillColor;
        }

        var background = slider.GetComponentInChildren<Image>(true);
        if (background != null)
            EasternFantasyUI.StylePanel(background, 0.88f);
    }

    private void StyleMenuElement(UIState state)
    {
        if (UIElements.TryGetValue(state, out var element) == false || element == null)
            return;

        var panel = element.GetComponent<Image>();
        if (panel == null)
            panel = element.GetComponentInChildren<Image>(true);
        EasternFantasyUI.StylePanel(panel, 0.96f);

        foreach (var button in element.GetComponentsInChildren<Button>(true))
            EasternFantasyUI.StyleButton(button);
    }

    private void AnimateStateElement(UIState state)
    {
        if (state == UIState.None ||
            UIElements.TryGetValue(state, out var element) == false ||
            element == null)
        {
            return;
        }

        var group = element.GetComponent<CanvasGroup>();
        if (group == null)
            group = element.gameObject.AddComponent<CanvasGroup>();
        if (group == null) return;

        group.DOKill();
        group.alpha = 0f;
        group.DOFade(1f, 0.18f).SetUpdate(true);
    }
}
