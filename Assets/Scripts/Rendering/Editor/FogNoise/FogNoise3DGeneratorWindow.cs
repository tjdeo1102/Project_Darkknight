using System;
using UnityEditor;
using UnityEngine;

public sealed class FogNoise3DGeneratorWindow : EditorWindow
{
    private const int Resolution = 64;
    private const string DefaultDirectory = "Assets/Art/VFX/Fog/Textures";
    private const string DefaultAssetName = "FogNoise3D";

    [SerializeField] private int seed;
    [SerializeField, Range(1, 16)] private int divisionsA = 3;
    [SerializeField, Range(1, 24)] private int divisionsB = 7;
    [SerializeField, Range(1, 32)] private int divisionsC = 11;
    [SerializeField, Range(0f, 1f)] private float persistence = 0.65f;
    [SerializeField] private bool invert = true;

    [MenuItem("Tools/Dark Knight/Generate Fog Noise 3D")]
    private static void Open()
    {
        GetWindow<FogNoise3DGeneratorWindow>("Fog Noise 3D");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("64x64x64 Single-Channel Worley Noise", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Creates a seamless R8 Texture3D for the volumetric fog material. " +
            "Generation runs only in the Editor.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.IntField("Resolution", Resolution);

        seed = EditorGUILayout.IntField("Seed", seed);
        divisionsA = EditorGUILayout.IntSlider("Large Cells", divisionsA, 1, 16);
        divisionsB = EditorGUILayout.IntSlider("Medium Cells", divisionsB, 1, 24);
        divisionsC = EditorGUILayout.IntSlider("Small Cells", divisionsC, 1, 32);
        persistence = EditorGUILayout.Slider("Detail Persistence", persistence, 0f, 1f);
        invert = EditorGUILayout.Toggle("Invert", invert);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Texture3D", GUILayout.Height(32f)))
            Generate();
    }

    private void Generate()
    {
        EnsureDefaultDirectory();

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Fog Noise Texture3D",
            DefaultAssetName,
            "asset",
            "Choose where to save the generated fog noise.",
            DefaultDirectory);

        if (string.IsNullOrEmpty(path))
            return;

        Texture3D generatedTexture = null;

        try
        {
            byte[] voxels = BuildNoise();
            generatedTexture = new Texture3D(Resolution, Resolution, Resolution, TextureFormat.R8, false)
            {
                name = System.IO.Path.GetFileNameWithoutExtension(path),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 0
            };

            generatedTexture.SetPixelData(voxels, 0);
            generatedTexture.Apply(false, true);

            UnityEngine.Object existingAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (existingAsset != null && existingAsset is not Texture3D)
                throw new InvalidOperationException($"The selected path contains a non-Texture3D asset: {path}");

            Texture3D savedTexture;
            if (existingAsset is Texture3D existingTexture)
            {
                EditorUtility.CopySerialized(generatedTexture, existingTexture);
                DestroyImmediate(generatedTexture);
                generatedTexture = null;
                savedTexture = existingTexture;
                EditorUtility.SetDirty(savedTexture);
            }
            else
            {
                AssetDatabase.CreateAsset(generatedTexture, path);
                savedTexture = generatedTexture;
                generatedTexture = null;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = savedTexture;
            EditorGUIUtility.PingObject(savedTexture);
        }
        catch (OperationCanceledException)
        {
            if (generatedTexture != null)
                DestroyImmediate(generatedTexture);
        }
        catch (Exception exception)
        {
            if (generatedTexture != null)
                DestroyImmediate(generatedTexture);

            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Fog Noise Generation Failed", exception.Message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private byte[] BuildNoise()
    {
        Vector3[] pointsA = CreateFeaturePoints(divisionsA, seed);
        Vector3[] pointsB = CreateFeaturePoints(divisionsB, seed + 1013);
        Vector3[] pointsC = CreateFeaturePoints(divisionsC, seed + 2029);
        float[] values = new float[Resolution * Resolution * Resolution];

        float min = float.MaxValue;
        float max = float.MinValue;
        float persistenceSquared = persistence * persistence;
        float weightSum = 1f + persistence + persistenceSquared;

        for (int z = 0; z < Resolution; z++)
        {
            if (EditorUtility.DisplayCancelableProgressBar(
                    "Generating Fog Noise",
                    $"Sampling volume slice {z + 1}/{Resolution}",
                    z / (float)Resolution))
            {
                throw new OperationCanceledException();
            }

            for (int y = 0; y < Resolution; y++)
            {
                for (int x = 0; x < Resolution; x++)
                {
                    Vector3 position = new Vector3(
                        (x + 0.5f) / Resolution,
                        (y + 0.5f) / Resolution,
                        (z + 0.5f) / Resolution);

                    float value =
                        SampleWorley(position, pointsA, divisionsA) +
                        SampleWorley(position, pointsB, divisionsB) * persistence +
                        SampleWorley(position, pointsC, divisionsC) * persistenceSquared;

                    value /= weightSum;
                    if (invert)
                        value = 1f - value;

                    int index = x + Resolution * (y + z * Resolution);
                    values[index] = value;
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                }
            }
        }

        byte[] voxels = new byte[values.Length];
        float range = Mathf.Max(max - min, Mathf.Epsilon);

        for (int i = 0; i < values.Length; i++)
            voxels[i] = (byte)Mathf.RoundToInt(Mathf.Clamp01((values[i] - min) / range) * 255f);

        return voxels;
    }

    private static Vector3[] CreateFeaturePoints(int divisions, int pointSeed)
    {
        var random = new System.Random(pointSeed);
        var points = new Vector3[divisions * divisions * divisions];
        float cellSize = 1f / divisions;

        for (int z = 0; z < divisions; z++)
        {
            for (int y = 0; y < divisions; y++)
            {
                for (int x = 0; x < divisions; x++)
                {
                    int index = x + divisions * (y + z * divisions);
                    points[index] = new Vector3(
                        (x + (float)random.NextDouble()) * cellSize,
                        (y + (float)random.NextDouble()) * cellSize,
                        (z + (float)random.NextDouble()) * cellSize);
                }
            }
        }

        return points;
    }

    private static float SampleWorley(Vector3 position, Vector3[] points, int divisions)
    {
        int cellX = Mathf.FloorToInt(position.x * divisions);
        int cellY = Mathf.FloorToInt(position.y * divisions);
        int cellZ = Mathf.FloorToInt(position.z * divisions);
        float minimumSquaredDistance = float.MaxValue;

        for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    int adjacentX = cellX + offsetX;
                    int adjacentY = cellY + offsetY;
                    int adjacentZ = cellZ + offsetZ;
                    int wrappedX = PositiveModulo(adjacentX, divisions);
                    int wrappedY = PositiveModulo(adjacentY, divisions);
                    int wrappedZ = PositiveModulo(adjacentZ, divisions);
                    int index = wrappedX + divisions * (wrappedY + wrappedZ * divisions);

                    Vector3 point = points[index];
                    point.x += Mathf.Floor(adjacentX / (float)divisions);
                    point.y += Mathf.Floor(adjacentY / (float)divisions);
                    point.z += Mathf.Floor(adjacentZ / (float)divisions);

                    minimumSquaredDistance = Mathf.Min(
                        minimumSquaredDistance,
                        (position - point).sqrMagnitude);
                }
            }
        }

        return Mathf.Sqrt(minimumSquaredDistance);
    }

    private static int PositiveModulo(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private static void EnsureDefaultDirectory()
    {
        string current = "Assets";
        string[] folders = { "Art", "VFX", "Fog", "Textures" };

        foreach (string folder in folders)
        {
            string next = $"{current}/{folder}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, folder);

            current = next;
        }
    }
}
