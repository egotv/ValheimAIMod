using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ShieldGenerator : MonoBehaviour, Hoverable, Interactable
{
	private static bool s_cleanShields = false;

	private static List<ShieldGenerator> m_instances = new List<ShieldGenerator>();

	private static int m_instanceChangeID = 0;

	private static ShieldDomeImageEffect m_shieldDomeEffect;

	public string m_name = "$piece_shieldgenerator";

	public string m_add = "$piece_shieldgenerator_add";

	public Switch m_addFuelSwitch;

	public GameObject m_enabledObject;

	public GameObject m_disabledObject;

	[Header("Fuel")]
	public List<ItemDrop> m_fuelItems = new List<ItemDrop>();

	public int m_maxFuel = 10;

	public int m_defaultFuel;

	public float m_fuelPerDamage = 0.01f;

	public EffectList m_fuelAddedEffects = new EffectList();

	[Header("Shield")]
	public GameObject m_shieldDome;

	public float m_minShieldRadius = 10f;

	public float m_maxShieldRadius = 30f;

	public float m_decreaseInertia = 0.98f;

	public float m_startStopSpeed = 0.5f;

	public bool m_offWhenNoFuel = true;

	[Header("Attack")]
	public bool m_enableAttack = true;

	public float m_attackChargeTime = 900f;

	public bool m_damagePlayers = true;

	public GameObject m_attackObject;

	public EffectList m_attackEffects = new EffectList();

	[Header("Effects")]
	public EffectList m_shieldHitEffects = new EffectList();

	public EffectList m_shieldStart = new EffectList();

	public EffectList m_shieldStop = new EffectList();

	public EffectList m_shieldLowLoop = new EffectList();

	public float m_shieldLowLoopFuelStart;

	public ParticleSystem[] m_energyParticles;

	public ParticleSystem m_energyParticlesFlare;

	public Light[] m_coloredLights;

	private static readonly int s_emissiveProperty = Shader.PropertyToID("_EmissionColor");

	private ZNetView m_nview;

	private StringBuilder m_sb = new StringBuilder();

	private bool m_firstCheck;

	private int m_projectileMask;

	private float m_radius;

	private float m_radiusTarget;

	private float m_radiusSent;

	private float m_lastFuel;

	private float m_lastFuelSent;

	private float m_lastHitTime;

	private float m_lastHitTimeSent;

	private float m_attackCharge;

	private bool m_isPlacementGhost;

	private GameObject[] m_lowLoopInstances;

	private Gradient m_particleFlareGradient;

	private MeshRenderer[] m_meshRenderers;

	private MaterialPropertyBlock m_propertyBlock;

	private void Start()
	{
		if (Player.IsPlacementGhost(base.gameObject))
		{
			base.enabled = false;
			m_isPlacementGhost = true;
			return;
		}
		m_instances.Add(this);
		m_instanceChangeID++;
		m_nview = GetComponent<ZNetView>();
		if (m_nview == null)
		{
			m_nview = GetComponentInParent<ZNetView>();
		}
		if (!(m_nview == null) && m_nview.GetZDO() != null)
		{
			if ((bool)m_addFuelSwitch)
			{
				Switch addFuelSwitch = m_addFuelSwitch;
				addFuelSwitch.m_onUse = (Switch.Callback)Delegate.Combine(addFuelSwitch.m_onUse, new Switch.Callback(OnAddFuel));
				m_addFuelSwitch.m_onHover = OnHoverAddFuel;
			}
			m_nview.Register("RPC_AddFuel", RPC_AddFuel);
			m_nview.Register("RPC_Attack", RPC_Attack);
			m_nview.Register("RPC_HitNow", RPC_HitNow);
			m_projectileMask = LayerMask.GetMask();
			if (!m_shieldDomeEffect)
			{
				m_shieldDomeEffect = UnityEngine.Object.FindFirstObjectByType<ShieldDomeImageEffect>();
			}
			if (!m_enableAttack && m_fuelItems.Count == 0)
			{
				m_addFuelSwitch.gameObject.SetActive(value: false);
			}
			m_particleFlareGradient = new Gradient();
			m_particleFlareGradient.colorKeys = new GradientColorKey[1]
			{
				new GradientColorKey(Color.white, 0f)
			};
			m_particleFlareGradient.alphaKeys = new GradientAlphaKey[1]
			{
				new GradientAlphaKey(0f, 0f)
			};
			m_propertyBlock = new MaterialPropertyBlock();
			m_meshRenderers = m_enabledObject.GetComponentsInChildren<MeshRenderer>();
			InvokeRepeating("UpdateShield", 0f, 0.22f);
		}
	}

	public string GetHoverText()
	{
		if (!m_enableAttack)
		{
			return "";
		}
		if (m_attackCharge <= 0f)
		{
			return Localization.instance.Localize(m_name + "\n$piece_shieldgenerator_waiting");
		}
		if (m_attackCharge >= 1f)
		{
			return Localization.instance.Localize(m_name + "\n$piece_shieldgenerator_ready \n[<color=yellow><b>$KEY_Use</b></color>] $piece_shieldgenerator_use");
		}
		return Localization.instance.Localize(m_name + "\n$piece_shieldgenerator_charging " + (Terminal.m_showTests ? m_attackCharge.ToString("0.00") : ""));
	}

	public string GetHoverName()
	{
		return "";
	}

	public bool Interact(Humanoid user, bool repeat, bool alt)
	{
		if (!m_enableAttack)
		{
			return false;
		}
		if (repeat)
		{
			return false;
		}
		if (user == Player.m_localPlayer && m_attackCharge >= 1f)
		{
			m_nview.InvokeRPC("RPC_Attack");
		}
		return false;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void RPC_Attack(long sender)
	{
		m_attackCharge = 0f;
		if (!m_nview.IsOwner())
		{
			return;
		}
		SetFuel(0f);
		m_nview.GetZDO().Set(ZDOVars.s_startTime, 0L);
		UpdateAttackCharge();
		if ((bool)m_attackObject)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(m_attackObject, base.transform.position, base.transform.rotation);
			if (!m_damagePlayers)
			{
				gameObject.GetComponentInChildren<Aoe>()?.Setup(Player.m_localPlayer, Vector3.zero, 1f, null, null, null);
			}
		}
		m_attackEffects.Create(base.transform.position, base.transform.rotation);
		m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, base.transform);
	}

	private void RPC_HitNow(long sender)
	{
		m_lastHitTime = Time.time;
	}

	private void UpdateAttackCharge()
	{
		m_attackCharge = GetAttackCharge();
	}

	private float GetAttackCharge()
	{
		long @long = m_nview.GetZDO().GetLong(ZDOVars.s_startTime, 0L);
		if (@long <= 0)
		{
			return 0f;
		}
		DateTime time = ZNet.instance.GetTime();
		DateTime dateTime = new DateTime(@long);
		return (float)((time - dateTime).TotalSeconds / (double)m_attackChargeTime);
	}

	private void OnDestroy()
	{
		if (!m_isPlacementGhost)
		{
			m_shieldDomeEffect.RemoveShield(this);
			m_instances.Remove(this);
			m_instanceChangeID++;
			Character.SetupContinuousEffect(base.transform, base.transform.position, enabledEffect: false, m_shieldLowLoop, ref m_lowLoopInstances);
		}
	}

	private float GetFuel()
	{
		if (!m_nview.IsValid())
		{
			return 0f;
		}
		return m_nview.GetZDO().GetFloat(ZDOVars.s_fuel, m_defaultFuel);
	}

	private void SetFuel(float fuel)
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(ZDOVars.s_fuel, Mathf.Max(fuel, 0f));
		}
	}

	private bool OnAddFuel(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (GetFuel() > (float)(m_maxFuel - 1))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull");
			return false;
		}
		if (item != null)
		{
			bool flag = false;
			foreach (ItemDrop fuelItem in m_fuelItems)
			{
				if (item.m_shared.m_name == fuelItem.m_itemData.m_shared.m_name)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_wrongitem");
				return false;
			}
		}
		else
		{
			bool flag2 = false;
			foreach (ItemDrop fuelItem2 in m_fuelItems)
			{
				if (user.GetInventory().HaveItem(fuelItem2.m_itemData.m_shared.m_name))
				{
					item = fuelItem2.m_itemData;
					flag2 = true;
					break;
				}
			}
			if (!flag2)
			{
				user.Message(MessageHud.MessageType.Center, "$msg_donthaveany $piece_shieldgenerator_fuelname");
				return false;
			}
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + item.m_shared.m_name);
		user.GetInventory().RemoveItem(item.m_shared.m_name, 1);
		m_nview.InvokeRPC("RPC_AddFuel");
		return true;
	}

	private void RPC_AddFuel(long sender)
	{
		if (m_nview.IsOwner())
		{
			float fuel = GetFuel();
			SetFuel(fuel + 1f);
			m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, base.transform);
		}
	}

	private void Update()
	{
		if ((bool)m_shieldDome)
		{
			float num = m_shieldDome.transform.localScale.x + (m_radius - m_shieldDome.transform.localScale.x) * m_decreaseInertia;
			m_shieldDome.transform.localScale = new Vector3(num, num, num);
		}
		if (m_radiusTarget != m_radius)
		{
			if (!m_firstCheck)
			{
				m_firstCheck = true;
				m_radius = m_radiusTarget;
			}
			float num2 = m_radiusTarget - m_radius;
			m_radius += Mathf.Min(m_startStopSpeed * Time.deltaTime, Mathf.Abs(num2)) * (float)((num2 > 0f) ? 1 : (-1));
		}
		if (m_lastFuel != m_lastFuelSent || m_radius != m_radiusSent || m_lastHitTime != m_lastHitTimeSent)
		{
			m_shieldDomeEffect.SetShieldData(this, m_shieldDome.transform.position, m_radius, m_lastFuel, m_lastHitTime);
			m_lastFuelSent = m_lastFuel;
			m_radiusSent = m_radius;
			m_lastHitTimeSent = m_lastHitTime;
		}
	}

	private void UpdateShield()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		float fuel = GetFuel();
		m_enabledObject?.SetActive(fuel > 0f);
		m_disabledObject?.SetActive(fuel <= 0f);
		_ = m_radius;
		m_lastFuel = fuel / (float)m_maxFuel;
		float radiusTarget = m_radiusTarget;
		m_radiusTarget = m_minShieldRadius + m_lastFuel * (m_maxShieldRadius - m_minShieldRadius);
		Color domeColor = ShieldDomeImageEffect.GetDomeColor(m_lastFuel);
		ParticleSystem[] energyParticles = m_energyParticles;
		foreach (ParticleSystem obj in energyParticles)
		{
			ParticleSystem.MainModule main = obj.main;
			main.startColor = domeColor;
			ParticleSystem.EmissionModule emission = obj.emission;
			emission.rateOverTime = m_lastFuel * 5f;
		}
		m_energyParticlesFlare.customData.SetColor(ParticleSystemCustomData.Custom1, domeColor * Mathf.Pow(m_lastFuel, 0.5f));
		Light[] coloredLights = m_coloredLights;
		for (int i = 0; i < coloredLights.Length; i++)
		{
			coloredLights[i].color = domeColor;
		}
		m_propertyBlock.SetColor(s_emissiveProperty, domeColor * 2f);
		MeshRenderer[] meshRenderers = m_meshRenderers;
		for (int i = 0; i < meshRenderers.Length; i++)
		{
			meshRenderers[i].SetPropertyBlock(m_propertyBlock);
		}
		if (m_offWhenNoFuel)
		{
			if (fuel <= 0f)
			{
				m_radiusTarget = 0f;
				if (radiusTarget > 0f && m_nview.IsOwner())
				{
					m_shieldStop.Create(m_shieldDome.transform.position, m_shieldDome.transform.rotation);
				}
			}
			if (fuel > 0f && radiusTarget <= 0f && m_nview.IsOwner())
			{
				m_shieldStart.Create(m_shieldDome.transform.position, m_shieldDome.transform.rotation);
			}
		}
		if (m_shieldLowLoopFuelStart > 0f && m_nview.IsOwner())
		{
			Character.SetupContinuousEffect(base.transform, base.transform.position, m_lastFuel > 0f && m_lastFuel < m_shieldLowLoopFuelStart, m_shieldLowLoop, ref m_lowLoopInstances);
		}
		if (m_nview.IsOwner() && fuel >= (float)m_maxFuel && m_nview.GetZDO().GetLong(ZDOVars.s_startTime, 0L) <= 0)
		{
			DateTime time = ZNet.instance.GetTime();
			m_nview.GetZDO().Set(ZDOVars.s_startTime, time.Ticks);
		}
		UpdateAttackCharge();
	}

	public static void CheckProjectile(Projectile projectile)
	{
		if (!projectile.HasBeenOutsideShields)
		{
			int num = m_instances.Count;
			foreach (ShieldGenerator instance in m_instances)
			{
				if (Vector3.Distance(instance.m_shieldDome.transform.position, projectile.transform.position) < instance.m_radius || !CheckShield(instance))
				{
					num--;
				}
			}
			if (num == 0)
			{
				projectile.TriggerShieldsLeftFlag();
			}
		}
		else
		{
			foreach (ShieldGenerator instance2 in m_instances)
			{
				if (CheckShield(instance2) && Vector3.Distance(instance2.m_shieldDome.transform.position, projectile.m_startPoint) > instance2.m_radius && Vector3.Distance(instance2.m_shieldDome.transform.position, projectile.transform.position) < instance2.m_radius)
				{
					instance2.OnProjectileHit(projectile.gameObject);
				}
			}
		}
		ShieldCleanup();
	}

	public float GetFuelRatio()
	{
		return Mathf.Clamp01(m_lastFuel);
	}

	public static void CheckObjectInsideShield(Cinder zinder)
	{
		foreach (ShieldGenerator instance in m_instances)
		{
			if (CheckShield(instance) && Vector3.Distance(instance.m_shieldDome.transform.position, zinder.transform.position) < instance.m_radius)
			{
				instance.OnProjectileHit(zinder.gameObject);
			}
		}
		ShieldCleanup();
	}

	public static bool IsInsideShield(Vector3 point)
	{
		foreach (ShieldGenerator instance in m_instances)
		{
			if (CheckShield(instance) && (bool)instance && (bool)instance.m_shieldDome && Vector3.Distance(instance.m_shieldDome.transform.position, point) < instance.m_radius)
			{
				return true;
			}
		}
		ShieldCleanup();
		return false;
	}

	public static bool IsInsideMaxShield(Vector3 point)
	{
		foreach (ShieldGenerator instance in m_instances)
		{
			if (CheckShield(instance) && (bool)instance && (bool)instance.m_shieldDome && Vector3.Distance(instance.m_shieldDome.transform.position, point) < instance.m_maxShieldRadius)
			{
				return true;
			}
		}
		ShieldCleanup();
		return false;
	}

	public static bool IsInsideShieldCached(Vector3 point, ref int changeID)
	{
		if (Mathf.Abs(changeID) <= m_instanceChangeID)
		{
			if (!IsInsideMaxShield(point))
			{
				changeID = -m_instanceChangeID;
				return false;
			}
			changeID = m_instanceChangeID;
			if (IsInsideShield(point))
			{
				return true;
			}
		}
		if (changeID > 0 && IsInsideShield(point))
		{
			return true;
		}
		return false;
	}

	private static bool CheckShield(ShieldGenerator shield)
	{
		if (!shield || !shield.m_shieldDome)
		{
			s_cleanShields = true;
			return false;
		}
		return true;
	}

	private static void ShieldCleanup()
	{
		if (s_cleanShields)
		{
			int num = m_instances.RemoveAll((ShieldGenerator x) => x == null || x.m_shieldDome == null);
			if (num > 0)
			{
				ZLog.LogWarning($"Removed {num} invalid shield instances. Some shields may be broken?");
			}
			s_cleanShields = false;
		}
	}

	public void OnProjectileHit(GameObject obj)
	{
		Vector3 position = obj.transform.position;
		Projectile component = obj.GetComponent<Projectile>();
		component?.OnHit(null, position, water: false, -obj.transform.forward);
		ZNetScene.instance.Destroy(obj.gameObject);
		if (m_fuelPerDamage > 0f)
		{
			float num = m_fuelPerDamage * (component ? component.m_damage.GetTotalDamage() : 10f);
			SetFuel(GetFuel() - num);
		}
		m_nview.InvokeRPC(ZNetView.Everybody, "RPC_HitNow");
		m_shieldHitEffects.Create(position, Quaternion.LookRotation(base.transform.position.DirTo(position)));
		UpdateShield();
	}

	private string OnHoverAddFuel()
	{
		float fuel = GetFuel();
		return Localization.instance.Localize($"{m_name} ({Mathf.Ceil(fuel)}/{m_maxFuel})\n[<color=yellow><b>$KEY_Use</b></color>] {m_add}");
	}

	public static bool HasShields()
	{
		if (m_instances == null)
		{
			return false;
		}
		foreach (ShieldGenerator instance in m_instances)
		{
			if (instance.m_lastFuel > 0f)
			{
				return true;
			}
		}
		return false;
	}

	private static float SDFSmoothMin(float a, float b, float k)
	{
		k *= 6f;
		float num = Mathf.Max(k - Mathf.Abs(a - b), 0f) / k;
		return Mathf.Min(a, b) - num * num * num * k * (1f / 6f);
	}

	public static Vector3 DirectionToShieldWall(Vector3 pos)
	{
		float num = 0.001f;
		return -Vector3.Normalize(new Vector3(DistanceToShieldWall(pos + new Vector3(0f, num, 0f)), DistanceToShieldWall(pos + new Vector3(0f, 0f, num)), DistanceToShieldWall(pos + new Vector3(num, 0f, 0f))));
	}

	public static float DistanceToShieldWall(Vector3 pos)
	{
		float num = float.PositiveInfinity;
		foreach (ShieldGenerator instance in m_instances)
		{
			if (instance.m_lastFuel != 0f)
			{
				float b = Vector3.Distance(instance.transform.position, pos) - instance.m_radius;
				num = SDFSmoothMin(num, b, ShieldDomeImageEffect.Smoothing);
			}
		}
		return num;
	}

	public static Vector3 GetClosestShieldPoint(Vector3 pos)
	{
		float num = DistanceToShieldWall(pos);
		Vector3 vector = DirectionToShieldWall(pos);
		return pos + vector * num;
	}

	public static ShieldGenerator GetClosestShieldGenerator(Vector3 pos, bool ignoreRadius = false)
	{
		ShieldGenerator result = null;
		float num = float.PositiveInfinity;
		foreach (ShieldGenerator instance in m_instances)
		{
			float num2 = (ignoreRadius ? 0f : instance.m_radius);
			float num3 = Mathf.Abs(Vector3.Distance(instance.transform.position, pos) - num2);
			if (num3 < num)
			{
				num = num3;
				result = instance;
			}
		}
		return result;
	}
}
