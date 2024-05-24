using System;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.UI;

public class RadialMenuElement : MonoBehaviour
{
	[SerializeField]
	protected Image m_icon;

	[SerializeField]
	protected Image m_background;

	[SerializeField]
	protected CanvasGroup m_canvasGroup;

	public Material m_backgroundMaterial;

	public Material BackgroundMaterial
	{
		get
		{
			if (m_backgroundMaterial == null)
			{
				m_backgroundMaterial = new Material(Background.material);
				Background.material = m_backgroundMaterial;
			}
			return m_backgroundMaterial;
		}
	}

	public Image Icon => m_icon;

	public Image Background => m_background;

	public string Name { get; protected set; }

	public string Description { get; protected set; }

	public string Buttons { get; set; }

	public Func<bool> Interact { get; set; }

	public float Alpha
	{
		get
		{
			return m_canvasGroup.alpha;
		}
		set
		{
			m_canvasGroup.alpha = value;
		}
	}

	public bool Selected
	{
		get
		{
			return BackgroundMaterial.GetInt("_Selected") == 1;
		}
		set
		{
			BackgroundMaterial.SetInt("_Selected", value ? 1 : 0);
		}
	}

	public bool Activated
	{
		get
		{
			return BackgroundMaterial.GetInt("_Activated") == 1;
		}
		set
		{
			BackgroundMaterial.SetInt("_Activated", value ? 1 : 0);
		}
	}

	public void SetSegement(int index, int segments)
	{
		BackgroundMaterial.SetInt("_Segments", segments);
		BackgroundMaterial.SetFloat("_Offset", (float)index / (float)segments);
	}

	public void SetSegement(float offset, int segments)
	{
		offset = Math.Clamp(offset, 0f, 1f);
		BackgroundMaterial.SetInt("_Segments", segments);
		BackgroundMaterial.SetFloat("_Offset", offset);
	}
}
