using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Ship : MonoBehaviour, IMonoUpdater
{
	public enum Speed
	{
		Stop,
		Back,
		Slow,
		Half,
		Full
	}

	private bool m_forwardPressed;

	private bool m_backwardPressed;

	private float m_sendRudderTime;

	private float m_ashDamageMsgTimer;

	public float m_ashDamageMsgTime = 10f;

	[Header("Objects")]
	public GameObject m_sailObject;

	public GameObject m_mastObject;

	public GameObject m_rudderObject;

	public ShipControlls m_shipControlls;

	public Transform m_controlGuiPos;

	public GameObject m_ashdamageEffects;

	[Header("Misc")]
	public BoxCollider m_floatCollider;

	public float m_waterLevelOffset;

	public float m_forceDistance = 1f;

	public float m_force = 0.5f;

	public float m_damping = 0.05f;

	public float m_dampingSideway = 0.05f;

	public float m_dampingForward = 0.01f;

	public float m_angularDamping = 0.01f;

	public float m_disableLevel = -0.5f;

	public float m_sailForceOffset;

	public float m_sailForceFactor = 0.1f;

	public float m_rudderSpeed = 0.5f;

	public float m_stearForceOffset = -10f;

	public float m_stearForce = 0.5f;

	public float m_stearVelForceFactor = 0.1f;

	public float m_backwardForce = 50f;

	public float m_rudderRotationMax = 30f;

	public float m_minWaterImpactForce = 2.5f;

	public float m_minWaterImpactInterval = 2f;

	public float m_waterImpactDamage = 10f;

	public float m_upsideDownDmgInterval = 1f;

	public float m_upsideDownDmg = 20f;

	public EffectList m_waterImpactEffect = new EffectList();

	public bool m_ashlandsReady;

	private bool m_sailWasInPosition;

	private Vector3 m_windChangeVelocity = Vector3.zero;

	private Speed m_speed;

	private float m_rudder;

	private float m_rudderValue;

	private Vector3 m_sailForce = Vector3.zero;

	private readonly List<Player> m_players = new List<Player>();

	private List<AudioSource> m_ashlandsFxAudio;

	private WaterVolume m_previousCenter;

	private WaterVolume m_previousLeft;

	private WaterVolume m_previousRight;

	private WaterVolume m_previousForward;

	private WaterVolume m_previousBack;

	private static readonly List<Ship> s_currentShips = new List<Ship>();

	private GlobalWind m_globalWind;

	private Rigidbody m_body;

	private ZNetView m_nview;

	private IDestructible m_destructible;

	private Cloth m_sailCloth;

	private float m_lastDepth = -9999f;

	private float m_lastWaterImpactTime;

	private float m_upsideDownDmgTimer;

	private float m_ashlandsDmgTimer;

	private float m_rudderPaddleTimer;

	private float m_lastUpdateWaterForceTime;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_destructible = GetComponent<IDestructible>();
		WearNTear component = GetComponent<WearNTear>();
		if ((bool)component)
		{
			component.m_onDestroyed = (Action)Delegate.Combine(component.m_onDestroyed, new Action(OnDestroyed));
		}
		if (m_nview.GetZDO() == null)
		{
			base.enabled = false;
		}
		m_body.maxDepenetrationVelocity = 2f;
		Heightmap.ForceGenerateAll();
		m_sailCloth = m_sailObject.GetComponentInChildren<Cloth>();
		if ((bool)m_sailCloth)
		{
			m_globalWind = m_sailCloth.gameObject.GetComponent<GlobalWind>();
		}
		if ((bool)m_ashdamageEffects)
		{
			m_ashdamageEffects.SetActive(value: false);
			m_ashlandsFxAudio = m_ashdamageEffects.GetComponentsInChildren<AudioSource>().ToList();
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

	public bool CanBeRemoved()
	{
		return m_players.Count == 0;
	}

	private void Start()
	{
		m_nview.Register("Stop", RPC_Stop);
		m_nview.Register("Forward", RPC_Forward);
		m_nview.Register("Backward", RPC_Backward);
		m_nview.Register<float>("Rudder", RPC_Rudder);
		InvokeRepeating("UpdateOwner", 2f, 2f);
	}

	private void PrintStats()
	{
		if (m_players.Count != 0)
		{
			ZLog.Log("Vel:" + m_body.velocity.magnitude.ToString("0.0"));
		}
	}

	public void ApplyControlls(Vector3 dir)
	{
		bool flag = (double)dir.z > 0.5;
		bool flag2 = (double)dir.z < -0.5;
		if (flag && !m_forwardPressed)
		{
			Forward();
		}
		if (flag2 && !m_backwardPressed)
		{
			Backward();
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		float num = Mathf.Lerp(0.5f, 1f, Mathf.Abs(m_rudderValue));
		m_rudder = dir.x * num;
		m_rudderValue += m_rudder * m_rudderSpeed * fixedDeltaTime;
		m_rudderValue = Mathf.Clamp(m_rudderValue, -1f, 1f);
		if (Time.time - m_sendRudderTime > 0.2f)
		{
			m_sendRudderTime = Time.time;
			m_nview.InvokeRPC("Rudder", m_rudderValue);
		}
		m_forwardPressed = flag;
		m_backwardPressed = flag2;
	}

	public void Forward()
	{
		m_nview.InvokeRPC("Forward");
	}

	public void Backward()
	{
		m_nview.InvokeRPC("Backward");
	}

	public void Rudder(float rudder)
	{
		m_nview.Invoke("Rudder", rudder);
	}

	private void RPC_Rudder(long sender, float value)
	{
		m_rudderValue = value;
	}

	public void Stop()
	{
		m_nview.InvokeRPC("Stop");
	}

	private void RPC_Stop(long sender)
	{
		m_speed = Speed.Stop;
	}

	private void RPC_Forward(long sender)
	{
		switch (m_speed)
		{
		case Speed.Stop:
			m_speed = Speed.Slow;
			break;
		case Speed.Slow:
			m_speed = Speed.Half;
			break;
		case Speed.Half:
			m_speed = Speed.Full;
			break;
		case Speed.Back:
			m_speed = Speed.Stop;
			break;
		case Speed.Full:
			break;
		}
	}

	private void RPC_Backward(long sender)
	{
		switch (m_speed)
		{
		case Speed.Stop:
			m_speed = Speed.Back;
			break;
		case Speed.Slow:
			m_speed = Speed.Stop;
			break;
		case Speed.Half:
			m_speed = Speed.Slow;
			break;
		case Speed.Full:
			m_speed = Speed.Half;
			break;
		case Speed.Back:
			break;
		}
	}

	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		bool flag = HaveControllingPlayer();
		UpdateControlls(fixedDeltaTime);
		UpdateSail(fixedDeltaTime);
		UpdateRudder(fixedDeltaTime, flag);
		if ((bool)m_nview && !m_nview.IsOwner())
		{
			return;
		}
		UpdateUpsideDmg(fixedDeltaTime);
		TakeAshlandsDamage(fixedDeltaTime);
		if (m_players.Count == 0)
		{
			m_speed = Speed.Stop;
			m_rudderValue = 0f;
		}
		if (!flag && (m_speed == Speed.Slow || m_speed == Speed.Back))
		{
			m_speed = Speed.Stop;
		}
		Vector3 worldCenterOfMass = m_body.worldCenterOfMass;
		Vector3 vector = m_floatCollider.transform.position + m_floatCollider.transform.forward * m_floatCollider.size.z / 2f;
		Vector3 vector2 = m_floatCollider.transform.position - m_floatCollider.transform.forward * m_floatCollider.size.z / 2f;
		Vector3 vector3 = m_floatCollider.transform.position - m_floatCollider.transform.right * m_floatCollider.size.x / 2f;
		Vector3 vector4 = m_floatCollider.transform.position + m_floatCollider.transform.right * m_floatCollider.size.x / 2f;
		float waterLevel = Floating.GetWaterLevel(worldCenterOfMass, ref m_previousCenter);
		float waterLevel2 = Floating.GetWaterLevel(vector3, ref m_previousLeft);
		float waterLevel3 = Floating.GetWaterLevel(vector4, ref m_previousRight);
		float waterLevel4 = Floating.GetWaterLevel(vector, ref m_previousForward);
		float waterLevel5 = Floating.GetWaterLevel(vector2, ref m_previousBack);
		float num = (waterLevel + waterLevel2 + waterLevel3 + waterLevel4 + waterLevel5) / 5f;
		float num2 = worldCenterOfMass.y - num - m_waterLevelOffset;
		if (!(num2 > m_disableLevel))
		{
			m_body.WakeUp();
			UpdateWaterForce(num2, Time.time);
			Vector3 vector5 = new Vector3(vector3.x, waterLevel2, vector3.z);
			Vector3 vector6 = new Vector3(vector4.x, waterLevel3, vector4.z);
			Vector3 vector7 = new Vector3(vector.x, waterLevel4, vector.z);
			Vector3 vector8 = new Vector3(vector2.x, waterLevel5, vector2.z);
			float num3 = fixedDeltaTime * 50f;
			float num4 = Mathf.Clamp01(Mathf.Abs(num2) / m_forceDistance);
			Vector3 vector9 = Vector3.up * (m_force * num4);
			m_body.AddForceAtPosition(vector9 * num3, worldCenterOfMass, ForceMode.VelocityChange);
			float num5 = Vector3.Dot(m_body.velocity, base.transform.forward);
			float num6 = Vector3.Dot(m_body.velocity, base.transform.right);
			Vector3 velocity = m_body.velocity;
			float value = velocity.y * velocity.y * Mathf.Sign(velocity.y) * m_damping * num4;
			float value2 = num5 * num5 * Mathf.Sign(num5) * m_dampingForward * num4;
			float value3 = num6 * num6 * Mathf.Sign(num6) * m_dampingSideway * num4;
			velocity.y -= Mathf.Clamp(value, -1f, 1f);
			velocity -= base.transform.forward * Mathf.Clamp(value2, -1f, 1f);
			velocity -= base.transform.right * Mathf.Clamp(value3, -1f, 1f);
			if (velocity.magnitude > m_body.velocity.magnitude)
			{
				velocity = velocity.normalized * m_body.velocity.magnitude;
			}
			if (m_players.Count == 0)
			{
				velocity.x *= 0.1f;
				velocity.z *= 0.1f;
			}
			m_body.velocity = velocity;
			m_body.angularVelocity -= m_body.angularVelocity * (m_angularDamping * num4);
			float num7 = 0.15f;
			float num8 = 0.5f;
			float f = Mathf.Clamp((vector7.y - vector.y) * num7, 0f - num8, num8);
			float f2 = Mathf.Clamp((vector8.y - vector2.y) * num7, 0f - num8, num8);
			float f3 = Mathf.Clamp((vector5.y - vector3.y) * num7, 0f - num8, num8);
			float f4 = Mathf.Clamp((vector6.y - vector4.y) * num7, 0f - num8, num8);
			f = Mathf.Sign(f) * Mathf.Abs(Mathf.Pow(f, 2f));
			f2 = Mathf.Sign(f2) * Mathf.Abs(Mathf.Pow(f2, 2f));
			f3 = Mathf.Sign(f3) * Mathf.Abs(Mathf.Pow(f3, 2f));
			f4 = Mathf.Sign(f4) * Mathf.Abs(Mathf.Pow(f4, 2f));
			m_body.AddForceAtPosition(Vector3.up * (f * num3), vector, ForceMode.VelocityChange);
			m_body.AddForceAtPosition(Vector3.up * (f2 * num3), vector2, ForceMode.VelocityChange);
			m_body.AddForceAtPosition(Vector3.up * (f3 * num3), vector3, ForceMode.VelocityChange);
			m_body.AddForceAtPosition(Vector3.up * (f4 * num3), vector4, ForceMode.VelocityChange);
			float sailSize = 0f;
			if (m_speed == Speed.Full)
			{
				sailSize = 1f;
			}
			else if (m_speed == Speed.Half)
			{
				sailSize = 0.5f;
			}
			Vector3 sailForce = GetSailForce(sailSize, fixedDeltaTime);
			Vector3 position = worldCenterOfMass + base.transform.up * m_sailForceOffset;
			m_body.AddForceAtPosition(sailForce, position, ForceMode.VelocityChange);
			Vector3 position2 = base.transform.position + base.transform.forward * m_stearForceOffset;
			float num9 = num5 * m_stearVelForceFactor;
			m_body.AddForceAtPosition(base.transform.right * num9 * (0f - m_rudderValue) * fixedDeltaTime, position2, ForceMode.VelocityChange);
			Vector3 zero = Vector3.zero;
			switch (m_speed)
			{
			case Speed.Slow:
				zero += base.transform.forward * (m_backwardForce * (1f - Mathf.Abs(m_rudderValue)));
				break;
			case Speed.Back:
				zero += -base.transform.forward * (m_backwardForce * (1f - Mathf.Abs(m_rudderValue)));
				break;
			}
			if (m_speed == Speed.Back || m_speed == Speed.Slow)
			{
				float num10 = ((m_speed != Speed.Back) ? 1 : (-1));
				zero += base.transform.right * (m_stearForce * (0f - m_rudderValue) * num10);
			}
			m_body.AddForceAtPosition(zero * fixedDeltaTime, position2, ForceMode.VelocityChange);
			ApplyEdgeForce(fixedDeltaTime);
		}
	}

	private void UpdateUpsideDmg(float dt)
	{
		if (!(base.transform.up.y >= 0f))
		{
			m_upsideDownDmgTimer += dt;
			if (!(m_upsideDownDmgTimer <= m_upsideDownDmgInterval))
			{
				m_upsideDownDmgTimer = 0f;
				HitData hitData = new HitData();
				hitData.m_damage.m_blunt = m_upsideDownDmg;
				hitData.m_point = base.transform.position;
				hitData.m_dir = Vector3.up;
				m_destructible.Damage(hitData);
			}
		}
	}

	private void TakeAshlandsDamage(float dt)
	{
		if (m_ashlandsReady)
		{
			return;
		}
		float ashlandsOceanGradient = WorldGenerator.GetAshlandsOceanGradient(base.transform.position);
		if ((bool)m_ashdamageEffects)
		{
			if (ashlandsOceanGradient < 0f)
			{
				m_ashdamageEffects.SetActive(value: false);
				{
					foreach (AudioSource item in m_ashlandsFxAudio)
					{
						item.Stop();
					}
					return;
				}
			}
			m_ashdamageEffects.SetActive(value: true);
		}
		if (m_ashDamageMsgTimer <= 0f && (bool)ZoneSystem.instance && (bool)Player.m_localPlayer)
		{
			ZoneSystem.instance.SetGlobalKey(GlobalKeys.AshlandsOcean);
			m_ashDamageMsgTimer = m_ashDamageMsgTime;
		}
		else
		{
			m_ashDamageMsgTimer -= Time.fixedDeltaTime;
		}
		m_ashlandsDmgTimer += dt;
		if (!((double)m_ashlandsDmgTimer <= 1.0))
		{
			m_ashlandsDmgTimer = 0f;
			ashlandsOceanGradient = Mathf.Clamp(ashlandsOceanGradient, 0f, 3f);
			HitData hitData = new HitData();
			hitData.m_damage.m_blunt = Mathf.Floor(Mathf.Lerp(1f, 30f, ashlandsOceanGradient));
			hitData.m_hitType = HitData.HitType.AshlandsOcean;
			hitData.m_point = base.transform.position;
			hitData.m_dir = Vector3.up;
			m_destructible.Damage(hitData);
		}
	}

	private Vector3 GetSailForce(float sailSize, float dt)
	{
		Vector3 windDir = EnvMan.instance.GetWindDir();
		float windIntensity = EnvMan.instance.GetWindIntensity();
		float num = Mathf.Lerp(0.25f, 1f, windIntensity);
		float windAngleFactor = GetWindAngleFactor();
		windAngleFactor *= num;
		Vector3 target = Vector3.Normalize(windDir + base.transform.forward) * (windAngleFactor * m_sailForceFactor * sailSize);
		m_sailForce = Vector3.SmoothDamp(m_sailForce, target, ref m_windChangeVelocity, 1f, 99f);
		return m_sailForce;
	}

	public float GetWindAngleFactor()
	{
		float num = Vector3.Dot(EnvMan.instance.GetWindDir(), -base.transform.forward);
		float num2 = Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(num));
		float num3 = 1f - Utils.LerpStep(0.75f, 0.8f, num);
		return num2 * num3;
	}

	private void UpdateWaterForce(float depth, float time)
	{
		float num = depth - m_lastDepth;
		float num2 = time - m_lastUpdateWaterForceTime;
		m_lastDepth = depth;
		m_lastUpdateWaterForceTime = time;
		float num3 = num / num2;
		if (!(num3 > 0f) && Utils.Abs(num3) > m_minWaterImpactForce && time - m_lastWaterImpactTime > m_minWaterImpactInterval)
		{
			m_lastWaterImpactTime = time;
			m_waterImpactEffect.Create(base.transform.position, base.transform.rotation);
			if (m_players.Count > 0)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_blunt = m_waterImpactDamage;
				hitData.m_point = base.transform.position;
				hitData.m_dir = Vector3.up;
				m_destructible.Damage(hitData);
			}
		}
	}

	private void ApplyEdgeForce(float dt)
	{
		float magnitude = base.transform.position.magnitude;
		float num = 10420f;
		if (magnitude > num)
		{
			Vector3 vector = Vector3.Normalize(base.transform.position);
			float num2 = Utils.LerpStep(num, 10500f, magnitude) * 8f;
			Vector3 vector2 = vector * num2;
			m_body.AddForce(vector2 * dt, ForceMode.VelocityChange);
		}
	}

	private void FixTilt()
	{
		float num = Mathf.Asin(base.transform.right.y);
		float num2 = Mathf.Asin(base.transform.forward.y);
		if (Mathf.Abs(num) > (float)Math.PI / 6f)
		{
			if (num > 0f)
			{
				base.transform.RotateAround(base.transform.position, base.transform.forward, (0f - Time.fixedDeltaTime) * 20f);
			}
			else
			{
				base.transform.RotateAround(base.transform.position, base.transform.forward, Time.fixedDeltaTime * 20f);
			}
		}
		if (Mathf.Abs(num2) > (float)Math.PI / 6f)
		{
			if (num2 > 0f)
			{
				base.transform.RotateAround(base.transform.position, base.transform.right, (0f - Time.fixedDeltaTime) * 20f);
			}
			else
			{
				base.transform.RotateAround(base.transform.position, base.transform.right, Time.fixedDeltaTime * 20f);
			}
		}
	}

	private void UpdateControlls(float dt)
	{
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(ZDOVars.s_forward, (int)m_speed);
			m_nview.GetZDO().Set(ZDOVars.s_rudder, m_rudderValue);
			return;
		}
		m_speed = (Speed)m_nview.GetZDO().GetInt(ZDOVars.s_forward);
		if (Time.time - m_sendRudderTime > 1f)
		{
			m_rudderValue = m_nview.GetZDO().GetFloat(ZDOVars.s_rudder);
		}
	}

	public bool IsSailUp()
	{
		if (m_speed != Speed.Half)
		{
			return m_speed == Speed.Full;
		}
		return true;
	}

	private void UpdateSail(float dt)
	{
		UpdateSailSize(dt);
		Vector3 windDir = EnvMan.instance.GetWindDir();
		windDir = Vector3.Cross(Vector3.Cross(windDir, base.transform.up), base.transform.up);
		if (m_speed == Speed.Full || m_speed == Speed.Half)
		{
			float t = 0.5f + Vector3.Dot(base.transform.forward, windDir) * 0.5f;
			Quaternion to = Quaternion.LookRotation(-Vector3.Lerp(windDir, Vector3.Normalize(windDir - base.transform.forward), t), base.transform.up);
			m_mastObject.transform.rotation = Quaternion.RotateTowards(m_mastObject.transform.rotation, to, 30f * dt);
		}
		else if (m_speed == Speed.Back)
		{
			Quaternion from = Quaternion.LookRotation(-base.transform.forward, base.transform.up);
			Quaternion to2 = Quaternion.LookRotation(-windDir, base.transform.up);
			to2 = Quaternion.RotateTowards(from, to2, 80f);
			m_mastObject.transform.rotation = Quaternion.RotateTowards(m_mastObject.transform.rotation, to2, 30f * dt);
		}
	}

	private void UpdateRudder(float dt, bool haveControllingPlayer)
	{
		if (!m_rudderObject)
		{
			return;
		}
		Quaternion b = Quaternion.Euler(0f, m_rudderRotationMax * (0f - m_rudderValue), 0f);
		if (haveControllingPlayer)
		{
			if (m_speed == Speed.Slow)
			{
				m_rudderPaddleTimer += dt;
				b *= Quaternion.Euler(0f, Mathf.Sin(m_rudderPaddleTimer * 6f) * 20f, 0f);
			}
			else if (m_speed == Speed.Back)
			{
				m_rudderPaddleTimer += dt;
				b *= Quaternion.Euler(0f, Mathf.Sin(m_rudderPaddleTimer * -3f) * 40f, 0f);
			}
		}
		m_rudderObject.transform.localRotation = Quaternion.Slerp(m_rudderObject.transform.localRotation, b, 0.5f);
	}

	private void UpdateSailSize(float dt)
	{
		float num = 0f;
		switch (m_speed)
		{
		case Speed.Back:
			num = 0.1f;
			break;
		case Speed.Half:
			num = 0.5f;
			break;
		case Speed.Full:
			num = 1f;
			break;
		case Speed.Slow:
			num = 0.1f;
			break;
		case Speed.Stop:
			num = 0.1f;
			break;
		}
		Vector3 localScale = m_sailObject.transform.localScale;
		bool flag = Mathf.Abs(localScale.y - num) < 0.01f;
		if (!flag)
		{
			localScale.y = Mathf.MoveTowards(localScale.y, num, dt);
			m_sailObject.transform.localScale = localScale;
		}
		if ((bool)m_sailCloth)
		{
			if (m_speed == Speed.Stop || m_speed == Speed.Slow || m_speed == Speed.Back)
			{
				if (flag && m_sailCloth.enabled)
				{
					m_sailCloth.enabled = false;
				}
			}
			else if (flag)
			{
				if (!m_sailWasInPosition)
				{
					Utils.RecreateComponent(ref m_sailCloth);
					if ((bool)m_globalWind)
					{
						m_globalWind.UpdateClothReference(m_sailCloth);
					}
				}
			}
			else
			{
				m_sailCloth.enabled = true;
			}
		}
		m_sailWasInPosition = flag;
	}

	private void UpdateOwner()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && !(Player.m_localPlayer == null) && m_players.Count > 0 && !IsPlayerInBoat(Player.m_localPlayer))
		{
			RefreshPlayerList();
			long newOwnerID = GetNewOwnerID();
			m_nview.GetZDO().SetOwner(newOwnerID);
			ZLog.Log("Changing ship owner to " + newOwnerID);
		}
	}

	private long GetNewOwnerID()
	{
		long num = 0L;
		for (int i = 0; i < m_players.Count; i++)
		{
			num = m_players[i].GetOwner();
			if (num != 0L)
			{
				break;
			}
		}
		if (num == 0L)
		{
			num = ZDOMan.GetSessionID();
		}
		return num;
	}

	private void RefreshPlayerList()
	{
		for (int i = 0; i < m_players.Count; i++)
		{
			if (m_players[i].GetOwner() == 0L)
			{
				m_players.RemoveAt(i);
			}
		}
	}

	private void OnTriggerEnter(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if ((bool)component)
		{
			m_players.Add(component);
			ZLog.Log("Player onboard, total onboard " + m_players.Count);
			if (component == Player.m_localPlayer)
			{
				s_currentShips.Add(this);
			}
		}
		Character component2 = collider.GetComponent<Character>();
		if ((bool)component2)
		{
			component2.InNumShipVolumes++;
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if ((bool)component)
		{
			m_players.Remove(component);
			ZLog.Log("Player over board, players left " + m_players.Count);
			if (component == Player.m_localPlayer)
			{
				s_currentShips.Remove(this);
			}
		}
		Character component2 = collider.GetComponent<Character>();
		if ((bool)component2)
		{
			component2.InNumShipVolumes--;
		}
	}

	public bool IsPlayerInBoat(ZDOID zdoid)
	{
		foreach (Player player in m_players)
		{
			if (player.GetZDOID() == zdoid)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsPlayerInBoat(Player player)
	{
		return m_players.Contains(player);
	}

	public bool IsPlayerInBoat(long playerID)
	{
		foreach (Player player in m_players)
		{
			if (player.GetPlayerID() == playerID)
			{
				return true;
			}
		}
		return false;
	}

	public bool HasPlayerOnboard()
	{
		return m_players.Count > 0;
	}

	private void OnDestroyed()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			Gogan.LogEvent("Game", "ShipDestroyed", base.gameObject.name, 0L);
		}
		s_currentShips.Remove(this);
	}

	public bool IsWindControllActive()
	{
		foreach (Player player in m_players)
		{
			if (player.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.SailingPower))
			{
				return true;
			}
		}
		return false;
	}

	public static Ship GetLocalShip()
	{
		if (s_currentShips.Count != 0)
		{
			return s_currentShips[s_currentShips.Count - 1];
		}
		return null;
	}

	private bool HaveControllingPlayer()
	{
		if (m_players.Count != 0)
		{
			return m_shipControlls.HaveValidUser();
		}
		return false;
	}

	public bool IsOwner()
	{
		if (m_nview.IsValid())
		{
			return m_nview.IsOwner();
		}
		return false;
	}

	public float GetSpeed()
	{
		return Vector3.Dot(m_body.velocity, base.transform.forward);
	}

	public Speed GetSpeedSetting()
	{
		return m_speed;
	}

	public float GetRudder()
	{
		return m_rudder;
	}

	public float GetRudderValue()
	{
		return m_rudderValue;
	}

	public float GetShipYawAngle()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return 0f;
		}
		return 0f - Utils.YawFromDirection(mainCamera.transform.InverseTransformDirection(base.transform.forward));
	}

	public float GetWindAngle()
	{
		Vector3 windDir = EnvMan.instance.GetWindDir();
		return 0f - Utils.YawFromDirection(base.transform.InverseTransformDirection(windDir));
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(base.transform.position + base.transform.forward * m_stearForceOffset, 0.25f);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position + base.transform.up * m_sailForceOffset, 0.25f);
	}
}
