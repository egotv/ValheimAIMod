using System;
using System.Collections.Generic;
using UnityEngine;

public class Ragdoll : MonoBehaviour
{
	public float m_velMultiplier = 1f;

	public float m_ttl;

	public Renderer m_mainModel;

	public EffectList m_removeEffect = new EffectList();

	public Action<Vector3> m_onDestroyed;

	public bool m_float;

	public float m_floatOffset = -0.1f;

	public bool m_dropItems = true;

	public GameObject m_lootSpawnJoint;

	private const float m_floatForce = 20f;

	private const float m_damping = 0.05f;

	private ZNetView m_nview;

	private Rigidbody[] m_bodies;

	private const float m_dropOffset = 0.75f;

	private const float m_dropArea = 0.5f;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_bodies = GetComponentsInChildren<Rigidbody>();
		Invoke("RemoveInitVel", 2f);
		if ((bool)m_mainModel)
		{
			float @float = m_nview.GetZDO().GetFloat(ZDOVars.s_hue);
			float float2 = m_nview.GetZDO().GetFloat(ZDOVars.s_saturation);
			float float3 = m_nview.GetZDO().GetFloat(ZDOVars.s_value);
			m_mainModel.material.SetFloat("_Hue", @float);
			m_mainModel.material.SetFloat("_Saturation", float2);
			m_mainModel.material.SetFloat("_Value", float3);
		}
		InvokeRepeating("DestroyNow", m_ttl, 1f);
	}

	public Vector3 GetAverageBodyPosition()
	{
		if (m_bodies.Length == 0)
		{
			return base.transform.position;
		}
		Vector3 zero = Vector3.zero;
		Rigidbody[] bodies = m_bodies;
		foreach (Rigidbody rigidbody in bodies)
		{
			zero += rigidbody.position;
		}
		return zero / m_bodies.Length;
	}

	private void DestroyNow()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			Vector3 vector = GetAverageBodyPosition();
			_ = Quaternion.identity;
			m_removeEffect.Create(vector, Quaternion.identity);
			if (m_lootSpawnJoint != null)
			{
				vector = m_lootSpawnJoint.transform.position;
			}
			SpawnLoot(vector);
			ZNetScene.instance.Destroy(base.gameObject);
		}
	}

	private void RemoveInitVel()
	{
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(ZDOVars.s_initVel, Vector3.zero);
		}
	}

	private void Start()
	{
		Vector3 vec = m_nview.GetZDO().GetVec3(ZDOVars.s_initVel, Vector3.zero);
		if (vec != Vector3.zero)
		{
			vec.y = Mathf.Min(vec.y, 4f);
			Rigidbody[] bodies = m_bodies;
			for (int i = 0; i < bodies.Length; i++)
			{
				bodies[i].velocity = vec * UnityEngine.Random.value;
			}
		}
	}

	public void Setup(Vector3 velocity, float hue, float saturation, float value, CharacterDrop characterDrop)
	{
		velocity.x *= m_velMultiplier;
		velocity.z *= m_velMultiplier;
		m_nview.GetZDO().Set(ZDOVars.s_initVel, velocity);
		m_nview.GetZDO().Set(ZDOVars.s_hue, hue);
		m_nview.GetZDO().Set(ZDOVars.s_saturation, saturation);
		m_nview.GetZDO().Set(ZDOVars.s_value, value);
		if ((bool)m_mainModel)
		{
			m_mainModel.material.SetFloat("_Hue", hue);
			m_mainModel.material.SetFloat("_Saturation", saturation);
			m_mainModel.material.SetFloat("_Value", value);
		}
		if ((bool)characterDrop && m_dropItems)
		{
			SaveLootList(characterDrop);
		}
	}

	private void SaveLootList(CharacterDrop characterDrop)
	{
		List<KeyValuePair<GameObject, int>> list = characterDrop.GenerateDropList();
		if (list.Count > 0)
		{
			ZDO zDO = m_nview.GetZDO();
			zDO.Set(ZDOVars.s_drops, list.Count);
			for (int i = 0; i < list.Count; i++)
			{
				KeyValuePair<GameObject, int> keyValuePair = list[i];
				int prefabHash = ZNetScene.instance.GetPrefabHash(keyValuePair.Key);
				zDO.Set("drop_hash" + i, prefabHash);
				zDO.Set("drop_amount" + i, keyValuePair.Value);
			}
		}
	}

	private void SpawnLoot(Vector3 center)
	{
		ZDO zDO = m_nview.GetZDO();
		int @int = zDO.GetInt(ZDOVars.s_drops);
		if (@int <= 0)
		{
			return;
		}
		List<KeyValuePair<GameObject, int>> list = new List<KeyValuePair<GameObject, int>>();
		for (int i = 0; i < @int; i++)
		{
			int int2 = zDO.GetInt("drop_hash" + i);
			int int3 = zDO.GetInt("drop_amount" + i);
			GameObject prefab = ZNetScene.instance.GetPrefab(int2);
			if (prefab == null)
			{
				ZLog.LogWarning("Ragdoll: Missing prefab:" + int2 + " when dropping loot");
			}
			else
			{
				list.Add(new KeyValuePair<GameObject, int>(prefab, int3));
			}
		}
		CharacterDrop.DropItems(list, center + Vector3.up * 0.75f, 0.5f);
	}

	private void FixedUpdate()
	{
		if (m_float)
		{
			UpdateFloating(Time.fixedDeltaTime);
		}
	}

	private void UpdateFloating(float dt)
	{
		Rigidbody[] bodies = m_bodies;
		foreach (Rigidbody rigidbody in bodies)
		{
			Vector3 worldCenterOfMass = rigidbody.worldCenterOfMass;
			worldCenterOfMass.y += m_floatOffset;
			float liquidLevel = Floating.GetLiquidLevel(worldCenterOfMass);
			if (worldCenterOfMass.y < liquidLevel)
			{
				float num = (liquidLevel - worldCenterOfMass.y) / 0.5f;
				Vector3 vector = Vector3.up * 20f * num;
				rigidbody.AddForce(vector * dt, ForceMode.VelocityChange);
				rigidbody.velocity -= rigidbody.velocity * 0.05f * num;
			}
		}
	}
}
