using System.Collections.Generic;
using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "ValheimRadialConfig", menuName = "Valheim/RadialConfig", order = 1)]
public class ValheimRadialConfig : ScriptableObject, IRadialConfig
{
	[SerializeField]
	protected ItemElement m_itemPrefab;

	[SerializeField]
	protected GroupElement m_groupPrefab;

	[SerializeField]
	protected HammerItemElement m_hammerPrefab;

	[SerializeField]
	protected EmptyElement m_emptyPrefab;

	[SerializeField]
	protected ItemGroupMappings m_mappings;

	[SerializeField]
	protected ItemGroupConfig m_itemGroupConfig;

	[SerializeField]
	protected EmoteGroupConfig m_emoteGroupConfig;

	[SerializeField]
	protected HotbarGroupConfig m_hotbarGroupConfig;

	public string LocalizedName => "Main";

	public Sprite Sprite => null;

	public void SetRadial(DynamicRadialMenu radial, int page)
	{
		radial.OnInteractionDelay = delegate(float delay)
		{
			PlayerController.SetTakeInputDelay(delay);
		};
		radial.SetXYControls();
		radial.SetItemInteractionControls();
		radial.SetPaginationControls();
		radial.SetClose(useBackButtonToClose: true);
		List<RadialMenuElement> list = new List<RadialMenuElement>();
		AddHotbarGroup(radial, list);
		AddItemGroup(radial, list, "consumables");
		AddItemGroup(radial, list, "handitems");
		AddItemGroup(radial, list, "armor_utility");
		AddEmoteGroup(radial, list);
		AddItemGroup(radial, list, "allitems");
		AddHammer(list);
		AddEmpty(list);
		radial.MaxElementsPerPage = 8;
		radial.SetElements(list.ToArray(), page, addBackButton: false, fillUp: false);
	}

	private void AddEmoteGroup(DynamicRadialMenu radial, List<RadialMenuElement> elements)
	{
		GroupElement groupElement = Object.Instantiate(m_groupPrefab);
		EmoteGroupConfig config = Object.Instantiate(m_emoteGroupConfig);
		groupElement.Init(config, this, radial);
		elements.Add(groupElement);
	}

	private void AddHotbarGroup(DynamicRadialMenu radial, List<RadialMenuElement> elements)
	{
		GroupElement groupElement = Object.Instantiate(m_groupPrefab);
		HotbarGroupConfig config = Object.Instantiate(m_hotbarGroupConfig);
		groupElement.Init(config, this, radial);
		elements.Add(groupElement);
	}

	private void AddItemGroup(DynamicRadialMenu radial, List<RadialMenuElement> elements, string groupName)
	{
		GroupElement groupElement = Object.Instantiate(m_groupPrefab);
		ItemGroupConfig itemGroupConfig = Object.Instantiate(m_itemGroupConfig);
		itemGroupConfig.GroupName = groupName;
		groupElement.Init(itemGroupConfig, this, radial);
		elements.Add(groupElement);
	}

	private void AddEmpty(List<RadialMenuElement> elements)
	{
		EmptyElement emptyElement = Object.Instantiate(m_emptyPrefab);
		emptyElement.Init();
		elements.Add(emptyElement);
	}

	private void AddHammer(List<RadialMenuElement> elements)
	{
		HammerItemElement hammerItemElement = Object.Instantiate(m_hammerPrefab);
		hammerItemElement.Init();
		elements.Add(hammerItemElement);
	}
}
