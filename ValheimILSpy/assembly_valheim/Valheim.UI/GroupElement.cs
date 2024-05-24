namespace Valheim.UI;

public class GroupElement : RadialMenuElement
{
	public void Init(IRadialConfig config, IRadialConfig backConfig, DynamicRadialMenu radial)
	{
		if (config == null)
		{
			base.Name = "";
			base.Interact = null;
		}
		else
		{
			base.Name = config.LocalizedName;
			base.Interact = delegate
			{
				radial.Open(config, backConfig);
				return false;
			};
		}
		m_icon.gameObject.SetActive(config.Sprite != null);
		m_icon.sprite = config.Sprite;
	}
}
