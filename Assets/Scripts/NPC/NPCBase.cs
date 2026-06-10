using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCBase : MonoBehaviour
{
    public NPCType Type;
    public Vector3 SpawnOffset = Vector3.up;
    public float ChunkRefreshDelay = 2f;
    public List<string> DialogTexts;
    public NavMeshAgent NavAgent;
    [Header("Max Count is 3")]
    public List<InventoryItem> SelectItems;

    private bool m_isTouch;
    private bool m_isInteract;
    private bool m_canSelectItem;
    private int m_textLine;
    private InGameLoop gameLoop;
    private ChunkManager m_chunkManager;
    private PlayerController m_player;
    private NPCDialog m_dialog;
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
        m_canSelectItem = true;
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

            if (gameLoop.UI != null && gameLoop.UI.UIElements.TryGetValue(UIState.Dialog, out var element))
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
            // 플레이어 향해 회전
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
        // Interact만 동작하도록 설정
        if (m_dialog == null || DialogTexts.Count < 1)
        {
            EndInteract();
        }
        else
        {
            m_textLine = 0;
            // Dialog UI 제작후, 컨트롤러에 등록하면, 해당 Dialog UI를 활성화

            gameLoop.UI.ChangeState(UIState.Dialog);
            m_dialog.Init(this);

            m_isInteract = true;

            Interact();
        }
    }

    public virtual void Interact()
    {
        if (m_dialog == null) return;

        // DIalog UI에서 ContinueDialog 메서드를 호출하며 순서대로 메세지 띄우기
        if (m_textLine < DialogTexts.Count -1)
        {
            m_dialog.ContinueDialog(DialogTexts[m_textLine],false);
        }
        else if (m_textLine == DialogTexts.Count -1)
        {
            if (m_canSelectItem)
            {
                m_dialog.SelectDialog(DialogTexts[m_textLine], SelectItems);
            }
            else
                m_dialog.ContinueDialog(DialogTexts[m_textLine], false);
        }
        else
        {
            if (m_canSelectItem == false)
                m_dialog.ContinueDialog("", true);
        }

        m_textLine++;
    }

    public virtual void EndInteract()
    {
        if (m_dialog == null) return;
        if (m_canSelectItem)
        {
            m_canSelectItem = false;
        }
        m_isInteract = false;
        m_dialog = null;
    }
    public void ChunkRefresh()
    {
        if (m_chunkManager == null) return;

        var chunk = m_chunkManager.GetChunk(transform.position);
        // 청크와 함께 관리
        if (chunk != null)
        {
            transform.parent = chunk.ChunkObject.transform;
        }
        // 정해진 청크 위치에 없는 적은 다시 반환
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
