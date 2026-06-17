using System;
using System.Collections.Generic;
using UnityEngine;

public static class NPCDialogueDatabase
{
    private const string ResourcePath = "NPC/NPCDialogues";
    private static Dictionary<string, NPCDialogueEntry> s_dialogues;

    public static NPCDialogueEntry Get(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(id)) return null;

        s_dialogues.TryGetValue(id, out var dialogue);
        return dialogue;
    }

    public static string FormatOption(
        NPCDialogueRoute route,
        IReadOnlyList<InventoryItem> shopItems)
    {
        if (route == null) return string.Empty;

        var text = route.optionText ?? string.Empty;
        var reward = route.reward;
        if (reward == null ||
            string.Equals(reward.type, "ShopItem", StringComparison.OrdinalIgnoreCase) == false)
        {
            return text;
        }

        var item = GetShopItem(shopItems, reward.shopItemSlot);
        return item != null
            ? $"{item.Name}\n{item.Price:N0} G"
            : "품절";
    }

    public static InventoryItem GetShopItem(
        IReadOnlyList<InventoryItem> shopItems,
        int slot)
    {
        return shopItems != null && slot >= 0 && slot < shopItems.Count
            ? shopItems[slot]
            : null;
    }

    public static NPCDialogueEntry CreateLegacy(
        IReadOnlyList<string> lines,
        int shopItemCount)
    {
        var introCount = Mathf.Max(0, (lines?.Count ?? 0) - 1);
        var introLines = new string[introCount];
        for (var index = 0; index < introCount; index++)
        {
            introLines[index] = lines[index];
        }

        var routeCount = Mathf.Clamp(shopItemCount, 0, 3);
        var routes = new NPCDialogueRoute[routeCount];
        for (var index = 0; index < routeCount; index++)
        {
            routes[index] = new NPCDialogueRoute
            {
                id = $"legacy_shop_{index}",
                optionText = "{itemName}",
                lines = Array.Empty<string>(),
                reward = new NPCDialogueReward
                {
                    type = "ShopItem",
                    shopItemSlot = index,
                    successMessage = "거래 고맙네. 행운을 빌지, 모험가."
                }
            };
        }

        return new NPCDialogueEntry
        {
            id = "legacy",
            displayName = "Merchant",
            introLines = introLines,
            choicePrompt = lines != null && lines.Count > 0
                ? lines[lines.Count - 1]
                : "필요한 장비를 골라보게.",
            routes = routes
        };
    }

    private static void EnsureLoaded()
    {
        if (s_dialogues != null) return;

        s_dialogues = new Dictionary<string, NPCDialogueEntry>(
            StringComparer.OrdinalIgnoreCase);
        var asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null)
        {
            Debug.LogError($"NPC dialogue JSON was not found at Resources/{ResourcePath}.json.");
            return;
        }

        var collection = JsonUtility.FromJson<NPCDialogueCollection>(asset.text);
        if (collection?.npcs == null)
        {
            Debug.LogError("NPC dialogue JSON is empty or invalid.");
            return;
        }

        foreach (var dialogue in collection.npcs)
        {
            if (dialogue == null || string.IsNullOrWhiteSpace(dialogue.id)) continue;
            if (s_dialogues.TryAdd(dialogue.id, dialogue) == false)
            {
                Debug.LogWarning($"Duplicate NPC dialogue id ignored: {dialogue.id}");
            }
        }
    }
}
