using UnityEngine;

namespace UnityStandardAssets.ImageEffects;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Rendering/Sun Shafts")]
public class SunShafts : PostEffectsBase
{
	public enum SunShaftsResolution
	{
		Low,
		Normal,
		High
	}

	public enum ShaftsScreenBlendMode
	{
		Screen,
		Add
	}

	public SunShaftsResolution resolution = SunShaftsResolution.Normal;

	public ShaftsScreenBlendMode screenBlendMode;

	public Transform sunTransform;

	public int radialBlurIterations = 2;

	public Color sunColor = Color.white;

	public Color sunThreshold = new Color(0.87f, 0.74f, 0.65f);

	public float sunShaftBlurRadius = 2.5f;

	public float sunShaftIntensity = 1.15f;

	public float maxRadius = 0.75f;

	public bool useDepthTexture = true;

	public Shader sunShaftsShader;

	private Material sunShaftsMaterial;

	public Shader simpleClearShader;

	private Material simpleClearMaterial;

	private static readonly int s_blurRadius4 = Shader.PropertyToID("_BlurRadius4");

	private static readonly int s_sunPosition = Shader.PropertyToID("_SunPosition");

	private static readonly int s_sunThreshold = Shader.PropertyToID("_SunThreshold");

	private static readonly int s_skybox = Shader.PropertyToID("_Skybox");

	private static readonly int s_sunColor = Shader.PropertyToID("_SunColor");

	private static readonly int s_colorBuffer = Shader.PropertyToID("_ColorBuffer");

	private static readonly Vector4 s_oneOneZeroZero = new Vector4(1f, 1f, 0f, 0f);

	private static Vector4 s_ofsZero = new Vector4(0f, 0f, 0f, 0f);

	private static readonly Vector3 s_halfHalfZero = new Vector3(0.5f, 0.5f, 0f);

	public override bool CheckResources()
	{
		CheckSupport(useDepthTexture);
		sunShaftsMaterial = CheckShaderAndCreateMaterial(sunShaftsShader, sunShaftsMaterial);
		simpleClearMaterial = CheckShaderAndCreateMaterial(simpleClearShader, simpleClearMaterial);
		if (!isSupported)
		{
			ReportAutoDisable();
		}
		return isSupported;
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!CheckResources())
		{
			Graphics.Blit(source, destination);
			return;
		}
		if (useDepthTexture)
		{
			GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
		}
		int num = 4;
		if (resolution == SunShaftsResolution.Normal)
		{
			num = 2;
		}
		else if (resolution == SunShaftsResolution.High)
		{
			num = 1;
		}
		Vector4 value = (sunTransform ? GetComponent<Camera>().WorldToViewportPoint(sunTransform.position) : s_halfHalfZero);
		int width = source.width / num;
		int height = source.height / num;
		RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0);
		value.w = maxRadius;
		sunShaftsMaterial.SetVector(s_blurRadius4, s_oneOneZeroZero * sunShaftBlurRadius);
		sunShaftsMaterial.SetVector(s_sunPosition, value);
		sunShaftsMaterial.SetVector(s_sunThreshold, sunThreshold);
		if (!useDepthTexture)
		{
			RenderTextureFormat format = (GetComponent<Camera>().allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
			RenderTexture renderTexture = (RenderTexture.active = RenderTexture.GetTemporary(source.width, source.height, 0, format));
			GL.ClearWithSkybox(clearDepth: false, GetComponent<Camera>());
			sunShaftsMaterial.SetTexture(s_skybox, renderTexture);
			Graphics.Blit(source, temporary, sunShaftsMaterial, 3);
			RenderTexture.ReleaseTemporary(renderTexture);
		}
		else
		{
			Graphics.Blit(source, temporary, sunShaftsMaterial, 2);
		}
		DrawBorder(temporary, simpleClearMaterial);
		radialBlurIterations = Mathf.Clamp(radialBlurIterations, 1, 4);
		float num2 = sunShaftBlurRadius * 0.0013020834f;
		sunShaftsMaterial.SetVector(s_blurRadius4, new Vector4(num2, num2, 0f, 0f));
		sunShaftsMaterial.SetVector(s_sunPosition, value);
		for (int i = 0; i < radialBlurIterations; i++)
		{
			RenderTexture temporary3 = RenderTexture.GetTemporary(width, height, 0);
			Graphics.Blit(temporary, temporary3, sunShaftsMaterial, 1);
			RenderTexture.ReleaseTemporary(temporary);
			s_ofsZero.y = (s_ofsZero.x = sunShaftBlurRadius * (((float)i * 2f + 1f) * 6f) / 768f);
			sunShaftsMaterial.SetVector(s_blurRadius4, s_ofsZero);
			temporary = RenderTexture.GetTemporary(width, height, 0);
			Graphics.Blit(temporary3, temporary, sunShaftsMaterial, 1);
			RenderTexture.ReleaseTemporary(temporary3);
			s_ofsZero.y = (s_ofsZero.x = sunShaftBlurRadius * (((float)i * 2f + 2f) * 6f) / 768f);
			sunShaftsMaterial.SetVector(s_blurRadius4, s_ofsZero);
		}
		if (value.z >= 0f)
		{
			sunShaftsMaterial.SetColor(s_sunColor, sunColor * sunShaftIntensity);
		}
		else
		{
			sunShaftsMaterial.SetVector(s_sunColor, Vector4.zero);
		}
		sunShaftsMaterial.SetTexture(s_colorBuffer, temporary);
		Graphics.Blit(source, destination, sunShaftsMaterial, (screenBlendMode != 0) ? 4 : 0);
		RenderTexture.ReleaseTemporary(temporary);
	}
}
