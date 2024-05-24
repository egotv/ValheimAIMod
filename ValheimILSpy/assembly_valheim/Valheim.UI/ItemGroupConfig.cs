using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "ItemGroupConfig", menuName = "Valheim/ItemGroupConfig", order = 1)]
public class ItemGroupConfig : ScriptableObject, IRadialConfig
{
	[SerializeField]
	protected ItemElement m_itemPrefab;

	[SerializeField]
	protected ItemGroupMappings m_itemTypeMappings;

	public string GroupName { get; set; }

	public ItemDrop.ItemData.ItemType[] ItemTypes => m_itemTypeMappings.GetMapping(GroupName).ItemTypes;

	public string LocalizedName => Localization.instance.Localize("$" + m_itemTypeMappings.GetMapping(GroupName).LocaString);

	public Sprite Sprite => m_itemTypeMappings.GetMapping(GroupName).Sprite;

	public void SetRadial(DynamicRadialMenu radial, int page)
	{
		radial.SetClose(useBackButtonToClose: false);
		List<RadialMenuElement> list = new List<RadialMenuElement>();
		List<ItemDrop.ItemData> allItemsSorted = Player.m_localPlayer.GetInventory().GetAllItemsSorted();
		radial.MaxElementsPerPage = 8;
		foreach (ItemDrop.ItemData item in allItemsSorted)
		{
			if (GroupName == ItemGroupMapping.None || Array.IndexOf(ItemTypes, item.m_shared.m_itemType) > -1)
			{
				ItemElement itemElement = UnityEngine.Object.Instantiate(m_itemPrefab);
				itemElement.Init(item);
				list.Add(itemElement);
			}
		}
		radial.SetElements(list.ToArray(), page, addBackButton: true, fillUp: true);
	}
}
