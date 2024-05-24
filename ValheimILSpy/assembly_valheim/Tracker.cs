using UnityEngine;

public class Tracker : MonoBehaviour
{
	private bool m_active;

	private void Awake()
	{
		ZNetView component = GetComponent<ZNetView>();
		if ((bool)component && component.IsOwner())
		{
			m_active = true;
			ZNet.instance.SetReferencePosition(base.transform.position);
		}
	}

	public void SetActive(bool active)
	{
		m_active = active;
	}

	private void OnDestroy()
	{
		m_active = false;
	}

	private void FixedUpdate()
	{
		if (m_active)
		{
			ZNet.instance.SetReferencePosition(base.transform.position);
		}
	}
}
