using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DropTable
{
	[Serializable]
	public struct DropData
	{
		public GameObject m_item;

		public int m_stackMin;

		public int m_stackMax;

		public float m_weight;

		public bool m_dontScale;
	}

	private static List<DropData> drops = new List<DropData>();

	private static List<ItemDrop.ItemData> toDrop = new List<ItemDrop.ItemData>();

	private static List<DropData> dropsTemp = new List<DropData>();

	public List<DropData> m_drops = new List<DropData>();

	public int m_dropMin = 1;

	public int m_dropMax = 1;

	[Range(0f, 1f)]
	public float m_dropChance = 1f;

	public bool m_oneOfEach;

	public DropTable Clone()
	{
		return MemberwiseClone() as DropTable;
	}

	public List<ItemDrop.ItemData> GetDropListItems()
	{
		toDrop.Clear();
		if (m_drops.Count == 0)
		{
			return toDrop;
		}
		if (UnityEngine.Random.value > m_dropChance)
		{
			return toDrop;
		}
		drops.Clear();
		drops.AddRange(m_drops);
		float num = 0f;
		foreach (DropData drop in drops)
		{
			num += drop.m_weight;
		}
		int num2 = UnityEngine.Random.Range(m_dropMin, m_dropMax + 1);
		for (int i = 0; i < num2; i++)
		{
			float num3 = UnityEngine.Random.Range(0f, num);
			bool flag = false;
			float num4 = 0f;
			foreach (DropData drop2 in drops)
			{
				num4 += drop2.m_weight;
				if (num3 <= num4)
				{
					flag = true;
					AddItemToList(toDrop, drop2);
					if (m_oneOfEach)
					{
						drops.Remove(drop2);
						num -= drop2.m_weight;
					}
					break;
				}
			}
			if (!flag && drops.Count > 0)
			{
				AddItemToList(toDrop, drops[0]);
			}
		}
		return toDrop;
	}

	private void AddItemToList(List<ItemDrop.ItemData> toDrop, DropData data)
	{
		ItemDrop.ItemData itemData = data.m_item.GetComponent<ItemDrop>().m_itemData;
		ItemDrop.ItemData itemData2 = itemData.Clone();
		itemData2.m_dropPrefab = data.m_item;
		int num = Mathf.Max(1, data.m_stackMin);
		int num2 = Mathf.Min(itemData.m_shared.m_maxStackSize, data.m_stackMax);
		itemData2.m_stack = (data.m_dontScale ? UnityEngine.Random.Range(num, num2 + 1) : Game.instance.ScaleDrops(itemData2, num, num2 + 1));
		itemData2.m_worldLevel = (byte)Game.m_worldLevel;
		toDrop.Add(itemData2);
	}

	public List<GameObject> GetDropList()
	{
		int amount = UnityEngine.Random.Range(m_dropMin, m_dropMax + 1);
		return GetDropList(amount);
	}

	private List<GameObject> GetDropList(int amount)
	{
		List<GameObject> list = new List<GameObject>();
		if (m_drops.Count == 0)
		{
			return list;
		}
		if (UnityEngine.Random.value > m_dropChance)
		{
			return list;
		}
		float num = Mathf.Ceil(Game.m_resourceRate);
		for (int i = 0; (float)i < num; i++)
		{
			dropsTemp.Clear();
			dropsTemp.AddRange(m_drops);
			bool num2 = (float)(i + 1) == num;
			float num3 = Game.m_resourceRate % 1f;
			if (num3 == 0f)
			{
				num3 = 1f;
			}
			float num4 = ((!num2) ? 1f : ((num3 == 0f) ? 1f : num3));
			float num5 = 0f;
			foreach (DropData item in dropsTemp)
			{
				num5 += item.m_weight;
				if (item.m_weight <= 0f && dropsTemp.Count > 1)
				{
					ZLog.LogWarning($"Droptable item '{item.m_item}' has a weight of 0 and will not be dropped correctly!");
				}
			}
			if (num4 < 1f && amount > dropsTemp.Count)
			{
				amount = (int)Mathf.Max(1f, Mathf.Round((float)amount * num4));
			}
			for (int j = 0; j < amount; j++)
			{
				float num6 = UnityEngine.Random.Range(0f, num5);
				bool flag = false;
				float num7 = 0f;
				foreach (DropData item2 in dropsTemp)
				{
					num7 += item2.m_weight;
					if (num6 <= num7)
					{
						flag = true;
						int num8 = 0;
						num8 = ((!item2.m_dontScale) ? ((int)Mathf.Max(1f, Mathf.Round(UnityEngine.Random.Range(Mathf.Round((float)item2.m_stackMin * num4), Mathf.Round((float)item2.m_stackMax * num4))))) : ((i == 0) ? UnityEngine.Random.Range(item2.m_stackMin, item2.m_stackMax) : 0));
						for (int k = 0; k < num8; k++)
						{
							list.Add(item2.m_item);
						}
						if (m_oneOfEach)
						{
							dropsTemp.Remove(item2);
							num5 -= item2.m_weight;
						}
						break;
					}
				}
				if (!flag && dropsTemp.Count > 0)
				{
					list.Add(dropsTemp[0].m_item);
				}
			}
		}
		return list;
	}

	public bool IsEmpty()
	{
		return m_drops.Count == 0;
	}
}
