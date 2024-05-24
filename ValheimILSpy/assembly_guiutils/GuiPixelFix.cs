using UnityEngine;

public class GuiPixelFix : MonoBehaviour
{
	private void LateUpdate()
	{
		RectTransform rectTransform = base.transform as RectTransform;
		if (!(rectTransform.parent == null))
		{
			Rect rect = (rectTransform.parent as RectTransform).rect;
			Vector2 offsetMax = rectTransform.offsetMax;
			offsetMax.x = ((rect.width % 2f != 0f) ? 1 : 0);
			offsetMax.y = ((rect.height % 2f != 0f) ? 1 : 0);
			rectTransform.offsetMax = offsetMax;
		}
	}
}
