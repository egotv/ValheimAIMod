using System;
using UnityEngine;

namespace Valheim.UI;

[Serializable]
public struct ItemGroupMapping
{
	public string Name;

	public ItemDrop.ItemData.ItemType[] ItemTypes;

	public Sprite Sprite;

	public string LocaString;

	public static string None = "none";
}
