using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Valheim.SettingsGui;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class ShieldDomeImageEffect : MonoBehaviour
{
	[Serializable]
	public struct ShieldDome
	{
		public Vector3 position;

		public float radius;

		[FormerlySerializedAs("health")]
		[Range(0f, 1f)]
		public float fuelFactor;

		public float lastHitTime;
	}

	private static class Uniforms
	{
		internal static readonly int _TopLeft = Shader.PropertyToID("_TopLeft");

		internal static readonly int _TopRight = Shader.PropertyToID("_TopRight");

		internal static readonly int _BottomLeft = Shader.PropertyToID("_BottomLeft");

		internal static readonly int _BottomRight = Shader.PropertyToID("_BottomRight");

		internal static readonly int _Smoothing = Shader.PropertyToID("_Smoothing");

		internal static readonly int _DepthFade = Shader.PropertyToID("_DepthFade");

		internal static readonly int _EdgeGlow = Shader.PropertyToID("_EdgeGlow");

		internal static readonly int _DrawDistance = Shader.PropertyToID("_DrawDistance");

		internal static readonly int _DomeBuffer = Shader.PropertyToID("_DomeBuffer");

		internal static readonly int _DomeCount = Shader.PropertyToID("_DomeCount");

		internal static readonly int _MaxSteps = Shader.PropertyToID("_MaxSteps");

		internal static readonly int _SurfaceDistance = Shader.PropertyToID("_SurfaceDistance");

		internal static readonly int _NormalBias = Shader.PropertyToID("_NormalBias");

		internal static readonly int _RefractStrength = Shader.PropertyToID("_RefractStrength");

		internal static readonly int _ShieldColorGradient = Shader.PropertyToID("_ShieldColorGradient");

		internal static readonly int _ShieldTime = Shader.PropertyToID("_ShieldTime");

		internal static readonly int _NoiseTexture = Shader.PropertyToID("_NoiseTexture");

		internal static readonly int _CurlNoiseTexture = Shader.PropertyToID("_CurlNoiseTexture");

		internal static readonly int _NoiseSize = Shader.PropertyToID("_NoiseSize");

		internal static readonly int _CurlNoiseSize = Shader.PropertyToID("_CurlNoiseSize");

		internal static readonly int _CurlNoiseStrength = Shader.PropertyToID("_CurlNoiseStrength");
	}

	private int m_ShieldDomeStride = 24;

	private Material m_effectMaterial;

	private Camera m_cam;

	private ComputeBuffer m_shieldDomeBuffer;

	private Texture2D m_gradientTex;

	private static Gradient s_staticGradient;

	public static float Smoothing;

	[Min(0.1f)]
	public float m_smoothing = 0.25f;

	public float m_depthFadeDistance = 3f;

	[Min(0.25f)]
	public float m_edgeGlowDistance = 1f;

	public Gradient m_shieldColorGradient;

	[Range(-10f, 10f)]
	public float m_refractStrength = 1f;

	private ShieldDome[] m_shieldDomes;

	private Dictionary<ShieldGenerator, ShieldDome> m_shieldDomeData = new Dictionary<ShieldGenerator, ShieldDome>();

	[Header("Textures")]
	public Texture2D m_noiseTexture;

	public float m_noiseSize = 15f;

	public Texture3D m_curlNoiseTexture;

	public float m_curlSize;

	public float m_curlStrength;

	[Header("Quality")]
	[Range(0f, 0.2f)]
	public float m_surfaceDistance = 0.001f;

	[Range(0f, 0.5f)]
	public float m_normalBias = 0.1f;

	public float m_drawDistance = 100f;

	private void Awake()
	{
		m_effectMaterial = new Material(Shader.Find("Hidden/ShieldDomePass"));
		m_gradientTex = new Texture2D(256, 1, TextureFormat.RGB24, mipChain: false);
		m_gradientTex.wrapMode = TextureWrapMode.Clamp;
		for (int i = 0; i < 256; i++)
		{
			Color color = m_shieldColorGradient.Evaluate((float)i / 256f);
			m_gradientTex.SetPixel(i, 0, color);
		}
		m_gradientTex.Apply();
		s_staticGradient = m_shieldColorGradient;
		Smoothing = m_smoothing;
	}

	[ImageEffectAllowedInSceneView]
	private void OnRenderImage(RenderTexture src, RenderTexture dest)
	{
		if (m_shieldDomes == null || m_shieldDomes.Length == 0)
		{
			Graphics.Blit(src, dest);
			return;
		}
		if (m_cam == null)
		{
			m_cam = Camera.main;
		}
		Vector3 direction = m_cam.ViewportPointToRay(new Vector3(0f, 1f, 0f)).direction;
		Vector3 direction2 = m_cam.ViewportPointToRay(new Vector3(1f, 1f, 0f)).direction;
		Vector3 direction3 = m_cam.ViewportPointToRay(new Vector3(0f, 0f, 0f)).direction;
		Vector3 direction4 = m_cam.ViewportPointToRay(new Vector3(1f, 0f, 0f)).direction;
		m_effectMaterial.SetVector(Uniforms._TopLeft, direction);
		m_effectMaterial.SetVector(Uniforms._TopRight, direction2);
		m_effectMaterial.SetVector(Uniforms._BottomLeft, direction3);
		m_effectMaterial.SetVector(Uniforms._BottomRight, direction4);
		m_effectMaterial.SetFloat(Uniforms._Smoothing, m_smoothing);
		m_effectMaterial.SetFloat(Uniforms._DepthFade, m_depthFadeDistance);
		m_effectMaterial.SetFloat(Uniforms._EdgeGlow, m_edgeGlowDistance);
		m_effectMaterial.SetFloat(Uniforms._DrawDistance, m_drawDistance);
		m_effectMaterial.SetTexture(Uniforms._ShieldColorGradient, m_gradientTex);
		m_effectMaterial.SetFloat(Uniforms._ShieldTime, Time.time);
		m_effectMaterial.SetTexture(Uniforms._NoiseTexture, m_noiseTexture);
		m_effectMaterial.SetTexture(Uniforms._CurlNoiseTexture, m_curlNoiseTexture);
		m_effectMaterial.SetFloat(Uniforms._NoiseSize, m_noiseSize);
		m_effectMaterial.SetFloat(Uniforms._CurlNoiseSize, m_curlSize);
		m_effectMaterial.SetFloat(Uniforms._CurlNoiseStrength, m_curlStrength);
		int value = 32 + GraphicsModeManager.CurrentDeviceQualitySettings.Lod * 32;
		m_effectMaterial.SetInt(Uniforms._MaxSteps, value);
		m_effectMaterial.SetFloat(Uniforms._SurfaceDistance, m_surfaceDistance);
		m_effectMaterial.SetFloat(Uniforms._NormalBias, m_normalBias);
		m_effectMaterial.SetFloat(Uniforms._RefractStrength, m_refractStrength);
		PrepareComputeBuffer();
		Graphics.Blit(src, dest, m_effectMaterial, 0);
	}

	private void PrepareComputeBuffer()
	{
		if (m_shieldDomes == null || m_shieldDomes.Length == 0)
		{
			m_shieldDomeBuffer?.Release();
			return;
		}
		if (m_shieldDomeBuffer == null || m_shieldDomeBuffer.count != m_shieldDomes.Length)
		{
			m_shieldDomeBuffer?.Release();
			m_shieldDomeBuffer = new ComputeBuffer(m_shieldDomes.Length, m_ShieldDomeStride, ComputeBufferType.Structured);
		}
		m_shieldDomeBuffer.SetData(m_shieldDomes);
		m_effectMaterial.SetBuffer(Uniforms._DomeBuffer, m_shieldDomeBuffer);
		m_effectMaterial.SetInt(Uniforms._DomeCount, m_shieldDomes.Length);
	}

	public void SetShieldData(ShieldGenerator shield, Vector3 position, float radius, float fuelFactor, float lastHitTime)
	{
		m_shieldDomeData[shield] = new ShieldDome
		{
			position = position,
			radius = radius,
			fuelFactor = fuelFactor,
			lastHitTime = lastHitTime
		};
		updateShieldData();
	}

	public void RemoveShield(ShieldGenerator shield)
	{
		if (m_shieldDomeData.Remove(shield))
		{
			updateShieldData();
		}
	}

	private void updateShieldData()
	{
		if (m_shieldDomes == null || m_shieldDomes.Length != m_shieldDomeData.Count)
		{
			m_shieldDomes = new ShieldDome[m_shieldDomeData.Count];
		}
		int num = 0;
		foreach (KeyValuePair<ShieldGenerator, ShieldDome> shieldDomeDatum in m_shieldDomeData)
		{
			m_shieldDomes[num++] = shieldDomeDatum.Value;
		}
	}

	private void OnDestroy()
	{
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(m_effectMaterial);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(m_effectMaterial);
		}
	}

	public static Color GetDomeColor(float fuelFactor)
	{
		return s_staticGradient.Evaluate(fuelFactor);
	}
}
