using System.Collections.Generic;
using UnityEngine;

public class OfferingBowl : MonoBehaviour, Hoverable, Interactable
{
	[Header("Tokens")]
	public string m_name = "$piece_offerbowl";

	public string m_useItemText = "$piece_offerbowl_offeritem";

	public string m_usedAltarText = "$msg_offerdone";

	public string m_cantOfferText = "$msg_cantoffer";

	public string m_wrongOfferText = "$msg_offerwrong";

	public string m_incompleteOfferText = "$msg_incompleteoffering";

	[Header("Settings")]
	public ItemDrop m_bossItem;

	public int m_bossItems = 1;

	public GameObject m_bossPrefab;

	public ItemDrop m_itemPrefab;

	public Transform m_itemSpawnPoint;

	public string m_setGlobalKey = "";

	public bool m_renderSpawnAreaGizmos;

	public bool m_alertOnSpawn;

	[Header("Boss")]
	public float m_spawnBossDelay = 5f;

	public float m_spawnBossMaxDistance = 40f;

	public float m_spawnBossMinDistance;

	public float m_spawnBossMaxYDistance = 9999f;

	public int m_getSolidHeightMargin = 1000;

	public bool m_enableSolidHeightCheck = true;

	public float m_spawnPointClearingRadius;

	public float m_spawnYOffset = 1f;

	public Vector3 m_spawnAreaOffset;

	public List<GameObject> m_spawnPoints = new List<GameObject>();

	[Header("Use itemstands")]
	public bool m_useItemStands;

	public string m_itemStandPrefix = "";

	public float m_itemstandMaxRange = 20f;

	[Header("Effects")]
	public EffectList m_fuelAddedEffects = new EffectList();

	public EffectList m_spawnBossStartEffects = new EffectList();

	public EffectList m_spawnBossDoneffects = new EffectList();

	private Vector3 m_bossSpawnPoint;

	private int m_solidRayMask;

	private static readonly Collider[] s_tempColliders = new Collider[1];

	private ZNetView m_nview;

	private Humanoid m_interactUser;

	private ItemDrop.ItemData m_usedSpawnItem;

	private void Awake()
	{
		m_solidRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece");
	}

	private void Start()
	{
		m_nview = GetComponentInParent<ZNetView>();
		if ((bool)m_nview)
		{
			m_nview.Register<Vector3, bool>("RPC_SpawnBoss", RPC_SpawnBoss);
			m_nview.Register("RPC_BossSpawnInitiated", RPC_BossSpawnInitiated);
			m_nview.Register("RPC_RemoveBossSpawnInventoryItems", RPC_RemoveBossSpawnInventoryItems);
		}
	}

