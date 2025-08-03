using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using UnityEngine;

public class EnemyStat : MonoBehaviour
{
    [Header("Require Setting")]
    public EnemyController Ctrl;
    public List<StatBaseSO> StatData;
    public bool CanKnockBack = true;

    [Header("Stat")]
    public Stat Health = new();
    public Stat Mana = new();
    public Stat AttackPower = new();
    public Stat Defense = new();
    public Stat Speed = new();
    public Stat RunSpeed = new();
    public Stat LifeSteel = new();
    public Stat Money = new();

    public Dictionary<StatType, Stat> StatDic;

    private void Start()
    {
        StatDic = new Dictionary<StatType, Stat>()
        {
            { StatType.Health, Health },
            { StatType.Mana, Mana },
            { StatType.AttackPower, AttackPower },
            { StatType.Defense, Defense },
            { StatType.Speed, Speed},
            { StatType.RunSpeed, RunSpeed},
            { StatType.LifeSteel, LifeSteel},
            { StatType.Money, Money},
        };
        Ctrl.GameLoop.StageLevel.OnValueChanged += UpdateStat;
        UpdateStat(Ctrl.GameLoop.StageLevel.Value);
    }

    public void UpdateStat(int newLevel)
    {
        if (newLevel > StatData.Count) return;
        StatData[newLevel - 1].SetStat(ref StatDic);
    }

    public void ApplyDamage(float damage, Vector3 attackerPos, float force)
    {
        if (StatDic.TryGetValue(StatType.Health, out var value))
        {
            value.AddModifier(new StatModifier(-damage), StatModifyType.Damage);

            if (value.TotalValue < 0.001f)
            {
                DIe();
            }
            else if (CanKnockBack)
            {
                var dir = (transform.position - attackerPos).normalized;
                var rig = Ctrl.Rigid;
                var delay = force / (rig.linearDamping * 2f);
                Ctrl.AI.BTAgent.SetVariableValue("CurrentType", EnemyStateType.KnockBack);
                rig.AddForce(dir * force * rig.mass, ForceMode.Impulse);
            }
        }
    }

    public void DIe()
    {
        if (Ctrl.GameLoop == null || Ctrl.GameLoop.EnemySpawner== null || Ctrl.Rigid == null) return;

        Ctrl.GameLoop.Player.model.Stats[StatType.Money].AddModifier(new StatModifier(StatDic[StatType.Money].TotalValue), StatModifyType.KillEnemy);
        if (Ctrl.EnemyType == EnemyType.Boss_1 ||
            Ctrl.EnemyType == EnemyType.Boss_2 || 
            Ctrl.EnemyType == EnemyType.Boss_3)
           Ctrl.GameLoop.ClearBoss();

        else Ctrl.GameLoop.KillCount.Value++;

        StartCoroutine(DIeRoutine());
    }

    private IEnumerator DIeRoutine()
    {
        Ctrl.AI.BTAgent.SetVariableValue("CurrentType", EnemyStateType.Die);

        // 충돌 설정 정리
        Ctrl.Rigid.linearVelocity = Vector3.zero;
        Ctrl.Rigid.useGravity = false;
        var cols = Ctrl.GetComponentsInChildren<Collider>();
        foreach (var item in cols)
        {
            item.enabled = false;
        }
        yield return null;

        // 투명화해서 제거
        var renderer = Ctrl.GetComponentInChildren<SkinnedMeshRenderer>();
        var originMat = renderer.material;
        var mat = new Material(originMat);
        renderer.material = mat;
        var col = mat.color;

        yield return new WaitUntil(() =>
        {
            var anim = Ctrl.Anim;
            if (anim == null) return false;

            var stateInfo = anim.GetCurrentAnimatorStateInfo(0);

            return stateInfo.IsName("Die") && stateInfo.normalizedTime >= 1f;
        });

        var tween = DOTween.To(() => mat.color.a, x =>
            { col.a = x; mat.color = col;}, 0f, 1f);

        yield return tween.WaitForCompletion();

        // 스탯 정리
        foreach (var item in StatDic.Values)
        {
            item.RemoveAllModifier();
            yield return null;
        }

        // 이벤트 해제
        Ctrl.GameLoop.StageLevel.OnValueChanged -= UpdateStat;


        Ctrl.GameLoop.EnemySpawner.DestroyEnemy(Ctrl);
        // 초기화
        Ctrl.Rigid.useGravity = true;
        foreach (var item in cols)
        {
            item.enabled = true;
        }
        renderer.material = originMat;

        yield break;
    }

}
