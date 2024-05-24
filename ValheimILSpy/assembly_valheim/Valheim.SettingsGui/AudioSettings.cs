using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Valheim.SettingsGui;

public class AudioSettings : SettingsBase
{
	[Header("Audio")]
	[SerializeField]
	private Slider m_volumeSlider;

	[SerializeField]
	private TMP_Text m_volumeText;

	[SerializeField]
	private Slider m_sfxVolumeSlider;

	[SerializeField]
	private TMP_Text m_sfxVolumeText;

	[SerializeField]
	private Slider m_musicVolumeSlider;

	[SerializeField]
	private TMP_Text m_musicVolumeText;

	[SerializeField]
	private Toggle m_continousMusic;

	[SerializeField]
	private AudioMixer m_masterMixer;

	private float m_oldVolume;

	private float m_oldSfxVolume;

	private float m_oldMusicVolume;

	public override void FixBackButtonNavigation(Button backButton)
	{
		SetNavigation(m_continousMusic, NavigationDirection.OnDown, backButton);
		SetNavigation(backButton, NavigationDirection.OnUp, m_continousMusic);
	}

	public override void FixOkButtonNavigation(Button okButton)
	{
		SetNavigation(okButton, NavigationDirection.OnUp, m_continousMusic);
	}

	public override void LoadSettings()
	{
		m_volumeSlider.value = PlatformPrefs.GetFloat("MasterVolume", AudioListener.volume);
		m_sfxVolumeSlider.value = PlatformPrefs.GetFloat("SfxVolume", 1f);
		m_musicVolumeSlider.value = PlatformPrefs.GetFloat("MusicVolume", 1f);
		m_continousMusic.isOn = PlatformPrefs.GetInt("ContinousMusic", 1) == 1;
		m_oldVolume = m_volumeSlider.value;
		m_oldSfxVolume = m_sfxVolumeSlider.value;
		m_oldMusicVolume = m_musicVolumeSlider.value;
	}

	public override void SaveSettings()
	{
		PlatformPrefs.SetFloat("MasterVolume", m_volumeSlider.value);
		PlatformPrefs.SetFloat("MusicVolume", m_musicVolumeSlider.value);
		PlatformPrefs.SetFloat("SfxVolume", m_sfxVolumeSlider.value);
		PlatformPrefs.SetInt("ContinousMusic", m_continousMusic.isOn ? 1 : 0);
		Settings.ContinousMusic = m_continousMusic.isOn;
		Saved?.Invoke();
	}

	public override void ResetSettings()
	{
		AudioListener.volume = m_oldVolume;
		MusicMan.m_masterMusicVolume = m_oldMusicVolume;
		AudioMan.SetSFXVolume(m_oldSfxVolume);
	}

	public void OnAudioChanged()
	{
		AudioListener.volume = m_volumeSlider.value;
		m_volumeText.text = Mathf.Round(AudioListener.volume * 100f) + "%";
		MusicMan.m_masterMusicVolume = m_musicVolumeSlider.value;
		m_musicVolumeText.text = Mathf.Round(MusicMan.m_masterMusicVolume * 100f) + "%";
		AudioMan.SetSFXVolume(m_sfxVolumeSlider.value);
		m_sfxVolumeText.text = Mathf.Round(AudioMan.GetSFXVolume() * 100f) + "%";
	}
}
