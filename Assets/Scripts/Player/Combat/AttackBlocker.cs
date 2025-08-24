using UnityEngine;

public class AttackBlocker : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var combat = animator.GetComponent<PlayerCombat>() ?? animator.GetComponentInChildren<PlayerCombat>();
        if (combat) combat.isPunching = true;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var combat = animator.GetComponent<PlayerCombat>() ?? animator.GetComponentInChildren<PlayerCombat>();
        if (combat) combat.isPunching = false;
    }
}
