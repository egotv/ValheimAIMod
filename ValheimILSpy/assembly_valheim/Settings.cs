using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

public class Settings : MonoBehaviour
{
	private static Settings m_instance;

	private static bool m_startUp = true;

	public const bool c_vsyncRequiresRestart = false;

	public const float c_renderScaleDefault = 1f;

	public static int FPSLimit = -1;

	public static bool ReduceBackgroundUsage = false;

	public static bool ContinousMusic = true;

	public static bool ReduceFlashingLights = false;

	public static AssetMemoryUsagePolicy AssetMemoryUsagePolicy = AssetMemoryUsagePolicy.KeepSynchronousOnlyLoaded;

	[SerializeField]
	private GameObject[] m_tabKeyHints;

	[SerializeField]
	private GameObject m_settingsPanel;

	[SerializeField]
	private TabHandler m_tabHandler;

	[SerializeField]
	private Button m_backButton;

	[SerializeField]
	private Button m_okButton;

	private bool m_navigationBlocked;

	private List<SettingsBase> SettingsTabs;

	private int m_tabsToSave;

	public Action<string, int> SharedSettingsChanged;

	public static Settings instance => m_instance;

	public event Action SettingsPopupDestroyed;

	private void Awake()
	{
		m_instance = this;
		m_tabHandler = GetComponentInChildren<TabHandler>();
		SetAvailableTabs();
		ZInput.OnInputLayoutChanged += OnInputLayoutChanged;
		OnInputLayoutChanged();
	}

	private void Update()
	{
		if (!m_navigationBlocked && ZInput.GetKeyDown(KeyCode.Escape))
		{
			OnBack();
		}
	}

	private void SetAvailableTabs()
	{
		SettingsTabs = new List<SettingsBase>();
		foreach (TabHandler.Tab tab in m_tabHandler.m_tabs)
		{
			SettingsTabs.Add(tab.m_page.gameObject.GetComponent<SettingsBase>());
		}
		LoadTabSettings();
		m_tabHandler.ActiveTabChanged -= ActiveTabChanged;
		m_tabHandler.ActiveTabChanged += ActiveTabChanged;
	}

	private void OnInputLayoutChanged()
	{
		GameObject[] tabKeyHints = m_tabKeyHints;
		for (int i = 0; i < tabKeyHints.Length; i++)
		{
			tabKeyHints[i].SetActive(ZInput.GamepadActive);
		}
	}

	private void ActiveTabChanged(int index)
	{
		SettingsTabs[index].FixBackButtonNavigation(m_backButton);
		SettingsTabs[index].FixOkButtonNavigation(m_okButton);
	}

	private void LoadTabSettings()
	{
		foreach (SettingsBase settingsTab in SettingsTabs)
		{
			settingsTab.LoadSettings();
		}
	}

	private void ResetTabSettings()
	{
		foreach (SettingsBase settingsTab in SettingsTabs)
		{
			settingsTab.ResetSettings();
		}
		ZInput.instance.Save();
	}

	private void SaveTabSettings()
	{
		m_tabsToSave = 0;
		foreach (SettingsBase settingsTab in SettingsTabs)
		{
			settingsTab.Saved = (Action)Delegate.Remove(settingsTab.Saved, new Action(TabSaved));
			settingsTab.Saved = (Action)Delegate.Combine(settingsTab.Saved, new Action(TabSaved));
			m_tabsToSave++;
		}
		foreach (SettingsBase settingsTab2 in SettingsTabs)
		{
			settingsTab2.SaveSettings();
		}
	}

