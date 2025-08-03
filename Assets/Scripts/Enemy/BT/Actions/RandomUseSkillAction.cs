using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Random = UnityEngine.Random;
using System.Collections.Generic;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RandomUseSkill", story: "[animator] set Rrandom [SkillParameter] in [MaxCount]", category: "Action", id: "bc2daad3a997915b145121bff133d195")]
public partial class RandomUseSkillAction : Action
{
    [SerializeReference] public BlackboardVariable<Animator> animator;
    [SerializeReference] public BlackboardVariable<string> SkillParameter;
    [SerializeReference] public BlackboardVariable<int> MaxCount;
    [SerializeReference] public BlackboardVariable<List<float>> SkillDelayList;
    [SerializeReference] public BlackboardVariable<float> SkillDelay;

    private int paramHash;
    protected override Status OnStart()
    {
        paramHash = Animator.StringToHash(SkillParameter.Value);

        var sel = Random.Range(-4, MaxCount.Value);
        if (sel > -1 && sel < SkillDelayList.Value.Count) SkillDelay.Value = SkillDelayList.Value[sel];
        else SkillDelay.Value = 0.2f;

        animator.Value.SetInteger(paramHash, sel);

        return Status.Running;
    }
}

