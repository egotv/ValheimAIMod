using System;
using UnityEngine;

public class SapCollector : MonoBehaviour, Hoverable, Interactable
{
	public string m_name = "";

	public Transform m_spawnPoint;

	public GameObject m_workingEffect;

	public GameObject m_notEmptyEffect;

	public float m_secPerUnit = 10f;

	public int m_maxLevel = 4;

	public ItemDrop m_spawnItem;

	public EffectList m_spawnEffect = new EffectList();

	public ZNetView m_mustConnectTo;

	public bool m_rayCheckConnectedBelow;

	[Header("Texts")]
	public string m_extractText = "$piece_sapcollector_extract";

	public string m_drainingText = "$piece_sapcollector_draining";

	public string m_drainingSlowText = "$piece_sapcollector_drainingslow";

	public string m_notConnectedText = "$piece_sapcollector_notconnected";

	public string m_fullText = "$piece_sapcollector_isfull";

	private ZNetView m_nview;

	private Collider m_collider;

	private Piece m_piece;

	private ZNetView m_connectedObject;

	private ResourceRoot m_root;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_collider = GetComponentInChildren<Collider>();
		m_piece = GetComponent<Piece>();
		if (m_nview.GetZDO() != null)
		{
			if (m_nview.IsOwner() && m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, 0L) == 0L)
			{
				m_nview.GetZDO().Set(ZDOVars.s_lastTime, ZNet.instance.GetTime().Ticks);
			}
			m_nview.Register("RPC_Extract", RPC_Extract);
			m_nview.Register("RPC_UpdateEffects", RPC_UpdateEffects);
			InvokeRepeating("UpdateTick", UnityEngine.Random.Range(0f, 2f), 5f);
		}
	}

	public string GetHoverText()
	{
		int level = GetLevel();
		string statusText = GetStatusText();
		string text = m_name + " ( " + statusText + ", " + level + " / " + m_maxLevel + " )";
		if (level > 0)
		{
			text = text + "\n[<color=yellow><b>$KEY_Use</b></color>] " + m_extractText;
		}
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return true;
		}
		if (GetLevel() > 0)
		{
			Extract();
			Game.instance.IncrementPlayerStat(PlayerStatType.SapHarvested);
			return true;
		}
		return false;
	}

	private string GetStatusText()
	{
		if (GetLevel() >= m_maxLevel)
		{
			return m_fullText;
		}
		if (!m_root)
		{
			return m_notConnectedText;
		}
		if (m_root.IsLevelLow())
		{
			return m_drainingSlowText;
		}
		return m_drainingText;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void Extract()
	{
		m_nview.InvokeRPC("RPC_Extract");
	}

	private void RPC_Extract(long caller)
	{
		int level = GetLevel();
		if (level > 0)
		{
			m_spawnEffect.Create(m_spawnPoint.position, Quaternion.identity);
			for (int i = 0; i < level; i++)
			{
				Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere;
				Vector3 position = m_spawnPoint.position + insideUnitSphere * 0.2f;
				UnityEngine.Object.Instantiate(m_spawnItem, position, Quaternion.identity).GetComponent<ItemDrop>()?.SetStack(Game.instance.ScaleDrops(m_spawnItem.m_itemData, 1));
			}
			ResetLevel();
			m_nview.InvokeRPC(ZNetView.Everybody, "RPC_UpdateEffects");
		}
	}

	private float GetTimeSinceLastUpdate()
	{
		DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong(ZDOVars.s_lastTime, ZNet.instance.GetTime().Ticks));
		DateTime time = ZNet.instance.GetTime();
		TimeSpan timeSpan = time - dateTime;
		m_nview.GetZDO().Set(ZDOVars.s_lastTime, time.Ticks);
		double num = timeSpan.TotalSeconds;
		if (num < 0.0)
		{
			num = 0.0;
		}
		return (float)num;
	}

	private void ResetLevel()
	{
		m_nview.GetZDO().Set(ZDOVars.s_level, 0);
	}

	private void IncreseLevel(int i)
	{
		int level = GetLevel();
		level += i;
		level = Mathf.Clamp(level, 0, m_maxLevel);
		m_nview.GetZDO().Set(ZDOVars.s_level, level);
	}

	private int GetLevel()
	{
		if (m_nview.GetZDO() == null)
		{
			return 0;
		}
		return m_nview.GetZDO().GetInt(ZDOVars.s_level);
	}

	private void UpdateTick()
	{
		if ((bool)m_mustConnectTo && !m_root)
		{
			Collider[] array = Physics.OverlapSphere(base.transform.position, 0.2f);
			for (int i = 0; i < array.Length; i++)
			{
				ResourceRoot componentInParent = array[i].GetComponentInParent<ResourceRoot>();
				if ((object)componentInParent != null)
				{
					m_root = componentInParent;
					break;
				}
			}
		}
		if (m_nview.IsOwner())
		{
			float timeSinceLastUpdate = GetTimeSinceLastUpdate();
			if (GetLevel() < m_maxLevel && (bool)m_root && m_root.CanDrain(1f))
			{
				float @float = m_nview.GetZDO().GetFloat(ZDOVars.s_product);
				@float += timeSinceLastUpdate;
				if (@float > m_secPerUnit)
				{
					int num = (int)(@float / m_secPerUnit);
					if ((bool)m_root)
					{
						num = Mathf.Min((int)m_root.GetLevel(), num);
					}
					if (num > 0)
					{
						IncreseLevel(num);
						if ((bool)m_root)
						{
							m_root.Drain(num);
						}
					}
					@float = 0f;
				}
				m_nview.GetZDO().Set(ZDOVars.s_product, @float);
			}
		}
		UpdateEffects();
	}

	private void RPC_UpdateEffects(long caller)
	{
		UpdateEffects();
	}

	private void UpdateEffects()
	{
		int level = GetLevel();
		bool active = level < m_maxLevel && (bool)m_root && m_root.CanDrain(1f);
		m_notEmptyEffect.SetActive(level > 0);
		m_workingEffect.SetActive(active);
	}
}
