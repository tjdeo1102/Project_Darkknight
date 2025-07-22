using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStateUI : MonoBehaviour
{
    public EnemyController Ctrl;
    public Image HPBar;

    private void OnEnable()
    {
        StartCoroutine(InitRoutine());
    }
    private void OnDisable()
    {
        if (Ctrl.Model.StatDic != null)
            Ctrl.Model.StatDic[StatType.Health].OnChangeStat -= UpdateHPBar;
    }

    IEnumerator InitRoutine()
    {
        yield return new WaitForSeconds(1f);
        Ctrl.Model.StatDic[StatType.Health].OnChangeStat += UpdateHPBar;
        UpdateHPBar();
    }

    private void UpdateHPBar()
    {
        var stat = Ctrl.Model.StatDic[StatType.Health];
        HPBar.fillAmount = stat.TotalValue / stat.maxValue;
    }
}
