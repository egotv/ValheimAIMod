using UnityEngine;

public class AnimSetTrigger : StateMachineBehaviour
{
	public string TriggerOnEnter;

	public bool TriggerOnEnterEnable = true;

	public string TriggerOnExit;

	public bool TriggerOnExitEnable = true;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (!string.IsNullOrEmpty(TriggerOnEnter))
		{
			if (TriggerOnEnterEnable)
			{
				animator.SetTrigger(TriggerOnEnter);
			}
			else
			{
				animator.ResetTrigger(TriggerOnEnter);
			}
		}
	}

	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (!string.IsNullOrEmpty(TriggerOnExit))
		{
			if (TriggerOnExitEnable)
			{
				animator.SetTrigger(TriggerOnExit);
			}
			else
			{
				animator.ResetTrigger(TriggerOnExit);
			}
		}
	}
}
