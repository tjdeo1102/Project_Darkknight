#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System;

public class CSVImporter : EditorWindow
{
    private TextAsset m_csvFile;
    private string m_csvPath;

    private DefaultAsset m_soFolder;
    private string m_soFolderPath;

    private string m_encoding = "euc-kr";
    private CSVImportType m_type;
    private Dictionary<string, int> m_headDic;
    private Dictionary<string, string[]> m_csvDic;

    [MenuItem("Tools/Import Data from CSV")]
    public static void ShowWindow()
    {
        GetWindow<CSVImporter>("CSV Data Importer");
    }

    void OnGUI()
    {
        m_csvFile = EditorGUILayout.ObjectField("csv파일 선택",m_csvFile, typeof(TextAsset), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as TextAsset;
        m_encoding = EditorGUILayout.TextField("CSV인코딩",m_encoding);
        m_soFolder = EditorGUILayout.ObjectField("스킬SO폴더 선택",m_soFolder, typeof(DefaultAsset), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as DefaultAsset;
        m_type = (CSVImportType)EditorGUILayout.EnumPopup("Type",m_type);

        if (m_csvFile != null)
        {
            m_csvPath = AssetDatabase.GetAssetPath(m_csvFile);

            if (!m_csvPath.EndsWith(".csv"))
            {
                EditorGUILayout.HelpBox("Not CSV File", MessageType.Error);
            }
        }

        if (m_soFolder != null)
        {
            m_soFolderPath = AssetDatabase.GetAssetPath(m_soFolder);
        }

        if (GUILayout.Button("Import"))
        {
            Import();
        }
    }

    void Import()
    {
        if (TryParseCSV() == false)
        {
            Debug.LogWarning("No CSV Data");
            return;
        }
        string[] soGUIDs = LoadSOPaths();
        foreach (var guid in soGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CSVScriptableObject data = LoadSO(path);
            if (data == null) continue;

            string fileName = Path.GetFileNameWithoutExtension(path);

            if (m_csvDic.TryGetValue(data.ID, out string[] row))
            {
                var fields = data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(a => a.IsDefined(typeof(CSVField), false))
                    .ToList();

                UpdateField(data, row);

                EditorUtility.SetDirty(data);
                Debug.Log($"Updated {fileName}");
            }
            else
            {
                Debug.LogWarning($"No {fileName}");
            }
        }


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private bool TryParseCSV()
    {
        var lines = File.ReadAllLines(m_csvPath, Encoding.GetEncoding(m_encoding));
        if (lines.Length < 2) return false;
        var headers = lines[0].Split(',');
        m_headDic = new();
        for (int i = 0; i < headers.Length; i++)
            m_headDic[headers[i]] = i;

        m_csvDic = new();
        for (int i = 1; i < lines.Length; i++)
        {
            // 텍스트에 , 예외처리 미구현 (사용시 주의)
            var row = lines[i].Split(',');
            m_csvDic[row[0].Trim()] = row;
        }

        return true;
    }

    private string[] LoadSOPaths()
    {
        if (Enum.IsDefined(typeof(CSVImportType), m_type))
        {
            return AssetDatabase.FindAssets($"t:{m_type.ToString()}", new[] { m_soFolderPath });
        }
        else
            return Array.Empty<string>();
    }

    private CSVScriptableObject LoadSO(string path)
    {
        if (m_type == CSVImportType.SkillBase)
        {
            return AssetDatabase.LoadAssetAtPath<SkillBase>(path);
        }
        else if (m_type == CSVImportType.InventroyItem)
        {
            return AssetDatabase.LoadAssetAtPath<InventoryItem>(path);
        }
        else if (m_type == CSVImportType.StatBaseSO)
        {
            return AssetDatabase.LoadAssetAtPath<StatBaseSO>(path);
        }
        else return null;
    }

    private void UpdateField(CSVScriptableObject data, string[] row)
    {
        // 리플렉션으로 자동화 (런타임 시, 작동하는 로직이 아니므로 효율 상관 x)
        var fields = data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.IsDefined(typeof(CSVField), false)).ToList();

        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<CSVField>();
            if (attr.HeaderType == CSVFIledType.None)
                UpdatePrimitiveField(field, data, row);
            else if (attr.HeaderType == CSVFIledType.StatArr)
                UpdateStatArrayField(field, data, row);
        }
    }

    private void UpdatePrimitiveField(FieldInfo field, CSVScriptableObject data, string[] row)
    {
        if (!m_headDic.TryGetValue(field.Name, out int idx)) return;
        string value = row[idx];

        if (field.FieldType == typeof(string))
            field.SetValue(data, value);
        else if (field.FieldType == typeof(int) && int.TryParse(value, out int i))
            field.SetValue(data, i);
        else if (field.FieldType == typeof(float) && float.TryParse(value, out float f))
            field.SetValue(data, f);
        else if (field.FieldType == typeof(bool) && bool.TryParse(value, out bool b))
            field.SetValue(data, b);
        else if (field.FieldType.IsEnum && Enum.TryParse(field.FieldType, value, out object e))
            field.SetValue(data, e);
    }

    private void UpdateStatArrayField(FieldInfo field, CSVScriptableObject data, string[] row)
    {
        List<StatChange> list = new();

        for (int i = 0; m_headDic.TryGetValue($"StatType{i}" ,out var col0); i++)
        {
            var sc = new StatChange();

            if (Enum.TryParse(row[col0], out StatType statType))
            {
                sc.StatType = statType;
            }
            else continue;
            string keyModifierFixed = $"StatModifierFixed{i}";
            string keyModifierPercent = $"StatModifierPercent{i}";
            string keyMinNeeded = $"MinNeeded{i}";
            string keyDuration = $"Duration{i}";
            string keyEnforceFac = $"EnforceFactor{i}";
            sc.StatModifier = new StatModifier();
            if (m_headDic.TryGetValue(keyModifierFixed, out var col1))
            {
                var value = row[col1];
                if (float.TryParse(value, out var res))
                {
                    sc.StatModifier.FixedValue = res;
                }
            }

            if (m_headDic.TryGetValue(keyModifierPercent, out var col2))
            {
                var value = row[col2];
                if (float.TryParse(value, out var res))
                {
                    sc.StatModifier.PercentValue = res;
                }
            }

            if (m_headDic.TryGetValue(keyMinNeeded, out var col3) &&
                float.TryParse(row[col3], out float min))
            {
                sc.MinNeeded = min;
            }

            if (m_headDic.TryGetValue(keyDuration, out var col4) &&
                float.TryParse(row[col4], out float dur))
            {
                sc.Duration = dur;
            }

            if (m_headDic.TryGetValue(keyEnforceFac, out var col5) &&
                float.TryParse(row[col5], out float factor))
            {
                sc.EnforceStatFactor = factor;
            }
            list.Add(sc);
        }

        field.SetValue(data, list.ToArray());
    }

}
#endif