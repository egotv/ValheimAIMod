using System;
using System.Collections.Generic;
using UnityEngine;

public class Demister : MonoBehaviour
{
	public float m_disableForcefieldDelay;

	[NonSerialized]
	public ParticleSystemForceField m_forceField;

	private Vector3 m_lastUpdatePosition;

	private static List<Demister> m_instances = new List<Demister>();

	private void Awake()
	{
		m_forceField = GetComponent<ParticleSystemForceField>();
		m_lastUpdatePosition = base.transform.position;
		if (m_disableForcefieldDelay > 0f)
		{
			Invoke("DisableForcefield", m_disableForcefieldDelay);
		}
	}

	private void OnEnable()
	{
		m_instances.Add(this);
	}

	private void OnDisable()
	{
		m_instances.Remove(this);
	}

	private void DisableForcefield()
	{
		m_forceField.enabled = false;
	}

	public float GetMovedDistance()
	{
		Vector3 position = base.transform.position;
		if (position == m_lastUpdatePosition)
		{
			return 0f;
		}
		float a = Vector3.Distance(position, m_lastUpdatePosition);
		m_lastUpdatePosition = position;
		return Mathf.Min(a, 10f);
	}

	public static List<Demister> GetDemisters()
	{
		return m_instances;
	}
}
