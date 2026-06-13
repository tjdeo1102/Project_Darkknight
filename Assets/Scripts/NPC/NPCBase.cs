using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;

public class NPCBase : MonoBehaviour
{
    public NPCType Type;
    [Tooltip("Empty values use npc_01, npc_02, ... based on NPCType.")]
    public string DialogueId;
    public Vector3 SpawnOffset = Vector3.up;
    public float ChunkRefreshDelay = 2f;

    [Header("Legacy fallback")]
    public List<string> DialogTexts;
    public NavMeshAgent NavAgent;
    [Header("Max Count is 3")]
    public List<InventoryItem> SelectItems;

    private bool m_isTouch;
    private bool m_isInteract;
    private bool m_rewardClaimed;
    private bool m_waitingForChoice;
    private int m_introLine;
    private int m_routeLine;
    private InGameLoop gameLoop;
    private ChunkManager m_chunkManager;
    private PlayerController m_player;
    private NPCDialog m_dialog;
    private NPCDialogueEntry m_dialogue;
    private NPCDialogueRoute m_selectedRoute;
    private readonly HashSet<Collider> m_playerColliders = new();

    private void Awake()
    {
        if (NavAgent != null) NavAgent.enabled = false;
    }

    private void OnEnable()
    {
        m_isTouch = false;
        m_player = null;
        m_playerColliders.Clear();
        m_rewardClaimed = false;
        m_waitingForChoice = false;
        m_isInteract = false;
        if (SelectItems != null && SelectItems.Count > 3)
        {
            SelectItems.RemoveRange(3, SelectItems.Count - 3);
        }

        if (InGameLoop.Instance != null)
        {
            gameLoop = InGameLoop.Instance;
            gameLoop.StageLevel.OnValueChanged += DestroyThisNPC;
            m_chunkManager = gameLoop.ChunkManager;

            if (gameLoop.UI != null &&
                gameLoop.UI.UIElements.TryGetValue(UIState.Dialog, out var element))
            {
                m_dialog = element as NPCDialog;
            }
        }
    }

    private void OnDisable()
    {
        if (gameLoop != null)
        {
            gameLoop.StageLevel.OnValueChanged -= DestroyThisNPC;
        }
        if (NavAgent != null) NavAgent.enabled = false;
        m_isTouch = false;
        m_player = null;
        m_playerColliders.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(TagManager.GetTagString(TargetTag.Player)) == false) return;

        var ctrl = other.GetComponentInParent<PlayerController>();
        if (ctrl == null) return;

