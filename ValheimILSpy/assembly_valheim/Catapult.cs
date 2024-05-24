using System;
using System.Collections.Generic;
using System.Linq;
using Dynamics;
using UnityEngine;
using UnityEngine.Serialization;

public class Catapult : MonoBehaviour
{
	[Header("Legs")]
	public List<Switch> m_legs = new List<Switch>();

	[FormerlySerializedAs("m_legAnimationDown")]
	public AnimationCurve m_legAnimationCurve;

	private AnimationCurve m_legAnimationCurveUp;

	public float m_legAnimationDegrees = 90f;

	public float m_legAnimationUpMultiplier = 1f;

	public float m_legAnimationTime = 5f;

	public float m_legDownMass = 500f;

	[Header("Shooting")]
	public GameObject m_forceVector;

	public GameObject m_arm;

	public Switch m_loadPoint;

	public Transform m_shootPoint;

	public AnimationCurve m_armAnimation;

	public float m_armAnimationDegrees = 180f;

	public float m_armAnimationTime = 2f;

	public float m_releaseAnimationTime;

	public float m_shootAfterLoadDelay = 1f;

	public Projectile m_projectile;

	public ItemDrop m_defaultAmmo;

	public int m_maxLoadStack = 1;

	public float m_hitNoise = 1f;

	public float m_randomRotationMin = 2f;

	public float m_randomRotationMax = 10f;

	public float m_shootVelocityVariation = 0.1f;

	[Header("Dynamics")]
	public DynamicsParameters m_armDynamicsSettings;

	private FloatDynamics m_armDynamics;

	public DynamicsParameters m_legDynamicsSettings;

	private FloatDynamics m_legDynamics;

	[Header("Ammo")]
	[Tooltip("If checked, will include all except listed types. If unchecked, will exclude all except listed types.")]
	public bool m_defaultIncludeAndListExclude = true;

	public bool m_onlyUseIncludedProjectiles = true;

	public bool m_onlyIncludedItemsDealDamage = true;

	public List<ItemDrop.ItemData.ItemType> m_includeExcludeTypesList = new List<ItemDrop.ItemData.ItemType>();

	public List<ItemDrop> m_includeItemsOverride = new List<ItemDrop>();

	public List<ItemDrop> m_excludeItemsOverride = new List<ItemDrop>();

	[Header("Character Launching")]
	public SphereCollider m_launchCollectArea;

	public float m_preLaunchForce = 5f;

	public float m_launchForce = 100f;

	[Header("Effects")]
	public EffectList m_legDownEffect = new EffectList();

	public EffectList m_legDownDoneEffect = new EffectList();

	public EffectList m_legUpEffect = new EffectList();

	public EffectList m_legUpDoneEffect = new EffectList();

	public EffectList m_shootStartEffect = new EffectList();

	public EffectList m_shootReleaseEffect = new EffectList();

	public EffectList m_armReturnEffect = new EffectList();

	public EffectList m_loadItemEffect = new EffectList();

	private static int m_characterMask;

	private ZNetView m_nview;

	private ZNetView m_wagonNview;

	private Vagon m_wagon;

	private Rigidbody m_rigidBody;

	private float m_baseMass;

	private ItemDrop.ItemData m_loadedItem;

	private int m_loadStack;

	private GameObject m_visualItem;

	private GameObject m_shotItem;

	private bool m_lockedLegs;

	private Vector3[] m_legRotations;

	private Quaternion[] m_legRotationQuat;

	private float m_legAnimTimer;

	private bool m_movingLegs;

	private Vector3 m_armRotation;

	private float m_armAnimTime;

	private Projectile m_lastProjectile;

	private ItemDrop.ItemData m_lastAmmo;

	private Collider[] m_colliders = new Collider[10];

