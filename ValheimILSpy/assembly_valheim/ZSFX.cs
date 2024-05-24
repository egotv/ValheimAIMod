using System;
using System.Collections.Generic;
using UnityEngine;

public class ZSFX : MonoBehaviour, IMonoUpdater
{
	public bool m_playOnAwake = true;

	[Header("Clips")]
	public AudioClip[] m_audioClips = new AudioClip[0];

	[Header("Audio System")]
	[Tooltip("How many of the same sound can play in a small area? Uses the min distance of 3D sounds, or 1 meter, whichever is higher")]
	public int m_maxConcurrentSources;

	[Tooltip("Ignore the distance check, don't play sound if any other of the same sound were played recently")]
	public bool m_ignoreConcurrencyDistance;

	[Header("Random")]
	public float m_maxPitch = 1f;

	public float m_minPitch = 1f;

	public float m_maxVol = 1f;

	public float m_minVol = 1f;

	[Header("Fade")]
	public float m_fadeInDuration;

	public float m_fadeOutDuration;

	public float m_fadeOutDelay;

	public bool m_fadeOutOnAwake;

	[Header("Pan")]
	public bool m_randomPan;

	public float m_minPan = -1f;

	public float m_maxPan = 1f;

	[Header("Delay")]
	public float m_maxDelay;

	public float m_minDelay;

	[Header("Reverb")]
	public bool m_distanceReverb = true;

	public bool m_useCustomReverbDistance;

	public float m_customReverbDistance = 10f;

	[HideInInspector]
	public int m_hash;

	private const float m_globalReverbDistance = 64f;

	private const float m_minReverbSpread = 45f;

	private const float m_maxReverbSpread = 120f;

	private float m_delay;

	private float m_time;

	private float m_fadeOutTimer = -1f;

	private float m_fadeInTimer = -1f;

	private float m_vol = 1f;

	private float m_concurrencyVolumeModifier = 1f;

	private float m_volumeModifier = 1f;

	private float m_pitchModifier = 1f;

	private float m_reverbPitchModifier;

	private bool m_disabledFromConcurrency;

	private float m_baseSpread;

	private float m_basePitch;

	private float m_updateReverbTimer;

	private AudioSource m_audioSource;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	public event Action<ZSFX> OnDestroyingSfx = delegate
	{
	};

	public void Awake()
	{
		m_delay = UnityEngine.Random.Range(m_minDelay, m_maxDelay);
		m_audioSource = GetComponent<AudioSource>();
		m_baseSpread = m_audioSource.spread;
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		if (m_playOnAwake && m_audioSource.loop)
		{
			m_time = 0f;
			m_delay = UnityEngine.Random.Range(m_minDelay, m_maxDelay);
			m_audioSource.Stop();
		}
		Instances.Remove(this);
	}

	private void OnDestroy()
	{
		this.OnDestroyingSfx(this);
	}

	public void CustomUpdate(float dt, float time)
	{
		if (m_audioSource == null)
		{
			return;
		}
		m_time += dt;
		if (m_delay >= 0f && m_time >= m_delay)
		{
			m_delay = -1f;
			if (m_playOnAwake)
			{
				Play();
			}
		}
		if (IsLooping())
		{
			m_concurrencyVolumeModifier = Mathf.MoveTowards(m_concurrencyVolumeModifier, (!m_disabledFromConcurrency) ? 1 : 0, dt / 0.5f);
		}
		if (!m_audioSource.isPlaying)
		{
			return;
		}
		if (m_distanceReverb && m_audioSource.loop)
		{
			m_updateReverbTimer += dt;
			if (m_updateReverbTimer > 1f)
			{
				m_updateReverbTimer = 0f;
				UpdateReverb();
			}
		}
		if (m_fadeOutOnAwake && m_time > m_fadeOutDelay)
		{
			m_fadeOutOnAwake = false;
			FadeOut();
		}
		float vol = m_vol;
		float num = 1f;
		if (m_fadeOutTimer >= 0f)
		{
			m_fadeOutTimer += dt;
			if (m_fadeOutTimer >= m_fadeOutDuration)
			{
				m_audioSource.volume = 0f;
				Stop();
				return;
			}
			num = 1f - Mathf.Clamp01(m_fadeOutTimer / m_fadeOutDuration);
		}
		else if (m_fadeInTimer >= 0f)
		{
			m_fadeInTimer += dt;
			num = Mathf.Clamp01(m_fadeInTimer / m_fadeInDuration);
			if (m_fadeInTimer > m_fadeInDuration)
			{
				m_fadeInTimer = -1f;
			}
		}
		m_audioSource.volume = vol * num * m_concurrencyVolumeModifier * m_volumeModifier;
		float num2 = m_basePitch * m_pitchModifier;
		num2 -= num2 * m_reverbPitchModifier;
		m_audioSource.pitch = num2;
	}

