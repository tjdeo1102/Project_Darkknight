using UnityEngine;

public class LookTarget : MonoBehaviour
{
    public Transform Target;

    public float scaleFactor = 1f;
    public float minScale = 0.5f;
    public float maxScale = 1.5f;
    public bool AutoFindCamera;

    private void Start()
    {
        if (AutoFindCamera)
        {
            Target = Camera.main.transform;
        }

    }
    void LateUpdate()
    {
        if (Target == null) return;

        float dist = Vector3.Distance(transform.position, Target.position);
        float scale = Mathf.Clamp(dist * scaleFactor, minScale, maxScale);
        transform.localScale = Vector3.one * scale;
        transform.forward = Target.forward;
    }
}
