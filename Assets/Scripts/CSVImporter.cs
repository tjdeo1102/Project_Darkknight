using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Splines;
using System.Reflection;
using System.Text;
using UnityEngine.Rendering;
using System;
using static StatSkill;
using static UnityEngine.Rendering.DebugUI;

public class CSVImporter : EditorWindow
{
    private TextAsset m_csvFile;
    private string m_csvPath;

    private DefaultAsset m_soFolder;
    private string m_soFolderPath;

    [MenuItem("Tools/Import SkillData from CSV")]
    public static void ShowWindow()
    {
        GetWindow<CSVImporter>("SkillData CSV Importer");
    }

    void OnGUI()
    {
        m_csvFile = EditorGUILayout.ObjectField("csv파일 선택",m_csvFile, typeof(TextAsset), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as TextAsset;
        m_soFolder = EditorGUILayout.ObjectField("스킬SO폴더 선택",m_soFolder, typeof(DefaultAsset), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as DefaultAsset;

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
        var lines = File.ReadAllLines(m_csvPath, Encoding.GetEncoding("euc-kr"));
        if (lines.Length < 2)
        {
            Debug.LogWarning("No CSV Data");
            return;
        }
        var headers = lines[0].Split(',');
        Dictionary<string, int> headDic = new();
        for (int i = 0; i < headers.Length; i++)
        {
            headDic.Add(headers[i],i);
        }

        Dictionary<string, string[]> csvDict = new();
        for (int i = 1; i < lines.Length; i++)
        {
            // 텍스트에 , 예외처리 미구현 (사용시 주의)
            var row = lines[i].Split(',');
            var filename = row[0].Trim(); 
            csvDict[filename] = row;
        }

        var soGUIDs = AssetDatabase.FindAssets("t:SkillBase", new[] { m_soFolderPath });
        foreach (var guid in soGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SkillBase data = AssetDatabase.LoadAssetAtPath<SkillBase>(path);;
            string fileName = Path.GetFileNameWithoutExtension(path);

            if (csvDict.TryGetValue(data.ID, out var row))
            {
                // 리플렉션으로 자동화 (런타임 시, 작동하는 로직이 아니므로 효율 상관 x)
                var fields = data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(a => a.IsDefined(typeof(CSVField), false))
                    .ToArray();

                foreach (var field in fields)
                {
                    var att = field.GetCustomAttribute<CSVField>();
                    if (att != null && att.HeaderType == CSVFIledType.None)
                    {
                        // 매핑되는 헤더의 index를 통해 해당 값을 불러오기
                        if (headDic.TryGetValue(field.Name, out var idx) == false) continue;
                        
                        string stringValue = row[idx];
                        if (field.FieldType == typeof(string))
                        {
                            field.SetValue(data, stringValue);
                        }
                        else if (field.FieldType == typeof(int) && int.TryParse(stringValue, out int intValue))
                        {
                            field.SetValue(data, intValue);
                        }
                        else if (field.FieldType == typeof(float) && float.TryParse(stringValue, out float floatValue))
                        {
                            field.SetValue(data, floatValue);
                        }
                        else if (field.FieldType == typeof(bool) && bool.TryParse(stringValue, out bool boolValue))
                        {
                            field.SetValue(data, boolValue);
                        }
                    }
                    // 딕셔너리의 경우: (ex. 스탯 스킬의 스탯 딕셔너리)
                    else
                    {
                        if (att.HeaderType == CSVFIledType.StatArr)
                        {
                            // csvDict의 row에서 StatType ~ EnforceFactor까지의 그룹을 여러개 추출해서 Arr로 저장
                            int groupCount = typeof(StatChange).GetFields(BindingFlags.Public | BindingFlags.Instance).Length;
                            var arr = field.GetValue(data) as StatChange[];
                            var arrLen = arr.Length;
                            // 처음 field 이름
                            string firstField = "StatType0";
                            if (headDic.TryGetValue(firstField, out int firstIdx) == false) continue;
                            int iterCount = (headDic.Count - firstIdx) / groupCount;
                            arr = new StatChange[iterCount];

                            for (int i = 0; i < iterCount; i++)
                            {
                                var sc = new StatChange();

                                string keyStatType = $"StatType{i}";
                                string keyModifier = $"StatModifier{i}";
                                string keyMinNeeded = $"MinNeeded{i}";
                                string keyDuration = $"Duration{i}";
                                string keyEnforceFac = $"EnforceFactor{i}";

                                // 키가 존재할 경우에만 처리
                                if (headDic.ContainsKey(keyStatType) &&
                                    Enum.TryParse(row[headDic[keyStatType]], out StatType statType))
                                {
                                    sc.StatType = statType;
                                }

                                if (headDic.ContainsKey(keyModifier))
                                {
                                    var value = row[headDic[keyModifier]];
                                    var parts = value.Split(',');
                                    float m1 = 0f, m2 = 0f;
                                    if (parts.Length >= 2)
                                    {
                                        float.TryParse(parts[0], out m1);
                                        float.TryParse(parts[1], out m2);
                                    }
                                    sc.StatModifier = new StatModifier(m1, m2);
                                }

                                if (headDic.ContainsKey(keyMinNeeded) &&
                                    float.TryParse(row[headDic[keyMinNeeded]], out float min))
                                {
                                    sc.MinNeeded = min;
                                }

                                if (headDic.ContainsKey(keyDuration) &&
                                    float.TryParse(row[headDic[keyDuration]], out float dur))
                                {
                                    sc.Duration = dur;
                                }

                                if (headDic.ContainsKey(keyEnforceFac) &&
                                    float.TryParse(row[headDic[keyEnforceFac]], out float factor))
                                {
                                    sc.EnforceStatFactor = factor;
                                }

                                arr[i] = sc;
                            }

                            field.SetValue(data, arr);
                        }
                    }
                }

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
}
