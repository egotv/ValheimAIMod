using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class MusicMan : MonoBehaviour
{
	[Serializable]
	public class NamedMusic
	{
		public string m_name = "";

		public AudioClip[] m_clips;

		public float m_volume = 1f;

		public float m_fadeInTime = 3f;

		public bool m_alwaysFadeout;

		public bool m_loop;

		public bool m_resume;

		public bool m_enabled = true;

		public bool m_ambientMusic;

		[NonSerialized]
		public int m_savedPlaybackPos;

		[NonSerialized]
		public float m_lastPlayedTime;
	}

	private string m_triggeredMusic = "";

	private static MusicMan m_instance;

	public static float m_masterMusicVolume = 1f;

	public AudioMixerGroup m_musicMixer;

	public List<NamedMusic> m_music = new List<NamedMusic>();

	public float m_musicResetNonContinous = 120f;

	[Header("Combat")]
	public float m_combatMusicTimeout = 4f;

	[Header("Sailing")]
	public float m_sailMusicShipSpeedThreshold = 3f;

	public float m_sailMusicMinSailTime = 20f;

	[Header("Ambient music")]
	public float m_randomMusicIntervalMin = 300f;

	public float m_randomMusicIntervalMax = 500f;

	private NamedMusic m_queuedMusic;

	private NamedMusic m_currentMusic;

	private NamedMusic m_lastStartedMusic;

	private float m_musicVolume = 1f;

	private float m_musicFadeTime = 3f;

	private bool m_alwaysFadeout;

	private bool m_stopMusic;

	private string m_randomEventMusic;

	private float m_lastAmbientMusicTime;

	private float m_randomAmbientInterval;

	private string m_triggerMusic;

	private string m_locationMusic;

	public string m_lastLocationMusic;

	private DateTime m_lastLocationMusicChange;

	public int m_repeatLocationMusicResetSeconds = 300;

	private float m_combatTimer;

	private float m_resetMusicTimer;

	private AudioSource m_musicSource;

	private float m_currentMusicVol;

	public float m_currentMusicVolMax = 1f;

	private float m_sailDuration;

	private float m_notSailDuration;

	private float m_musicOnTopDuckVolume;

	private const string c_Duckmusic = "Music_ontop_ducking";

	public static MusicMan instance => m_instance;

	private void Awake()
	{
		if ((bool)m_instance)
		{
			return;
		}
		m_instance = this;
		GameObject gameObject = new GameObject("music");
		gameObject.transform.SetParent(base.transform);
		m_musicSource = gameObject.AddComponent<AudioSource>();
		m_musicSource.loop = true;
		m_musicSource.spatialBlend = 0f;
		m_musicSource.outputAudioMixerGroup = m_musicMixer;
		m_musicSource.priority = 0;
		m_musicSource.bypassReverbZones = true;
		m_randomAmbientInterval = UnityEngine.Random.Range(m_randomMusicIntervalMin, m_randomMusicIntervalMax);
		m_masterMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
		ApplySettings();
		foreach (NamedMusic item in m_music)
		{
			AudioClip[] clips = item.m_clips;
			foreach (AudioClip audioClip in clips)
			{
				if (audioClip == null || !audioClip)
				{
					item.m_enabled = false;
					ZLog.LogWarning("Missing audio clip in music " + item.m_name);
					break;
				}
			}
		}
	}

	public void ApplySettings()
	{
		foreach (NamedMusic item in m_music)
		{
			if (item.m_ambientMusic)
			{
				item.m_loop = Settings.ContinousMusic;
				if (!Settings.ContinousMusic && GetCurrentMusic() == item.m_name && m_musicSource.loop)
				{
					ZLog.Log("Stopping looping music because continous music is disabled");
					StopMusic();
				}
			}
		}
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	private void Update()
	{
		if (!(m_instance != this))
		{
			float deltaTime = Time.deltaTime;
			UpdateCurrentMusic(deltaTime);
			UpdateCombatMusic(deltaTime);
			m_currentMusicVolMax = MusicVolume.UpdateProximityVolumes(m_musicSource);
			UpdateMusic(deltaTime);
		}
	}

	private void UpdateCurrentMusic(float dt)
	{
		string currentMusic = GetCurrentMusic();
		if (Game.instance != null)
		{
			if (Player.m_localPlayer == null)
			{
				StartMusic("respawn");
				return;
			}
			if (currentMusic == "respawn")
			{
				StopMusic();
			}
		}
		if ((bool)Player.m_localPlayer && Player.m_localPlayer.InIntro())
		{
			StartMusic("intro");
			return;
		}
		if (currentMusic == "intro")
		{
			StopMusic();
		}
		float target = ((m_randomEventMusic == null) ? 0f : (-80f));
		m_musicOnTopDuckVolume = Mathf.MoveTowards(m_musicOnTopDuckVolume, target, 80f * Time.deltaTime);
		m_musicMixer.audioMixer.SetFloat("Music_ontop_ducking", m_musicOnTopDuckVolume);
		if (!HandleEventMusic(currentMusic) && !HandleLocationMusic(currentMusic) && !HandleSailingMusic(dt, currentMusic) && !HandleTriggerMusic(currentMusic))
		{
			HandleEnvironmentMusic(dt, currentMusic);
		}
	}

	private bool HandleEnvironmentMusic(float dt, string currentMusic)
	{
		if (!EnvMan.instance)
		{
			return false;
		}
		NamedMusic environmentMusic = GetEnvironmentMusic();
		string currentMusic2 = GetCurrentMusic();
		if (environmentMusic == null || (m_currentMusic != null && environmentMusic.m_name != currentMusic2))
		{
			StopMusic();
			return true;
		}
		if (environmentMusic.m_name == currentMusic2)
		{
			return true;
		}
		if (!environmentMusic.m_loop)
		{
			if (Time.time - m_lastAmbientMusicTime < m_randomAmbientInterval)
			{
				return false;
			}
			m_randomAmbientInterval = UnityEngine.Random.Range(m_randomMusicIntervalMin, m_randomMusicIntervalMax);
			m_lastAmbientMusicTime = Time.time;
			ZLog.Log("Environment music starting at random ambient interval");
		}
		StartMusic(environmentMusic);
		return true;
	}

	private NamedMusic GetEnvironmentMusic()
	{
		string text = null;
		text = ((!Player.m_localPlayer || !Player.m_localPlayer.IsSafeInHome()) ? EnvMan.instance.GetAmbientMusic() : "home");
		return FindMusic(text);
	}

	private bool HandleTriggerMusic(string currentMusic)
	{
		if (m_triggerMusic != null)
		{
			StartMusic(m_triggerMusic);
			m_triggeredMusic = m_triggerMusic;
			m_triggerMusic = null;
			return true;
		}
		if (m_triggeredMusic != null)
		{
			if (currentMusic == m_triggeredMusic)
			{
				return true;
			}
			m_triggeredMusic = null;
		}
		return false;
	}

	public void LocationMusic(string name)
	{
		m_locationMusic = name;
	}

	private bool HandleLocationMusic(string currentMusic)
	{
		if (m_lastLocationMusic != null && DateTime.Now > m_lastLocationMusicChange + TimeSpan.FromSeconds(m_repeatLocationMusicResetSeconds))
		{
			m_lastLocationMusic = null;
			m_lastLocationMusicChange = DateTime.Now;
		}
		if (m_locationMusic != null)
		{
			if (currentMusic == m_locationMusic && !m_musicSource.isPlaying)
			{
				m_locationMusic = null;
				return false;
			}
			if (currentMusic != m_locationMusic)
			{
				m_lastLocationMusicChange = DateTime.Now;
			}
			if (StartMusic(m_locationMusic))
			{
				m_lastLocationMusic = m_locationMusic;
			}
			else
			{
				ZLog.Log("Location music missing: " + m_locationMusic);
				m_locationMusic = null;
			}
			return true;
		}
		return false;
	}

	private bool HandleEventMusic(string currentMusic)
	{
		if ((bool)RandEventSystem.instance)
		{
			string musicOverride = RandEventSystem.instance.GetMusicOverride();
			if (musicOverride != null)
			{
				StartMusic(musicOverride);
				m_randomEventMusic = musicOverride;
				return true;
			}
			if (currentMusic == m_randomEventMusic)
			{
				m_randomEventMusic = null;
				StopMusic();
			}
		}
		return false;
	}

	private bool HandleCombatMusic(string currentMusic)
	{
		if (InCombat())
		{
			StartMusic("combat");
			return true;
		}
		if (currentMusic == "combat")
		{
			StopMusic();
		}
		return false;
	}

	private bool HandleSailingMusic(float dt, string currentMusic)
	{
		if (IsSailing())
		{
			m_notSailDuration = 0f;
			m_sailDuration += dt;
			if (m_sailDuration > m_sailMusicMinSailTime)
			{
				StartMusic(GetSailingMusic());
				return true;
			}
		}
		else
		{
			m_sailDuration = 0f;
			m_notSailDuration += dt;
			if (m_notSailDuration > m_sailMusicMinSailTime / 2f && currentMusic == GetSailingMusic() && currentMusic != EnvMan.instance.GetAmbientMusic())
			{
				StopMusic();
			}
		}
		return false;
	}

	public string GetSailingMusic()
	{
		if ((bool)Player.m_localPlayer && Player.m_localPlayer.GetCurrentBiome() == Heightmap.Biome.AshLands)
		{
			return "sailing_ashlands";
		}
		return "sailing";
	}

	private bool IsSailing()
	{
		if (!Player.m_localPlayer)
		{
			return false;
		}
		Ship localShip = Ship.GetLocalShip();
		if ((bool)localShip && localShip.GetSpeed() > m_sailMusicShipSpeedThreshold)
		{
			return true;
		}
		return false;
	}

	private void UpdateMusic(float dt)
	{
		if (m_queuedMusic != null || m_stopMusic)
		{
			if (!m_musicSource.isPlaying || m_currentMusicVol <= 0f)
			{
				if (m_musicSource.isPlaying && m_currentMusic != null && m_currentMusic.m_loop && m_currentMusic.m_resume)
				{
					m_currentMusic.m_lastPlayedTime = Time.time;
					m_currentMusic.m_savedPlaybackPos = m_musicSource.timeSamples;
					ZLog.Log("Stopped music " + m_currentMusic.m_name + " at " + m_currentMusic.m_savedPlaybackPos);
				}
				m_musicSource.Stop();
				m_stopMusic = false;
				m_currentMusic = null;
				if (m_queuedMusic != null)
				{
					m_musicSource.clip = m_queuedMusic.m_clips[UnityEngine.Random.Range(0, m_queuedMusic.m_clips.Length)];
					m_musicSource.loop = m_queuedMusic.m_loop;
					m_musicSource.volume = 0f;
					m_musicSource.timeSamples = 0;
					m_musicSource.Play();
					if (m_queuedMusic.m_loop && m_queuedMusic.m_resume && Time.time - m_queuedMusic.m_lastPlayedTime < m_musicSource.clip.length * 2f)
					{
						m_musicSource.timeSamples = m_queuedMusic.m_savedPlaybackPos;
						ZLog.Log("Resumed music " + m_queuedMusic.m_name + " at " + m_queuedMusic.m_savedPlaybackPos);
					}
					m_currentMusicVol = 0f;
					m_musicVolume = m_queuedMusic.m_volume;
					m_musicFadeTime = m_queuedMusic.m_fadeInTime;
					m_alwaysFadeout = m_queuedMusic.m_alwaysFadeout;
					m_currentMusic = m_queuedMusic;
					m_queuedMusic = null;
				}
			}
			else
			{
				float num = ((m_queuedMusic != null) ? Mathf.Min(m_queuedMusic.m_fadeInTime, m_musicFadeTime) : m_musicFadeTime);
				m_currentMusicVol = Mathf.MoveTowards(m_currentMusicVol, 0f, dt / num);
				m_musicSource.volume = Utils.SmoothStep(0f, 1f, m_currentMusicVol) * m_musicVolume * m_masterMusicVolume;
			}
		}
		else if (m_musicSource.isPlaying)
		{
			float num2 = m_musicSource.clip.length - m_musicSource.time;
			if (m_alwaysFadeout && !m_musicSource.loop && num2 < m_musicFadeTime)
			{
				m_currentMusicVol = Mathf.MoveTowards(m_currentMusicVol, 0f, dt / m_musicFadeTime);
				m_musicSource.volume = Utils.SmoothStep(0f, 1f, m_currentMusicVol) * m_musicVolume * m_masterMusicVolume;
			}
			else
			{
				m_currentMusicVol = Mathf.MoveTowards(m_currentMusicVol, m_currentMusicVolMax, dt / m_musicFadeTime);
				m_musicSource.volume = Utils.SmoothStep(0f, 1f, m_currentMusicVol) * m_musicVolume * m_masterMusicVolume;
			}
			if (!Settings.ContinousMusic && num2 < m_musicFadeTime)
			{
				StopMusic();
				ZLog.Log("Music stopped after finishing, because continous music is disabled");
			}
		}
		else if (m_currentMusic != null && !m_musicSource.isPlaying)
		{
			m_currentMusic = null;
		}
		if (m_resetMusicTimer > 0f)
		{
			m_resetMusicTimer -= dt;
		}
		if (Terminal.m_showTests)
		{
			Terminal.m_testList["Music current"] = ((m_currentMusic == null) ? "NULL" : m_currentMusic.m_name);
			Terminal.m_testList["Music last started"] = ((m_lastStartedMusic == null) ? "NULL" : m_lastStartedMusic.m_name);
			Terminal.m_testList["Music queued"] = ((m_queuedMusic == null) ? "NULL" : m_queuedMusic.m_name);
			Terminal.m_testList["Music stopping"] = m_stopMusic.ToString();
			Terminal.m_testList["Music reset non continous"] = $"{m_resetMusicTimer} / {m_musicResetNonContinous}";
			if (ZInput.GetKeyDown(KeyCode.N) && ZInput.GetKey(KeyCode.LeftShift) && m_musicSource != null && m_musicSource.isPlaying)
			{
				m_musicSource.time = m_musicSource.clip.length - 4f;
			}
		}
	}

	private void UpdateCombatMusic(float dt)
	{
		if (m_combatTimer > 0f)
		{
			m_combatTimer -= Time.deltaTime;
		}
	}

	public void ResetCombatTimer()
	{
		m_combatTimer = m_combatMusicTimeout;
	}

	private bool InCombat()
	{
		return m_combatTimer > 0f;
	}

	public void TriggerMusic(string name)
	{
		m_triggerMusic = name;
	}

	private bool StartMusic(string name)
	{
		if (GetCurrentMusic() == name)
		{
			return true;
		}
		NamedMusic music = FindMusic(name);
		return StartMusic(music);
	}

	private bool StartMusic(NamedMusic music)
	{
		if (music != null && GetCurrentMusic() == music.m_name)
		{
			return true;
		}
		if (music == m_lastStartedMusic && !Settings.ContinousMusic && m_resetMusicTimer > 0f)
		{
			return false;
		}
		m_lastStartedMusic = music;
		m_resetMusicTimer = m_musicResetNonContinous + ((music != null && music.m_clips.Length != 0) ? music.m_clips[0].length : 0f);
		if (music != null)
		{
			m_queuedMusic = music;
			m_stopMusic = false;
			ZLog.Log("Starting music " + music.m_name);
			return true;
		}
		StopMusic();
		return false;
	}

	private NamedMusic FindMusic(string name)
	{
		if (name == null || name.Length == 0)
		{
			return null;
		}
		foreach (NamedMusic item in m_music)
		{
			if (item.m_name == name && item.m_enabled && item.m_clips.Length != 0 && (bool)item.m_clips[0])
			{
				return item;
			}
		}
		return null;
	}

	public bool IsPlaying()
	{
		return m_musicSource.isPlaying;
	}

	private string GetCurrentMusic()
	{
		if (m_stopMusic)
		{
			return "";
		}
		if (m_queuedMusic != null)
		{
			return m_queuedMusic.m_name;
		}
		if (m_currentMusic != null)
		{
			return m_currentMusic.m_name;
		}
		return "";
	}

	private void StopMusic()
	{
		m_queuedMusic = null;
		m_stopMusic = true;
	}

	public void Reset()
	{
		StopMusic();
		m_combatTimer = 0f;
		m_randomEventMusic = null;
		m_triggerMusic = null;
		m_locationMusic = null;
	}
}
