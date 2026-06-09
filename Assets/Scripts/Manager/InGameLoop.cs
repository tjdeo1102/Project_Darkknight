using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

[System.Serializable]
public class ObservableVariable<T>
{
    [SerializeField] private T value;
    public Action<T> OnValueChanged;

    public T Value
    {
        get => value;
        set
        {
            this.value = value;
            OnValueChanged?.Invoke(this.value);
        }
    }
}

public class InGameLoop : ManagerBase<InGameLoop>
{
    [Header("GameSetting")]
    public int ExitStage = 3;
    public int StageUpCount = 15;
    public float DeadDuration = 2f;
    public bool IsStartGame = false;
    public PlayerController Player;
    public UIController UI;

    [Header("BossMap")]
    public Transform PlayerTeleport;
    public Transform BossTeleport;

    [Header("GameState")]
    public ObservableVariable<int> KillCount;
    public ObservableVariable<int> StageLevel;

    [Header("Require Component")]
    public EnemySpawner EnemySpawner;
    public NPCSpawner NPCSpawner;
    public ChunkManager ChunkManager;

    private List<IManager> m_managers;
    private InputActionMap m_originInputMap;
    private bool IsSpawnBoss;
    private Vector3 m_lastPlayerPos;
    private async void Start()
    {
        await Init();

        StartCoroutine(GameStartRoutine());
    }

    private void OnDisable()
    {
        KillCount.OnValueChanged -= OnUpdateKill;
    }
    private async Task Init()
    {
        IsSpawnBoss = false;

        KillCount = new();
        StageLevel = new();
        StageLevel.Value = 1;
        KillCount.Value = 0;

        KillCount.OnValueChanged += OnUpdateKill;

        if (transform.parent != null)
        {
            m_managers = transform.parent.GetComponentsInChildren<IManager>().ToList();
        }
        IsReady = true;

        // Spawner 초기화
        if (NPCSpawner != null)
        {
            AsyncOperationHandle<IList<GameObject>> npcHandle =
            Addressables.LoadAssetsAsync<GameObject>("NPC", null);

            AsyncOperationHandle<IList<InventoryItem>> itemHandle =
            Addressables.LoadAssetsAsync<InventoryItem>("InventoryItem", null);

            await itemHandle.Task;
            await npcHandle.Task;

            var itemList = new List<InventoryItem>();
            var npcList = new List<NPCBase>();
            NPCSpawner.Items = itemList;
            NPCSpawner.AllNPCs = npcList;

            if (itemHandle.Status == AsyncOperationStatus.Succeeded
                && npcHandle.Status == AsyncOperationStatus.Succeeded)
            {
                itemList.AddRange(itemHandle.Result);
                foreach (var npc in npcHandle.Result)
                {
                    if (npc.TryGetComponent<NPCBase>(out var npcCtrl))
                        npcList.Add(npcCtrl);
                }
                NPCSpawner.Init();
            }
            else
            {
                Debug.LogError("Failed to load");
                IsReady = false;
            }
        }
        else IsReady = false;

        if (EnemySpawner != null)
        {
            AsyncOperationHandle<IList<GameObject>> enemyHandle =
            Addressables.LoadAssetsAsync<GameObject>("Enemy", null);

            await enemyHandle.Task;

            var enemyList = new List<EnemyController>();
            EnemySpawner.AllEnemys = enemyList;

            if (enemyHandle.Status == AsyncOperationStatus.Succeeded)
            {
                foreach (var enemy in enemyHandle.Result)
                {
                    if (enemy.TryGetComponent<EnemyController>(out var enemyCtrl))
                        enemyList.Add(enemyCtrl);
                }
                EnemySpawner.Init();
            }
            else
            {
                Debug.LogError("Failed to load");
                IsReady = false;
            }
        }
        else IsReady = false;
    }

    IEnumerator GameStartRoutine()
    {
        while(m_managers != null && m_managers.Any(x => x.IsReady == false))
        {
            yield return null;
        }
        if (ChunkManager != null) StartCoroutine(ChunkManager.PlayerStartRoutine());
        if (Player != null) Player.gameObject.SetActive(true);
        IsStartGame = true;

        if (m_managers != null)
        {
            foreach (IManager manager in m_managers)
            {
                manager.StartInit();
            }
        }
    }

    public void OnUpdateKill(int killCount)
    {
        if (killCount >= StageUpCount)
        {
            SpawnBoss();
        }
    }


    public void SpawnBoss()
    {
        if (IsSpawnBoss == true) return;
        int cinemaBoss = (int)EnemyType.CinemaBoss_1 + StageLevel.Value - 1;
        if (EnemySpawner != null)
        {
            EnemySpawner.CinemaBossSpawn(StageLevel.Value - 1);
        }
        if (Player != null)
        {
            m_lastPlayerPos = Player.transform.position;
            Player.transform.position = PlayerTeleport.transform.position;
            var camCol = Player.GetComponentInChildren<CinemachineDecollider>();
            if (camCol != null)
            {
                camCol.enabled = false;
                Camera.main.transform.position = Player.transform.position;
                camCol.enabled = true;
            }
        }
        IsSpawnBoss = true;
    }

    public void PlayerPause(bool isPause)
    {
        if (isPause)
        {
            if (Enum.TryParse(Player.input.currentActionMap.name, out m_originInputMap) == false)
            {
                m_originInputMap = InputActionMap.Player;
            }
            Player.SetActionMap(InputActionMap.Pause);
        }
        else
        {
            Player.SetActionMap(m_originInputMap);
            int boss = (int)EnemyType.Boss_1 + StageLevel.Value - 1;
            EnemySpawner.Spawn((EnemyType)boss, BossTeleport.transform.position);
        }
    }

    public void ClearBoss()
    {
        KillCount.Value = 0;
        IsSpawnBoss = false;
        if (StageLevel.Value >= ExitStage)
            UI.ChangeState(UIState.GameClear);
        else 
            UI.ChangeState(UIState.MidBossClear);
    }

    public void NextStage()
    {
        StageLevel.Value++;
        if (Player != null)
        {
            Player.transform.position = m_lastPlayerPos;
            var camCol = Player.GetComponentInChildren<CinemachineDecollider>();
            if (camCol != null)
            {
                camCol.enabled = false;
                Camera.main.transform.position = Player.transform.position;
                camCol.enabled = true;
            }
        }
        UI.ChangeState(UIState.None);
    }

    public void DiePlayer()
    {
        UI.DeadUI(DeadDuration);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void EndGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
