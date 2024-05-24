using System.Collections.Generic;

namespace Valheim.UI;

public class HammerItemElement : ItemElement
{
	private static ItemDrop.ItemData m_lastLeftItem;

	private static ItemDrop.ItemData m_lastRightItem;

	public void Init()
	{
		List<ItemDrop.ItemData> allItemsSorted = Player.m_localPlayer.GetInventory().GetAllItemsSorted();
		ItemDrop.ItemData item = null;
		foreach (ItemDrop.ItemData item2 in allItemsSorted)
		{
			if (IsHammer(item2))
			{
				item = item2;
				break;
			}
		}
		Init(item);
	}

	private static bool IsHammer(ItemDrop.ItemData item)
	{
		if (item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool && item.m_shared.m_skillType == Skills.SkillType.Swords)
		{
			return item.m_shared.m_name.Contains("hammer");
		}
		return false;
	}

	protected override void SetInteraction(ItemDrop.ItemData item)
	{
		base.Interact = delegate
		{
			if ((bool)Player.m_localPlayer)
			{
				if (item.m_equipped)
				{
					Player.m_localPlayer.UseItem(null, item, fromInventoryGui: false);
					if (m_lastLeftItem != null)
					{
						Player.m_localPlayer.EquipItem(m_lastLeftItem);
						m_lastLeftItem = null;
					}
					if (m_lastRightItem != null)
					{
						Player.m_localPlayer.EquipItem(m_lastRightItem);
						m_lastRightItem = null;
					}
				}
				else
				{
					m_lastLeftItem = Player.m_localPlayer.LeftItem;
					m_lastRightItem = Player.m_localPlayer.RightItem;
					Player.m_localPlayer.UseItem(null, item, fromInventoryGui: false);
				}
			}
			return true;
		};
	}
}
