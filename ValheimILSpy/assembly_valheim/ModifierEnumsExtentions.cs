public static class ModifierEnumsExtentions
{
	public static string GetDisplayString(this WorldModifiers modifiers)
	{
		return modifiers switch
		{
			WorldModifiers.Default => "$menu_default", 
			WorldModifiers.Combat => "$menu_combat", 
			WorldModifiers.DeathPenalty => "$menu_deathpenalty", 
			WorldModifiers.Resources => "$menu_resources", 
			WorldModifiers.Raids => "$menu_events", 
			WorldModifiers.Portals => "$menu_portals", 
			_ => "$menu_unknown", 
		};
	}

	public static string GetDisplayString(this WorldModifierOption modifiers)
	{
		return modifiers switch
		{
			WorldModifierOption.Default => "$menu_default", 
			WorldModifierOption.None => "$menu_none", 
			WorldModifierOption.Less => "$menu_less", 
			WorldModifierOption.MuchLess => "$menu_muchless", 
			WorldModifierOption.More => "$menu_more", 
			WorldModifierOption.MuchMore => "$menu_muchmore", 
			WorldModifierOption.Casual => "$menu_modifier_casual", 
			WorldModifierOption.VeryEasy => "$menu_modifier_veryeasy", 
			WorldModifierOption.Easy => "$menu_modifier_easy", 
			WorldModifierOption.Hard => "$menu_modifier_hard", 
			WorldModifierOption.VeryHard => "$menu_modifier_veryhard", 
			WorldModifierOption.Hardcore => "$menu_modifier_hardcore", 
			WorldModifierOption.Most => "$menu_modifier_most", 
			_ => "$menu_unknown", 
		};
	}

	public static string GetDisplayString(this WorldPresets preset)
	{
		return preset switch
		{
			WorldPresets.Default => "$menu_default", 
			WorldPresets.Custom => "$menu_modifier_custom", 
			WorldPresets.Normal => "$menu_modifier_normal", 
			WorldPresets.Casual => "$menu_modifier_casual", 
			WorldPresets.Easy => "$menu_modifier_easy", 
			WorldPresets.Hard => "$menu_modifier_hard", 
			WorldPresets.Hardcore => "$menu_modifier_hardcore", 
			WorldPresets.Immersive => "$menu_modifier_immersive", 
			WorldPresets.Hammer => "$menu_modifier_hammer", 
			_ => "$menu_unknown", 
		};
	}
}
