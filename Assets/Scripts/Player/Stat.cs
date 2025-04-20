using UnityEngine.Events;
using UnityEngine;

[System.Serializable]
public class Stat<T>
{
    [SerializeField] private T value;
    public UnityEvent<T> OnChange = new UnityEvent<T>();

    public T Value
    {
        get => value;
        set
        {
            this.value = value;
            OnChange?.Invoke(value);
        }
    }
}