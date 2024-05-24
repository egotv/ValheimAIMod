namespace Valheim.UI;

public class BackElement : RadialMenuElement
{
	public void Init(DynamicRadialMenu radial)
	{
		base.Name = "Back";
		base.Interact = delegate
		{
			radial.Back();
			return false;
		};
	}
}
