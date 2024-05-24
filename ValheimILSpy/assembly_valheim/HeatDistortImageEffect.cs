using UnityEngine;

[ExecuteAlways]
public class HeatDistortImageEffect : MonoBehaviour
{
	private static readonly int s_intensity = Shader.PropertyToID("_Intensity");

	private static readonly int s_vignetteStrength = Shader.PropertyToID("_VignetteStrength");

	private static readonly int s_vignetteSmoothness = Shader.PropertyToID("_VignetteSmoothness");

	private static readonly int s_noiseTexture = Shader.PropertyToID("_NoiseTexture");

	private static readonly int s_distortionStrength = Shader.PropertyToID("_DistortionStrength");

	private static readonly int s_segments = Shader.PropertyToID("_Segments");

	private static readonly int s_stretch = Shader.PropertyToID("_Stretch");

	private static readonly int s_scrollspeed = Shader.PropertyToID("_ScrollSpeed");

	private static readonly int s_color = Shader.PropertyToID("_Color");

	private Material m_material;

	private bool m_initalized;

	[SerializeField]
	private Color m_color;

	[SerializeField]
	[Range(0f, 1f)]
	public float m_intensity;

	[SerializeField]
	[Range(-0.25f, 0.25f)]
	private float m_distortionStrength = 0.15f;

	[SerializeField]
	[Range(0f, 15f)]
	private float m_vignetteStrength = 1f;

	[SerializeField]
	[Range(0f, 1f)]
	private float m_vignetteSmoothness = 0.5f;

	[SerializeField]
	private Texture2D m_noiseTexture;

	[SerializeField]
	[Range(1f, 8f)]
	private int m_segments = 3;

	[SerializeField]
	[Range(0f, 8f)]
	private float m_stretch = 1f;

	[SerializeField]
	[Range(-1f, 1f)]
	private float m_scrollSpeed;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!m_initalized)
		{
			m_initalized = true;
			m_material = new Material(Shader.Find("Hidden/CameraHeatDistort"));
		}
		m_material.SetFloat(s_intensity, m_intensity);
		m_material.SetFloat(s_vignetteSmoothness, m_vignetteSmoothness);
		m_material.SetFloat(s_vignetteStrength, m_vignetteStrength);
		m_material.SetTexture(s_noiseTexture, m_noiseTexture);
		m_material.SetFloat(s_distortionStrength, m_distortionStrength);
		m_material.SetInt(s_segments, m_segments);
		m_material.SetFloat(s_stretch, m_stretch);
		m_material.SetFloat(s_scrollspeed, m_scrollSpeed);
		m_material.SetColor(s_color, m_color);
		Graphics.Blit(source, destination, m_material);
	}
}
