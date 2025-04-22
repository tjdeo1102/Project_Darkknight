using UnityEngine;

public enum StatType
{
    Health,Mana,AttackPower,Defense,Speed,RunSpeed
}

public class PlayerModel : MonoBehaviour
{
    public Stat<float> Health = new Stat<float>();
    public Stat<float> Mana = new Stat<float>();
    public Stat<float> AttackPower = new Stat<float>();
    public Stat<float> Defense = new Stat<float>();
    public Stat<float> Speed = new Stat<float>();
    public Stat<float> RunSpeed = new Stat<float>();

}
