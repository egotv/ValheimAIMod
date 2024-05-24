using UnityEngine;

public class WayStone : MonoBehaviour, Hoverable, Interactable
{
	[TextArea]
	public string m_activateMessage = "You touch the cold stone surface and you think of home.";

	public GameObject m_activeObject;

	public EffectList m_activeEffect;

	private void Awake()
	{
		m_activeObject.SetActive(value: false);
	}

	public string GetHoverText()
	{
		if (m_activeObject.activeSelf)
		{
			return "Activated waystone";
		}
		return Localization.instance.Localize("Waystone\n[<color=yellow><b>$KEY_Use</b></color>] Activate");
	}

	public string GetHoverName()
	{
		return "Waystone";
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!m_activeObject.activeSelf)
		{
			character.Message(MessageHud.MessageType.Center, m_activateMessage);
			m_activeObject.SetActive(value: true);
			m_activeEffect.Create(base.gameObject.transform.position, base.gameObject.transform.rotation);
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void FixedUpdate()
	{
		if (m_activeObject.activeSelf && Game.instance != null)
		{
			Vector3 forward = GetSpawnPoint() - base.transform.position;
			forward.y = 0f;
			forward.Normalize();
			m_activeObject.transform.rotation = Quaternion.LookRotation(forward);
		}
	}

	private Vector3 GetSpawnPoint()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		if (playerProfile.HaveCustomSpawnPoint())
		{
			return playerProfile.GetCustomSpawnPoint();
		}
		return playerProfile.GetHomePoint();
	}
}