	private List<Character> m_launchCharacters = new List<Character>();

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		m_wagon = GetComponent<Vagon>();
		m_rigidBody = GetComponent<Rigidbody>();
		m_baseMass = m_rigidBody.mass;
		m_legRotations = new Vector3[m_legs.Count];
		m_legRotationQuat = new Quaternion[m_legs.Count];
		for (int i = 0; i < m_legs.Count; i++)
		{
			Switch @switch = m_legs[i];
			@switch.m_onUse = (Switch.Callback)Delegate.Combine(@switch.m_onUse, new Switch.Callback(OnLegUse));
			Switch switch2 = m_legs[i];
			switch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(switch2.m_onHover, new Switch.TooltipCallback(OnLegHover));
			m_legRotations[i] = m_legs[i].transform.localEulerAngles;
			m_legRotationQuat[i] = m_legs[i].transform.localRotation;
		}
		Switch loadPoint = m_loadPoint;
		loadPoint.m_onUse = (Switch.Callback)Delegate.Combine(loadPoint.m_onUse, new Switch.Callback(OnLoadPointUse));
		Switch loadPoint2 = m_loadPoint;
		loadPoint2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(loadPoint2.m_onHover, new Switch.TooltipCallback(OnHoverLoadPoint));
		m_armRotation = m_arm.transform.localRotation.eulerAngles;
		m_armDynamics = new FloatDynamics(m_armDynamicsSettings, 0f);
		m_legDynamics = new FloatDynamics(m_legDynamicsSettings, 1f);
		m_legAnimationCurveUp = new AnimationCurve();
		for (int j = 0; j < m_legAnimationCurve.keys.Length; j++)
		{
			Keyframe key = m_legAnimationCurve.keys[j];
			key.value = 1f - key.value;
			m_legAnimationCurveUp.AddKey(key);
		}
		if ((bool)m_nview)
		{
			ZDO zDO = m_nview.GetZDO();
			if (zDO != null && zDO.GetBool(ZDOVars.s_locked))
			{
				m_lockedLegs = zDO.GetBool(ZDOVars.s_locked);
			}
		}
		m_nview.Register("RPC_Shoot", RPC_Shoot);
		m_nview.Register<bool>("RPC_OnLegUse", RPC_OnLegUse);
		m_nview.Register<string>("RPC_SetLoadedVisual", RPC_SetLoadedVisual);
		m_legAnimTimer = 1f;
		m_movingLegs = true;
		UpdateLegAnimation(Time.fixedDeltaTime);
		if (m_characterMask == 0)
		{
			m_characterMask = LayerMask.GetMask("character");
		}
	}

	private void FixedUpdate()
	{
		UpdateArmAnimation(Time.fixedDeltaTime);
		UpdateLegAnimation(Time.fixedDeltaTime);
	}

	private void UpdateArmAnimation(float dt)
	{
		float num = m_armAnimTime / m_armAnimationTime;
		float num2 = m_armDynamics.Update(dt, m_armAnimation.Evaluate(num));
		m_arm.transform.localEulerAngles = new Vector3(m_armRotation.x + num2 * m_armAnimationDegrees, m_armRotation.y, m_armRotation.z);
		if (m_armAnimTime <= 0f)
		{
			return;
		}
		m_armAnimTime += dt;
		if (m_armAnimTime > m_armAnimationTime)
		{
			m_armAnimTime = 0f;
			m_arm.transform.localEulerAngles = m_armRotation;
			m_armReturnEffect.Create(m_loadPoint.transform.position, m_loadPoint.transform.rotation);
		}
		else if (num > m_releaseAnimationTime && (m_loadedItem != null || m_launchCharacters.Count > 0))
		{
			Release();
		}
		else
		{
			if (!(m_preLaunchForce > 0f))
			{
				return;
			}
			Vector3 normalized = (m_forceVector.transform.position - base.transform.position).normalized;
			foreach (Character launchCharacter in m_launchCharacters)
			{
				launchCharacter.ForceJump(normalized * m_preLaunchForce);
			}
		}
	}

	private void UpdateLegAnimation(float dt)
	{
		if (!m_nview || !m_nview.IsValid())
		{
			return;
		}
		if (m_movingLegs)
		{
			m_legAnimTimer += Time.deltaTime;
			if (m_legAnimTimer >= m_legAnimationTime)
			{
				m_movingLegs = false;
				for (int i = 0; i < m_legs.Count; i++)
				{
					Vector3 position = m_legs[i].transform.GetChild(0).transform.position;
					if (!m_lockedLegs)
					{
						m_legUpDoneEffect.Create(m_legs[i].transform.position, m_legs[i].transform.rotation);
						continue;
					}
					m_legDownDoneEffect.Create(position, Quaternion.identity);
					m_rigidBody.mass = m_legDownMass;
				}
				return;
			}
		}
		float time = m_legAnimTimer / m_legAnimationTime;
		AnimationCurve animationCurve = (m_lockedLegs ? m_legAnimationCurve : m_legAnimationCurveUp);
		float num = m_legDynamics.Update(dt, animationCurve.Evaluate(time));
		for (int j = 0; j < m_legs.Count; j++)
		{
			Vector3 localEulerAngles = m_legRotations[j];
			localEulerAngles.z += num * m_legAnimationDegrees;
			m_legs[j].transform.localEulerAngles = localEulerAngles;
		}
	}

	private bool OnLegUse(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (m_movingLegs)
		{
			return false;
		}
		m_nview.InvokeRPC(ZNetView.Everybody, "RPC_OnLegUse", !m_lockedLegs);
		if (!m_nview.IsOwner())
		{
			m_nview.InvokeRPC("RPC_RequestOwn");
		}
		return true;
	}

	private void RPC_OnLegUse(long sender, bool value)
	{
		m_lockedLegs = value;
		if ((bool)m_nview && m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(ZDOVars.s_locked, m_lockedLegs);
		}
		m_legAnimTimer = 0f;
		m_movingLegs = true;
		if (m_lockedLegs)
		{
			m_legDownEffect.Create(base.transform.position, base.transform.rotation);
			return;
		}
		m_legUpEffect.Create(base.transform.position, base.transform.rotation);
		m_rigidBody.mass = m_baseMass;
	}

	private string OnLegHover()
	{
		if (m_movingLegs)
		{
			return "";
		}
		return Localization.instance.Localize(m_lockedLegs ? "[<color=yellow><b>$KEY_Use</b></color>] $piece_catapult_legsup" : "[<color=yellow><b>$KEY_Use</b></color>] $piece_catapult_legsdown");
	}

	private bool OnLoadPointUse(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (m_loadedItem != null || item == null || m_armAnimTime != 0f)
		{
			user.UseIemBlockkMessage();
			return false;
		}
		if (!CanItemBeLoaded(item))
		{
			user.Message(MessageHud.MessageType.Center, "$piece_catapult_wontfit");
			user.UseIemBlockkMessage();
			return false;
		}
		m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetLoadedVisual", item.m_dropPrefab.name);
		m_loadedItem = item;
		m_loadStack = Mathf.Min(item.m_stack, m_maxLoadStack);
		Invoke("Shoot", m_shootAfterLoadDelay);
		if (item.m_equipped)
		{
			user.UnequipItem(item);
		}
		user.GetInventory().RemoveItem(item, m_loadStack);
		if (!m_nview.IsOwner())
		{
			m_nview.InvokeRPC("RPC_RequestOwn");
		}
		m_loadItemEffect.Create(m_loadPoint.transform.position, m_loadPoint.transform.rotation);
		return true;
	}

	private bool CanItemBeLoaded(ItemDrop.ItemData item)
	{
		if (m_includeItemsOverride.Any((ItemDrop x) => x.m_itemData.m_shared.m_name == item.m_shared.m_name))
		{
			return true;
		}
		if (m_onlyUseIncludedProjectiles && ItemStand.GetAttachPrefab(item.m_dropPrefab) == null)
		{
			return false;
		}
		if (m_defaultIncludeAndListExclude && m_includeExcludeTypesList.Contains(item.m_shared.m_itemType))
		{
			return false;
		}
		if (!m_defaultIncludeAndListExclude && !m_includeExcludeTypesList.Contains(item.m_shared.m_itemType))
		{
			return false;
		}
		if (m_excludeItemsOverride.Any((ItemDrop x) => x.m_itemData.m_shared.m_name == item.m_shared.m_name))
		{
			return false;
		}
		return true;
	}

	private void Shoot()
	{
		m_nview.InvokeRPC(ZNetView.Everybody, "RPC_Shoot");
	}

	private void RPC_Shoot(long sender)
	{
		m_shootStartEffect.Create(m_loadPoint.transform.position, m_loadPoint.transform.rotation);
		m_armAnimTime = 1E-06f;
		CollectLaunchCharacters();
	}

	private void RPC_SetLoadedVisual(long sender, string name)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		if ((object)component != null)
		{
			m_loadedItem = component.m_itemData;
		}
		GameObject attachPrefab = ItemStand.GetAttachPrefab(itemPrefab);
		if (attachPrefab == null)
		{
			ZLog.LogError("Valid catapult ammo '" + name + "' is missing attach prefab, aborting.");
			return;
		}
		attachPrefab = ItemStand.GetAttachGameObject(attachPrefab);
		m_visualItem = UnityEngine.Object.Instantiate(attachPrefab, m_loadPoint.transform);
		m_visualItem.transform.localPosition = Vector3.zero;
	}

	private void Release()
	{
		ShootProjectile();
		LaunchCharacters();
		m_loadedItem = null;
	}

	private void ShootProjectile()
	{
		Vector3 vector = m_forceVector.transform.position - base.transform.position;
		Vector3 vector2 = vector.normalized;
		m_shootReleaseEffect.Create(m_loadPoint.transform.position, Quaternion.LookRotation(vector2));
		Projectile projectile = m_projectile;
		bool flag = m_includeItemsOverride.Any((ItemDrop x) => x.m_itemData.m_shared.m_name == m_loadedItem.m_shared.m_name);
		if ((!m_onlyUseIncludedProjectiles || (m_onlyUseIncludedProjectiles && flag)) && m_loadedItem.m_shared.m_attack.m_attackProjectile != null)
		{
			Projectile component = m_loadedItem.m_shared.m_attack.m_attackProjectile.GetComponent<Projectile>();
			if ((object)component != null)
			{
				projectile = component;
			}
		}
		m_lastAmmo = m_defaultAmmo.m_itemData;
		if (m_nview.IsOwner())
		{
			for (int i = 0; i < m_loadStack; i++)
			{
				m_lastProjectile = UnityEngine.Object.Instantiate(projectile, m_shootPoint.transform.position, m_shootPoint.transform.rotation);
				HitData hitData = new HitData();
				if (projectile == m_projectile)
				{
					if ((bool)m_lastProjectile.m_visual)
					{
						m_lastProjectile.m_visual.gameObject.SetActive(value: false);
					}
					m_lastProjectile.GetComponent<ZNetView>().GetZDO().Set(ZDOVars.s_visual, m_loadedItem.m_dropPrefab.name);
					Collider componentInChildren = m_lastProjectile.m_visual.GetComponentInChildren<Collider>();
					if ((object)componentInChildren != null)
					{
						componentInChildren.enabled = false;
					}
					if (!m_onlyIncludedItemsDealDamage || (m_onlyIncludedItemsDealDamage && flag))
					{
						hitData.m_toolTier = (short)m_lastAmmo.m_shared.m_toolTier;
						hitData.m_pushForce = m_lastAmmo.m_shared.m_attackForce;
						hitData.m_backstabBonus = m_lastAmmo.m_shared.m_backstabBonus;
						hitData.m_staggerMultiplier = m_lastAmmo.m_shared.m_attack.m_staggerMultiplier;
						hitData.m_damage.Add(m_lastAmmo.GetDamage());
						hitData.m_statusEffectHash = (m_lastAmmo.m_shared.m_attackStatusEffect ? m_lastAmmo.m_shared.m_attackStatusEffect.NameHash() : 0);
						hitData.m_blockable = m_lastAmmo.m_shared.m_blockable;
						hitData.m_dodgeable = m_lastAmmo.m_shared.m_dodgeable;
						hitData.m_skill = m_lastAmmo.m_shared.m_skillType;
						if (m_lastAmmo.m_shared.m_attackStatusEffect != null)
						{
							hitData.m_statusEffectHash = m_lastAmmo.m_shared.m_attackStatusEffect.NameHash();
						}
					}
				}
				else if (!m_onlyIncludedItemsDealDamage || (m_onlyIncludedItemsDealDamage && flag))
				{
					hitData.m_toolTier = (short)m_loadedItem.m_shared.m_toolTier;
					hitData.m_pushForce = m_loadedItem.m_shared.m_attackForce;
					hitData.m_backstabBonus = m_loadedItem.m_shared.m_backstabBonus;
					hitData.m_damage.Add(m_loadedItem.GetDamage());
					hitData.m_statusEffectHash = (m_loadedItem.m_shared.m_attackStatusEffect ? m_loadedItem.m_shared.m_attackStatusEffect.NameHash() : 0);
					hitData.m_skillLevel = 1f;
					hitData.m_itemLevel = (short)m_loadedItem.m_quality;
					hitData.m_itemWorldLevel = (byte)m_loadedItem.m_worldLevel;
					hitData.m_blockable = m_loadedItem.m_shared.m_blockable;
					hitData.m_dodgeable = m_loadedItem.m_shared.m_dodgeable;
					hitData.m_skill = m_loadedItem.m_shared.m_skillType;
					hitData.m_hitType = HitData.HitType.Catapult;
				}
				if (m_lastAmmo.m_shared.m_attack.m_projectileAccuracyMin > 0f || m_lastAmmo.m_shared.m_attack.m_projectileAccuracy > 0f)
				{
					float num = UnityEngine.Random.Range(m_lastAmmo.m_shared.m_attack.m_projectileAccuracyMin, m_lastAmmo.m_shared.m_attack.m_projectileAccuracy);
					Vector3 axis = Vector3.Cross(vector2, Vector3.up);
					Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - num, num), Vector3.up);
					vector2 = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - num, num), axis) * vector2;
					vector2 = quaternion * vector2;
				}
				Vector3 velocity = vector * m_lastAmmo.m_shared.m_attack.m_projectileVel * UnityEngine.Random.Range(1f, 1f + m_shootVelocityVariation);
				projectile.m_respawnItemOnHit = !flag;
				m_lastProjectile.Setup(null, velocity, m_hitNoise, hitData, m_loadedItem, m_lastAmmo);
				m_lastProjectile.m_rotateVisual = UnityEngine.Random.Range(m_randomRotationMin, m_randomRotationMax);
				m_lastProjectile.m_rotateVisualY = UnityEngine.Random.Range(m_randomRotationMin, m_randomRotationMax);
				m_lastProjectile.m_rotateVisualZ = UnityEngine.Random.Range(m_randomRotationMin, m_randomRotationMax);
			}
		}
		UnityEngine.Object.Destroy(m_visualItem);
		m_visualItem = null;
	}

	private void CollectLaunchCharacters()
	{
		m_launchCharacters.Clear();
		int num = Physics.OverlapSphereNonAlloc(m_launchCollectArea.transform.position, m_launchCollectArea.radius, m_colliders, m_characterMask);
		for (int i = 0; i < num; i++)
		{
			Character componentInParent = m_colliders[i].GetComponentInParent<Character>();
			if ((object)componentInParent != null)
			{
				ZNetView component = componentInParent.GetComponent<ZNetView>();
				if ((object)component != null && component.IsOwner())
				{
					m_launchCharacters.Add(componentInParent);
					componentInParent.SetTempParent(m_arm.transform);
				}
			}
		}
	}

	private void LaunchCharacters()
	{
		foreach (Character launchCharacter in m_launchCharacters)
		{
			launchCharacter.ReleaseTempParent();
			Vector3 normalized = (m_forceVector.transform.position - base.transform.position).normalized;
			launchCharacter.ForceJump(normalized * m_launchForce);
			launchCharacter.StandUpOnNextGround();
		}
		m_launchCharacters.Clear();
	}

	private string OnHoverLoadPoint()
	{
		return Localization.instance.Localize((m_loadedItem == null) ? "[<color=yellow><b>1-8</b></color>] $piece_catapult_placeitem" : "");
	}
}
