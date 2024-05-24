using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.UI;

public class RadialLayout : LayoutGroup
{
	[SerializeField]
	protected float radius;

	[SerializeField]
	protected bool clockwise;

	[Range(0f, 360f)]
	[SerializeField]
	protected float minAngle;

	[Range(0f, 360f)]
	[SerializeField]
	protected float maxAngle = 360f;

	[Range(0f, 360f)]
	[SerializeField]
	protected float startAngle;

	[Header("Child rotation")]
	[Range(0f, 360f)]
	[SerializeField]
	protected float startElementAngle;

	[SerializeField]
	protected bool rotateElements;

	[SerializeField]
	protected int maxElements;

	public int MaxElements
	{
		get
		{
			return maxElements;
		}
		set
		{
			maxElements = value;
		}
	}

	public float Radius
	{
		get
		{
			return radius;
		}
		set
		{
			if (radius != value)
			{
				radius = value;
				OnValueChanged();
			}
		}
	}

	public bool Clockwise
	{
		get
		{
			return clockwise;
		}
		set
		{
			if (clockwise != value)
			{
				clockwise = value;
				OnValueChanged();
			}
		}
	}

	public float MinAngle
	{
		get
		{
			return minAngle;
		}
		set
		{
			if (minAngle != value)
			{
				minAngle = value;
				OnValueChanged();
			}
		}
	}

	public float MaxAngle
	{
		get
		{
			return maxAngle;
		}
		set
		{
			if (maxAngle != value)
			{
				maxAngle = value;
				OnValueChanged();
			}
		}
	}

	public float StartAngle
	{
		get
		{
			return startAngle;
		}
		set
		{
			if (startAngle != value)
			{
				startAngle = value;
				OnValueChanged();
			}
		}
	}

	private void OnValueChanged()
	{
		CalculateRadial();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		CalculateRadial();
	}

	public override void SetLayoutHorizontal()
	{
	}

	public override void SetLayoutVertical()
	{
	}

	public override void CalculateLayoutInputVertical()
	{
		CalculateRadial();
	}

	public override void CalculateLayoutInputHorizontal()
	{
		CalculateRadial();
	}

	public void CalculateRadial()
	{
		List<RectTransform> childElements = GetChildElements();
		int count = childElements.Count;
		int num = ((maxElements > 0) ? maxElements : childElements.Count);
		m_Tracker.Clear();
		if (count != 0)
		{
			base.rectTransform.sizeDelta = new Vector2(radius, radius) * 2f;
			float num2 = 360f / (float)num * ((float)num - 1f);
			float num3 = minAngle;
			if (num3 > num2)
			{
				num3 = num2;
			}
			float num4 = 360f - maxAngle;
			if (num4 > num2)
			{
				num4 = num2;
			}
			float num5 = startAngle + num3;
			float num6 = (num2 - num3 - num4) / ((float)num - 1f);
			if (clockwise)
			{
				num6 = 0f - num6;
			}
			DrivenTransformProperties drivenTransformProperties = GetDrivenTransformProperties();
			for (int i = 0; i < count; i++)
			{
				RectTransform rectTransform = childElements[i];
				m_Tracker.Add(this, rectTransform, drivenTransformProperties);
				Vector3 vector = new Vector3(Mathf.Cos(num5 * ((float)Math.PI / 180f)), Mathf.Sin(num5 * ((float)Math.PI / 180f)), 0f);
				rectTransform.localPosition = vector * radius;
				Vector2 vector3 = (rectTransform.pivot = new Vector2(0.5f, 0.5f));
				Vector2 anchorMin = (rectTransform.anchorMax = vector3);
				rectTransform.anchorMin = anchorMin;
				rectTransform.localEulerAngles = new Vector3(0f, 0f, rotateElements ? (startElementAngle + num5) : startElementAngle);
				num5 = (num5 + num6) % 360f;
			}
		}
	}

	private List<RectTransform> GetChildElements()
	{
		List<RectTransform> list = new List<RectTransform>();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			RectTransform rectTransform = base.transform.GetChild(i) as RectTransform;
			LayoutElement component = rectTransform.GetComponent<LayoutElement>();
			if (!(rectTransform == null) && rectTransform.gameObject.activeSelf && (!(component != null) || !component.ignoreLayout))
			{
				list.Add(rectTransform);
			}
		}
		return list;
	}

	private DrivenTransformProperties GetDrivenTransformProperties()
	{
		DrivenTransformProperties drivenTransformProperties = DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Pivot;
		if (rotateElements)
		{
			drivenTransformProperties |= DrivenTransformProperties.Rotation;
		}
		return drivenTransformProperties;
	}
}
