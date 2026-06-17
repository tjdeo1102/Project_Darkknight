using TMPro;
using UnityEngine;

public class NPCNameplate : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    private Transform m_camera;

    public void Initialize(NPCBase owner)
    {
        if (label != null)
            label.text = owner != null ? owner.GetDisplayName() : string.Empty;
    }

    private void LateUpdate()
    {
        if (m_camera == null && Camera.main != null)
            m_camera = Camera.main.transform;
        if (m_camera == null) return;

        transform.rotation = Quaternion.LookRotation(
            transform.position - m_camera.position,
            Vector3.up);
    }
}
