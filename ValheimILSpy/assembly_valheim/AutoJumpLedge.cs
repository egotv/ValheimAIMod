using UnityEngine;

public class AutoJumpLedge : MonoBehaviour
{
	public bool m_forwardOnly = true;

	public float m_upVel = 1f;

	public float m_forwardVel = 1f;

	private void OnTriggerStay(Collider collider)
	{
		Character component = collider.GetComponent<Character>();
		if ((bool)component)
		{
			component.OnAutoJump(base.transform.forward, m_upVel, m_forwardVel);
		}
	}
}