	private void ApplyAndClose()
	{
		ZInput.instance.Save();
		if ((bool)GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if ((bool)CameraEffects.instance)
		{
			CameraEffects.instance.ApplySettings();
		}
		if ((bool)ClutterSystem.instance)
		{
			ClutterSystem.instance.ApplySettings();
		}
		if ((bool)MusicMan.instance)
		{
			MusicMan.instance.ApplySettings();
		}
		if ((bool)GameCamera.instance)
		{
			GameCamera.instance.ApplySettings();
		}
		if ((bool)KeyHints.instance)
		{
			KeyHints.instance.ApplySettings();
		}
		DynamicParticleReduction[] array = UnityEngine.Object.FindObjectsOfType<DynamicParticleReduction>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ApplySettings();
		}
		PlayerPrefs.Save();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void TabSaved()
	{
		if (--m_tabsToSave <= 0)
		{
			ApplyAndClose();
		}
	}

	private void OnDestroy()
	{
		ZInput.OnInputLayoutChanged -= OnInputLayoutChanged;
		m_tabHandler.ActiveTabChanged -= ActiveTabChanged;
		this.SettingsPopupDestroyed?.Invoke();
		m_instance = null;
	}

	public void OnBack()
	{
		ResetTabSettings();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public void OnOk()
	{
		SaveTabSettings();
	}

	public void BlockNavigation(bool block)
	{
		m_navigationBlocked = block;
		m_okButton.gameObject.SetActive(!block);
		m_backButton.gameObject.SetActive(!block);
		m_tabHandler.m_gamepadInput = !block;
		m_tabHandler.m_keybaordInput = !block;
		m_tabHandler.m_tabKeyInput = !block;
	}

	public static void SetPlatformDefaultPrefs()
	{
		if (IsSteamRunningOnSteamDeck())
		{
			ZLog.Log("Running on Steam Deck!");
		}
		else
		{
			ZLog.Log("Using default prefs");
		}
		PlatformPrefs.SetDefaults(new PlatformPrefs.PlatformDefaults("deck_", () => IsSteamRunningOnSteamDeck(), new Dictionary<string, PlatformPrefs>
		{
			{ "GuiScale", 1.15f },
			{ "DOF", 0 },
			{ "VSync", 0 },
			{ "Bloom", 1 },
			{ "SSAO", 1 },
			{ "SunShafts", 1 },
			{ "AntiAliasing", 0 },
			{ "ChromaticAberration", 1 },
			{ "MotionBlur", 0 },
			{ "SoftPart", 1 },
			{ "Tesselation", 0 },
			{ "DistantShadows", 1 },
			{ "ShadowQuality", 0 },
			{ "LodBias", 1 },
			{ "Lights", 1 },
			{ "ClutterQuality", 1 },
			{ "PointLights", 1 },
			{ "PointLightShadows", 1 },
			{ "FPSLimit", 60 }
		}));
	}

	public static bool IsSteamRunningOnSteamDeck()
	{
		string environmentVariable = Environment.GetEnvironmentVariable("SteamDeck");
		if (!string.IsNullOrEmpty(environmentVariable))
		{
			return environmentVariable != "0";
		}
		return false;
	}

	public static void ApplyStartupSettings()
	{
		ReduceBackgroundUsage = PlatformPrefs.GetInt("ReduceBackgroundUsage") == 1;
		ContinousMusic = PlatformPrefs.GetInt("ContinousMusic", 1) == 1;
		ReduceFlashingLights = PlatformPrefs.GetInt("ReduceFlashingLights") == 1;
		Raven.m_tutorialsEnabled = PlatformPrefs.GetInt("TutorialsEnabled", 1) == 1;
		GraphicsModeManager.Initialize();
	}

	public static void ApplyQualitySettings()
	{
		_ = m_startUp;
		QualitySettings.vSyncCount = ((PlatformPrefs.GetInt("VSync") == 1) ? 1 : 0);
		QualitySettings.softParticles = PlatformPrefs.GetInt("SoftPart", 1) == 1;
		if (PlatformPrefs.GetInt("Tesselation", 1) == 1)
		{
			Shader.EnableKeyword("TESSELATION_ON");
		}
		else
		{
			Shader.DisableKeyword("TESSELATION_ON");
		}
		switch (PlatformPrefs.GetInt("LodBias", 2))
		{
		case 0:
			QualitySettings.lodBias = 1f;
			break;
		case 1:
			QualitySettings.lodBias = 1.5f;
			break;
		case 2:
			QualitySettings.lodBias = 2f;
			break;
		case 3:
			QualitySettings.lodBias = 5f;
			break;
		}
		switch (PlatformPrefs.GetInt("Lights", 2))
		{
		case 0:
			QualitySettings.pixelLightCount = 2;
			break;
		case 1:
			QualitySettings.pixelLightCount = 4;
			break;
		case 2:
			QualitySettings.pixelLightCount = 8;
			break;
		}
		LightLod.m_lightLimit = GetPointLightLimit(PlatformPrefs.GetInt("PointLights", 3));
		LightLod.m_shadowLimit = GetPointLightShadowLimit(PlatformPrefs.GetInt("PointLightShadows", 2));
		FPSLimit = PlatformPrefs.GetInt("FPSLimit", -1);
		float @float = PlatformPrefs.GetFloat("RenderScale", 1f);
		if (@float <= 0f)
		{
			VirtualFrameBuffer.m_autoRenderScale = true;
		}
		else
		{
			VirtualFrameBuffer.m_autoRenderScale = false;
			VirtualFrameBuffer.m_global3DRenderScale = Mathf.Clamp01(@float);
		}
		ApplyShadowQuality();
		m_startUp = false;
	}

	public static int GetPointLightLimit(int level)
	{
		return level switch
		{
			0 => 4, 
			1 => 15, 
			3 => -1, 
			_ => 40, 
		};
	}

	public static int GetPointLightShadowLimit(int level)
	{
		return level switch
		{
			0 => 0, 
			1 => 1, 
			3 => -1, 
			_ => 3, 
		};
	}

	public static void ApplyShadowQuality()
	{
		int @int = PlatformPrefs.GetInt("ShadowQuality", 2);
		int int2 = PlatformPrefs.GetInt("DistantShadows", 1);
		switch (@int)
		{
		case 0:
			QualitySettings.shadowCascades = 2;
			QualitySettings.shadowDistance = 80f;
			QualitySettings.shadowResolution = ShadowResolution.Low;
			break;
		case 1:
			QualitySettings.shadowCascades = 3;
			QualitySettings.shadowDistance = 120f;
			QualitySettings.shadowResolution = ShadowResolution.Medium;
			break;
		case 2:
			QualitySettings.shadowCascades = 4;
			QualitySettings.shadowDistance = 150f;
			QualitySettings.shadowResolution = ShadowResolution.High;
			break;
		}
		Heightmap.EnableDistantTerrainShadows = int2 == 1;
	}
}
