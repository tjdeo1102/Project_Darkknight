using UnityEngine.Events;
using UnityEngine;

[System.Serializable]
public class Stat
{
    [SerializeField] private float value;
    public UnityEvent<float> OnChange = new UnityEvent<float>();

    public float Value
    {
        get => value;
        set
        {
            this.value = value;
            if (value < 0f) this.value = 0f;

            OnChange?.Invoke(value);
        }
    }
}