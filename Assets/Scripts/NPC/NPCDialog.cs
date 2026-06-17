using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCDialog : UIElementBase
{
    [Serializable]
    public class ButtonContent
    {
        public Button btn;
        public TextMeshProUGUI textMesh;
        [HideInInspector] public InventoryItem selctItem;
    }

    [SerializeField] private TextMeshProUGUI mainTextMesh;
    [SerializeField] private List<ButtonContent> select;

    private NPCBase m_interactNPC;
    private Sequence m_closeSequence;
    private TextMeshProUGUI m_speakerText;
    private Button m_inventoryButton;
    private bool m_isStyled;

    private void OnEnable()
    {
        if (Controller == null) return;

        EnsureEasternFantasyPresentation();
        if (select != null && select.Count > 3)
        {
            select.RemoveRange(3, select.Count - 3);
        }
        if (m_interactNPC == null)
            SetChoicesActive(false);
    }

    protected override void OnDisable()
    {
        m_closeSequence?.Kill();
        if (select != null)
        {
            foreach (var item in select)
            {
                item?.btn?.onClick.RemoveAllListeners();
            }
        }
        m_interactNPC?.EndInteract();
        m_interactNPC = null;
        base.OnDisable();
    }

    public void Init(NPCBase npc)
    {
        m_interactNPC = npc;
        if (m_speakerText != null)
            m_speakerText.text = npc != null ? npc.GetDisplayName() : string.Empty;
        if (select == null) return;

        for (var index = 0; index < select.Count; index++)
        {
            var routeIndex = index;
            select[index].btn.onClick.RemoveAllListeners();
            select[index].btn.onClick.AddListener(
                () => m_interactNPC?.SelectRoute(routeIndex));
        }
    }

    public void ShowLine(string message)
    {
        if (mainTextMesh != null) mainTextMesh.text = message ?? string.Empty;
        if (m_inventoryButton != null)
            m_inventoryButton.gameObject.SetActive(false);
        SetChoicesActive(false);
    }

    public void ShowRoutes(
        string message,
        IReadOnlyList<NPCDialogueRoute> routes,
        IReadOnlyList<InventoryItem> shopItems)
    {
        if (mainTextMesh != null) mainTextMesh.text = message ?? string.Empty;
        if (m_inventoryButton != null)
            m_inventoryButton.gameObject.SetActive(true);
        if (select == null) return;

        for (var index = 0; index < select.Count; index++)
        {
            var hasRoute = routes != null &&
                           index < routes.Count &&
                           routes[index] != null;
            select[index].btn.gameObject.SetActive(hasRoute);
            if (hasRoute && select[index].textMesh != null)
            {
                select[index].textMesh.text =
                    NPCDialogueDatabase.FormatOption(routes[index], shopItems);
            }
        }
    }

    public void ShowCompletion(string message)
    {
        ShowLine(message);
        m_closeSequence?.Kill();
        m_closeSequence = DOTween.Sequence()
            .SetUpdate(true)
            .AppendInterval(2f)
            .OnComplete(() =>
            {
                m_interactNPC?.EndInteract();
                Controller.ChangeState(UIState.None);
            });
    }

    public void HideChoices()
    {
        SetChoicesActive(false);
    }

    private void SetChoicesActive(bool isActive)
    {
        if (select == null) return;
        foreach (var item in select)
        {
            if (item?.btn != null) item.btn.gameObject.SetActive(isActive);
        }
    }

    private void EnsureEasternFantasyPresentation()
    {
        if (m_isStyled) return;
        m_isStyled = true;

        var rootImage = GetComponent<Image>();
        EasternFantasyUI.StylePanel(rootImage, 0.66f);

        if (mainTextMesh != null)
        {
            mainTextMesh.color = EasternFantasyUI.Paper;
            mainTextMesh.fontSize = 25f;
            mainTextMesh.enableAutoSizing = true;
            mainTextMesh.fontSizeMin = 18f;
            mainTextMesh.fontSizeMax = 25f;
            mainTextMesh.characterSpacing = 0f;
        }

        m_speakerText = EasternFantasyUI.CreateLabel(
            transform,
            "SpeakerName",
            mainTextMesh);
        var speakerRect = m_speakerText.rectTransform;
        speakerRect.anchorMin = new Vector2(0.12f, 0.25f);
        speakerRect.anchorMax = new Vector2(0.42f, 0.25f);
        speakerRect.pivot = new Vector2(0f, 0f);
        speakerRect.anchoredPosition = new Vector2(0f, 4f);
        speakerRect.sizeDelta = new Vector2(0f, 38f);
        m_speakerText.alignment = TextAlignmentOptions.Left;
        m_speakerText.fontSize = 24f;
        m_speakerText.color = EasternFantasyUI.Gold;

        if (select != null)
        {
            foreach (var choice in select)
                EasternFantasyUI.StyleButton(choice?.btn);
        }

        CreateInventoryButton();
    }

    private void CreateInventoryButton()
    {
        var buttonObject = new GameObject(
            "InventoryButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(transform, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-38f, 28f);
        rect.sizeDelta = new Vector2(170f, 44f);

        m_inventoryButton = buttonObject.GetComponent<Button>();
        m_inventoryButton.targetGraphic = buttonObject.GetComponent<Image>();
        m_inventoryButton.onClick.AddListener(
            () => Controller?.OpenOverlay(UIState.Inventory, UIState.Dialog));
        EasternFantasyUI.StyleButton(m_inventoryButton);
        buttonObject.SetActive(false);

        var label = EasternFantasyUI.CreateLabel(
            buttonObject.transform,
            "Label",
            mainTextMesh);
        label.text = "인벤토리 확인";
        label.fontSize = 19f;
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);
    }
}
