using System.Collections.Generic;
using UnityEngine;

public class TerrainLod : MonoBehaviour
{
	private enum HeightmapState
	{
		NeedsRebuild,
		ReadyToRebuild,
		Done
	}

	private class HeightmapWithOffset
	{
		public Heightmap m_heightmap;

		public Vector3 m_offset;

		public HeightmapState m_state;

		public HeightmapWithOffset(Heightmap heightmap, Vector3 offset)
		{
			m_heightmap = heightmap;
			m_offset = offset;
			m_state = HeightmapState.NeedsRebuild;
		}
	}

	[SerializeField]
	private float m_updateStepDistance = 256f;

	[SerializeField]
	private float m_terrainSize = 2400f;

	[SerializeField]
	private int m_regionsPerAxis = 3;

	[SerializeField]
	private float m_vertexDistance = 10f;

	[SerializeField]
	private Material m_material;

	private List<HeightmapWithOffset> m_heightmaps = new List<HeightmapWithOffset>();

	private Vector3 m_lastPoint = new Vector3(99999f, 0f, 99999f);

	private HeightmapState m_heightmapState = HeightmapState.Done;

	private void OnEnable()
	{
		CreateMeshes();
	}

	private void OnDisable()
	{
		ResetMeshes();
	}

	private void CreateMeshes()
	{
		float num = m_terrainSize / (float)m_regionsPerAxis;
		float num2 = Mathf.Round(m_vertexDistance);
		int width = Mathf.RoundToInt(num / num2);
		for (int i = 0; i < m_regionsPerAxis; i++)
		{
			for (int j = 0; j < m_regionsPerAxis; j++)
			{
				Vector3 offset = new Vector3(((float)i * 2f - (float)m_regionsPerAxis + 1f) * m_terrainSize * 0.5f / (float)m_regionsPerAxis, 0f, ((float)j * 2f - (float)m_regionsPerAxis + 1f) * m_terrainSize * 0.5f / (float)m_regionsPerAxis);
				CreateMesh(num2, width, offset);
			}
		}
	}

	private void CreateMesh(float scale, int width, Vector3 offset)
	{
		GameObject obj = new GameObject("lodMesh");
		obj.transform.position = offset;
		obj.transform.SetParent(base.transform);
		Heightmap heightmap = obj.AddComponent<Heightmap>();
		m_heightmaps.Add(new HeightmapWithOffset(heightmap, offset));
		heightmap.m_scale = scale;
		heightmap.m_width = width;
		heightmap.m_material = m_material;
		heightmap.IsDistantLod = true;
		heightmap.enabled = true;
	}

	private void ResetMeshes()
	{
		for (int i = 0; i < m_heightmaps.Count; i++)
		{
			Object.Destroy(m_heightmaps[i].m_heightmap.gameObject);
		}
		m_heightmaps.Clear();
		m_lastPoint = new Vector3(99999f, 0f, 99999f);
		m_heightmapState = HeightmapState.Done;
	}

	private void Update()
	{
		UpdateHeightmaps();
	}

	private void UpdateHeightmaps()
	{
		if (ZNet.GetConnectionStatus() == ZNet.ConnectionStatus.Connected && NeedsRebuild() && IsAllTerrainReady())
		{
			RebuildAllHeightmaps();
		}
	}

	private void RebuildAllHeightmaps()
	{
		for (int i = 0; i < m_heightmaps.Count; i++)
		{
			RebuildHeightmap(m_heightmaps[i]);
		}
		m_heightmapState = HeightmapState.Done;
	}

	private bool IsAllTerrainReady()
	{
		int num = 0;
		for (int i = 0; i < m_heightmaps.Count; i++)
		{
			if (IsTerrainReady(m_heightmaps[i]))
			{
				num++;
			}
		}
		return num == m_heightmaps.Count;
	}

	private bool IsTerrainReady(HeightmapWithOffset heightmapWithOffset)
	{
		Heightmap heightmap = heightmapWithOffset.m_heightmap;
		Vector3 offset = heightmapWithOffset.m_offset;
		if (heightmapWithOffset.m_state == HeightmapState.ReadyToRebuild)
		{
			return true;
		}
		if (HeightmapBuilder.instance.IsTerrainReady(m_lastPoint + offset, heightmap.m_width, heightmap.m_scale, heightmap.IsDistantLod, WorldGenerator.instance))
		{
			heightmapWithOffset.m_state = HeightmapState.ReadyToRebuild;
			return true;
		}
		return false;
	}

	private void RebuildHeightmap(HeightmapWithOffset heightmapWithOffset)
	{
		Heightmap heightmap = heightmapWithOffset.m_heightmap;
		Vector3 offset = heightmapWithOffset.m_offset;
		heightmap.transform.position = m_lastPoint + offset;
		heightmap.Regenerate();
		heightmapWithOffset.m_state = HeightmapState.Done;
	}

	private bool NeedsRebuild()
	{
		if (m_heightmapState == HeightmapState.NeedsRebuild)
		{
			return true;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return false;
		}
		Vector3 position = mainCamera.transform.position;
		if (Utils.DistanceXZ(position, m_lastPoint) > m_updateStepDistance && m_heightmapState == HeightmapState.Done)
		{
			for (int i = 0; i < m_heightmaps.Count; i++)
			{
				m_heightmaps[i].m_state = HeightmapState.NeedsRebuild;
			}
			m_lastPoint = new Vector3(Mathf.Round(position.x / m_vertexDistance) * m_vertexDistance, 0f, Mathf.Round(position.z / m_vertexDistance) * m_vertexDistance);
			m_heightmapState = HeightmapState.NeedsRebuild;
			return true;
		}
		return false;
	}
}
