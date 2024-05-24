namespace Valheim.UI;

public class EmptyElement : RadialMenuElement
{
	public void Init()
	{
		base.Name = "Empty";
		base.Interact = () => false;
	}
}
