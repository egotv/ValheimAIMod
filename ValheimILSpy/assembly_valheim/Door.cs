using UnityEngine;

public class Door : MonoBehaviour, Hoverable, Interactable
{
	public string m_name = "door";

	public ItemDrop m_keyItem;

	public bool m_canNotBeClosed;

	public bool m_invertedOpenClosedText;

	public bool m_checkGuardStone = true;

	public GameObject m_openEnable;

	public EffectList m_openEffects = new EffectList();

	public EffectList m_closeEffects = new EffectList();

	public EffectList m_lockedEffects = new EffectList();

	private ZNetView m_nview;

	private Animator m_animator;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			m_animator = GetComponentInChildren<Animator>();
			if ((bool)m_nview)
			{
				m_nview.Register<bool>("UseDoor", RPC_UseDoor);
			}
			InvokeRepeating("UpdateState", 0f, 0.2f);
		}
	}

	private void UpdateState()
	{
		if (m_nview.IsValid())
		{
			int @int = m_nview.GetZDO().GetInt(ZDOVars.s_state);
			SetState(@int);
		}
	}

	private void SetState(int state)
	{
		if (m_animator.GetInteger("state") != state)
		{
			if (state != 0)
			{
				m_openEffects.Create(base.transform.position, base.transform.rotation);
			}
			else
			{
				m_closeEffects.Create(base.transform.position, base.transform.rotation);
			}
			m_animator.SetInteger("state", state);
		}
		if ((bool)m_openEnable)
		{
			m_openEnable.SetActive(state != 0);
		}
	}

	private bool CanInteract()
	{
		if ((m_keyItem != null || m_canNotBeClosed) && m_nview.GetZDO().GetInt(ZDOVars.s_state) != 0)
		{
			return false;
		}
		if (!m_animator.GetCurrentAnimatorStateInfo(0).IsTag("open"))
		{
			return m_animator.GetCurrentAnimatorStateInfo(0).IsTag("closed");
		}
		return true;
	}

	public string GetHoverText()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		if (m_canNotBeClosed && !CanInteract())
		{
			return "";
		}
		if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(m_name + "\n$piece_noaccess");
		}
		if (CanInteract())
		{
			if (m_nview.GetZDO().GetInt(ZDOVars.s_state) != 0)
			{
				return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + (m_invertedOpenClosedText ? "$piece_door_open" : "$piece_door_close"));
			}
			return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + (m_invertedOpenClosedText ? "$piece_door_close" : "$piece_door_open"));
		}
		return Localization.instance.Localize(m_name);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!CanInteract())
		{
			return false;
		}
		if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position))
		{
			return true;
		}
		if (m_keyItem != null)
		{
			if (!HaveKey(character))
			{
				m_lockedEffects.Create(base.transform.position, base.transform.rotation);
				if (Game.m_worldLevel > 0 && HaveKey(character, matchWorldLevel: false))
				{
					character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_ng_the_x") + m_keyItem.m_itemData.m_shared.m_name + Localization.instance.Localize("$msg_ng_x_is_too_low"));
				}
				else
				{
					character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_needkey", m_keyItem.m_itemData.m_shared.m_name));
				}
				return true;
			}
			character.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_usingkey", m_keyItem.m_itemData.m_shared.m_name));
		}
		Vector3 normalized = (character.transform.position - base.transform.position).normalized;
		Game.instance.IncrementPlayerStat((m_nview.GetZDO().GetInt(ZDOVars.s_state) == 0) ? PlayerStatType.DoorsOpened : PlayerStatType.DoorsClosed);
		Open(normalized);
		return true;
	}

	private void Open(Vector3 userDir)
	{
		bool flag = Vector3.Dot(base.transform.forward, userDir) < 0f;
		m_nview.InvokeRPC("UseDoor", flag);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (m_keyItem != null && m_keyItem.m_itemData.m_shared.m_name == item.m_shared.m_name)
		{
			if (!CanInteract())
			{
				return false;
			}
			if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position))
			{
				return true;
			}
			user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_door_usingkey", m_keyItem.m_itemData.m_shared.m_name));
			Vector3 normalized = (user.transform.position - base.transform.position).normalized;
			Open(normalized);
			return true;
		}
		return false;
	}

	private bool HaveKey(Humanoid player, bool matchWorldLevel = true)
	{
		if (m_keyItem == null)
		{
			return true;
		}
		return player.GetInventory().HaveItem(m_keyItem.m_itemData.m_shared.m_name, matchWorldLevel);
	}

	private void RPC_UseDoor(long uid, bool forward)
	{
		if (!CanInteract())
		{
			return;
		}
		if (m_nview.GetZDO().GetInt(ZDOVars.s_state) == 0)
		{
			if (forward)
			{
				m_nview.GetZDO().Set(ZDOVars.s_state, 1);
			}
			else
			{
				m_nview.GetZDO().Set(ZDOVars.s_state, -1);
			}
		}
		else
		{
			m_nview.GetZDO().Set(ZDOVars.s_state, 0);
		}
		UpdateState();
	}
}
