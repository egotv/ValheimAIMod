using UnityEngine;
using UnityEngine.Serialization;

public class Chair : MonoBehaviour, Hoverable, Interactable
{
	public string m_name = "Chair";

	public float m_useDistance = 2f;

	public Transform m_attachPoint;

	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	public string m_attachAnimation = "attach_chair";

	[FormerlySerializedAs("m_onShip")]
	public bool m_inShip;

	private const float m_minSitDelay = 2f;

	private static float m_lastSitTime;

	public string GetHoverText()
	{
		if (Time.time - m_lastSitTime < 2f)
		{
			return "";
		}
		if (!InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid human, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		Player player = human as Player;
		if (!InUseDistance(player))
		{
			return false;
		}
		if (Time.time - m_lastSitTime < 2f)
		{
			return false;
		}
		Player closestPlayer = Player.GetClosestPlayer(m_attachPoint.position, 0.1f);
		if (closestPlayer != null && closestPlayer != Player.m_localPlayer)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_blocked");
			return false;
		}
		if ((bool)player)
		{
			if (player.IsEncumbered())
			{
				return false;
			}
			player.AttachStart(m_attachPoint, null, hideWeapons: false, isBed: false, m_inShip, m_attachAnimation, m_detachOffset);
			m_lastSitTime = Time.time;
		}
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, m_attachPoint.position) < m_useDistance;
	}
}
