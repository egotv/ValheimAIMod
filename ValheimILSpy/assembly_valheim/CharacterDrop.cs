using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Character))]
public class CharacterDrop : MonoBehaviour
{
	[Serializable]
	public class Drop
	{
		public GameObject m_prefab;

		public int m_amountMin = 1;

		public int m_amountMax = 1;

		public float m_chance = 1f;

		public bool m_onePerPlayer;

		public bool m_levelMultiplier = true;

		public bool m_dontScale;
	}

	public Vector3 m_spawnOffset = Vector3.zero;

	public List<Drop> m_drops = new List<Drop>();

	private const float m_dropArea = 0.5f;

	private const float m_vel = 5f;

	private bool m_dropsEnabled = true;

	private Character m_character;

	private void Start()
	{
		m_character = GetComponent<Character>();
		if ((bool)m_character)
		{
			Character character = m_character;
			character.m_onDeath = (Action)Delegate.Combine(character.m_onDeath, new Action(OnDeath));
		}
	}

	public void SetDropsEnabled(bool enabled)
	{
		m_dropsEnabled = enabled;
	}

	private void OnDeath()
	{
		if (m_dropsEnabled)
		{
			List<KeyValuePair<GameObject, int>> drops = GenerateDropList();
			Vector3 centerPos = m_character.GetCenterPoint() + base.transform.TransformVector(m_spawnOffset);
			DropItems(drops, centerPos, 0.5f);
		}
	}

	public List<KeyValuePair<GameObject, int>> GenerateDropList()
	{
		List<KeyValuePair<GameObject, int>> list = new List<KeyValuePair<GameObject, int>>();
		int num = ((!m_character) ? 1 : Mathf.Max(1, (int)Mathf.Pow(2f, m_character.GetLevel() - 1)));
		foreach (Drop drop in m_drops)
		{
			if (drop.m_prefab == null)
			{
				continue;
			}
			float num2 = drop.m_chance;
			if (drop.m_levelMultiplier)
			{
				num2 *= (float)num;
			}
			if (UnityEngine.Random.value <= num2)
			{
				int num3 = (drop.m_dontScale ? UnityEngine.Random.Range(drop.m_amountMin, drop.m_amountMax) : Game.instance.ScaleDrops(drop.m_prefab, drop.m_amountMin, drop.m_amountMax));
				if (drop.m_levelMultiplier)
				{
					num3 *= num;
				}
				if (drop.m_onePerPlayer)
				{
					num3 = ZNet.instance.GetNrOfPlayers();
				}
				if (num3 > 100)
				{
					num3 = 100;
				}
				if (num3 > 0)
				{
					list.Add(new KeyValuePair<GameObject, int>(drop.m_prefab, num3));
				}
			}
		}
		return list;
	}

	public static void DropItems(List<KeyValuePair<GameObject, int>> drops, Vector3 centerPos, float dropArea)
	{
		foreach (KeyValuePair<GameObject, int> drop in drops)
		{
			for (int i = 0; i < drop.Value; i++)
			{
				Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
				Vector3 vector = UnityEngine.Random.insideUnitSphere * dropArea;
				GameObject gameObject = UnityEngine.Object.Instantiate(drop.Key, centerPos + vector, rotation);
				ItemDrop component = gameObject.GetComponent<ItemDrop>();
				if ((object)component != null)
				{
					component.m_itemData.m_worldLevel = (byte)Game.m_worldLevel;
				}
				Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
				if ((bool)component2)
				{
					Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere;
					if (insideUnitSphere.y < 0f)
					{
						insideUnitSphere.y = 0f - insideUnitSphere.y;
					}
					component2.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);
				}
			}
		}
	}
}
