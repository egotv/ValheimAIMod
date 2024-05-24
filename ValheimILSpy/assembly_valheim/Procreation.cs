using System;
using UnityEngine;

public class Procreation : MonoBehaviour
{
	public float m_updateInterval = 10f;

	public float m_totalCheckRange = 10f;

	public int m_maxCreatures = 4;

	public float m_partnerCheckRange = 3f;

	public float m_pregnancyChance = 0.5f;

	public float m_pregnancyDuration = 10f;

	public int m_requiredLovePoints = 4;

	public GameObject m_offspring;

	public int m_minOffspringLevel;

	public float m_spawnOffset = 2f;

	public GameObject m_seperatePartner;

	public GameObject m_noPartnerOffspring;

	public EffectList m_birthEffects = new EffectList();

	public EffectList m_loveEffects = new EffectList();

	private GameObject m_myPrefab;

	private GameObject m_offspringPrefab;

	private ZNetView m_nview;

	private BaseAI m_baseAI;

	private Character m_character;

	private Tameable m_tameable;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_baseAI = GetComponent<BaseAI>();
		m_character = GetComponent<Character>();
		m_tameable = GetComponent<Tameable>();
		InvokeRepeating("Procreate", UnityEngine.Random.Range(m_updateInterval, m_updateInterval + m_updateInterval * 0.5f), m_updateInterval);
	}

	private void Procreate()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner() || !m_character.IsTamed())
		{
			return;
		}
		if (m_offspringPrefab == null)
		{
			string prefabName = Utils.GetPrefabName(m_offspring);
			m_offspringPrefab = ZNetScene.instance.GetPrefab(prefabName);
			int prefab = m_nview.GetZDO().GetPrefab();
			m_myPrefab = ZNetScene.instance.GetPrefab(prefab);
		}
		if (IsPregnant())
		{
			if (!IsDue())
			{
				return;
			}
			ResetPregnancy();
			GameObject original = m_offspringPrefab;
			if ((bool)m_noPartnerOffspring)
			{
				int nrOfInstances = SpawnSystem.GetNrOfInstances(m_seperatePartner ? m_seperatePartner : m_myPrefab, base.transform.position, m_partnerCheckRange, eventCreaturesOnly: false, procreationOnly: true);
				if ((!m_seperatePartner && nrOfInstances < 2) || ((bool)m_seperatePartner && nrOfInstances < 1))
				{
					original = m_noPartnerOffspring;
				}
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(original, base.transform.position - base.transform.forward * m_spawnOffset, Quaternion.LookRotation(-base.transform.forward, Vector3.up));
			Character component = gameObject.GetComponent<Character>();
			if ((bool)component)
			{
				component.SetTamed(m_character.IsTamed());
				component.SetLevel(Mathf.Max(m_minOffspringLevel, m_character.GetLevel()));
			}
			else
			{
				gameObject.GetComponent<ItemDrop>()?.SetQuality(Mathf.Max(m_minOffspringLevel, m_character.GetLevel()));
			}
			m_birthEffects.Create(gameObject.transform.position, Quaternion.identity);
		}
		else
		{
			if (UnityEngine.Random.value <= m_pregnancyChance || m_baseAI.IsAlerted() || m_tameable.IsHungry())
			{
				return;
			}
			int nrOfInstances2 = SpawnSystem.GetNrOfInstances(m_myPrefab, base.transform.position, m_totalCheckRange);
			int nrOfInstances3 = SpawnSystem.GetNrOfInstances(m_offspringPrefab, base.transform.position, m_totalCheckRange);
			if (nrOfInstances2 + nrOfInstances3 >= m_maxCreatures)
			{
				return;
			}
			int nrOfInstances4 = SpawnSystem.GetNrOfInstances(m_seperatePartner ? m_seperatePartner : m_myPrefab, base.transform.position, m_partnerCheckRange, eventCreaturesOnly: false, procreationOnly: true);
			if ((bool)m_noPartnerOffspring || (((bool)m_seperatePartner || nrOfInstances4 >= 2) && (!m_seperatePartner || nrOfInstances4 >= 1)))
			{
				if (nrOfInstances4 > 0)
				{
					m_loveEffects.Create(base.transform.position, base.transform.rotation);
				}
				int @int = m_nview.GetZDO().GetInt(ZDOVars.s_lovePoints);
				@int++;
				m_nview.GetZDO().Set(ZDOVars.s_lovePoints, @int);
				if (@int >= m_requiredLovePoints)
				{
					m_nview.GetZDO().Set(ZDOVars.s_lovePoints, 0);
					MakePregnant();
				}
			}
		}
	}

	public bool ReadyForProcreation()
	{
		if (m_character.IsTamed() && !IsPregnant())
		{
			return !m_tameable.IsHungry();
		}
		return false;
	}

	private void MakePregnant()
	{
		m_nview.GetZDO().Set(ZDOVars.s_pregnant, ZNet.instance.GetTime().Ticks);
	}

	private void ResetPregnancy()
	{
		m_nview.GetZDO().Set(ZDOVars.s_pregnant, 0L);
	}

	private bool IsDue()
	{
		long @long = m_nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L);
		if (@long == 0L)
		{
			return false;
		}
		DateTime dateTime = new DateTime(@long);
		return (ZNet.instance.GetTime() - dateTime).TotalSeconds > (double)m_pregnancyDuration;
	}

	private bool IsPregnant()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetLong(ZDOVars.s_pregnant, 0L) != 0;
	}
}
