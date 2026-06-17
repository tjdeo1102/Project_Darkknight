using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerModel : MonoBehaviour
{
    [Header("Require Setting")]
    public PlayerController Ctrl;
    public StatBaseSO InitStat;
    [Header("Hit Setting")]
    public bool CanHitEffect = true;
    public float HitEffectLength = 0.2f;
    [Header("Stats")]
    public Stat Health = new();
    public Stat Mana = new();
    public Stat AttackPower = new();
    public Stat Defense = new();
    public Stat Speed = new();
    public Stat RunSpeed = new();
    public Stat LifeSteel = new();
    public Stat Money = new();

    public Dictionary<StatType, Stat> Stats;

    private bool isPlayEffect = false;
    private bool m_isDead;

    private void OnEnable()
    {
        m_isDead = false;
    }

    private void Start()
    {
        Stats = new Dictionary<StatType, Stat>()
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
        InitStat.SetStat(ref Stats);
    }

    public void ApplyDamage(float damage, Vector3 attackerPos, float force)
    {
        if (m_isDead) return;

        if (Stats.TryGetValue(StatType.Health, out var value))
        {
            value.AddModifier(new StatModifier(-damage), StatModifyType.Damage);

            if (value.TotalValue < 0.0001f)
            {
                m_isDead = true;
                InGameLoop.Instance?.DiePlayer();
            }

            if (CanHitEffect)
            {
                Ctrl.Impulse.GenerateImpulse(force);
                if (isPlayEffect == false)
                {
                    isPlayEffect = true;

                    Sequence seq = DOTween.Sequence();

                    seq.Append(DOTween.To(() => Ctrl.hitScreenVolume.weight, x => Ctrl.hitScreenVolume.weight = x, 0.3f, HitEffectLength / 2))
                       .Append(DOTween.To(() => Ctrl.hitScreenVolume.weight, x => Ctrl.hitScreenVolume.weight = x, 0f, HitEffectLength / 2))
                       .OnComplete(() => isPlayEffect = false);

                    seq.Play();
                }

            }
        }
    }

}
