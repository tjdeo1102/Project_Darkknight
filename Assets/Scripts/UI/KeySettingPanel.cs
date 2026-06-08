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
    private InputAction m_rebindingAction;
    private InputActionRebindingExtensions.RebindingOperation m_rebindOperation;
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
            // Actions with the same name share the same binding count.
            if (actionKV.Value[0].bindings.Count < 2)
            {
                var sameBindings = actionKV.Value
                            .Select(action => action.bindings[0])
                            .ToList();
                SetBindingContent(actionKV.Value[0], sameBindings, actionKV.Key);
            }
            // Actions with multiple bindings expose each binding separately.
            else
            {
                var action = actionKV.Value[0];
                var bindings = action.bindings;
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var sameBindings = actionKV.Value
                            .Select(action => action.bindings[i])
                            .ToList();
                    // Composite parts use their own binding names.
                    if (bindings[i].isComposite) continue;
                    else if (bindings[i].isPartOfComposite)
                    {
                        SetBindingContent(action, sameBindings, bindings[i].name);
                    }
                    // Non-composite bindings are shown with an action index.
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

        texts[0].text = "    " + name;
        texts[0].fontSize = 25;
        texts[1].text = bindings[0].ToDisplayString();
        texts[1].fontSize = 25;

        btn.onClick.AddListener(() => RebindKey(action, bindings, texts[1]));
    }

    private void RebindKey(InputAction action, List<InputBinding> bindings, TMP_Text viewText)
    {
        CancelRebind();

        var idx = action.bindings.IndexOf(x => x.id == bindings[0].id);
        if (idx < 0) return;

        viewText.text = "Press Any Key...";
        m_rebindingAction = action;
        action.Disable();

        m_rebindOperation = action.PerformInteractiveRebinding(idx)
            .WithCancelingThrough("<Keyboard>/Escape")
            .OnCancel(o =>
            {
                viewText.text = bindings[0].ToDisplayString();
                FinishRebind(o);
            })
            .OnComplete(o =>
            {
                string newPath = o.selectedControl?.path;
                if (string.IsNullOrEmpty(newPath) == false)
                {
                    foreach (var binding in bindings)
                    {
                        var index = action.bindings.IndexOf(y => y.id == binding.id);
                        if (index != -1) action.ChangeBinding(index).WithPath(newPath);
                    }
                }

                viewText.text = action.GetBindingDisplayString(idx);
                FinishRebind(o);
            });

        m_rebindOperation.Start();
    }

    private void FinishRebind(InputActionRebindingExtensions.RebindingOperation operation)
    {
        operation?.Dispose();

        if (m_rebindingAction != null && m_rebindingAction.enabled == false)
            m_rebindingAction.Enable();

        if (m_rebindOperation == operation)
            m_rebindOperation = null;

        m_rebindingAction = null;
    }

    private void CancelRebind()
    {
        if (m_rebindOperation == null) return;

        var operation = m_rebindOperation;
        m_rebindOperation = null;
        operation.Cancel();
    }

    private void OnDisable()
    {
        CancelRebind();
    }

    private void OnDestroy()
    {
        CancelRebind();
        foreach (var btn in gameObject.GetComponentsInChildren<Button>())
        {
            btn.onClick.RemoveAllListeners(); 
        }
    }


}
