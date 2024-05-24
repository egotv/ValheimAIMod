using System;
using System.Collections.Generic;
using System.Linq;
using CircularBuffer;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public class AudioMan : MonoBehaviour
{
	[Serializable]
	public class BiomeAmbients
	{
		public string m_name = "";

		public float m_forceFadeout = 3f;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		public List<AudioClip> m_randomAmbientClips = new List<AudioClip>();

		public List<AudioClip> m_randomAmbientClipsDay = new List<AudioClip>();

		public List<AudioClip> m_randomAmbientClipsNight = new List<AudioClip>();
	}

	private enum Snapshot
	{
		Default,
		Menu,
		Indoor
	}

	private class SoundHash
	{
		public int hash;

		public float playTime;

		public Vector3 position;

		public SoundHash(int h, float pt, Vector3 pos)
		{
			hash = h;
			playTime = pt;
			position = pos;
		}
	}

	private bool m_zoneSystemValid;

	private bool m_envManValid;

	private readonly List<ZSFX> m_loopingSfx = new List<ZSFX>();

	private readonly List<int> m_checkedHashes = new List<int>();

	private AudioListener m_activeAudioListener;

	private static AudioMan m_instance;

	[Header("Mixers")]
	public AudioMixerGroup m_ambientMixer;

	public AudioMixerGroup m_guiMixer;

	public AudioMixer m_masterMixer;

	public float m_snapshotTransitionTime = 2f;

	[Header("Wind")]
	public AudioClip m_windAudio;

	public float m_windMinVol;

	public float m_windMaxVol = 1f;

	public float m_windMinPitch = 0.5f;

	public float m_windMaxPitch = 1.5f;

	public float m_windVariation = 0.2f;

	public float m_windIntensityPower = 1.5f;

	[Header("Ocean")]
	public AudioClip m_oceanAudio;

	public float m_oceanVolumeMax = 1f;

	public float m_oceanVolumeMin = 1f;

	public float m_oceanFadeSpeed = 0.1f;

	public float m_oceanMoveSpeed = 0.1f;

	public float m_oceanDepthTreshold = 10f;

	[Header("Random ambients")]
	public float m_ambientFadeTime = 2f;

	[Min(1f)]
	public float m_randomAmbientInterval = 5f;

	[Range(0f, 1f)]
	public float m_randomAmbientChance = 0.5f;

	public float m_randomMinDistance = 5f;

	public float m_randomMaxDistance = 20f;

	public List<BiomeAmbients> m_randomAmbients = new List<BiomeAmbients>();

	public GameObject m_randomAmbientPrefab;

	[Header("Lava Ambience")]
	[Min(10f)]
	public float m_lavaScanRadius = 40f;

	[Min(0f)]
	public float m_lavaNoiseMinDistance = 2f;

	[Min(10f)]
	public float m_lavaNoiseMaxDistance = 10f;

	[Min(1f)]
	public float m_lavaNoiseInterval = 2.5f;

	[Range(0f, 1f)]
	public float m_lavaNoiseChance = 0.25f;

	public List<AudioClip> m_randomLavaNoises;

	public GameObject m_lavaLoopPrefab;

	[Space(16f)]
	public int m_maxLavaLoops;

	public float m_minDistanceBetweenLavaLoops = 10f;

	public float m_maxLavaLoopDistance = 40f;

	[Header("Shield Dome Hum")]
	public bool m_enableShieldDomeHum = true;

	public GameObject m_shieldHumPrefab;

	[Header("ZSFX Settings")]
	[Min(0f)]
	[Tooltip("How soon a sound trying to play after the same one counts as concurrent")]
	public float m_concurrencyThreshold = 0.2f;

	[Min(0f)]
	[Tooltip("Automatically makes sure no looping sounds are playing more than this many at a time. ZSFX components that have a max concurrency value set will use that instead.")]
	public int m_forcedMaxConcurrentLoops = 5;

	private AudioSource m_oceanAmbientSource;

	private AudioSource m_ambientLoopSource;

	private AudioSource m_windLoopSource;

	private AudioSource m_shieldHumSource;

	private AudioClip m_queuedAmbientLoop;

	private float m_queuedAmbientVol;

	private float m_ambientVol;

	private float m_randomAmbientTimer;

	private bool m_stopAmbientLoop;

	private bool m_indoor;

	private float m_oceanUpdateTimer;

	private bool m_haveOcean;

	private Vector3 m_avgOceanPoint = Vector3.zero;

	private Vector3 m_listenerPos = Vector3.zero;

	private float m_lavaAmbientTimer;

	private CircularBuffer<Vector3> m_validLavaPositions = new CircularBuffer<Vector3>(128);

	private List<ZSFX> m_ambientLavaLoops = new List<ZSFX>();

	private Snapshot m_currentSnapshot;

	private readonly CircularBuffer<SoundHash> m_soundList = new CircularBuffer<SoundHash>(512);

	public static AudioMan instance => m_instance;

	private void Awake()
	{
		if (m_instance != null)
		{
			ZLog.Log("Audioman already exist, destroying self");
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		m_instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		GameObject gameObject = new GameObject("ocean_ambient_loop");
		gameObject.transform.SetParent(base.transform);
		m_oceanAmbientSource = gameObject.AddComponent<AudioSource>();
		m_oceanAmbientSource.loop = true;
		m_oceanAmbientSource.spatialBlend = 0.75f;
		m_oceanAmbientSource.outputAudioMixerGroup = m_ambientMixer;
		m_oceanAmbientSource.maxDistance = 128f;
		m_oceanAmbientSource.minDistance = 40f;
		m_oceanAmbientSource.spread = 90f;
		m_oceanAmbientSource.rolloffMode = AudioRolloffMode.Linear;
		m_oceanAmbientSource.clip = m_oceanAudio;
		m_oceanAmbientSource.bypassReverbZones = true;
		m_oceanAmbientSource.dopplerLevel = 0f;
		m_oceanAmbientSource.volume = 0f;
		m_oceanAmbientSource.priority = 0;
		m_oceanAmbientSource.Play();
		GameObject gameObject2 = new GameObject("ambient_loop");
		gameObject2.transform.SetParent(base.transform);
		m_ambientLoopSource = gameObject2.AddComponent<AudioSource>();
		m_ambientLoopSource.loop = true;
		m_ambientLoopSource.spatialBlend = 0f;
		m_ambientLoopSource.outputAudioMixerGroup = m_ambientMixer;
		m_ambientLoopSource.bypassReverbZones = true;
		m_ambientLoopSource.priority = 0;
		m_ambientLoopSource.volume = 0f;
		GameObject gameObject3 = new GameObject("wind_loop");
		gameObject3.transform.SetParent(base.transform);
		m_windLoopSource = gameObject3.AddComponent<AudioSource>();
		m_windLoopSource.loop = true;
		m_windLoopSource.spatialBlend = 0f;
		m_windLoopSource.outputAudioMixerGroup = m_ambientMixer;
		m_windLoopSource.bypassReverbZones = true;
		m_windLoopSource.clip = m_windAudio;
		m_windLoopSource.volume = 0f;
		m_windLoopSource.priority = 0;
		m_windLoopSource.Play();
		if (m_enableShieldDomeHum)
		{
			GameObject gameObject4 = UnityEngine.Object.Instantiate(m_shieldHumPrefab);
			gameObject4.transform.SetParent(base.transform);
			m_shieldHumSource = gameObject4.GetComponent<AudioSource>();
		}
		m_maxLavaLoops = GetLoopingMaxConcurrency(m_lavaLoopPrefab.GetComponent<ZSFX>());
	}

	private void Start()
	{
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
		{
			AudioListener.volume = 0f;
			return;
		}
		AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", AudioListener.volume);
		SetSFXVolume(PlayerPrefs.GetFloat("SfxVolume", GetSFXVolume()));
	}

	private void OnApplicationQuit()
	{
		StopAllAudio();
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	private void StopAllAudio()
	{
		AudioSource[] array = UnityEngine.Object.FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		m_zoneSystemValid = ZoneSystem.instance != null;
		m_envManValid = EnvMan.instance != null;
		m_listenerPos = GetActiveAudioListener().transform.position;
		UpdateAmbientLoop(deltaTime);
		UpdateRandomAmbient(deltaTime);
		UpdateLavaAmbient(deltaTime);
		UpdateSnapshots(deltaTime);
		UpdateLoopingConcurrency();
		UpdateShieldHum();
	}

	private void UpdateShieldHum()
	{
		if (!m_enableShieldDomeHum)
		{
			return;
		}
		if (ShieldGenerator.HasShields())
		{
			if (!m_shieldHumSource.isPlaying)
			{
				m_shieldHumSource.Play();
			}
			m_shieldHumSource.transform.position = ShieldGenerator.GetClosestShieldPoint(GetActiveAudioListener().transform.position);
		}
		else
		{
			m_shieldHumSource.Stop();
		}
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		m_zoneSystemValid = ZoneSystem.instance != null;
		m_envManValid = EnvMan.instance != null;
		UpdateOceanAmbiance(fixedDeltaTime);
		UpdateWindAmbience(fixedDeltaTime);
	}

	public static float GetSFXVolume()
	{
		if (m_instance == null)
		{
			return 1f;
		}
		m_instance.m_masterMixer.GetFloat("SfxVol", out var value);
		if (!(value > -80f))
		{
			return 0f;
		}
		return Mathf.Pow(10f, value / 10f);
	}

	public static void SetSFXVolume(float vol)
	{
		if (!(m_instance == null))
		{
			float value = ((vol > 0f) ? (Mathf.Log10(Mathf.Clamp(vol, 0.001f, 1f)) * 10f) : (-80f));
			m_instance.m_masterMixer.SetFloat("SfxVol", value);
			m_instance.m_masterMixer.SetFloat("GuiVol", value);
		}
	}

	private void UpdateRandomAmbient(float dt)
	{
		if (InMenu())
		{
			return;
		}
		m_randomAmbientTimer += dt;
		if (!(m_randomAmbientTimer > m_randomAmbientInterval))
		{
			return;
		}
		m_randomAmbientTimer = 0f;
		if (!(UnityEngine.Random.value <= m_randomAmbientChance))
		{
			return;
		}
		float fadeoutDuration = 0f;
		if (SelectRandomAmbientClip(out var clip, out fadeoutDuration))
		{
			Vector3 randomAmbiencePoint = GetRandomAmbiencePoint();
			GameObject gameObject = UnityEngine.Object.Instantiate(m_randomAmbientPrefab, randomAmbiencePoint, Quaternion.identity, base.transform);
			ZSFX component = gameObject.GetComponent<ZSFX>();
			component.m_audioClips = new AudioClip[1] { clip };
			component.Play();
			TimedDestruction component2 = gameObject.GetComponent<TimedDestruction>();
			if (fadeoutDuration > 0f)
			{
				component.m_fadeOutDelay = 0f;
				component.m_fadeOutDuration = fadeoutDuration;
				component.m_fadeOutOnAwake = true;
				component2.m_timeout = fadeoutDuration + 2f;
			}
			else
			{
				component.m_fadeOutDelay = clip.length - 1f;
				component.m_fadeOutDuration = 1f;
				component.m_fadeOutOnAwake = true;
				component2.m_timeout = clip.length * 1.5f;
			}
			component2.Trigger();
		}
	}

	private void UpdateLavaAmbient(float dt)
	{
		ScanForLava();
		UpdateLavaAmbientLoops();
		if (InMenu() || m_validLavaPositions.Size == 0 || !m_envManValid || EnvMan.instance.GetCurrentBiome() != Heightmap.Biome.AshLands)
		{
			return;
		}
		m_lavaAmbientTimer += dt;
		if (m_lavaAmbientTimer < m_lavaNoiseInterval)
		{
			return;
		}
		m_lavaAmbientTimer = 0f;
		if (UnityEngine.Random.value > m_lavaNoiseChance)
		{
			return;
		}
		int i = 0;
		Vector3 vector = Vector3.zero;
		for (; i < 5; i++)
		{
			vector = m_validLavaPositions[UnityEngine.Random.Range(0, m_validLavaPositions.Size - 1)];
			float num = vector.DistanceTo(GetActiveAudioListener().transform.position);
			if (num > m_lavaNoiseMinDistance && num < m_lavaNoiseMaxDistance)
			{
				break;
			}
		}
		if (i != 5)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(m_randomAmbientPrefab, vector, Quaternion.identity, base.transform);
			ZSFX component = gameObject.GetComponent<ZSFX>();
			AudioClip audioClip = m_randomLavaNoises[UnityEngine.Random.Range(0, m_randomLavaNoises.Count - 1)];
			component.m_audioClips = new AudioClip[1] { audioClip };
			component.Play();
			TimedDestruction component2 = gameObject.GetComponent<TimedDestruction>();
			component2.m_timeout = audioClip.length;
			component2.Trigger();
		}
	}

	private void UpdateLavaAmbientLoops()
	{
		if (Time.frameCount % 24 != 0)
		{
			return;
		}
		if (m_ambientLavaLoops.Count < m_maxLavaLoops && m_validLavaPositions.Size > 0 && m_envManValid && EnvMan.instance.GetCurrentBiome() == Heightmap.Biome.AshLands)
		{
			Vector3 vector = m_validLavaPositions[UnityEngine.Random.Range(0, m_validLavaPositions.Size - 1)];
			float num = float.PositiveInfinity;
			foreach (ZSFX ambientLavaLoop in m_ambientLavaLoops)
			{
				Vector3 position = ambientLavaLoop.transform.position;
				float num2 = position.DistanceTo(vector);
				if (num2 < num && ambientLavaLoop.transform.position != position)
				{
					num = num2;
				}
			}
			if (num <= m_minDistanceBetweenLavaLoops)
			{
				return;
			}
			ZSFX component = UnityEngine.Object.Instantiate(m_lavaLoopPrefab, vector, Quaternion.identity).GetComponent<ZSFX>();
			component.OnDestroyingSfx += delegate(ZSFX zsfx)
			{
				if (m_ambientLavaLoops.Contains(zsfx))
				{
					m_ambientLavaLoops.Remove(zsfx);
				}
			};
			m_ambientLavaLoops.Add(component);
		}
		for (int num3 = m_ambientLavaLoops.Count - 1; num3 >= 0; num3--)
		{
			ZSFX zSFX = m_ambientLavaLoops[num3];
			if (!(zSFX.gameObject.transform.position.DistanceTo(m_listenerPos) < m_maxLavaLoopDistance))
			{
				zSFX.GetComponent<TimedDestruction>().Trigger();
				m_ambientLavaLoops.Remove(zSFX);
			}
		}
	}

	private void ScanForLava()
	{
		if (Time.frameCount % 12 == 0 && m_envManValid && EnvMan.instance.GetCurrentBiome() == Heightmap.Biome.AshLands && m_zoneSystemValid)
		{
			Vector2 normalized = UnityEngine.Random.insideUnitCircle.normalized;
			normalized *= UnityEngine.Random.Range(2f, m_lavaScanRadius);
			Vector3 position = m_listenerPos + new Vector3(normalized.x, 0f, normalized.y);
			if (ZoneSystem.instance.IsLava(ref position))
			{
				m_validLavaPositions.PushFront(position);
			}
		}
	}

	private Vector3 GetRandomAmbiencePoint()
	{
		float f = UnityEngine.Random.value * (float)Math.PI * 2f;
		float num = UnityEngine.Random.Range(m_randomMinDistance, m_randomMaxDistance);
		return m_listenerPos + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
	}

	private bool SelectRandomAmbientClip(out AudioClip clip, out float fadeoutDuration)
	{
		fadeoutDuration = 0f;
		clip = null;
		if (!m_envManValid)
		{
			return false;
		}
		EnvSetup currentEnvironment = EnvMan.instance.GetCurrentEnvironment();
		BiomeAmbients biomeAmbients = null;
		biomeAmbients = ((currentEnvironment == null || string.IsNullOrEmpty(currentEnvironment.m_ambientList)) ? GetBiomeAmbients(EnvMan.instance.GetCurrentBiome()) : GetAmbients(currentEnvironment.m_ambientList));
		if (biomeAmbients == null)
		{
			return false;
		}
		fadeoutDuration = biomeAmbients.m_forceFadeout;
		List<AudioClip> list = new List<AudioClip>(biomeAmbients.m_randomAmbientClips);
		List<AudioClip> collection = (EnvMan.IsDaylight() ? biomeAmbients.m_randomAmbientClipsDay : biomeAmbients.m_randomAmbientClipsNight);
		list.AddRange(collection);
		if (list.Count == 0)
		{
			return false;
		}
		clip = list[UnityEngine.Random.Range(0, list.Count)];
		return true;
	}

	private void UpdateAmbientLoop(float dt)
	{
		if (!m_envManValid)
		{
			m_ambientLoopSource.Stop();
		}
		else if ((bool)m_queuedAmbientLoop || m_stopAmbientLoop)
		{
			if (!m_ambientLoopSource.isPlaying || m_ambientLoopSource.volume <= 0f)
			{
				m_ambientLoopSource.Stop();
				m_stopAmbientLoop = false;
				if ((bool)m_queuedAmbientLoop)
				{
					m_ambientLoopSource.clip = m_queuedAmbientLoop;
					m_ambientLoopSource.volume = 0f;
					m_ambientLoopSource.Play();
					m_ambientVol = m_queuedAmbientVol;
					m_queuedAmbientLoop = null;
				}
			}
			else
			{
				m_ambientLoopSource.volume = Mathf.MoveTowards(m_ambientLoopSource.volume, 0f, dt / m_ambientFadeTime);
			}
		}
		else if (m_ambientLoopSource.isPlaying)
		{
			m_ambientLoopSource.volume = Mathf.MoveTowards(m_ambientLoopSource.volume, m_ambientVol, dt / m_ambientFadeTime);
		}
	}

	public void SetIndoor(bool indoor)
	{
		m_indoor = indoor;
	}

	private bool InMenu()
	{
		if (!(FejdStartup.instance != null) && !Menu.IsVisible() && (!Game.instance || !Game.instance.WaitingForRespawn()))
		{
			return TextViewer.IsShowingIntro();
		}
		return true;
	}

	private void UpdateSnapshots(float dt)
	{
		if (InMenu())
		{
			SetSnapshot(Snapshot.Menu);
		}
		else if (m_indoor)
		{
			SetSnapshot(Snapshot.Indoor);
		}
		else
		{
			SetSnapshot(Snapshot.Default);
		}
	}

	private void SetSnapshot(Snapshot snapshot)
	{
		if (m_currentSnapshot != snapshot)
		{
			m_currentSnapshot = snapshot;
			switch (snapshot)
			{
			case Snapshot.Default:
				m_masterMixer.FindSnapshot("Default").TransitionTo(m_snapshotTransitionTime);
				break;
			case Snapshot.Indoor:
				m_masterMixer.FindSnapshot("Indoor").TransitionTo(m_snapshotTransitionTime);
				break;
			case Snapshot.Menu:
				m_masterMixer.FindSnapshot("Menu").TransitionTo(m_snapshotTransitionTime);
				break;
			}
		}
	}

	public void StopAmbientLoop()
	{
		m_queuedAmbientLoop = null;
		m_stopAmbientLoop = true;
	}

	public void QueueAmbientLoop(AudioClip clip, float vol)
	{
		if ((!(m_queuedAmbientLoop == clip) || m_queuedAmbientVol != vol) && (!(m_queuedAmbientLoop == null) || !(m_ambientLoopSource.clip == clip) || m_ambientVol != vol))
		{
			m_queuedAmbientLoop = clip;
			m_queuedAmbientVol = vol;
			m_stopAmbientLoop = false;
		}
	}

	private void UpdateWindAmbience(float dt)
	{
		if (!m_zoneSystemValid || !m_envManValid)
		{
			m_windLoopSource.volume = 0f;
			return;
		}
		float windIntensity = EnvMan.instance.GetWindIntensity();
		windIntensity = Mathf.Pow(windIntensity, m_windIntensityPower);
		windIntensity += windIntensity * Mathf.Sin(Time.time) * Mathf.Sin(Time.time * 1.54323f) * Mathf.Sin(Time.time * 2.31237f) * m_windVariation;
		m_windLoopSource.volume = Mathf.Lerp(m_windMinVol, m_windMaxVol, windIntensity);
		m_windLoopSource.pitch = Mathf.Lerp(m_windMinPitch, m_windMaxPitch, windIntensity);
	}

	private void UpdateOceanAmbiance(float dt)
	{
		if (!m_zoneSystemValid || !m_envManValid)
		{
			m_oceanAmbientSource.volume = 0f;
			return;
		}
		m_oceanUpdateTimer += dt;
		if (m_oceanUpdateTimer > 2f)
		{
			m_oceanUpdateTimer = 0f;
			m_haveOcean = FindAverageOceanPoint(out m_avgOceanPoint);
		}
		if (m_haveOcean)
		{
			float windIntensity = EnvMan.instance.GetWindIntensity();
			float target = Mathf.Lerp(m_oceanVolumeMin, m_oceanVolumeMax, windIntensity);
			m_oceanAmbientSource.volume = Mathf.MoveTowards(m_oceanAmbientSource.volume, target, m_oceanFadeSpeed * dt);
			m_oceanAmbientSource.transform.position = Vector3.Lerp(m_oceanAmbientSource.transform.position, m_avgOceanPoint, m_oceanMoveSpeed);
		}
		else
		{
			m_oceanAmbientSource.volume = Mathf.MoveTowards(m_oceanAmbientSource.volume, 0f, m_oceanFadeSpeed * dt);
		}
	}

	private bool FindAverageOceanPoint(out Vector3 point)
	{
		Vector3 zero = Vector3.zero;
		int num = 0;
		Vector2i zone = ZoneSystem.instance.GetZone(m_listenerPos);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				Vector2i id = zone;
				id.x += j;
				id.y += i;
				Vector3 zonePos = ZoneSystem.instance.GetZonePos(id);
				if (IsOceanZone(zonePos))
				{
					num++;
					zero += zonePos;
				}
			}
		}
		if (num > 0)
		{
			zero /= (float)num;
			point = zero;
			point.y = ZoneSystem.instance.m_waterLevel;
			return true;
		}
		point = Vector3.zero;
		return false;
	}

	private bool IsOceanZone(Vector3 centerPos)
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(centerPos);
		if (ZoneSystem.instance.m_waterLevel - groundHeight > m_oceanDepthTreshold)
		{
			return true;
		}
		return false;
	}

	private BiomeAmbients GetAmbients(string name)
	{
		foreach (BiomeAmbients randomAmbient in m_randomAmbients)
		{
			if (randomAmbient.m_name == name)
			{
				return randomAmbient;
			}
		}
		return null;
	}

	private BiomeAmbients GetBiomeAmbients(Heightmap.Biome biome)
	{
		foreach (BiomeAmbients randomAmbient in m_randomAmbients)
		{
			if ((randomAmbient.m_biome & biome) != 0)
			{
				return randomAmbient;
			}
		}
		return null;
	}

	public bool RequestPlaySound(ZSFX sfx)
	{
		if (sfx.IsLooping())
		{
			RegisterLoopingSound(sfx);
			return true;
		}
		if (sfx.m_maxConcurrentSources <= 0)
		{
			return true;
		}
		int hash = sfx.m_hash;
		float time = Time.time;
		Vector3 position = sfx.gameObject.transform.position;
		int num = 0;
		foreach (SoundHash sound in m_soundList)
		{
			if (hash == sound.hash)
			{
				if (time - sound.playTime < m_concurrencyThreshold && Vector3.Distance(sound.position, position) < sfx.GetConcurrencyDistance())
				{
					num++;
				}
				if (num >= sfx.m_maxConcurrentSources)
				{
					return false;
				}
			}
		}
		m_soundList.PushFront(new SoundHash(hash, time, position));
		return true;
	}

	private void RegisterLoopingSound(ZSFX sfx)
	{
		if (GetLoopingMaxConcurrency(sfx) < 1)
		{
			return;
		}
		int num = 0;
		foreach (ZSFX item in m_loopingSfx)
		{
			if (item.m_hash == sfx.m_hash)
			{
				num++;
			}
		}
		if (num > sfx.m_maxConcurrentSources)
		{
			sfx.ConcurrencyDisable();
		}
		m_loopingSfx.Add(sfx);
		sfx.OnDestroyingSfx += delegate(ZSFX zsfx)
		{
			m_loopingSfx.Remove(zsfx);
		};
	}

	private int GetLoopingMaxConcurrency(ZSFX sfx)
	{
		if (sfx.m_maxConcurrentSources < 0)
		{
			return -1;
		}
		if (sfx.m_maxConcurrentSources != 0)
		{
			return sfx.m_maxConcurrentSources;
		}
		return m_forcedMaxConcurrentLoops;
	}

	private void UpdateLoopingConcurrency()
	{
		if (Time.frameCount % 16 != 0)
		{
			return;
		}
		Camera cam = Utils.GetMainCamera();
		m_checkedHashes.Clear();
		foreach (ZSFX sfx in m_loopingSfx)
		{
			if (m_checkedHashes.Contains(sfx.m_hash))
			{
				continue;
			}
			m_checkedHashes.Add(sfx.m_hash);
			int maxConcurrentSources = sfx.m_maxConcurrentSources;
			List<ZSFX> list = (from x in m_loopingSfx
				where x.m_hash == sfx.m_hash
				orderby Vector3.Distance(x.gameObject.transform.position, cam.transform.position)
				select x).ToList();
			for (int i = 0; i < list.Count; i++)
			{
				if (i > maxConcurrentSources)
				{
					list[i].ConcurrencyDisable();
				}
				else
				{
					list[i].ConcurrencyEnable();
				}
			}
		}
	}

	public AudioListener GetActiveAudioListener()
	{
		if ((bool)m_activeAudioListener && m_activeAudioListener.isActiveAndEnabled)
		{
			return m_activeAudioListener;
		}
		AudioListener[] array = UnityEngine.Object.FindObjectsOfType<AudioListener>(includeInactive: false);
		m_activeAudioListener = Array.Find(array, (AudioListener l) => l.enabled);
		return m_activeAudioListener;
	}
}
