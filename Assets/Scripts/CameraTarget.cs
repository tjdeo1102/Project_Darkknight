using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public Transform target;

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = new Vector3(target.position.x, transform.position.y, target.position.z);
    }
}
