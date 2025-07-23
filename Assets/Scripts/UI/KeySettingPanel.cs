using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Button = UnityEngine.UI.Button;

public class KeySettingPanel : MonoBehaviour
{
    public InputActionAsset InputActions;
    public Transform Content;
    public TextMeshProUGUI SettingTextPref;
    public Transform SettingButtonPref;

    public PlayerController PlayerCtrl;
    private Dictionary<string, List<InputAction>> actionDic;
    private void Awake()
    {
        InputActions = PlayerCtrl.input.actions; 
        actionDic = new();
        InitKeySetting();
    }

    private void InitKeySetting()
    {
        foreach (var map in InputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                if (actionDic.ContainsKey(action.name) == false) actionDic.Add(action.name, new List<InputAction>());
                actionDic[action.name].Add(action);
            }
        }

        foreach (var actionKV in actionDic)
        {
            var actionText = Instantiate(SettingTextPref, Content);
            actionText.text = actionKV.Key;
            actionText.fontSize = 30;
            // 같은 액션끼리 바인딩 개수는 같음
            if (actionKV.Value[0].bindings.Count < 2)
            {
                var sameBindings = actionKV.Value
                            .Select(action => action.bindings[0])
                            .ToList();
                SetBindingContent(actionKV.Value[0], sameBindings, actionKV.Key);
            }
            // 다수 바인딩으로 엮인 액션
            else
            {
                var action = actionKV.Value[0];
                var bindings = action.bindings;
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var sameBindings = actionKV.Value
                            .Select(action => action.bindings[i])
                            .ToList();
                    // Vector2와 같은 컴포사이트 바인딩은 고유 바인딩 이름으로 표현
                    if (bindings[i].isComposite) continue;
                    else if (bindings[i].isPartOfComposite)
                    {
                        SetBindingContent(action, sameBindings, bindings[i].name);
                    }
                    // 액션 + 인덱스 표현
                    else
                    {
                        SetBindingContent(action, sameBindings, action.name + i);
                    }
                }
            }
        }
    }

    private void SetBindingContent(InputAction action, List<InputBinding> bindings, string name)
    {
        var bindingText = Instantiate(SettingButtonPref, Content);
        var texts = bindingText.GetComponentsInChildren<TextMeshProUGUI>();
        var btn = bindingText.GetComponentInChildren<Button>();

        texts[0].text = "    ㄴ" + name;
        texts[0].fontSize = 25;
        texts[1].text = bindings[0].ToDisplayString();
        texts[1].fontSize = 25;

        btn.onClick.AddListener(() => RebindKey(action, bindings, texts[1]));
    }

    private void RebindKey(InputAction action,List<InputBinding> bindings, TMP_Text viewText)
    {
        var idx = action.bindings.IndexOf(x => x.id == bindings[0].id);

        viewText.text = "Press Any Key...";
        action.Disable();

        action.PerformInteractiveRebinding(idx)
            .WithCancelingThrough("<Mouse>/leftButton")
            .WithCancelingThrough("<Keyboard>/Escape")
            .OnCancel(o =>
            {
                o.Dispose();
                action.Enable();
                viewText.text = bindings[0].ToDisplayString();
            })
            .OnComplete(o =>
            {
                string newPath = o.selectedControl?.path;
                // 동시에 모든 같은 키 전부 변경 적용
                foreach (var binding in bindings)
                {
                    var index = action.bindings.IndexOf(y => y.id == binding.id);
                    if (index != -1) action.ChangeBinding(index).WithPath(newPath);
                }
                o.Dispose();
                action.Enable();
                viewText.text = action.GetBindingDisplayString(idx);
            })
            .Start();
    }

    private void OnDestroy()
    {
        foreach (var btn in gameObject.GetComponentsInChildren<Button>())
        {
            btn.onClick.RemoveAllListeners(); 
        }
    }


}
