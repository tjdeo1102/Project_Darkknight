using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NPCDialog : UIElementBase
{
    [Serializable]
    public class ButtonContent
    {
        public Button btn;
        public TextMeshProUGUI textMesh;
        [HideInInspector] public InventoryItem selctItem;
    }
    [SerializeField] private TextMeshProUGUI mainTextMesh;
    [SerializeField] private List<ButtonContent> select;

    private NPCBase m_interactNPC;

    private void OnEnable()
    {
        Controller.ChangeState(UIState.Dialog);

        if (select != null && select.Count > 3)
        {
            select.RemoveRange(3, select.Count - 3);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        foreach (var item in select)
        {
            item.btn.onClick.RemoveAllListeners();
        }
    }

    public void Init(NPCBase npc)
    {
        m_interactNPC = npc;
        foreach (var item in select)
        {
            item.btn.onClick.AddListener(() => EndInteract(item.selctItem));
        }
    }

    public void ContinueDialog(string message,bool isEnd)
    {
        mainTextMesh.text = message;
        if(isEnd) EndInteract(null);
    }

    public void SelectDialog(string message, List<InventoryItem> selectItems)
    {
        mainTextMesh.text = message;
        for (int i = 0; i < selectItems.Count; i++)
        {
            select[i].textMesh.text = $"{selectItems[i].Name} - {selectItems[i].Price}$";
            select[i].selctItem = selectItems[i];
            select[i].btn.gameObject.SetActive(true);
        }
    }

    private void EndInteract(InventoryItem selectItem)
    {
        if (selectItem != null)
        {
            var player = Controller.Player;
            // 결제
            if (player != null
                && player.model.Stats[StatType.Money].TotalValue < selectItem.Price)
            {
                mainTextMesh.text = "빈손으로 또 왔군. 이번에도 거래는 없어.";
            }
            else
            {
                player.model.Stats[StatType.Money].AddModifier(new StatModifier(-selectItem.Price), StatModifyType.BuyItem);

                // 아이템 생성해서 넣기
                if (Controller.UIElements.TryGetValue(UIState.Inventory,out var element))
                {
                    var inventory = element as InventorySystem;
                    inventory?.TryAddItem(Instantiate(selectItem));
                    mainTextMesh.text = "거래는 끝났소. 행운을 빌지, 모험가.";
                }
            }
        }
        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true) // Time.timeScale 무시
           .AppendInterval(2f)
           .OnComplete(() =>
           {
               m_interactNPC.EndInteract();
               foreach (var item in select)
               {
                   item.btn.gameObject.SetActive(false);
               }
               gameObject.SetActive(false);
           });
    }
}
