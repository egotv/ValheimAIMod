using System.Collections.Generic;
using UnityEngine;

public class SlowUpdate : MonoBehaviour
{
	private static List<SlowUpdate> m_allInstances = new List<SlowUpdate>();

	private int m_myIndex = -1;

	public virtual void Awake()
	{
		m_allInstances.Add(this);
		m_myIndex = m_allInstances.Count - 1;
	}

	public virtual void OnDestroy()
	{
		if (m_myIndex != -1)
		{
			m_allInstances[m_myIndex] = m_allInstances[m_allInstances.Count - 1];
			m_allInstances[m_myIndex].m_myIndex = m_myIndex;
			m_allInstances.RemoveAt(m_allInstances.Count - 1);
		}
	}

	public virtual void SUpdate()
	{
	}

	public static List<SlowUpdate> GetAllInstaces()
	{
		return m_allInstances;
	}
}
