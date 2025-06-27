using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RadialMenu : MonoBehaviour, IUIElements
{
    [Serializable]
    public struct MenuContent
    {
        public RectTransform Icon;
        public UnityEvent OnExecute;
    }

    public MenuContent[] contents;
    public float IconOvelayMagnitude;
    public TextMeshProUGUI Text;

    private Vector2 m_center;
    private int m_lastSelect;
    private Vector2 m_lastIconOriginScale;

    public UIController Controller { get; set; }

    void OnEnable()
    {
        m_lastSelect = -1;
        var pos = GetComponent<RectTransform>().position;
        // Recttransform에 맞게 스크린 위치 좌표
        m_center = RectTransformUtility.WorldToScreenPoint(null, pos);
    }

    void OnDisable()
    {
        contents[m_lastSelect].Icon.localScale = m_lastIconOriginScale;
        Text.text = "";
    }
    // Update is called once per frame
    void Update()
    {
        Vector2 mousePos = Input.mousePosition;
        var dir = mousePos - m_center;

        // 일정 거리 이내일 때는 버튼 동작 안함
        if (dir.sqrMagnitude < 100) return;

        // 45도 회전
        var deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 135f;
        if (deg < 0) deg += 360f;

        // 4개의 메뉴 (360f / 4)
        var select = Mathf.FloorToInt(deg / 90f);

        OverlayIcon(select);

        if (Input.GetMouseButtonDown(0) 
            && m_lastSelect < contents.Length
            && m_lastSelect > -1
            && contents != null)
        {
            contents[m_lastSelect].OnExecute?.Invoke();
            gameObject.SetActive(false);
        }
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
