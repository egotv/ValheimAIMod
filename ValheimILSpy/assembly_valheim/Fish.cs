using System;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour, IWaterInteractable, Hoverable, Interactable, IMonoUpdater
{
	[Serializable]
	public class BaitSetting
	{
		public ItemDrop m_bait;

		[Range(0f, 1f)]
		public float m_chance;
	}

	public string m_name = "Fish";

	public float m_swimRange = 20f;

	public float m_minDepth = 1f;

	public float m_maxDepth = 4f;

	public float m_speed = 10f;

	public float m_acceleration = 5f;

	public float m_turnRate = 10f;

	public float m_wpDurationMin = 4f;

	public float m_wpDurationMax = 4f;

	public float m_avoidSpeedScale = 2f;

	public float m_avoidRange = 5f;

	public float m_height = 0.2f;

	public float m_hookForce = 4f;

	public float m_staminaUse = 1f;

	public float m_escapeStaminaUse = 2f;

	public float m_escapeMin = 0.5f;

	public float m_escapeMax = 3f;

	public float m_escapeWaitMin = 0.75f;

	public float m_escapeWaitMax = 4f;

	public float m_escapeMaxPerLevel = 1.5f;

	public float m_baseHookChance = 0.5f;

	public GameObject m_pickupItem;

	public int m_pickupItemStackSize = 1;

	private float m_escapeTime;

	private DateTime m_nextEscape;

	private Vector3 m_spawnPoint;

	private bool m_fast;

	private DateTime m_lastCollision;

	private DateTime m_blockChange;

	[Tooltip("Fish aren't smart enough to change their mind too often (and makes reactions/collisions feel less artificial)")]
	public float m_blockChangeDurationMin = 0.1f;

	public float m_blockChangeDurationMax = 0.6f;

	public float m_collisionFleeTimeout = 1.5f;

	private Vector3 m_waypoint;

	private FishingFloat m_waypointFF;

	private FishingFloat m_failedBait;

	private bool m_haveWaypoint;

	[Header("Baits")]
	public List<BaitSetting> m_baits = new List<BaitSetting>();

	public DropTable m_extraDrops = new DropTable();

	[Header("Jumping")]
	public float m_jumpSpeed = 3f;

	public float m_jumpHeight = 14f;

	public float m_jumpForwardStrength = 16f;

	public float m_jumpHeightLand = 3f;

	public float m_jumpChance = 0.25f;

	public float m_jumpOnLandChance = 0.5f;

	public float m_jumpOnLandDecay = 0.5f;

	public float m_maxJumpDepthOffset = 0.5f;

	public float m_jumpFrequencySeconds = 0.1f;

	public float m_jumpOnLandRotation = 2f;

	public float m_waveJumpMultiplier = 0.05f;

	public float m_jumpMaxLevel = 2f;

	public EffectList m_jumpEffects = new EffectList();

	private float m_JumpHeightStrength;

	private bool m_jumpedFromLand;

	private bool m_isColliding;

	private bool m_isJumping;

	private DateTime m_lastJumpCheck;

	private float m_swimTimer;

	private float m_lastNibbleTime;

	[Header("Waves")]
	public float m_waveFollowDirection = 7f;

	private float m_lastWave;

	private float m_inWater = -10000f;

	private WaterVolume m_waterVolume;

	private LiquidSurface m_liquidSurface;

	private FishingFloat m_fishingFloat;

	private float m_pickupTime;

	private long m_lastOwner = -1L;

	private Vector3 m_originalLocalRef;

	private bool m_lodVisible = true;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private ItemDrop m_itemDrop;

	private LODGroup m_lodGroup;

	private static Vector4 s_wind;

	private static float s_wrappedTimeSeconds;

	private static DateTime s_now;

	private static float s_deltaTime;

	private static float s_time;

	private static float s_dawnDusk;

	private static int s_updatedFrame;

	private float m_waterDepth;

	private float m_waterWave;

	private int m_waterWaveCount;

	private readonly int[] m_liquids = new int[2];

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_itemDrop = GetComponent<ItemDrop>();
		m_lodGroup = GetComponent<LODGroup>();
		if ((bool)m_itemDrop)
		{
			if (m_itemDrop.m_itemData.m_quality > 1)
			{
				m_itemDrop.SetQuality(m_itemDrop.m_itemData.m_quality);
			}
			ItemDrop itemDrop = m_itemDrop;
			itemDrop.m_onDrop = (Action<ItemDrop>)Delegate.Combine(itemDrop.m_onDrop, new Action<ItemDrop>(onDrop));
			if (m_pickupItem == null)
			{
				m_pickupItem = base.gameObject;
			}
		}
		m_waterWaveCount = UnityEngine.Random.Range(0, 1);
		if ((bool)m_lodGroup)
		{
			m_originalLocalRef = m_lodGroup.localReferencePoint;
		}
	}

	private void Start()
	{
		m_spawnPoint = m_nview.GetZDO().GetVec3(ZDOVars.s_spawnPoint, base.transform.position);
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(ZDOVars.s_spawnPoint, m_spawnPoint);
		}
		if (m_nview.IsOwner())
		{
			RandomizeWaypoint(canHook: true, DateTime.Now);
		}
		if ((bool)m_nview && m_nview.IsValid())
		{
			m_nview.Register("RequestPickup", RPC_RequestPickup);
			m_nview.Register("Pickup", RPC_Pickup);
		}
		if (m_waterVolume != null)
		{
			m_waterDepth = m_waterVolume.Depth(base.transform.position);
			m_waterWave = m_waterVolume.CalcWave(base.transform.position, m_waterDepth, s_wrappedTimeSeconds, 1f);
		}
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	public string GetHoverText()
	{
		string text = m_name;
		if (IsOutOfWater())
		{
			if ((bool)m_itemDrop)
			{
				return m_itemDrop.GetHoverText();
			}
			text += "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup";
		}
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!IsOutOfWater())
		{
			return false;
		}
		if (Pickup(character))
		{
			return true;
		}
		return false;
	}

	public bool Pickup(Humanoid character)
	{
		if ((bool)m_itemDrop)
		{
			m_itemDrop.Pickup(character);
			return true;
		}
		if (m_pickupItem == null)
		{
			return false;
		}
		if (!character.GetInventory().CanAddItem(m_pickupItem, m_pickupItemStackSize))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_noroom");
			return false;
		}
		m_nview.InvokeRPC("RequestPickup");
		return true;
	}

	private void RPC_RequestPickup(long uid)
	{
		if (Time.time - m_pickupTime > 2f)
		{
			m_pickupTime = Time.time;
			m_nview.InvokeRPC(uid, "Pickup");
		}
	}

	private void RPC_Pickup(long uid)
	{
		if ((bool)Player.m_localPlayer && Player.m_localPlayer.PickupPrefab(m_pickupItem, m_pickupItemStackSize) != null)
		{
			m_nview.ClaimOwnership();
			m_nview.Destroy();
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void SetLiquidLevel(float level, LiquidType type, Component liquidObj)
	{
		if (type == LiquidType.Water)
		{
			m_inWater = level;
		}
		m_liquidSurface = null;
		m_waterVolume = null;
		if (liquidObj is WaterVolume waterVolume)
		{
			m_waterVolume = waterVolume;
		}
		else if (liquidObj is LiquidSurface liquidSurface)
		{
			m_liquidSurface = liquidSurface;
		}
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	public bool IsOutOfWater()
	{
		return m_inWater < base.transform.position.y - m_height;
	}

	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		if (Time.frameCount != s_updatedFrame)
		{
			EnvMan.instance.GetWindData(out var wind, out var wind2, out var _);
			s_wind = wind + wind2;
			s_wrappedTimeSeconds = ZNet.instance.GetWrappedDayTimeSeconds();
			s_now = DateTime.Now;
			s_deltaTime = fixedDeltaTime;
			s_time = Time.time;
			s_dawnDusk = 1f - Mathf.Abs(Mathf.Abs(EnvMan.instance.GetDayFraction() * 2f - 1f) - 0.5f) * 2f;
			s_updatedFrame = Time.frameCount;
		}
		Vector3 position = base.transform.position;
		bool flag = IsOutOfWater();
		if (m_waterVolume != null)
		{
			if ((++m_waterWaveCount & 1) == 1)
			{
				m_waterDepth = m_waterVolume.Depth(position);
			}
			else
			{
				m_waterWave = m_waterVolume.CalcWave(position, m_waterDepth, s_wrappedTimeSeconds, 1f);
			}
		}
		SetVisible(m_nview.HasOwner());
		if (m_lastOwner != m_nview.GetZDO().GetOwner())
		{
			m_lastOwner = m_nview.GetZDO().GetOwner();
			m_body.WakeUp();
		}
		if (!flag && UnityEngine.Random.value > 0.975f && m_nview.GetZDO().GetInt(ZDOVars.s_hooked) == 1 && m_nview.GetZDO().GetFloat(ZDOVars.s_escape) > 0f)
		{
			m_jumpEffects.Create(position, Quaternion.identity, base.transform);
		}
		if (!m_nview.IsOwner())
		{
			return;
		}
		FishingFloat fishingFloat = FishingFloat.FindFloat(this);
		if ((bool)fishingFloat)
		{
			Utils.Pull(m_body, fishingFloat.transform.position, 1f, m_hookForce, 1f, 0.5f);
		}
		if (m_isColliding && flag)
		{
			ConsiderJump(s_now);
		}
		if (m_escapeTime > 0f)
		{
			m_body.rotation *= Quaternion.AngleAxis(Mathf.Sin(m_escapeTime * 40f) * 12f, Vector3.up);
			m_escapeTime -= s_deltaTime;
			if (m_escapeTime <= 0f)
			{
				m_nview.GetZDO().Set(ZDOVars.s_escape, 0);
				m_nextEscape = s_now + TimeSpan.FromSeconds(UnityEngine.Random.Range(m_escapeWaitMin, m_escapeWaitMax));
			}
		}
		else if (s_now > m_nextEscape && IsHooked())
		{
			Escape();
		}
		if (m_inWater <= -10000f || m_inWater < position.y + m_height)
		{
			m_body.useGravity = true;
			if (flag)
			{
				if (m_isJumping)
				{
					Vector3 velocity = m_body.velocity;
					if (!m_jumpedFromLand && velocity != Vector3.zero)
					{
						velocity.y *= 1.6f;
						m_body.rotation = Quaternion.RotateTowards(m_body.rotation, Quaternion.LookRotation(velocity), 5f);
					}
				}
				return;
			}
		}
		if (m_isJumping)
		{
			if (m_body.velocity.y < 0f)
			{
				m_jumpEffects.Create(position, Quaternion.identity);
				m_isJumping = false;
				m_body.rotation = Quaternion.Euler(0f, m_body.rotation.eulerAngles.y, 0f);
				RandomizeWaypoint(canHook: true, s_now);
			}
		}
		else if (m_waterWave >= m_minDepth && m_waterWave < m_minDepth + m_maxJumpDepthOffset)
		{
			ConsiderJump(s_now);
		}
		m_JumpHeightStrength = 1f;
		m_body.useGravity = false;
		m_fast = false;
		bool flag2 = s_now > m_blockChange;
		Player playerNoiseRange = Player.GetPlayerNoiseRange(position);
		if ((bool)playerNoiseRange)
		{
			if (Vector3.Distance(position, playerNoiseRange.transform.position) > m_avoidRange / 2f && !IsHooked())
			{
				if (flag2 || s_now > m_lastCollision + TimeSpan.FromSeconds(m_collisionFleeTimeout))
				{
					Vector3 normalized = (position - playerNoiseRange.transform.position).normalized;
					SwimDirection(normalized, fast: true, avoidLand: true, s_deltaTime);
				}
				return;
			}
			m_fast = true;
			if (m_swimTimer > 0.5f)
			{
				m_swimTimer = 0.5f;
			}
		}
		m_swimTimer -= s_deltaTime;
		if (m_swimTimer <= 0f && flag2)
		{
			RandomizeWaypoint(!m_fast, s_now);
		}
		if (m_haveWaypoint)
		{
			if ((bool)m_waypointFF)
			{
				m_waypoint = m_waypointFF.transform.position + Vector3.down;
			}
			if (Vector2.Distance(m_waypoint, position) < 0.2f || (m_escapeTime < 0f && IsHooked()))
			{
				if (!m_waypointFF)
				{
					m_haveWaypoint = false;
					return;
				}
				if (s_time - m_lastNibbleTime > 1f && m_failedBait != m_waypointFF)
				{
					m_lastNibbleTime = s_time;
					bool flag3 = TestBate(m_waypointFF);
					m_waypointFF.Nibble(this, flag3);
					if (!flag3)
					{
						m_failedBait = m_waypointFF;
					}
				}
			}
			Vector3 dir = Vector3.Normalize(m_waypoint - position);
			SwimDirection(dir, m_fast, avoidLand: false, s_deltaTime);
		}
		else
		{
			Stop(s_deltaTime);
		}
		if (!flag && m_waterVolume != null)
		{
			m_body.AddForce(new Vector3(0f, m_waterWave - m_lastWave, 0f) * 10f, ForceMode.VelocityChange);
			m_lastWave = m_waterWave;
			if (m_waterWave > 0f)
			{
				m_body.AddForce((Vector3)s_wind * m_waveFollowDirection * m_waterWave);
			}
		}
	}

	private void Stop(float dt)
	{
		if (!(m_inWater < base.transform.position.y + m_height))
		{
			Vector3 forward = base.transform.forward;
			forward.y = 0f;
			forward.Normalize();
			Quaternion to = Quaternion.LookRotation(forward, Vector3.up);
			Quaternion rot = Quaternion.RotateTowards(m_body.rotation, to, m_turnRate * dt);
			m_body.MoveRotation(rot);
			Vector3 force = -m_body.velocity * m_acceleration;
			m_body.AddForce(force, ForceMode.VelocityChange);
		}
	}

	private void SwimDirection(Vector3 dir, bool fast, bool avoidLand, float dt)
	{
		Vector3 vector = dir;
		vector.y = 0f;
		if (vector == Vector3.zero)
		{
			ZLog.LogWarning("Invalid swim direction");
			return;
		}
		vector.Normalize();
		float num = m_turnRate;
		if (fast)
		{
			num *= m_avoidSpeedScale;
		}
		Quaternion to = Quaternion.LookRotation(vector, Vector3.up);
		Quaternion rotation = Quaternion.RotateTowards(base.transform.rotation, to, num * dt);
		if (!m_isJumping || !(m_body.velocity.y > 0f))
		{
			if (!m_isJumping)
			{
				m_body.rotation = rotation;
			}
			float num2 = m_speed;
			if (fast)
			{
				num2 *= m_avoidSpeedScale;
			}
			if (avoidLand && GetPointDepth(base.transform.position + base.transform.forward) < m_minDepth)
			{
				num2 = 0f;
			}
			if (fast && Vector3.Dot(dir, base.transform.forward) < 0f)
			{
				num2 = 0f;
			}
			Vector3 forward = base.transform.forward;
			forward.y = dir.y;
			Vector3 vector2 = forward * num2 - m_body.velocity;
			if (m_inWater < base.transform.position.y + m_height && vector2.y > 0f)
			{
				vector2.y = 0f;
			}
			m_body.AddForce(vector2 * m_acceleration, ForceMode.VelocityChange);
		}
	}

	private FishingFloat FindFloat()
	{
		foreach (FishingFloat allInstance in FishingFloat.GetAllInstances())
		{
			if (allInstance.IsInWater() && !(Vector3.Distance(base.transform.position, allInstance.transform.position) > allInstance.m_range) && !(allInstance.GetCatch() != null))
			{
				float baseHookChance = m_baseHookChance;
				if (UnityEngine.Random.value < baseHookChance)
				{
					return allInstance;
				}
			}
		}
		return null;
	}

	private bool TestBate(FishingFloat ff)
	{
		string bait = ff.GetBait();
		foreach (BaitSetting bait2 in m_baits)
		{
			if (bait2.m_bait.name == bait && UnityEngine.Random.value < bait2.m_chance)
			{
				return true;
			}
		}
		return false;
	}

	private bool RandomizeWaypoint(bool canHook, DateTime now)
	{
		if (m_isJumping)
		{
			return false;
		}
		Vector2 vector = UnityEngine.Random.insideUnitCircle * m_swimRange;
		m_waypoint = m_spawnPoint + new Vector3(vector.x, 0f, vector.y);
		m_waypointFF = null;
		if (canHook)
		{
			FishingFloat fishingFloat = FindFloat();
			if ((bool)fishingFloat && fishingFloat != m_failedBait)
			{
				m_waypointFF = fishingFloat;
				m_waypoint = fishingFloat.transform.position + Vector3.down;
			}
		}
		float pointDepth = GetPointDepth(m_waypoint);
		if (pointDepth < m_minDepth)
		{
			return false;
		}
		Vector3 p = (m_waypoint + base.transform.position) * 0.5f;
		if (GetPointDepth(p) < m_minDepth)
		{
			return false;
		}
		float maxInclusive = Mathf.Min(m_maxDepth, pointDepth - m_height);
		float waterLevel = GetWaterLevel(m_waypoint);
		m_waypoint.y = waterLevel - UnityEngine.Random.Range(m_minDepth, maxInclusive);
		m_haveWaypoint = true;
		m_swimTimer = UnityEngine.Random.Range(m_wpDurationMin, m_wpDurationMax);
		m_blockChange = now + TimeSpan.FromSeconds(UnityEngine.Random.Range(m_blockChangeDurationMin, m_blockChangeDurationMax));
		return true;
	}

	private void Escape()
	{
		m_escapeTime = UnityEngine.Random.Range(m_escapeMin, m_escapeMax + (float)((!m_itemDrop) ? 1 : m_itemDrop.m_itemData.m_quality) * m_escapeMaxPerLevel);
		m_nview.GetZDO().Set(ZDOVars.s_escape, m_escapeTime);
	}

	private float GetPointDepth(Vector3 p)
	{
		if ((bool)ZoneSystem.instance && ZoneSystem.instance.GetSolidHeight(p, out var height, (!(m_waterVolume != null)) ? 1000 : 0))
		{
			return GetWaterLevel(p) - height;
		}
		return 0f;
	}

	private float GetWaterLevel(Vector3 point)
	{
		if (!(m_waterVolume != null))
		{
			return 30f;
		}
		return m_waterVolume.GetWaterSurface(point);
	}

	private bool DangerNearby()
	{
		return Player.GetPlayerNoiseRange(base.transform.position) != null;
	}

	public ZDOID GetZDOID()
	{
		return m_nview.GetZDO().m_uid;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * m_height, new Vector3(1f, 0.02f, 1f));
	}

	private void OnCollisionEnter(Collision collision)
	{
		m_isColliding = true;
		onCollision();
	}

	private void OnCollisionStay(Collision collision)
	{
		if (DateTime.Now > m_lastCollision + TimeSpan.FromSeconds(0.5))
		{
			onCollision();
		}
		if (m_isJumping)
		{
			m_isJumping = false;
		}
	}

	private void OnCollisionExit(Collision collision)
	{
		m_isColliding = false;
	}

	private void onCollision()
	{
		m_lastCollision = DateTime.Now;
		if (!m_nview || !m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		for (int i = 0; i < 10; i++)
		{
			if (RandomizeWaypoint(!m_fast, DateTime.Now))
			{
				break;
			}
		}
	}

	private void onDrop(ItemDrop item)
	{
		m_JumpHeightStrength = 0f;
	}

	private void ConsiderJump(DateTime now)
	{
		if (((bool)m_itemDrop && (float)m_itemDrop.m_itemData.m_quality > m_jumpMaxLevel) || !(m_JumpHeightStrength > 0f) || !(now > m_lastJumpCheck + TimeSpan.FromSeconds(m_jumpFrequencySeconds)))
		{
			return;
		}
		m_lastJumpCheck = now;
		if (IsOutOfWater())
		{
			if (UnityEngine.Random.Range(0f, 1f) < m_jumpOnLandChance * m_JumpHeightStrength)
			{
				Jump();
			}
		}
		else if (UnityEngine.Random.Range(0f, 1f) < (m_jumpChance + Mathf.Min(0f, m_lastWave) * m_waveJumpMultiplier) * s_dawnDusk)
		{
			Jump();
		}
	}

	private void Jump()
	{
		if (!m_isJumping)
		{
			m_isJumping = true;
			if (IsOutOfWater())
			{
				m_jumpedFromLand = true;
				m_JumpHeightStrength *= m_jumpOnLandDecay;
				float jumpOnLandRotation = m_jumpOnLandRotation;
				m_body.AddForce(new Vector3(0f, m_JumpHeightStrength * m_jumpHeightLand * base.transform.localScale.y, 0f), ForceMode.Impulse);
				m_body.AddTorque(UnityEngine.Random.Range(0f - jumpOnLandRotation, jumpOnLandRotation), UnityEngine.Random.Range(0f - jumpOnLandRotation, jumpOnLandRotation), UnityEngine.Random.Range(0f - jumpOnLandRotation, jumpOnLandRotation), ForceMode.Impulse);
			}
			else
			{
				m_jumpedFromLand = false;
				m_jumpEffects.Create(base.transform.position, Quaternion.identity);
				m_body.AddForce(new Vector3(0f, m_jumpHeight * base.transform.localScale.y, 0f), ForceMode.Impulse);
				m_body.AddForce(base.transform.forward * m_jumpForwardStrength * base.transform.localScale.y, ForceMode.Impulse);
			}
		}
	}

	public void OnHooked(FishingFloat ff)
	{
		if ((bool)m_nview && m_nview.IsValid())
		{
			m_nview.ClaimOwnership();
		}
		m_fishingFloat = ff;
		if (m_nview.IsValid())
		{
			m_nview.GetZDO().Set(ZDOVars.s_hooked, (ff != null) ? 1 : 0);
			Escape();
		}
	}

	public bool IsHooked()
	{
		return m_fishingFloat != null;
	}

	public bool IsEscaping()
	{
		if (m_escapeTime > 0f)
		{
			return IsHooked();
		}
		return false;
	}

	public float GetStaminaUse()
	{
		if (!IsEscaping())
		{
			return m_staminaUse;
		}
		return m_escapeStaminaUse;
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

	public int Increment(LiquidType type)
	{
		return ++m_liquids[(int)type];
	}

	public int Decrement(LiquidType type)
	{
		return --m_liquids[(int)type];
	}
}
