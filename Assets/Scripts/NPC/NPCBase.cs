using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCBase : MonoBehaviour
{
    public NPCType Type;
    [Tooltip("Empty values use npc_01, npc_02, ... based on NPCType.")]
    public string DialogueId;
    public Vector3 SpawnOffset = Vector3.up;
    public float ChunkRefreshDelay = 2f;
    [SerializeField] private NPCNameplate nameplatePrefab;

    [Header("Legacy fallback")]
    public List<string> DialogTexts;
    public NavMeshAgent NavAgent;
    [Header("Max Count is 3")]
    public List<InventoryItem> SelectItems;
    [Header("Rest Area Roam")]
    [SerializeField, Min(0.5f)] private float restAreaRoamIntervalMin = 2f;
    [SerializeField, Min(0.5f)] private float restAreaRoamIntervalMax = 5f;
    [SerializeField, Min(0.1f)] private float restAreaRoamStoppingDistance = 0.8f;
    [SerializeField, Min(0.1f)] private float restAreaRoamSampleRadius = 0.45f;

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
    private NPCSpawner m_shopProvider;
    private bool m_areShopItemsAssigned;
    private readonly HashSet<Collider> m_playerColliders = new();
    private readonly List<Vector3> m_restAreaRoamPoints = new();
    private NPCNameplate m_nameplate;
    private bool m_canRestAreaRoam;
    private float m_nextRestAreaRoamTime;

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

        EnsureNameplate();
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
        m_restAreaRoamPoints.Clear();
        m_canRestAreaRoam = false;
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
        if (m_isTouch)
        {
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

        UpdateRestAreaRoam();
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
        EnsureShopItemsAssigned();

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

    public void PrepareShop(NPCSpawner shopProvider)
    {
        m_shopProvider = shopProvider;
        m_areShopItemsAssigned = false;
        SelectItems ??= new List<InventoryItem>();
        SelectItems.Clear();
    }

    public void ConfigureRestAreaRoam(IReadOnlyList<Vector3> roamPoints)
    {
        m_restAreaRoamPoints.Clear();
        if (roamPoints != null)
        {
            for (var i = 0; i < roamPoints.Count; i++)
                m_restAreaRoamPoints.Add(roamPoints[i]);
        }

        m_canRestAreaRoam = NavAgent != null && m_restAreaRoamPoints.Count > 1;
        if (m_canRestAreaRoam == false) return;

        NavAgent.enabled = true;
        NavAgent.stoppingDistance = restAreaRoamStoppingDistance;
        if (NavMesh.SamplePosition(transform.position, out var hit, restAreaRoamSampleRadius, NavMesh.AllAreas) &&
            IsValidRestAreaPoint(hit.position))
        {
            NavAgent.Warp(hit.position);
        }

        m_nextRestAreaRoamTime = Time.time + Random.Range(0.5f, restAreaRoamIntervalMax);
    }

    private void UpdateRestAreaRoam()
    {
        if (m_canRestAreaRoam == false || m_isInteract || NavAgent == null || NavAgent.enabled == false)
            return;
        if (Time.time < m_nextRestAreaRoamTime)
            return;
        if (NavAgent.isOnNavMesh == false)
            return;
        if (NavAgent.pathPending || NavAgent.remainingDistance > NavAgent.stoppingDistance)
            return;

        var target = m_restAreaRoamPoints[Random.Range(0, m_restAreaRoamPoints.Count)];
        if (NavMesh.SamplePosition(target, out var hit, restAreaRoamSampleRadius, NavMesh.AllAreas) &&
            IsValidRestAreaPoint(hit.position))
        {
            NavAgent.SetDestination(hit.position);
        }

        m_nextRestAreaRoamTime = Time.time + Random.Range(restAreaRoamIntervalMin, restAreaRoamIntervalMax);
    }

    private bool IsValidRestAreaPoint(Vector3 position)
    {
        return ChunkManager.Instance == null || ChunkManager.Instance.IsInRestArea(position);
    }

    private void EnsureShopItemsAssigned()
    {
        if (m_areShopItemsAssigned) return;

        m_areShopItemsAssigned = true;
        m_shopProvider?.AssignItemsForFirstInteraction(this);
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
            element is not InventorySystem inventory)
        {
            message = "인벤토리를 찾을 수 없습니다.";
            return false;
        }

        if (inventory.CanPurchaseProgressionItem(item, out var requiredLevel) == false)
        {
            message =
                $"{item.ItemType} {requiredLevel}단계를 먼저 보유해야 이 장비를 구매할 수 있습니다.";
            return false;
        }

        if (inventory.HasEmptySlot() == false)
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

    public string GetDisplayName()
    {
        var dialogue = NPCDialogueDatabase.Get(GetDialogueId());
        if (string.IsNullOrWhiteSpace(dialogue?.displayName) == false)
            return dialogue.displayName;

        return string.IsNullOrWhiteSpace(DialogueId)
            ? $"Merchant {(int)Type:00}"
            : DialogueId;
    }

    private void EnsureNameplate()
    {
        if (m_nameplate == null)
        {
            if (nameplatePrefab == null)
            {
                Debug.LogError(
                    $"NPC nameplate prefab is not assigned to {name}.",
                    this);
                return;
            }

            m_nameplate = Instantiate(nameplatePrefab, transform, false);
        }

        m_nameplate.Initialize(this);
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
