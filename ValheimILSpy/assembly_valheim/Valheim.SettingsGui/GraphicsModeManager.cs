using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Valheim.SettingsGui;

public static class GraphicsModeManager
{
	private const string GRAPHICS_QUALITY_MODE = "GraphicsQualityMode";

	private static bool _initialized;

	private static DeviceQualitySettings _currentDeviceSettings;

	private static List<GraphicsQualityMode> _graphicsQualityModes;

	private static Dictionary<GraphicsQualityMode, DeviceQualitySettings> _deviceQualitySettings;

	public static DeviceQualitySettings CurrentDeviceQualitySettings => _currentDeviceSettings;

	public static PlatformHardware ThisPlatformHardware
	{
		get
		{
			if (Settings.IsSteamRunningOnSteamDeck())
			{
				return PlatformHardware.SteamDeck;
			}
			return PlatformHardware.Standalone;
		}
	}

	public static List<GraphicsQualityMode> GraphicsQualityModes => _graphicsQualityModes;

	public static GraphicsQualityMode ActiveGraphicQualityMode
	{
		get
		{
			GraphicsQualityMode graphicsQualityMode = PlatformPrefs.GetInt("GraphicsQualityMode").ToGraphicQualityMode();
			if (GraphicsQualityModes.Contains(graphicsQualityMode))
			{
				return graphicsQualityMode;
			}
			return SetToDefault();
		}
		set
		{
			if (!GraphicsQualityModes.Contains(value))
			{
				Debug.LogWarning($"Graphics quality mode {value} not supported on platform hardware.");
				return;
			}
			PlatformPrefs.SetInt("GraphicsQualityMode", value.ToInt());
			ApplyMode(value);
		}
	}

	public static DeviceQualitySettings CustomDeviceQualitySettings
	{
		get
		{
			if (_deviceQualitySettings.Keys.Contains(GraphicsQualityMode.Custom))
			{
				return _deviceQualitySettings[GraphicsQualityMode.Custom];
			}
			DeviceQualitySettings deviceQualitySettings = ScriptableObject.CreateInstance<DeviceQualitySettings>();
			deviceQualitySettings.GraphicQualityMode = GraphicsQualityMode.Custom;
			deviceQualitySettings.name = "Custom";
			deviceQualitySettings.NameTextId = "$settings_quality_mode_custom";
			deviceQualitySettings.LoadFromPlatformPrefs();
			_deviceQualitySettings[GraphicsQualityMode.Custom] = deviceQualitySettings;
			return deviceQualitySettings;
		}
	}

	public static bool HasCustomInNonDev()
	{
		return true;
	}

