using System;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour, IDestructible, Hoverable, IWaterInteractable, IMonoUpdater
{
	public enum Faction
	{
		Players,
		AnimalsVeg,
		ForestMonsters,
		Undead,
		Demon,
		MountainMonsters,
		SeaMonsters,
		PlainsMonsters,
		Boss,
		MistlandsMonsters,
		Dverger,
		PlayerSpawned
	}

	public enum GroundTiltType
	{
		None,
		Pitch,
		Full,
		PitchRaycast,
		FullRaycast,
		Flying
	}

	private float m_underWorldCheckTimer;

	private static float takeInputDelay = 0f;

	private float currentRotSpeedFactor;

	private float m_standUp;

	private const float c_StandUpTime = 2f;

	private const float c_StandUpTriggerState = -100f;

	private Collider m_lowestContactCollider;

	private bool m_groundContact;

	private Vector3 m_groundContactPoint = Vector3.zero;

	private Vector3 m_groundContactNormal = Vector3.zero;

	private Transform oldParent;

	private int m_cachedCurrentAnimHash;

	private int m_cachedNextAnimHash;

	private int m_cachedNextOrCurrentAnimHash;

	private int m_cachedAnimHashFrame;

	public ZNetView m_nViewOverride;

	public Action<float, Character> m_onDamaged;

	public Action m_onDeath;

	public Action<int> m_onLevelSet;

	public Action<Vector3> m_onLand;

	[Header("Character")]
	public string m_name = "";

	public string m_group = "";

	public Faction m_faction = Faction.AnimalsVeg;

	public bool m_boss;

	public bool m_dontHideBossHud;

	public string m_bossEvent = "";

	[Tooltip("Also sets player unique key")]
	public string m_defeatSetGlobalKey = "";

	public bool m_aiSkipTarget;

	[Header("Movement & Physics")]
	public float m_crouchSpeed = 2f;

	public float m_walkSpeed = 5f;

	public float m_speed = 10f;

	public float m_turnSpeed = 300f;

	public float m_runSpeed = 20f;

	public float m_runTurnSpeed = 300f;

	public float m_flySlowSpeed = 5f;

	public float m_flyFastSpeed = 12f;

	public float m_flyTurnSpeed = 12f;

	public float m_acceleration = 1f;

	public float m_jumpForce = 10f;

	public float m_jumpForceForward;

	public float m_jumpForceTiredFactor = 0.7f;

	public float m_airControl = 0.1f;

	public bool m_canSwim = true;

	public float m_swimDepth = 2f;

	public float m_swimSpeed = 2f;

	public float m_swimTurnSpeed = 100f;

	public float m_swimAcceleration = 0.05f;

	public GroundTiltType m_groundTilt;

	public float m_groundTiltSpeed = 50f;

	public bool m_flying;

	public float m_jumpStaminaUsage = 10f;

	public bool m_disableWhileSleeping;

	[Header("Bodyparts")]
	public Transform m_eye;

	protected Transform m_head;

	[Header("Effects")]
	public EffectList m_hitEffects = new EffectList();

	public EffectList m_critHitEffects = new EffectList();

	public EffectList m_backstabHitEffects = new EffectList();

	public EffectList m_deathEffects = new EffectList();

	public EffectList m_waterEffects = new EffectList();

	public EffectList m_tarEffects = new EffectList();

	public EffectList m_slideEffects = new EffectList();

	public EffectList m_jumpEffects = new EffectList();

	public EffectList m_flyingContinuousEffect = new EffectList();

	[Header("Health & Damage")]
	public bool m_tolerateWater = true;

	public bool m_tolerateFire;

	public bool m_tolerateSmoke = true;

	public bool m_tolerateTar;

	public float m_health = 10f;

	public HitData.DamageModifiers m_damageModifiers;

	public WeakSpot[] m_weakSpots;

	public bool m_staggerWhenBlocked = true;

	public float m_staggerDamageFactor;

	private float m_staggerDamage;

	private float m_backstabTime = -99999f;

	private GameObject[] m_waterEffects_instances;

	private GameObject[] m_slideEffects_instances;

	private GameObject[] m_flyingEffects_instances;

	protected Vector3 m_moveDir = Vector3.zero;

	protected Vector3 m_lookDir = Vector3.forward;

	protected Quaternion m_lookYaw = Quaternion.identity;

	protected bool m_run;

	protected bool m_walk;

	private Vector3 m_lookTransitionStart;

	private Vector3 m_lookTransitionTarget;

	protected float m_lookTransitionTime;

	protected float m_lookTransitionTimeTotal;

	protected bool m_attack;

	protected bool m_attackHold;

	protected bool m_secondaryAttack;

	protected bool m_secondaryAttackHold;

	protected bool m_blocking;

	protected GameObject m_visual;

	protected LODGroup m_lodGroup;

	protected Rigidbody m_body;

	protected CapsuleCollider m_collider;

	protected ZNetView m_nview;

	protected ZSyncAnimation m_zanim;

	protected Animator m_animator;

	protected CharacterAnimEvent m_animEvent;

	protected BaseAI m_baseAI;

	private const float c_MaxFallHeight = 20f;

	private const float c_MinFallHeight = 4f;

	private const float c_MaxFallDamage = 100f;

	private const float c_StaggerDamageBonus = 2f;

	private const float c_AutoJumpInterval = 0.5f;

	private const float c_MinSlideDegreesPlayer = 38f;

	private const float c_MinSlideDegreesMount = 45f;

	private const float c_MinSlideDegreesMonster = 90f;

	private const float c_RootMotionMultiplier = 55f;

	private const float c_PushForceScale = 2.5f;

	private const float c_ContinuousPushForce = 20f;

	private const float c_PushForceDissipation = 100f;

	private const float c_MaxMoveForce = 20f;

	private const float c_StaggerResetTime = 5f;

	private const float c_BackstabResetTime = 300f;

	private const float m_slopeStaminaDrain = 10f;

	public const float m_minSlideDegreesPlayer = 38f;

	public const float m_minSlideDegreesMount = 45f;

	public const float m_minSlideDegreesMonster = 90f;

	private const float m_rootMotionMultiplier = 55f;

	private const float m_pushForceScale = 2.5f;

	private const float m_continousPushForce = 20f;

	private const float m_pushForcedissipation = 100f;

	private const float m_maxMoveForce = 20f;

	private const float m_staggerResetTime = 5f;

	private const float m_backstabResetTime = 300f;

	private float m_jumpTimer;

	private float m_lastAutoJumpTime;

	private float m_lastGroundTouch;

	private Vector3 m_lastGroundNormal = Vector3.up;

	private Vector3 m_lastGroundPoint = Vector3.up;

	private Collider m_lastGroundCollider;

	private Rigidbody m_lastGroundBody;

	private float m_groundForceTimer;

	private float m_originalMass;

	private Vector3 m_lastAttachPos = Vector3.zero;

	private Rigidbody m_lastAttachBody;

	protected float m_maxAirAltitude = -10000f;

	private float m_waterLevel = -10000f;

	private float m_tarLevel = -10000f;

	private float m_liquidLevel = -10000f;

	private float m_swimTimer = 999f;

	private float m_lavaTimer = 999f;

	private float m_aboveOrInLavaTimer = 999f;

	private float m_fallTimer;

	protected SEMan m_seman;

	private float m_noiseRange;

	private float m_syncNoiseTimer;

	private bool m_tamed;

	private float m_lastTamedCheck;

	private Tameable m_tameable;

	private MonsterAI m_tameableMonsterAI;

	private int m_level = 1;

	private RaycastHit[] m_lavaRoofCheck = new RaycastHit[1];

	private bool m_localPlayerHasHit;

	protected HitData m_lastHit;

	private Vector3 m_currentVel = Vector3.zero;

	private float m_currentTurnVel;

	private float m_currentTurnVelChange;

	private Vector3 m_groundTiltNormal = Vector3.up;

	protected Vector3 m_pushForce = Vector3.zero;

	private Vector3 m_rootMotion = Vector3.zero;

	private static readonly int s_forwardSpeed = ZSyncAnimation.GetHash("forward_speed");

	private static readonly int s_sidewaySpeed = ZSyncAnimation.GetHash("sideway_speed");

	private static readonly int s_turnSpeed = ZSyncAnimation.GetHash("turn_speed");

	private static readonly int s_inWater = ZSyncAnimation.GetHash("inWater");

	private static readonly int s_onGround = ZSyncAnimation.GetHash("onGround");

	private static readonly int s_encumbered = ZSyncAnimation.GetHash("encumbered");

	private static readonly int s_flying = ZSyncAnimation.GetHash("flying");

	private float m_slippage;

	protected bool m_wallRunning;

	private bool m_sliding;

	private bool m_running;

	private bool m_walking;

	private Vector3 m_originalLocalRef;

	private bool m_lodVisible = true;

	private static int s_smokeRayMask = 0;

	private float m_smokeCheckTimer;

	[Header("Heat & Lava")]
	public float m_minLavaMaskThreshold = 0.1f;

	public float m_maxLavaMaskThreshold = 0.33f;

	public float m_heatBuildupBase = 1.5f;

	public float m_heatCooldownBase = 1f;

	public float m_heatBuildupWater = 2f;

	public float m_heatWaterTouchMultiplier = 0.2f;

	public float m_lavaDamageTickInterval = 0.2f;

	public float m_heatLevelFirstDamageThreshold = 0.7f;

	public float m_lavaFirstDamage = 10f;

	public float m_lavaFullDamage = 100f;

	public float m_lavaAirDamageHeight = 3f;

	public float m_dayHeatGainRunning = 0.2f;

	public float m_dayHeatGainStill = -0.05f;

	public float m_dayHeatEquipmentStop = 0.5f;

	public float m_lavaSlowMax = 0.5f;

	public float m_lavaSlowHeight = 0.8f;

	public EffectList m_lavaHeatEffects = new EffectList();

	private Dictionary<ParticleSystem, float> m_lavaHeatParticles = new Dictionary<ParticleSystem, float>();

	private List<ZSFX> m_lavaHeatAudio = new List<ZSFX>();

	private float m_lavaHeatLevel;

	private float m_lavaProximity;

	private float m_lavaHeightFactor;

	private float m_lavaDamageTimer;

	private static bool s_dpsDebugEnabled = false;

	private static readonly List<KeyValuePair<float, float>> s_enemyDamage = new List<KeyValuePair<float, float>>();

	private static readonly List<KeyValuePair<float, float>> s_playerDamage = new List<KeyValuePair<float, float>>();

	private static readonly List<Character> s_characters = new List<Character>();

	private static int s_characterLayer = 0;

	private static int s_characterNetLayer = 0;

	private static int s_characterGhostLayer = 0;

	protected static int s_groundRayMask = 0;

	protected static int s_characterLayerMask = 0;

	private static int s_blockedRayMask;

	private float m_cashedInLiquidDepth;

	protected static readonly int s_animatorTagFreeze = ZSyncAnimation.GetHash("freeze");

	protected static readonly int s_animatorTagStagger = ZSyncAnimation.GetHash("stagger");

	protected static readonly int s_animatorTagSitting = ZSyncAnimation.GetHash("sitting");

	private static readonly int s_animatorFalling = ZSyncAnimation.GetHash("falling");

	private static readonly int s_tilt = ZSyncAnimation.GetHash("tilt");

	private static readonly int s_statusEffectBurning = "Burning".GetStableHashCode();

	private static readonly int s_statusEffectFrost = "Frost".GetStableHashCode();

	private static readonly int s_statusEffectLightning = "Lightning".GetStableHashCode();

	private static readonly int s_statusEffectPoison = "Poison".GetStableHashCode();

	private static readonly int s_statusEffectSmoked = "Smoked".GetStableHashCode();

	private static readonly int s_statusEffectSpirit = "Spirit".GetStableHashCode();

	private static readonly int s_statusEffectTared = "Tared".GetStableHashCode();

	private static readonly int s_statusEffectWet = "Wet".GetStableHashCode();

	public static int m_debugFlySpeed = 20;

	private readonly int[] m_liquids = new int[2];

	public int InNumShipVolumes { get; set; }

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	protected virtual void Awake()
	{
		s_characters.Add(this);
		m_collider = GetComponent<CapsuleCollider>();
		m_body = GetComponent<Rigidbody>();
		m_zanim = GetComponent<ZSyncAnimation>();
		m_nview = ((m_nViewOverride != null) ? m_nViewOverride : GetComponent<ZNetView>());
		m_animator = GetComponentInChildren<Animator>();
		m_animEvent = m_animator.GetComponent<CharacterAnimEvent>();
		m_baseAI = GetComponent<BaseAI>();
		m_animator.logWarnings = false;
		m_visual = base.transform.Find("Visual").gameObject;
		m_lodGroup = m_visual.GetComponent<LODGroup>();
		m_head = Utils.GetBoneTransform(m_animator, HumanBodyBones.Head);
		m_body.maxDepenetrationVelocity = 2f;
		m_originalMass = m_body.mass;
		if (s_smokeRayMask == 0)
		{
			s_smokeRayMask = LayerMask.GetMask("smoke");
			s_characterLayer = LayerMask.NameToLayer("character");
			s_characterNetLayer = LayerMask.NameToLayer("character_net");
			s_characterGhostLayer = LayerMask.NameToLayer("character_ghost");
			s_groundRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "blocker", "vehicle");
			s_characterLayerMask = LayerMask.GetMask("character", "character_noenv");
			s_blockedRayMask = LayerMask.GetMask("piece", "Default", "static_solid", "Default_small", "terrain");
		}
		if ((bool)m_lodGroup)
		{
			m_originalLocalRef = m_lodGroup.localReferencePoint;
		}
		m_seman = new SEMan(this, m_nview);
		if (m_nview.GetZDO() != null)
		{
			if (!IsPlayer())
			{
				m_tamed = m_nview.GetZDO().GetBool(ZDOVars.s_tamed, m_tamed);
				m_level = m_nview.GetZDO().GetInt(ZDOVars.s_level, 1);
				if (m_nview.IsOwner() && GetHealth() == GetMaxHealth())
				{
					SetupMaxHealth();
				}
			}
			m_nview.Register<HitData>("RPC_Damage", RPC_Damage);
			m_nview.Register<float, bool>("RPC_Heal", RPC_Heal);
			m_nview.Register<float>("RPC_AddNoise", RPC_AddNoise);
			m_nview.Register<Vector3>("RPC_Stagger", RPC_Stagger);
			m_nview.Register("RPC_ResetCloth", RPC_ResetCloth);
			m_nview.Register<bool>("RPC_SetTamed", RPC_SetTamed);
			m_nview.Register<float>("RPC_FreezeFrame", RPC_FreezeFrame);
			m_nview.Register<Vector3, Quaternion, bool>("RPC_TeleportTo", RPC_TeleportTo);
		}
		if (!IsPlayer())
		{
			if (Game.m_enemySpeedSize != 1f && !InInterior())
			{
				base.transform.localScale *= Game.m_enemySpeedSize;
			}
			if (Game.m_worldLevel > 0)
			{
				base.transform.localScale *= 1f + (float)Game.m_worldLevel * Game.instance.m_worldLevelEnemyMoveSpeedMultiplier;
			}
		}
	}

	protected virtual void OnEnable()
	{
		Instances.Add(this);
	}

	protected virtual void OnDisable()
	{
		Instances.Remove(this);
	}

	private void SetupMaxHealth()
	{
		int level = GetLevel();
		SetMaxHealth(GetMaxHealthBase() * (float)level);
	}

	protected virtual void Start()
	{
	}

	protected virtual void OnDestroy()
	{
		m_seman.OnDestroy();
		s_characters.Remove(this);
	}

	public void SetLevel(int level)
	{
		if (level >= 1)
		{
			m_level = level;
			m_nview.GetZDO().Set(ZDOVars.s_level, level);
			SetupMaxHealth();
			if (m_onLevelSet != null)
			{
				m_onLevelSet(m_level);
			}
		}
	}

	public int GetLevel()
	{
		return m_level;
	}

	public virtual bool IsPlayer()
	{
		return false;
	}

	public Faction GetFaction()
	{
		return m_faction;
	}

	public string GetGroup()
	{
		return m_group;
	}

	public virtual void CustomFixedUpdate(float dt)
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		ZDO zDO = m_nview.GetZDO();
		bool num = zDO.IsOwner();
		bool visible = zDO.HasOwner();
		CalculateLiquidDepth();
		UpdateLayer();
		UpdateContinousEffects();
		UpdateWater(dt);
		UpdateGroundTilt(dt);
		SetVisible(visible);
		UpdateLookTransition(dt);
		if (num)
		{
			UpdateGroundContact(dt);
			UpdateNoise(dt);
			m_seman.Update(zDO, dt);
			UpdateStagger(dt);
			UpdatePushback(dt);
			UpdateMotion(dt);
			UpdateSmoke(dt);
			UpdateLava(dt);
			UpdateAshlandsWater(dt);
			UpdateHeatDamage(dt);
			UnderWorldCheck(dt);
			SyncVelocity();
			if (m_groundForceTimer > 0f)
			{
				m_groundForceTimer -= dt;
			}
			CheckDeath();
		}
		UpdateHeatEffects(dt);
		if (IsPlayer() && Terminal.m_showTests)
		{
			Terminal.m_testList["Player.Zone"] = ZoneSystem.instance.GetZone(base.transform.position).ToString();
		}
	}

	private void UpdateLayer()
	{
		if (m_collider.gameObject.layer == s_characterLayer || m_collider.gameObject.layer == s_characterNetLayer)
		{
			if (m_nview.IsOwner())
			{
				m_collider.gameObject.layer = (IsAttached() ? s_characterNetLayer : s_characterLayer);
			}
			else
			{
				m_collider.gameObject.layer = s_characterNetLayer;
			}
		}
		if (m_disableWhileSleeping)
		{
			if ((bool)m_baseAI && m_baseAI.IsSleeping())
			{
				m_body.isKinematic = true;
			}
			else
			{
				m_body.isKinematic = false;
			}
		}
	}

	private void UnderWorldCheck(float dt)
	{
		if (IsDead())
		{
			return;
		}
		m_underWorldCheckTimer += dt;
		if (m_underWorldCheckTimer > 5f || IsPlayer())
		{
			m_underWorldCheckTimer = 0f;
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			if (base.transform.position.y < groundHeight - 1f)
			{
				Vector3 position = base.transform.position;
				position.y = groundHeight + 0.5f;
				base.transform.position = position;
				m_body.position = position;
				m_body.velocity = Vector3.zero;
			}
		}
	}

	private void UpdateSmoke(float dt)
	{
		if (m_tolerateSmoke)
		{
			return;
		}
		m_smokeCheckTimer += dt;
		if (m_smokeCheckTimer > 2f)
		{
			m_smokeCheckTimer = 0f;
			if (Physics.CheckSphere(GetTopPoint() + Vector3.up * 0.1f, 0.5f, s_smokeRayMask))
			{
				m_seman.AddStatusEffect(s_statusEffectSmoked, resetTime: true);
			}
			else
			{
				m_seman.RemoveStatusEffect(s_statusEffectSmoked, quiet: true);
			}
		}
	}

	private void UpdateLava(float dt)
	{
		m_lavaTimer += dt;
		m_aboveOrInLavaTimer += dt;
		if (!WorldGenerator.IsAshlands(base.transform.position.x, base.transform.position.z))
		{
			return;
		}
		Vector3 p = base.transform.position;
		m_lavaProximity = 0f;
		ZoneSystem.instance.GetGroundData(ref p, out var _, out var biome, out var _, out var hmap);
		if (hmap != null)
		{
			m_lavaProximity = Mathf.Min(1f, hmap.GetLava(p) * (1f / m_maxLavaMaskThreshold));
		}
		if (m_lavaProximity > m_minLavaMaskThreshold)
		{
			m_aboveOrInLavaTimer = 0f;
		}
		m_lavaHeightFactor = base.transform.position.y - p.y;
		m_lavaHeightFactor = (m_lavaAirDamageHeight - m_lavaHeightFactor) / m_lavaAirDamageHeight;
		bool flag = false;
		if (m_lavaProximity > m_minLavaMaskThreshold && Physics.Raycast(base.transform.position + Vector3.up, Vector3.down, out var hitInfo, 50f, s_blockedRayMask) && hitInfo.collider.GetComponent<Heightmap>() == null)
		{
			flag = true;
		}
		if (!flag && IsRiding())
		{
			flag = true;
		}
		float num = 1f - GetEquipmentHeatResistanceModifier();
		if (Terminal.m_showTests && IsPlayer())
		{
			Terminal.m_testList["Lava/Height/Resist"] = m_lavaProximity.ToString("0.00") + " / " + m_lavaHeightFactor.ToString("0.00") + " / " + num.ToString("0.00");
		}
		if (m_lavaProximity > m_minLavaMaskThreshold && m_lavaHeightFactor > 0f && !flag)
		{
			m_lavaHeatLevel += m_lavaProximity * dt * m_heatBuildupBase * m_lavaHeightFactor * num;
			m_lavaTimer = 0f;
		}
		else if (biome == Heightmap.Biome.AshLands && m_dayHeatGainRunning != 0f && IsPlayer() && EnvMan.IsDay() && !IsUnderRoof() && GetEquipmentHeatResistanceModifier() < m_dayHeatEquipmentStop)
		{
			if (m_currentVel.magnitude > 0.1f && IsOnGround())
			{
				m_lavaHeatLevel += dt * m_dayHeatGainRunning * num;
			}
			else if (!InWater())
			{
				m_lavaHeatLevel += dt * m_dayHeatGainStill;
			}
			if (m_lavaHeatLevel > m_heatLevelFirstDamageThreshold)
			{
				m_lavaHeatLevel = m_heatLevelFirstDamageThreshold;
			}
		}
		else if (!InWater())
		{
			m_lavaHeatLevel -= dt * m_heatCooldownBase;
		}
		if (m_tolerateFire)
		{
			m_lavaHeatLevel = 0f;
		}
		else
		{
			m_lavaHeatLevel = Mathf.Clamp(m_lavaHeatLevel, 0f, 1f);
		}
	}

	private void UpdateHeatEffects(float dt)
	{
		bool flag = false;
		if (!(Player.m_localPlayer == this))
		{
			return;
		}
		GameCamera.instance.m_heatDistortImageEffect.m_intensity = (flag ? 0f : m_lavaHeatLevel);
		if (!m_lavaHeatEffects.HasEffects())
		{
			return;
		}
		if (m_lavaHeatLevel > 0f && m_lavaHeatParticles.Count == 0 && !IsDead())
		{
			GameObject[] array = m_lavaHeatEffects.Create(base.transform.position, Quaternion.identity, base.transform);
			foreach (KeyValuePair<ParticleSystem, float> lavaHeatParticle in m_lavaHeatParticles)
			{
				UnityEngine.Object.Destroy(lavaHeatParticle.Key.gameObject);
			}
			foreach (ZSFX item2 in m_lavaHeatAudio)
			{
				UnityEngine.Object.Destroy(item2.gameObject);
			}
			m_lavaHeatParticles.Clear();
			m_lavaHeatAudio.Clear();
			GameObject[] array2 = array;
			foreach (GameObject gameObject in array2)
			{
				ParticleSystem[] componentsInChildren = gameObject.GetComponentsInChildren<ParticleSystem>();
				foreach (ParticleSystem particleSystem in componentsInChildren)
				{
					m_lavaHeatParticles.Add(particleSystem, particleSystem.emissionRate);
				}
				ZSFX[] componentsInChildren2 = gameObject.GetComponentsInChildren<ZSFX>();
				foreach (ZSFX item in componentsInChildren2)
				{
					m_lavaHeatAudio.Add(item);
				}
			}
		}
		foreach (KeyValuePair<ParticleSystem, float> lavaHeatParticle2 in m_lavaHeatParticles)
		{
			if (lavaHeatParticle2.Key != null)
			{
				lavaHeatParticle2.Key.emissionRate = m_lavaHeatLevel * lavaHeatParticle2.Value;
			}
		}
		if (Player.m_localPlayer == this)
		{
			foreach (ZSFX item3 in m_lavaHeatAudio)
			{
				if (item3 != null)
				{
					item3.SetVolumeModifier(IsDead() ? 0f : m_lavaHeatLevel);
				}
			}
		}
		if (!IsDead())
		{
			return;
		}
		foreach (KeyValuePair<ParticleSystem, float> lavaHeatParticle3 in m_lavaHeatParticles)
		{
			ZNetView component = lavaHeatParticle3.Key.gameObject.GetComponent<ZNetView>();
			if ((object)component != null && component.IsValid() && component.IsOwner())
			{
				component.Destroy();
			}
		}
		foreach (ZSFX item4 in m_lavaHeatAudio)
		{
			ZNetView component2 = item4.gameObject.GetComponent<ZNetView>();
			if ((object)component2 != null && component2.IsValid() && component2.IsOwner())
			{
				component2.Destroy();
			}
		}
		m_lavaHeatParticles.Clear();
		m_lavaHeatAudio.Clear();
	}

	private void UpdateHeatDamage(float dt)
	{
		bool flag = false;
		m_lavaDamageTimer += dt;
		if (!(m_lavaDamageTimer > m_lavaDamageTickInterval) || flag)
		{
			return;
		}
		m_lavaDamageTimer = 0f;
		float num = 1f - GetEquipmentHeatResistanceModifier();
		if (m_lavaHeatLevel >= 1f)
		{
			if (!InWater() && m_lavaProximity > m_minLavaMaskThreshold)
			{
				m_seman.AddStatusEffect(s_statusEffectBurning, resetTime: true);
			}
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = m_lavaFullDamage * num;
			hitData.m_point = m_lastGroundPoint;
			hitData.m_dir = m_lastGroundNormal;
			hitData.m_hitType = HitData.HitType.Burning;
			Damage(hitData);
		}
		else if (m_lavaHeatLevel >= m_heatLevelFirstDamageThreshold)
		{
			if (!InWater() && m_lavaProximity > m_minLavaMaskThreshold)
			{
				m_seman.AddStatusEffect(s_statusEffectBurning, resetTime: true);
			}
			HitData hitData2 = new HitData();
			hitData2.m_damage.m_damage = m_lavaFirstDamage * num;
			hitData2.m_point = m_lastGroundPoint;
			hitData2.m_dir = m_lastGroundNormal;
			hitData2.m_hitType = HitData.HitType.Burning;
			Damage(hitData2);
		}
	}

	private bool IsUnderRoof()
	{
		return Physics.RaycastNonAlloc(base.transform.position + Vector3.up * 0.2f, Vector3.up, m_lavaRoofCheck, 20f, LayerMask.GetMask("Default", "static_solid", "piece")) > 0;
	}

	private void UpdateAshlandsWater(float dt)
	{
		if (m_tolerateFire || !InWater())
		{
			return;
		}
		float num = WorldGenerator.GetAshlandsOceanGradient(base.transform.position);
		if (!IsSwimming())
		{
			num *= m_heatWaterTouchMultiplier;
		}
		if (!(num < 0f))
		{
			num = Mathf.Clamp01(num);
			float num2 = 1f - GetEquipmentHeatResistanceModifier();
			m_lavaHeatLevel += num * dt * m_heatBuildupWater * num2;
			if (m_lavaHeatLevel > m_heatLevelFirstDamageThreshold)
			{
				m_lavaHeatLevel = m_heatLevelFirstDamageThreshold;
			}
		}
	}

	private void UpdateContinousEffects()
	{
		SetupContinuousEffect(base.transform, base.transform.position, m_sliding, m_slideEffects, ref m_slideEffects_instances);
		Vector3 position = base.transform.position;
		position.y = GetLiquidLevel() + 0.05f;
		EffectList effects = ((InTar() && m_tarEffects.HasEffects()) ? m_tarEffects : m_waterEffects);
		SetupContinuousEffect(base.transform, position, InLiquid(), effects, ref m_waterEffects_instances);
		SetupContinuousEffect(base.transform, base.transform.position, IsFlying(), m_flyingContinuousEffect, ref m_flyingEffects_instances);
	}

	public static void SetupContinuousEffect(Transform transform, Vector3 point, bool enabledEffect, EffectList effects, ref GameObject[] instances)
	{
		if (!effects.HasEffects())
		{
			return;
		}
		if (enabledEffect)
		{
			if (instances == null)
			{
				instances = effects.Create(point, Quaternion.identity, transform);
				return;
			}
			GameObject[] array = instances;
			foreach (GameObject gameObject in array)
			{
				if ((bool)gameObject)
				{
					gameObject.transform.position = point;
				}
			}
		}
		else
		{
			if (instances == null)
			{
				return;
			}
			GameObject[] array = instances;
			foreach (GameObject gameObject2 in array)
			{
				if ((bool)gameObject2)
				{
					ParticleSystem[] componentsInChildren = gameObject2.GetComponentsInChildren<ParticleSystem>();
					foreach (ParticleSystem obj in componentsInChildren)
					{
						ParticleSystem.EmissionModule emission = obj.emission;
						emission.enabled = false;
						obj.Stop();
					}
					CamShaker componentInChildren = gameObject2.GetComponentInChildren<CamShaker>();
					if ((bool)componentInChildren)
					{
						UnityEngine.Object.Destroy(componentInChildren);
					}
					ZSFX componentInChildren2 = gameObject2.GetComponentInChildren<ZSFX>();
					if ((bool)componentInChildren2)
					{
						componentInChildren2.FadeOut();
					}
					TimedDestruction component = gameObject2.GetComponent<TimedDestruction>();
					if ((bool)component)
					{
						component.Trigger();
					}
					else
					{
						UnityEngine.Object.Destroy(gameObject2);
					}
				}
			}
			instances = null;
		}
	}

	protected virtual void OnSwimming(Vector3 targetVel, float dt)
	{
	}

	protected virtual void OnSneaking(float dt)
	{
	}

	protected virtual void OnJump()
	{
	}

	protected virtual bool TakeInput()
	{
		return true;
	}

	private float GetSlideAngle()
	{
		if (IsPlayer())
		{
			return 38f;
		}
		if (HaveRider())
		{
			return 45f;
		}
		return 90f;
	}

	public bool HaveRider()
	{
		if ((bool)m_baseAI)
		{
			return m_baseAI.HaveRider();
		}
		return false;
	}

	private void ApplySlide(float dt, ref Vector3 currentVel, Vector3 bodyVel, bool running)
	{
		bool flag = CanWallRun();
		Vector3 obj = ((m_groundTilt != 0) ? m_groundTiltNormal : m_lastGroundNormal);
		float num = Mathf.Clamp(Mathf.Acos(Mathf.Clamp01(obj.y)) * 57.29578f, 0f, 90f);
		Vector3 lastGroundNormal = m_lastGroundNormal;
		lastGroundNormal.y = 0f;
		lastGroundNormal.Normalize();
		_ = m_body.velocity;
		Vector3 vector = Vector3.Cross(rhs: Vector3.Cross(m_lastGroundNormal, Vector3.up), lhs: m_lastGroundNormal);
		bool flag2 = currentVel.magnitude > 0.1f;
		if (num > GetSlideAngle())
		{
			if (running && flag && flag2)
			{
				m_slippage = 0f;
				m_wallRunning = true;
			}
			else
			{
				m_slippage = Mathf.MoveTowards(m_slippage, 1f, 1f * dt);
			}
			Vector3 b = vector * 5f;
			currentVel = Vector3.Lerp(currentVel, b, m_slippage);
			m_sliding = m_slippage > 0.5f;
		}
		else
		{
			m_slippage = 0f;
		}
	}

	private void UpdateMotion(float dt)
	{
		UpdateBodyFriction();
		m_sliding = false;
		m_wallRunning = false;
		m_running = false;
		m_walking = false;
		if (IsDead())
		{
			return;
		}
		if (IsDebugFlying())
		{
			UpdateDebugFly(dt);
			return;
		}
		if (InIntro())
		{
			m_maxAirAltitude = base.transform.position.y;
			m_body.velocity = Vector3.zero;
			m_body.angularVelocity = Vector3.zero;
		}
		if (!InLiquidSwimDepth() && !IsOnGround() && !IsAttached())
		{
			float y = base.transform.position.y;
			m_maxAirAltitude = Mathf.Max(m_maxAirAltitude, y);
			m_fallTimer += dt;
			if (IsPlayer() && m_fallTimer > 0.1f)
			{
				m_zanim.SetBool(s_animatorFalling, value: true);
			}
		}
		else
		{
			m_fallTimer = 0f;
			if (IsPlayer())
			{
				m_zanim.SetBool(s_animatorFalling, value: false);
			}
		}
		if (IsSwimming())
		{
			UpdateSwimming(dt);
		}
		else if (m_flying)
		{
			UpdateFlying(dt);
		}
		else
		{
			if ((bool)m_baseAI && !m_baseAI.IsSleeping())
			{
				UpdateWalking(dt);
			}
			if (!m_baseAI)
			{
				UpdateWalking(dt);
			}
		}
		m_lastGroundTouch += Time.fixedDeltaTime;
		m_jumpTimer += Time.fixedDeltaTime;
		if (m_standUp > 0f)
		{
			m_standUp -= Time.fixedDeltaTime;
		}
	}

	public static void SetTakeInputDelay(float delayInSeconds)
	{
		takeInputDelay = delayInSeconds;
	}

	private void UpdateDebugFly(float dt)
	{
		takeInputDelay = Mathf.Max(0f, takeInputDelay - dt);
		float num = (m_run ? ((float)m_debugFlySpeed * 2.5f) : ((float)m_debugFlySpeed));
		Vector3 b = m_moveDir * num;
		if (TakeInput())
		{
			if ((ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump")) && !(takeInputDelay > 0f) && !Hud.IsPieceSelectionVisible())
			{
				b.y = num;
			}
			else if (ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetButtonPressedTimer("JoyCrouch") > 0.33f)
			{
				b.y = 0f - num;
			}
		}
		m_currentVel = Vector3.Lerp(m_currentVel, b, 0.5f);
		m_body.velocity = m_currentVel;
		m_body.useGravity = false;
		m_lastGroundTouch = 0f;
		m_maxAirAltitude = base.transform.position.y;
		m_body.rotation = Quaternion.RotateTowards(base.transform.rotation, m_lookYaw, m_turnSpeed * dt);
		m_body.angularVelocity = Vector3.zero;
		UpdateEyeRotation();
	}

	private void UpdateSwimming(float dt)
	{
		bool flag = IsOnGround();
		if (Mathf.Max(0f, m_maxAirAltitude - base.transform.position.y) > 0.5f && m_onLand != null)
		{
			m_onLand(new Vector3(base.transform.position.x, GetLiquidLevel(), base.transform.position.z));
		}
		m_maxAirAltitude = base.transform.position.y;
		float speed = m_swimSpeed * GetAttackSpeedFactorMovement();
		if (InMinorActionSlowdown())
		{
			speed = 0f;
		}
		m_seman.ApplyStatusEffectSpeedMods(ref speed, m_moveDir);
		Vector3 vector = m_moveDir * speed;
		if (IsPlayer())
		{
			m_currentVel = Vector3.Lerp(m_currentVel, vector, m_swimAcceleration);
		}
		else
		{
			float magnitude = vector.magnitude;
			float magnitude2 = m_currentVel.magnitude;
			if (magnitude > magnitude2)
			{
				magnitude = Mathf.MoveTowards(magnitude2, magnitude, m_swimAcceleration);
				vector = vector.normalized * magnitude;
			}
			m_currentVel = Vector3.Lerp(m_currentVel, vector, 0.5f);
		}
		if (m_currentVel.magnitude > 0.1f)
		{
			AddNoise(15f);
		}
		AddPushbackForce(ref m_currentVel);
		Vector3 force = m_currentVel - m_body.velocity;
		force.y = 0f;
		if (force.magnitude > 20f)
		{
			force = force.normalized * 20f;
		}
		m_body.AddForce(force, ForceMode.VelocityChange);
		float num = GetLiquidLevel() - m_swimDepth;
		if (base.transform.position.y < num)
		{
			float t = Mathf.Clamp01((num - base.transform.position.y) / 2f);
			float target = Mathf.Lerp(0f, 10f, t);
			Vector3 velocity = m_body.velocity;
			velocity.y = Mathf.MoveTowards(velocity.y, target, 50f * dt);
			m_body.velocity = velocity;
		}
		else
		{
			float t2 = Mathf.Clamp01((0f - (num - base.transform.position.y)) / 1f);
			float num2 = Mathf.Lerp(0f, 10f, t2);
			Vector3 velocity2 = m_body.velocity;
			velocity2.y = Mathf.MoveTowards(velocity2.y, 0f - num2, 30f * dt);
			m_body.velocity = velocity2;
		}
		float target2 = 0f;
		if (m_moveDir.magnitude > 0.1f || AlwaysRotateCamera())
		{
			float speed2 = m_swimTurnSpeed;
			m_seman.ApplyStatusEffectSpeedMods(ref speed2, m_currentVel);
			target2 = UpdateRotation(speed2, dt, smooth: false);
		}
		m_body.angularVelocity = Vector3.zero;
		UpdateEyeRotation();
		m_body.useGravity = true;
		float value = ((IsPlayer() || HaveRider()) ? Vector3.Dot(m_currentVel, base.transform.forward) : Vector3.Dot(m_body.velocity, base.transform.forward));
		float value2 = Vector3.Dot(m_currentVel, base.transform.right);
		m_currentTurnVel = Mathf.SmoothDamp(m_currentTurnVel, target2, ref m_currentTurnVelChange, 0.5f, 99f);
		m_zanim.SetFloat(s_forwardSpeed, value);
		m_zanim.SetFloat(s_sidewaySpeed, value2);
		m_zanim.SetFloat(s_turnSpeed, m_currentTurnVel);
		m_zanim.SetBool(s_inWater, !flag);
		m_zanim.SetBool(s_onGround, value: false);
		m_zanim.SetBool(s_encumbered, value: false);
		m_zanim.SetBool(s_flying, value: false);
		if (!flag)
		{
			OnSwimming(vector, dt);
		}
	}

	private void UpdateFlying(float dt)
	{
		float num = (m_run ? m_flyFastSpeed : m_flySlowSpeed) * GetAttackSpeedFactorMovement();
		Vector3 b = (CanMove() ? (m_moveDir * num) : Vector3.zero);
		m_currentVel = Vector3.Lerp(m_currentVel, b, m_acceleration);
		m_maxAirAltitude = base.transform.position.y;
		ApplyRootMotion(ref m_currentVel);
		AddPushbackForce(ref m_currentVel);
		Vector3 force = m_currentVel - m_body.velocity;
		if (force.magnitude > 20f)
		{
			force = force.normalized * 20f;
		}
		m_body.AddForce(force, ForceMode.VelocityChange);
		float target = 0f;
		if ((m_moveDir.magnitude > 0.1f || AlwaysRotateCamera()) && !InDodge() && CanMove())
		{
			float speed = m_flyTurnSpeed;
			m_seman.ApplyStatusEffectSpeedMods(ref speed, m_currentVel);
			target = UpdateRotation(speed, dt, smooth: true);
		}
		m_body.angularVelocity = Vector3.zero;
		UpdateEyeRotation();
		m_body.useGravity = false;
		float num2 = Vector3.Dot(m_currentVel, base.transform.forward);
		float value = Vector3.Dot(m_currentVel, base.transform.right);
		float num3 = Vector3.Dot(m_body.velocity, base.transform.forward);
		m_currentTurnVel = Mathf.SmoothDamp(m_currentTurnVel, target, ref m_currentTurnVelChange, 0.5f, 99f);
		m_zanim.SetFloat(s_forwardSpeed, IsPlayer() ? num2 : num3);
		m_zanim.SetFloat(s_sidewaySpeed, value);
		m_zanim.SetFloat(s_turnSpeed, m_currentTurnVel);
		m_zanim.SetBool(s_inWater, value: false);
		m_zanim.SetBool(s_onGround, value: false);
		m_zanim.SetBool(s_encumbered, value: false);
		m_zanim.SetBool(s_flying, value: true);
	}

	private void UpdateWalking(float dt)
	{
		Vector3 moveDir = m_moveDir;
		bool flag = IsCrouching();
		m_running = CheckRun(moveDir, dt);
		float speed = m_speed * GetJogSpeedFactor();
		if ((m_walk || InMinorActionSlowdown()) && !flag)
		{
			speed = m_walkSpeed;
			m_walking = moveDir.magnitude > 0.1f;
		}
		else if (m_running)
		{
			speed = m_runSpeed * GetRunSpeedFactor();
			if (IsPlayer() && moveDir.magnitude > 0f)
			{
				moveDir.Normalize();
			}
		}
		else if (flag || IsEncumbered())
		{
			speed = m_crouchSpeed;
		}
		if (!IsPlayer())
		{
			if (!InInterior())
			{
				speed *= Game.m_enemySpeedSize;
			}
			speed *= 1f + (float)Game.m_worldLevel * Game.instance.m_worldLevelEnemyMoveSpeedMultiplier;
		}
		ApplyLiquidResistance(ref speed);
		speed *= GetAttackSpeedFactorMovement();
		m_seman.ApplyStatusEffectSpeedMods(ref speed, moveDir);
		if (m_lavaProximity > 0.33f && m_lavaHeightFactor > m_lavaSlowHeight)
		{
			float num = (m_lavaProximity - 0.33f) * 1.4925374f;
			speed *= 1f - num * m_lavaSlowMax;
		}
		Vector3 vector = (CanMove() ? (moveDir * speed) : Vector3.zero);
		if (vector.magnitude > 0f && IsOnGround())
		{
			vector = Vector3.ProjectOnPlane(vector, m_lastGroundNormal).normalized * vector.magnitude;
		}
		float magnitude = vector.magnitude;
		float magnitude2 = m_currentVel.magnitude;
		if (magnitude > magnitude2)
		{
			magnitude = Mathf.MoveTowards(magnitude2, magnitude, m_acceleration);
			vector = vector.normalized * magnitude;
		}
		else
		{
			magnitude = Mathf.MoveTowards(magnitude2, magnitude, m_acceleration * 2f);
			vector = ((vector.magnitude > 0f) ? (vector.normalized * magnitude) : (m_currentVel.normalized * magnitude));
		}
		m_currentVel = Vector3.Lerp(m_currentVel, vector, 0.5f);
		Vector3 velocity = m_body.velocity;
		Vector3 currentVel = m_currentVel;
		currentVel.y = velocity.y;
		if (IsOnGround() && m_lastAttachBody == null)
		{
			ApplySlide(dt, ref currentVel, velocity, m_running);
			currentVel.y = Mathf.Min(currentVel.y, 3f);
		}
		ApplyRootMotion(ref currentVel);
		AddPushbackForce(ref currentVel);
		ApplyGroundForce(ref currentVel, vector);
		Vector3 vector2 = currentVel - velocity;
		if (!IsOnGround())
		{
			if (vector.magnitude > 0.1f)
			{
				vector2 *= m_airControl;
			}
			else
			{
				vector2 = Vector3.zero;
			}
		}
		if (IsAttached())
		{
			vector2 = Vector3.zero;
		}
		if (vector2.magnitude > 20f)
		{
			vector2 = vector2.normalized * 20f;
		}
		if (vector2.magnitude > 0.01f)
		{
			m_body.AddForce(vector2, ForceMode.VelocityChange);
		}
		Vector3 vel = m_body.velocity;
		m_seman.ModifyWalkVelocity(ref vel);
		m_body.velocity = vel;
		if ((bool)m_lastGroundBody && m_lastGroundBody.gameObject.layer != base.gameObject.layer && m_lastGroundBody.mass > m_body.mass)
		{
			float num2 = m_body.mass / m_lastGroundBody.mass;
			m_lastGroundBody.AddForceAtPosition(-vector2 * num2, base.transform.position, ForceMode.VelocityChange);
		}
		float target = 0f;
		if (((moveDir.magnitude > 0.1f || AlwaysRotateCamera() || m_standUp > 0f) && !InDodge() && CanMove()) || m_groundContact)
		{
			float speed2 = (m_run ? m_runTurnSpeed : m_turnSpeed);
			m_seman.ApplyStatusEffectSpeedMods(ref speed2, m_currentVel);
			target = UpdateRotation(speed2, dt, smooth: false);
		}
		if (IsSneaking())
		{
			OnSneaking(dt);
		}
		UpdateEyeRotation();
		m_body.useGravity = true;
		float num3 = Vector3.Dot(m_currentVel, Vector3.ProjectOnPlane(base.transform.forward, m_lastGroundNormal).normalized);
		float num4 = Vector3.Dot(m_body.velocity, m_visual.transform.forward);
		if (IsRiding())
		{
			num3 = num4;
		}
		else if (!IsPlayer() && !HaveRider())
		{
			num3 = Mathf.Min(num3, num4);
		}
		float value = Vector3.Dot(m_currentVel, Vector3.ProjectOnPlane(base.transform.right, m_lastGroundNormal).normalized);
		m_currentTurnVel = Mathf.SmoothDamp(m_currentTurnVel, target, ref m_currentTurnVelChange, 0.5f, 99f);
		m_zanim.SetFloat(s_forwardSpeed, num3);
		m_zanim.SetFloat(s_sidewaySpeed, value);
		m_zanim.SetFloat(s_turnSpeed, m_currentTurnVel);
		m_zanim.SetBool(s_inWater, value: false);
		m_zanim.SetBool(s_onGround, IsOnGround());
		m_zanim.SetBool(s_encumbered, IsEncumbered());
		m_zanim.SetBool(s_flying, value: false);
		if (m_currentVel.magnitude > 0.1f)
		{
			if (m_running)
			{
				AddNoise(30f);
			}
			else if (!flag)
			{
				AddNoise(15f);
			}
		}
	}

	public bool IsSneaking()
	{
		if (IsCrouching() && m_currentVel.magnitude > 0.1f)
		{
			return IsOnGround();
		}
		return false;
	}

	private float GetSlopeAngle()
	{
		if (!IsOnGround())
		{
			return 0f;
		}
		float num = Vector3.SignedAngle(base.transform.forward, m_lastGroundNormal, base.transform.right);
		return 0f - (90f - (0f - num));
	}

	protected void AddPushbackForce(ref Vector3 velocity)
	{
		if (m_pushForce != Vector3.zero)
		{
			Vector3 normalized = m_pushForce.normalized;
			float num = Vector3.Dot(normalized, velocity);
			if (num < 20f)
			{
				velocity += normalized * (20f - num);
			}
			if (IsSwimming() || m_flying)
			{
				velocity *= 0.5f;
			}
		}
	}

	private void ApplyPushback(HitData hit)
	{
		ApplyPushback(hit.m_dir, hit.m_pushForce);
	}

	public void ApplyPushback(Vector3 dir, float pushForce)
	{
		if (pushForce != 0f && dir != Vector3.zero)
		{
			float num = pushForce * Mathf.Clamp01(1f + GetEquipmentMovementModifier()) / m_body.mass * 2.5f;
			dir.y = 0f;
			dir.Normalize();
			Vector3 pushForce2 = dir * num;
			if (m_pushForce.magnitude < pushForce2.magnitude)
			{
				m_pushForce = pushForce2;
			}
		}
	}

	private void UpdatePushback(float dt)
	{
		m_pushForce = Vector3.MoveTowards(m_pushForce, Vector3.zero, 100f * dt);
	}

	public void TimeoutGroundForce(float time)
	{
		m_groundForceTimer = time;
	}

	private void ApplyGroundForce(ref Vector3 vel, Vector3 targetVel)
	{
		Vector3 vector = Vector3.zero;
		if (IsOnGround() && (bool)m_lastGroundBody && m_groundForceTimer <= 0f)
		{
			vector = m_lastGroundBody.GetPointVelocity(base.transform.position);
			vector.y = 0f;
		}
		Ship standingOnShip = GetStandingOnShip();
		if (standingOnShip != null)
		{
			if (targetVel.magnitude > 0.01f)
			{
				m_lastAttachBody = null;
			}
			else if (m_lastAttachBody != m_lastGroundBody)
			{
				m_lastAttachBody = m_lastGroundBody;
				m_lastAttachPos = m_lastAttachBody.transform.InverseTransformPoint(m_body.position);
			}
			if ((bool)m_lastAttachBody)
			{
				Vector3 vector2 = m_lastAttachBody.transform.TransformPoint(m_lastAttachPos);
				Vector3 vector3 = vector2 - m_body.position;
				if (vector3.magnitude < 4f)
				{
					Vector3 position = vector2;
					position.y = m_body.position.y;
					if (standingOnShip.IsOwner())
					{
						vector3.y = 0f;
						vector += vector3 * 10f;
					}
					else
					{
						m_body.position = position;
					}
				}
				else
				{
					m_lastAttachBody = null;
				}
			}
		}
		else
		{
			m_lastAttachBody = null;
		}
		vel += vector;
	}

	private float UpdateRotation(float turnSpeed, float dt, bool smooth)
	{
		Quaternion quaternion = ((AlwaysRotateCamera() || m_moveDir == Vector3.zero) ? m_lookYaw : Quaternion.LookRotation(m_moveDir));
		float yawDeltaAngle = Utils.GetYawDeltaAngle(base.transform.rotation, quaternion);
		float num = 1f;
		if (!IsPlayer())
		{
			num = Mathf.Clamp01(Mathf.Abs(yawDeltaAngle) / 90f);
			num = Mathf.Pow(num, 0.5f);
			float f = Mathf.Clamp01(Mathf.Abs(yawDeltaAngle) / 90f);
			f = Mathf.Pow(f, 0.5f);
			if (smooth)
			{
				currentRotSpeedFactor = Mathf.MoveTowards(currentRotSpeedFactor, f, dt);
				num = currentRotSpeedFactor;
			}
			else
			{
				num = f;
			}
		}
		float num2 = turnSpeed * GetAttackSpeedFactorRotation() * num;
		Quaternion rotation = Quaternion.RotateTowards(base.transform.rotation, quaternion, num2 * dt);
		if (Mathf.Abs(yawDeltaAngle) > 0.001f)
		{
			base.transform.rotation = rotation;
		}
		return num2 * Mathf.Sign(yawDeltaAngle) * ((float)Math.PI / 180f);
	}

	private void UpdateGroundTilt(float dt)
	{
		if (m_visual == null || ((bool)m_baseAI && m_baseAI.IsSleeping()))
		{
			return;
		}
		if (m_nview.IsOwner())
		{
			if (m_groundTilt != 0)
			{
				if (!IsFlying() && IsOnGround() && !IsAttached())
				{
					Vector3 vector = m_lastGroundNormal;
					if (m_groundTilt == GroundTiltType.PitchRaycast || m_groundTilt == GroundTiltType.FullRaycast)
					{
						Vector3 p = base.transform.position + base.transform.forward * m_collider.radius;
						Vector3 p2 = base.transform.position - base.transform.forward * m_collider.radius;
						GetGroundHeight(p, out var _, out var normal);
						GetGroundHeight(p2, out var _, out var normal2);
						vector = (vector + normal + normal2).normalized;
					}
					Vector3 target = base.transform.InverseTransformVector(vector);
					target = Vector3.RotateTowards(Vector3.up, target, 0.87266463f, 1f);
					m_groundTiltNormal = Vector3.Lerp(m_groundTiltNormal, target, 0.05f);
					Vector3 vector3;
					if (m_groundTilt == GroundTiltType.Pitch || m_groundTilt == GroundTiltType.PitchRaycast)
					{
						Vector3 vector2 = Vector3.Project(m_groundTiltNormal, Vector3.right);
						vector3 = m_groundTiltNormal - vector2;
					}
					else
					{
						vector3 = m_groundTiltNormal;
					}
					Quaternion to = Quaternion.LookRotation(Vector3.Cross(vector3, Vector3.left), vector3);
					m_visual.transform.localRotation = Quaternion.RotateTowards(m_visual.transform.localRotation, to, dt * m_groundTiltSpeed);
				}
				else if (IsFlying() && !IsOnGround() && m_groundTilt == GroundTiltType.Flying && m_currentVel.sqrMagnitude > 0f)
				{
					m_groundTiltNormal = Vector3.Cross(base.transform.InverseTransformVector(m_currentVel.normalized), Vector3.right);
					Quaternion b = Quaternion.LookRotation(Vector3.Cross(m_groundTiltNormal, Vector3.left), m_groundTiltNormal);
					b = Quaternion.Lerp(Quaternion.identity, b, m_currentVel.magnitude * 0.33f);
					m_visual.transform.localRotation = Quaternion.RotateTowards(m_visual.transform.localRotation, b, dt * m_groundTiltSpeed);
				}
				else
				{
					m_groundTiltNormal = Vector3.up;
					if (IsSwimming())
					{
						m_visual.transform.localRotation = Quaternion.RotateTowards(m_visual.transform.localRotation, Quaternion.identity, dt * m_groundTiltSpeed);
					}
					else
					{
						m_visual.transform.localRotation = Quaternion.RotateTowards(m_visual.transform.localRotation, Quaternion.identity, dt * m_groundTiltSpeed * 2f);
					}
				}
				m_nview.GetZDO().Set(ZDOVars.s_tiltrot, m_visual.transform.localRotation);
			}
			else if (CanWallRun())
			{
				if (m_wallRunning)
				{
					Vector3 vector4 = Vector3.Lerp(Vector3.up, m_lastGroundNormal, 0.65f);
					Vector3 forward = Vector3.ProjectOnPlane(base.transform.forward, vector4);
					forward.Normalize();
					Quaternion to2 = Quaternion.LookRotation(forward, vector4);
					m_visual.transform.rotation = Quaternion.RotateTowards(m_visual.transform.rotation, to2, 30f * dt);
				}
				else
				{
					m_visual.transform.localRotation = Quaternion.RotateTowards(m_visual.transform.localRotation, Quaternion.identity, dt * m_groundTiltSpeed * 2f);
				}
				m_nview.GetZDO().Set(ZDOVars.s_tiltrot, m_visual.transform.localRotation);
			}
		}
		else if (m_groundTilt != 0 || CanWallRun())
		{
			Quaternion quaternion = m_nview.GetZDO().GetQuaternion(ZDOVars.s_tiltrot, Quaternion.identity);
			m_visual.transform.localRotation = Quaternion.RotateTowards(m_visual.transform.localRotation, quaternion, dt * m_groundTiltSpeed);
		}
		m_animator.SetFloat(s_tilt, Vector3.Dot(m_visual.transform.forward, Vector3.up));
	}

	private bool GetGroundHeight(Vector3 p, out float height, out Vector3 normal)
	{
		p.y += 10f;
		if (Physics.Raycast(p, Vector3.down, out var hitInfo, 20f, s_groundRayMask))
		{
			height = hitInfo.point.y;
			normal = hitInfo.normal;
			return true;
		}
		height = p.y;
		normal = Vector3.zero;
		return false;
	}

	public bool IsWallRunning()
	{
		return m_wallRunning;
	}

	private bool IsOnSnow()
	{
		return false;
	}

	public void Heal(float hp, bool showText = true)
	{
		if (!(hp <= 0f))
		{
			if (m_nview.IsOwner())
			{
				RPC_Heal(0L, hp, showText);
				return;
			}
			m_nview.InvokeRPC("RPC_Heal", hp, showText);
		}
	}

	private void RPC_Heal(long sender, float hp, bool showText)
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		float health = GetHealth();
		if (health <= 0f || IsDead())
		{
			return;
		}
		float num = Mathf.Min(health + hp, GetMaxHealth());
		if (num > health)
		{
			SetHealth(num);
			if (showText)
			{
				Vector3 topPoint = GetTopPoint();
				DamageText.instance.ShowText(DamageText.TextType.Heal, topPoint, hp, IsPlayer());
			}
		}
	}

	public Vector3 GetTopPoint()
	{
		return base.transform.TransformPoint(m_collider.center) + m_visual.transform.up * (m_collider.height * 0.5f);
	}

	public float GetRadius()
	{
		if (IsPlayer())
		{
			return m_collider.radius;
		}
		return m_collider.radius * base.transform.localScale.magnitude;
	}

	public float GetHeight()
	{
		return Mathf.Max(m_collider.height, m_collider.radius * 2f);
	}

	public Vector3 GetHeadPoint()
	{
		return m_head.position;
	}

	public Vector3 GetEyePoint()
	{
		return m_eye.position;
	}

	public Vector3 GetCenterPoint()
	{
		return m_collider.bounds.center;
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Character;
	}

	private short FindWeakSpotIndex(Collider c)
	{
		if (c == null || m_weakSpots == null || m_weakSpots.Length == 0)
		{
			return -1;
		}
		for (short num = 0; num < m_weakSpots.Length; num++)
		{
			if (m_weakSpots[num].m_collider == c)
			{
				return num;
			}
		}
		return -1;
	}

	private WeakSpot GetWeakSpot(short index)
	{
		if (index < 0 || index >= m_weakSpots.Length)
		{
			return null;
		}
		return m_weakSpots[index];
	}

	public void Damage(HitData hit)
	{
		if (m_nview.IsValid())
		{
			hit.m_weakSpot = FindWeakSpotIndex(hit.m_hitCollider);
			m_nview.InvokeRPC("RPC_Damage", hit);
		}
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (IsDebugFlying())
		{
			return;
		}
		if (hit.GetAttacker() == Player.m_localPlayer)
		{
			Game.instance.IncrementPlayerStat(IsPlayer() ? PlayerStatType.PlayerHits : PlayerStatType.EnemyHits);
			m_localPlayerHasHit = true;
		}
		if (!m_nview.IsOwner() || GetHealth() <= 0f || IsDead() || IsTeleporting() || InCutscene() || (hit.m_dodgeable && IsDodgeInvincible()))
		{
			return;
		}
		Character attacker = hit.GetAttacker();
		if ((hit.HaveAttacker() && attacker == null) || (IsPlayer() && !IsPVPEnabled() && attacker != null && attacker.IsPlayer() && !hit.m_ignorePVP))
		{
			return;
		}
		if (attacker != null && !attacker.IsPlayer())
		{
			float difficultyDamageScalePlayer = Game.instance.GetDifficultyDamageScalePlayer(base.transform.position);
			hit.ApplyModifier(difficultyDamageScalePlayer);
			hit.ApplyModifier(Game.m_enemyDamageRate);
		}
		m_seman.OnDamaged(hit, attacker);
		if (m_baseAI != null && m_baseAI.IsAggravatable() && !m_baseAI.IsAggravated() && (bool)attacker && attacker.IsPlayer() && hit.GetTotalDamage() > 0f)
		{
			BaseAI.AggravateAllInArea(base.transform.position, 20f, BaseAI.AggravatedReason.Damage);
		}
		if (m_baseAI != null && !m_baseAI.IsAlerted() && hit.m_backstabBonus > 1f && Time.time - m_backstabTime > 300f && (!ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs) || !m_baseAI.CanSeeTarget(attacker)))
		{
			m_backstabTime = Time.time;
			hit.ApplyModifier(hit.m_backstabBonus);
			m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, base.transform);
		}
		if (IsStaggering() && !IsPlayer())
		{
			hit.ApplyModifier(2f);
			m_critHitEffects.Create(hit.m_point, Quaternion.identity, base.transform);
		}
		if (hit.m_blockable && IsBlocking())
		{
			BlockAttack(hit, attacker);
		}
		ApplyPushback(hit);
		if (hit.m_statusEffectHash != 0)
		{
			StatusEffect statusEffect = m_seman.GetStatusEffect(hit.m_statusEffectHash);
			if (statusEffect == null)
			{
				statusEffect = m_seman.AddStatusEffect(hit.m_statusEffectHash, resetTime: false, hit.m_itemLevel, hit.m_skillLevel);
			}
			else
			{
				statusEffect.ResetTime();
				statusEffect.SetLevel(hit.m_itemLevel, hit.m_skillLevel);
			}
			if (statusEffect != null && attacker != null)
			{
				statusEffect.SetAttacker(attacker);
			}
		}
		WeakSpot weakSpot = GetWeakSpot(hit.m_weakSpot);
		if (weakSpot != null)
		{
			ZLog.Log("HIT Weakspot:" + weakSpot.gameObject.name);
		}
		HitData.DamageModifiers damageModifiers = GetDamageModifiers(weakSpot);
		hit.ApplyResistance(damageModifiers, out var significantModifier);
		if (IsPlayer())
		{
			float bodyArmor = GetBodyArmor();
			hit.ApplyArmor(bodyArmor);
			DamageArmorDurability(hit);
		}
		else if (Game.m_worldLevel > 0)
		{
			hit.ApplyArmor(Game.m_worldLevel * Game.instance.m_worldLevelEnemyBaseAC);
		}
		float poison = hit.m_damage.m_poison;
		float fire = hit.m_damage.m_fire;
		float spirit = hit.m_damage.m_spirit;
		hit.m_damage.m_poison = 0f;
		hit.m_damage.m_fire = 0f;
		hit.m_damage.m_spirit = 0f;
		ApplyDamage(hit, showDamageText: true, triggerEffects: true, significantModifier);
		AddFireDamage(fire);
		AddSpiritDamage(spirit);
		AddPoisonDamage(poison);
		AddFrostDamage(hit.m_damage.m_frost);
		AddLightningDamage(hit.m_damage.m_lightning);
	}

	protected HitData.DamageModifier GetDamageModifier(HitData.DamageType damageType)
	{
		return GetDamageModifiers().GetModifier(damageType);
	}

	public HitData.DamageModifiers GetDamageModifiers(WeakSpot weakspot = null)
	{
		HitData.DamageModifiers mods = (weakspot ? weakspot.m_damageModifiers.Clone() : m_damageModifiers.Clone());
		ApplyArmorDamageMods(ref mods);
		m_seman.ApplyDamageMods(ref mods);
		return mods;
	}

	public void ApplyDamage(HitData hit, bool showDamageText, bool triggerEffects, HitData.DamageModifier mod = HitData.DamageModifier.Normal)
	{
		if (IsDebugFlying() || IsDead() || IsTeleporting() || InCutscene())
		{
			return;
		}
		float totalDamage = hit.GetTotalDamage();
		if (!IsPlayer())
		{
			float difficultyDamageScaleEnemy = Game.instance.GetDifficultyDamageScaleEnemy(base.transform.position);
			hit.ApplyModifier(difficultyDamageScaleEnemy);
			hit.ApplyModifier(Game.m_playerDamageRate);
		}
		else
		{
			hit.ApplyModifier(Game.m_localDamgeTakenRate);
			Game.instance.IncrementPlayerStat((hit.GetAttacker() is Player) ? PlayerStatType.HitsTakenPlayers : PlayerStatType.HitsTakenEnemies);
		}
		float totalDamage2 = hit.GetTotalDamage();
		if (totalDamage2 <= 0.1f)
		{
			return;
		}
		if (showDamageText && (totalDamage2 > 0f || !IsPlayer()))
		{
			DamageText.instance.ShowText(mod, hit.m_point, totalDamage, IsPlayer() || IsTamed());
		}
		m_lastHit = hit;
		float health = GetHealth();
		health -= totalDamage2;
		if (health <= 0f && (InGodMode() || InGhostMode()))
		{
			health = 1f;
		}
		if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("damage"))
		{
			Terminal.Log($"Damage: Character {m_name} took {totalDamage2} damage from {hit}");
		}
		SetHealth(health);
		float totalStaggerDamage = hit.m_damage.GetTotalStaggerDamage();
		AddStaggerDamage(totalStaggerDamage * hit.m_staggerMultiplier, hit.m_dir);
		if (triggerEffects && totalDamage2 > GetMaxHealth() / 10f)
		{
			DoDamageCameraShake(hit);
			if (hit.m_damage.GetTotalPhysicalDamage() > 0f)
			{
				m_hitEffects.Create(hit.m_point, Quaternion.identity, base.transform);
			}
		}
		OnDamaged(hit);
		if (m_onDamaged != null)
		{
			m_onDamaged(totalDamage2, hit.GetAttacker());
		}
		if (s_dpsDebugEnabled)
		{
			AddDPS(totalDamage2, this);
		}
	}

	protected virtual void DoDamageCameraShake(HitData hit)
	{
	}

	protected virtual void DamageArmorDurability(HitData hit)
	{
	}

	private void AddFireDamage(float damage)
	{
		if (!(damage <= 0f))
		{
			SE_Burning sE_Burning = m_seman.GetStatusEffect(s_statusEffectBurning) as SE_Burning;
			if (sE_Burning == null)
			{
				sE_Burning = m_seman.AddStatusEffect(s_statusEffectBurning) as SE_Burning;
			}
			if (!sE_Burning.AddFireDamage(damage))
			{
				m_seman.RemoveStatusEffect(sE_Burning);
			}
		}
	}

	private void AddSpiritDamage(float damage)
	{
		if (!(damage <= 0f))
		{
			SE_Burning sE_Burning = m_seman.GetStatusEffect(s_statusEffectSpirit) as SE_Burning;
			if (sE_Burning == null)
			{
				sE_Burning = m_seman.AddStatusEffect(s_statusEffectSpirit) as SE_Burning;
			}
			if (!sE_Burning.AddSpiritDamage(damage))
			{
				m_seman.RemoveStatusEffect(sE_Burning);
			}
		}
	}

	private void AddPoisonDamage(float damage)
	{
		if (!(damage <= 0f))
		{
			SE_Poison sE_Poison = m_seman.GetStatusEffect(s_statusEffectPoison) as SE_Poison;
			if (sE_Poison == null)
			{
				sE_Poison = m_seman.AddStatusEffect(s_statusEffectPoison) as SE_Poison;
			}
			sE_Poison.AddDamage(damage);
		}
	}

	private void AddFrostDamage(float damage)
	{
		if (!(damage <= 0f))
		{
			SE_Frost sE_Frost = m_seman.GetStatusEffect(s_statusEffectFrost) as SE_Frost;
			if (sE_Frost == null)
			{
				sE_Frost = m_seman.AddStatusEffect(s_statusEffectFrost) as SE_Frost;
			}
			sE_Frost.AddDamage(damage);
		}
	}

	private void AddLightningDamage(float damage)
	{
		if (!(damage <= 0f))
		{
			m_seman.AddStatusEffect(s_statusEffectLightning, resetTime: true);
		}
	}

	private static void AddDPS(float damage, Character me)
	{
		if (me == Player.m_localPlayer)
		{
			CalculateDPS("To-you ", s_playerDamage, damage);
		}
		else
		{
			CalculateDPS("To-others ", s_enemyDamage, damage);
		}
	}

	private static void CalculateDPS(string name, List<KeyValuePair<float, float>> damages, float damage)
	{
		float time = Time.time;
		if (damages.Count > 0 && Time.time - damages[damages.Count - 1].Key > 5f)
		{
			damages.Clear();
		}
		damages.Add(new KeyValuePair<float, float>(time, damage));
		float num = Time.time - damages[0].Key;
		if (num < 0.01f)
		{
			return;
		}
		float num2 = 0f;
		foreach (KeyValuePair<float, float> damage2 in damages)
		{
			num2 += damage2.Value;
		}
		float num3 = num2 / num;
		string text = "DPS " + name + " ( " + damages.Count + " attacks, " + num.ToString("0.0") + "s ): " + num3.ToString("0.0");
		ZLog.Log(text);
		MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, text);
	}

	public float GetStaggerPercentage()
	{
		return Mathf.Clamp01(m_staggerDamage / GetStaggerTreshold());
	}

	private float GetStaggerTreshold()
	{
		return GetMaxHealth() * m_staggerDamageFactor;
	}

	protected bool AddStaggerDamage(float damage, Vector3 forceDirection)
	{
		if (m_staggerDamageFactor <= 0f)
		{
			return false;
		}
		m_staggerDamage += damage;
		float staggerTreshold = GetStaggerTreshold();
		if (m_staggerDamage >= staggerTreshold)
		{
			m_staggerDamage = staggerTreshold;
			Stagger(forceDirection);
			if (IsPlayer())
			{
				Hud.instance.StaggerBarFlash();
			}
			return true;
		}
		return false;
	}

	private void UpdateStagger(float dt)
	{
		if (!(m_staggerDamageFactor <= 0f) || IsPlayer())
		{
			float num = GetMaxHealth() * m_staggerDamageFactor;
			m_staggerDamage -= num / 5f * dt;
			if (m_staggerDamage < 0f)
			{
				m_staggerDamage = 0f;
			}
		}
	}

	public void Stagger(Vector3 forceDirection)
	{
		if (m_nview.IsOwner())
		{
			RPC_Stagger(0L, forceDirection);
			return;
		}
		m_nview.InvokeRPC("RPC_Stagger", forceDirection);
	}

	private void RPC_Stagger(long sender, Vector3 forceDirection)
	{
		if (!IsStaggering())
		{
			if (forceDirection.magnitude > 0.01f)
			{
				forceDirection.y = 0f;
				base.transform.rotation = Quaternion.LookRotation(-forceDirection);
			}
			m_zanim.SetSpeed(1f);
			m_zanim.SetTrigger("stagger");
		}
	}

	protected virtual void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
	{
	}

	public virtual float GetBodyArmor()
	{
		return 0f;
	}

	protected virtual bool BlockAttack(HitData hit, Character attacker)
	{
		return false;
	}

	protected virtual void OnDamaged(HitData hit)
	{
	}

	private void OnCollisionStay(Collision collision)
	{
		if (!m_nview.IsOwner() || m_jumpTimer < 0.1f)
		{
			return;
		}
		ContactPoint[] contacts = collision.contacts;
		for (int i = 0; i < contacts.Length; i++)
		{
			ContactPoint contactPoint = contacts[i];
			float num = contactPoint.point.y - base.transform.position.y;
			if (!(contactPoint.normal.y > 0.1f) || !(num < m_collider.radius))
			{
				continue;
			}
			if (contactPoint.normal.y > m_groundContactNormal.y || !m_groundContact)
			{
				if (m_standUp == -100f)
				{
					m_standUp = 2f;
				}
				m_groundContact = true;
				m_groundContactNormal = contactPoint.normal;
				m_groundContactPoint = contactPoint.point;
				m_lowestContactCollider = collision.collider;
			}
			else
			{
				Vector3 groundContactNormal = Vector3.Normalize(m_groundContactNormal + contactPoint.normal);
				if (groundContactNormal.y > m_groundContactNormal.y)
				{
					m_groundContactNormal = groundContactNormal;
					m_groundContactPoint = (m_groundContactPoint + contactPoint.point) * 0.5f;
				}
			}
		}
	}

	public void StandUpOnNextGround()
	{
		m_standUp = -100f;
	}

	private void UpdateGroundContact(float dt)
	{
		if (!m_groundContact)
		{
			return;
		}
		m_lastGroundCollider = m_lowestContactCollider;
		m_lastGroundNormal = m_groundContactNormal;
		m_lastGroundPoint = m_groundContactPoint;
		m_lastGroundBody = (m_lastGroundCollider ? m_lastGroundCollider.attachedRigidbody : null);
		if (!IsPlayer() && m_lastGroundBody != null && m_lastGroundBody.gameObject.layer == base.gameObject.layer)
		{
			m_lastGroundCollider = null;
			m_lastGroundBody = null;
		}
		float num = Mathf.Max(0f, m_maxAirAltitude - base.transform.position.y);
		if (num > 0.8f && m_onLand != null)
		{
			Vector3 lastGroundPoint = m_lastGroundPoint;
			if (InLiquid())
			{
				lastGroundPoint.y = GetLiquidLevel();
			}
			m_onLand(m_lastGroundPoint);
		}
		if (IsPlayer() && num > 4f)
		{
			float damage = Mathf.Clamp01((num - 4f) / 16f) * 100f;
			m_seman.ModifyFallDamage(damage, ref damage);
			if (damage > 0f)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = damage;
				hitData.m_point = m_lastGroundPoint;
				hitData.m_dir = m_lastGroundNormal;
				hitData.m_hitType = HitData.HitType.Fall;
				Damage(hitData);
			}
		}
		ResetGroundContact();
		m_lastGroundTouch = 0f;
		m_maxAirAltitude = base.transform.position.y;
		if (IsPlayer() && Terminal.m_showTests)
		{
			Dictionary<string, string> testList = Terminal.m_testList;
			Collider lastGroundCollider = Player.m_localPlayer.GetLastGroundCollider();
			testList["Player.CollisionLayer"] = (((object)lastGroundCollider != null && (bool)lastGroundCollider) ? LayerMask.LayerToName(lastGroundCollider.gameObject.layer) : "none");
		}
	}

	private void ResetGroundContact()
	{
		m_lowestContactCollider = null;
		m_groundContact = false;
		m_groundContactNormal = Vector3.zero;
		m_groundContactPoint = Vector3.zero;
	}

	public Ship GetStandingOnShip()
	{
		if (InNumShipVolumes == 0)
		{
			return null;
		}
		if (!IsOnGround())
		{
			return null;
		}
		if ((bool)m_lastGroundBody)
		{
			return m_lastGroundBody.GetComponent<Ship>();
		}
		return null;
	}

	public bool IsOnGround()
	{
		if (!(m_lastGroundTouch < 0.2f))
		{
			return m_body.IsSleeping();
		}
		return true;
	}

	private void CheckDeath()
	{
		if (!IsDead() && GetHealth() <= 0f)
		{
			OnDeath();
		}
	}

	protected virtual void OnRagdollCreated(Ragdoll ragdoll)
	{
	}

	protected virtual void OnDeath()
	{
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		bool flag = m_lastHit != null && m_lastHit.GetAttacker() == Player.m_localPlayer;
		if (flag && IsPlayer() && this is Player player)
		{
			playerProfile.IncrementStat(PlayerStatType.PlayerKills);
			playerProfile.m_enemyStats.IncrementOrSet(player.GetPlayerName());
		}
		if (!IsPlayer())
		{
			if (m_localPlayerHasHit)
			{
				playerProfile.IncrementStat(IsBoss() ? PlayerStatType.BossKills : PlayerStatType.EnemyKills);
			}
			if (flag)
			{
				playerProfile.IncrementStat(IsBoss() ? PlayerStatType.BossLastHits : PlayerStatType.EnemyKillsLastHits);
			}
			playerProfile.m_enemyStats.IncrementOrSet(m_name);
		}
		if (!string.IsNullOrEmpty(m_defeatSetGlobalKey))
		{
			Player.m_addUniqueKeyQueue.Add(m_defeatSetGlobalKey);
		}
		if ((bool)m_nview && !m_nview.IsOwner())
		{
			return;
		}
		GameObject[] array = m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform);
		for (int i = 0; i < array.Length; i++)
		{
			Ragdoll component = array[i].GetComponent<Ragdoll>();
			if ((bool)component)
			{
				CharacterDrop component2 = GetComponent<CharacterDrop>();
				LevelEffects componentInChildren = GetComponentInChildren<LevelEffects>();
				Vector3 velocity = m_body.velocity;
				if (m_pushForce.magnitude * 0.5f > velocity.magnitude)
				{
					velocity = m_pushForce * 0.5f;
				}
				float hue = 0f;
				float saturation = 0f;
				float value = 0f;
				if ((bool)componentInChildren)
				{
					componentInChildren.GetColorChanges(out hue, out saturation, out value);
				}
				component.Setup(velocity, hue, saturation, value, component2);
				OnRagdollCreated(component);
				if ((bool)component2 && component.m_dropItems)
				{
					component2.SetDropsEnabled(enabled: false);
				}
			}
		}
		if (!string.IsNullOrEmpty(m_defeatSetGlobalKey))
		{
			ZoneSystem.instance.SetGlobalKey(m_defeatSetGlobalKey);
		}
		if (m_onDeath != null)
		{
			m_onDeath();
		}
		if (IsBoss() && m_nview.GetZDO().GetBool("bosscount"))
		{
			ZoneSystem.instance.GetGlobalKey(GlobalKeys.activeBosses, out float value2);
			ZoneSystem.instance.SetGlobalKey(GlobalKeys.activeBosses, Mathf.Max(0f, value2 - 1f));
		}
		ZNetScene.instance.Destroy(base.gameObject);
		Gogan.LogEvent("Game", "Killed", m_name, 0L);
	}

	public float GetHealth()
	{
		return m_nview.GetZDO()?.GetFloat(ZDOVars.s_health, GetMaxHealth()) ?? GetMaxHealth();
	}

	public void SetHealth(float health)
	{
		if (health >= GetMaxHealth())
		{
			m_localPlayerHasHit = false;
		}
		ZDO zDO = m_nview.GetZDO();
		if (zDO != null && m_nview.IsOwner())
		{
			if (health < 0f)
			{
				health = 0f;
			}
			zDO.Set(ZDOVars.s_health, health);
		}
	}

	public void UseHealth(float hp)
	{
		if (!(hp <= 0f))
		{
			float health = GetHealth();
			health -= hp;
			health = Mathf.Clamp(health, 0f, GetMaxHealth());
			SetHealth(health);
			if (IsPlayer())
			{
				Hud.instance.DamageFlash();
			}
		}
	}

	public float GetHealthPercentage()
	{
		return GetHealth() / GetMaxHealth();
	}

	public virtual bool IsDead()
	{
		return false;
	}

	public void SetMaxHealth(float health)
	{
		if (m_nview.GetZDO() != null)
		{
			m_nview.GetZDO().Set(ZDOVars.s_maxHealth, health);
		}
		if (GetHealth() > health)
		{
			SetHealth(health);
		}
	}

	public float GetMaxHealth()
	{
		if (m_nview.GetZDO() != null)
		{
			return m_nview.GetZDO().GetFloat(ZDOVars.s_maxHealth, m_health);
		}
		return GetMaxHealthBase();
	}

	public float GetMaxHealthBase()
	{
		float num = m_health;
		if (!IsPlayer() && Game.m_worldLevel > 0)
		{
			num *= (float)Game.m_worldLevel * Game.instance.m_worldLevelEnemyHPMultiplier;
		}
		return num;
	}

	public virtual float GetMaxStamina()
	{
		return 0f;
	}

	public virtual float GetMaxEitr()
	{
		return 0f;
	}

	public virtual float GetEitrPercentage()
	{
		return 1f;
	}

	public virtual float GetStaminaPercentage()
	{
		return 1f;
	}

	public bool IsBoss()
	{
		return m_boss;
	}

	public bool TryUseEitr(float eitrUse = 0f)
	{
		if (eitrUse == 0f)
		{
			return true;
		}
		if (GetMaxEitr() == 0f)
		{
			Message(MessageHud.MessageType.Center, "$hud_eitrrequired");
			return false;
		}
		if (!HaveEitr(eitrUse + 0.1f))
		{
			if (IsPlayer())
			{
				Hud.instance.EitrBarEmptyFlash();
			}
			return false;
		}
		return true;
	}

	public void SetLookDir(Vector3 dir, float transitionTime = 0f)
	{
		if (transitionTime > 0f)
		{
			m_lookTransitionTime = (m_lookTransitionTimeTotal = transitionTime);
			m_lookTransitionStart = GetLookDir();
			m_lookTransitionTarget = Vector3.Normalize(dir);
			return;
		}
		if (dir.magnitude <= Mathf.Epsilon)
		{
			dir = base.transform.forward;
		}
		else
		{
			dir.Normalize();
		}
		m_lookDir = dir;
		dir.y = 0f;
		m_lookYaw = Quaternion.LookRotation(dir);
	}

	private void UpdateLookTransition(float dt)
	{
		if (m_lookTransitionTime > 0f)
		{
			SetLookDir(Vector3.Lerp(m_lookTransitionTarget, m_lookTransitionStart, Mathf.SmoothStep(0f, 1f, m_lookTransitionTime / m_lookTransitionTimeTotal)));
			m_lookTransitionTime -= dt;
		}
	}

	public Vector3 GetLookDir()
	{
		return m_eye.forward;
	}

	public virtual void OnAttackTrigger()
	{
	}

	public virtual void OnStopMoving()
	{
	}

	public virtual void OnWeaponTrailStart()
	{
	}

	public void SetMoveDir(Vector3 dir)
	{
		m_moveDir = dir;
	}

	public void SetRun(bool run)
	{
		m_run = run;
	}

	public void SetWalk(bool walk)
	{
		m_walk = walk;
	}

	public bool GetWalk()
	{
		return m_walk;
	}

	protected virtual void UpdateEyeRotation()
	{
		m_eye.rotation = Quaternion.LookRotation(m_lookDir);
	}

	public void OnAutoJump(Vector3 dir, float upVel, float forwardVel)
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && IsOnGround() && !IsDead() && !InAttack() && !InDodge() && !IsKnockedBack() && !(Time.time - m_lastAutoJumpTime < 0.5f))
		{
			m_lastAutoJumpTime = Time.time;
			if (!(Vector3.Dot(m_moveDir, dir) < 0.5f))
			{
				Vector3 zero = Vector3.zero;
				zero.y = upVel;
				zero += dir * forwardVel;
				m_body.velocity = zero;
				m_lastGroundTouch = 1f;
				m_jumpTimer = 0f;
				m_jumpEffects.Create(base.transform.position, base.transform.rotation, base.transform);
				SetCrouch(crouch: false);
				UpdateBodyFriction();
			}
		}
	}

	public void Jump(bool force = false)
	{
		if (!IsOnGround() || IsDead() || (!force && InAttack()) || IsEncumbered() || InDodge() || IsKnockedBack() || IsStaggering())
		{
			return;
		}
		bool flag = false;
		if (!HaveStamina(m_jumpStaminaUsage))
		{
			if (IsPlayer())
			{
				Hud.instance.StaminaBarEmptyFlash();
			}
			flag = true;
		}
		float speed = m_speed;
		m_seman.ApplyStatusEffectSpeedMods(ref speed, m_currentVel);
		if (speed <= 0f)
		{
			flag = true;
		}
		float num = 0f;
		Skills skills = GetSkills();
		if (skills != null)
		{
			num = skills.GetSkillFactor(Skills.SkillType.Jump);
			if (!flag)
			{
				RaiseSkill(Skills.SkillType.Jump);
			}
		}
		Vector3 jump = m_body.velocity;
		Mathf.Acos(Mathf.Clamp01(m_lastGroundNormal.y));
		Vector3 normalized = (m_lastGroundNormal + Vector3.up).normalized;
		float num2 = 1f + num * 0.4f;
		float num3 = m_jumpForce * num2;
		float num4 = Vector3.Dot(normalized, jump);
		if (num4 < num3)
		{
			jump += normalized * (num3 - num4);
		}
		if (IsPlayer())
		{
			jump += m_moveDir * m_jumpForceForward * num2;
		}
		else
		{
			jump += base.transform.forward * m_jumpForceForward * num2;
		}
		if (flag)
		{
			jump *= m_jumpForceTiredFactor;
		}
		m_seman.ApplyStatusEffectJumpMods(ref jump);
		if (!(jump.x <= 0f) || !(jump.y <= 0f) || !(jump.z <= 0f))
		{
			ForceJump(jump);
		}
	}

	public void ForceJump(Vector3 vel, bool effects = true)
	{
		m_body.WakeUp();
		m_body.velocity = vel;
		ResetGroundContact();
		m_lastGroundTouch = 1f;
		m_jumpTimer = 0f;
		AddNoise(30f);
		if (effects)
		{
			m_zanim.SetTrigger("jump");
			m_jumpEffects.Create(base.transform.position, base.transform.rotation, base.transform);
			ResetCloth();
			OnJump();
			SetCrouch(crouch: false);
			UpdateBodyFriction();
		}
	}

	public void SetTempParent(Transform t)
	{
		oldParent = base.transform.parent;
		base.transform.parent = t;
	}

	public void ReleaseTempParent()
	{
		base.transform.parent = oldParent;
	}

	private void UpdateBodyFriction()
	{
		m_collider.material.frictionCombine = PhysicMaterialCombine.Multiply;
		if (IsDead())
		{
			m_collider.material.staticFriction = 1f;
			m_collider.material.dynamicFriction = 1f;
			m_collider.material.frictionCombine = PhysicMaterialCombine.Maximum;
		}
		else if (IsSwimming())
		{
			m_collider.material.staticFriction = 0.2f;
			m_collider.material.dynamicFriction = 0.2f;
		}
		else if (!IsOnGround())
		{
			m_collider.material.staticFriction = 0f;
			m_collider.material.dynamicFriction = 0f;
		}
		else if (IsFlying())
		{
			m_collider.material.staticFriction = 0f;
			m_collider.material.dynamicFriction = 0f;
		}
		else if (m_moveDir.magnitude < 0.1f)
		{
			m_collider.material.staticFriction = 0.8f * (1f - m_slippage);
			m_collider.material.dynamicFriction = 0.8f * (1f - m_slippage);
			m_collider.material.frictionCombine = PhysicMaterialCombine.Maximum;
		}
		else
		{
			m_collider.material.staticFriction = 0.4f * (1f - m_slippage);
			m_collider.material.dynamicFriction = 0.4f * (1f - m_slippage);
		}
	}

	public virtual bool StartAttack(Character target, bool charge)
	{
		return false;
	}

	public virtual float GetTimeSinceLastAttack()
	{
		return 99999f;
	}

	public virtual void OnNearFire(Vector3 point)
	{
	}

	public ZDOID GetZDOID()
	{
		if (!m_nview.IsValid())
		{
			return ZDOID.None;
		}
		return m_nview.GetZDO().m_uid;
	}

	public bool IsOwner()
	{
		if (m_nview.IsValid())
		{
			return m_nview.IsOwner();
		}
		return false;
	}

	public long GetOwner()
	{
		if (!m_nview.IsValid())
		{
			return 0L;
		}
		return m_nview.GetZDO().GetOwner();
	}

	public virtual bool UseMeleeCamera()
	{
		return false;
	}

	protected virtual bool AlwaysRotateCamera()
	{
		return true;
	}

	public void SetLiquidLevel(float level, LiquidType type, Component liquidObj)
	{
		switch (type)
		{
		case LiquidType.Water:
			m_waterLevel = level;
			break;
		case LiquidType.Tar:
			m_tarLevel = level;
			break;
		}
		m_liquidLevel = Mathf.Max(m_waterLevel, m_tarLevel);
	}

	public virtual bool IsPVPEnabled()
	{
		return false;
	}

	public virtual bool InIntro()
	{
		return false;
	}

	public virtual bool InCutscene()
	{
		return false;
	}

	public virtual bool IsCrouching()
	{
		return false;
	}

	public virtual bool InBed()
	{
		return false;
	}

	public virtual bool IsAttached()
	{
		return false;
	}

	public virtual bool IsAttachedToShip()
	{
		return false;
	}

	public virtual bool IsRiding()
	{
		return false;
	}

	protected virtual void SetCrouch(bool crouch)
	{
	}

	public virtual void AttachStart(Transform attachPoint, GameObject colliderRoot, bool hideWeapons, bool isBed, bool onShip, string attachAnimation, Vector3 detachOffset, Transform cameraPos = null)
	{
	}

	public virtual void AttachStop()
	{
	}

	private void UpdateWater(float dt)
	{
		m_swimTimer += dt;
		float depth = InLiquidDepth();
		if (m_canSwim && InLiquidSwimDepth(depth))
		{
			m_swimTimer = 0f;
		}
		if (m_nview.IsOwner() && InLiquidWetDepth(depth))
		{
			if (m_waterLevel > m_tarLevel)
			{
				m_seman.AddStatusEffect(s_statusEffectWet, resetTime: true);
			}
			else if (m_tarLevel > m_waterLevel && !m_tolerateTar)
			{
				m_seman.AddStatusEffect(s_statusEffectTared, resetTime: true);
			}
		}
	}

	private void ApplyLiquidResistance(ref float speed)
	{
		float num = InLiquidDepth();
		if (!(num <= 0f) && !m_seman.HaveStatusEffect(s_statusEffectTared))
		{
			float num2 = ((m_tarLevel > m_waterLevel) ? 0.1f : 0.05f);
			float num3 = m_collider.height / 3f;
			float num4 = Mathf.Clamp01(num / num3);
			speed -= speed * speed * num4 * num2;
		}
	}

	public bool IsSwimming()
	{
		return m_swimTimer < 0.5f;
	}

	public bool InLava()
	{
		return m_lavaTimer < 0.5f;
	}

	public bool AboveOrInLava()
	{
		return m_aboveOrInLavaTimer < 0.5f;
	}

	private bool InLiquidSwimDepth()
	{
		return InLiquidDepth() > Mathf.Max(0f, m_swimDepth - 0.4f);
	}

	private bool InLiquidSwimDepth(float depth)
	{
		return depth > Mathf.Max(0f, m_swimDepth - 0.4f);
	}

	private bool InLiquidKneeDepth()
	{
		return InLiquidDepth() > 0.4f;
	}

	private bool InLiquidKneeDepth(float depth)
	{
		return depth > 0.4f;
	}

	private bool InLiquidWetDepth(float depth)
	{
		if (!InLiquidSwimDepth(depth))
		{
			if (IsSitting())
			{
				return InLiquidKneeDepth(depth);
			}
			return false;
		}
		return true;
	}

	private float InLiquidDepth()
	{
		return m_cashedInLiquidDepth;
	}

	private void CalculateLiquidDepth()
	{
		if (IsTeleporting() || GetStandingOnShip() != null || IsAttachedToShip())
		{
			m_cashedInLiquidDepth = 0f;
		}
		else
		{
			m_cashedInLiquidDepth = Mathf.Max(0f, GetLiquidLevel() - base.transform.position.y);
		}
	}

	protected void InvalidateCachedLiquidDepth()
	{
		m_cashedInLiquidDepth = 0f;
	}

	public float GetLiquidLevel()
	{
		return m_liquidLevel;
	}

	public bool InLiquid()
	{
		return InLiquidDepth() > 0f;
	}

	private bool InTar()
	{
		if (m_tarLevel > m_waterLevel)
		{
			return InLiquid();
		}
		return false;
	}

	public bool InWater()
	{
		if (m_waterLevel > m_tarLevel)
		{
			return InLiquid();
		}
		return false;
	}

	protected virtual bool CheckRun(Vector3 moveDir, float dt)
	{
		if (!m_run)
		{
			return false;
		}
		if (moveDir.magnitude < 0.1f)
		{
			return false;
		}
		if (IsCrouching() || IsEncumbered())
		{
			return false;
		}
		if (InDodge())
		{
			return false;
		}
		return true;
	}

	public bool IsRunning()
	{
		return m_running;
	}

	public bool IsWalking()
	{
		return m_walking;
	}

	public virtual bool InPlaceMode()
	{
		return false;
	}

	public virtual void AddEitr(float v)
	{
	}

	public virtual void UseEitr(float eitr)
	{
	}

	public virtual bool HaveEitr(float amount = 0f)
	{
		return true;
	}

	public virtual bool HaveStamina(float amount = 0f)
	{
		return true;
	}

	public bool HaveHealth(float amount = 0f)
	{
		return GetHealth() >= amount;
	}

	public virtual void AddStamina(float v)
	{
	}

	public virtual void UseStamina(float stamina, bool isHomeUsage = false)
	{
	}

	protected int GetNextOrCurrentAnimHash()
	{
		if (m_cachedAnimHashFrame == MonoUpdaters.UpdateCount)
		{
			return m_cachedNextOrCurrentAnimHash;
		}
		UpdateCachedAnimHashes();
		return m_cachedNextOrCurrentAnimHash;
	}

	protected int GetCurrentAnimHash()
	{
		if (m_cachedAnimHashFrame == MonoUpdaters.UpdateCount)
		{
			return m_cachedCurrentAnimHash;
		}
		UpdateCachedAnimHashes();
		return m_cachedCurrentAnimHash;
	}

	protected int GetNextAnimHash()
	{
		if (m_cachedAnimHashFrame == MonoUpdaters.UpdateCount)
		{
			return m_cachedNextAnimHash;
		}
		UpdateCachedAnimHashes();
		return m_cachedNextAnimHash;
	}

	private void UpdateCachedAnimHashes()
	{
		m_cachedAnimHashFrame = MonoUpdaters.UpdateCount;
		m_cachedCurrentAnimHash = m_animator.GetCurrentAnimatorStateInfo(0).tagHash;
		m_cachedNextAnimHash = 0;
		m_cachedNextOrCurrentAnimHash = m_cachedCurrentAnimHash;
		if (m_animator.IsInTransition(0))
		{
			m_cachedNextAnimHash = m_animator.GetNextAnimatorStateInfo(0).tagHash;
			m_cachedNextOrCurrentAnimHash = m_cachedNextAnimHash;
		}
	}

	public bool IsStaggering()
	{
		if (GetNextAnimHash() != s_animatorTagStagger)
		{
			return GetCurrentAnimHash() == s_animatorTagStagger;
		}
		return true;
	}

	public virtual bool CanMove()
	{
		if (IsStaggering())
		{
			return false;
		}
		int nextOrCurrentAnimHash = GetNextOrCurrentAnimHash();
		if (nextOrCurrentAnimHash != s_animatorTagFreeze)
		{
			return nextOrCurrentAnimHash != s_animatorTagSitting;
		}
		return false;
	}

	public virtual bool IsEncumbered()
	{
		return false;
	}

	public virtual bool IsTeleporting()
	{
		return false;
	}

	private bool CanWallRun()
	{
		return IsPlayer();
	}

	public void ShowPickupMessage(ItemDrop.ItemData item, int amount)
	{
		Message(MessageHud.MessageType.TopLeft, "$msg_added " + item.m_shared.m_name, amount, item.GetIcon());
	}

	public void ShowRemovedMessage(ItemDrop.ItemData item, int amount)
	{
		Message(MessageHud.MessageType.TopLeft, "$msg_removed " + item.m_shared.m_name, amount, item.GetIcon());
	}

	public virtual void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
	{
	}

	public CapsuleCollider GetCollider()
	{
		return m_collider;
	}

	public virtual float GetStealthFactor()
	{
		return 1f;
	}

	private void UpdateNoise(float dt)
	{
		m_noiseRange = Mathf.Max(0f, m_noiseRange - dt * 4f);
		m_syncNoiseTimer += dt;
		if (m_syncNoiseTimer > 0.5f)
		{
			m_syncNoiseTimer = 0f;
			m_nview.GetZDO().Set(ZDOVars.s_noise, m_noiseRange);
		}
	}

	public void AddNoise(float range)
	{
		if (m_nview.IsValid())
		{
			if (m_nview.IsOwner())
			{
				RPC_AddNoise(0L, range);
				return;
			}
			m_nview.InvokeRPC("RPC_AddNoise", range);
		}
	}

	private void RPC_AddNoise(long sender, float range)
	{
		if (m_nview.IsOwner() && range > m_noiseRange)
		{
			m_noiseRange = range;
			m_seman.ModifyNoise(m_noiseRange, ref m_noiseRange);
		}
	}

	public float GetNoiseRange()
	{
		if (!m_nview.IsValid())
		{
			return 0f;
		}
		if (m_nview.IsOwner())
		{
			return m_noiseRange;
		}
		return m_nview.GetZDO().GetFloat(ZDOVars.s_noise);
	}

	public virtual bool InGodMode()
	{
		return false;
	}

	public virtual bool InGhostMode()
	{
		return false;
	}

	public virtual bool IsDebugFlying()
	{
		return false;
	}

	public virtual string GetHoverText()
	{
		Tameable component = GetComponent<Tameable>();
		if ((bool)component)
		{
			return component.GetHoverText();
		}
		return "";
	}

	public virtual string GetHoverName()
	{
		Tameable component = GetComponent<Tameable>();
		if ((bool)component)
		{
			return component.GetHoverName();
		}
		return Localization.instance.Localize(m_name);
	}

	public virtual bool IsDrawingBow()
	{
		return false;
	}

	public virtual bool InAttack()
	{
		return false;
	}

	protected virtual void StopEmote()
	{
	}

	public virtual bool InMinorAction()
	{
		return false;
	}

	public virtual bool InMinorActionSlowdown()
	{
		return false;
	}

	public virtual bool InDodge()
	{
		return false;
	}

	public virtual bool IsDodgeInvincible()
	{
		return false;
	}

	public virtual bool InEmote()
	{
		return false;
	}

	public virtual bool IsBlocking()
	{
		return false;
	}

	public bool IsFlying()
	{
		return m_flying;
	}

	public bool IsKnockedBack()
	{
		return m_pushForce != Vector3.zero;
	}

	private void OnDrawGizmosSelected()
	{
		if (m_nview != null && m_nview.GetZDO() != null)
		{
			float @float = m_nview.GetZDO().GetFloat(ZDOVars.s_noise);
			Gizmos.DrawWireSphere(base.transform.position, @float);
		}
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * m_swimDepth, new Vector3(1f, 0.05f, 1f));
		if (IsOnGround())
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(m_lastGroundPoint, m_lastGroundPoint + m_lastGroundNormal);
		}
	}

	public virtual bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		return false;
	}

	protected void RPC_TeleportTo(long sender, Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (m_nview.IsOwner())
		{
			TeleportTo(pos, rot, distantTeleport);
		}
	}

	private void SyncVelocity()
	{
		m_nview.GetZDO().Set(ZDOVars.s_bodyVelocity, m_body.velocity);
	}

	public Vector3 GetVelocity()
	{
		if (!m_nview.IsValid())
		{
			return Vector3.zero;
		}
		if (m_nview.IsOwner())
		{
			return m_body.velocity;
		}
		return m_nview.GetZDO().GetVec3(ZDOVars.s_bodyVelocity, Vector3.zero);
	}

	public void AddRootMotion(Vector3 vel)
	{
		if (InDodge() || InAttack() || InEmote())
		{
			m_rootMotion += vel;
		}
	}

	private void ApplyRootMotion(ref Vector3 vel)
	{
		Vector3 vector = m_rootMotion * 55f;
		if (vector.magnitude > vel.magnitude)
		{
			vel = vector;
		}
		m_rootMotion = Vector3.zero;
	}

	public static void GetCharactersInRange(Vector3 point, float radius, List<Character> characters)
	{
		float num = radius * radius;
		foreach (Character s_character in s_characters)
		{
			if (Utils.DistanceSqr(s_character.transform.position, point) < num)
			{
				characters.Add(s_character);
			}
		}
	}

	public static List<Character> GetAllCharacters()
	{
		return s_characters;
	}

	public static bool IsCharacterInRange(Vector3 point, float range)
	{
		foreach (Character s_character in s_characters)
		{
			if (Vector3.Distance(s_character.transform.position, point) < range)
			{
				return true;
			}
		}
		return false;
	}

	public virtual void OnTargeted(bool sensed, bool alerted)
	{
	}

	public GameObject GetVisual()
	{
		return m_visual;
	}

	protected void UpdateLodgroup()
	{
		if (!(m_lodGroup == null))
		{
			Renderer[] componentsInChildren = m_visual.GetComponentsInChildren<Renderer>();
			LOD[] lODs = m_lodGroup.GetLODs();
			lODs[0].renderers = componentsInChildren;
			m_lodGroup.SetLODs(lODs);
		}
	}

	public virtual bool IsSitting()
	{
		return false;
	}

	public virtual float GetEquipmentMovementModifier()
	{
		return 0f;
	}

	public virtual float GetEquipmentHomeItemModifier()
	{
		return 0f;
	}

	public virtual float GetEquipmentHeatResistanceModifier()
	{
		return 0f;
	}

	public virtual float GetEquipmentJumpStaminaModifier()
	{
		return 0f;
	}

	public virtual float GetEquipmentAttackStaminaModifier()
	{
		return 0f;
	}

	public virtual float GetEquipmentBlockStaminaModifier()
	{
		return 0f;
	}

	public virtual float GetEquipmentDodgeStaminaModifier()
	{
		return 0f;
	}

	public virtual float GetEquipmentSwimStaminaModifier()
	{
		return 0f;
	}

	public virtual float GetEquipmentSneakStaminaModifier()
	{
		return 0f;
	}

	public virtual float GetEquipmentRunStaminaModifier()
	{
		return 0f;
	}

	protected virtual float GetJogSpeedFactor()
	{
		return 1f;
	}

	protected virtual float GetRunSpeedFactor()
	{
		if (HaveRider())
		{
			float riderSkill = m_baseAI.GetRiderSkill();
			return 1f + riderSkill * 0.25f;
		}
		return 1f;
	}

	protected virtual float GetAttackSpeedFactorMovement()
	{
		return 1f;
	}

	protected virtual float GetAttackSpeedFactorRotation()
	{
		return 1f;
	}

	public virtual void RaiseSkill(Skills.SkillType skill, float value = 1f)
	{
		if (!IsTamed())
		{
			return;
		}
		if (!m_tameable)
		{
			m_tameable = GetComponent<Tameable>();
			m_tameableMonsterAI = GetComponent<MonsterAI>();
		}
		if (!m_tameable || !m_tameableMonsterAI)
		{
			ZLog.LogWarning(m_name + " is tamed but missing tameable or monster AI script!");
		}
		else
		{
			if (m_tameable.m_levelUpOwnerSkill == Skills.SkillType.None)
			{
				return;
			}
			GameObject followTarget = m_tameableMonsterAI.GetFollowTarget();
			if ((object)followTarget == null || !followTarget)
			{
				return;
			}
			Character component = followTarget.GetComponent<Character>();
			if ((object)component != null)
			{
				Skills skills = component.GetSkills();
				if ((object)skills != null)
				{
					skills.RaiseSkill(m_tameable.m_levelUpOwnerSkill, value * m_tameable.m_levelUpFactor);
					Terminal.Log($"{base.name} leveling up from '{skill}' to master {component.name} skill '{m_tameable.m_levelUpOwnerSkill}' at factor {value * m_tameable.m_levelUpFactor}");
				}
			}
		}
	}

	public virtual Skills GetSkills()
	{
		return null;
	}

	public float GetSkillLevel(Skills.SkillType skillType)
	{
		return GetSkills()?.GetSkillLevel(skillType) ?? 0f;
	}

	public virtual float GetSkillFactor(Skills.SkillType skill)
	{
		return 0f;
	}

	public virtual float GetRandomSkillFactor(Skills.SkillType skill)
	{
		return Mathf.Pow(UnityEngine.Random.Range(0.75f, 1f), 0.5f) * m_nview.GetZDO().GetFloat(ZDOVars.s_randomSkillFactor, 1f);
	}

	public bool IsMonsterFaction(float time)
	{
		if (IsTamed(time))
		{
			return false;
		}
		if (m_faction != Faction.ForestMonsters && m_faction != Faction.Undead && m_faction != Faction.Demon && m_faction != Faction.PlainsMonsters && m_faction != Faction.MountainMonsters && m_faction != Faction.SeaMonsters)
		{
			return m_faction == Faction.MistlandsMonsters;
		}
		return true;
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	public Collider GetLastGroundCollider()
	{
		return m_lastGroundCollider;
	}

	public Vector3 GetLastGroundNormal()
	{
		return m_groundContactNormal;
	}

	public void ResetCloth()
	{
		m_nview.InvokeRPC(ZNetView.Everybody, "RPC_ResetCloth");
	}

	private void RPC_ResetCloth(long sender)
	{
		Cloth[] componentsInChildren = GetComponentsInChildren<Cloth>();
		foreach (Cloth cloth in componentsInChildren)
		{
			if (cloth.enabled)
			{
				cloth.enabled = false;
				cloth.enabled = true;
			}
		}
	}

	public virtual bool GetRelativePosition(out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		relativeVel = Vector3.zero;
		if (IsOnGround() && (bool)m_lastGroundBody)
		{
			ZNetView component = m_lastGroundBody.GetComponent<ZNetView>();
			if ((bool)component && component.IsValid())
			{
				parent = component.GetZDO().m_uid;
				attachJoint = "";
				relativePos = component.transform.InverseTransformPoint(base.transform.position);
				relativeRot = Quaternion.Inverse(component.transform.rotation) * base.transform.rotation;
				relativeVel = component.transform.InverseTransformVector(m_body.velocity - m_lastGroundBody.velocity);
				return true;
			}
		}
		parent = ZDOID.None;
		attachJoint = "";
		relativePos = Vector3.zero;
		relativeRot = Quaternion.identity;
		return false;
	}

	public Quaternion GetLookYaw()
	{
		return m_lookYaw;
	}

	public Vector3 GetMoveDir()
	{
		return m_moveDir;
	}

	public BaseAI GetBaseAI()
	{
		return m_baseAI;
	}

	public float GetMass()
	{
		return m_body.mass;
	}

	protected void SetVisible(bool visible)
	{
		if (!(m_lodGroup == null) && m_lodVisible != visible)
		{
			m_lodVisible = visible;
			if (m_lodVisible)
			{
				m_lodGroup.localReferencePoint = m_originalLocalRef;
			}
			else
			{
				m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
			}
		}
	}

	public void SetTamed(bool tamed)
	{
		if (m_nview.IsValid() && m_tamed != tamed)
		{
			m_nview.InvokeRPC("RPC_SetTamed", tamed);
		}
	}

	private void RPC_SetTamed(long sender, bool tamed)
	{
		if (m_nview.IsOwner() && m_tamed != tamed)
		{
			m_tamed = tamed;
			m_nview.GetZDO().Set(ZDOVars.s_tamed, m_tamed);
		}
	}

	private bool IsTamed(float time)
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (!m_nview.GetZDO().IsOwner() && time - m_lastTamedCheck > 1f)
		{
			m_lastTamedCheck = time;
			m_tamed = m_nview.GetZDO().GetBool(ZDOVars.s_tamed, m_tamed);
		}
		return m_tamed;
	}

	public bool IsTamed()
	{
		return IsTamed(Time.time);
	}

	public ZSyncAnimation GetZAnim()
	{
		return m_zanim;
	}

	public SEMan GetSEMan()
	{
		return m_seman;
	}

	public bool InInterior()
	{
		return InInterior(base.transform);
	}

	public static bool InInterior(Transform me)
	{
		return InInterior(me.position);
	}

	public static bool InInterior(Vector3 position)
	{
		return position.y > 3000f;
	}

	public static void SetDPSDebug(bool enabled)
	{
		s_dpsDebugEnabled = enabled;
	}

	public static bool IsDPSDebugEnabled()
	{
		return s_dpsDebugEnabled;
	}

	public void TakeOff()
	{
		m_flying = true;
		m_jumpEffects.Create(base.transform.position, Quaternion.identity);
		m_animator.SetTrigger("fly_takeoff");
	}

	public void Land()
	{
		m_flying = false;
		m_animator.SetTrigger("fly_land");
	}

	public void FreezeFrame(float duration)
	{
		m_nview.InvokeRPC(ZNetView.Everybody, "RPC_FreezeFrame", duration);
	}

	private void RPC_FreezeFrame(long sender, float duration)
	{
		m_animEvent.FreezeFrame(duration);
	}

	public void SetExtraMass(float amount)
	{
		m_body.mass = m_originalMass + amount;
	}

	public int Increment(LiquidType type)
	{
		return ++m_liquids[(int)type];
	}

	public int Decrement(LiquidType type)
	{
		return --m_liquids[(int)type];
	}
}
