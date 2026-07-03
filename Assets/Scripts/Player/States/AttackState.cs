using UnityEngine;

public class AttackState : State
{
    private readonly string tagName = "Attack";
    private readonly int attackParam = Animator.StringToHash("Attack");
    private float enterTime;
    private CombatActionSO currentActionData;
    public AttackState(PlayerController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (ctrl.combat != null)
        {
            ctrl.combat.ClearComboReservation();
            var weapon = ctrl.combat.GetCurWeapon();
            if (weapon != null)
            {
                currentActionData = weapon.GetCurrentAttackAction();
                weapon.TryPlayCurrentAttackAnimation();
            }
        }
        else
        {
            ctrl.animator.SetTrigger(attackParam);
            currentActionData = null;
        }

        enterTime = Time.time;

    }

    public override void Update()
    {
        base.Update();
        if (Time.time - enterTime < 0.1f) return;

        var stateInfo = ctrl.animator.GetCurrentAnimatorStateInfo(0);
        bool inTransition = ctrl.animator.IsInTransition(0);


        if (!stateInfo.IsTag(tagName) && !inTransition)
        {
            ctrl.machine.ChangeState(StateType.Idle);
            return;
        }

        float cancelTime = currentActionData != null ? currentActionData.CancelAvailableNormalizedTime : 0.25f;
        float finishTime = currentActionData != null ? currentActionData.FinishNormalizedTime : 0.75f;

        // 콤보 예약 입력이 들어왔는지 판단 (예: 입력 버퍼 확인 변수)
        bool hasNextComboInput = ctrl.combat != null && ctrl.combat.HasComboReserved();
        if (hasNextComboInput)
        {
            // 예약 공격을 소모
            ctrl.combat.ClearComboReservation();
            // [상황 1] 콤보 입력이 있는 경우 -> 최소 캔슬 가능 시간만 지나면 즉시 다음 연계 실행!
            if (stateInfo.normalizedTime >= cancelTime)
            {
                var weapon = ctrl.combat.GetCurWeapon();
                if (weapon != null)
                {
                    weapon.Attack(); 
                    
                    // 다시 자기 자신(AttackState)의 Enter를 호출하여 애니메이션을 갱신합니다.
                    ctrl.machine.ChangeState(StateType.Attack); 
                }
            }
            return;
        }
        else
        {
            // [상황 2] 추가 공격 입력이 없는 경우 -> 피니시 시점까지 탈출 블록
            if (stateInfo.normalizedTime > finishTime)
                ctrl.machine.ChangeState(StateType.Idle);
            return;
        }
    }
}
