using System;
using System.Collections.Generic;
using UnityEngine;

public class TriggerTracker : MonoBehaviour
{
	public Action m_changed;

	private List<Collider> m_colliders = new List<Collider>();

	private void OnTriggerEnter(Collider other)
	{
		if (!m_colliders.Contains(other))
		{
			m_colliders.Add(other);
		}
		m_changed();
	}

	private void OnTriggerExit(Collider other)
	{
		m_colliders.Remove(other);
		m_changed();
	}

	public bool IsColliding()
	{
		return m_colliders.Count > 0;
	}

	public List<Collider> GetColliders()
	{
		return m_colliders;
	}
}
