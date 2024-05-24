using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollRectEnsureVisible : MonoBehaviour
{
	private RectTransform maskTransform;

	private ScrollRect mScrollRect;

	private RectTransform mScrollTransform;

	private RectTransform mContent;

	private bool mInitialized;

	private void Awake()
	{
		if (!mInitialized)
		{
			Initialize();
		}
	}

	private void Initialize()
	{
		mScrollRect = GetComponent<ScrollRect>();
		mScrollTransform = mScrollRect.transform as RectTransform;
		mContent = mScrollRect.content;
		Reset();
		mInitialized = true;
	}

	public void CenterOnItem(RectTransform target)
	{
		if (!mInitialized)
		{
			Initialize();
		}
		Vector3 worldPointInWidget = GetWorldPointInWidget(mScrollTransform, GetWidgetWorldPoint(target));
		Vector3 vector = GetWorldPointInWidget(mScrollTransform, GetWidgetWorldPoint(maskTransform)) - worldPointInWidget;
		vector.z = 0f;
		if (!mScrollRect.horizontal)
		{
			vector.x = 0f;
		}
		if (!mScrollRect.vertical)
		{
			vector.y = 0f;
		}
		Vector2 vector2 = new Vector2(vector.x / (mContent.rect.size.x - mScrollTransform.rect.size.x), vector.y / (mContent.rect.size.y - mScrollTransform.rect.size.y));
		Vector2 normalizedPosition = mScrollRect.normalizedPosition - vector2;
		if (mScrollRect.movementType != 0)
		{
			normalizedPosition.x = Mathf.Clamp01(normalizedPosition.x);
			normalizedPosition.y = Mathf.Clamp01(normalizedPosition.y);
		}
		mScrollRect.normalizedPosition = normalizedPosition;
	}

	private void Reset()
	{
		if (!(maskTransform == null))
		{
			return;
		}
		Mask componentInChildren = GetComponentInChildren<Mask>(includeInactive: true);
		if ((bool)componentInChildren)
		{
			maskTransform = componentInChildren.rectTransform;
		}
		if (maskTransform == null)
		{
			RectMask2D componentInChildren2 = GetComponentInChildren<RectMask2D>(includeInactive: true);
			if ((bool)componentInChildren2)
			{
				maskTransform = componentInChildren2.rectTransform;
			}
		}
	}

	private Vector3 GetWidgetWorldPoint(RectTransform target)
	{
		Vector3 vector = new Vector3((0.5f - target.pivot.x) * target.rect.size.x, (0.5f - target.pivot.y) * target.rect.size.y, 0f);
		Vector3 position = target.localPosition + vector;
		return target.parent.TransformPoint(position);
	}

	private Vector3 GetWorldPointInWidget(RectTransform target, Vector3 worldPoint)
	{
		return target.InverseTransformPoint(worldPoint);
	}
}
