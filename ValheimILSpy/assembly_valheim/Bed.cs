using UnityEngine;

public class Bed : MonoBehaviour, Hoverable, Interactable
{
	public Transform m_spawnPoint;

	public float m_monsterCheckRadius = 20f;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			m_nview.Register<long, string>("SetOwner", RPC_SetOwner);
		}
	}

	public string GetHoverText()
	{
		string ownerName = GetOwnerName();
		if (ownerName == "")
		{
			return Localization.instance.Localize("$piece_bed_unclaimed\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_claim");
		}
		string text = ownerName + "'s $piece_bed";
		if (IsMine())
		{
			if (IsCurrent())
			{
				return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_sleep");
			}
			return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_bed_setspawn");
		}
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize("$piece_bed");
	}

	public bool Interact(Humanoid human, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (m_nview.GetZDO() == null)
		{
			return false;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		long owner = GetOwner();
		Player human2 = human as Player;
		if (owner == 0L)
		{
			ZLog.Log("Has no creator");
			if (!CheckExposure(human2))
			{
				return false;
			}
			SetOwner(playerID, Game.instance.GetPlayerProfile().GetName());
			Game.instance.GetPlayerProfile().SetCustomSpawnPoint(GetSpawnPoint());
			human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset");
		}
		else if (IsMine())
		{
			ZLog.Log("Is mine");
			if (IsCurrent())
			{
				ZLog.Log("is current spawnpoint");
				if (!EnvMan.CanSleep())
				{
					human.Message(MessageHud.MessageType.Center, "$msg_cantsleep");
					return false;
				}
				if (!CheckEnemies(human2))
				{
					return false;
				}
				if (!CheckExposure(human2))
				{
					return false;
				}
				if (!CheckFire(human2))
				{
					return false;
				}
				if (!CheckWet(human2))
				{
					return false;
				}
				human.AttachStart(m_spawnPoint, base.gameObject, hideWeapons: true, isBed: true, onShip: false, "attach_bed", new Vector3(0f, 0.5f, 0f));
				return false;
			}
			ZLog.Log("Not current spawn point");
			if (!CheckExposure(human2))
			{
				return false;
			}
			Game.instance.GetPlayerProfile().SetCustomSpawnPoint(GetSpawnPoint());
			human.Message(MessageHud.MessageType.Center, "$msg_spawnpointset");
		}
		return false;
	}

	private bool CheckWet(Player human)
	{
		if (human.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectWet))
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedwet");
			return false;
		}
		return true;
	}

	private bool CheckEnemies(Player human)
	{
		if (human.IsSensed())
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedenemiesnearby");
			return false;
		}
		return true;
	}

	private bool CheckExposure(Player human)
	{
		Cover.GetCoverForPoint(GetSpawnPoint(), out var coverPercentage, out var underRoof);
		if (!underRoof)
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedneedroof");
			return false;
		}
		if (coverPercentage < 0.8f)
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bedtooexposed");
			return false;
		}
		ZLog.Log("exporeusre check " + coverPercentage + "  " + underRoof);
		return true;
	}

	private bool CheckFire(Player human)
	{
		if (!EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Heat))
		{
			human.Message(MessageHud.MessageType.Center, "$msg_bednofire");
			return false;
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public bool IsCurrent()
	{
		if (!IsMine())
		{
			return false;
		}
		return Vector3.Distance(GetSpawnPoint(), Game.instance.GetPlayerProfile().GetCustomSpawnPoint()) < 1f;
	}

	public Vector3 GetSpawnPoint()
	{
		return m_spawnPoint.position;
	}

	private bool IsMine()
	{
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		long owner = GetOwner();
		return playerID == owner;
	}

	private void SetOwner(long uid, string name)
	{
		m_nview.InvokeRPC("SetOwner", uid, name);
	}

	private void RPC_SetOwner(long sender, long uid, string name)
	{
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(ZDOVars.s_owner, uid);
			m_nview.GetZDO().Set(ZDOVars.s_ownerName, name);
		}
	}

	private long GetOwner()
	{
		if (m_nview.GetZDO() == null)
		{
			return 0L;
		}
		return m_nview.GetZDO().GetLong(ZDOVars.s_owner, 0L);
	}

	private string GetOwnerName()
	{
		if (m_nview.GetZDO() == null)
		{
			return "";
		}
		if (!IsMine())
		{
			return CensorShittyWords.FilterUGC(m_nview.GetZDO().GetString(ZDOVars.s_ownerName), UGCType.CharacterName, null, GetOwner());
		}
		return m_nview.GetZDO().GetString(ZDOVars.s_ownerName);
	}
}
