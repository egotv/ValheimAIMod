using UnityEngine;

public class TeleportHome : MonoBehaviour
{
	private void OnTriggerEnter(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (!(component == null) && !(Player.m_localPlayer != component))
		{
			Game.instance.RequestRespawn(0f);
		}
	}
}
