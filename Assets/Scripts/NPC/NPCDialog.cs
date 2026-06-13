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

    private void OnEnable()
    {
        if (Controller == null) return;

        if (select != null && select.Count > 3)
        {
            select.RemoveRange(3, select.Count - 3);
        }
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
        base.OnDisable();
    }

    public void Init(NPCBase npc)
    {
        m_interactNPC = npc;
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
        SetChoicesActive(false);
    }

    public void ShowRoutes(
        string message,
        IReadOnlyList<NPCDialogueRoute> routes,
        IReadOnlyList<InventoryItem> shopItems)
    {
        if (mainTextMesh != null) mainTextMesh.text = message ?? string.Empty;
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
}
