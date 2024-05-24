using UnityEngine;

public class Teleport : MonoBehaviour, Hoverable, Interactable
{
	public string m_hoverText = "$location_enter";

	public string m_enterText = "";

	public Teleport m_targetPoint;

	public string GetHoverText()
	{
		return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] " + m_hoverText);
	}

	public string GetHoverName()
	{
		return "";
	}

	private void OnTriggerEnter(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (!(component == null) && !(Player.m_localPlayer != component))
		{
			Interact(component, hold: false, alt: false);
		}
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (m_targetPoint == null)
		{
			return false;
		}
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBossPortals) && character.InInterior() && Location.IsInsideActiveBossDungeon(character.transform.position))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_blockedbyboss");
			return false;
		}
		if (character.TeleportTo(m_targetPoint.GetTeleportPoint(), m_targetPoint.transform.rotation, distantTeleport: false))
		{
			Game.instance.IncrementPlayerStat(character.InInterior() ? PlayerStatType.PortalDungeonOut : PlayerStatType.PortalDungeonIn);
			if (m_enterText.Length > 0)
			{
				MessageHud.instance.ShowBiomeFoundMsg(m_enterText, playStinger: false);
			}
			return true;
		}
		return false;
	}

	private Vector3 GetTeleportPoint()
	{
		return base.transform.position + base.transform.forward - base.transform.up;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void OnDrawGizmos()
	{
	}
}
