using UnityEngine;

public class RopeAttachment : MonoBehaviour, Interactable, Hoverable
{
	public string m_name = "Rope";

	public string m_hoverText = "Pull";

	public float m_pullDistance = 5f;

	public float m_pullForce = 1f;

	public float m_maxPullVel = 1f;

	private Rigidbody m_boatBody;

	private Character m_puller;

	private void Awake()
	{
		m_boatBody = GetComponentInParent<Rigidbody>();
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if ((bool)m_puller)
		{
			m_puller = null;
			ZLog.Log("Detached rope");
		}
		else
		{
			m_puller = character;
			ZLog.Log("Attached rope");
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public string GetHoverText()
	{
		return m_hoverText;
	}

	public string GetHoverName()
	{
		return m_name;
	}

	private void FixedUpdate()
	{
		if ((bool)m_puller && Vector3.Distance(m_puller.transform.position, base.transform.position) > m_pullDistance)
		{
			Vector3 position = ((m_puller.transform.position - base.transform.position).normalized * m_maxPullVel - m_boatBody.GetPointVelocity(base.transform.position)) * m_pullForce;
			m_boatBody.AddForceAtPosition(base.transform.position, position);
		}
	}
}
