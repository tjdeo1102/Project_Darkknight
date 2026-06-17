using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public Transform target;

    [Header("Minimap Fog of War")]
    [SerializeField, Min(1f)] private float revealRadius = 12f;
    [SerializeField, Min(0.5f)] private float explorationCellSize = 3f;
    [SerializeField, Min(0.05f)] private float updateInterval = 0.1f;
    [SerializeField] private Color unexploredColor = Color.black;
    [SerializeField] private Color exploredColor = new Color(0.18f, 0.2f, 0.22f, 1f);

    private float m_nextUpdateTime;
    private MinimapFogOfWar m_fogOfWar;

    private void Awake()
    {
        if (TryGetComponent(out Camera minimapCamera))
            minimapCamera.backgroundColor = unexploredColor;

        m_fogOfWar = GetComponent<MinimapFogOfWar>();
        if (m_fogOfWar == null)
            m_fogOfWar = gameObject.AddComponent<MinimapFogOfWar>();

        m_fogOfWar.Configure(revealRadius, explorationCellSize, exploredColor);
    }

    private void Start()
    {
        if (target != null)
            m_fogOfWar.RegisterDynamicMarkerRoot(target, MinimapFogOfWar.DynamicMarkerKind.Player);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        var y = transform.position.y;
        if (Mathf.Abs(target.position.y - y) > 1000f) y = target.position.y + 10f;
        transform.position = new Vector3(target.position.x, y, target.position.z);

        if (Time.unscaledTime < m_nextUpdateTime)
            return;

        m_nextUpdateTime = Time.unscaledTime + updateInterval;
        m_fogOfWar.RevealAround(target.position);
    }
}
