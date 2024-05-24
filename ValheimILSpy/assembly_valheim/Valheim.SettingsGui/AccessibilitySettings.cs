using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class AccessibilitySettings : SettingsBase
{
	private float m_oldGuiScale;

	[Header("Accessibility")]
	[SerializeField]
	private Slider m_guiScaleSlider;

	[SerializeField]
	private TMP_Text m_guiScaleText;

	[SerializeField]
	private Toggle m_toggleRun;

	[SerializeField]
	private Toggle m_immersiveCamera;

	[SerializeField]
	private Toggle m_cameraShake;

	[SerializeField]
	private Toggle m_reduceFlashingLights;

	[SerializeField]
	private Toggle m_motionblurToggle;

	[SerializeField]
	private Toggle m_depthOfFieldToggle;

	public override void FixBackButtonNavigation(Button backButton)
	{
		SetNavigation(m_depthOfFieldToggle, NavigationDirection.OnDown, backButton);
		SetNavigation(backButton, NavigationDirection.OnUp, m_depthOfFieldToggle);
	}

	public override void FixOkButtonNavigation(Button okButton)
	{
		SetNavigation(okButton, NavigationDirection.OnUp, m_depthOfFieldToggle);
	}

	public override void LoadSettings()
	{
		m_oldGuiScale = PlatformPrefs.GetFloat("GuiScale", 1f);
		m_guiScaleSlider.value = m_oldGuiScale * 100f;
		m_toggleRun.isOn = PlatformPrefs.GetInt("ToggleRun", ZInput.IsGamepadActive() ? 1 : 0) == 1;
		m_immersiveCamera.isOn = PlatformPrefs.GetInt("ShipCameraTilt", 1) == 1;
		m_cameraShake.isOn = PlatformPrefs.GetInt("CameraShake", 1) == 1;
		m_reduceFlashingLights.isOn = PlatformPrefs.GetInt("ReduceFlashingLights") == 1;
		m_motionblurToggle.isOn = PlatformPrefs.GetInt("MotionBlur", 1) == 1;
		m_depthOfFieldToggle.isOn = PlatformPrefs.GetInt("DOF", 1) == 1;
		Settings.ReduceFlashingLights = m_reduceFlashingLights.isOn;
		Settings instance = Settings.instance;
		instance.SharedSettingsChanged = (Action<string, int>)Delegate.Remove(instance.SharedSettingsChanged, new Action<string, int>(SharedSettingsChanged));
		Settings instance2 = Settings.instance;
		instance2.SharedSettingsChanged = (Action<string, int>)Delegate.Combine(instance2.SharedSettingsChanged, new Action<string, int>(SharedSettingsChanged));
		Settings.instance.SettingsPopupDestroyed -= SettingsPopupDestroyed;
		Settings.instance.SettingsPopupDestroyed += SettingsPopupDestroyed;
	}

	private void SettingsPopupDestroyed()
	{
		Settings instance = Settings.instance;
		instance.SharedSettingsChanged = (Action<string, int>)Delegate.Remove(instance.SharedSettingsChanged, new Action<string, int>(SharedSettingsChanged));
		Settings.instance.SettingsPopupDestroyed -= SettingsPopupDestroyed;
	}

	public override void SaveSettings()
	{
		PlatformPrefs.SetFloat("GuiScale", m_guiScaleSlider.value / 100f);
		PlatformPrefs.SetInt("ToggleRun", m_toggleRun.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ShipCameraTilt", m_immersiveCamera.isOn ? 1 : 0);
		PlatformPrefs.SetInt("CameraShake", m_cameraShake.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ReduceFlashingLights", m_reduceFlashingLights.isOn ? 1 : 0);
		Settings.ReduceFlashingLights = m_reduceFlashingLights.isOn;
		Saved?.Invoke();
	}

	public override void ResetSettings()
	{
		GuiScaler.SetScale(m_oldGuiScale);
	}

	private void SharedSettingsChanged(string setting, int value)
	{
		if (setting == "MotionBlur" && m_motionblurToggle.isOn != (value == 1))
		{
			m_motionblurToggle.isOn = value == 1;
		}
		else if (setting == "DepthOfField" && m_depthOfFieldToggle.isOn != (value == 1))
		{
			m_depthOfFieldToggle.isOn = value == 1;
		}
		else if (setting == "ToggleRun" && m_toggleRun.isOn != (value == 1))
		{
			m_toggleRun.isOn = value == 1;
		}
	}

	public void OnUIScaleChanged()
	{
		m_guiScaleText.text = m_guiScaleSlider.value + "%";
		GuiScaler.SetScale(m_guiScaleSlider.value / 100f);
	}

	public void OnMotionBlurChanged()
	{
		Settings.instance.SharedSettingsChanged?.Invoke("MotionBlur", m_motionblurToggle.isOn ? 1 : 0);
	}

	public void OnDepthOfFieldChanged()
	{
		Settings.instance.SharedSettingsChanged?.Invoke("DepthOfField", m_depthOfFieldToggle.isOn ? 1 : 0);
	}

	public void OnToggleRunChanged()
	{
		Settings.instance.SharedSettingsChanged?.Invoke("ToggleRun", m_toggleRun.isOn ? 1 : 0);
	}
}
