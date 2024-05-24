using System;
using System.Collections.Generic;
using UnityEngine;

public class PieceTable : MonoBehaviour
{
	public const int m_gridWidth = 15;

	public const int m_gridHeight = 6;

	public List<GameObject> m_pieces = new List<GameObject>();

	public bool m_useCategories = true;

	public bool m_canRemovePieces = true;

	[NonSerialized]
	private List<List<Piece>> m_availablePieces = new List<List<Piece>>();

	[NonSerialized]
	public Piece.PieceCategory m_selectedCategory;

	[NonSerialized]
	public Vector2Int[] m_selectedPiece = new Vector2Int[5];

	[NonSerialized]
	public Vector2Int[] m_lastSelectedPiece = new Vector2Int[5];

	[HideInInspector]
	public List<Piece.PieceCategory> m_categoriesFolded = new List<Piece.PieceCategory>();

	public void UpdateAvailable(HashSet<string> knownRecipies, Player player, bool hideUnavailable, bool noPlacementCost)
	{
		if (m_availablePieces.Count == 0)
		{
			for (int i = 0; i < 5; i++)
			{
				m_availablePieces.Add(new List<Piece>());
			}
		}
		foreach (List<Piece> availablePiece in m_availablePieces)
		{
			availablePiece.Clear();
		}
		foreach (GameObject piece in m_pieces)
		{
			Piece component = piece.GetComponent<Piece>();
			bool flag = player.CurrentSeason != null && player.CurrentSeason.Pieces.Contains(piece);
			if (!noPlacementCost && (!knownRecipies.Contains(component.m_name) || !(component.m_enabled || flag) || (hideUnavailable && !player.HaveRequirements(component, Player.RequirementMode.CanAlmostBuild))))
			{
				continue;
			}
			if (component.m_category == Piece.PieceCategory.All)
			{
				for (int j = 0; j < 5; j++)
				{
					m_availablePieces[j].Add(component);
				}
			}
			else
			{
				m_availablePieces[(int)component.m_category].Add(component);
			}
		}
	}

	public GameObject GetSelectedPrefab()
	{
		Piece selectedPiece = GetSelectedPiece();
		if ((bool)selectedPiece)
		{
			return selectedPiece.gameObject;
		}
		return null;
	}

	public Piece GetPiece(int category, Vector2Int p)
	{
		if (m_availablePieces[category].Count == 0)
		{
			return null;
		}
		int num = p.y * 15 + p.x;
		if (num < 0 || num >= m_availablePieces[category].Count)
		{
			return null;
		}
		return m_availablePieces[category][num];
	}

	public Piece GetPiece(Vector2Int p)
	{
		return GetPiece((int)m_selectedCategory, p);
	}

	public bool IsPieceAvailable(Piece piece)
	{
		foreach (Piece item in m_availablePieces[(int)m_selectedCategory])
		{
			if (item == piece)
			{
				return true;
			}
		}
		return false;
	}

	public Piece GetSelectedPiece()
	{
		Vector2Int selectedIndex = GetSelectedIndex();
		return GetPiece((int)m_selectedCategory, selectedIndex);
	}

	public int GetAvailablePiecesInCategory(Piece.PieceCategory cat)
	{
		return m_availablePieces[(int)cat].Count;
	}

	public List<Piece> GetPiecesInSelectedCategory()
	{
		return m_availablePieces[(int)m_selectedCategory];
	}

	public int GetAvailablePiecesInSelectedCategory()
	{
		return GetAvailablePiecesInCategory(m_selectedCategory);
	}

	public Vector2Int GetSelectedIndex()
	{
		return m_selectedPiece[(int)m_selectedCategory];
	}

	public bool GetPieceIndex(Piece p, out Vector2Int index, out int category)
	{
		string prefabName = Utils.GetPrefabName(p.gameObject);
		for (int i = 0; i < m_availablePieces.Count; i++)
		{
			for (int j = 0; j < m_availablePieces[i].Count; j++)
			{
				if (Utils.GetPrefabName(m_availablePieces[i][j].gameObject) == prefabName)
				{
					category = i;
					index = new Vector2Int(j % 15, (j - j % 15) / 15);
					return true;
				}
			}
		}
		index = Vector2Int.zero;
		category = -1;
		return false;
	}

	public void SetSelected(Vector2Int p)
	{
		m_selectedPiece[(int)m_selectedCategory] = p;
	}

	public void LeftPiece()
	{
		if (m_availablePieces[(int)m_selectedCategory].Count > 1)
		{
			Vector2Int vector2Int = m_selectedPiece[(int)m_selectedCategory];
			int x = vector2Int.x - 1;
			vector2Int.x = x;
			if (vector2Int.x < 0)
			{
				vector2Int.x = 14;
			}
			m_selectedPiece[(int)m_selectedCategory] = vector2Int;
		}
	}

	public void RightPiece()
	{
		if (m_availablePieces[(int)m_selectedCategory].Count > 1)
		{
			Vector2Int vector2Int = m_selectedPiece[(int)m_selectedCategory];
			int x = vector2Int.x + 1;
			vector2Int.x = x;
			if (vector2Int.x >= 15)
			{
				vector2Int.x = 0;
			}
			m_selectedPiece[(int)m_selectedCategory] = vector2Int;
		}
	}

	public void DownPiece()
	{
		if (m_availablePieces[(int)m_selectedCategory].Count > 1)
		{
			Vector2Int vector2Int = m_selectedPiece[(int)m_selectedCategory];
			int y = vector2Int.y + 1;
			vector2Int.y = y;
			if (vector2Int.y >= 6)
			{
				vector2Int.y = 0;
			}
			m_selectedPiece[(int)m_selectedCategory] = vector2Int;
		}
	}

	public void UpPiece()
	{
		if (m_availablePieces[(int)m_selectedCategory].Count > 1)
		{
			Vector2Int vector2Int = m_selectedPiece[(int)m_selectedCategory];
			int y = vector2Int.y - 1;
			vector2Int.y = y;
			if (vector2Int.y < 0)
			{
				vector2Int.y = 5;
			}
			m_selectedPiece[(int)m_selectedCategory] = vector2Int;
		}
	}

	public void NextCategory()
	{
		if (m_useCategories)
		{
			m_selectedCategory++;
			if (m_selectedCategory == Piece.PieceCategory.Max)
			{
				m_selectedCategory = Piece.PieceCategory.Misc;
			}
		}
	}

	public void PrevCategory()
	{
		if (m_useCategories)
		{
			m_selectedCategory--;
			if (m_selectedCategory < Piece.PieceCategory.Misc)
			{
				m_selectedCategory = Piece.PieceCategory.Furniture;
			}
		}
	}

	public void SetCategory(int index)
	{
		if (m_useCategories)
		{
			m_selectedCategory = (Piece.PieceCategory)index;
			m_selectedCategory = (Piece.PieceCategory)Mathf.Clamp((int)m_selectedCategory, 0, 4);
		}
	}
}
