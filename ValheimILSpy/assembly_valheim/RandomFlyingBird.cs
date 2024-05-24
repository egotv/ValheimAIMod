using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomFlyingBird : MonoBehaviour, IMonoUpdater
{
	public float m_flyRange = 20f;

	public float m_minAlt = 5f;

	public float m_maxAlt = 20f;

	public float m_speed = 10f;

	public float m_turnRate = 10f;

	public float m_wpDuration = 4f;

	public float m_flapDuration = 2f;

	public float m_sailDuration = 4f;

	public float m_landChance = 0.5f;

	public float m_landDuration = 2f;

	public float m_avoidDangerDistance = 4f;

	public bool m_noRandomFlightAtNight = true;

	public float m_randomNoiseIntervalMin = 3f;

	public float m_randomNoiseIntervalMax = 6f;

	public bool m_noNoiseAtNight = true;

	public EffectList m_randomNoise = new EffectList();

	public int m_randomIdles;

	public float m_randomIdleTimeMin = 1f;

	public float m_randomIdleTimeMax = 4f;

	public bool m_singleModel;

	public GameObject m_flyingModel;

	public GameObject m_landedModel;

	private Vector3 m_spawnPoint;

	private Vector3 m_waypoint;

	private bool m_groundwp;

	private float m_flyTimer;

	private float m_modeTimer;

	private float m_idleTimer;

	private float m_idleTargetTime = 1f;

	private float m_randomNoiseTimer;

	private ZSyncAnimation m_anim;

	private bool m_flapping = true;

	private float m_landedTimer;

	private static readonly int s_flapping = ZSyncAnimation.GetHash("flapping");

	private static readonly int s_flying = ZSyncAnimation.GetHash("flying");

	private static readonly int s_idle = ZSyncAnimation.GetHash("idle");

	private ZNetView m_nview;

	protected LODGroup m_lodGroup;

	private Vector3 m_originalLocalRef;

	private bool m_lodVisible = true;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_anim = GetComponentInChildren<ZSyncAnimation>();
		m_lodGroup = GetComponent<LODGroup>();
		if (!m_singleModel)
		{
			m_landedModel.SetActive(value: true);
			m_flyingModel.SetActive(value: true);
		}
		else
		{
			m_flyingModel.SetActive(value: true);
		}
		m_idleTargetTime = UnityEngine.Random.Range(m_randomIdleTimeMin, m_randomIdleTimeMax);
		m_spawnPoint = m_nview.GetZDO().GetVec3(ZDOVars.s_spawnPoint, base.transform.position);
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(ZDOVars.s_spawnPoint, m_spawnPoint);
		}
		m_randomNoiseTimer = UnityEngine.Random.Range(m_randomNoiseIntervalMin, m_randomNoiseIntervalMax);
		if (m_nview.IsOwner())
		{
			RandomizeWaypoint(ground: false);
		}
		if ((bool)m_lodGroup)
		{
			m_originalLocalRef = m_lodGroup.localReferencePoint;
		}
	}

	private void OnEnable()
	{
		if (m_nview != null)
		{
			Instances.Add(this);
		}
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	public void CustomFixedUpdate(float dt)
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		bool flag = EnvMan.IsDaylight();
		m_randomNoiseTimer -= dt;
		if (m_randomNoiseTimer <= 0f)
		{
			if (flag || !m_noNoiseAtNight)
			{
				m_randomNoise.Create(base.transform.position, Quaternion.identity, base.transform);
			}
			m_randomNoiseTimer = UnityEngine.Random.Range(m_randomNoiseIntervalMin, m_randomNoiseIntervalMax);
		}
		bool @bool = m_nview.GetZDO().GetBool(ZDOVars.s_landed);
		if (!m_singleModel)
		{
			m_landedModel.SetActive(@bool);
			m_flyingModel.SetActive(!@bool);
		}
		SetVisible(m_nview.HasOwner());
		if (!m_nview.IsOwner())
		{
			return;
		}
		m_flyTimer += dt;
		m_modeTimer += dt;
		if (m_singleModel)
		{
			m_anim.SetBool(s_flying, !@bool);
		}
		if (@bool)
		{
			Vector3 forward = base.transform.forward;
			forward.y = 0f;
			forward.Normalize();
			base.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
			if (m_randomIdles > 0)
			{
				m_idleTimer += Time.fixedDeltaTime;
				if (m_idleTimer > m_idleTargetTime)
				{
					m_idleTargetTime = UnityEngine.Random.Range(m_randomIdleTimeMin, m_randomIdleTimeMax);
					m_idleTimer = 0f;
					m_anim.SetFloat(s_idle, UnityEngine.Random.Range(0, m_randomIdles));
				}
			}
			m_landedTimer += dt;
			if (((flag || !m_noRandomFlightAtNight) && m_landedTimer > m_landDuration) || DangerNearby(base.transform.position))
			{
				m_nview.GetZDO().Set(ZDOVars.s_landed, value: false);
				RandomizeWaypoint(ground: false);
			}
			return;
		}
		if (m_flapping)
		{
			if (m_modeTimer > m_flapDuration)
			{
				m_modeTimer = 0f;
				m_flapping = false;
			}
		}
		else if (m_modeTimer > m_sailDuration)
		{
			m_flapping = true;
			m_modeTimer = 0f;
		}
		m_anim.SetBool(s_flapping, m_flapping);
		Vector3 vector = Vector3.Normalize(m_waypoint - base.transform.position);
		float num = (m_groundwp ? (m_turnRate * 4f) : m_turnRate);
		Vector3 vector2 = Vector3.RotateTowards(base.transform.forward, vector, num * ((float)Math.PI / 180f) * dt, 1f);
		float num2 = Vector3.SignedAngle(base.transform.forward, vector, Vector3.up);
		Vector3 vector3 = Vector3.Cross(vector2, Vector3.up);
		Vector3 up = Vector3.up;
		if (num2 > 0f)
		{
			up += -vector3 * 1.5f * Utils.LerpStep(0f, 45f, num2);
		}
		else
		{
			up += vector3 * 1.5f * Utils.LerpStep(0f, 45f, 0f - num2);
		}
		float num3 = m_speed;
		bool flag2 = false;
		if (m_groundwp)
		{
			float num4 = Vector3.Distance(base.transform.position, m_waypoint);
			if (num4 < 5f)
			{
				num3 *= Mathf.Clamp(num4 / 5f, 0.2f, 1f);
				vector2.y = 0f;
				vector2.Normalize();
				up = Vector3.up;
				flag2 = true;
			}
			if (num4 < 0.2f)
			{
				base.transform.position = m_waypoint;
				m_nview.GetZDO().Set(ZDOVars.s_landed, value: true);
				m_landedTimer = 0f;
				m_flapping = true;
				m_modeTimer = 0f;
			}
		}
		else if (m_flyTimer >= m_wpDuration)
		{
			bool ground = UnityEngine.Random.value < m_landChance;
			RandomizeWaypoint(ground);
		}
		Quaternion to = Quaternion.LookRotation(vector2, up.normalized);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, 200f * dt);
		if (flag2)
		{
			base.transform.position += vector * num3 * dt;
		}
		else
		{
			base.transform.position += base.transform.forward * num3 * dt;
		}
	}

	private void RandomizeWaypoint(bool ground)
	{
		m_flyTimer = 0f;
		if (ground && FindLandingPoint(out var waypoint))
		{
			m_waypoint = waypoint;
			m_groundwp = true;
			return;
		}
		Vector2 vector = UnityEngine.Random.insideUnitCircle * m_flyRange;
		m_waypoint = m_spawnPoint + new Vector3(vector.x, 0f, vector.y);
		if (ZoneSystem.instance.GetSolidHeight(m_waypoint, out var height))
		{
			float num = 32f;
			if (height < num)
			{
				height = num;
			}
			m_waypoint.y = height + UnityEngine.Random.Range(m_minAlt, m_maxAlt);
		}
		m_groundwp = false;
	}

	private bool FindLandingPoint(out Vector3 waypoint)
	{
		waypoint = new Vector3(0f, -999f, 0f);
		bool result = false;
		for (int i = 0; i < 10; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * m_flyRange;
			Vector3 vector2 = m_spawnPoint + new Vector3(vector.x, 0f, vector.y);
			if (ZoneSystem.instance.GetSolidHeight(vector2, out var height) && height > 30f && height > waypoint.y)
			{
				vector2.y = height;
				if (!DangerNearby(vector2))
				{
					waypoint = vector2;
					result = true;
				}
			}
		}
		return result;
	}

	private bool DangerNearby(Vector3 p)
	{
		return Player.IsPlayerInRange(p, m_avoidDangerDistance);
	}

	private void SetVisible(bool visible)
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
}
