using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISelectableGroup : Selectable
{
	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		ZLog.Log("Select a child");
	}
}
