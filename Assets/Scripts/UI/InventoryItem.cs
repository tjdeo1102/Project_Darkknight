using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "InventoryItem", menuName = "Scriptable Objects/Item")]
public class InventoryItem : ScriptableObject
{
    public string Description;
    public ItemType ItemType;
    public Sprite Icon;

    [Serializable]
    public struct ModifyStats
    {
        public StatType type;
        [SerializeField] public StatModifier modifier;
    }
    public ModifyStats[] ModifierStats;
}
