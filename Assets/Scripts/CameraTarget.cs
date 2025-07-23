using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public Transform target;

    // Update is called once per frame
    void LateUpdate()
    {
        var y = transform.position.y;
        if (Mathf.Abs(target.position.y - y) > 1000f) y = target.position.y + 10f;
        transform.position = new Vector3(target.position.x, y, target.position.z);
    }
}
