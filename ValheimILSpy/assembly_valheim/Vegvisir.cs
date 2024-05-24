using System;
using System.Collections.Generic;
using UnityEngine;

public class Vegvisir : MonoBehaviour, Hoverable, Interactable
{
	[Serializable]
	public class VegvisrLocation
	{
		public string m_locationName = "";

		public string m_pinName = "Pin";

		public Minimap.PinType m_pinType;

		[Tooltip("Discovers all locations of given name, rather than just the closest one.")]
		public bool m_discoverAll;

		public bool m_showMap = true;
	}

	public string m_name = "$piece_vegvisir";

	public string m_useText = "$piece_register_location";

	public string m_hoverName = "Pin";

	public string m_setsGlobalKey = "";

	public string m_setsPlayerKey = "";

	public List<VegvisrLocation> m_locations = new List<VegvisrLocation>();

	public string GetHoverText()
	{
		return Localization.instance.Localize(m_name + " " + m_hoverName + "\n[<color=yellow><b>$KEY_Use</b></color>] " + m_useText);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		foreach (VegvisrLocation location in m_locations)
		{
			Game.instance.DiscoverClosestLocation(location.m_locationName, base.transform.position, location.m_pinName, (int)location.m_pinType, location.m_showMap, location.m_discoverAll);
			Gogan.LogEvent("Game", "Vegvisir", location.m_locationName, 0L);
		}
		if (!string.IsNullOrEmpty(m_setsGlobalKey))
		{
			ZoneSystem.instance.SetGlobalKey(m_setsGlobalKey);
		}
		if (!string.IsNullOrEmpty(m_setsPlayerKey) && character is Player player)
		{
			player.AddUniqueKey(m_setsPlayerKey);
		}
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}
}
