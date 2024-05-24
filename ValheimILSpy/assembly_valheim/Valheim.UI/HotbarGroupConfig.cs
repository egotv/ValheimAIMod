using System.Collections.Generic;
using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "HotbarGroupConfig", menuName = "Valheim/HotbarGroupConfig", order = 1)]
public class HotbarGroupConfig : ScriptableObject, IRadialConfig
{
	[SerializeField]
	protected ItemElement m_itemPrefab;

	[SerializeField]
	protected Sprite m_icon;

	public string LocalizedName => Localization.instance.Localize("$radial_hotbar");

	public Sprite Sprite => m_icon;

	public void SetRadial(DynamicRadialMenu radial, int page)
	{
		radial.SetClose(useBackButtonToClose: false);
		List<RadialMenuElement> list = new List<RadialMenuElement>();
		Player localPlayer = Player.m_localPlayer;
		List<ItemDrop.ItemData> list2 = new List<ItemDrop.ItemData>();
		localPlayer.GetInventory().GetBoundItemsSorted(list2);
		for (int i = 0; i < 8; i++)
		{
			ItemElement itemElement = Object.Instantiate(m_itemPrefab);
			itemElement.Init(null);
			foreach (ItemDrop.ItemData item in list2)
			{
				if (item.m_gridPos.x == i)
				{
					itemElement.Init(item);
					break;
				}
			}
			list.Add(itemElement);
		}
		radial.MaxElementsPerPage = 9;
		radial.SetElements(list.ToArray(), page, addBackButton: true, fillUp: true);
	}
}
