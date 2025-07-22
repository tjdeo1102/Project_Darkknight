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
    public ItemType ItemType;
    public Sprite Icon;

    [CSVField(CSVFIledType.StatArr)]
    public StatChange[] ModifierStats;
}
