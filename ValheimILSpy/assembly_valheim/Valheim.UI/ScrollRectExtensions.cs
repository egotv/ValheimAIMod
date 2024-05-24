using UnityEngine;
using UnityEngine.UI;

namespace Valheim.UI;

public static class ScrollRectExtensions
{
	public static void SnapToChild(this ScrollRect scrollRect, RectTransform child)
	{
		Vector2 vector = scrollRect.viewport.transform.InverseTransformPoint(child.position);
		float height = scrollRect.viewport.rect.height;
		bool num = vector.y > 0f;
		bool flag = 0f - vector.y + child.rect.height > height;
		float num2 = (num ? (0f - vector.y) : (flag ? (0f - vector.y + child.rect.height - height) : 0f));
		scrollRect.content.anchoredPosition = new Vector2(0f, scrollRect.content.anchoredPosition.y + num2);
	}

	public static bool IsVisible(this ScrollRect scrollRect, RectTransform child)
	{
		float height = scrollRect.viewport.rect.height;
		Vector2 vector = scrollRect.viewport.transform.InverseTransformPoint(child.position);
		if (vector.y < 0f)
		{
			return 0f - vector.y + child.rect.height < height;
		}
		return false;
	}
}