	public void FadeOut()
	{
		if (m_fadeOutTimer < 0f)
		{
			m_fadeOutTimer = 0f;
		}
	}

	public void Stop()
	{
		if (m_audioSource != null)
		{
			m_audioSource.Stop();
		}
	}

	public bool IsPlaying()
	{
		if (m_audioSource == null)
		{
			return false;
		}
		return m_audioSource.isPlaying;
	}

	private void UpdateReverb()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (m_distanceReverb && m_audioSource.spatialBlend != 0f && mainCamera != null)
		{
			float num = Vector3.Distance(mainCamera.transform.position, base.transform.position);
			bool num2 = Mister.InsideMister(base.transform.position);
			float num3 = (m_useCustomReverbDistance ? m_customReverbDistance : 64f);
			float num4 = Mathf.Clamp01(num / num3);
			float b = Mathf.Clamp01(m_audioSource.maxDistance / num3) * Mathf.Clamp01(num / m_audioSource.maxDistance);
			float num5 = Mathf.Max(num4, b);
			if (num2)
			{
				num5 = Mathf.Lerp(num5, 0f, num4);
				m_reverbPitchModifier = 0.5f * num4;
			}
			m_audioSource.bypassReverbZones = false;
			m_audioSource.reverbZoneMix = num5;
			if (m_baseSpread < 120f)
			{
				float a = Mathf.Max(m_baseSpread, 45f);
				m_audioSource.spread = Mathf.Lerp(a, 120f, num5);
			}
		}
		else
		{
			m_audioSource.bypassReverbZones = true;
		}
	}

	public void Play()
	{
		if (!(m_audioSource == null) && m_audioClips.Length != 0 && m_audioSource.gameObject.activeInHierarchy && AudioMan.instance.RequestPlaySound(this))
		{
			if (m_audioSource.loop && m_disabledFromConcurrency)
			{
				m_concurrencyVolumeModifier = 0f;
			}
			int num = UnityEngine.Random.Range(0, m_audioClips.Length);
			m_audioSource.clip = m_audioClips[num];
			m_audioSource.pitch = UnityEngine.Random.Range(m_minPitch, m_maxPitch);
			m_basePitch = m_audioSource.pitch;
			if (m_randomPan)
			{
				m_audioSource.panStereo = UnityEngine.Random.Range(m_minPan, m_maxPan);
			}
			m_vol = UnityEngine.Random.Range(m_minVol, m_maxVol);
			if (m_fadeInDuration > 0f)
			{
				m_audioSource.volume = 0f;
				m_fadeInTimer = 0f;
			}
			else
			{
				m_audioSource.volume = m_vol;
			}
			UpdateReverb();
			m_audioSource.Play();
		}
	}

	public void GenerateHash()
	{
		m_hash = Guid.NewGuid().GetHashCode();
	}

	public float GetConcurrencyDistance()
	{
		if (!m_ignoreConcurrencyDistance)
		{
			return Mathf.Max(1f, m_audioSource.minDistance);
		}
		return float.PositiveInfinity;
	}

	public void ConcurrencyDisable()
	{
		m_disabledFromConcurrency = true;
	}

	public void ConcurrencyEnable()
	{
		m_disabledFromConcurrency = false;
	}

	public bool IsLooping()
	{
		return m_audioSource.loop;
	}

	public void SetVolumeModifier(float v)
	{
		m_volumeModifier = v;
	}

	public float GetVolumeModifier()
	{
		return m_volumeModifier;
	}

	public void SetPitchModifier(float p)
	{
		m_pitchModifier = p;
	}

	public float GetPitchModifier()
	{
		return m_pitchModifier;
	}
}
