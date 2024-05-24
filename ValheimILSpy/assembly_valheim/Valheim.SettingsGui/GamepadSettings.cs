using System.Collections.Generic;
using GUIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class GamepadSettings : SettingsBase
{
	[SerializeField]
	private UIGroupHandler m_groupHandler;

	[Header("Gamepad")]
	[SerializeField]
	private Toggle m_gamepadEnabled;

	[SerializeField]
	private Slider m_gamepadSensitivitySlider;

	[SerializeField]
	private TMP_Text m_cameraSensitivityText;

	[SerializeField]
	private Button m_leftLayoutButton;

	[SerializeField]
	private Button m_rightLayoutButton;

	[SerializeField]
	private GamepadMapController m_gamepadMapController;

	[SerializeField]
	private TMP_Text m_layoutText;

	[SerializeField]
	private Toggle m_swapTriggers;

	[SerializeField]
	private GuiDropdown m_glyphs;

	[SerializeField]
	private Toggle m_invertCameraY;

	[SerializeField]
	private Toggle m_invertCameraX;

	[SerializeField]
	private Toggle m_useRadial;

	[SerializeField]
	private GameObject m_emptyToggleShift;

	private const string GlyphsXbox = "Xbox";

	private const string GlyphsPlaystation = "Playstation";

	private List<string> m_glyphOptions = new List<string> { "Xbox", "Playstation" };

	private InputLayout m_initialLayout;

	private InputLayout m_currentLayout;

	private bool m_initialAlternativeGlyphs;

	private bool m_initialSwapTriggers;

	public override void FixBackButtonNavigation(Button backButton)
	{
		SetNavigation(m_gamepadSensitivitySlider, NavigationDirection.OnDown, backButton);
		SetNavigation(backButton, NavigationDirection.OnUp, m_gamepadSensitivitySlider);
	}

	public override void FixOkButtonNavigation(Button okButton)
	{
		SetNavigation(okButton, NavigationDirection.OnUp, m_gamepadSensitivitySlider);
	}

	public override void LoadSettings()
	{
		m_useRadial.gameObject.SetActive(value: false);
		PlayerController.m_gamepadSens = PlatformPrefs.GetFloat("GamepadSensitivity", PlayerController.m_gamepadSens);
		PlayerController.m_invertCameraY = PlatformPrefs.GetInt("InvertCameraY", PlatformPrefs.GetInt("InvertMouse")) == 1;
		PlayerController.m_invertCameraX = PlatformPrefs.GetInt("InvertCameraX") == 1;
		m_initialLayout = ZInput.InputLayout;
		m_currentLayout = m_initialLayout;
		m_initialAlternativeGlyphs = PlatformPrefs.GetInt("AltGlyphs") == 1;
		m_initialSwapTriggers = ZInput.SwapTriggers;
		m_gamepadEnabled.isOn = ZInput.IsGamepadEnabled();
		m_gamepadSensitivitySlider.value = PlayerController.m_gamepadSens;
		m_invertCameraY.isOn = PlayerController.m_invertCameraY;
		m_invertCameraX.isOn = PlayerController.m_invertCameraX;
		m_swapTriggers.isOn = m_initialSwapTriggers;
		m_useRadial.isOn = PlayerPrefs.GetInt("Use_radials", 0) == 1;
		m_glyphs.ClearOptions();
		m_glyphs.AddOptions(m_glyphOptions);
		m_glyphs.value = m_glyphOptions.IndexOf(m_initialAlternativeGlyphs ? "Playstation" : "Xbox");
		m_gamepadMapController.Show(m_initialLayout);
		OnLayoutChanged();
	}

	public override void ResetSettings()
	{
		m_glyphs.value = m_glyphOptions.IndexOf(m_initialAlternativeGlyphs ? "Playstation" : "Xbox");
		m_currentLayout = m_initialLayout;
		m_swapTriggers.isOn = m_initialSwapTriggers;
		OnLayoutChanged();
	}

	public override void SaveSettings()
	{
		PlatformPrefs.SetFloat("GamepadSensitivity", m_gamepadSensitivitySlider.value);
		PlatformPrefs.SetInt("InvertCameraY", m_invertCameraY.isOn ? 1 : 0);
		PlatformPrefs.SetInt("InvertCameraX", m_invertCameraX.isOn ? 1 : 0);
		PlatformPrefs.SetInt("AltGlyphs", (m_glyphs.value == m_glyphOptions.IndexOf("Playstation")) ? 1 : 0);
		PlatformPrefs.SetInt("SwapTriggers", m_swapTriggers.isOn ? 1 : 0);
		PlayerController.m_gamepadSens = m_gamepadSensitivitySlider.value;
		PlayerController.m_invertCameraY = m_invertCameraY.isOn;
		PlayerController.m_invertCameraX = m_invertCameraX.isOn;
		ZInput.SwapTriggers = m_swapTriggers.isOn;
		ZInput.PlayStationGlyphs = m_glyphs.value == m_glyphOptions.IndexOf("Playstation");
		ZInput.SetGamepadEnabled(m_gamepadEnabled.isOn);
		Saved?.Invoke();
	}

	public void OnGamepadSensitivityChanged()
	{
		m_cameraSensitivityText.text = Mathf.Round(m_gamepadSensitivitySlider.value * 100f) + "%";
	}

	public void OnLayoutLeft()
	{
		m_currentLayout = GamepadMapController.PrevLayout(m_gamepadMapController.VisibleLayout);
		OnLayoutChanged();
	}

	public void OnLayoutRight()
	{
		m_currentLayout = GamepadMapController.NextLayout(m_gamepadMapController.VisibleLayout);
		OnLayoutChanged();
	}

	public void OnLayoutChanged()
	{
		if (ZInput.instance != null)
		{
			ZInput.PlayStationGlyphs = m_glyphs.value == m_glyphOptions.IndexOf("Playstation");
			ZInput.SwapTriggers = m_swapTriggers.isOn;
			ZInput.instance.ChangeLayout(m_currentLayout);
			m_gamepadMapController.Show(m_currentLayout, GamepadMapController.GetType(m_glyphs.value == m_glyphOptions.IndexOf("Playstation"), Settings.IsSteamRunningOnSteamDeck()));
			m_layoutText.text = Localization.instance.Localize(GamepadMapController.GetLayoutStringId(m_currentLayout));
		}
	}
}
