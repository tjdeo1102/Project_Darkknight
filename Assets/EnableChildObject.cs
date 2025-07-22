using UnityEngine;

public class EnableChildObject : MonoBehaviour
{
    public GameObject[] childs;

    private void OnEnable()
    {
        foreach (var child in childs)
        {
            child.SetActive(true);
        }
    }
}