	public static GraphicsQualityMode SetToDefault()
	{
		int num = -1;
		foreach (KeyValuePair<GraphicsQualityMode, DeviceQualitySettings> deviceQualitySetting in _deviceQualitySettings)
		{
			if (deviceQualitySetting.Value.IsDefault)
			{
				num = deviceQualitySetting.Key.ToInt();
				break;
			}
		}
		if (num < 0)
		{
			PlatformPrefs.SetInt("GraphicsQualityMode", GraphicsQualityMode.Custom.ToInt());
			num = CustomDeviceQualitySettings.GraphicQualityMode.ToInt();
		}
		PlatformPrefs.SetInt("GraphicsQualityMode", num);
		return num.ToGraphicQualityMode();
	}

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}
		_deviceQualitySettings = new Dictionary<GraphicsQualityMode, DeviceQualitySettings>();
		_graphicsQualityModes = new List<GraphicsQualityMode>();
		foreach (DeviceQualitySettings item in Resources.LoadAll<DeviceQualitySettings>("QualitySettings").ToList().FindAll((DeviceQualitySettings s) => s.PlatformHardware == ThisPlatformHardware))
		{
			_deviceQualitySettings[item.GraphicQualityMode] = item;
			if (item.GraphicQualityMode != GraphicsQualityMode.Constrained)
			{
				_graphicsQualityModes.Add(item.GraphicQualityMode);
			}
		}
		_graphicsQualityModes.Add(GraphicsQualityMode.Custom);
		if (PlatformPrefs.GetInt("GraphicsQualityMode", -1) < 0)
		{
			SetToDefault();
		}
		if (ApplyMode(ActiveGraphicQualityMode))
		{
			_initialized = true;
		}
		else
		{
			ZLog.LogError("Could not initialize Quality Settings");
		}
	}

	private static bool ApplyMode(GraphicsQualityMode mode)
	{
		Debug.Log($"Trying to set graphic mode to '{mode}'");
		if (!_deviceQualitySettings.Keys.Contains(mode) || !_deviceQualitySettings[mode].IsSupportedOnHardware())
		{
			Debug.Log($"Graphic mode '{mode}' is not or no longer supported. Fallback to default mode for platform...");
			mode = SetToDefault();
		}
		if (!_deviceQualitySettings.Keys.Contains(mode))
		{
			return false;
		}
		_currentDeviceSettings = _deviceQualitySettings[mode];
		_currentDeviceSettings.Apply();
		Debug.Log($"Graphic mode '{mode}' applied!");
		return true;
	}

	public static DeviceQualitySettings GetSettingsForMode(GraphicsQualityMode mode)
	{
		if (_deviceQualitySettings.Keys.Contains(mode))
		{
			return _deviceQualitySettings[mode];
		}
		if (mode == GraphicsQualityMode.Custom)
		{
			return CustomDeviceQualitySettings;
		}
		return null;
	}

	public static bool MatchesMode(GraphicsQualityMode mode, bool considerChangeable, int resolutionWidth, int resolutionHeight, int fpsLimit, bool vsync, int vegetation, int levelOfDetail, int lights, int shadowQuality, int pointLights, int pointLightShadows, int renderScale, bool distantShadows, bool tesselation, bool ssao, bool bloom, bool depthOfField, bool motionBlur, bool chromaticAberration, bool sunShafts, bool softParticles, bool antiAliasing, bool detailedParticleSystems)
	{
		DeviceQualitySettings settingsForMode = GetSettingsForMode(mode);
		if (settingsForMode == null)
		{
			return false;
		}
		if ((!settingsForMode.FixedResolution || (resolutionWidth == settingsForMode.ResolutionWidth && resolutionHeight == settingsForMode.ResolutionHeight && fpsLimit == settingsForMode.FpsLimit && vsync == settingsForMode.Vsync)) && (vegetation == settingsForMode.Vegetation || (considerChangeable && settingsForMode.VegetationChangeable)) && (levelOfDetail == settingsForMode.Lod || (considerChangeable && settingsForMode.LodChangeable)) && (lights == settingsForMode.Lights || (considerChangeable && settingsForMode.LightsChangeable)) && (shadowQuality == settingsForMode.ShadowQuality || (considerChangeable && settingsForMode.ShadowQualityChangeable)) && (pointLights == settingsForMode.PointLights || (considerChangeable && settingsForMode.PointLightsChangeable)) && (pointLightShadows == settingsForMode.PointLightShadows || (considerChangeable && settingsForMode.PointLightShadowsChangeable)) && (renderScale == settingsForMode.RenderScale || (considerChangeable && settingsForMode.RenderScaleChangeable)) && (distantShadows == settingsForMode.DistantShadows || (considerChangeable && settingsForMode.DistantShadowsChangeable)) && (tesselation == settingsForMode.Tesselation || (considerChangeable && settingsForMode.TesselationChangeable)) && (ssao == settingsForMode.SSAO || (considerChangeable && settingsForMode.SSAOChangeable)) && (bloom == settingsForMode.Bloom || (considerChangeable && settingsForMode.BloomChangeable)) && (chromaticAberration == settingsForMode.ChromaticAberration || (considerChangeable && settingsForMode.ChromaticAberrationChangeable)) && (sunShafts == settingsForMode.SunShafts || (considerChangeable && settingsForMode.SunShaftsChangeable)) && (softParticles == settingsForMode.SoftParticles || (considerChangeable && settingsForMode.SoftParticles)) && (antiAliasing == settingsForMode.AntiAliasing || (considerChangeable && settingsForMode.AntiAliasingChangeable)))
		{
			if (detailedParticleSystems != settingsForMode.DetailedParticleSystems)
			{
				if (considerChangeable)
				{
					return settingsForMode.DetailedParticleSystemsChangeable;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public static void SaveSettings(GraphicsQualityMode mode, int resolutionWidth, int resolutionHeight, int fpsLimit, bool Vsync, int vegetation, int levelOfDetail, int lights, int shadowQuality, int pointLights, int pointLightShadows, int renderScale, bool distantShadows, bool tesselation, bool ssao, bool bloom, bool depthOfField, bool motionBlur, bool chromaticAberration, bool sunShafts, bool softParticles, bool antiAliasing, bool detailedParticleSystems)
	{
		if (MatchesMode(mode, considerChangeable: false, resolutionWidth, resolutionHeight, fpsLimit, Vsync, vegetation, levelOfDetail, lights, shadowQuality, pointLights, pointLightShadows, renderScale, distantShadows, tesselation, ssao, bloom, depthOfField, motionBlur, chromaticAberration, sunShafts, softParticles, antiAliasing, detailedParticleSystems))
		{
			DeviceQualitySettings deviceQualitySettings = _deviceQualitySettings[mode];
			deviceQualitySettings.FpsLimit = fpsLimit;
			deviceQualitySettings.Vsync = Vsync;
			deviceQualitySettings.DepthOfField = depthOfField;
			deviceQualitySettings.MotionBlur = motionBlur;
			ActiveGraphicQualityMode = mode;
			SyncFieldsToModes(deviceQualitySettings);
		}
		else if (_graphicsQualityModes.Contains(GraphicsQualityMode.Custom))
		{
			CustomDeviceQualitySettings.ResolutionWidth = resolutionWidth;
			CustomDeviceQualitySettings.ResolutionHeight = resolutionHeight;
			CustomDeviceQualitySettings.FpsLimit = fpsLimit;
			CustomDeviceQualitySettings.Vsync = Vsync;
			CustomDeviceQualitySettings.Vegetation = vegetation;
			CustomDeviceQualitySettings.Lod = levelOfDetail;
			CustomDeviceQualitySettings.Lights = lights;
			CustomDeviceQualitySettings.ShadowQuality = shadowQuality;
			CustomDeviceQualitySettings.PointLights = pointLights;
			CustomDeviceQualitySettings.PointLightShadows = pointLightShadows;
			CustomDeviceQualitySettings.RenderScale = renderScale;
			CustomDeviceQualitySettings.DistantShadows = distantShadows;
			CustomDeviceQualitySettings.Tesselation = tesselation;
			CustomDeviceQualitySettings.SSAO = ssao;
			CustomDeviceQualitySettings.Bloom = bloom;
			CustomDeviceQualitySettings.DepthOfField = depthOfField;
			CustomDeviceQualitySettings.MotionBlur = motionBlur;
			CustomDeviceQualitySettings.ChromaticAberration = chromaticAberration;
			CustomDeviceQualitySettings.SunShafts = sunShafts;
			CustomDeviceQualitySettings.SoftParticles = softParticles;
			CustomDeviceQualitySettings.AntiAliasing = antiAliasing;
			CustomDeviceQualitySettings.DetailedParticleSystems = detailedParticleSystems;
			ActiveGraphicQualityMode = GraphicsQualityMode.Custom;
			SyncFieldsToModes(CustomDeviceQualitySettings);
		}
		else if (MatchesMode(mode, considerChangeable: true, resolutionWidth, resolutionHeight, fpsLimit, Vsync, vegetation, levelOfDetail, lights, shadowQuality, pointLights, pointLightShadows, renderScale, distantShadows, tesselation, ssao, bloom, depthOfField, motionBlur, chromaticAberration, sunShafts, softParticles, antiAliasing, detailedParticleSystems))
		{
			DeviceQualitySettings settingsForMode = GetSettingsForMode(mode);
			settingsForMode.FpsLimit = fpsLimit;
			settingsForMode.Vsync = Vsync;
			settingsForMode.Vegetation = vegetation;
			settingsForMode.Lod = levelOfDetail;
			settingsForMode.Lights = lights;
			settingsForMode.ShadowQuality = shadowQuality;
			settingsForMode.PointLights = pointLights;
			settingsForMode.PointLightShadows = pointLightShadows;
			settingsForMode.RenderScale = renderScale;
			settingsForMode.DistantShadows = distantShadows;
			settingsForMode.Tesselation = tesselation;
			settingsForMode.SSAO = ssao;
			settingsForMode.Bloom = bloom;
			settingsForMode.DepthOfField = depthOfField;
			settingsForMode.MotionBlur = motionBlur;
			settingsForMode.ChromaticAberration = chromaticAberration;
			settingsForMode.SunShafts = sunShafts;
			settingsForMode.SoftParticles = softParticles;
			settingsForMode.AntiAliasing = antiAliasing;
			settingsForMode.DetailedParticleSystems = detailedParticleSystems;
			ActiveGraphicQualityMode = mode;
			SyncFieldsToModes(settingsForMode);
		}
		else
		{
			Debug.LogError("This shouldn't happen");
		}
	}

	private static void SyncFieldsToModes(DeviceQualitySettings fromSettings)
	{
		foreach (KeyValuePair<GraphicsQualityMode, DeviceQualitySettings> deviceQualitySetting in _deviceQualitySettings)
		{
			if (deviceQualitySetting.Key != fromSettings.GraphicQualityMode && deviceQualitySetting.Key != GraphicsQualityMode.Constrained)
			{
				deviceQualitySetting.Value.Vsync = fromSettings.Vsync;
				deviceQualitySetting.Value.FpsLimit = fromSettings.FpsLimit;
				deviceQualitySetting.Value.MotionBlur = fromSettings.MotionBlur;
				deviceQualitySetting.Value.DepthOfField = fromSettings.DepthOfField;
			}
		}
	}

	public static void Reset()
	{
		ApplyMode(ActiveGraphicQualityMode);
	}
}
