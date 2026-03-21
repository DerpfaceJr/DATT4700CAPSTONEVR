using UnityEngine;

/// <summary>
/// Add this behaviour to the Attack state in the Animator (State Machine → Attack state → Add Behaviour).
/// OnStateExit notifies EnemySeekAndAttack (which closes the damage window and clears attack state).
/// If there is no EnemySeekAndAttack, closes EnemyAttackDamageWindow only.
/// </summary>
public class EnemyAttackAnimatorExit : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator == null) return;
        EnemySeekAndAttack seek = animator.transform.GetComponentInParent<EnemySeekAndAttack>();
        if (seek != null)
            seek.NotifyAnimatorLeftAttackState();
        else
        {
            EnemyAttackDamageWindow window = animator.transform.GetComponentInParent<EnemyAttackDamageWindow>();
            if (window != null)
                window.CloseDamageWindow();
        }
    }
}
