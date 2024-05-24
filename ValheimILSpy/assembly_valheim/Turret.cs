using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Turret : MonoBehaviour, Hoverable, Interactable, IPieceMarker
{
	[Serializable]
	public struct AmmoType
	{
		public ItemDrop m_ammo;

		public GameObject m_visual;
	}

	[Serializable]
	public struct TrophyTarget
	{
		public string m_nameOverride;

		public ItemDrop m_item;

		public List<Character> m_targets;
	}

	public string m_name = "Turret";

	[Header("Turret")]
	public GameObject m_turretBody;

	public GameObject m_turretBodyArmed;

	public GameObject m_turretBodyUnarmed;

	public GameObject m_turretNeck;

	public GameObject m_eye;

	[Header("Look & Scan")]
	public float m_turnRate = 10f;

	public float m_horizontalAngle = 25f;

	public float m_verticalAngle = 20f;

	public float m_viewDistance = 10f;

	public float m_noTargetScanRate = 10f;

	public float m_lookAcceleration = 1.2f;

	public float m_lookDeacceleration = 0.05f;

	public float m_lookMinDegreesDelta = 0.005f;

	[Header("Attack Settings (rest in projectile)")]
	public ItemDrop m_defaultAmmo;

	public float m_attackCooldown = 1f;

	public float m_attackWarmup = 1f;

	public float m_hitNoise = 10f;

	public float m_shootWhenAimDiff = 0.9f;

	public float m_predictionModifier = 1f;

	public float m_updateTargetIntervalNear = 1f;

	public float m_updateTargetIntervalFar = 10f;

	[Header("Ammo")]
	public int m_maxAmmo;

	public string m_ammoType = "$ammo_turretbolt";

	public List<AmmoType> m_allowedAmmo = new List<AmmoType>();

	public bool m_returnAmmoOnDestroy = true;

	public float m_holdRepeatInterval = 0.2f;

	[Header("Target mode: Everything")]
	public bool m_targetPlayers = true;

	public bool m_targetTamed = true;

	public bool m_targetEnemies = true;

	[Header("Target mode: Configured")]
	public bool m_targetTamedConfig;

	public List<TrophyTarget> m_configTargets = new List<TrophyTarget>();

	public int m_maxConfigTargets = 1;

	[Header("Effects")]
	public CircleProjector m_marker;

	public float m_markerHideTime = 0.5f;

	public EffectList m_shootEffect;

	public EffectList m_addAmmoEffect;

	public EffectList m_reloadEffect;

	public EffectList m_warmUpStartEffect;

	public EffectList m_newTargetEffect;

	public EffectList m_lostTargetEffect;

	public EffectList m_setTargetEffect;

	private ZNetView m_nview;

	private GameObject m_lastProjectile;

	private ItemDrop.ItemData m_lastAmmo;

	private Character m_target;

	private bool m_haveTarget;

	private Quaternion m_baseBodyRotation;

	private Quaternion m_baseNeckRotation;

	private Quaternion m_lastRotation;

	private float m_aimDiffToTarget;

	private float m_updateTargetTimer;

	private float m_lastUseTime;

	private float m_scan;

	private readonly List<ItemDrop> m_targetItems = new List<ItemDrop>();

	private readonly List<Character> m_targetCharacters = new List<Character>();

	private string m_targetsText;

	private readonly StringBuilder sb = new StringBuilder();

	private uint m_lastUpdateTargetRevision = uint.MaxValue;

	protected void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if ((bool)m_nview)
		{
			m_nview.Register<string>("RPC_AddAmmo", RPC_AddAmmo);
			m_nview.Register<ZDOID>("RPC_SetTarget", RPC_SetTarget);
		}
		m_updateTargetTimer = UnityEngine.Random.Range(0f, m_updateTargetIntervalNear);
		m_baseBodyRotation = m_turretBody.transform.localRotation;
		m_baseNeckRotation = m_turretNeck.transform.localRotation;
		WearNTear component = GetComponent<WearNTear>();
		if ((object)component != null)
		{
			component.m_onDestroyed = (Action)Delegate.Combine(component.m_onDestroyed, new Action(OnDestroyed));
		}
		if ((bool)m_marker)
		{
			m_marker.m_radius = m_viewDistance;
			m_marker.gameObject.SetActive(value: false);
		}
		foreach (AmmoType item in m_allowedAmmo)
		{
			item.m_visual.SetActive(value: false);
		}
		if ((bool)m_nview && m_nview.IsValid())
		{
			UpdateVisualBolt();
		}
		ReadTargets();
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		UpdateReloadState();
		UpdateMarker(fixedDeltaTime);
		if (!m_nview.IsValid())
		{
			return;
		}
		UpdateTurretRotation();
		UpdateVisualBolt();
		if (!m_nview.IsOwner())
		{
			if (m_nview.IsValid() && m_lastUpdateTargetRevision != m_nview.GetZDO().DataRevision)
			{
				m_lastUpdateTargetRevision = m_nview.GetZDO().DataRevision;
				ReadTargets();
			}
		}
		else
		{
			UpdateTarget(fixedDeltaTime);
			UpdateAttack(fixedDeltaTime);
		}
	}

	private void UpdateTurretRotation()
	{
		if (IsCoolingDown())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		bool flag = (bool)m_target && HasAmmo();
		Vector3 forward;
		if (flag)
		{
			if (m_lastAmmo == null)
			{
				m_lastAmmo = GetAmmoItem();
			}
			if (m_lastAmmo == null)
			{
				ZLog.LogWarning("Turret had invalid ammo, resetting ammo");
				m_nview.GetZDO().Set(ZDOVars.s_ammo, 0);
				return;
			}
			float num = Vector2.Distance(m_target.transform.position, m_eye.transform.position) / m_lastAmmo.m_shared.m_attack.m_projectileVel;
			Vector3 vector = m_target.GetVelocity() * num * m_predictionModifier;
			forward = m_target.transform.position + vector - m_turretBody.transform.position;
			ref float y = ref forward.y;
			float num2 = y;
			CapsuleCollider componentInChildren = m_target.GetComponentInChildren<CapsuleCollider>();
			y = num2 + (((object)componentInChildren != null) ? (componentInChildren.height / 2f) : 1f);
		}
		else if (!HasAmmo())
		{
			forward = base.transform.forward + new Vector3(0f, -0.3f, 0f);
		}
		else
		{
			m_scan += fixedDeltaTime;
			if (m_scan > m_noTargetScanRate * 2f)
			{
				m_scan = 0f;
			}
			forward = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y + (float)((m_scan - m_noTargetScanRate > 0f) ? 1 : (-1)) * m_horizontalAngle, 0f) * Vector3.forward;
		}
		forward.Normalize();
		Quaternion quaternion = Quaternion.LookRotation(forward, Vector3.up);
		Vector3 eulerAngles = quaternion.eulerAngles;
		float y2 = base.transform.rotation.eulerAngles.y;
		eulerAngles.y -= y2;
		if (m_horizontalAngle >= 0f)
		{
			float num3 = eulerAngles.y;
			if (num3 > 180f)
			{
				num3 -= 360f;
			}
			else if (num3 < -180f)
			{
				num3 += 360f;
			}
			if (num3 > m_horizontalAngle)
			{
				eulerAngles = new Vector3(eulerAngles.x, m_horizontalAngle + y2, eulerAngles.z);
				quaternion.eulerAngles = eulerAngles;
			}
			else if (num3 < 0f - m_horizontalAngle)
			{
				eulerAngles = new Vector3(eulerAngles.x, 0f - m_horizontalAngle + y2, eulerAngles.z);
				quaternion.eulerAngles = eulerAngles;
			}
		}
		Quaternion quaternion2 = Utils.RotateTorwardsSmooth(m_turretBody.transform.rotation, quaternion, m_lastRotation, m_turnRate * fixedDeltaTime, m_lookAcceleration, m_lookDeacceleration, m_lookMinDegreesDelta);
		m_lastRotation = m_turretBody.transform.rotation;
		m_turretBody.transform.rotation = m_baseBodyRotation * quaternion2;
		m_turretNeck.transform.rotation = m_baseNeckRotation * Quaternion.Euler(0f, m_turretBody.transform.rotation.eulerAngles.y, m_turretBody.transform.rotation.eulerAngles.z);
		m_aimDiffToTarget = (flag ? Quaternion.Dot(quaternion2, quaternion) : (-1f));
	}

	private void UpdateTarget(float dt)
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		if (!HasAmmo())
		{
			if (m_haveTarget)
			{
				m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetTarget", ZDOID.None);
			}
			return;
		}
		m_updateTargetTimer -= dt;
		if (m_updateTargetTimer <= 0f)
		{
			m_updateTargetTimer = (Character.IsCharacterInRange(base.transform.position, 40f) ? m_updateTargetIntervalNear : m_updateTargetIntervalFar);
			Character character = BaseAI.FindClosestCreature(base.transform, m_eye.transform.position, 0f, m_viewDistance, m_horizontalAngle, alerted: false, mistVision: false, passiveAggresive: true, m_targetPlayers, (m_targetItems.Count > 0) ? m_targetTamedConfig : m_targetTamed, m_targetEnemies, m_targetCharacters);
			if (character != m_target)
			{
				if ((bool)character)
				{
					m_newTargetEffect.Create(base.transform.position, base.transform.rotation);
				}
				else
				{
					m_lostTargetEffect.Create(base.transform.position, base.transform.rotation);
				}
				m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetTarget", character ? character.GetZDOID() : ZDOID.None);
			}
		}
		if (m_haveTarget && (!m_target || m_target.IsDead()))
		{
			ZLog.Log("Target is gone");
			m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetTarget", ZDOID.None);
			m_lostTargetEffect.Create(base.transform.position, base.transform.rotation);
		}
	}

	private void UpdateAttack(float dt)
	{
		if ((bool)m_target && !(m_aimDiffToTarget < m_shootWhenAimDiff) && HasAmmo() && !IsCoolingDown())
		{
			ShootProjectile();
		}
	}

	public void ShootProjectile()
	{
		Transform transform = m_eye.transform;
		m_shootEffect.Create(transform.position, transform.rotation);
		m_nview.GetZDO().Set(ZDOVars.s_lastAttack, (float)ZNet.instance.GetTimeSeconds());
		m_lastAmmo = GetAmmoItem();
		int @int = m_nview.GetZDO().GetInt(ZDOVars.s_ammo);
		int num = Mathf.Min(1, (m_maxAmmo == 0) ? m_lastAmmo.m_shared.m_attack.m_projectiles : Mathf.Min(@int, m_lastAmmo.m_shared.m_attack.m_projectiles));
		if (m_maxAmmo > 0)
		{
			m_nview.GetZDO().Set(ZDOVars.s_ammo, @int - num);
		}
		ZLog.Log($"Turret '{base.name}' is shooting {num} projectiles, ammo: {@int}/{m_maxAmmo}");
		for (int i = 0; i < num; i++)
		{
			Vector3 forward = transform.forward;
			Vector3 axis = Vector3.Cross(forward, Vector3.up);
			float projectileAccuracy = m_lastAmmo.m_shared.m_attack.m_projectileAccuracy;
			Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - projectileAccuracy, projectileAccuracy), Vector3.up);
			forward = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - projectileAccuracy, projectileAccuracy), axis) * forward;
			forward = quaternion * forward;
			m_lastProjectile = UnityEngine.Object.Instantiate(m_lastAmmo.m_shared.m_attack.m_attackProjectile, transform.position, transform.rotation);
			HitData hitData = new HitData();
			hitData.m_toolTier = (short)m_lastAmmo.m_shared.m_toolTier;
			hitData.m_pushForce = m_lastAmmo.m_shared.m_attackForce;
			hitData.m_backstabBonus = m_lastAmmo.m_shared.m_backstabBonus;
			hitData.m_staggerMultiplier = m_lastAmmo.m_shared.m_attack.m_staggerMultiplier;
			hitData.m_damage.Add(m_lastAmmo.GetDamage());
			hitData.m_statusEffectHash = (m_lastAmmo.m_shared.m_attackStatusEffect ? m_lastAmmo.m_shared.m_attackStatusEffect.NameHash() : 0);
			hitData.m_blockable = m_lastAmmo.m_shared.m_blockable;
			hitData.m_dodgeable = m_lastAmmo.m_shared.m_dodgeable;
			hitData.m_skill = m_lastAmmo.m_shared.m_skillType;
			hitData.m_itemWorldLevel = (byte)Game.m_worldLevel;
			hitData.m_hitType = HitData.HitType.Turret;
			if (m_lastAmmo.m_shared.m_attackStatusEffect != null)
			{
				hitData.m_statusEffectHash = m_lastAmmo.m_shared.m_attackStatusEffect.NameHash();
			}
			m_lastProjectile.GetComponent<IProjectile>()?.Setup(null, forward * m_lastAmmo.m_shared.m_attack.m_projectileVel, m_hitNoise, hitData, null, m_lastAmmo);
		}
	}

	public bool IsCoolingDown()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return (double)(m_nview.GetZDO().GetFloat(ZDOVars.s_lastAttack) + m_attackCooldown) > ZNet.instance.GetTimeSeconds();
	}

	public bool HasAmmo()
	{
		if (m_maxAmmo != 0)
		{
			return GetAmmo() > 0;
		}
		return true;
	}

	public int GetAmmo()
	{
		return m_nview.GetZDO().GetInt(ZDOVars.s_ammo);
	}

	public string GetAmmoType()
	{
		if (!m_defaultAmmo)
		{
			return m_nview.GetZDO().GetString(ZDOVars.s_ammoType);
		}
		return m_defaultAmmo.name;
	}

	public void UpdateReloadState()
	{
		bool flag = IsCoolingDown();
		if (!m_turretBodyArmed.activeInHierarchy && !flag)
		{
			m_reloadEffect.Create(base.transform.position, base.transform.rotation);
		}
		m_turretBodyArmed.SetActive(!flag);
		m_turretBodyUnarmed.SetActive(flag);
	}

	private ItemDrop.ItemData GetAmmoItem()
	{
		string ammoType = GetAmmoType();
		GameObject prefab = ZNetScene.instance.GetPrefab(ammoType);
		if (!prefab)
		{
			ZLog.LogWarning("Turret '" + base.name + "' is trying to fire but has no ammo or default ammo!");
			return null;
		}
		return prefab.GetComponent<ItemDrop>().m_itemData;
	}

	public string GetHoverText()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		if (!m_targetEnemies)
		{
			return Localization.instance.Localize(m_name);
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(m_name + "\n$piece_noaccess");
		}
		sb.Clear();
		sb.Append((!HasAmmo()) ? (m_name + " ($piece_turret_noammo)") : $"{m_name} ({GetAmmo()} / {m_maxAmmo})");
		if (m_targetCharacters.Count == 0)
		{
			sb.Append(" $piece_turret_target $piece_turret_target_everything");
		}
		else
		{
			sb.Append(" $piece_turret_target ");
			sb.Append(m_targetsText);
		}
		sb.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_turret_addammo\n[<color=yellow><b>1-8</b></color>] $piece_turret_target_set");
		return Localization.instance.Localize(sb.ToString());
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			if (m_holdRepeatInterval <= 0f)
			{
				return false;
			}
			if (Time.time - m_lastUseTime < m_holdRepeatInterval)
			{
				return false;
			}
		}
		m_lastUseTime = Time.time;
		return UseItem(character, null);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (item == null)
		{
			item = FindAmmoItem(user.GetInventory(), onlyCurrentlyLoadableType: true);
			if (item == null)
			{
				if (GetAmmo() > 0 && FindAmmoItem(user.GetInventory(), onlyCurrentlyLoadableType: false) != null)
				{
					ItemDrop component = ZNetScene.instance.GetPrefab(GetAmmoType()).GetComponent<ItemDrop>();
					user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_turretotherammo") + Localization.instance.Localize(component.m_itemData.m_shared.m_name));
					return false;
				}
				user.Message(MessageHud.MessageType.Center, "$msg_noturretammo");
				return false;
			}
		}
		foreach (TrophyTarget configTarget in m_configTargets)
		{
			if (!(item.m_shared.m_name == configTarget.m_item.m_itemData.m_shared.m_name))
			{
				continue;
			}
			if (m_targetItems.Contains(configTarget.m_item))
			{
				m_targetItems.Remove(configTarget.m_item);
			}
			else
			{
				if (m_targetItems.Count >= m_maxConfigTargets)
				{
					m_targetItems.RemoveAt(0);
				}
				m_targetItems.Add(configTarget.m_item);
			}
			SetTargets();
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$piece_turret_target_set_msg " + ((m_targetCharacters.Count == 0) ? "$piece_turret_target_everything" : m_targetsText)));
			m_setTargetEffect.Create(base.transform.position, base.transform.rotation);
			Game.instance.IncrementPlayerStat(PlayerStatType.TurretTrophySet);
			return true;
		}
		if (!IsItemAllowed(item.m_dropPrefab.name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_wontwork");
			return false;
		}
		if (GetAmmo() > 0 && GetAmmoType() != item.m_dropPrefab.name)
		{
			ItemDrop component2 = ZNetScene.instance.GetPrefab(GetAmmoType()).GetComponent<ItemDrop>();
			user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_turretotherammo") + Localization.instance.Localize(component2.m_itemData.m_shared.m_name));
			return false;
		}
		ZLog.Log("trying to add ammo " + item.m_shared.m_name);
		if (GetAmmo() >= m_maxAmmo)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull");
			return false;
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name);
		user.GetInventory().RemoveItem(item, 1);
		Game.instance.IncrementPlayerStat(PlayerStatType.TurretAmmoAdded);
		m_nview.InvokeRPC("RPC_AddAmmo", item.m_dropPrefab.name);
		return true;
	}

	private void RPC_AddAmmo(long sender, string name)
	{
		if (m_nview.IsOwner())
		{
			if (!IsItemAllowed(name))
			{
				ZLog.Log("Item not allowed " + name);
				return;
			}
			int @int = m_nview.GetZDO().GetInt(ZDOVars.s_ammo);
			m_nview.GetZDO().Set(ZDOVars.s_ammo, @int + 1);
			m_nview.GetZDO().Set(ZDOVars.s_ammoType, name);
			m_addAmmoEffect.Create(m_turretBody.transform.position, m_turretBody.transform.rotation);
			UpdateVisualBolt();
			ZLog.Log("Added ammo " + name);
		}
	}

	private void RPC_SetTarget(long sender, ZDOID character)
	{
		GameObject gameObject = ZNetScene.instance.FindInstance(character);
		if ((bool)gameObject)
		{
			Character component = gameObject.GetComponent<Character>();
			if ((object)component != null)
			{
				m_target = component;
				m_haveTarget = true;
				return;
			}
		}
		m_target = null;
		m_haveTarget = false;
		m_scan = 0f;
	}

	private void UpdateVisualBolt()
	{
		if (HasAmmo())
		{
			_ = !IsCoolingDown();
		}
		else
			_ = 0;
		string ammoType = GetAmmoType();
		bool flag = HasAmmo() && !IsCoolingDown();
		foreach (AmmoType item in m_allowedAmmo)
		{
			bool flag2 = item.m_ammo.name == ammoType;
			item.m_visual.SetActive(flag2 && flag);
		}
	}

	private bool IsItemAllowed(string itemName)
	{
		foreach (AmmoType item in m_allowedAmmo)
		{
			if (item.m_ammo.name == itemName)
			{
				return true;
			}
		}
		return false;
	}

	private ItemDrop.ItemData FindAmmoItem(Inventory inventory, bool onlyCurrentlyLoadableType)
	{
		if (onlyCurrentlyLoadableType && HasAmmo())
		{
			return inventory.GetAmmoItem(m_ammoType, GetAmmoType());
		}
		return inventory.GetAmmoItem(m_ammoType);
	}

	private void OnDestroyed()
	{
		if (m_nview.IsOwner() && m_returnAmmoOnDestroy)
		{
			int ammo = GetAmmo();
			string ammoType = GetAmmoType();
			GameObject prefab = ZNetScene.instance.GetPrefab(ammoType);
			for (int i = 0; i < ammo; i++)
			{
				Vector3 position = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
				Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
				UnityEngine.Object.Instantiate(prefab, position, rotation);
			}
		}
	}

	public void ShowHoverMarker()
	{
		ShowBuildMarker();
	}

	public void ShowBuildMarker()
	{
		if ((bool)m_marker)
		{
			m_marker.gameObject.SetActive(value: true);
			CancelInvoke("HideMarker");
			Invoke("HideMarker", m_markerHideTime);
		}
	}

	private void UpdateMarker(float dt)
	{
		if ((bool)m_marker && m_marker.isActiveAndEnabled)
		{
			m_marker.m_start = base.transform.rotation.eulerAngles.y - m_horizontalAngle;
			m_marker.m_turns = m_horizontalAngle * 2f / 360f;
		}
	}

	private void HideMarker()
	{
		if ((bool)m_marker)
		{
			m_marker.gameObject.SetActive(value: false);
		}
	}

	private void SetTargets()
	{
		if (!m_nview.IsOwner())
		{
			m_nview.ClaimOwnership();
		}
		m_nview.GetZDO().Set(ZDOVars.s_targets, m_targetItems.Count);
		for (int i = 0; i < m_targetItems.Count; i++)
		{
			m_nview.GetZDO().Set("target" + i, m_targetItems[i].m_itemData.m_shared.m_name);
		}
		ReadTargets();
	}

	private void ReadTargets()
	{
		if (!m_nview || !m_nview.IsValid())
		{
			return;
		}
		m_targetItems.Clear();
		m_targetCharacters.Clear();
		m_targetsText = "";
		int @int = m_nview.GetZDO().GetInt(ZDOVars.s_targets);
		for (int i = 0; i < @int; i++)
		{
			string @string = m_nview.GetZDO().GetString("target" + i);
			foreach (TrophyTarget configTarget in m_configTargets)
			{
				if (!(configTarget.m_item.m_itemData.m_shared.m_name == @string))
				{
					continue;
				}
				m_targetItems.Add(configTarget.m_item);
				m_targetCharacters.AddRange(configTarget.m_targets);
				if (m_targetsText.Length > 0)
				{
					m_targetsText += ", ";
				}
				if (!string.IsNullOrEmpty(configTarget.m_nameOverride))
				{
					m_targetsText += configTarget.m_nameOverride;
					break;
				}
				for (int j = 0; j < configTarget.m_targets.Count; j++)
				{
					m_targetsText += configTarget.m_targets[j].m_name;
					if (j + 1 < configTarget.m_targets.Count)
					{
						m_targetsText += ", ";
					}
				}
				break;
			}
		}
	}
}
