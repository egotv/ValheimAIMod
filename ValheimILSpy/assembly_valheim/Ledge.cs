using System;
using System.Collections.Generic;
using UnityEngine;

public class Ledge : MonoBehaviour
{
	public Collider m_collider;

	public TriggerTracker m_above;

	private void Awake()
	{
		if (GetComponent<ZNetView>().GetZDO() != null)
		{
			m_collider.enabled = true;
			TriggerTracker above = m_above;
			above.m_changed = (Action)Delegate.Combine(above.m_changed, new Action(Changed));
		}
	}

	private void Changed()
	{
		List<Collider> colliders = m_above.GetColliders();
		if (colliders.Count == 0)
		{
			m_collider.enabled = true;
			return;
		}
		bool flag = false;
		foreach (Collider item in colliders)
		{
			if (item.transform.position.y > base.transform.position.y)
			{
				flag = true;
				break;
			}
		}
		m_collider.enabled = flag;
	}
}
