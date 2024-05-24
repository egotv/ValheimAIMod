using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuiScaler : MonoBehaviour
{
	private static List<GuiScaler> m_scalers = new List<GuiScaler>();

	private static int m_minWidth = 1920;

	private static int m_minHeight = 1080;

	private CanvasScaler m_canvasScaler;

	private static float m_largeGuiScale = 1f;

	private void Awake()
	{
		m_canvasScaler = GetComponent<CanvasScaler>();
		m_scalers.Add(this);
		m_largeGuiScale = PlatformPrefs.GetFloat("GuiScale", 1f);
	}

	private void OnDestroy()
	{
		m_scalers.Remove(this);
	}

	private void Update()
	{
		UpdateScale();
	}

	private void UpdateScale()
	{
		float screenSizeFactor = GetScreenSizeFactor();
		m_canvasScaler.scaleFactor = screenSizeFactor;
	}

	private float GetScreenSizeFactor()
	{
		float a = (float)Screen.width / (float)m_minWidth;
		float b = (float)Screen.height / (float)m_minHeight;
		return Mathf.Min(a, b) * m_largeGuiScale;
	}

	public static void LoadGuiScale()
	{
		m_largeGuiScale = PlatformPrefs.GetFloat("GuiScale", 1f);
	}

	public static void SetScale(float scale)
	{
		m_largeGuiScale = scale;
	}
}
