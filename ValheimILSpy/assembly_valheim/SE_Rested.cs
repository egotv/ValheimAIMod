using System.Collections.Generic;
using UnityEngine;

public class SE_Rested : SE_Stats
{
	[Header("__SE_Rested__")]
	public float m_baseTTL = 300f;

	public float m_TTLPerComfortLevel = 60f;

	private const float c_ComfortRadius = 10f;

	private float m_timeSinceComfortUpdate;

	private static readonly List<Piece> s_tempPieces = new List<Piece>();

	public override void Setup(Character character)
	{
		base.Setup(character);
		UpdateTTL();
		Player player = m_character as Player;
		m_character.Message(MessageHud.MessageType.Center, "$se_rested_start ($se_rested_comfort:" + player.GetComfortLevel() + ")");
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_timeSinceComfortUpdate -= dt;
	}

	public override void ResetTime()
	{
		UpdateTTL();
	}

	private void UpdateTTL()
	{
		Player player = m_character as Player;
		float num = m_baseTTL + (float)(player.GetComfortLevel() - 1) * m_TTLPerComfortLevel;
		float num2 = m_ttl - m_time;
		if (num > num2)
		{
			m_ttl = num;
			m_time = 0f;
		}
	}

	private static int PieceComfortSort(Piece x, Piece y)
	{
		if (x.m_comfortGroup != y.m_comfortGroup)
		{
			return x.m_comfortGroup.CompareTo(y.m_comfortGroup);
		}
		float num = x.GetComfort();
		float num2 = y.GetComfort();
		if (num != num2)
		{
			return num2.CompareTo(num);
		}
		return y.m_name.CompareTo(x.m_name);
	}

	public static int CalculateComfortLevel(Player player)
	{
		return CalculateComfortLevel(player.InShelter(), player.transform.position);
	}

	public static int CalculateComfortLevel(bool inShelter, Vector3 position)
	{
		int num = 1;
		if (inShelter)
		{
			num++;
			List<Piece> nearbyComfortPieces = GetNearbyComfortPieces(position);
			nearbyComfortPieces.Sort(PieceComfortSort);
			for (int i = 0; i < nearbyComfortPieces.Count; i++)
			{
				Piece piece = nearbyComfortPieces[i];
				if (i > 0)
				{
					Piece piece2 = nearbyComfortPieces[i - 1];
					if ((piece.m_comfortGroup != 0 && piece.m_comfortGroup == piece2.m_comfortGroup) || piece.m_name == piece2.m_name)
					{
						continue;
					}
				}
				num += piece.GetComfort();
			}
		}
		return num;
	}

	private static List<Piece> GetNearbyComfortPieces(Vector3 point)
	{
		s_tempPieces.Clear();
		Piece.GetAllComfortPiecesInRadius(point, 10f, s_tempPieces);
		return s_tempPieces;
	}
}
