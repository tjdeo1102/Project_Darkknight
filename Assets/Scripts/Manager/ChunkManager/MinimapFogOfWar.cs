using System.Collections.Generic;
using UnityEngine;

public class MinimapFogOfWar : MonoBehaviour
{
    public enum DynamicMarkerKind
    {
        Player,
        Entity
    }

    private sealed class MapMarker
    {
        public Renderer Renderer;
        public bool IsOutline;
        public bool LastEnabled;
        public bool LastRestTint;

        public MapMarker(Renderer renderer, bool isOutline)
        {
            Renderer = renderer;
            IsOutline = isOutline;
            LastEnabled = renderer != null && renderer.enabled;
        }
    }

    private sealed class FogOverlay
    {
        public Renderer Renderer;
        public bool LastEnabled;

        public FogOverlay(Renderer renderer)
        {
            Renderer = renderer;
            LastEnabled = renderer != null && renderer.enabled;
        }
    }

    private sealed class FogCell
    {
        public readonly List<MapMarker> BaseMarkers = new();
        public readonly List<FogOverlay> FogOverlays = new();
        public bool Explored;
        public bool CurrentVisible;
        public bool RestArea;
    }

    private sealed class ChunkMarkerRegistration
    {
        public readonly List<(Vector2Int Cell, MapMarker Marker)> Markers = new();
        public readonly List<(Vector2Int Cell, FogOverlay Overlay)> Overlays = new();
    }

    private sealed class DynamicMarker
    {
        public Renderer Renderer;
        public Transform Transform;
        public DynamicMarkerKind Kind;
        public bool LastEnabled;

        public DynamicMarker(Renderer renderer, DynamicMarkerKind kind)
        {
            Renderer = renderer;
            Transform = renderer != null ? renderer.transform : null;
            Kind = kind;
            LastEnabled = renderer != null && renderer.enabled;
        }
    }

    public static MinimapFogOfWar Instance { get; private set; }

    [SerializeField, Min(1f)] private float revealRadius = 12f;
    [SerializeField, Min(0.5f)] private float cellSize = 3f;
    [SerializeField] private Color fogColor = Color.black;
    [SerializeField] private Color restAreaOutlineColor = new(1f, 0.82f, 0.1f, 1f);
    [SerializeField, Min(0f)] private float fogOverlayHeight = 2f;
    [SerializeField, Min(0.1f)] private float fogOverlayScale = 1.08f;

