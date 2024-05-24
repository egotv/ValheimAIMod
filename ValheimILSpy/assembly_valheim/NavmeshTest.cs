using System.Collections.Generic;
using UnityEngine;

public class NavmeshTest : MonoBehaviour
{
	public Transform m_target;

	public Pathfinding.AgentType m_agentType = Pathfinding.AgentType.Humanoid;

	public bool m_cleanPath = true;

	private List<Vector3> m_path = new List<Vector3>();

	private bool m_havePath;

	private void Awake()
	{
	}

	private void Update()
	{
		if (Pathfinding.instance.GetPath(base.transform.position, m_target.position, m_path, m_agentType, requireFullPath: false, m_cleanPath))
		{
			m_havePath = true;
		}
		else
		{
			m_havePath = false;
		}
	}

	private void OnDrawGizmos()
	{
		if (m_target == null)
		{
			return;
		}
		if (m_havePath)
		{
			Gizmos.color = Color.yellow;
			for (int i = 0; i < m_path.Count - 1; i++)
			{
				Vector3 vector = m_path[i];
				Gizmos.DrawLine(to: m_path[i + 1] + Vector3.up * 0.2f, from: vector + Vector3.up * 0.2f);
			}
			foreach (Vector3 item in m_path)
			{
				Gizmos.DrawSphere(item + Vector3.up * 0.2f, 0.1f);
			}
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(base.transform.position, 0.3f);
			Gizmos.DrawSphere(m_target.position, 0.3f);
		}
		else
		{
			Gizmos.color = Color.red;
			Gizmos.DrawLine(base.transform.position + Vector3.up * 0.2f, m_target.position + Vector3.up * 0.2f);
			Gizmos.DrawSphere(base.transform.position, 0.3f);
			Gizmos.DrawSphere(m_target.position, 0.3f);
		}
	}
}
