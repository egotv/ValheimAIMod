using System.Collections.Generic;
using UnityEngine;

public class Mister : MonoBehaviour
{
	public float m_radius = 50f;

	public float m_height = 10f;

	private float m_tempDistance;

	private static List<Mister> m_instances = new List<Mister>();

	private void Awake()
	{
	}

	private void OnEnable()
	{
		m_instances.Add(this);
	}

	private void OnDisable()
	{
		m_instances.Remove(this);
	}

	public static List<Mister> GetMisters()
	{
		return m_instances;
	}

	public static List<Mister> GetDemistersSorted(Vector3 refPoint)
	{
		foreach (Mister instance in m_instances)
		{
			instance.m_tempDistance = Vector3.Distance(instance.transform.position, refPoint);
		}
		m_instances.Sort((Mister a, Mister b) => a.m_tempDistance.CompareTo(b.m_tempDistance));
		return m_instances;
	}

	public static Mister FindMister(Vector3 p)
	{
		foreach (Mister instance in m_instances)
		{
			if (Vector3.Distance(instance.transform.position, p) < instance.m_radius)
			{
				return instance;
			}
		}
		return null;
	}

	public static bool InsideMister(Vector3 p, float radius = 0f)
	{
		foreach (Mister instance in m_instances)
		{
			if (Vector3.Distance(instance.transform.position, p) < instance.m_radius + radius && p.y - radius < instance.transform.position.y + instance.m_height)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsCompletelyInsideOtherMister(float thickness)
	{
		Vector3 position = base.transform.position;
		foreach (Mister instance in m_instances)
		{
			if (!(instance == this) && Vector3.Distance(position, instance.transform.position) + m_radius + thickness < instance.m_radius && position.y + m_height < instance.transform.position.y + instance.m_height)
			{
				return true;
			}
		}
		return false;
	}

	public bool Inside(Vector3 p, float radius)
	{
		if (Vector3.Distance(p, base.transform.position) < radius)
		{
			return p.y - radius < base.transform.position.y + m_height;
		}
		return false;
	}

	public static bool IsInsideOtherMister(Vector3 p, Mister ignore)
	{
		foreach (Mister instance in m_instances)
		{
			if (!(instance == ignore) && Vector3.Distance(p, instance.transform.position) < instance.m_radius && p.y < instance.transform.position.y + instance.m_height)
			{
				return true;
			}
		}
		return false;
	}

	private void OnDrawGizmosSelected()
	{
	}
}
