using UnityEngine;

public class TeleportWorldTrigger : MonoBehaviour
{
	private TeleportWorld m_teleportWorld;

	private void Awake()
	{
		m_teleportWorld = GetComponentInParent<TeleportWorld>();
	}

	private void OnTriggerEnter(Collider colliderIn)
	{
		Player component = colliderIn.GetComponent<Player>();
		if (!(component == null) && !(Player.m_localPlayer != component))
		{
			ZLog.Log("Teleportation TRIGGER");
			m_teleportWorld.Teleport(component);
		}
	}
}
