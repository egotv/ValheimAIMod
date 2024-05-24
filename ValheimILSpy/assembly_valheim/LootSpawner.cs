using System;
using System.Collections.Generic;
using UnityEngine;

public class LootSpawner : MonoBehaviour
{
	public DropTable m_items = new DropTable();

	public EffectList m_spawnEffect = new EffectList();

	public float m_respawnTimeMinuts = 10f;

	public bool m_spawnAtNight = true;

	public bool m_spawnAtDay = true;

	public bool m_spawnWhenEnemiesCleared;

	public float m_enemiesCheckRange = 30f;

	private const float c_TriggerDistance = 20f;

	private ZNetView m_nview;

	private bool m_seenEnemies;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			InvokeRepeating("UpdateSpawner", 10f, 2f);
		}
	}

	private void UpdateSpawner()
	{
		if (!m_nview.IsOwner() || (!m_spawnAtDay && EnvMan.IsDay()) || (!m_spawnAtNight && EnvMan.IsNight()))
		{
			return;
		}
		if (m_spawnWhenEnemiesCleared)
		{
			bool num = IsMonsterInRange(base.transform.position, m_enemiesCheckRange);
			if (num && !m_seenEnemies)
			{
				m_seenEnemies = true;
			}
			if (num || !m_seenEnemies)
			{
				return;
			}
		}
		long @long = m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L);
		DateTime time = ZNet.instance.GetTime();
		DateTime dateTime = new DateTime(@long);
		TimeSpan timeSpan = time - dateTime;
		if ((!(m_respawnTimeMinuts <= 0f) || @long == 0L) && !(timeSpan.TotalMinutes < (double)m_respawnTimeMinuts) && Player.IsPlayerInRange(base.transform.position, 20f))
		{
			List<GameObject> dropList = m_items.GetDropList();
			for (int i = 0; i < dropList.Count; i++)
			{
				Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.3f;
				Vector3 position = base.transform.position + new Vector3(vector.x, 0.3f * (float)i, vector.y);
				Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
				UnityEngine.Object.Instantiate(dropList[i], position, rotation);
			}
			m_spawnEffect.Create(base.transform.position, Quaternion.identity);
			m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
			m_seenEnemies = false;
		}
	}

	public static bool IsMonsterInRange(Vector3 point, float range)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		float time = Time.time;
		foreach (Character item in allCharacters)
		{
			if (item.IsMonsterFaction(time) && Vector3.Distance(item.transform.position, point) < range)
			{
				return true;
			}
		}
		return false;
	}

	private void OnDrawGizmos()
	{
	}
}
