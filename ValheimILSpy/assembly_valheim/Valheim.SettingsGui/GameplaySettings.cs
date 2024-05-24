using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class GameplaySettings : SettingsBase
{
	[Header("Gameplay")]
	[SerializeField]
	private TMP_Text m_language;

	[SerializeField]
	private TMP_Text m_communityTranslation;

	[SerializeField]
	private TMP_Text m_cloudStorageWarning;

	[SerializeField]
	private Toggle m_toggleRun;

	[SerializeField]
	private Toggle m_toggleAttackTowardsPlayerLookDir;

	[SerializeField]
	private Toggle m_showKeyHints;

	[SerializeField]
	private Toggle m_tutorialsEnabled;

	[SerializeField]
	private Button m_resetTutorial;

	[SerializeField]
	private Toggle m_reduceBGUsage;

	[SerializeField]
	private Slider m_autoBackups;

	[SerializeField]
	private TMP_Text m_autoBackupsText;

	private string m_languageKey = "";

	private int m_showCloudWarningBackupThreshold = 4;

	private const string c_AttackTowardsPlayerLookDirString = "AttackTowardsPlayerLookDir";

	public override void FixBackButtonNavigation(Button backButton)
	{
		SetNavigation(m_autoBackups, NavigationDirection.OnDown, backButton);
		SetNavigation(backButton, NavigationDirection.OnUp, m_autoBackups);
	}

	public override void FixOkButtonNavigation(Button okButton)
	{
		SetNavigation(okButton, NavigationDirection.OnUp, m_autoBackups);
	}

	public static void SetControllerSpecificFirstTimeSettings()
	{
		if (!PlayerPrefs.HasKey("AttackTowardsPlayerLookDir"))
		{
			PlatformPrefs.SetInt("AttackTowardsPlayerLookDir", ZInput.GamepadActive ? 1 : 0);
		}
	}

	public override void LoadSettings()
	{
		SetControllerSpecificFirstTimeSettings();
		m_communityTranslation.gameObject.SetActive(value: false);
		m_languageKey = Localization.instance.GetSelectedLanguage();
		m_toggleRun.isOn = PlatformPrefs.GetInt("ToggleRun", ZInput.IsGamepadActive() ? 1 : 0) == 1;
		m_toggleAttackTowardsPlayerLookDir.isOn = PlatformPrefs.GetInt("AttackTowardsPlayerLookDir") == 1;
		m_showKeyHints.isOn = PlatformPrefs.GetInt("KeyHints", 1) == 1;
		m_tutorialsEnabled.isOn = PlatformPrefs.GetInt("TutorialsEnabled", 1) == 1;
		m_reduceBGUsage.isOn = PlatformPrefs.GetInt("ReduceBackgroundUsage") == 1;
		m_autoBackups.value = PlatformPrefs.GetInt("AutoBackups", 4);
		UpdateLanguageText();
		OnAutoBackupsChanged();
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
		PlatformPrefs.SetInt("KeyHints", m_showKeyHints.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ToggleRun", m_toggleRun.isOn ? 1 : 0);
		PlatformPrefs.SetInt("AttackTowardsPlayerLookDir", m_toggleAttackTowardsPlayerLookDir.isOn ? 1 : 0);
		PlatformPrefs.SetInt("TutorialsEnabled", m_tutorialsEnabled.isOn ? 1 : 0);
		PlatformPrefs.SetInt("ReduceBackgroundUsage", m_reduceBGUsage.isOn ? 1 : 0);
		PlatformPrefs.SetInt("AutoBackups", (int)m_autoBackups.value);
		ZInput.ToggleRun = m_toggleRun.isOn;
		Raven.m_tutorialsEnabled = m_tutorialsEnabled.isOn;
		Settings.ReduceBackgroundUsage = m_reduceBGUsage.isOn;
		if (Player.m_localPlayer != null)
		{
			Player.m_localPlayer.AttackTowardsPlayerLookDir = m_toggleAttackTowardsPlayerLookDir.isOn;
		}
		Localization.instance.SetLanguage(m_languageKey);
		Saved?.Invoke();
	}

	private void SharedSettingsChanged(string setting, int value)
	{
		if (setting == "ToggleRun" && m_toggleRun.isOn != (value == 1))
		{
			m_toggleRun.isOn = value == 1;
		}
	}

	public void OnLanguageLeft()
	{
		m_languageKey = Localization.instance.GetPrevLanguage(m_languageKey);
		UpdateLanguageText();
	}

	public void OnLanguageRight()
	{
		m_languageKey = Localization.instance.GetNextLanguage(m_languageKey);
		UpdateLanguageText();
	}

	private void UpdateLanguageText()
	{
		m_language.text = Localization.instance.Localize("$language_" + m_languageKey.ToLower());
		m_communityTranslation.gameObject.SetActive(m_language.text.Contains("*"));
	}

	public void OnResetTutorial()
	{
		Player.ResetSeenTutorials();
	}

	public void OnAutoBackupsChanged()
	{
		m_autoBackupsText.text = ((m_autoBackups.value == 1f) ? "0" : m_autoBackups.value.ToString());
		m_cloudStorageWarning.gameObject.SetActive(m_autoBackups.value > (float)m_showCloudWarningBackupThreshold);
	}

	public void OnToggleRunChanged()
	{
		Settings.instance.SharedSettingsChanged?.Invoke("ToggleRun", m_toggleRun.isOn ? 1 : 0);
	}
}
