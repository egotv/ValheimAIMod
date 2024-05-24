using System;
using UnityEngine;

namespace Valheim.SettingsGui;

[CreateAssetMenu(fileName = "New device quality settings", menuName = "Device Quality Settings")]
public class DeviceQualitySettings : ScriptableObject
{
	[SerializeField]
	protected GraphicsQualityMode m_graphicsQualityMode;

	[SerializeField]
	protected PlatformHardware m_platformHardware;

	[SerializeField]
	protected string m_nameTextId;

	[SerializeField]
	protected string m_descriptionTextId;

	[SerializeField]
	protected bool m_default;

	[SerializeField]
	protected bool m_fixedResolution;

	[SerializeField]
	protected int m_resolutionWidth = 1920;

	[SerializeField]
	protected int m_resolutionHeight = 1080;

	[SerializeField]
	[Range(-1f, 361f)]
	protected int m_fpsLimit = 60;

	[SerializeField]
	protected bool m_vsync;

	[SerializeField]
	protected bool m_vegetationChangeable = true;

	[SerializeField]
	[Range(1f, 3f)]
	protected int m_vegetation;

	[SerializeField]
	protected bool m_lightsChangeable = true;

	[SerializeField]
	[Range(0f, 2f)]
	protected int m_lights;

	[SerializeField]
	protected bool m_lodChangeable = true;

	[SerializeField]
	[Range(0f, 3f)]
	protected int m_lod;

	[SerializeField]
	protected bool m_shadowQualityChangeable = true;

	[SerializeField]
	[Range(0f, 2f)]
	protected int m_shadowQuality;

	[SerializeField]
	protected bool m_pointLightsChangeable = true;

	[SerializeField]
	[Range(0f, 3f)]
	protected int m_pointLights;

	[SerializeField]
	protected bool m_pointLightShadowsChangeable = true;

	[SerializeField]
	[Range(0f, 3f)]
	protected int m_pointLightShadows;

	[SerializeField]
	protected bool m_renderScaleChangeable = true;

	[SerializeField]
	[Range(0f, 20f)]
	protected int m_renderScale;

	public const int c_renderScaleMaxInt = 20;

	[SerializeField]
	protected bool m_ssaoChangeable = true;

	[SerializeField]
	protected bool m_ssao;

	[SerializeField]
	protected bool m_tesselationChangeable = true;

	[SerializeField]
	protected bool m_tesselation;

	[SerializeField]
	protected bool m_distantShadowsChangeable = true;

	[SerializeField]
	protected bool m_distantShadows;

	[SerializeField]
	protected bool m_softParticlesChangeable = true;

	[SerializeField]
	protected bool m_softParticles;

	[SerializeField]
	protected bool m_antiAliasingChangeable = true;

	[SerializeField]
	protected bool m_antiAliasing;

	[SerializeField]
	protected bool m_detailedParticleSystemsChangeable = true;

	[SerializeField]
	protected bool m_detailedParticleSystems;

	[SerializeField]
	protected bool m_bloomChangeable = true;

	[SerializeField]
	protected bool m_bloom;

	[SerializeField]
	protected bool m_depthOfFieldChangeable = true;

	[SerializeField]
	protected bool m_depthOfField;

	[SerializeField]
	protected bool m_motionBlurChangeable = true;

	[SerializeField]
	protected bool m_motionBlur;

	[SerializeField]
	protected bool m_chromaticAberrationChangeable = true;

	[SerializeField]
	protected bool m_chromaticAberration;

	[SerializeField]
	protected bool m_sunShaftsChangeable = true;

	[SerializeField]
	protected bool m_sunShafts;

	public GraphicsQualityMode GraphicQualityMode
	{
		get
		{
			return m_graphicsQualityMode;
		}
		set
		{
			m_graphicsQualityMode = value;
		}
	}

	public PlatformHardware PlatformHardware => m_platformHardware;

	public bool IsDefault => m_default;

	public string NameTextId
	{
		get
		{
			return m_nameTextId;
		}
		set
		{
			if (m_graphicsQualityMode != GraphicsQualityMode.Custom)
			{
				throw new Exception($"Only values from {GraphicsQualityMode.Custom} are allowed to be set via script");
			}
			m_nameTextId = value;
		}
	}

	public string DescriptionTextId
	{
		get
		{
			return m_descriptionTextId;
		}
		set
		{
			if (m_graphicsQualityMode != GraphicsQualityMode.Custom)
			{
				throw new Exception($"Only values from {GraphicsQualityMode.Custom} are allowed to be set via script");
			}
			m_descriptionTextId = value;
		}
	}