    private readonly Dictionary<Vector2Int, FogCell> m_cells = new();
    private readonly Dictionary<Chunk, ChunkMarkerRegistration> m_chunkMarkers = new();
    private readonly List<DynamicMarker> m_dynamicMarkers = new();
    private readonly HashSet<Vector2Int> m_currentVisibleCells = new();
    private readonly HashSet<Vector2Int> m_nextVisibleCells = new();
    private readonly List<Vector2Int> m_dirtyCells = new();
    private readonly HashSet<Renderer> m_registeredDynamicRenderers = new();
    private readonly HashSet<Renderer> m_registeredMapRenderers = new();
    private Material m_fogMaterial;
    private Mesh m_fogMesh;
    private int m_minimapLayer = -1;
    private MaterialPropertyBlock m_restAreaOutlineProperties;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            return;
        }

        Instance = this;
        EnsureFogResources();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Configure(float radius, float explorationCellSize, Color exploredTint)
    {
        revealRadius = Mathf.Max(1f, radius);
        cellSize = Mathf.Max(0.5f, explorationCellSize);
        EnsureFogResources();
    }

    public void RegisterChunk(Chunk chunk)
    {
        if (chunk?.ChunkObject == null) return;

        UnregisterChunk(chunk);

        var registration = new ChunkMarkerRegistration();
        var renderers = chunk.ChunkObject.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null || renderer.gameObject.layer != MinimapLayer) continue;
            if (IsBaseMapMarker(renderer) == false) continue;

            var cell = WorldToCell(renderer.transform.position);
            var marker = new MapMarker(renderer, IsOutlineMapMarker(renderer));
            registration.Markers.Add((cell, marker));
            m_registeredMapRenderers.Add(renderer);

            if (m_cells.TryGetValue(cell, out var fogCell) == false)
            {
                fogCell = new FogCell();
                m_cells[cell] = fogCell;
            }

            fogCell.RestArea = chunk.IsWorldPositionInRestAreaRoom(renderer.transform.position);
            fogCell.BaseMarkers.Add(marker);
        }

        var createdOverlayCells = new HashSet<Vector2Int>();
        foreach (var entry in registration.Markers)
        {
            if (createdOverlayCells.Add(entry.Cell) == false) continue;
            if (m_cells.TryGetValue(entry.Cell, out var fogCell) == false) continue;

            var overlay = CreateFogOverlay(chunk, entry.Cell, entry.Marker.Renderer.transform.position);
            if (overlay == null) continue;

            registration.Overlays.Add((entry.Cell, overlay));
            fogCell.FogOverlays.Add(overlay);
            ApplyCellState(entry.Cell, fogCell);
        }

        m_chunkMarkers[chunk] = registration;
    }

    public void UnregisterChunk(Chunk chunk)
    {
        if (chunk == null || m_chunkMarkers.TryGetValue(chunk, out var registration) == false)
            return;

        foreach (var entry in registration.Markers)
        {
            m_registeredMapRenderers.Remove(entry.Marker.Renderer);
            if (m_cells.TryGetValue(entry.Cell, out var cell) == false) continue;

            cell.BaseMarkers.Remove(entry.Marker);
            if (cell.BaseMarkers.Count == 0 && cell.FogOverlays.Count == 0 && cell.Explored == false && cell.CurrentVisible == false)
            {
                m_cells.Remove(entry.Cell);
            }
        }

        foreach (var entry in registration.Overlays)
        {
            if (m_cells.TryGetValue(entry.Cell, out var cell))
            {
                cell.FogOverlays.Remove(entry.Overlay);
                if (cell.BaseMarkers.Count == 0 && cell.FogOverlays.Count == 0 && cell.Explored == false && cell.CurrentVisible == false)
                    m_cells.Remove(entry.Cell);
            }

            if (entry.Overlay?.Renderer != null)
                Destroy(entry.Overlay.Renderer.gameObject);
        }

        m_chunkMarkers.Remove(chunk);
    }

    public void RegisterDynamicMarkerRoot(Transform root, DynamicMarkerKind kind)
    {
        if (root == null) return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null || renderer.gameObject.layer != MinimapLayer) continue;
            if (m_registeredMapRenderers.Contains(renderer)) continue;
            if (m_registeredDynamicRenderers.Add(renderer) == false) continue;

            var marker = new DynamicMarker(renderer, kind);
            m_dynamicMarkers.Add(marker);
            ApplyDynamicMarkerState(marker);
        }
    }

    public void UnregisterDynamicMarkerRoot(Transform root)
    {
        if (root == null) return;

        for (var i = m_dynamicMarkers.Count - 1; i >= 0; i--)
        {
            var marker = m_dynamicMarkers[i];
            if (marker == null || marker.Renderer == null || marker.Transform == null)
            {
                m_dynamicMarkers.RemoveAt(i);
                continue;
            }

            if (marker.Transform == root || marker.Transform.IsChildOf(root))
            {
                m_registeredDynamicRenderers.Remove(marker.Renderer);
                marker.Renderer.enabled = true;
                m_dynamicMarkers.RemoveAt(i);
            }
        }
    }

    public void RevealAround(Vector3 center)
    {
        RevealArea(center, revealRadius, true);
    }

    public void RevealArea(Vector3 center, float radius, bool markAsCurrent)
    {
        if (cellSize <= 0f) return;

        if (markAsCurrent)
        {
            foreach (var cell in m_currentVisibleCells)
            {
                if (m_cells.TryGetValue(cell, out var fogCell))
                {
                    fogCell.CurrentVisible = false;
                    m_dirtyCells.Add(cell);
                }
            }

            m_currentVisibleCells.Clear();
            m_nextVisibleCells.Clear();
        }

        var centerCell = WorldToCell(center);
        var cellRadius = Mathf.CeilToInt(radius / cellSize);
        var radiusSquared = radius * radius;

        for (var y = -cellRadius; y <= cellRadius; y++)
        {
            for (var x = -cellRadius; x <= cellRadius; x++)
            {
                var cell = centerCell + new Vector2Int(x, y);
                var cellCenter = CellToWorld(cell, center.y);
                var delta = cellCenter - center;
                delta.y = 0f;
                if (delta.sqrMagnitude > radiusSquared) continue;

                if (m_cells.TryGetValue(cell, out var fogCell) == false)
                {
                    fogCell = new FogCell();
                    m_cells[cell] = fogCell;
                }

                fogCell.Explored = true;
                if (markAsCurrent)
                {
                    fogCell.CurrentVisible = true;
                    m_nextVisibleCells.Add(cell);
                }

                m_dirtyCells.Add(cell);
            }
        }

        if (markAsCurrent)
        {
            foreach (var cell in m_nextVisibleCells)
                m_currentVisibleCells.Add(cell);
        }

        ApplyDirtyCells();
        UpdateDynamicMarkers();
    }

    private void ApplyDirtyCells()
    {
        for (var i = 0; i < m_dirtyCells.Count; i++)
        {
            var cellKey = m_dirtyCells[i];
            if (m_cells.TryGetValue(cellKey, out var cell))
                ApplyCellState(cellKey, cell);
        }

        m_dirtyCells.Clear();
    }

    private void ApplyCellState(Vector2Int cellKey, FogCell cell)
    {
        if (cell == null) return;

        foreach (var marker in cell.BaseMarkers)
        {
            if (marker?.Renderer == null) continue;

            SetMarkerEnabled(marker, true);
            SetMarkerRestTint(marker, marker.IsOutline && cell.RestArea);
        }

        foreach (var overlay in cell.FogOverlays)
        {
            if (overlay?.Renderer == null) continue;

            SetOverlayEnabled(overlay, cell.Explored == false);
        }
    }

    private void UpdateDynamicMarkers()
    {
        for (var i = m_dynamicMarkers.Count - 1; i >= 0; i--)
        {
            var marker = m_dynamicMarkers[i];
            if (marker == null || marker.Renderer == null || marker.Transform == null)
            {
                m_dynamicMarkers.RemoveAt(i);
                continue;
            }

            ApplyDynamicMarkerState(marker);
        }
    }

    private void ApplyDynamicMarkerState(DynamicMarker marker)
    {
        var visible = marker.Kind == DynamicMarkerKind.Player ||
                      IsCurrentVisible(WorldToCell(marker.Transform.position));

        if (marker.LastEnabled == visible && marker.Renderer.enabled == visible)
            return;

        marker.Renderer.enabled = visible;
        marker.LastEnabled = visible;
    }

    private bool IsCurrentVisible(Vector2Int cell)
    {
        return m_cells.TryGetValue(cell, out var fogCell) && fogCell.CurrentVisible;
    }

    private static bool IsBaseMapMarker(Renderer renderer)
    {
        var name = renderer.gameObject.name;
        return name.Contains("WhiteMark") || name.Contains("BlackMark") || name.Contains("GrayMark");
    }

    private static bool IsOutlineMapMarker(Renderer renderer)
    {
        return renderer.gameObject.name.Contains("WhiteMark");
    }

    private void SetMarkerEnabled(MapMarker marker, bool enabled)
    {
        if (marker.LastEnabled == enabled && marker.Renderer.enabled == enabled)
            return;

        marker.Renderer.enabled = enabled;
        marker.LastEnabled = enabled;
    }

    private void SetOverlayEnabled(FogOverlay overlay, bool enabled)
    {
        if (overlay.LastEnabled == enabled && overlay.Renderer.enabled == enabled)
            return;

        overlay.Renderer.enabled = enabled;
        overlay.LastEnabled = enabled;
    }

    private void SetMarkerRestTint(MapMarker marker, bool enabled)
    {
        if (marker.LastRestTint == enabled)
            return;

        EnsureFogResources();
        marker.Renderer.SetPropertyBlock(enabled ? m_restAreaOutlineProperties : null);
        marker.LastRestTint = enabled;
    }

    private FogOverlay CreateFogOverlay(Chunk chunk, Vector2Int cell, Vector3 samplePosition)
    {
        if (chunk?.ChunkObject == null) return null;

        EnsureFogResources();

        var overlayObject = new GameObject($"MinimapFog_{cell.x}_{cell.y}");
        overlayObject.layer = MinimapLayer;
        overlayObject.transform.SetParent(chunk.ChunkObject.transform, true);

        var position = CellToWorld(cell, samplePosition.y + fogOverlayHeight);
        overlayObject.transform.position = position;
        overlayObject.transform.localScale = new Vector3(cellSize * fogOverlayScale, 1f, cellSize * fogOverlayScale);

        var meshFilter = overlayObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = m_fogMesh;

        var renderer = overlayObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = m_fogMaterial;
        renderer.sortingOrder = short.MaxValue;

        return new FogOverlay(renderer);
    }

    private void EnsureFogResources()
    {
        m_minimapLayer = LayerMask.NameToLayer("MinimapMark");
        if (m_minimapLayer < 0)
            m_minimapLayer = 0;

        if (m_fogMesh == null)
        {
            m_fogMesh = new Mesh { name = "Minimap Fog Overlay Quad" };
            m_fogMesh.SetVertices(new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f)
            });
            m_fogMesh.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            m_fogMesh.SetUVs(0, new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            });
            m_fogMesh.RecalculateNormals();
            m_fogMesh.RecalculateBounds();
        }

        m_restAreaOutlineProperties ??= new MaterialPropertyBlock();
        m_restAreaOutlineProperties.Clear();
        m_restAreaOutlineProperties.SetColor("_BaseColor", restAreaOutlineColor);
        m_restAreaOutlineProperties.SetColor("_Color", restAreaOutlineColor);

        if (m_fogMaterial != null) return;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        m_fogMaterial = new Material(shader) { name = "Runtime Minimap Fog Overlay" };

        if (m_fogMaterial.HasProperty("_BaseColor"))
            m_fogMaterial.SetColor("_BaseColor", fogColor);
        if (m_fogMaterial.HasProperty("_Color"))
            m_fogMaterial.SetColor("_Color", fogColor);
    }

    private int MinimapLayer
    {
        get
        {
            if (m_minimapLayer < 0)
                EnsureFogResources();

            return m_minimapLayer;
        }
    }

    private Vector2Int WorldToCell(Vector3 position)
    {
        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize));
    }

    private Vector3 CellToWorld(Vector2Int cell, float y)
    {
        return new Vector3(
            (cell.x + 0.5f) * cellSize,
            y,
            (cell.y + 0.5f) * cellSize);
    }
}
