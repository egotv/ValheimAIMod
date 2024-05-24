using System;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour, Hoverable, Interactable
{
	private enum TrapState
	{
		Unarmed,
		Armed,
		Active
	}

	public string m_name = "Trap";

	public GameObject m_AOE;

	public Collider m_trigger;

	public int m_rearmCooldown = 60;

	public GameObject m_visualArmed;

	public GameObject m_visualUnarmed;

	public bool m_triggeredByEnemies;

	public bool m_triggeredByPlayers;

	public bool m_forceStagger = true;

	public EffectList m_triggerEffects;

	public EffectList m_armEffects;

	public bool m_startsArmed;

	private ZNetView m_nview;

	private Aoe m_aoe;

	private Piece m_piece;

	private Humanoid m_tempTriggeringHumanoid;

	private Vector3 m_tempTriggeredPosition = Vector3.zero;

	private List<Action> m_onReceiveOwnershipActions = new List<Action>();

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_aoe = m_AOE.GetComponent<Aoe>();
		m_piece = GetComponent<Piece>();
		if (!m_aoe)
		{
			ZLog.LogError("Trap '" + base.gameObject.name + "' is missing AOE!");
		}
		m_aoe.gameObject.SetActive(value: false);
		if ((bool)m_nview)
		{
			m_nview.Register<int>("RPC_RequestStateChange", RPC_RequestStateChange);
			m_nview.Register<int, long>("RPC_OnStateChanged", RPC_OnStateChanged);
			if (m_startsArmed && m_nview.IsValid() && m_nview.GetZDO().GetInt(ZDOVars.s_state, -1) == -1)
			{
				m_nview.GetZDO().Set(ZDOVars.s_state, 1);
			}
			UpdateState();
		}
	}

	private void Update()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		if (IsActive() && !IsCoolingDown())
		{
			RequestStateChange(TrapState.Unarmed);
		}
		if (m_onReceiveOwnershipActions.Count > 0)
		{
			for (int i = 0; i < m_onReceiveOwnershipActions.Count; i++)
			{
				m_onReceiveOwnershipActions[i]?.Invoke();
			}
			m_onReceiveOwnershipActions.Clear();
		}
	}

	public bool IsArmed()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetInt(ZDOVars.s_state) == 1;
	}

	public bool IsActive()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetInt(ZDOVars.s_state) == 2;
	}

	public bool IsCoolingDown()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return (double)(m_nview.GetZDO().GetFloat(ZDOVars.s_triggered) + (float)m_rearmCooldown) > ZNet.instance.GetTimeSeconds();
	}

	public string GetHoverText()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(m_name + "\n$piece_noaccess");
		}
		if (IsArmed())
		{
			return Localization.instance.Localize(m_name + " ($piece_trap_armed)");
		}
		if (IsCoolingDown())
		{
			return Localization.instance.Localize(m_name);
		}
		return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_trap_arm");
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
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return true;
		}
		if (IsArmed())
		{
			return false;
		}
		if (IsCoolingDown())
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$piece_trap_cooldown"));
			return true;
		}
		RequestStateChange(TrapState.Armed);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void RequestStateChange(TrapState newState)
	{
		if (m_nview.IsValid())
		{
			if (m_nview.IsOwner())
			{
				RPC_RequestStateChange(ZNet.GetUID(), (int)newState);
				return;
			}
			m_nview.InvokeRPC("RPC_RequestStateChange", (int)newState);
		}
	}

	private void RPC_OnStateChanged(long uid, int value, long idOfClientModifyingState)
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		m_onReceiveOwnershipActions.Clear();
		switch (value)
		{
		case 1:
			if (idOfClientModifyingState == ZNet.GetUID())
			{
				m_armEffects.Create(base.transform.position, base.transform.rotation);
				Game.instance.IncrementPlayerStat(PlayerStatType.TrapArmed);
			}
			m_tempTriggeringHumanoid = null;
			break;
		case 2:
			if (idOfClientModifyingState != ZNet.GetUID())
			{
				break;
			}
			if (m_nview.IsOwner())
			{
				TriggerTrap();
			}
			else
			{
				m_onReceiveOwnershipActions.Add(TriggerTrap);
			}
			if (!(m_tempTriggeringHumanoid != null) || m_tempTriggeringHumanoid.GetZDOID().ID != Player.m_localPlayer.GetZDOID().ID)
			{
				break;
			}
			if (m_nview.IsOwner())
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.TrapTriggered);
				break;
			}
			m_onReceiveOwnershipActions.Add(delegate
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.TrapTriggered);
			});
			break;
		default:
			m_tempTriggeringHumanoid = null;
			break;
		}
		UpdateState((TrapState)value);
	}

	private void TriggerTrap()
	{
		if (m_tempTriggeringHumanoid != null)
		{
			m_tempTriggeringHumanoid.transform.position = m_tempTriggeredPosition;
			Physics.SyncTransforms();
			if (m_forceStagger)
			{
				m_tempTriggeringHumanoid.Stagger(Vector3.zero);
			}
		}
		m_nview.GetZDO().Set(ZDOVars.s_state, 2);
		m_nview.GetZDO().Set(ZDOVars.s_triggered, (float)ZNet.instance.GetTimeSeconds());
		UnityEngine.Object.Instantiate(m_aoe.gameObject, base.transform).SetActive(value: true);
		m_triggerEffects.Create(base.transform.position, base.transform.rotation);
	}

	private void RPC_RequestStateChange(long senderID, int value)
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		if (m_nview.GetZDO().GetInt(ZDOVars.s_state) != value)
		{
			if (value != 2)
			{
				m_nview.GetZDO().Set(ZDOVars.s_state, value);
			}
			else if (senderID != ZNet.GetUID())
			{
				m_nview.GetZDO().SetOwner(senderID);
			}
			m_nview.InvokeRPC(ZNetView.Everybody, "RPC_OnStateChanged", value, senderID);
		}
	}

	private void UpdateState(TrapState state)
	{
		m_piece.m_randomTarget = state == TrapState.Unarmed;
		m_visualArmed.SetActive(state == TrapState.Armed);
		m_visualUnarmed.SetActive(state != TrapState.Armed);
	}

	private void UpdateState()
	{
		if ((bool)m_nview && m_nview.IsValid())
		{
			TrapState @int = (TrapState)m_nview.GetZDO().GetInt(ZDOVars.s_state);
			UpdateState(@int);
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		Humanoid humanoid = null;
		Player componentInParent = collider.GetComponentInParent<Player>();
		if (componentInParent != null)
		{
			if (!m_triggeredByPlayers || componentInParent != Player.m_localPlayer)
			{
				return;
			}
		}
		else if (collider.GetComponentInParent<MonsterAI>() != null)
		{
			if (!m_triggeredByEnemies)
			{
				return;
			}
			humanoid = collider.GetComponentInParent<Humanoid>();
			if (humanoid != null && !humanoid.IsOwner())
			{
				return;
			}
		}
		if (IsArmed())
		{
			if (humanoid == null)
			{
				humanoid = collider.GetComponentInParent<Humanoid>();
			}
			if (humanoid != null)
			{
				m_tempTriggeringHumanoid = humanoid;
				m_tempTriggeredPosition = humanoid.transform.position;
			}
			RequestStateChange(TrapState.Active);
		}
	}
}