	public bool FixedResolution
	{
		get
		{
			return m_fixedResolution;
		}
		set
		{
			if (m_graphicsQualityMode != GraphicsQualityMode.Custom)
			{
				throw new Exception($"Only values from {GraphicsQualityMode.Custom} are allowed to be set via script");
			}
			m_fixedResolution = value;
		}
	}

	public int ResolutionWidth
	{
		get
		{
			if (!m_fixedResolution)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_resolutionWidth", m_resolutionWidth);
			}
			return m_resolutionWidth;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_resolutionWidth = value;
			}
			else if (!m_fixedResolution)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_resolutionWidth", value);
				m_resolutionWidth = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public int ResolutionHeight
	{
		get
		{
			if (!m_fixedResolution)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_resolutionHeight", m_resolutionHeight);
			}
			return m_resolutionHeight;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_resolutionHeight = value;
			}
			else if (!m_fixedResolution)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_resolutionHeight", value);
				m_resolutionHeight = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public int FpsLimit
	{
		get
		{
			if (!m_fixedResolution)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_fpsLimit", m_fpsLimit);
			}
			return m_fpsLimit;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_fpsLimit = value;
			}
			else if (!m_fixedResolution)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_fpsLimit", value);
				m_fpsLimit = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool Vsync
	{
		get
		{
			if (!m_fixedResolution)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_vsync", m_vsync ? 1 : 0) == 1;
			}
			return m_vsync;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_vsync = value;
			}
			else if (!m_fixedResolution)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_vsync", value ? 1 : 0);
				m_vsync = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool VegetationChangeable => m_vegetationChangeable;

