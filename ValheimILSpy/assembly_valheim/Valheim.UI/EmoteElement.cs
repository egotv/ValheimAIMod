namespace Valheim.UI;

public class EmoteElement : RadialMenuElement
{
	public void Init(EmoteDataMapping mapping)
	{
		if (mapping.Emote == Emotes.Count)
		{
			base.Name = "";
			base.Interact = null;
		}
		else
		{
			base.Name = ((!string.IsNullOrEmpty(mapping.LocaString)) ? Localization.instance.Localize("$" + mapping.LocaString) : mapping.Emote.ToString());
			base.Interact = delegate
			{
				Emote.DoEmote(mapping.Emote);
				return true;
			};
		}
		m_icon.gameObject.SetActive(mapping.Sprite != null);
		m_icon.sprite = mapping.Sprite;
	}
}
