using UnityEngine;

public class EventZone : MonoBehaviour
{
	public string m_event = "";

	private static EventZone m_triggered;

	private void OnTriggerStay(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (!(component == null) && !(Player.m_localPlayer != component))
		{
			m_triggered = this;
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (!(m_triggered != this))
		{
			Player component = collider.GetComponent<Player>();
			if (!(component == null) && !(Player.m_localPlayer != component))
			{
				m_triggered = null;
			}
		}
	}

	public static string GetEvent()
	{
		if ((bool)m_triggered && m_triggered.m_event.Length > 0)
		{
			return m_triggered.m_event;
		}
		return null;
	}
}
