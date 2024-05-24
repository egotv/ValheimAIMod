using System;

public class Emote : Attribute
{
	public bool OneShot = true;

	public bool FaceLookDirection;

	public static void DoEmote(Emotes emote)
	{
		Emote attributeOfType = emote.GetAttributeOfType<Emote>();
		if ((bool)Player.m_localPlayer && Player.m_localPlayer.StartEmote(emote.ToString().ToLower(), attributeOfType?.OneShot ?? true) && attributeOfType != null && attributeOfType.FaceLookDirection)
		{
			Player.m_localPlayer.FaceLookDirection();
		}
	}
}
