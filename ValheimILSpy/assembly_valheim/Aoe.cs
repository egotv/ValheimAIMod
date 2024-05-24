using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Aoe : MonoBehaviour, IProjectile, IMonoUpdater
{
	public string m_name = "";

	[Header("Attack (overridden by item )")]
	public bool m_useAttackSettings = true;

	public HitData.DamageTypes m_damage;

	public bool m_scaleDamageByDistance;

	public AnimationCurve m_distanceScaleCurve = AnimationCurve.Linear(1f, 1f, 0f, 0f);

	public bool m_dodgeable;

	public bool m_blockable;

	public int m_toolTier;

	public float m_attackForce;

	public float m_backstabBonus = 4f;

	public string m_statusEffect = "";

	public string m_statusEffectIfBoss = "";

	public string m_statusEffectIfPlayer = "";

	private int m_statusEffectHash;

	private int m_statusEffectIfBossHash;

	private int m_statusEffectIfPlayerHash;

	[Header("Attack (other)")]
	public HitData.DamageTypes m_damagePerLevel;

	public bool m_attackForceForward;

	public GameObject m_spawnOnHitTerrain;

	public bool m_hitTerrainOnlyOnce;

	public FootStep.GroundMaterial m_spawnOnGroundType = FootStep.GroundMaterial.Everything;

	public float m_groundLavaValue = -1f;

	public float m_hitNoise;

	public bool m_placeOnGround;

	public bool m_randomRotation;

	public int m_maxTargetsFromCenter;

	[Header("Multi Spawn (Lava Bomb)")]
	public int m_multiSpawnMin;

	public int m_multiSpawnMax;

	public float m_multiSpawnDistanceMin;

	public float m_multiSpawnDistanceMax;

	public float m_multiSpawnScaleMin;

	public float m_multiSpawnScaleMax;

	public float m_multiSpawnSpringDelayMax;

	[Header("Chain Spawn")]
	public float m_chainStartChance;

	public float m_chainStartChanceFalloff = 0.8f;

	public float m_chainChancePerTarget;

	public GameObject m_chainObj;

	public float m_chainStartDelay;

	public int m_chainMinTargets;

	public int m_chainMaxTargets;

	public EffectList m_chainEffects = new EffectList();

	private float m_chainDelay;

	private float m_chainChance;

	[Header("Damage self")]
	public float m_damageSelf;

	[Header("Ignore targets")]
	public bool m_hitOwner;

	public bool m_hitParent = true;

	public bool m_hitSame;

	public bool m_hitFriendly = true;

	public bool m_hitEnemy = true;

	public bool m_hitCharacters = true;

	public bool m_hitProps = true;

	public bool m_hitTerrain;

	public bool m_ignorePVP;

	[Header("Launch Characters")]
	public bool m_launchCharacters;

	public Vector2 m_launchForceMinMax = Vector2.up;

	[Range(0f, 1f)]
	public float m_launchForceUpFactor = 0.5f;

	[Header("Other")]
	public Skills.SkillType m_skill;

	public bool m_canRaiseSkill = true;

	public bool m_useTriggers;

	public bool m_triggerEnterOnly;

	public BoxCollider m_useCollider;

	public float m_radius = 4f;

	[Tooltip("Wait this long before we start doing any damage")]
	public float m_activationDelay;

	public float m_ttl = 4f;

	[Tooltip("When set, ttl will be a random value between ttl and ttlMax")]
	public float m_ttlMax;

	public bool m_hitAfterTtl;

	public float m_hitInterval = 1f;

	public bool m_hitOnEnable;

	public bool m_attachToCaster;

	public EffectList m_hitEffects = new EffectList();

	public EffectList m_initiateEffect = new EffectList();

	private static Collider[] s_hits = new Collider[100];

	private static List<Collider> s_hitList = new List<Collider>();

	private static int s_hitListCount;

	private static List<GameObject> s_chainObjs = new List<GameObject>();

	private ZNetView m_nview;

	private Character m_owner;

	private readonly List<GameObject> m_hitList = new List<GameObject>();

	private float m_hitTimer;

	private float m_activationTimer;

	private Vector3 m_offset = Vector3.zero;

	private Quaternion m_localRot = Quaternion.identity;

	private int m_level;

	private int m_worldLevel = -1;

	private int m_rayMask;

	private bool m_gaveSkill;

	private bool m_hasHitTerrain;

	private bool m_initRun = true;

	private HitData m_hitData;

	private ItemDrop.ItemData m_itemData;

	private ItemDrop.ItemData m_ammo;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		m_nview = GetComponentInParent<ZNetView>();
		m_rayMask = 0;
		if (m_hitCharacters)
		{
			m_rayMask |= LayerMask.GetMask("character", "character_net", "character_ghost");
		}
		if (m_hitProps)
		{
			m_rayMask |= LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "hitbox", "character_noenv", "vehicle");
		}
		if (m_hitTerrain)
		{
			m_rayMask |= LayerMask.GetMask("terrain");
		}
		if (!string.IsNullOrEmpty(m_statusEffect))
		{
			m_statusEffectHash = m_statusEffect.GetStableHashCode();
		}
		if (!string.IsNullOrEmpty(m_statusEffectIfBoss))
		{
			m_statusEffectIfBossHash = m_statusEffectIfBoss.GetStableHashCode();
		}
		if (!string.IsNullOrEmpty(m_statusEffectIfPlayer))
		{
			m_statusEffectIfPlayerHash = m_statusEffectIfPlayer.GetStableHashCode();
		}
		m_activationTimer = m_activationDelay;
		if (m_ttlMax > 0f)
		{
			m_ttl = Random.Range(m_ttl, m_ttlMax);
		}
		m_chainDelay = m_chainStartDelay;
		if (m_chainChance == 0f)
		{
			m_chainChance = m_chainStartChance;
		}
	}

	protected virtual void OnEnable()
	{
		m_initRun = true;
		Instances.Add(this);
	}

	protected virtual void OnDisable()
	{
		Instances.Remove(this);
	}

	private HitData.DamageTypes GetDamage()
	{
		return GetDamage(m_level);
	}

	private HitData.DamageTypes GetDamage(int itemQuality)
	{
		if (itemQuality <= 1)
		{
			return m_damage;
		}
		HitData.DamageTypes damage = m_damage;
		int num = ((m_worldLevel >= 0) ? m_worldLevel : Game.m_worldLevel);
		if (num > 0)
		{
			damage.IncreaseEqually(num * Game.instance.m_worldLevelGearBaseDamage, seperateUtilityDamage: true);
		}
		damage.Add(m_damagePerLevel, itemQuality - 1);
		return damage;
	}

	public string GetTooltipString(int itemQuality)
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		stringBuilder.Append("AOE");
		stringBuilder.Append(GetDamage(itemQuality).GetTooltipString());
		stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", m_attackForce);
		stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>", m_backstabBonus);
		return stringBuilder.ToString();
	}

	private void Update()
	{
		if (m_activationTimer > 0f)
		{
			m_activationTimer -= Time.deltaTime;
		}
		if (m_hitInterval > 0f && m_useTriggers)
		{
			m_hitTimer -= Time.deltaTime;
			if (m_hitTimer <= 0f)
			{
				m_hitTimer = m_hitInterval;
				m_hitList.Clear();
			}
		}
	}

	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (m_nview != null && !m_nview.IsOwner())
		{
			return;
		}
		if (m_initRun && !m_useTriggers && !m_hitAfterTtl && m_activationTimer <= 0f)
		{
			m_initRun = false;
			if (m_hitInterval <= 0f)
			{
				Initiate();
			}
		}
		if (m_owner != null && m_attachToCaster)
		{
			base.transform.position = m_owner.transform.TransformPoint(m_offset);
			base.transform.rotation = m_owner.transform.rotation * m_localRot;
		}
		if (m_activationTimer > 0f)
		{
			return;
		}
		if (m_hitInterval > 0f && !m_useTriggers)
		{
			m_hitTimer -= fixedDeltaTime;
			if (m_hitTimer <= 0f)
			{
				m_hitTimer = m_hitInterval;
				Initiate();
			}
		}
		if (m_chainStartChance > 0f && m_chainDelay >= 0f)
		{
			m_chainDelay -= fixedDeltaTime;
			if (m_chainDelay <= 0f && Random.value < m_chainStartChance)
			{
				Vector3 position = base.transform.position;
				FindHits();
				SortHits();
				int num = Random.Range(m_chainMinTargets, m_chainMaxTargets + 1);
				foreach (Collider s_hit in s_hitList)
				{
					if (Random.value < m_chainChancePerTarget)
					{
						Vector3 position2 = s_hit.gameObject.transform.position;
						bool flag = false;
						for (int i = 0; i < s_chainObjs.Count; i++)
						{
							if ((bool)s_chainObjs[i])
							{
								if (Vector3.Distance(s_chainObjs[i].transform.position, position2) < 0.1f)
								{
									flag = true;
									break;
								}
							}
							else
							{
								s_chainObjs.RemoveAt(i);
							}
						}
						if (!flag)
						{
							GameObject gameObject = Object.Instantiate(m_chainObj, position2, s_hit.gameObject.transform.rotation);
							s_chainObjs.Add(gameObject);
							IProjectile componentInChildren = gameObject.GetComponentInChildren<IProjectile>();
							if (componentInChildren != null)
							{
								componentInChildren.Setup(m_owner, position.DirTo(position2), -1f, m_hitData, m_itemData, m_ammo);
								if (componentInChildren is Aoe aoe)
								{
									aoe.m_chainChance = m_chainChance * m_chainStartChanceFalloff;
								}
							}
							num--;
							float num2 = Vector3.Distance(position2, base.transform.position);
							GameObject[] array = m_chainEffects.Create(position + Vector3.up, Quaternion.LookRotation(position.DirTo(position2 + Vector3.up)));
							for (int j = 0; j < array.Length; j++)
							{
								array[j].transform.localScale = Vector3.one * num2;
							}
						}
					}
					if (num <= 0)
					{
						break;
					}
				}
			}
		}
		if (!(m_ttl > 0f))
		{
			return;
		}
		m_ttl -= fixedDeltaTime;
		if (m_ttl <= 0f)
		{
			if (m_hitAfterTtl)
			{
				Initiate();
			}
			if ((bool)ZNetScene.instance)
			{
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
	}

	public void Initiate()
	{
		m_initiateEffect.Create(base.transform.position, Quaternion.identity);
		CheckHits();
	}

	private void CheckHits()
	{
		FindHits();
		if (m_maxTargetsFromCenter > 0)
		{
			SortHits();
			int num = m_maxTargetsFromCenter;
			{
				foreach (Collider s_hit in s_hitList)
				{
					if (OnHit(s_hit, s_hit.transform.position))
					{
						num--;
					}
					if (num <= 0)
					{
						break;
					}
				}
				return;
			}
		}
		for (int i = 0; i < s_hitList.Count; i++)
		{
			OnHit(s_hitList[i], s_hitList[i].transform.position);
		}
	}

	private void FindHits()
	{
		m_hitList.Clear();
		int num = ((m_useCollider != null) ? Physics.OverlapBoxNonAlloc(base.transform.position + m_useCollider.center, m_useCollider.size / 2f, s_hits, base.transform.rotation, m_rayMask) : Physics.OverlapSphereNonAlloc(base.transform.position, m_radius, s_hits, m_rayMask));
		s_hitList.Clear();
		for (int i = 0; i < num; i++)
		{
			Collider collider = s_hits[i];
			if (ShouldHit(collider))
			{
				s_hitList.Add(collider);
			}
		}
	}

	private bool ShouldHit(Collider collider)
	{
		GameObject gameObject = Projectile.FindHitObject(collider);
		if ((bool)gameObject)
		{
			Character component = gameObject.GetComponent<Character>();
			if ((object)component != null)
			{
				if (m_nview == null && !component.IsOwner())
				{
					return false;
				}
				if (m_owner != null)
				{
					if (!m_hitOwner && component == m_owner)
					{
						return false;
					}
					if (!m_hitSame && component.m_name == m_owner.m_name)
					{
						return false;
					}
					bool flag = BaseAI.IsEnemy(m_owner, component) || ((bool)component.GetBaseAI() && component.GetBaseAI().IsAggravatable() && m_owner.IsPlayer());
					if (!m_hitFriendly && !flag)
					{
						return false;
					}
					if (!m_hitEnemy && flag)
					{
						return false;
					}
				}
				if (!m_hitCharacters)
				{
					return false;
				}
				if (m_dodgeable && component.IsDodgeInvincible())
				{
					return false;
				}
			}
		}
		return true;
	}

	private void SortHits()
	{
		s_hitList.Sort((Collider a, Collider b) => Vector3.Distance(a.transform.position, base.transform.position).CompareTo(Vector3.Distance(b.transform.position, base.transform.position)));
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		m_owner = owner;
		if (item != null)
		{
			m_level = item.m_quality;
			m_worldLevel = item.m_worldLevel;
			m_itemData = item;
		}
		if (m_attachToCaster && owner != null)
		{
			m_offset = owner.transform.InverseTransformPoint(base.transform.position);
			m_localRot = Quaternion.Inverse(owner.transform.rotation) * base.transform.rotation;
		}
		if (hitData != null && m_useAttackSettings)
		{
			m_damage = hitData.m_damage;
			m_blockable = hitData.m_blockable;
			m_dodgeable = hitData.m_dodgeable;
			m_attackForce = hitData.m_pushForce;
			m_backstabBonus = hitData.m_backstabBonus;
			if (m_statusEffectHash != hitData.m_statusEffectHash)
			{
				m_statusEffectHash = hitData.m_statusEffectHash;
				m_statusEffect = "<changed>";
			}
			m_toolTier = hitData.m_toolTier;
			m_skill = hitData.m_skill;
		}
		m_ammo = ammo;
		m_hitData = hitData;
	}

	private void OnCollisionEnter(Collision collision)
	{
		CauseTriggerDamage(collision.collider, onTriggerEnter: true);
	}

	private void OnCollisionStay(Collision collision)
	{
		CauseTriggerDamage(collision.collider, onTriggerEnter: false);
	}

	private void OnTriggerEnter(Collider collider)
	{
		CauseTriggerDamage(collider, onTriggerEnter: true);
	}

	private void OnTriggerStay(Collider collider)
	{
		CauseTriggerDamage(collider, onTriggerEnter: false);
	}

	private void CauseTriggerDamage(Collider collider, bool onTriggerEnter)
	{
		if (!(m_triggerEnterOnly && onTriggerEnter) && !(m_activationTimer > 0f))
		{
			if (!m_useTriggers)
			{
				ZLog.LogWarning("AOE got OnTriggerStay but trigger damage is disabled in " + base.gameObject.name);
			}
			else if (ShouldHit(collider))
			{
				OnHit(collider, collider.bounds.center);
			}
		}
	}

	private bool OnHit(Collider collider, Vector3 hitPoint)
	{
		GameObject gameObject = Projectile.FindHitObject(collider);
		if (m_hitList.Contains(gameObject))
		{
			return false;
		}
		m_hitList.Add(gameObject);
		float num = 1f;
		if ((bool)m_owner && m_owner.IsPlayer() && m_skill != 0)
		{
			num = m_owner.GetRandomSkillFactor(m_skill);
		}
		bool result = false;
		bool flag = false;
		float num2 = 1f;
		if (m_scaleDamageByDistance)
		{
			num2 = m_distanceScaleCurve.Evaluate(Mathf.Clamp01(Vector3.Distance(gameObject.transform.position, base.transform.position) / m_radius));
		}
		IDestructible component = gameObject.GetComponent<IDestructible>();
		if (component != null)
		{
			if (!m_hitParent)
			{
				if (!(base.gameObject.transform.parent != null) || !(gameObject == base.gameObject.transform.parent.gameObject))
				{
					IDestructible componentInParent = base.gameObject.GetComponentInParent<IDestructible>();
					if (componentInParent == null || componentInParent != component)
					{
						goto IL_0109;
					}
				}
				return false;
			}
			goto IL_0109;
		}
		Heightmap component2 = gameObject.GetComponent<Heightmap>();
		if ((object)component2 != null)
		{
			FootStep.GroundMaterial groundMaterial = component2.GetGroundMaterial(Vector3.up, base.transform.position, m_groundLavaValue);
			FootStep.GroundMaterial groundMaterial2 = component2.GetGroundMaterial(Vector3.up, base.transform.position);
			FootStep.GroundMaterial groundMaterial3 = ((m_groundLavaValue >= 0f) ? groundMaterial : groundMaterial2);
			if ((bool)m_spawnOnHitTerrain && (m_spawnOnGroundType == FootStep.GroundMaterial.Everything || m_spawnOnGroundType.HasFlag(groundMaterial3)) && (!m_hitTerrainOnlyOnce || !m_hasHitTerrain))
			{
				m_hasHitTerrain = true;
				int num3 = ((m_multiSpawnMin == 0) ? 1 : Random.Range(m_multiSpawnMin, m_multiSpawnMax));
				Vector3 position = base.transform.position;
				for (int i = 0; i < num3; i++)
				{
					GameObject gameObject2 = Attack.SpawnOnHitTerrain(position, m_spawnOnHitTerrain, m_owner, m_hitNoise, null, null, m_randomRotation);
					float num4 = ((num3 == 1) ? 0f : ((float)i / (float)(num3 - 1)));
					float num5 = Random.Range(m_multiSpawnDistanceMin, m_multiSpawnDistanceMax);
					Vector2 insideUnitCircle = Random.insideUnitCircle;
					position += new Vector3(insideUnitCircle.x * num5, 0f, insideUnitCircle.y * num5);
					if ((bool)gameObject2 && i > 0)
					{
						gameObject2.transform.localScale = Utils.Vec3((1f - num4) * (m_multiSpawnScaleMax - m_multiSpawnScaleMin) + m_multiSpawnScaleMin);
					}
					if (m_multiSpawnSpringDelayMax > 0f)
					{
						ConditionalObject componentInChildren = gameObject2.GetComponentInChildren<ConditionalObject>();
						if ((object)componentInChildren != null)
						{
							componentInChildren.m_appearDelay = num4 * m_multiSpawnSpringDelayMax;
						}
					}
					if (m_placeOnGround)
					{
						gameObject2.transform.position = new Vector3(gameObject2.transform.position.x, ZoneSystem.instance.GetGroundHeight(gameObject2.transform.position), gameObject2.transform.position.z);
					}
				}
			}
			result = true;
		}
		goto IL_0644;
		IL_0644:
		if ((object)gameObject.GetComponent<MineRock5>() == null)
		{
			m_hitEffects.Create(hitPoint, Quaternion.identity);
		}
		if (!m_gaveSkill && (bool)m_owner && m_skill != Skills.SkillType.None && flag && m_canRaiseSkill)
		{
			m_owner.RaiseSkill(m_skill);
			m_gaveSkill = true;
		}
		return result;
		IL_0109:
		Character character = component as Character;
		if ((bool)character)
		{
			if (m_launchCharacters)
			{
				float num6 = Random.Range(m_launchForceMinMax.x, m_launchForceMinMax.y);
				num6 *= num2;
				Vector3 a = hitPoint.DirTo(base.transform.position);
				if (m_launchForceUpFactor > 0f)
				{
					a = Vector3.Slerp(a, Vector3.up, m_launchForceUpFactor);
				}
				character.ForceJump(a.normalized * num6);
			}
			flag = true;
		}
		else if (!m_hitProps)
		{
			return false;
		}
		bool flag2 = (component is Destructible destructible && destructible.m_spawnWhenDestroyed != null) || (object)gameObject.GetComponent<MineRock5>() != null;
		Vector3 dir = (m_attackForceForward ? base.transform.forward : (hitPoint - base.transform.position).normalized);
		HitData hitData = new HitData();
		hitData.m_hitCollider = collider;
		hitData.m_damage = GetDamage();
		hitData.m_pushForce = m_attackForce * num * num2;
		hitData.m_backstabBonus = m_backstabBonus;
		hitData.m_point = (flag2 ? base.transform.position : hitPoint);
		hitData.m_dir = dir;
		hitData.m_statusEffectHash = GetStatusEffect(character);
		hitData.m_skillLevel = m_owner?.GetSkillLevel(m_skill) ?? 0f;
		hitData.m_itemLevel = (short)m_level;
		hitData.m_itemWorldLevel = (byte)((m_worldLevel >= 0) ? m_worldLevel : Game.m_worldLevel);
		hitData.m_dodgeable = m_dodgeable;
		hitData.m_blockable = m_blockable;
		hitData.m_ranged = true;
		hitData.m_ignorePVP = m_owner == character || m_ignorePVP;
		hitData.m_toolTier = (short)m_toolTier;
		hitData.SetAttacker(m_owner);
		hitData.m_damage.Modify(num);
		hitData.m_damage.Modify(num2);
		hitData.m_hitType = ((!(hitData.GetAttacker() is Player)) ? HitData.HitType.EnemyHit : HitData.HitType.PlayerHit);
		hitData.m_radius = m_radius;
		component.Damage(hitData);
		if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("damage"))
		{
			Terminal.Log("Damage AOE: hitting target" + ((m_owner == null) ? " without owner" : (" with owner: " + m_owner)));
		}
		if (m_damageSelf > 0f)
		{
			IDestructible componentInParent2 = GetComponentInParent<IDestructible>();
			if (componentInParent2 != null)
			{
				HitData hitData2 = new HitData();
				hitData2.m_damage.m_damage = m_damageSelf;
				hitData2.m_point = hitPoint;
				hitData2.m_blockable = false;
				hitData2.m_dodgeable = false;
				hitData2.m_hitType = HitData.HitType.Self;
				componentInParent2.Damage(hitData2);
			}
		}
		result = true;
		goto IL_0644;
	}

	private int GetStatusEffect(Character character)
	{
		if ((bool)character)
		{
			if (character.IsBoss() && m_statusEffectIfBossHash != 0)
			{
				return m_statusEffectIfBossHash;
			}
			if (character.IsPlayer() && m_statusEffectIfPlayerHash != 0)
			{
				return m_statusEffectIfPlayerHash;
			}
		}
		return m_statusEffectHash;
	}

	private void OnDrawGizmos()
	{
		_ = m_useTriggers;
	}
}
