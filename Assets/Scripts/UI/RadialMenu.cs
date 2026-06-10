using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RadialMenu : UIElementBase
{
    [Serializable]
    public struct MenuContent
    {
        public RectTransform Icon;
        public UIState ChangeState;
    }

    public MenuContent[] contents;
    public float IconOvelayMagnitude;
    [Min(0f)] public float SelectionDistance = 100f;
    public TextMeshProUGUI Text;

    private Vector2 m_center;
    private int m_lastSelect;
    private Vector2 m_lastIconOriginScale;

    void OnEnable()
    {
        m_lastSelect = -1;
        if (Text != null) Text.text = "";
        var pos = GetComponent<RectTransform>().position;
        // Recttransform에 맞게 스크린 위치 좌표
        m_center = RectTransformUtility.WorldToScreenPoint(null, pos);
    }

    protected override void OnDisable()
    {
        ClearSelection();
        base.OnDisable();
    }
    // Update is called once per frame
    void Update()
    {
        if (contents == null || contents.Length == 0 || Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        var dir = mousePos - m_center;

        // 일정 거리 이내일 때는 버튼 동작 안함
        if (dir.sqrMagnitude < SelectionDistance * SelectionDistance)
        {
            ClearSelection();
            return;
        }

        // 45도 회전
        var deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 135f;
        if (deg < 0) deg += 360f;

        // 4개의 메뉴 (360f / 4)
        var select = Mathf.FloorToInt(deg / 90f);

        OverlayIcon(select);

    }


    public bool TryGetSelectedState(out UIState state)
    {
        if (contents != null && m_lastSelect >= 0 && m_lastSelect < contents.Length)
        {
            state = contents[m_lastSelect].ChangeState;
            return true;
        }

        state = UIState.None;
        return false;
    }

    private void ClearSelection()
    {
        if (contents != null && m_lastSelect >= 0 && m_lastSelect < contents.Length)
        {
            var icon = contents[m_lastSelect].Icon;
            if (icon != null) icon.localScale = m_lastIconOriginScale;
        }

        m_lastSelect = -1;
        if (Text != null) Text.text = "";
    }
    void OverlayIcon(int idx)
    {
        if (contents.Length <= idx || m_lastSelect == idx) return;
        if (contents[idx].Icon == null) return;

        if (m_lastSelect > -1) contents[m_lastSelect].Icon.localScale = m_lastIconOriginScale;
        m_lastSelect = idx;
        m_lastIconOriginScale = contents[idx].Icon.localScale;
        contents[idx].Icon.localScale *= IconOvelayMagnitude;
        Text.text = contents[idx].Icon.name;
    }
}