	public string GetHoverText()
	{
		if (m_useItemStands)
		{
			return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + m_useItemText);
		}
		return Localization.instance.Localize(m_name + "\n[<color=yellow><b>1-8</b></color>] " + m_useItemText);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold || IsBossSpawnQueued() || !m_useItemStands)
		{
			return false;
		}
		foreach (ItemStand item in FindItemStands())
		{
			if (!item.HaveAttachment())
			{
				user.Message(MessageHud.MessageType.Center, m_incompleteOfferText);
				return false;
			}
		}
		m_interactUser = user;
		InitiateSpawnBoss(GetSpawnPosition(), removeItemsFromInventory: false);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (m_useItemStands)
		{
			return false;
		}
		if (IsBossSpawnQueued())
		{
			return true;
		}
		if (m_bossItem != null)
		{
			if (item.m_shared.m_name == m_bossItem.m_itemData.m_shared.m_name)
			{
				int num = user.GetInventory().CountItems(m_bossItem.m_itemData.m_shared.m_name);
				if (num < m_bossItems)
				{
					if (num == 0 && Game.m_worldLevel > 0 && user.GetInventory().CountItems(m_bossItem.m_itemData.m_shared.m_name, -1, matchWorldLevel: false) > 0)
					{
						user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_ng_the_x") + item.m_shared.m_name + Localization.instance.Localize("$msg_ng_x_is_too_low"));
					}
					else
					{
						user.Message(MessageHud.MessageType.Center, $"{m_incompleteOfferText}: {m_bossItem.m_itemData.m_shared.m_name} {num.ToString()} / {m_bossItems}");
					}
					return true;
				}
				if (m_bossPrefab != null)
				{
					m_usedSpawnItem = item;
					m_interactUser = user;
					InitiateSpawnBoss(GetSpawnPosition(), removeItemsFromInventory: true);
				}
				else if (m_itemPrefab != null && SpawnItem(m_itemPrefab, user as Player))
				{
					user.GetInventory().RemoveItem(item.m_shared.m_name, m_bossItems);
					user.ShowRemovedMessage(m_bossItem.m_itemData, m_bossItems);
					user.Message(MessageHud.MessageType.Center, m_usedAltarText);
					m_fuelAddedEffects.Create(m_itemSpawnPoint.position, base.transform.rotation);
				}
				if (!string.IsNullOrEmpty(m_setGlobalKey))
				{
					ZoneSystem.instance.SetGlobalKey(m_setGlobalKey);
				}
				return true;
			}
			user.Message(MessageHud.MessageType.Center, m_wrongOfferText);
			return true;
		}
		return false;
	}

	private bool SpawnItem(ItemDrop item, Player player)
	{
		if (item.m_itemData.m_shared.m_questItem && player.HaveUniqueKey(item.m_itemData.m_shared.m_name))
		{
			player.Message(MessageHud.MessageType.Center, m_cantOfferText);
			return false;
		}
		Object.Instantiate(item, m_itemSpawnPoint.position, Quaternion.identity);
		return true;
	}

	private Vector3 GetSpawnPosition()
	{
		if (m_spawnPoints.Count > 0)
		{
			return m_spawnPoints[Random.Range(0, m_spawnPoints.Count)].transform.position;
		}
		Vector3 vector = base.transform.localToWorldMatrix * m_spawnAreaOffset;
		return base.transform.position + vector;
	}

	private void InitiateSpawnBoss(Vector3 point, bool removeItemsFromInventory)
	{
		m_nview.InvokeRPC("RPC_SpawnBoss", point, removeItemsFromInventory);
	}

	private void RPC_SpawnBoss(long senderId, Vector3 point, bool removeItemsFromInventory)
	{
		if (m_nview.IsOwner() && !IsBossSpawnQueued() && CanSpawnBoss(point, out var spawnPoint) && ((bool)m_nview || !m_nview.IsValid()))
		{
			SpawnBoss(spawnPoint);
			m_nview.InvokeRPC(senderId, "RPC_BossSpawnInitiated");
			if (removeItemsFromInventory)
			{
				m_nview.InvokeRPC(senderId, "RPC_RemoveBossSpawnInventoryItems");
			}
			else
			{
				RemoveAltarItems();
			}
		}
	}

	private void SpawnBoss(Vector3 spawnPoint)
	{
		Invoke("DelayedSpawnBoss", m_spawnBossDelay);
		m_spawnBossStartEffects.Create(spawnPoint, Quaternion.identity);
		m_bossSpawnPoint = spawnPoint;
	}

	private void RPC_RemoveBossSpawnInventoryItems(long senderId)
	{
		m_interactUser.GetInventory().RemoveItem(m_usedSpawnItem.m_shared.m_name, m_bossItems);
		m_interactUser.ShowRemovedMessage(m_bossItem.m_itemData, m_bossItems);
		m_interactUser.Message(MessageHud.MessageType.Center, m_usedAltarText);
		if ((bool)m_itemSpawnPoint)
		{
			m_fuelAddedEffects.Create(m_itemSpawnPoint.position, base.transform.rotation);
		}
	}

	private void RemoveAltarItems()
	{
		foreach (ItemStand item in FindItemStands())
		{
			item.DestroyAttachment();
		}
		if ((bool)m_itemSpawnPoint)
		{
			m_fuelAddedEffects.Create(m_itemSpawnPoint.position, base.transform.rotation);
		}
	}

	private void RPC_BossSpawnInitiated(long senderId)
	{
		m_interactUser.Message(MessageHud.MessageType.Center, m_usedAltarText);
	}

	private bool CanSpawnBoss(Vector3 point, out Vector3 spawnPoint)
	{
		spawnPoint = Vector3.zero;
		for (int i = 0; i < 100; i++)
		{
			Vector2 vector = Random.insideUnitCircle * m_spawnBossMaxDistance;
			spawnPoint = point + new Vector3(vector.x, 0f, vector.y);
			if (m_enableSolidHeightCheck)
			{
				ZoneSystem.instance.GetSolidHeight(spawnPoint, out var height, m_getSolidHeightMargin);
				if (height < 0f || Mathf.Abs(height - base.transform.position.y) > m_spawnBossMaxYDistance || Vector3.Distance(spawnPoint, point) < m_spawnBossMinDistance)
				{
					continue;
				}
				if (m_spawnPointClearingRadius > 0f)
				{
					spawnPoint.y = height + m_spawnYOffset;
					int num = Physics.OverlapSphereNonAlloc(spawnPoint, m_spawnPointClearingRadius, null, m_solidRayMask);
					if (num > 0)
					{
						ZLog.Log(num);
						continue;
					}
				}
				spawnPoint.y = height + m_spawnYOffset;
			}
			return true;
		}
		return false;
	}

	private bool IsBossSpawnQueued()
	{
		return IsInvoking("DelayedSpawnBoss");
	}

	private void DelayedSpawnBoss()
	{
		GameObject gameObject = Object.Instantiate(m_bossPrefab, m_bossSpawnPoint, Quaternion.identity);
		BaseAI component = gameObject.GetComponent<BaseAI>();
		if (component != null)
		{
			component.SetPatrolPoint();
			if (m_alertOnSpawn)
			{
				component.Alert();
			}
		}
		GameObject[] array = m_spawnBossDoneffects.Create(m_bossSpawnPoint, Quaternion.identity);
		for (int i = 0; i < array.Length; i++)
		{
			IProjectile[] componentsInChildren = array[i].GetComponentsInChildren<IProjectile>();
			if (componentsInChildren.Length != 0)
			{
				IProjectile[] array2 = componentsInChildren;
				for (int j = 0; j < array2.Length; j++)
				{
					array2[j].Setup(gameObject.GetComponent<Character>(), Vector3.zero, -1f, null, null, null);
				}
			}
		}
	}

	private List<ItemStand> FindItemStands()
	{
		List<ItemStand> list = new List<ItemStand>();
		ItemStand[] array = Object.FindObjectsOfType<ItemStand>();
		foreach (ItemStand itemStand in array)
		{
			if (!(Vector3.Distance(base.transform.position, itemStand.transform.position) > m_itemstandMaxRange) && itemStand.gameObject.name.CustomStartsWith(m_itemStandPrefix))
			{
				list.Add(itemStand);
			}
		}
		return list;
	}

	private void OnDrawGizmosSelected()
	{
		if (m_renderSpawnAreaGizmos)
		{
			Gizmos.color = Color.green;
			Utils.DrawGizmoCylinder(GetSpawnPosition(), m_spawnBossMaxDistance, m_spawnBossMaxYDistance, 32);
			Gizmos.color = Color.red;
			if (m_spawnBossMinDistance > 0f)
			{
				Utils.DrawGizmoCylinder(GetSpawnPosition(), m_spawnBossMinDistance, m_spawnBossMaxYDistance, 32);
			}
		}
	}
}
