using System.Collections.Generic;
using UnityEngine;

public class ZSyncAnimation : MonoBehaviour, IMonoUpdater
{
	private ZNetView m_nview;

	private Animator m_animator;

	public List<string> m_syncBools = new List<string>();

	public List<string> m_syncFloats = new List<string>();

	public List<string> m_syncInts = new List<string>();

	public bool m_smoothCharacterSpeeds = true;

	private static readonly int s_forwardSpeedID = GetHash("forward_speed");

	private static readonly int s_sidewaySpeedID = GetHash("sideway_speed");

	private static readonly int s_animSpeedID = GetHash("anim_speed");

	private int[] m_boolHashes;

	private bool[] m_boolDefaults;

	private int[] m_floatHashes;

	private float[] m_floatDefaults;

	private int[] m_intHashes;

	private int[] m_intDefaults;

	private const int m_zdoSalt = 438569;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_animator = GetComponentInChildren<Animator>();
		m_animator.logWarnings = false;
		m_nview.Register<string>("SetTrigger", RPC_SetTrigger);
		m_boolHashes = new int[m_syncBools.Count];
		m_boolDefaults = new bool[m_syncBools.Count];
		for (int i = 0; i < m_syncBools.Count; i++)
		{
			m_boolHashes[i] = GetHash(m_syncBools[i]);
			m_boolDefaults[i] = m_animator.GetBool(m_boolHashes[i]);
		}
		m_floatHashes = new int[m_syncFloats.Count];
		m_floatDefaults = new float[m_syncFloats.Count];
		for (int j = 0; j < m_syncFloats.Count; j++)
		{
			m_floatHashes[j] = GetHash(m_syncFloats[j]);
			m_floatDefaults[j] = m_animator.GetFloat(m_floatHashes[j]);
		}
		m_intHashes = new int[m_syncInts.Count];
		m_intDefaults = new int[m_syncInts.Count];
		for (int k = 0; k < m_syncInts.Count; k++)
		{
			m_intHashes[k] = GetHash(m_syncInts[k]);
			m_intDefaults[k] = m_animator.GetInteger(m_intHashes[k]);
		}
		if (m_nview.GetZDO() == null)
		{
			base.enabled = false;
		}
		else
		{
			SyncParameters(Time.fixedDeltaTime);
		}
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	public static int GetHash(string name)
	{
		return Animator.StringToHash(name);
	}

	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (m_nview.IsValid())
		{
			SyncParameters(fixedDeltaTime);
		}
	}

	private void SyncParameters(float fixedDeltaTime)
	{
		ZDO zDO = m_nview.GetZDO();
		if (m_nview.IsOwner())
		{
			zDO.Set(s_animSpeedID, m_animator.speed);
			return;
		}
		for (int i = 0; i < m_boolHashes.Length; i++)
		{
			int num = m_boolHashes[i];
			bool @bool = zDO.GetBool(438569 + num, m_boolDefaults[i]);
			m_animator.SetBool(num, @bool);
		}
		for (int j = 0; j < m_floatHashes.Length; j++)
		{
			int num2 = m_floatHashes[j];
			float @float = zDO.GetFloat(438569 + num2, m_floatDefaults[j]);
			if (m_smoothCharacterSpeeds && (num2 == s_forwardSpeedID || num2 == s_sidewaySpeedID))
			{
				m_animator.SetFloat(num2, @float, 0.2f, fixedDeltaTime);
			}
			else
			{
				m_animator.SetFloat(num2, @float);
			}
		}
		for (int k = 0; k < m_intHashes.Length; k++)
		{
			int num3 = m_intHashes[k];
			int @int = zDO.GetInt(438569 + num3, m_intDefaults[k]);
			m_animator.SetInteger(num3, @int);
		}
		float float2 = zDO.GetFloat(s_animSpeedID, 1f);
		m_animator.speed = float2;
	}

	public void SetTrigger(string name)
	{
		m_nview.InvokeRPC(ZNetView.Everybody, "SetTrigger", name);
	}

	public void SetBool(string name, bool value)
	{
		int hash = GetHash(name);
		SetBool(hash, value);
	}

	public void SetBool(int hash, bool value)
	{
		if (m_animator.GetBool(hash) != value)
		{
			m_animator.SetBool(hash, value);
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(438569 + hash, value);
			}
		}
	}

	public void SetFloat(string name, float value)
	{
		int hash = GetHash(name);
		SetFloat(hash, value);
	}

	public void SetFloat(int hash, float value)
	{
		if (!(Mathf.Abs(m_animator.GetFloat(hash) - value) < 0.01f))
		{
			if (m_smoothCharacterSpeeds && (hash == s_forwardSpeedID || hash == s_sidewaySpeedID))
			{
				m_animator.SetFloat(hash, value, 0.2f, Time.fixedDeltaTime);
			}
			else
			{
				m_animator.SetFloat(hash, value);
			}
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(438569 + hash, value);
			}
		}
	}

	public void SetInt(string name, int value)
	{
		int hash = GetHash(name);
		SetInt(hash, value);
	}

	public void SetInt(int hash, int value)
	{
		if (m_animator.GetInteger(hash) != value)
		{
			m_animator.SetInteger(hash, value);
			if (m_nview.GetZDO() != null && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(438569 + hash, value);
			}
		}
	}

	private void RPC_SetTrigger(long sender, string name)
	{
		m_animator.SetTrigger(name);
	}

	public void SetSpeed(float speed)
	{
		m_animator.speed = speed;
	}

	public bool IsOwner()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.IsOwner();
	}
}
