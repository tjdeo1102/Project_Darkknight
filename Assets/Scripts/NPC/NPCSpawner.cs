using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    public int UnitCountPerChunk = 2;
    public int PoolSIze = 15;
    public List<NPCBase> AllNPCs;
    public List<InventoryItem> Items;
    private Dictionary<NPCType, ObjectPool<NPCBase>> m_allNPCs;
    private Dictionary<ItemType, List<InventoryItem>> m_allItems;
    private List<Coroutine> m_allCoroutine;
    private InGameLoop m_inGameLoop;
    private List<ItemType> m_pickList;
    public void Init()
    {
        m_inGameLoop = InGameLoop.Instance;
        m_allNPCs = new();
        foreach (var npc in AllNPCs)
        {
            var pool = new ObjectPool<NPCBase>();
            pool.poolObj = npc;
            pool.InitSize = PoolSIze;
            pool.Init(transform);
            m_allNPCs.Add(npc.Type, pool);
        }

        m_allItems = new();
        foreach (var item in Items)
        {
            if (m_allItems.ContainsKey(item.ItemType) == false) m_allItems[item.ItemType] = new();
            m_allItems[item.ItemType].Add(item);
        }
        foreach (var list in m_allItems.Values)
        {
            list.Sort((left, right) =>
            {
                var levelCompare =
                    left.ProgressionLevel.CompareTo(right.ProgressionLevel);
                return levelCompare != 0
                    ? levelCompare
                    : left.Price.CompareTo(right.Price);
            });
        }
        var itemTypeCount = (int)ItemType.Pants - (int)ItemType.Helmet + 1;
        m_pickList = Enumerable.Range((int)ItemType.Helmet, itemTypeCount)
                            .Select(i => (ItemType)i)
                            .Where(t => m_allItems.ContainsKey(t)).ToList();

        m_allCoroutine = new();
        StartCoroutine(CreatePoolObjectRoutine());
    }

    private IEnumerator CreatePoolObjectRoutine()
    {
        foreach (var pool in m_allNPCs.Values)
        {
            m_allCoroutine.Add(StartCoroutine(pool.AutoCreateObjectPerFrame()));
            yield return null;
        }
    }

    public void RandomUnitSpawnPerChunk(Chunk chunk)
    {
        for (var i = 0; i < UnitCountPerChunk; i++)
        {
            if (ChunkManager.Instance == null || m_inGameLoop == null) break;
            Vector3 pos = Vector3.zero;
            var res = ChunkManager.Instance.TryGetSpawnPointOnChunk(chunk, Vector3.up, out pos);
            if (res == false) continue;

            var keyValue = m_allNPCs.ElementAt(Random.Range(0, m_allNPCs.Keys.Count));

            var npc = keyValue.Value.GetObject();
            var trans = npc.transform;
            trans.position = pos;
            trans.rotation = Quaternion.identity;
            SetItem(npc);
            npc.ChunkRefresh();
        }
    }

    private void SetItem(NPCBase npc)
    {
        npc.SelectItems ??= new List<InventoryItem>();
        npc.SelectItems.Clear();

        var maxLevel = m_inGameLoop.ExitStage;
        var curLevel = m_inGameLoop.StageLevel.Value;
        var pickList = m_pickList.OrderBy(_ => Random.value).Take(3).ToList();
        var inventory = GetInventorySystem();

        foreach (var pick in pickList)
        {
            if (m_allItems.TryGetValue((ItemType)pick,out var list))
            {
                var sellableItems = GetSellableItems(pick, list, inventory);
                if (sellableItems.Count == 0 || maxLevel <= 0) continue;

                var clampedLevel = Mathf.Clamp(curLevel, 1, maxLevel);
                var startIdx = Mathf.FloorToInt(
                    (clampedLevel - 1) * sellableItems.Count / (float)maxLevel);
                var endExclusive = Mathf.CeilToInt(
                    clampedLevel * sellableItems.Count / (float)maxLevel);

                startIdx = Mathf.Clamp(startIdx, 0, sellableItems.Count - 1);
                endExclusive = Mathf.Clamp(
                    endExclusive,
                    startIdx + 1,
                    sellableItems.Count);
                var pickIdx = Random.Range(startIdx, endExclusive);

                npc.SelectItems.Add(sellableItems[pickIdx]);
            }
        }
    }

    private static List<InventoryItem> GetSellableItems(
        ItemType itemType,
        IEnumerable<InventoryItem> items,
        InventorySystem inventory)
    {
        if (itemType != ItemType.Weapon)
        {
            return items.Where(item => item != null).ToList();
        }

        return items.Where(item =>
            item != null &&
            (inventory != null
                ? inventory.CanPurchaseProgressionItem(item, out _)
                : item.ProgressionLevel <= 1))
            .ToList();
    }

    private InventorySystem GetInventorySystem()
    {
        if (m_inGameLoop?.UI == null ||
            m_inGameLoop.UI.UIElements.TryGetValue(
                UIState.Inventory,
                out var element) == false)
        {
            return null;
        }

        return element as InventorySystem;
    }

    public void DestroyNPC(NPCBase ctrl)
    {
        if (m_allNPCs.TryGetValue(ctrl.Type, out var pool))
        {
            ctrl.SelectItems.Clear();
            pool.ReturnObject(ctrl);
        }
    }
}
