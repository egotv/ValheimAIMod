using System;
using UnityEngine;

public class Gibber : MonoBehaviour
{
	[Serializable]
	public class GibbData
	{
		public GameObject m_object;

		public float m_chanceToSpawn = 1f;
	}

	public EffectList m_punchEffector = new EffectList();

	public GameObject m_gibHitEffect;

	public GameObject m_gibDestroyEffect;

	public float m_gibHitDestroyChance;

	public GibbData[] m_gibbs = new GibbData[0];

	public float m_minVel = 10f;

	public float m_maxVel = 20f;

	public float m_maxRotVel = 20f;

	public float m_impactDirectionMix = 0.5f;

	public float m_timeout = 5f;

	public float m_delay;

	[Range(0f, 1f)]
	public float m_chanceToRemoveGib;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
	}

	private void Start()
	{
		Vector3 vector = base.transform.position;
		Vector3 vector2 = Vector3.zero;
		if ((bool)m_nview && m_nview.IsValid())
		{
			vector = m_nview.GetZDO().GetVec3(ZDOVars.s_hitPoint, vector);
			vector2 = m_nview.GetZDO().GetVec3(ZDOVars.s_hitDir, vector2);
		}
		if (m_delay > 0f)
		{
			Invoke("Explode", m_delay);
		}
		else
		{
			Explode(vector, vector2);
		}
	}

	public void Setup(Vector3 hitPoint, Vector3 hitDir)
	{
		if ((bool)m_nview && m_nview.IsValid())
		{
			m_nview.GetZDO().Set(ZDOVars.s_hitPoint, hitPoint);
			m_nview.GetZDO().Set(ZDOVars.s_hitDir, hitDir);
		}
	}

	private void DestroyAll()
	{
		if ((bool)m_nview)
		{
			if (!m_nview.GetZDO().HasOwner())
			{
				m_nview.ClaimOwnership();
			}
			if (m_nview.IsOwner())
			{
				ZNetScene.instance.Destroy(base.gameObject);
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void CreateBodies()
	{
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			GameObject gameObject = componentsInChildren[i].gameObject;
			if (m_chanceToRemoveGib > 0f && UnityEngine.Random.value < m_chanceToRemoveGib)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
			else if (!gameObject.GetComponent<Rigidbody>())
			{
				gameObject.AddComponent<BoxCollider>();
				gameObject.AddComponent<Rigidbody>().maxDepenetrationVelocity = 2f;
				TimedDestruction timedDestruction = gameObject.AddComponent<TimedDestruction>();
				timedDestruction.m_timeout = UnityEngine.Random.Range(m_timeout / 2f, m_timeout);
				timedDestruction.Trigger();
			}
		}
	}

	private void Explode()
	{
		Explode(Vector3.zero, Vector3.zero);
	}

	private void Explode(Vector3 hitPoint, Vector3 hitDir)
	{
		InvokeRepeating("DestroyAll", m_timeout, 1f);
		float t = (((double)hitDir.magnitude > 0.01) ? m_impactDirectionMix : 0f);
		CreateBodies();
		Rigidbody[] componentsInChildren = base.gameObject.GetComponentsInChildren<Rigidbody>();
		if (componentsInChildren.Length == 0)
		{
			return;
		}
		Vector3 zero = Vector3.zero;
		int num = 0;
		Rigidbody[] array = componentsInChildren;
		foreach (Rigidbody rigidbody in array)
		{
			zero += rigidbody.worldCenterOfMass;
			num++;
		}
		zero /= (float)num;
		array = componentsInChildren;
		foreach (Rigidbody obj in array)
		{
			float num2 = UnityEngine.Random.Range(m_minVel, m_maxVel);
			Vector3 vector = Vector3.Lerp(Vector3.Normalize(obj.worldCenterOfMass - zero), hitDir, t);
			obj.velocity = vector * num2;
			obj.angularVelocity = new Vector3(UnityEngine.Random.Range(0f - m_maxRotVel, m_maxRotVel), UnityEngine.Random.Range(0f - m_maxRotVel, m_maxRotVel), UnityEngine.Random.Range(0f - m_maxRotVel, m_maxRotVel));
		}
		GibbData[] gibbs = m_gibbs;
		foreach (GibbData gibbData in gibbs)
		{
			if ((bool)gibbData.m_object && gibbData.m_chanceToSpawn < 1f && UnityEngine.Random.value > gibbData.m_chanceToSpawn)
			{
				UnityEngine.Object.Destroy(gibbData.m_object);
			}
		}
		if ((double)hitDir.magnitude > 0.01)
		{
			Quaternion baseRot = Quaternion.LookRotation(hitDir);
			m_punchEffector.Create(hitPoint, baseRot);
		}
	}
}
