using System;
using UnityEngine;

public abstract class CSVScriptableObject: ScriptableObject
{
    [CSVField(CSVFIledType.None)]
    public string ID;

    [HideInInspector] public string OriginSO;
    public void Backup()
    {
        if (string.IsNullOrEmpty(OriginSO) == false)
            JsonUtility.FromJsonOverwrite(OriginSO, this);

        OriginSO = JsonUtility.ToJson(this);
    }

    public void Restore()
    {
        if (string.IsNullOrEmpty(OriginSO)) return;
        JsonUtility.FromJsonOverwrite(OriginSO, this);
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class CSVField : Attribute
{
    public CSVFIledType HeaderType;

    public CSVField(CSVFIledType headerType)
    {
        HeaderType = headerType;
    }
}
