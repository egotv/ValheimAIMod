using UnityEngine;

public class MusicLocation : MonoBehaviour
{
	private float volume;

	public bool m_addRadiusFromLocation = true;

	public float m_radius = 10f;

	public bool m_oneTime = true;

	public bool m_notIfEnemies = true;

	public bool m_forceFade;

	private ZNetView m_nview;

	private AudioSource m_audioSource;

	private float m_baseVolume;

	private bool m_blockLoopAndFade;

	private void Awake()
	{
		m_audioSource = GetComponent<AudioSource>();
		m_baseVolume = m_audioSource.volume;
		m_nview = GetComponent<ZNetView>();
		if ((bool)m_nview)
		{
			m_nview.Register("SetPlayed", SetPlayed);
		}
		if (m_addRadiusFromLocation)
		{
			Location componentInParent = GetComponentInParent<Location>();
			if ((object)componentInParent != null)
			{
				m_radius += componentInParent.GetMaxRadius();
			}
		}
	}

	private void Update()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		float p_X = Vector3.Distance(base.transform.position, Player.m_localPlayer.transform.position);
		float target = 1f - Utils.SmoothStep(m_radius * 0.5f, m_radius, p_X);
		volume = Mathf.MoveTowards(volume, target, Time.deltaTime);
		float num = volume * m_baseVolume * MusicMan.m_masterMusicVolume;
		if (volume > 0f && !m_audioSource.isPlaying && !m_blockLoopAndFade)
		{
			if ((m_oneTime && HasPlayed()) || (m_notIfEnemies && BaseAI.HaveEnemyInRange(Player.m_localPlayer, base.transform.position, m_radius)))
			{
				return;
			}
			m_audioSource.time = 0f;
			m_audioSource.Play();
		}
		if (!Settings.ContinousMusic && m_audioSource.loop)
		{
			m_audioSource.loop = false;
			m_blockLoopAndFade = true;
		}
		if (m_blockLoopAndFade || m_forceFade)
		{
			float num2 = m_audioSource.time - m_audioSource.clip.length + 1.5f;
			if (num2 > 0f)
			{
				num *= 1f - num2 / 1.5f;
			}
			if (Terminal.m_showTests)
			{
				Terminal.m_testList["Music location fade"] = num2 + " " + (1f - num2 / 1.5f);
			}
		}
		m_audioSource.volume = num;
		if (m_blockLoopAndFade && volume <= 0f)
		{
			m_blockLoopAndFade = false;
			m_audioSource.loop = true;
		}
		if (Terminal.m_showTests && m_audioSource.isPlaying)
		{
			Terminal.m_testList["Music location current"] = m_audioSource.name;
			Terminal.m_testList["Music location vol / volume"] = num + " / " + volume;
			if (ZInput.GetKeyDown(KeyCode.N) && ZInput.GetKey(KeyCode.LeftShift))
			{
				m_audioSource.time = m_audioSource.clip.length - 4f;
			}
		}
		if (m_oneTime && volume > 0f && m_audioSource.time > m_audioSource.clip.length * 0.75f && !HasPlayed())
		{
			SetPlayed();
		}
	}

	private void SetPlayed()
	{
		m_nview.InvokeRPC("SetPlayed");
	}

	private void SetPlayed(long sender)
	{
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(ZDOVars.s_played, value: true);
			ZLog.Log("Setting location music as played");
		}
	}

	private bool HasPlayed()
	{
		return m_nview.GetZDO().GetBool(ZDOVars.s_played);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.5f);
		Gizmos.DrawWireSphere(base.transform.position, m_radius);
	}
}