	public int Vegetation
	{
		get
		{
			if (m_vegetationChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_vegetation", m_vegetation);
			}
			return m_vegetation;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_vegetation = value;
			}
			else if (m_vegetationChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_vegetation", value);
				m_vegetation = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool LightsChangeable => m_lightsChangeable;

	public int Lights
	{
		get
		{
			if (m_lightsChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_lights", m_lights);
			}
			return m_lights;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_lights = value;
			}
			else if (m_lightsChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_lights", value);
				m_lights = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool LodChangeable => m_lodChangeable;

	public int Lod
	{
		get
		{
			if (m_lodChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_lod", m_lod);
			}
			return m_lod;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_lod = value;
			}
			else if (m_lodChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_lod", value);
				m_lod = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool ShadowQualityChangeable => m_shadowQualityChangeable;

	public int ShadowQuality
	{
		get
		{
			if (m_shadowQualityChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_shadowQuality", m_shadowQuality);
			}
			return m_shadowQuality;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_shadowQuality = value;
			}
			else if (m_shadowQualityChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_shadowQuality", value);
				m_shadowQuality = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool PointLightsChangeable => m_pointLightsChangeable;

	public int PointLights
	{
		get
		{
			if (m_pointLightsChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_pointLights", m_pointLights);
			}
			return m_pointLights;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_pointLights = value;
			}
			else if (m_pointLightsChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_pointLights", value);
				m_pointLights = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool PointLightShadowsChangeable => m_pointLightShadowsChangeable;

	public int PointLightShadows
	{
		get
		{
			if (m_pointLightShadowsChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_pointLightShadows", m_pointLightShadows);
			}
			return m_pointLightShadows;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_pointLightShadows = value;
			}
			else if (m_pointLightShadowsChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_pointLightShadows", value);
				m_pointLightShadows = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool RenderScaleChangeable => m_renderScaleChangeable;

	public int RenderScale
	{
		get
		{
			if (m_renderScaleChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_renderScale", m_renderScale);
			}
			return m_renderScale;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_renderScale = value;
			}
			else if (m_renderScaleChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_renderScale", value);
				m_renderScale = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool DistantShadowsChangeable => m_distantShadowsChangeable;

	public bool DistantShadows
	{
		get
		{
			if (m_distantShadowsChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_distantShadows", m_distantShadows ? 1 : 0) == 1;
			}
			return m_distantShadows;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_distantShadows = value;
			}
			else if (m_distantShadowsChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_distantShadows", value ? 1 : 0);
				m_distantShadows = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool TesselationChangeable => m_tesselationChangeable;

	public bool Tesselation
	{
		get
		{
			if (m_tesselationChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_tesselation", m_tesselation ? 1 : 0) == 1;
			}
			return m_tesselation;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_tesselation = value;
			}
			else if (m_tesselationChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_tesselation", value ? 1 : 0);
				m_tesselation = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool SSAOChangeable => m_ssaoChangeable;

	public bool SSAO
	{
		get
		{
			if (m_ssaoChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_ssao", m_ssao ? 1 : 0) == 1;
			}
			return m_ssao;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_ssao = value;
			}
			else if (m_ssaoChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_ssao", value ? 1 : 0);
				m_ssao = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool BloomChangeable => m_bloomChangeable;

	public bool Bloom
	{
		get
		{
			if (m_bloomChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_bloom", m_bloom ? 1 : 0) == 1;
			}
			return m_bloom;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_bloom = value;
			}
			else if (m_bloomChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_bloom", value ? 1 : 0);
				m_bloom = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool DepthOfFieldChangeable => m_depthOfFieldChangeable;

	public bool DepthOfField
	{
		get
		{
			if (m_depthOfFieldChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_depthOfField", m_depthOfField ? 1 : 0) == 1;
			}
			return m_depthOfField;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_depthOfField = value;
			}
			else if (m_depthOfFieldChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_depthOfField", value ? 1 : 0);
				m_depthOfField = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool MotionBlurChangeable => m_motionBlurChangeable;

	public bool MotionBlur
	{
		get
		{
			if (m_motionBlurChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_motionBlur", m_motionBlur ? 1 : 0) == 1;
			}
			return m_motionBlur;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_motionBlur = value;
			}
			else if (m_motionBlurChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_motionBlur", value ? 1 : 0);
				m_motionBlur = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool ChromaticAberrationChangeable => m_chromaticAberrationChangeable;

	public bool ChromaticAberration
	{
		get
		{
			if (m_chromaticAberrationChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_chromaticAberration", m_chromaticAberration ? 1 : 0) == 1;
			}
			return m_chromaticAberration;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_chromaticAberration = value;
			}
			else if (m_chromaticAberrationChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_chromaticAberration", value ? 1 : 0);
				m_chromaticAberration = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool SunShaftsChangeable => m_sunShaftsChangeable;

	public bool SunShafts
	{
		get
		{
			if (m_sunShaftsChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_sunShafts", m_sunShafts ? 1 : 0) == 1;
			}
			return m_sunShafts;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_sunShafts = value;
			}
			else if (m_sunShaftsChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_sunShafts", value ? 1 : 0);
				m_sunShafts = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool SoftParticlesChangeable => m_softParticlesChangeable;

	public bool SoftParticles
	{
		get
		{
			if (m_softParticlesChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_softParticles", m_softParticles ? 1 : 0) == 1;
			}
			return m_softParticles;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_softParticles = value;
			}
			else if (m_softParticlesChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_softParticles", value ? 1 : 0);
				m_softParticles = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool AntiAliasingChangeable => m_antiAliasingChangeable;

	public bool AntiAliasing
	{
		get
		{
			if (m_antiAliasingChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_antiAliasing", m_antiAliasing ? 1 : 0) == 1;
			}
			return m_antiAliasing;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_antiAliasing = value;
			}
			else if (m_antiAliasingChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_antiAliasing", value ? 1 : 0);
				m_antiAliasing = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	public bool DetailedParticleSystemsChangeable => m_detailedParticleSystemsChangeable;

	public bool DetailedParticleSystems
	{
		get
		{
			if (m_detailedParticleSystemsChangeable)
			{
				return PlatformPrefs.GetInt(m_graphicsQualityMode.ToString() + "_m_detailedParticleSystems", m_detailedParticleSystems ? 1 : 0) == 1;
			}
			return m_detailedParticleSystems;
		}
		set
		{
			if (m_graphicsQualityMode == GraphicsQualityMode.Custom)
			{
				m_detailedParticleSystems = value;
			}
			else if (m_detailedParticleSystemsChangeable)
			{
				PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_detailedParticleSystems", value ? 1 : 0);
				m_detailedParticleSystems = value;
			}
			else
			{
				Debug.Log($"Only values from {GraphicsQualityMode.Custom} or settings marked as changeable are allowed to be set via script. Ignored value for mode {m_graphicsQualityMode}");
			}
		}
	}

	internal void Apply()
	{
		PlatformPrefs.SetInt("FPSLimit", FpsLimit);
		PlatformPrefs.SetInt("VSync", Vsync ? 1 : 0);
		PlatformPrefs.SetInt("ClutterQuality", Vegetation);
		PlatformPrefs.SetInt("LodBias", Lod);
		PlatformPrefs.SetInt("Lights", Lights);
		PlatformPrefs.SetInt("ShadowQuality", ShadowQuality);
		PlatformPrefs.SetInt("PointLights", PointLights);
		PlatformPrefs.SetInt("PointLightShadows", PointLightShadows);
		PlatformPrefs.SetFloat("RenderScale", (float)RenderScale / 20f);
		PlatformPrefs.SetInt("DistantShadows", DistantShadows ? 1 : 0);
		PlatformPrefs.SetInt("Tesselation", Tesselation ? 1 : 0);
		PlatformPrefs.SetInt("SSAO", SSAO ? 1 : 0);
		PlatformPrefs.SetInt("Bloom", Bloom ? 1 : 0);
		PlatformPrefs.SetInt("DOF", DepthOfField ? 1 : 0);
		PlatformPrefs.SetInt("MotionBlur", MotionBlur ? 1 : 0);
		PlatformPrefs.SetInt("ChromaticAberration", ChromaticAberration ? 1 : 0);
		PlatformPrefs.SetInt("SunShafts", SunShafts ? 1 : 0);
		PlatformPrefs.SetInt("SoftPart", SoftParticles ? 1 : 0);
		PlatformPrefs.SetInt("AntiAliasing", AntiAliasing ? 1 : 0);
		PlatformPrefs.SetInt("DetailedParticleSystems", DetailedParticleSystems ? 1 : 0);
		if (m_fixedResolution)
		{
			PlatformPrefs.SetInt("FPSLimit", m_fpsLimit);
			Screen.SetResolution(m_resolutionWidth, m_resolutionHeight, FullScreenMode.FullScreenWindow, new RefreshRate
			{
				numerator = (uint)m_fpsLimit,
				denominator = 1u
			});
		}
		Settings.ApplyQualitySettings();
	}

	public void LoadFromPlatformPrefs()
	{
		m_fpsLimit = PlatformPrefs.GetInt("FPSLimit", -1);
		m_vsync = PlatformPrefs.GetInt("VSync") == 1;
		m_vegetation = PlatformPrefs.GetInt("ClutterQuality", 3);
		m_lod = PlatformPrefs.GetInt("LodBias", 2);
		m_lights = PlatformPrefs.GetInt("Lights", 2);
		m_shadowQuality = PlatformPrefs.GetInt("ShadowQuality", 2);
		m_pointLights = PlatformPrefs.GetInt("PointLights", 3);
		m_pointLightShadows = PlatformPrefs.GetInt("PointLightShadows", 2);
		m_renderScale = (int)Mathf.Round(PlatformPrefs.GetFloat("RenderScale", 1f) * 20f);
		m_distantShadows = PlatformPrefs.GetInt("DistantShadows", 1) == 1;
		m_tesselation = PlatformPrefs.GetInt("Tesselation", 1) == 1;
		m_ssao = PlatformPrefs.GetInt("SSAO", 1) == 1;
		m_bloom = PlatformPrefs.GetInt("Bloom", 1) == 1;
		m_depthOfField = PlatformPrefs.GetInt("DOF", 1) == 1;
		m_motionBlur = PlatformPrefs.GetInt("MotionBlur", 1) == 1;
		m_chromaticAberration = PlatformPrefs.GetInt("ChromaticAberration", 1) == 1;
		m_sunShafts = PlatformPrefs.GetInt("SunShafts", 1) == 1;
		m_softParticles = PlatformPrefs.GetInt("SoftPart", 1) == 1;
		m_antiAliasing = PlatformPrefs.GetInt("AntiAliasing", 1) == 1;
		m_detailedParticleSystems = PlatformPrefs.GetInt("DetailedParticleSystems", 1) == 1;
	}

	public bool IsSupportedOnHardware()
	{
		return true;
	}

	public void ClearChanged()
	{
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_vsync", m_vsync ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "_m_vegetation", m_vegetation);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_lod", m_lod);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_lights", m_lights);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_shadowQuality", m_shadowQuality);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_pointLights", m_pointLights);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_pointLightShadows", m_pointLightShadows);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_renderScale", m_renderScale);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_distantShadows", m_distantShadows ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_tesselation", m_tesselation ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_ssao", m_ssao ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_bloom", m_bloom ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_depthOfField", m_depthOfField ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_motionBlur", m_motionBlur ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_chromaticAberration", m_chromaticAberration ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_sunShafts", m_sunShafts ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_softParticles", m_softParticles ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_antiAliasing", m_antiAliasing ? 1 : 0);
		PlatformPrefs.SetInt(m_graphicsQualityMode.ToString() + "m_detailedParticleSystems", m_detailedParticleSystems ? 1 : 0);
	}
}
