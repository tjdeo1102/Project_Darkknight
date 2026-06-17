using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "InventoryItem", menuName = "Scriptable Objects/Item")]
public class InventoryItem : CSVScriptableObject
{
    [CSVField(CSVFIledType.None)]
    public string Name;
    [CSVField(CSVFIledType.None)]
    public string Description;
    [CSVField(CSVFIledType.None)]
    public int Price;
    [CSVField(CSVFIledType.None)]
    public int Level;
    [CSVField(CSVFIledType.None)]
    public ItemType ItemType;
    public Sprite Icon;

    [CSVField(CSVFIledType.StatArr)]
    public StatChange[] ModifierStats;

    public int ProgressionLevel
    {
        get
        {
            if (Level > 0) return Level;
            return int.TryParse(ID, out var id) && id > 0
                ? (id - 1) % 10 + 1
                : 1;
        }
    }

    public int ProgressionSeries
    {
        get
        {
            return int.TryParse(ID, out var id) && id > 0
                ? (id - 1) / 10
                : -1;
        }
    }
}
