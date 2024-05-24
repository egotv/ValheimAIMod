using UnityEngine;

public class Cover
{
	private const float m_coverRayDistance = 30f;

	private static int m_coverRayMask = 0;

	private static Vector3[] m_coverRays = null;

	private static RaycastHit[] m_raycastHits = new RaycastHit[128];

	public static void GetCoverForPoint(Vector3 startPos, out float coverPercentage, out bool underRoof, float minDistance = 0.5f)
	{
		Setup();
		float num = 0f;
		underRoof = IsUnderRoof(startPos);
		Vector3[] coverRays = m_coverRays;
		foreach (Vector3 vector in coverRays)
		{
			if (Physics.Raycast(startPos + vector * minDistance, vector, out var _, 30f - minDistance, m_coverRayMask))
			{
				num += 1f;
			}
		}
		coverPercentage = num / (float)m_coverRays.Length;
	}

	public static bool IsUnderRoof(Vector3 startPos)
	{
		Setup();
		int num = Physics.SphereCastNonAlloc(startPos, 0.1f, Vector3.up, m_raycastHits, 100f, m_coverRayMask);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = m_raycastHits[i];
			if (!raycastHit.collider.gameObject.CompareTag("leaky"))
			{
				return true;
			}
		}
		return false;
	}

	private static void Setup()
	{
		if (m_coverRays == null)
		{
			m_coverRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
			CreateCoverRays();
		}
	}

	private static void CreateCoverRays()
	{
		m_coverRays = new Vector3[17]
		{
			new Vector3(0f, 1f, 0f),
			new Vector3(1f, 0f, 0f),
			new Vector3(-1f, 0f, 0f),
			new Vector3(0f, 0f, 1f),
			new Vector3(0f, 0f, -1f),
			new Vector3(-1f, 0f, -1f),
			new Vector3(1f, 0f, -1f),
			new Vector3(-1f, 0f, 1f),
			new Vector3(1f, 0f, 1f),
			new Vector3(-1f, 1f, 0f),
			new Vector3(1f, 1f, 0f),
			new Vector3(0f, 1f, 1f),
			new Vector3(0f, 1f, -1f),
			new Vector3(-1f, 1f, -1f),
			new Vector3(1f, 1f, -1f),
			new Vector3(-1f, 1f, 1f),
			new Vector3(1f, 1f, 1f)
		};
		Vector3[] coverRays = m_coverRays;
		foreach (Vector3 vector in coverRays)
		{
			vector.Normalize();
		}
	}
}
