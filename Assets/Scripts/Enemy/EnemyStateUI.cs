using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStateUI : MonoBehaviour
{
    public EnemyController Ctrl;
    public Image HPBar;
    [SerializeField] private bool showAtFullHealth;
    private bool m_isStyled;
    private CanvasGroup m_canvasGroup;
    private Transform m_camera;
    private LookTarget m_lookTarget;
    private Vector3 m_baseScale;
    private bool m_hasBaseScale;

    private void OnEnable()
    {
        ApplyEasternFantasyStyle();
        StartCoroutine(InitRoutine());
    }
    private void OnDisable()
    {
        if (Ctrl != null && Ctrl.Model != null && Ctrl.Model.StatDic != null)
            Ctrl.Model.StatDic[StatType.Health].OnChangeStat -= UpdateHPBar;
    }

    private void LateUpdate()
    {
        FaceCamera();
    }

    IEnumerator InitRoutine()
    {
        yield return new WaitForSeconds(1f);
        if (Ctrl == null || Ctrl.Model == null || Ctrl.Model.StatDic == null)
            yield break;
        Ctrl.Model.StatDic[StatType.Health].OnChangeStat += UpdateHPBar;
        UpdateHPBar();
    }

    private void UpdateHPBar()
    {
        if (Ctrl == null || Ctrl.Model == null || HPBar == null) return;

        var stat = Ctrl.Model.StatDic[StatType.Health];
        var fillAmount = stat.maxValue > 0f
            ? Mathf.Clamp01(stat.TotalValue / stat.maxValue)
            : 0f;

        HPBar.fillAmount = fillAmount;
        SetVisible(showAtFullHealth || fillAmount < 0.999f);
    }

    private void ApplyEasternFantasyStyle()
    {
        if (m_isStyled) return;
        m_isStyled = true;

        m_canvasGroup = GetComponent<CanvasGroup>();
        if (m_canvasGroup == null)
            m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
        SetVisible(false);

        m_lookTarget = GetComponent<LookTarget>();
        if (m_lookTarget != null)
            m_lookTarget.enabled = false;
        NormalizeScale();

        if (HPBar == null) return;

        HPBar.color = EasternFantasyUI.Vermilion;
        if (HPBar.transform.parent != null)
        {
            var background = HPBar.transform.parent.GetComponent<Image>();
            if (background != null)
                background.color = new Color(0.025f, 0.035f, 0.032f, 0.88f);
        }
    }

    private void SetVisible(bool isVisible)
    {
        if (m_canvasGroup == null) return;

        m_canvasGroup.alpha = isVisible ? 1f : 0f;
        m_canvasGroup.interactable = false;
        m_canvasGroup.blocksRaycasts = false;
    }

    private void NormalizeScale()
    {
        if (m_hasBaseScale == false)
        {
            var uniformScale = Mathf.Max(
                Mathf.Abs(transform.localScale.x),
                Mathf.Abs(transform.localScale.y));
            if (uniformScale <= Mathf.Epsilon)
                uniformScale = 1f;

            m_baseScale = Vector3.one * uniformScale;
            m_hasBaseScale = true;
        }

        transform.localScale = m_baseScale;
    }

    private void FaceCamera()
    {
        NormalizeScale();
        if (m_camera == null && Camera.main != null)
            m_camera = Camera.main.transform;
        if (m_camera == null) return;

        transform.rotation = Quaternion.LookRotation(
            transform.position - m_camera.position,
            Vector3.up);
    }
}
