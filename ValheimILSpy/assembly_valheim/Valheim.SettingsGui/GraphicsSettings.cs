using System;
using System.Collections.Generic;
using System.Linq;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class GraphicsSettings : SettingsBase
{
	[SerializeField]
	private UIGroupHandler m_groupHandler;

	[SerializeField]
	private VerticalLayoutGroup m_verticalLayoutGroup;

	[SerializeField]
	private TMP_Text m_devBuildSettingsText;

	[SerializeField]
	private GameObject m_devBuildSettingFramePrefab;

	[SerializeField]
	private TMP_Text m_devGraphicsModeValuesText;

	[SerializeField]
	private TMP_Text m_devPlayerPrefsValuesText;

	[Header("Resolution")]
	[SerializeField]
	private GameObject m_resolutionRoot;

	[SerializeField]
	private GuiDropdown m_resolutionDropdown;

	[SerializeField]
	private Toggle m_fullscreenToggle;

	[SerializeField]
	private GuiButton m_testResolutionButton;

	[SerializeField]
	private GameObject m_resDialog;

	[SerializeField]
	private GameObject m_resListElement;

	[SerializeField]
	private RectTransform m_resListRoot;

	[SerializeField]
	private Scrollbar m_resListScroll;

	[SerializeField]
	private GameObject m_resSwitchDialog;

	[SerializeField]
	private TMP_Text m_resSwitchCountdown;

	[SerializeField]
	private GuiButton m_resolutionOk;

	[SerializeField]
	private Toggle m_vsyncToggle;

	[SerializeField]
	private TMP_Text m_vsyncRequiresRestartText;

	[SerializeField]
	private int m_minResWidth = 1280;

	[SerializeField]
	private int m_minResHeight = 720;

	[Header("Graphic Presets")]
	[SerializeField]
	private GameObject m_graphicPresetsRoot;

	[SerializeField]
	private TMP_Text m_graphicsMode;

	[SerializeField]
	private Button m_graphicPresetLeft;

	[SerializeField]
	private Button m_graphicPresetRight;

	[SerializeField]
	private TMP_Text m_graphicsModeDescr;

	[Header("Quality")]
	[SerializeField]
	private Slider m_vegetationSlider;

	[SerializeField]
	private TMP_Text m_vegetationText;

	[SerializeField]
	private Slider m_levelOfDetailSlider;

	[SerializeField]
	private TMP_Text m_levelOfDetailText;

	[SerializeField]
	private Slider m_lightsSlider;

	[SerializeField]
	private TMP_Text m_lightsText;

	[SerializeField]
	private Slider m_shadowQualitySlider;

	[SerializeField]
	private TMP_Text m_shadowQualityText;

	[SerializeField]
	private Slider m_pointLightsSlider;

	[SerializeField]
	private TMP_Text m_pointLightsText;

	[SerializeField]
	private Slider m_pointLightShadowsSlider;

	[SerializeField]
	private TMP_Text m_pointLightShadowsText;

	[SerializeField]
	private Slider m_fpsLimitSlider;

	[SerializeField]
	private TMP_Text m_fpsLimitText;

	[SerializeField]
	private Slider m_renderScaleSlider;

	[SerializeField]
	private TMP_Text m_renderScaleText;

	[Header("Graphics")]
	[SerializeField]
	private Toggle m_distantShadowsToggle;

	[SerializeField]
	private Toggle m_tesselationToggle;

	[SerializeField]
	private Toggle m_ssaoToggle;

	[SerializeField]
	private Toggle m_bloomToggle;

	[SerializeField]
	private Toggle m_depthOfFieldToggle;

	[SerializeField]
	private Toggle m_motionBlurToggle;

	[SerializeField]
	private Toggle m_chromaticAberrationToggle;

	[SerializeField]
	private Toggle m_sunShaftsToggle;

	[SerializeField]
	private Toggle m_softPartToggle;

	[SerializeField]
	private Toggle m_antialiasingToggle;

	[SerializeField]
	private Toggle m_detailedParticleSystemsToggle;

	private List<Resolution> m_resolutions = new List<Resolution>();

	private List<Resolution> m_resolutionOptions = new List<Resolution>();

	private bool m_oldFullscreen;

	private Resolution m_oldRes;

	private Resolution m_selectedRes;

	private float m_resCountdownTimer = 1f;

	private bool m_oldVsync;

	private bool m_destroyAfterResChange;

	private bool m_pauseUpdateChecks;

	private GraphicsQualityMode m_currentMode;

	private List<GraphicsQualityMode> m_graphicsQualityModes;

	private bool m_canCustomize;

	public override void FixBackButtonNavigation(Button backButton)
	{
		Selectable selectable = new List<Selectable>
		{
			m_detailedParticleSystemsToggle, m_antialiasingToggle, m_softPartToggle, m_sunShaftsToggle, m_chromaticAberrationToggle, m_motionBlurToggle, m_depthOfFieldToggle, m_bloomToggle, m_ssaoToggle, m_tesselationToggle,
			m_distantShadowsToggle, m_renderScaleSlider, m_pointLightsSlider, m_shadowQualitySlider, m_lightsSlider, m_levelOfDetailSlider, m_vegetationSlider, m_graphicPresetLeft, m_vsyncToggle, m_fpsLimitSlider,
			m_resolutionDropdown
		}.FirstOrDefault((Selectable t) => t.gameObject.activeSelf);
		SetNavigation(selectable, NavigationDirection.OnDown, backButton);
		SetNavigation(backButton, NavigationDirection.OnUp, selectable);
	}

	public override void FixOkButtonNavigation(Button okButton)
	{
		Selectable target = new List<Selectable>
		{
			m_detailedParticleSystemsToggle, m_antialiasingToggle, m_softPartToggle, m_sunShaftsToggle, m_chromaticAberrationToggle, m_motionBlurToggle, m_depthOfFieldToggle, m_bloomToggle, m_ssaoToggle, m_tesselationToggle,
			m_distantShadowsToggle, m_renderScaleSlider, m_pointLightsSlider, m_shadowQualitySlider, m_lightsSlider, m_levelOfDetailSlider, m_vegetationSlider, m_graphicPresetLeft, m_vsyncToggle, m_fpsLimitSlider,
			m_resolutionDropdown
		}.FirstOrDefault((Selectable t) => t.gameObject.activeSelf);
		SetNavigation(okButton, NavigationDirection.OnUp, target);
	}

	public override void LoadSettings()
	{
		m_pauseUpdateChecks = true;
		m_graphicsQualityModes = GraphicsModeManager.GraphicsQualityModes;
		bool flag = m_graphicsQualityModes.Count > 1;
		m_graphicPresetsRoot.SetActive(flag);
		m_graphicPresetLeft.interactable = flag;
		m_graphicPresetRight.interactable = flag;
		m_canCustomize = m_graphicsQualityModes.Contains(GraphicsQualityMode.Custom);
		m_oldRes = Screen.currentResolution;
		m_oldRes.width = Screen.width;
		m_oldRes.height = Screen.height;
		m_selectedRes = m_oldRes;
		FillResList();
		m_resDialog.SetActive(value: false);
		m_oldFullscreen = Screen.fullScreen;
		m_oldVsync = PlatformPrefs.GetInt("VSync") == 1;
		m_fullscreenToggle.isOn = m_oldFullscreen;
		Debug.Log(m_oldRes);
		m_renderScaleSlider.minValue = 1f;
		m_renderScaleSlider.maxValue = 21f;
		m_fpsLimitSlider.minValue = 30f;
		m_fpsLimitSlider.maxValue = 361f;
		SetGraphicsMode(GraphicsModeManager.ActiveGraphicQualityMode, init: true);
		Settings instance = Settings.instance;
		instance.SharedSettingsChanged = (Action<string, int>)Delegate.Remove(instance.SharedSettingsChanged, new Action<string, int>(SharedSettingsChanged));
		Settings instance2 = Settings.instance;
		instance2.SharedSettingsChanged = (Action<string, int>)Delegate.Combine(instance2.SharedSettingsChanged, new Action<string, int>(SharedSettingsChanged));
		Settings.instance.SettingsPopupDestroyed -= SettingsPopupDestroyed;
		Settings.instance.SettingsPopupDestroyed += SettingsPopupDestroyed;
		m_pauseUpdateChecks = false;
		OnResolutionChanged();
		OnQualityChanged();
		m_verticalLayoutGroup.spacing = ((m_resolutionRoot.activeSelf && flag) ? 8 : ((!m_resolutionRoot.activeSelf && !flag) ? 30 : 20));
	}

	private void SettingsPopupDestroyed()
	{
		Settings instance = Settings.instance;
		instance.SharedSettingsChanged = (Action<string, int>)Delegate.Remove(instance.SharedSettingsChanged, new Action<string, int>(SharedSettingsChanged));
		Settings.instance.SettingsPopupDestroyed -= SettingsPopupDestroyed;
	}

	public override void ResetSettings()
	{
		RevertMode();
		SetGraphicsMode(GraphicsModeManager.ActiveGraphicQualityMode);
	}

	public override void SaveSettings()
	{
		DeviceQualitySettings settingsForMode = GraphicsModeManager.GetSettingsForMode(m_currentMode);
		if (!settingsForMode.IsSupportedOnHardware())
		{
			Saved?.Invoke();
			return;
		}
		GraphicsModeManager.SaveSettings(renderScale: (!settingsForMode.RenderScaleChangeable) ? settingsForMode.RenderScale : MapRenderScaleVisualToSaved((int)m_renderScaleSlider.value), mode: m_currentMode, resolutionWidth: m_selectedRes.width, resolutionHeight: m_selectedRes.height, fpsLimit: (int)m_fpsLimitSlider.value, Vsync: m_vsyncToggle.isOn, vegetation: (int)m_vegetationSlider.value, levelOfDetail: (int)m_levelOfDetailSlider.value, lights: (int)m_lightsSlider.value, shadowQuality: (int)m_shadowQualitySlider.value, pointLights: (int)m_pointLightsSlider.value, pointLightShadows: (int)m_pointLightShadowsSlider.value, distantShadows: m_distantShadowsToggle.isOn, tesselation: m_tesselationToggle.isOn, ssao: m_ssaoToggle.isOn, bloom: m_bloomToggle.isOn, depthOfField: m_depthOfFieldToggle.isOn, motionBlur: m_motionBlurToggle.isOn, chromaticAberration: m_chromaticAberrationToggle.isOn, sunShafts: m_sunShaftsToggle.isOn, softParticles: m_softPartToggle.isOn, antiAliasing: m_antialiasingToggle.isOn, detailedParticleSystems: m_detailedParticleSystemsToggle.isOn);
		if (!settingsForMode.FixedResolution && ResolutionSettingsChanged())
		{
			m_destroyAfterResChange = true;
			OnTestResolution();
		}
		else
		{
			Saved?.Invoke();
		}
	}

	private void SharedSettingsChanged(string setting, int value)
	{
		if (setting == "MotionBlur" && m_motionBlurToggle.isOn != (value == 1))
		{
			m_motionBlurToggle.isOn = value == 1;
		}
		else if (setting == "DepthOfField" && m_depthOfFieldToggle.isOn != (value == 1))
		{
			m_depthOfFieldToggle.isOn = value == 1;
		}
	}

	public void OnMotionBlurChanged()
	{
		Settings.instance.SharedSettingsChanged?.Invoke("MotionBlur", m_motionBlurToggle.isOn ? 1 : 0);
	}

	public void OnDepthOfFieldChanged()
	{
		Settings.instance.SharedSettingsChanged?.Invoke("DepthOfField", m_depthOfFieldToggle.isOn ? 1 : 0);
	}

	public void OnQualityChanged()
	{
		if (!m_pauseUpdateChecks)
		{
			m_vegetationText.text = GetQualityText(Math.Max(0, (int)m_vegetationSlider.value - 1));
			m_levelOfDetailText.text = GetQualityText((int)m_levelOfDetailSlider.value);
			m_lightsText.text = GetQualityText((int)m_lightsSlider.value);
			m_shadowQualityText.text = GetQualityText((int)m_shadowQualitySlider.value);
			int pointLightLimit = Settings.GetPointLightLimit((int)m_pointLightsSlider.value);
			m_pointLightsText.text = GetQualityText((int)m_pointLightsSlider.value) + " (" + ((pointLightLimit < 0) ? Localization.instance.Localize("$settings_infinite") : pointLightLimit.ToString()) + ")";
			int pointLightShadowLimit = Settings.GetPointLightShadowLimit((int)m_pointLightShadowsSlider.value);
			m_pointLightShadowsText.text = GetQualityText((int)m_pointLightShadowsSlider.value) + " (" + ((pointLightShadowLimit < 0) ? Localization.instance.Localize("$settings_infinite") : pointLightShadowLimit.ToString()) + ")";
			m_fpsLimitText.text = ((m_fpsLimitSlider.value > 360f) ? Localization.instance.Localize("$settings_unlimited") : m_fpsLimitSlider.value.ToString());
			m_renderScaleText.text = ((m_renderScaleSlider.value >= m_renderScaleSlider.maxValue) ? Localization.instance.Localize("$settings_native") : ((m_renderScaleSlider.value == m_renderScaleSlider.maxValue - 1f) ? Localization.instance.Localize("$settings_automatic") : (m_renderScaleSlider.value / (m_renderScaleSlider.maxValue - 1f)).ToString("0%")));
			UpdateModeStepperInfo();
			m_vsyncRequiresRestartText.gameObject.SetActive(m_oldVsync == m_vsyncToggle.isOn && false);
		}
	}

	private bool MatchesModeSettings(GraphicsQualityMode mode, bool considerChangeable)
	{
		return GraphicsModeManager.MatchesMode(mode, considerChangeable, m_selectedRes.width, m_selectedRes.height, (int)m_fpsLimitSlider.value, m_vsyncToggle.isOn, (int)m_vegetationSlider.value, (int)m_levelOfDetailSlider.value, (int)m_lightsSlider.value, (int)m_shadowQualitySlider.value, (int)m_pointLightsSlider.value, (int)m_pointLightShadowsSlider.value, MapRenderScaleVisualToSaved((int)m_renderScaleSlider.value), m_distantShadowsToggle.isOn, m_tesselationToggle.isOn, m_ssaoToggle.isOn, m_bloomToggle.isOn, m_depthOfFieldToggle.isOn, m_motionBlurToggle.isOn, m_chromaticAberrationToggle.isOn, m_sunShaftsToggle.isOn, m_softPartToggle.isOn, m_antialiasingToggle.isOn, m_detailedParticleSystemsToggle.isOn);
	}

	public void OnResolutionChanged()
	{
		if (!m_pauseUpdateChecks)
		{
			UpdateModeStepperInfo();
			m_testResolutionButton.interactable = ResolutionSettingsChanged();
			m_testResolutionButton.gameObject.SetActive(m_testResolutionButton.interactable);
		}
	}

	private bool ResolutionSettingsChanged()
	{
		if (m_oldRes.width == m_selectedRes.width && m_oldRes.height == m_selectedRes.height && m_oldRes.refreshRateRatio.value == m_selectedRes.refreshRateRatio.value)
		{
			return m_oldFullscreen != m_fullscreenToggle.isOn;
		}
		return true;
	}

	private void Awake()
	{
		m_resolutionDropdown.OnExpandedStateChange += OnResolutionDropdownExpanded;
		m_resolutionDropdown.onValueChanged.AddListener(OnResolutionSelected);
	}

	private void OnDestroy()
	{
		m_resolutionDropdown.OnExpandedStateChange -= OnResolutionDropdownExpanded;
		m_resolutionDropdown.onValueChanged.RemoveListener(OnResolutionSelected);
	}

	private void Update()
	{
		if (m_resSwitchDialog.activeSelf)
		{
			m_resCountdownTimer -= Time.unscaledDeltaTime;
			m_resSwitchCountdown.text = Mathf.CeilToInt(m_resCountdownTimer).ToString();
			if (m_resCountdownTimer <= 0f || ZInput.GetButtonDown("JoyBack") || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape))
			{
				m_resSwitchDialog.SetActive(value: false);
				RevertMode();
			}
		}
	}

	private static string GetQualityText(int level)
	{
		return level switch
		{
			1 => Localization.instance.Localize("[$settings_medium]"), 
			2 => Localization.instance.Localize("[$settings_high]"), 
			3 => Localization.instance.Localize("[$settings_veryhigh]"), 
			_ => Localization.instance.Localize("[$settings_low]"), 
		};
	}

	private void SetGraphicsMode(GraphicsQualityMode mode, bool init = false)
	{
		m_currentMode = mode;
		m_pauseUpdateChecks = true;
		bool flag = false;
		DeviceQualitySettings settingsForMode = GraphicsModeManager.GetSettingsForMode(mode);
		if (init || settingsForMode.FixedResolution)
		{
			m_fpsLimitSlider.value = (((float)settingsForMode.FpsLimit < m_fpsLimitSlider.minValue) ? m_fpsLimitSlider.maxValue : ((float)settingsForMode.FpsLimit));
			m_vsyncToggle.isOn = settingsForMode.Vsync;
		}
		foreach (Resolution resolution in m_resolutions)
		{
			if (settingsForMode.FixedResolution && resolution.width == settingsForMode.ResolutionWidth && resolution.height == settingsForMode.ResolutionHeight)
			{
				m_selectedRes = resolution;
				flag = true;
				break;
			}
		}
		m_vegetationSlider.value = settingsForMode.Vegetation;
		m_levelOfDetailSlider.value = settingsForMode.Lod;
		m_lightsSlider.value = settingsForMode.Lights;
		m_shadowQualitySlider.value = settingsForMode.ShadowQuality;
		m_pointLightsSlider.value = settingsForMode.PointLights;
		m_pointLightShadowsSlider.value = settingsForMode.PointLightShadows;
		m_renderScaleSlider.value = MapRenderScaleSavedToVisual(settingsForMode.RenderScale);
		m_distantShadowsToggle.isOn = settingsForMode.DistantShadows;
		m_tesselationToggle.isOn = settingsForMode.Tesselation;
		m_ssaoToggle.isOn = settingsForMode.SSAO;
		m_bloomToggle.isOn = settingsForMode.Bloom;
		m_depthOfFieldToggle.isOn = settingsForMode.DepthOfField;
		m_motionBlurToggle.isOn = settingsForMode.MotionBlur;
		m_chromaticAberrationToggle.isOn = settingsForMode.ChromaticAberration;
		m_sunShaftsToggle.isOn = settingsForMode.SunShafts;
		m_softPartToggle.isOn = settingsForMode.SoftParticles;
		m_antialiasingToggle.isOn = settingsForMode.AntiAliasing;
		m_detailedParticleSystemsToggle.isOn = settingsForMode.DetailedParticleSystems;
		if (m_canCustomize)
		{
			m_pauseUpdateChecks = false;
			OnQualityChanged();
			if (flag)
			{
				OnResolutionChanged();
			}
			return;
		}
		m_devBuildSettingsText.gameObject.SetActive(value: false);
		m_resolutionRoot.SetActive(!settingsForMode.FixedResolution);
		m_resolutionDropdown.gameObject.SetActive(!settingsForMode.FixedResolution);
		m_fpsLimitSlider.gameObject.SetActive(!settingsForMode.FixedResolution);
		m_vsyncToggle.gameObject.SetActive(!settingsForMode.FixedResolution);
		m_vegetationSlider.gameObject.SetActive(settingsForMode.VegetationChangeable);
		m_levelOfDetailSlider.gameObject.SetActive(settingsForMode.LodChangeable);
		m_lightsSlider.gameObject.SetActive(settingsForMode.LightsChangeable);
		m_shadowQualitySlider.gameObject.SetActive(settingsForMode.ShadowQualityChangeable);
		m_pointLightsSlider.gameObject.SetActive(settingsForMode.PointLightsChangeable);
		m_pointLightShadowsSlider.gameObject.SetActive(settingsForMode.PointLightShadowsChangeable);
		m_renderScaleSlider.gameObject.SetActive(settingsForMode.RenderScaleChangeable);
		m_distantShadowsToggle.transform.parent.gameObject.SetActive(settingsForMode.DistantShadowsChangeable);
		m_distantShadowsToggle.gameObject.SetActive(settingsForMode.DistantShadowsChangeable);
		m_tesselationToggle.transform.parent.gameObject.SetActive(settingsForMode.TesselationChangeable);
		m_tesselationToggle.gameObject.SetActive(settingsForMode.TesselationChangeable);
		m_ssaoToggle.transform.parent.gameObject.SetActive(settingsForMode.SSAOChangeable);
		m_ssaoToggle.gameObject.SetActive(settingsForMode.SSAOChangeable);
		m_bloomToggle.transform.parent.gameObject.SetActive(settingsForMode.BloomChangeable);
		m_bloomToggle.gameObject.SetActive(settingsForMode.BloomChangeable);
		m_depthOfFieldToggle.transform.parent.gameObject.SetActive(settingsForMode.DepthOfFieldChangeable);
		m_depthOfFieldToggle.gameObject.SetActive(settingsForMode.DepthOfFieldChangeable);
		m_motionBlurToggle.transform.parent.gameObject.SetActive(settingsForMode.MotionBlurChangeable);
		m_motionBlurToggle.gameObject.SetActive(settingsForMode.MotionBlurChangeable);
		m_chromaticAberrationToggle.transform.parent.gameObject.SetActive(settingsForMode.ChromaticAberrationChangeable);
		m_chromaticAberrationToggle.gameObject.SetActive(settingsForMode.ChromaticAberrationChangeable);
		m_sunShaftsToggle.transform.parent.gameObject.SetActive(settingsForMode.SunShaftsChangeable);
		m_sunShaftsToggle.gameObject.SetActive(settingsForMode.SunShaftsChangeable);
		m_softPartToggle.transform.parent.gameObject.SetActive(settingsForMode.SoftParticlesChangeable);
		m_softPartToggle.gameObject.SetActive(settingsForMode.SoftParticlesChangeable);
		m_antialiasingToggle.transform.parent.gameObject.SetActive(settingsForMode.AntiAliasingChangeable);
		m_antialiasingToggle.gameObject.SetActive(settingsForMode.AntiAliasingChangeable);
		m_detailedParticleSystemsToggle.transform.parent.gameObject.SetActive(settingsForMode.DetailedParticleSystemsChangeable);
		m_detailedParticleSystemsToggle.gameObject.SetActive(settingsForMode.DetailedParticleSystemsChangeable);
		foreach (Selectable item in new List<Selectable>
		{
			m_graphicPresetLeft, m_resolutionDropdown, m_fpsLimitSlider, m_vsyncToggle, m_vegetationSlider, m_levelOfDetailSlider, m_lightsSlider, m_shadowQualitySlider, m_pointLightsSlider, m_renderScaleSlider,
			m_distantShadowsToggle, m_tesselationToggle, m_ssaoToggle, m_bloomToggle, m_depthOfFieldToggle, m_motionBlurToggle, m_chromaticAberrationToggle, m_sunShaftsToggle, m_softPartToggle, m_antialiasingToggle,
			m_detailedParticleSystemsToggle
		})
		{
			if (item.gameObject.activeSelf && item.interactable)
			{
				m_groupHandler.m_defaultElement = item.gameObject;
				break;
			}
		}
		List<Selectable> list = new List<Selectable>
		{
			m_resolutionDropdown, m_fpsLimitSlider, m_vsyncToggle, m_graphicPresetLeft, m_vegetationSlider, m_levelOfDetailSlider, m_lightsSlider, m_shadowQualitySlider, m_pointLightsSlider, m_renderScaleSlider,
			m_distantShadowsToggle, m_tesselationToggle, m_ssaoToggle, m_bloomToggle, m_depthOfFieldToggle, m_motionBlurToggle, m_chromaticAberrationToggle, m_sunShaftsToggle, m_softPartToggle, m_antialiasingToggle,
			m_detailedParticleSystemsToggle
		};
		List<Selectable> list2 = new List<Selectable>(list);
		while (list2.Count > 10)
		{
			Selectable selectable = list2[0];
			list2.RemoveAt(0);
			SetNavigationToFirstActive(selectable, NavigationDirection.OnDown, list2);
		}
		SetNavigationToFirstActive(m_fullscreenToggle, NavigationDirection.OnDown, list);
		SetNavigationToFirstActive(m_testResolutionButton, NavigationDirection.OnDown, list);
		SetNavigationToFirstActive(m_graphicPresetRight, NavigationDirection.OnDown, list.GetRange(4, list.Count - 5));
		list2 = new List<Selectable>(list.GetRange(0, list.Count - 10));
		list2.Reverse();
		while (list2.Count > 0)
		{
			Selectable selectable2 = list2[0];
			list2.RemoveAt(0);
			SetNavigationToFirstActive(selectable2, NavigationDirection.OnUp, list2);
		}
		list2 = new List<Selectable>(list.GetRange(0, list.Count - 10));
		list2.Reverse();
		List<Selectable> list3 = new List<Selectable>(from s in list.GetRange(list.Count - 10, 10)
			where s.gameObject.activeSelf
			select s);
		int num = 0;
		int count = list3.Count;
		foreach (Selectable item2 in list3)
		{
			if (num < 3)
			{
				SetNavigationToFirstActive(item2, NavigationDirection.OnUp, list2);
			}
			else
			{
				SetNavigation(item2, NavigationDirection.OnUp, list3[num - 3]);
			}
			if (num < count - 1)
			{
				SetNavigation(item2, NavigationDirection.OnRight, list3[num + 1]);
			}
			if (num < count - 3)
			{
				SetNavigation(item2, NavigationDirection.OnDown, list3[num + 3]);
			}
			if (num > 0)
			{
				SetNavigation(item2, NavigationDirection.OnLeft, list3[num - 1]);
			}
			num++;
		}
		m_pauseUpdateChecks = false;
		OnQualityChanged();
		if (flag)
		{
			OnResolutionChanged();
		}
	}

	private void UpdateModeStepperInfo()
	{
		DeviceQualitySettings settingsForMode = GraphicsModeManager.GetSettingsForMode(m_currentMode);
		if (settingsForMode.IsSupportedOnHardware())
		{
			bool flag = m_currentMode != GraphicsQualityMode.Custom && m_graphicsQualityModes.Contains(GraphicsQualityMode.Custom) && !MatchesModeSettings(m_currentMode, considerChangeable: false);
			m_graphicsMode.alpha = 1f;
			m_graphicsMode.text = Localization.instance.Localize(settingsForMode.NameTextId) + (flag ? "*" : "");
			m_graphicsModeDescr.text = Localization.instance.Localize(flag ? "$settings_quality_mode_customized" : settingsForMode.DescriptionTextId);
		}
		else
		{
			m_graphicsMode.alpha = 0.25f;
			m_graphicsMode.text = Localization.instance.Localize(settingsForMode.NameTextId) + "*";
			m_graphicsModeDescr.text = Localization.instance.Localize("$settings_quality_mode_not_supported");
		}
	}

	private GraphicsQualityMode NextGraphicsMode(GraphicsQualityMode mode)
	{
		int num = m_graphicsQualityModes.IndexOf(mode);
		if (num < 0 || m_graphicsQualityModes.Count == 0)
		{
			if (m_graphicsQualityModes.Count <= 0)
			{
				return mode;
			}
			return m_graphicsQualityModes[0];
		}
		if (num >= m_graphicsQualityModes.Count - 1)
		{
			return m_graphicsQualityModes[0];
		}
		return m_graphicsQualityModes[num + 1];
	}

	private GraphicsQualityMode PrevGraphicsMode(GraphicsQualityMode mode)
	{
		int num = m_graphicsQualityModes.IndexOf(mode);
		if (num < 0 || m_graphicsQualityModes.Count == 0)
		{
			if (m_graphicsQualityModes.Count <= 0)
			{
				return mode;
			}
			return m_graphicsQualityModes[0];
		}
		if (num == 0)
		{
			return m_graphicsQualityModes[m_graphicsQualityModes.Count - 1];
		}
		return m_graphicsQualityModes[num - 1];
	}

	public void OnGraphicPresetLeft()
	{
		SetGraphicsMode(PrevGraphicsMode(m_currentMode));
	}

	public void OnGraphicPresetRight()
	{
		SetGraphicsMode(NextGraphicsMode(m_currentMode));
	}

	private void OnResolutionDropdownExpanded(bool expanded)
	{
		Settings.instance.BlockNavigation(expanded);
	}

	private void UpdateValidResolutions()
	{
		Resolution[] array = Screen.resolutions;
		if (array.Length == 0)
		{
			array = new Resolution[1] { m_oldRes };
		}
		m_resolutions.Clear();
		Resolution[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Resolution item = array2[i];
			if ((item.width >= m_minResWidth && item.height >= m_minResHeight) || item.width == m_oldRes.width || item.height == m_oldRes.height)
			{
				m_resolutions.Add(item);
			}
		}
		foreach (GraphicsQualityMode graphicsQualityMode in m_graphicsQualityModes)
		{
			DeviceQualitySettings settingsForMode = GraphicsModeManager.GetSettingsForMode(graphicsQualityMode);
			if (settingsForMode.FixedResolution)
			{
				Resolution resolution = default(Resolution);
				resolution.width = settingsForMode.ResolutionWidth;
				resolution.height = settingsForMode.ResolutionHeight;
				resolution.refreshRateRatio = new RefreshRate
				{
					numerator = (uint)settingsForMode.FpsLimit,
					denominator = 1u
				};
				Resolution item2 = resolution;
				m_resolutions.Add(item2);
			}
		}
		if (m_resolutions.Count == 0)
		{
			Resolution resolution = default(Resolution);
			resolution.width = 1280;
			resolution.height = 720;
			resolution.refreshRateRatio = new RefreshRate
			{
				numerator = 60u,
				denominator = 1u
			};
			Resolution item3 = resolution;
			m_resolutions.Add(item3);
		}
	}

	private void FillResList()
	{
		UpdateValidResolutions();
		m_resolutionDropdown.ClearOptions();
		List<string> list = new List<string>();
		m_resolutionOptions.Clear();
		int num = 0;
		int num2 = -1;
		foreach (Resolution resolution in m_resolutions)
		{
			string text = $"{resolution.width}x{resolution.height}";
			if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen || !list.Contains(text))
			{
				if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
				{
					list.Add($"{text} {resolution.refreshRateRatio.value}hz");
				}
				else
				{
					list.Add(text);
				}
				if (m_selectedRes.width == resolution.width && m_selectedRes.height == resolution.height)
				{
					num2 = num;
				}
				m_resolutionOptions.Add(resolution);
				num++;
			}
		}
		m_resolutionDropdown.AddOptions(list);
		if (num2 > -1)
		{
			m_resolutionDropdown.value = num2;
		}
	}

	public void OnResolutionSelected(int index)
	{
		m_selectedRes = m_resolutionOptions[index];
		Debug.Log(m_selectedRes);
		OnResolutionChanged();
	}

	public void OnResSwitchOK()
	{
		m_resSwitchDialog.SetActive(value: false);
		m_oldRes = m_selectedRes;
		m_oldFullscreen = m_fullscreenToggle.isOn;
		OnResolutionChanged();
		Settings.instance.BlockNavigation(block: false);
		if (m_destroyAfterResChange)
		{
			Saved?.Invoke();
		}
	}

	public void OnResSwitchCancel()
	{
		m_resSwitchDialog.SetActive(value: false);
		RevertMode();
		Settings.instance.BlockNavigation(block: false);
	}

	public void OnTestResolution()
	{
		ApplyResolution();
		m_resSwitchDialog.SetActive(value: true);
		m_resCountdownTimer = 5f;
		EventSystem.current.SetSelectedGameObject(m_resolutionOk.gameObject);
		Settings.instance.BlockNavigation(block: true);
	}

	private void ApplyResolution()
	{
		if (Screen.width != m_selectedRes.width || Screen.height != m_selectedRes.height || m_fullscreenToggle.isOn != Screen.fullScreen)
		{
			Screen.SetResolution(m_selectedRes.width, m_selectedRes.height, m_fullscreenToggle.isOn ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, m_selectedRes.refreshRateRatio);
		}
	}

	private int MapRenderScaleVisualToSaved(int visualValue)
	{
		if (visualValue < (int)m_renderScaleSlider.maxValue)
		{
			if (visualValue != (int)m_renderScaleSlider.maxValue - 1)
			{
				return visualValue;
			}
			return 0;
		}
		return 20;
	}

	private int MapRenderScaleSavedToVisual(int savedValue)
	{
		if (savedValue > 0)
		{
			if (savedValue < 20)
			{
				return savedValue;
			}
			return (int)m_renderScaleSlider.maxValue;
		}
		return (int)m_renderScaleSlider.maxValue - 1;
	}

	private void RevertMode()
	{
		m_selectedRes = m_oldRes;
		m_fullscreenToggle.isOn = m_oldFullscreen;
		ApplyResolution();
		OnResolutionChanged();
		Settings.instance.BlockNavigation(block: false);
		if (m_destroyAfterResChange)
		{
			Saved?.Invoke();
		}
	}
}
