using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    public Stat<float> Health = new Stat<float>();
    public Stat<float> Mana = new Stat<float>();
    public Stat<float> AttackPower = new Stat<float>();
    public Stat<float> Defense = new Stat<float>();
    public Stat<float> Speed = new Stat<float>();
    public Stat<float> RunSpeed = new Stat<float>();

}
