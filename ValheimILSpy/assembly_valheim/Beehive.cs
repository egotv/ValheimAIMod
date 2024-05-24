using System;
using UnityEngine;

public class Beehive : MonoBehaviour, Hoverable, Interactable
{
	public string m_name = "";

	public Transform m_coverPoint;

	public Transform m_spawnPoint;

	public GameObject m_beeEffect;

	public bool m_effectOnlyInDaylight = true;

	public float m_maxCover = 0.25f;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	public float m_secPerUnit = 10f;

	public int m_maxHoney = 4;

	public ItemDrop m_honeyItem;

	public EffectList m_spawnEffect = new EffectList();

	[Header("Texts")]
	public string m_extractText = "$piece_beehive_extract";

	public string m_checkText = "$piece_beehive_check";

	public string m_areaText = "$piece_beehive_area";

	public string m_freespaceText = "$piece_beehive_freespace";

	public string m_sleepText = "$piece_beehive_sleep";

	public string m_happyText = "$piece_beehive_happy";

	public string m_notConnectedText;

	public string m_blockedText;

	private ZNetView m_nview;

	private Collider m_collider;

	private Piece m_piece;

	private ZNetView m_connectedObject;

	private Piece m_blockingPiece;

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
			InvokeRepeating("UpdateBees", 0f, 10f);
		}
	}

	public string GetHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(m_name + "\n$piece_noaccess");
		}
		int honeyLevel = GetHoneyLevel();
		if (honeyLevel > 0)
		{
			return Localization.instance.Localize($"{m_name} ( {m_honeyItem.m_itemData.m_shared.m_name} x {honeyLevel} )\n[<color=yellow><b>$KEY_Use</b></color>] {m_extractText}");
		}
		return Localization.instance.Localize(m_name + " ( $piece_container_empty )\n[<color=yellow><b>$KEY_Use</b></color>] " + m_checkText);
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
		if (GetHoneyLevel() > 0)
		{
			Extract();
			Game.instance.IncrementPlayerStat(PlayerStatType.BeesHarvested);
		}
		else
		{
			if (!CheckBiome())
			{
				character.Message(MessageHud.MessageType.Center, m_areaText);
				return true;
			}
			if (!HaveFreeSpace())
			{
				character.Message(MessageHud.MessageType.Center, m_freespaceText);
				return true;
			}
			if (!EnvMan.IsDaylight() && m_effectOnlyInDaylight)
			{
				character.Message(MessageHud.MessageType.Center, m_sleepText);
				return true;
			}
			character.Message(MessageHud.MessageType.Center, m_happyText);
		}
		return true;
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
		int honeyLevel = GetHoneyLevel();
		if (honeyLevel > 0)
		{
			m_spawnEffect.Create(m_spawnPoint.position, Quaternion.identity);
			for (int i = 0; i < honeyLevel; i++)
			{
				Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
				Vector3 position = m_spawnPoint.position + new Vector3(vector.x, 0.25f * (float)i, vector.y);
				UnityEngine.Object.Instantiate(m_honeyItem, position, Quaternion.identity).GetComponent<ItemDrop>()?.SetStack(Game.instance.ScaleDrops(m_honeyItem.m_itemData, 1));
			}
			ResetLevel();
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
		int honeyLevel = GetHoneyLevel();
		honeyLevel += i;
		honeyLevel = Mathf.Clamp(honeyLevel, 0, m_maxHoney);
		m_nview.GetZDO().Set(ZDOVars.s_level, honeyLevel);
	}

	private int GetHoneyLevel()
	{
		return m_nview.GetZDO().GetInt(ZDOVars.s_level);
	}

	private void UpdateBees()
	{
		bool flag = CheckBiome() && HaveFreeSpace();
		bool active = flag && (!m_effectOnlyInDaylight || EnvMan.IsDaylight());
		m_beeEffect.SetActive(active);
		if (m_nview.IsOwner() && flag)
		{
			float timeSinceLastUpdate = GetTimeSinceLastUpdate();
			float @float = m_nview.GetZDO().GetFloat(ZDOVars.s_product);
			@float += timeSinceLastUpdate;
			if (@float > m_secPerUnit)
			{
				int i = (int)(@float / m_secPerUnit);
				IncreseLevel(i);
				@float = 0f;
			}
			m_nview.GetZDO().Set(ZDOVars.s_product, @float);
		}
	}

	private bool HaveFreeSpace()
	{
		if (m_maxCover <= 0f)
		{
			return true;
		}
		Cover.GetCoverForPoint(m_coverPoint.position, out var coverPercentage, out var _);
		return coverPercentage < m_maxCover;
	}

	private bool CheckBiome()
	{
		return (Heightmap.FindBiome(base.transform.position) & m_biome) != 0;
	}
}
