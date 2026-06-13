using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DialogueChoiceHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalTextColor = new(0.94f, 0.94f, 0.94f, 1f);
    [SerializeField] private Color highlightedTextColor = new(0.08f, 0.08f, 0.09f, 1f);

    private bool m_isHovered;
    private bool m_isSelected;

    private void OnEnable()
    {
        Refresh();
    }

    private void OnDisable()
    {
        m_isHovered = false;
        m_isSelected = false;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_isHovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_isHovered = false;
        Refresh();
    }

    public void OnSelect(BaseEventData eventData)
    {
        m_isSelected = true;
        Refresh();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        m_isSelected = false;
        Refresh();
    }

    private void Refresh()
    {
        if (label != null)
        {
            label.color = m_isHovered || m_isSelected
                ? highlightedTextColor
                : normalTextColor;
        }
    }
}
