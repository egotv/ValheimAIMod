using System;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, IProjectile
{
	public HitData.DamageTypes m_damage;

	public float m_aoe;

	public bool m_dodgeable;

	public bool m_blockable;

	public float m_attackForce;

	public float m_backstabBonus = 4f;

	public string m_statusEffect = "";

	private int m_statusEffectHash;

	public float m_healthReturn;

	public bool m_canHitWater;

	public float m_ttl = 4f;

	public float m_gravity;

	public float m_drag;

	public float m_rayRadius;

	public float m_hitNoise = 50f;

	public bool m_doOwnerRaytest;

	public bool m_stayAfterHitStatic;

	public bool m_stayAfterHitDynamic;

	public float m_stayTTL = 1f;

	public bool m_attachToRigidBody;

	public bool m_attachToClosestBone;

	public float m_attachPenetration;

	public float m_attachBoneNearify = 0.25f;

	public GameObject m_hideOnHit;

	public bool m_stopEmittersOnHit = true;

	public EffectList m_hitEffects = new EffectList();

	public EffectList m_hitWaterEffects = new EffectList();

	[Header("Bounce")]
	public bool m_bounce;

	public bool m_bounceOnWater;

	[Range(0f, 1f)]
	public float m_bouncePower = 0.85f;

	[Range(0f, 1f)]
	public float m_bounceRoughness = 0.3f;

	[Min(1f)]
	public int m_maxBounces = 99;

	[Min(0.01f)]
	public float m_minBounceVel = 0.25f;

	[Header("Spawn on hit")]
	public bool m_respawnItemOnHit;

	public bool m_spawnOnTtl;

	public GameObject m_spawnOnHit;

	[Range(0f, 1f)]
	public float m_spawnOnHitChance = 1f;

	public int m_spawnCount = 1;

	public List<GameObject> m_randomSpawnOnHit = new List<GameObject>();

	public int m_randomSpawnOnHitCount = 1;

	public bool m_randomSpawnSkipLava;

	public bool m_showBreakMessage;

	public bool m_staticHitOnly;

	public bool m_groundHitOnly;

	public Vector3 m_spawnOffset = Vector3.zero;

	public bool m_copyProjectileRotation = true;

	public bool m_spawnRandomRotation;

	public bool m_spawnFacingRotation;

	public EffectList m_spawnOnHitEffects = new EffectList();

	public OnProjectileHit m_onHit;

	[Header("Projectile Spawning")]
	public bool m_spawnProjectileNewVelocity;

	public float m_spawnProjectileMinVel = 1f;

	public float m_spawnProjectileMaxVel = 5f;

	[Range(0f, 1f)]
	public float m_spawnProjectileRandomDir;

	public bool m_spawnProjectileHemisphereDir;

	public bool m_projectilesInheritHitData;

	public bool m_onlySpawnedProjectilesDealDamage;

	public bool m_divideDamageBetweenProjectiles;

	[Header("Rotate projectile")]
	public float m_rotateVisual;

	public float m_rotateVisualY;

	public float m_rotateVisualZ;

	public GameObject m_visual;

	public bool m_canChangeVisuals;

	private ZNetView m_nview;

	private GameObject m_attachParent;

	private Vector3 m_attachParentOffset;

	private Quaternion m_attachParentOffsetRot;

	private bool m_hasLeftShields = true;

	private Vector3 m_vel = Vector3.zero;

	private Character m_owner;

	private Skills.SkillType m_skill;

	private float m_raiseSkillAmount = 1f;

	private ItemDrop.ItemData m_weapon;

	private ItemDrop.ItemData m_ammo;

	[NonSerialized]
	public ItemDrop.ItemData m_spawnItem;

	private HitData m_originalHitData;

	private bool m_didHit;

	private int m_bounceCount;

	private bool m_didBounce;

	private bool m_changedVisual;

	[HideInInspector]
	public Vector3 m_startPoint;

	private bool m_haveStartPoint;

	private static int s_rayMaskSolids;

	public bool HasBeenOutsideShields => m_hasLeftShields;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (s_rayMaskSolids == 0)
		{
			s_rayMaskSolids = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
		}
		if (!string.IsNullOrEmpty(m_statusEffect))
		{
			m_statusEffectHash = m_statusEffect.GetStableHashCode();
		}
		m_nview.Register("RPC_OnHit", RPC_OnHit);
		m_nview.Register<ZDOID>("RPC_Attach", RPC_Attach);
		UpdateVisual();
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		UpdateRotation(Time.fixedDeltaTime);
		if (!m_nview.IsOwner())
		{
			return;
		}
		if (!m_didHit)
		{
			Vector3 vector = base.transform.position;
			if (m_haveStartPoint)
			{
				vector = m_startPoint;
			}
			m_vel += Vector3.down * (m_gravity * Time.fixedDeltaTime);
			float num = Mathf.Pow(m_vel.magnitude, 2f) * m_drag * Time.fixedDeltaTime;
			m_vel += num * -m_vel.normalized;
			base.transform.position += m_vel * Time.fixedDeltaTime;
			if (m_rotateVisual == 0f)
			{
				base.transform.rotation = Quaternion.LookRotation(m_vel);
			}
			if (m_canHitWater)
			{
				float liquidLevel = Floating.GetLiquidLevel(base.transform.position);
				if (base.transform.position.y < liquidLevel)
				{
					OnHit(null, base.transform.position, water: true, Vector3.up);
				}
			}
			m_didBounce = false;
			if (!m_didHit)
			{
				Vector3 vector2 = base.transform.position - vector;
				if (!m_haveStartPoint)
				{
					vector -= vector2.normalized * (vector2.magnitude * 0.5f);
				}
				RaycastHit[] array = ((m_rayRadius != 0f) ? Physics.SphereCastAll(vector, m_rayRadius, vector2.normalized, vector2.magnitude, s_rayMaskSolids) : Physics.RaycastAll(vector, vector2.normalized, vector2.magnitude * 1.5f, s_rayMaskSolids));
				Debug.DrawLine(vector, base.transform.position, (array.Length != 0) ? Color.red : Color.yellow, 5f);
				if (array.Length != 0)
				{
					Array.Sort(array, (RaycastHit x, RaycastHit y) => x.distance.CompareTo(y.distance));
					RaycastHit[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						RaycastHit raycastHit = array2[i];
						Vector3 hitPoint = ((raycastHit.distance == 0f) ? vector : raycastHit.point);
						OnHit(raycastHit.collider, hitPoint, water: false, raycastHit.normal);
						if (m_didHit || m_didBounce)
						{
							break;
						}
					}
				}
			}
			if (m_haveStartPoint)
			{
				m_haveStartPoint = false;
			}
		}
		if (m_ttl > 0f)
		{
			m_ttl -= Time.fixedDeltaTime;
			if (m_ttl <= 0f)
			{
				if (m_spawnOnTtl)
				{
					SpawnOnHit(null, null, -m_vel.normalized);
				}
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
		ShieldGenerator.CheckProjectile(this);
	}

	private void Update()
	{
		UpdateVisual();
	}

	private void LateUpdate()
	{
		if ((bool)m_attachParent)
		{
			Vector3 point = m_attachParent.transform.position - m_attachParentOffset;
			Quaternion quaternion = m_attachParent.transform.rotation * m_attachParentOffsetRot;
			base.transform.position = Utils.RotatePointAroundPivot(point, m_attachParent.transform.position, quaternion);
			base.transform.localRotation = quaternion;
		}
	}

	private void UpdateVisual()
	{
		if (!m_canChangeVisuals || (object)m_nview == null || !m_nview.IsValid() || m_changedVisual || !m_nview.GetZDO().GetString(ZDOVars.s_visual, out var value))
		{
			return;
		}
		ZLog.Log("Visual prefab is " + value);
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(value);
		if ((object)itemPrefab.GetComponent<ItemDrop>() != null)
		{
			GameObject attachPrefab = ItemStand.GetAttachPrefab(itemPrefab);
			if (!(attachPrefab == null))
			{
				attachPrefab = ItemStand.GetAttachGameObject(attachPrefab);
				m_visual.gameObject.SetActive(value: false);
				m_visual = UnityEngine.Object.Instantiate(attachPrefab, base.transform);
				m_visual.transform.localPosition = Vector3.zero;
				m_changedVisual = true;
			}
		}
	}

	public Vector3 GetVelocity()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return Vector3.zero;
		}
		if (m_didHit)
		{
			return Vector3.zero;
		}
		return m_vel;
	}

	private void UpdateRotation(float dt)
	{
		if (!(m_visual == null) && ((double)m_rotateVisual != 0.0 || (double)m_rotateVisualY != 0.0 || (double)m_rotateVisualZ != 0.0))
		{
			m_visual.transform.Rotate(new Vector3(m_rotateVisual * dt, m_rotateVisualY * dt, m_rotateVisualZ * dt));
		}
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		m_owner = owner;
		m_vel = velocity;
		m_ammo = ammo;
		m_weapon = item;
		if (hitNoise >= 0f)
		{
			m_hitNoise = hitNoise;
		}
		if (hitData != null)
		{
			m_originalHitData = hitData;
			m_damage = hitData.m_damage;
			m_blockable = hitData.m_blockable;
			m_dodgeable = hitData.m_dodgeable;
			m_attackForce = hitData.m_pushForce;
			m_backstabBonus = hitData.m_backstabBonus;
			m_healthReturn = hitData.m_healthReturn;
			if (m_statusEffectHash != hitData.m_statusEffectHash)
			{
				m_statusEffectHash = hitData.m_statusEffectHash;
				m_statusEffect = "";
			}
			m_skill = hitData.m_skill;
			m_raiseSkillAmount = hitData.m_skillRaiseAmount;
		}
		if (m_spawnOnHit != null && m_onlySpawnedProjectilesDealDamage)
		{
			m_damage.Modify(0f);
		}
		if (m_respawnItemOnHit)
		{
			m_spawnItem = item;
		}
		if (m_doOwnerRaytest && (bool)owner)
		{
			m_startPoint = owner.GetCenterPoint();
			m_startPoint.y = base.transform.position.y;
			m_haveStartPoint = true;
		}
		else
		{
			m_startPoint = base.transform.position;
		}
		LineConnect component = GetComponent<LineConnect>();
		if ((bool)component && (bool)owner)
		{
			component.SetPeer(owner.GetZDOID());
		}
		m_hasLeftShields = !ShieldGenerator.IsInsideShield(base.transform.position);
	}

	private void DoAOE(Vector3 hitPoint, ref bool hitCharacter, ref bool didDamage)
	{
		Collider[] array = Physics.OverlapSphere(hitPoint, m_aoe, s_rayMaskSolids, QueryTriggerInteraction.UseGlobal);
		HashSet<GameObject> hashSet = new HashSet<GameObject>();
		Collider[] array2 = array;
		foreach (Collider collider in array2)
		{
			GameObject gameObject = FindHitObject(collider);
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (component == null || hashSet.Contains(gameObject))
			{
				continue;
			}
			hashSet.Add(gameObject);
			if (IsValidTarget(component))
			{
				if (component is Character)
				{
					hitCharacter = true;
				}
				Vector3 vector = collider.ClosestPointOnBounds(hitPoint);
				Vector3 vector2 = ((Vector3.Distance(vector, hitPoint) > 0.1f) ? (vector - hitPoint) : m_vel);
				vector2.y = 0f;
				vector2.Normalize();
				HitData hitData = new HitData();
				hitData.m_hitCollider = collider;
				hitData.m_damage = m_damage;
				hitData.m_pushForce = m_attackForce;
				hitData.m_backstabBonus = m_backstabBonus;
				hitData.m_ranged = true;
				hitData.m_point = vector;
				hitData.m_dir = vector2.normalized;
				hitData.m_statusEffectHash = m_statusEffectHash;
				hitData.m_skillLevel = (m_owner ? m_owner.GetSkillLevel(m_skill) : 1f);
				hitData.m_dodgeable = m_dodgeable;
				hitData.m_blockable = m_blockable;
				hitData.m_skill = m_skill;
				hitData.m_skillRaiseAmount = m_raiseSkillAmount;
				hitData.SetAttacker(m_owner);
				hitData.m_hitType = ((!(hitData.GetAttacker() is Player)) ? HitData.HitType.EnemyHit : HitData.HitType.PlayerHit);
				hitData.m_healthReturn = m_healthReturn;
				component.Damage(hitData);
				didDamage = true;
			}
		}
	}

	private bool IsValidTarget(IDestructible destr)
	{
		Character character = destr as Character;
		if ((bool)character)
		{
			if (character == m_owner)
			{
				return false;
			}
			if (m_owner != null)
			{
				bool flag = BaseAI.IsEnemy(m_owner, character) || ((bool)character.GetBaseAI() && character.GetBaseAI().IsAggravatable() && m_owner.IsPlayer());
				if (!m_owner.IsPlayer() && !flag)
				{
					return false;
				}
				if (m_owner.IsPlayer() && !m_owner.IsPVPEnabled() && !flag)
				{
					return false;
				}
			}
			if (m_dodgeable && character.IsDodgeInvincible())
			{
				return false;
			}
		}
		return true;
	}

	public void OnHit(Collider collider, Vector3 hitPoint, bool water, Vector3 normal)
	{
		GameObject gameObject = (collider ? FindHitObject(collider) : null);
		bool didDamage = false;
		bool hitCharacter = false;
		bool flag = m_bounce && normal != Vector3.zero;
		if (water)
		{
			flag = flag && m_bounceOnWater;
		}
		IDestructible destructible = (gameObject ? gameObject.GetComponent<IDestructible>() : null);
		if (destructible != null)
		{
			hitCharacter = destructible is Character;
			flag = flag && !hitCharacter;
			if (!IsValidTarget(destructible))
			{
				return;
			}
		}
		if (flag && m_bounceCount < m_maxBounces && m_vel.magnitude > m_minBounceVel)
		{
			Vector3 normalized = m_vel.normalized;
			if (m_bounceRoughness > 0f)
			{
				Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
				float f = Vector3.Dot(normal, onUnitSphere);
				onUnitSphere *= Mathf.Sign(f);
				normal = Vector3.Lerp(normal, onUnitSphere, m_bounceRoughness).normalized;
			}
			m_vel = Vector3.Reflect(normalized, normal) * (m_vel.magnitude * m_bouncePower);
			m_bounceCount++;
			m_didBounce = true;
			return;
		}
		if (m_aoe > 0f)
		{
			DoAOE(hitPoint, ref hitCharacter, ref didDamage);
		}
		else if (destructible != null)
		{
			HitData hitData = new HitData();
			hitData.m_hitCollider = collider;
			hitData.m_damage = m_damage;
			hitData.m_pushForce = m_attackForce;
			hitData.m_backstabBonus = m_backstabBonus;
			hitData.m_point = hitPoint;
			hitData.m_dir = base.transform.forward;
			hitData.m_statusEffectHash = m_statusEffectHash;
			hitData.m_dodgeable = m_dodgeable;
			hitData.m_blockable = m_blockable;
			hitData.m_ranged = true;
			hitData.m_skill = m_skill;
			hitData.m_skillRaiseAmount = m_raiseSkillAmount;
			hitData.SetAttacker(m_owner);
			hitData.m_hitType = ((!(hitData.GetAttacker() is Player)) ? HitData.HitType.EnemyHit : HitData.HitType.PlayerHit);
			hitData.m_healthReturn = m_healthReturn;
			destructible.Damage(hitData);
			if (m_healthReturn > 0f && (bool)m_owner)
			{
				m_owner.Heal(m_healthReturn);
			}
			didDamage = true;
		}
		if (water)
		{
			m_hitWaterEffects.Create(hitPoint, Quaternion.identity);
		}
		else
		{
			m_hitEffects.Create(hitPoint, Quaternion.identity);
		}
		if (m_spawnOnHit != null || m_spawnItem != null || m_randomSpawnOnHit.Count > 0)
		{
			SpawnOnHit(gameObject, collider, normal);
		}
		m_onHit?.Invoke(collider, hitPoint, water);
		if (m_hitNoise > 0f)
		{
			BaseAI.DoProjectileHitNoise(base.transform.position, m_hitNoise, m_owner);
		}
		if (didDamage && m_owner != null && hitCharacter)
		{
			m_owner.RaiseSkill(m_skill, m_raiseSkillAmount);
		}
		m_didHit = true;
		base.transform.position = hitPoint;
		m_nview.InvokeRPC("RPC_OnHit");
		m_ttl = m_stayTTL;
		if ((bool)collider && collider.attachedRigidbody != null)
		{
			ZNetView componentInParent = collider.gameObject.GetComponentInParent<ZNetView>();
			if ((bool)componentInParent && (m_attachToClosestBone || m_attachToRigidBody))
			{
				m_nview.InvokeRPC("RPC_Attach", componentInParent.GetZDO().m_uid);
			}
			else if (!m_stayAfterHitDynamic)
			{
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
		else if (!m_stayAfterHitStatic)
		{
			ZNetScene.instance.Destroy(base.gameObject);
		}
	}

	private void RPC_OnHit(long sender)
	{
		if ((bool)m_hideOnHit)
		{
			m_hideOnHit.SetActive(value: false);
		}
		if (m_stopEmittersOnHit)
		{
			ParticleSystem[] componentsInChildren = GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				ParticleSystem.EmissionModule emission = componentsInChildren[i].emission;
				emission.enabled = false;
			}
		}
	}

	private void RPC_Attach(long sender, ZDOID parent)
	{
		m_attachParent = ZNetScene.instance.FindInstance(parent);
		if (!m_attachParent)
		{
			return;
		}
		if (m_attachToClosestBone)
		{
			float dist = float.MaxValue;
			Animator componentInChildren = m_attachParent.gameObject.GetComponentInChildren<Animator>();
			if ((object)componentInChildren != null)
			{
				Utils.IterateHierarchy(componentInChildren.gameObject, delegate(GameObject obj)
				{
					float num = Vector3.Distance(base.transform.position, obj.transform.position);
					if (num < dist)
					{
						dist = num;
						m_attachParent = obj;
					}
				});
			}
		}
		base.transform.position += base.transform.forward * m_attachPenetration;
		base.transform.position += (m_attachParent.transform.position - base.transform.position) * m_attachBoneNearify;
		m_attachParentOffset = m_attachParent.transform.position - base.transform.position;
		m_attachParentOffsetRot = Quaternion.Inverse(m_attachParent.transform.localRotation * base.transform.localRotation);
	}

	private void SpawnOnHit(GameObject go, Collider collider, Vector3 normal)
	{
		if ((m_groundHitOnly && go.GetComponent<Heightmap>() == null) || (m_staticHitOnly && (((bool)collider && collider.attachedRigidbody != null) || ((bool)go && go.GetComponent<IDestructible>() != null))))
		{
			return;
		}
		Vector3 vector = base.transform.position + base.transform.TransformDirection(m_spawnOffset);
		Quaternion rotation = Quaternion.identity;
		if (m_copyProjectileRotation)
		{
			rotation = base.transform.rotation;
		}
		if (m_spawnRandomRotation)
		{
			rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
		}
		if (m_spawnFacingRotation)
		{
			rotation = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y, 0f);
		}
		if (m_spawnOnHit != null && (m_spawnOnHitChance >= 1f || UnityEngine.Random.value < m_spawnOnHitChance))
		{
			for (int i = 0; i < m_spawnCount; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(m_spawnOnHit, vector, rotation);
				Vector3 normalized = m_vel.normalized;
				Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
				if (m_spawnProjectileHemisphereDir)
				{
					onUnitSphere *= Mathf.Sign(Vector3.Dot(normal, onUnitSphere));
				}
				normalized = Vector3.Lerp(normalized, onUnitSphere, m_spawnProjectileRandomDir).normalized;
				float num = m_vel.magnitude;
				if (m_spawnProjectileNewVelocity)
				{
					num = UnityEngine.Random.Range(m_spawnProjectileMinVel, m_spawnProjectileMaxVel);
				}
				IProjectile componentInChildren = gameObject.GetComponentInChildren<IProjectile>();
				if (componentInChildren == null)
				{
					continue;
				}
				gameObject.transform.position += normal * 0.25f;
				HitData hitData = null;
				if (m_projectilesInheritHitData)
				{
					hitData = m_originalHitData;
					if (m_divideDamageBetweenProjectiles)
					{
						hitData.m_damage.Modify(1f / (float)m_spawnCount);
					}
				}
				componentInChildren.Setup(m_owner, normalized * num, m_hitNoise, hitData, m_weapon, m_ammo);
			}
		}
		if (m_spawnItem != null)
		{
			ItemDrop.DropItem(m_spawnItem, 0, vector, base.transform.rotation);
		}
		if (m_randomSpawnOnHit.Count > 0 && (!m_randomSpawnSkipLava || !ZoneSystem.instance.IsLava(base.transform.position)))
		{
			for (int j = 0; j < m_randomSpawnOnHitCount; j++)
			{
				GameObject gameObject2 = m_randomSpawnOnHit[UnityEngine.Random.Range(0, m_randomSpawnOnHit.Count)];
				if ((bool)gameObject2)
				{
					UnityEngine.Object.Instantiate(gameObject2, vector, rotation).GetComponent<IProjectile>()?.Setup(m_owner, m_vel, m_hitNoise, null, null, m_ammo);
				}
			}
		}
		m_spawnOnHitEffects.Create(vector, Quaternion.identity);
	}

	public static GameObject FindHitObject(Collider collider)
	{
		IDestructible componentInParent = collider.gameObject.GetComponentInParent<IDestructible>();
		if (componentInParent != null)
		{
			return (componentInParent as MonoBehaviour).gameObject;
		}
		if ((bool)collider.attachedRigidbody)
		{
			return collider.attachedRigidbody.gameObject;
		}
		return collider.gameObject;
	}

	public void TriggerShieldsLeftFlag()
	{
		m_hasLeftShields = true;
	}
}
