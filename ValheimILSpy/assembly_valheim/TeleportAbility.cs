using System.Collections.Generic;
using UnityEngine;

public class TeleportAbility : MonoBehaviour, IProjectile
{
	public string m_targetTag = "";

	public string m_message = "";

	public float m_maxTeleportRange = 100f;

	private Character m_owner;

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		m_owner = owner;
		GameObject gameObject = FindTarget();
		if ((bool)gameObject)
		{
			Vector3 position = gameObject.transform.position;
			if (ZoneSystem.instance.FindFloor(position, out position.y))
			{
				m_owner.transform.position = position;
				m_owner.transform.rotation = gameObject.transform.rotation;
				if (m_message.Length > 0)
				{
					Player.MessageAllInRange(base.transform.position, 100f, MessageHud.MessageType.Center, m_message);
				}
			}
		}
		ZNetScene.instance.Destroy(base.gameObject);
	}

	private GameObject FindTarget()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag(m_targetTag);
		List<GameObject> list = new List<GameObject>();
		GameObject[] array2 = array;
		foreach (GameObject gameObject in array2)
		{
			if (!(Vector3.Distance(gameObject.transform.position, m_owner.transform.position) > m_maxTeleportRange))
			{
				list.Add(gameObject);
			}
		}
		if (list.Count == 0)
		{
			ZLog.Log("No valid telport target in range");
			return null;
		}
		return list[Random.Range(0, list.Count)];
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}
}