        m_playerColliders.Add(other);
        m_player = ctrl;
        m_isTouch = true;
    }

    private void Update()
    {
        if (m_isTouch == false) return;
        var interactAction = m_player?.input?.currentActionMap?.FindAction("Interact");
        if (interactAction != null && interactAction.triggered)
        {
            var target = m_player.transform.position;
            target.y = transform.position.y;
            transform.LookAt(target, Vector3.up);
            if (m_isInteract) Interact();
            else StartInteract();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (m_playerColliders.Remove(other) == false) return;
        if (m_playerColliders.Count > 0) return;

        m_isTouch = false;
        m_player = null;
    }

    public void StartInteract()
    {
        if (ResolveDialog() == false)
        {
            EndInteract();
            return;
        }

        m_dialogue = NPCDialogueDatabase.Get(GetDialogueId()) ??
                     NPCDialogueDatabase.CreateLegacy(
                         DialogTexts,
                         SelectItems?.Count ?? 0);
        if (m_dialogue == null)
        {
            EndInteract();
            return;
        }

        gameLoop.UI.ChangeState(UIState.Dialog);
        m_dialog.Init(this);

        m_introLine = 0;
        m_routeLine = 0;
        m_selectedRoute = null;
        m_waitingForChoice = false;
        m_isInteract = true;
        Interact();
    }

    public virtual void Interact()
    {
        if (m_dialog == null || m_dialogue == null || m_waitingForChoice) return;

        if (m_selectedRoute == null)
        {
            if (m_introLine < (m_dialogue.introLines?.Length ?? 0))
            {
                m_dialog.ShowLine(m_dialogue.introLines[m_introLine++]);
                return;
            }

            m_waitingForChoice = true;
            m_dialog.ShowRoutes(
                m_dialogue.choicePrompt,
                m_dialogue.routes,
                SelectItems);
            return;
        }

        if (m_routeLine < (m_selectedRoute.lines?.Length ?? 0))
        {
            m_dialog.ShowLine(m_selectedRoute.lines[m_routeLine++]);
            return;
        }

        CompleteSelectedRoute();
    }

    public void SelectRoute(int routeIndex)
    {
        if (m_waitingForChoice == false ||
            m_dialogue?.routes == null ||
            routeIndex < 0 ||
            routeIndex >= m_dialogue.routes.Length)
        {
            return;
        }

        m_selectedRoute = m_dialogue.routes[routeIndex];
        m_routeLine = 0;
        m_waitingForChoice = false;
        m_dialog.HideChoices();
        Interact();
    }

    public virtual void EndInteract()
    {
        m_isInteract = false;
        m_waitingForChoice = false;
        m_selectedRoute = null;
        m_dialogue = null;
    }

    private void CompleteSelectedRoute()
    {
        if (m_selectedRoute == null) return;

        if (m_rewardClaimed)
        {
            m_dialog.ShowCompletion("이 NPC와의 거래는 이미 마쳤습니다.");
            return;
        }

        var success = TryApplyReward(m_selectedRoute.reward, out var message);
        if (success) m_rewardClaimed = true;
        m_dialog.ShowCompletion(message);
    }

    private bool TryApplyReward(NPCDialogueReward reward, out string message)
    {
        if (reward == null ||
            string.IsNullOrWhiteSpace(reward.type) ||
            string.Equals(reward.type, "None", System.StringComparison.OrdinalIgnoreCase))
        {
            message = reward?.successMessage ?? "이야기를 들어줘서 고맙습니다.";
            return true;
        }

        var player = gameLoop?.Player;
        if (player?.model == null)
        {
            message = "보상을 지급할 플레이어를 찾을 수 없습니다.";
            return false;
        }

        if (string.Equals(
                reward.type,
                "ShopItem",
                System.StringComparison.OrdinalIgnoreCase))
        {
            return TryPurchaseShopItem(reward, player, out message);
        }

        if (string.Equals(
                reward.type,
                "Money",
                System.StringComparison.OrdinalIgnoreCase))
        {
            player.model.Money.AddModifier(
                new StatModifier(Mathf.Max(0f, reward.amount)),
                StatModifyType.DialogReward);
            message = GetSuccessMessage(reward, $"{reward.amount:F0}$를 받았습니다.");
            return true;
        }

        if (string.Equals(
                reward.type,
                "RestoreHealth",
                System.StringComparison.OrdinalIgnoreCase))
        {
            RestoreStat(player.model.Health, reward.amount, StatModifyType.Damage);
            message = GetSuccessMessage(reward, "체력을 회복했습니다.");
            return true;
        }

        if (string.Equals(
                reward.type,
                "RestoreMana",
                System.StringComparison.OrdinalIgnoreCase))
        {
            RestoreStat(player.model.Mana, reward.amount, StatModifyType.SkillUse);
            message = GetSuccessMessage(reward, "마나를 회복했습니다.");
            return true;
        }

        message = $"지원하지 않는 NPC 보상 타입입니다: {reward.type}";
        return false;
    }

    private bool TryPurchaseShopItem(
        NPCDialogueReward reward,
        PlayerController player,
        out string message)
    {
        var item = NPCDialogueDatabase.GetShopItem(SelectItems, reward.shopItemSlot);
        if (item == null)
        {
            message = "선택한 장비는 현재 품절입니다.";
            return false;
        }

        if (player.model.Money.TotalValue < item.Price)
        {
            message = "소지금이 부족합니다.";
            return false;
        }

        if (gameLoop.UI.UIElements.TryGetValue(UIState.Inventory, out var element) == false ||
            element is not InventorySystem inventory ||
            inventory.HasEmptySlot() == false)
        {
            message = "인벤토리가 가득 찼습니다.";
            return false;
        }

        var purchasedItem = Instantiate(item);
        if (inventory.TryAddItem(purchasedItem) == false)
        {
            Destroy(purchasedItem);
            message = "장비를 인벤토리에 추가하지 못했습니다.";
            return false;
        }

        player.model.Money.AddModifier(
            new StatModifier(-item.Price),
            StatModifyType.BuyItem);
        message = GetSuccessMessage(
            reward,
            $"{item.Name}을(를) 구매했습니다.");
        return true;
    }

    private static void RestoreStat(
        Stat stat,
        float requestedAmount,
        StatModifyType modifierType)
    {
        if (stat == null) return;
        var amount = Mathf.Min(
            Mathf.Max(0f, requestedAmount),
            Mathf.Max(0f, stat.maxValue - stat.TotalValue));
        if (amount > 0f)
        {
            stat.AddModifier(new StatModifier(amount), modifierType);
        }
    }

    private static string GetSuccessMessage(
        NPCDialogueReward reward,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(reward.successMessage)
            ? fallback
            : reward.successMessage;
    }

    private string GetDialogueId()
    {
        return string.IsNullOrWhiteSpace(DialogueId)
            ? $"npc_{(int)Type:00}"
            : DialogueId.Trim();
    }

    private bool ResolveDialog()
    {
        if (m_dialog != null) return true;

        gameLoop ??= InGameLoop.Instance;
        if (gameLoop?.UI == null) return false;

        if (gameLoop.UI.UIElements.TryGetValue(UIState.Dialog, out var element))
        {
            m_dialog = element as NPCDialog;
        }

        return m_dialog != null;
    }

    public void ChunkRefresh()
    {
        if (m_chunkManager == null) return;

        var chunk = m_chunkManager.GetChunk(transform.position);
        if (chunk != null)
        {
            transform.parent = chunk.ChunkObject.transform;
        }
        else
        {
            DestroyThisNPC(-1);
        }
    }

    private void DestroyThisNPC(int stageLevel)
    {
        if (InGameLoop.Instance != null)
        {
            InGameLoop.Instance.NPCSpawner.DestroyNPC(this);
        }
    }
}

[Serializable]
public class NPCDialogueCollection
{
    public NPCDialogueEntry[] npcs;
}

[Serializable]
public class NPCDialogueEntry
{
    public string id;
    public string displayName;
    public string[] introLines;
    public string choicePrompt;
    public NPCDialogueRoute[] routes;
}

[Serializable]
public class NPCDialogueRoute
{
    public string id;
    public string optionText;
    public string[] lines;
    public NPCDialogueReward reward;
}

[Serializable]
public class NPCDialogueReward
{
    public string type;
    public int shopItemSlot = -1;
    public float amount;
    public string successMessage;
}

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
