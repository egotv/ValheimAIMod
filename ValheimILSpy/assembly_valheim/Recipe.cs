using UnityEngine;

public class Recipe : ScriptableObject
{
	public ItemDrop m_item;

	public int m_amount = 1;

	public bool m_enabled = true;

	[Tooltip("Only supported when using m_requireOnlyOneIngredient")]
	public float m_qualityResultAmountMultiplier = 1f;

	[Header("Requirements")]
	public CraftingStation m_craftingStation;

	public CraftingStation m_repairStation;

	public int m_minStationLevel = 1;

	public bool m_requireOnlyOneIngredient;

	public Piece.Requirement[] m_resources = new Piece.Requirement[0];

	public int GetRequiredStationLevel(int quality)
	{
		return Mathf.Max(1, m_minStationLevel) + (quality - 1);
	}

	public CraftingStation GetRequiredStation(int quality)
	{
		if ((bool)m_craftingStation)
		{
			return m_craftingStation;
		}
		if (quality > 1)
		{
			return m_repairStation;
		}
		return null;
	}

	public int GetAmount(int quality, out int need, out ItemDrop.ItemData singleReqItem)
	{
		int num = m_amount;
		need = 0;
		singleReqItem = null;
		if (m_requireOnlyOneIngredient)
		{
			singleReqItem = Player.m_localPlayer.GetFirstRequiredItem(Player.m_localPlayer.GetInventory(), this, quality, out need, out var extraAmount);
			num += (int)Mathf.Ceil((float)((singleReqItem.m_quality - 1) * num) * m_qualityResultAmountMultiplier) + extraAmount;
		}
		return num;
	}
}
