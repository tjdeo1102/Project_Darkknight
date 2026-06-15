using System.Collections.Generic;
using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public Transform target;

    [Header("Minimap Fog of War")]
    [SerializeField, Min(1f)] private float revealRadius = 12f;
    [SerializeField, Min(0.5f)] private float explorationCellSize = 3f;
    [SerializeField, Min(0.05f)] private float updateInterval = 0.1f;
    [SerializeField, Min(0.1f)] private float markerRefreshInterval = 0.5f;
    [SerializeField] private Color unexploredColor = Color.black;
    [SerializeField] private Color exploredColor = new Color(0.18f, 0.2f, 0.22f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly HashSet<Vector2Int> m_exploredCells = new();
    private readonly List<Renderer> m_minimapRenderers = new();
    private readonly MaterialPropertyBlock m_exploredProperties = new();
    private int m_minimapLayer = -1;
    private float m_nextUpdateTime;
    private float m_nextMarkerRefreshTime;

    private void Awake()
    {
        m_minimapLayer = LayerMask.NameToLayer("MinimapMark");
        m_exploredProperties.SetColor(BaseColorId, exploredColor);
        m_exploredProperties.SetColor(ColorId, exploredColor);

        if (TryGetComponent(out Camera minimapCamera))
            minimapCamera.backgroundColor = unexploredColor;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        var y = transform.position.y;
        if (Mathf.Abs(target.position.y - y) > 1000f) y = target.position.y + 10f;
        transform.position = new Vector3(target.position.x, y, target.position.z);

        if (m_minimapLayer < 0 || Time.unscaledTime < m_nextUpdateTime)
            return;

        m_nextUpdateTime = Time.unscaledTime + updateInterval;
        RevealAround(target.position);

        if (Time.unscaledTime >= m_nextMarkerRefreshTime)
        {
            m_nextMarkerRefreshTime = Time.unscaledTime + markerRefreshInterval;
            RefreshMinimapRenderers();
        }

        UpdateMarkerVisibility();
    }

    private void RevealAround(Vector3 center)
    {
        var centerCell = WorldToCell(center);
        var cellRadius = Mathf.CeilToInt(revealRadius / explorationCellSize);
        var revealRadiusSquared = revealRadius * revealRadius;

        for (var y = -cellRadius; y <= cellRadius; y++)
        {
            for (var x = -cellRadius; x <= cellRadius; x++)
            {
                var cell = centerCell + new Vector2Int(x, y);
                var cellCenter = CellToWorld(cell, center.y);
                var delta = cellCenter - center;
                delta.y = 0f;

                if (delta.sqrMagnitude <= revealRadiusSquared)
                    m_exploredCells.Add(cell);
            }
        }
    }

    private void RefreshMinimapRenderers()
    {
        m_minimapRenderers.Clear();
        var renderers = FindObjectsByType<Renderer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (var candidate in renderers)
        {
            if (candidate != null && candidate.gameObject.layer == m_minimapLayer)
                m_minimapRenderers.Add(candidate);
        }
    }

    private void UpdateMarkerVisibility()
    {
        var center = target.position;
        var revealRadiusSquared = revealRadius * revealRadius;

        for (var i = m_minimapRenderers.Count - 1; i >= 0; i--)
        {
            var marker = m_minimapRenderers[i];
            if (marker == null)
            {
                m_minimapRenderers.RemoveAt(i);
                continue;
            }

            var markerPosition = marker.transform.position;
            var delta = markerPosition - center;
            delta.y = 0f;
            var isCurrentlyVisible = delta.sqrMagnitude <= revealRadiusSquared;

            if (IsPlayerMarker(marker.transform))
            {
                SetVisible(marker);
                continue;
            }

            if (IsEntityMarker(marker.transform))
            {
                marker.enabled = isCurrentlyVisible;
                if (isCurrentlyVisible)
                    marker.SetPropertyBlock(null);
                continue;
            }

            if (isCurrentlyVisible)
            {
                SetVisible(marker);
            }
            else if (m_exploredCells.Contains(WorldToCell(markerPosition)))
            {
                marker.enabled = true;
                marker.SetPropertyBlock(m_exploredProperties);
            }
            else
            {
                marker.enabled = false;
            }
        }
    }

    private bool IsPlayerMarker(Transform marker)
    {
        return marker == target || marker.IsChildOf(target);
    }

    private static bool IsEntityMarker(Transform marker)
    {
        var current = marker;
        while (current != null)
        {
            if (current.CompareTag("Enemy"))
                return true;

            current = current.parent;
        }

        return false;
    }

    private static void SetVisible(Renderer marker)
    {
        marker.enabled = true;
        marker.SetPropertyBlock(null);
    }

    private Vector2Int WorldToCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / explorationCellSize),
            Mathf.FloorToInt(position.z / explorationCellSize));
    }

    private Vector3 CellToWorld(Vector2Int cell, float y)
    {
        return new Vector3(
            (cell.x + 0.5f) * explorationCellSize,
            y,
            (cell.y + 0.5f) * explorationCellSize);
    }

    private void OnDisable()
    {
        foreach (var marker in m_minimapRenderers)
        {
            if (marker != null)
                SetVisible(marker);
        }
    }
}
