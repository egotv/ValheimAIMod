using System.Collections.Generic;
using UnityEngine;

public class StaticTarget : MonoBehaviour
{
	[Header("Static target")]
	public bool m_primaryTarget;

	public bool m_randomTarget = true;

	private List<Collider> m_colliders;

	private Vector3 m_localCenter;

	private bool m_haveCenter;

	public virtual bool IsPriorityTarget()
	{
		return m_primaryTarget;
	}

	public virtual bool IsRandomTarget()
	{
		return m_randomTarget;
	}

	public Vector3 GetCenter()
	{
		if (!m_haveCenter)
		{
			List<Collider> allColliders = GetAllColliders();
			m_localCenter = Vector3.zero;
			foreach (Collider item in allColliders)
			{
				if ((bool)item)
				{
					m_localCenter += item.bounds.center;
				}
			}
			m_localCenter /= (float)m_colliders.Count;
			m_localCenter = base.transform.InverseTransformPoint(m_localCenter);
			m_haveCenter = true;
		}
		return base.transform.TransformPoint(m_localCenter);
	}

	public List<Collider> GetAllColliders()
	{
		if (m_colliders == null)
		{
			Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
			m_colliders = new List<Collider>();
			m_colliders.Capacity = componentsInChildren.Length;
			Collider[] array = componentsInChildren;
			foreach (Collider collider in array)
			{
				if (collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger)
				{
					m_colliders.Add(collider);
				}
			}
		}
		return m_colliders;
	}

	public Vector3 FindClosestPoint(Vector3 point)
	{
		List<Collider> allColliders = GetAllColliders();
		if (allColliders.Count == 0)
		{
			return base.transform.position;
		}
		float num = 9999999f;
		Vector3 result = Vector3.zero;
		foreach (Collider item in allColliders)
		{
			if ((bool)item)
			{
				MeshCollider meshCollider = item as MeshCollider;
				Vector3 vector = (((bool)meshCollider && !meshCollider.convex) ? item.ClosestPointOnBounds(point) : item.ClosestPoint(point));
				float num2 = Vector3.Distance(point, vector);
				if (num2 < num)
				{
					result = vector;
					num = num2;
				}
			}
		}
		return result;
	}
}
