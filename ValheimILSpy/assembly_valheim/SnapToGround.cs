using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SnapToGround : MonoBehaviour
{
	public float m_offset;

	private static List<SnapToGround> m_allSnappers = new List<SnapToGround>();

	private bool m_inList;

	private void Awake()
	{
		m_allSnappers.Add(this);
		m_inList = true;
	}

	private void OnDestroy()
	{
		if (m_inList)
		{
			m_allSnappers.Remove(this);
			m_inList = false;
		}
	}

	public void Snap()
	{
		if (!(ZoneSystem.instance == null))
		{
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			Vector3 position = base.transform.position;
			position.y = groundHeight + m_offset;
			base.transform.position = position;
			ZNetView component = GetComponent<ZNetView>();
			if (component != null && component.IsOwner())
			{
				component.GetZDO().SetPosition(position);
			}
		}
	}

	public bool HaveUnsnapped()
	{
		return m_allSnappers.Count > 0;
	}

	public static void SnappAll()
	{
		if (m_allSnappers.Count == 0)
		{
			return;
		}
		Heightmap.ForceGenerateAll();
		foreach (SnapToGround allSnapper in m_allSnappers)
		{
			allSnapper.Snap();
			allSnapper.m_inList = false;
		}
		m_allSnappers.Clear();
	}
}
