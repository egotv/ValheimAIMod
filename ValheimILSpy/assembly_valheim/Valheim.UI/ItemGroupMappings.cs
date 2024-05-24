using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "ItemGroupMappings", menuName = "Valheim/ItemGroupMappings", order = 1)]
public class ItemGroupMappings : ScriptableObject
{
	[SerializeField]
	protected ItemGroupMapping[] _itemGroups;

	public ItemGroupMapping[] Groups => _itemGroups;

	public ItemGroupMapping GetMapping(string group)
	{
		if (_itemGroups != null)
		{
			for (int i = 0; i < _itemGroups.Length; i++)
			{
				if (_itemGroups[i].Name == group)
				{
					return _itemGroups[i];
				}
			}
		}
		ItemGroupMapping result = default(ItemGroupMapping);
		result.Name = ItemGroupMapping.None;
		result.ItemTypes = new ItemDrop.ItemData.ItemType[1];
		return result;
	}
}
