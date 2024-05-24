using System.Collections.Generic;
using UnityEngine;

public class PointGenerator
{
	private int m_amount;

	private float m_gridSize = 8f;

	private Vector2Int m_currentCenterGrid = new Vector2Int(99999, 99999);

	private int m_currentGridWith;

	private List<Vector3> m_points = new List<Vector3>();

	public PointGenerator(int amount, float gridSize)
	{
		m_amount = amount;
		m_gridSize = gridSize;
	}

	public void Update(Vector3 center, float radius, List<Vector3> newPoints, List<Vector3> removedPoints)
	{
		Vector2Int grid = GetGrid(center);
		if (m_currentCenterGrid == grid)
		{
			newPoints.Clear();
			removedPoints.Clear();
			return;
		}
		int num = Mathf.CeilToInt(radius / m_gridSize);
		if (m_currentCenterGrid != grid || m_currentGridWith != num)
		{
			RegeneratePoints(grid, num);
		}
	}

	private void RegeneratePoints(Vector2Int centerGrid, int gridWith)
	{
		m_currentCenterGrid = centerGrid;
		Random.State state = Random.state;
		m_points.Clear();
		for (int i = centerGrid.y - gridWith; i <= centerGrid.y + gridWith; i++)
		{
			for (int j = centerGrid.x - gridWith; j <= centerGrid.x + gridWith; j++)
			{
				Random.InitState(j + i * 100);
				Vector3 gridPos = GetGridPos(new Vector2Int(j, i));
				for (int k = 0; k < m_amount; k++)
				{
					Vector3 item = new Vector3(Random.Range(gridPos.x - m_gridSize, gridPos.x + m_gridSize), Random.Range(gridPos.z - m_gridSize, gridPos.z + m_gridSize));
					m_points.Add(item);
				}
			}
		}
		Random.state = state;
	}

	public Vector2Int GetGrid(Vector3 point)
	{
		int x = Mathf.FloorToInt((point.x + m_gridSize / 2f) / m_gridSize);
		int y = Mathf.FloorToInt((point.z + m_gridSize / 2f) / m_gridSize);
		return new Vector2Int(x, y);
	}

	public Vector3 GetGridPos(Vector2Int grid)
	{
		return new Vector3((float)grid.x * m_gridSize, 0f, (float)grid.y * m_gridSize);
	}
}
