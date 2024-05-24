using UnityEngine;

public class StateController : StateMachineBehaviour
{
	public string m_effectJoint = "";

	public EffectList m_enterEffect = new EffectList();

	public bool m_enterDisableChildren;

	public bool m_enterEnableChildren;

	public GameObject[] m_enterDisable = new GameObject[0];

	public GameObject[] m_enterEnable = new GameObject[0];

	private Transform m_effectJoinT;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if (m_enterEffect.HasEffects())
		{
			m_enterEffect.Create(GetEffectPos(animator), animator.transform.rotation);
		}
		if (m_enterDisableChildren)
		{
			for (int i = 0; i < animator.transform.childCount; i++)
			{
				animator.transform.GetChild(i).gameObject.SetActive(value: false);
			}
		}
		if (m_enterEnableChildren)
		{
			for (int j = 0; j < animator.transform.childCount; j++)
			{
				animator.transform.GetChild(j).gameObject.SetActive(value: true);
			}
		}
	}

	private Vector3 GetEffectPos(Animator animator)
	{
		if (m_effectJoint.Length == 0)
		{
			return animator.transform.position;
		}
		if (m_effectJoinT == null)
		{
			m_effectJoinT = Utils.FindChild(animator.transform, m_effectJoint);
		}
		return m_effectJoinT.position;
	}
}
