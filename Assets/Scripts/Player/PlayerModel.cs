using System.Collections.Generic;
using UnityEngine;


public class PlayerModel : MonoBehaviour
{
    public Stat Health = new Stat();
    public Stat Mana = new Stat();
    public Stat AttackPower = new Stat();
    public Stat Defense = new Stat();
    public Stat Speed = new Stat();
    public Stat RunSpeed = new Stat();
    public Stat LifeSteel = new Stat();

    public Dictionary<StatType, Stat> Stats;

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
        };
    }
}
