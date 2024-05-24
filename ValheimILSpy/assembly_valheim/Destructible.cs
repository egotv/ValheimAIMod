using System;
using System.Collections.Generic;
using UnityEngine;

public class Destructible : MonoBehaviour, IDestructible
{
	public Action m_onDestroyed;

	public Action m_onDamaged;

	[Header("Destruction")]
	public DestructibleType m_destructibleType = DestructibleType.Default;

	public float m_health = 1f;

	public HitData.DamageModifiers m_damages;

	public float m_minDamageTreshold;

	public int m_minToolTier;

	public float m_hitNoise;

	public float m_destroyNoise;

	public bool m_triggerPrivateArea;

	public float m_ttl;

	public GameObject m_spawnWhenDestroyed;

	[Header("Effects")]
	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public bool m_autoCreateFragments;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private bool m_firstFrame = true;

	private bool m_destroyed;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		if ((bool)m_nview && m_nview.GetZDO() != null)
		{
			m_nview.Register<HitData>("RPC_Damage", RPC_Damage);
			if (m_autoCreateFragments)
			{
				m_nview.Register("RPC_CreateFragments", RPC_CreateFragments);
			}
			if (m_ttl > 0f)
			{
				InvokeRepeating("DestroyNow", m_ttl, 1f);
			}
		}
	}

	private void Start()
	{
		m_firstFrame = false;
	}

	public GameObject GetParentObject()
	{
		return null;
	}

	public DestructibleType GetDestructibleType()
	{
		return m_destructibleType;
	}

	public void Damage(HitData hit)
	{
		if (!m_firstFrame && m_nview.IsValid())
		{
			m_nview.InvokeRPC("RPC_Damage", hit);
		}
	}

	private void RPC_Damage(long sender, HitData hit)
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner() || m_destroyed)
		{
			return;
		}
		float @float = m_nview.GetZDO().GetFloat(ZDOVars.s_health, m_health + (float)Game.m_worldLevel * m_health * Game.instance.m_worldLevelMineHPMultiplier);
		if (@float <= 0f || m_destroyed)
		{
			return;
		}
		hit.ApplyResistance(m_damages, out var significantModifier);
		float totalDamage = hit.GetTotalDamage();
		if ((bool)m_body)
		{
			m_body.AddForceAtPosition(hit.m_dir * hit.m_pushForce, hit.m_point, ForceMode.Impulse);
		}
		if (!hit.CheckToolTier(m_minToolTier))
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, hit.m_point, 0f);
			return;
		}
		DamageText.instance.ShowText(significantModifier, hit.m_point, totalDamage);
		if (totalDamage <= 0f)
		{
			return;
		}
		@float -= totalDamage;
		m_nview.GetZDO().Set(ZDOVars.s_health, @float);
		if (m_triggerPrivateArea)
		{
			Character attacker = hit.GetAttacker();
			if ((bool)attacker)
			{
				bool destroyed = @float <= 0f;
				PrivateArea.OnObjectDamaged(base.transform.position, attacker, destroyed);
			}
		}
		m_hitEffect.Create(hit.m_point, Quaternion.identity, base.transform);
		if (m_onDamaged != null)
		{
			m_onDamaged();
		}
		if (m_hitNoise > 0f && hit.m_hitType != HitData.HitType.CinderFire)
		{
			Player closestPlayer = Player.GetClosestPlayer(hit.m_point, 10f);
			if ((bool)closestPlayer)
			{
				closestPlayer.AddNoise(m_hitNoise);
			}
		}
		if (@float <= 0f)
		{
			Destroy(hit);
		}
	}

	public void DestroyNow()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			Destroy();
		}
	}

	public void Destroy(HitData hit = null)
	{
		Vector3 hitPoint = hit?.m_point ?? Vector3.zero;
		Vector3 hitDir = hit?.m_dir ?? Vector3.zero;
		CreateDestructionEffects(hitPoint, hitDir);
		if (m_destroyNoise > 0f && (hit == null || hit.m_hitType != HitData.HitType.CinderFire))
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 10f);
			if ((bool)closestPlayer)
			{
				closestPlayer.AddNoise(m_destroyNoise);
			}
		}
		if ((bool)m_spawnWhenDestroyed)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(m_spawnWhenDestroyed, base.transform.position, base.transform.rotation);
			gameObject.GetComponent<ZNetView>().SetLocalScale(base.transform.localScale);
			gameObject.GetComponent<Gibber>()?.Setup(hitPoint, hitDir);
			if (hit != null)
			{
				gameObject.GetComponent<MineRock5>()?.Damage(hit);
			}
		}
		if (m_onDestroyed != null)
		{
			m_onDestroyed();
		}
		ZNetScene.instance.Destroy(base.gameObject);
		m_destroyed = true;
	}

	private void CreateDestructionEffects(Vector3 hitPoint, Vector3 hitDir)
	{
		GameObject[] array = m_destroyedEffect.Create(base.transform.position, base.transform.rotation, base.transform);
		for (int i = 0; i < array.Length; i++)
		{
			Gibber component = array[i].GetComponent<Gibber>();
			if ((bool)component)
			{
				component.Setup(hitPoint, hitDir);
			}
		}
		if (m_autoCreateFragments)
		{
			m_nview.InvokeRPC(ZNetView.Everybody, "RPC_CreateFragments");
		}
	}

	private void RPC_CreateFragments(long peer)
	{
		CreateFragments(base.gameObject);
	}

	public static void CreateFragments(GameObject rootObject, bool visibleOnly = true)
	{
		MeshRenderer[] componentsInChildren = rootObject.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
		int layer = LayerMask.NameToLayer("effect");
		List<Rigidbody> list = new List<Rigidbody>();
		MeshRenderer[] array = componentsInChildren;
		foreach (MeshRenderer meshRenderer in array)
		{
			if (!meshRenderer.gameObject.activeInHierarchy || (visibleOnly && !meshRenderer.isVisible))
			{
				continue;
			}
			MeshFilter component = meshRenderer.gameObject.GetComponent<MeshFilter>();
			if (!(component == null))
			{
				if (component.sharedMesh == null)
				{
					ZLog.Log("Meshfilter missing mesh " + component.gameObject.name);
					continue;
				}
				GameObject obj = new GameObject();
				obj.layer = layer;
				obj.transform.position = component.gameObject.transform.position;
				obj.transform.rotation = component.gameObject.transform.rotation;
				obj.transform.localScale = component.gameObject.transform.lossyScale * 0.9f;
				obj.AddComponent<MeshFilter>().sharedMesh = component.sharedMesh;
				MeshRenderer meshRenderer2 = obj.AddComponent<MeshRenderer>();
				meshRenderer2.sharedMaterials = meshRenderer.sharedMaterials;
				meshRenderer2.material.SetFloat("_RippleDistance", 0f);
				meshRenderer2.material.SetFloat("_ValueNoise", 0f);
				Rigidbody item = obj.AddComponent<Rigidbody>();
				obj.AddComponent<BoxCollider>();
				list.Add(item);
				obj.AddComponent<TimedDestruction>().Trigger(UnityEngine.Random.Range(2, 4));
			}
		}
		if (list.Count <= 0)
		{
			return;
		}
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (Rigidbody item2 in list)
		{
			zero += item2.worldCenterOfMass;
			num++;
		}
		zero /= (float)num;
		foreach (Rigidbody item3 in list)
		{
			Vector3 force = (item3.worldCenterOfMass - zero).normalized * 4f;
			force += UnityEngine.Random.onUnitSphere * 1f;
			item3.AddForce(force, ForceMode.VelocityChange);
		}
	}
}
