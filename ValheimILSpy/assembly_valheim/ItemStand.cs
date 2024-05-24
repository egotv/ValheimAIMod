using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemStand : MonoBehaviour, Interactable, Hoverable
{
	public ZNetView m_netViewOverride;

	public string m_name = "";

	public Transform m_attachOther;

	public Transform m_dropSpawnPoint;

	public bool m_canBeRemoved = true;

	public bool m_autoAttach;

	public List<ItemDrop.ItemData.ItemType> m_supportedTypes = new List<ItemDrop.ItemData.ItemType>();

	public List<ItemDrop> m_unsupportedItems = new List<ItemDrop>();

	public List<ItemDrop> m_supportedItems = new List<ItemDrop>();

	public EffectList m_effects = new EffectList();

	public EffectList m_destroyEffects = new EffectList();

	[Header("Guardian power")]
	public float m_powerActivationDelay = 2f;

	public StatusEffect m_guardianPower;

	public EffectList m_activatePowerEffects = new EffectList();

	public EffectList m_activatePowerEffectsPlayer = new EffectList();

	private string m_visualName = "";

	private int m_visualVariant;

	private GameObject m_visualItem;

	private string m_currentItemName = "";

	private ItemDrop.ItemData m_queuedItem;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = (m_netViewOverride ? m_netViewOverride : base.gameObject.GetComponent<ZNetView>());
		if (m_nview.GetZDO() != null)
		{
			WearNTear component = GetComponent<WearNTear>();
			if ((bool)component)
			{
				component.m_onDestroyed = (Action)Delegate.Combine(component.m_onDestroyed, new Action(OnDestroyed));
			}
			m_nview.Register("DropItem", RPC_DropItem);
			m_nview.Register("RequestOwn", RPC_RequestOwn);
			m_nview.Register("DestroyAttachment", RPC_DestroyAttachment);
			m_nview.Register<string, int, int>("SetVisualItem", RPC_SetVisualItem);
			InvokeRepeating("UpdateVisual", 1f, 4f);
		}
	}

	private void OnDestroyed()
	{
		if (m_nview.IsOwner())
		{
			DropItem();
		}
	}

	public string GetHoverText()
	{
		if (!Player.m_localPlayer)
		{
			return "";
		}
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(m_name + "\n$piece_noaccess");
		}
		if (HaveAttachment())
		{
			if (!m_canBeRemoved)
			{
				if (m_guardianPower != null)
				{
					if (IsInvoking("DelayedPowerActivation"))
					{
						return "";
					}
					string tooltipString = m_guardianPower.GetTooltipString();
					if (IsGuardianPowerActive(Player.m_localPlayer))
					{
						return Localization.instance.Localize("<color=orange>" + m_guardianPower.m_name + "</color>\n" + tooltipString + "\n\n$guardianstone_hook_alreadyactive");
					}
					return Localization.instance.Localize("<color=orange>" + m_guardianPower.m_name + "</color>\n" + tooltipString + "\n\n[<color=yellow><b>$KEY_Use</b></color>] $guardianstone_hook_activate");
				}
				return "";
			}
			return Localization.instance.Localize(m_name + " ( " + m_currentItemName + " )\n[<color=yellow><b>$KEY_Use</b></color>] $piece_itemstand_take");
		}
		if (m_autoAttach && m_supportedItems.Count == 1)
		{
			return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_itemstand_attach");
		}
		return Localization.instance.Localize(m_name + "\n[<color=yellow><b>1-8</b></color>] $piece_itemstand_attach");
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return true;
		}
		if (!HaveAttachment())
		{
			if (m_autoAttach && m_supportedItems.Count == 1)
			{
				ItemDrop.ItemData item = user.GetInventory().GetItem(m_supportedItems[0].m_itemData.m_shared.m_name);
				if (item != null)
				{
					UseItem(user, item);
					return true;
				}
				user.Message(MessageHud.MessageType.Center, "$piece_itemstand_missingitem");
				return false;
			}
		}
		else
		{
			if (m_canBeRemoved)
			{
				m_nview.InvokeRPC("DropItem");
				return true;
			}
			if (m_guardianPower != null)
			{
				if (IsInvoking("DelayedPowerActivation"))
				{
					return false;
				}
				if (IsGuardianPowerActive(user))
				{
					return false;
				}
				user.Message(MessageHud.MessageType.Center, "$guardianstone_hook_power_activate ");
				m_activatePowerEffects.Create(base.transform.position, base.transform.rotation);
				m_activatePowerEffectsPlayer.Create(user.transform.position, Quaternion.identity, user.transform);
				Invoke("DelayedPowerActivation", m_powerActivationDelay);
				return true;
			}
		}
		return false;
	}

	private bool IsGuardianPowerActive(Humanoid user)
	{
		return (user as Player).GetGuardianPowerName() == m_guardianPower.name;
	}

	private void DelayedPowerActivation()
	{
		Player localPlayer = Player.m_localPlayer;
		if (!(localPlayer == null))
		{
			localPlayer.SetGuardianPower(m_guardianPower.name);
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (HaveAttachment())
		{
			return false;
		}
		if (!CanAttach(item))
		{
			user.Message(MessageHud.MessageType.Center, "$piece_itemstand_cantattach");
			return true;
		}
		if (!m_nview.IsOwner())
		{
			m_nview.InvokeRPC("RequestOwn");
		}
		m_queuedItem = item;
		CancelInvoke("UpdateAttach");
		InvokeRepeating("UpdateAttach", 0f, 0.1f);
		return true;
	}

	private void RPC_DropItem(long sender)
	{
		if (m_nview.IsOwner() && m_canBeRemoved)
		{
			DropItem();
		}
	}

	public void DestroyAttachment()
	{
		m_nview.InvokeRPC("DestroyAttachment");
	}

	public void RPC_DestroyAttachment(long sender)
	{
		if (m_nview.IsOwner() && HaveAttachment())
		{
			m_nview.GetZDO().Set(ZDOVars.s_item, "");
			m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", "", 0, 0);
			m_destroyEffects.Create(m_dropSpawnPoint.position, Quaternion.identity);
		}
	}

	private void DropItem()
	{
		if (!HaveAttachment())
		{
			return;
		}
		string @string = m_nview.GetZDO().GetString(ZDOVars.s_item);
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(@string);
		if ((bool)itemPrefab)
		{
			Vector3 vector = Vector3.zero;
			Quaternion quaternion = Quaternion.identity;
			Transform transform = itemPrefab.transform.Find("attach");
			if ((bool)itemPrefab.transform.Find("attachobj") && (bool)transform)
			{
				quaternion = transform.transform.localRotation;
				vector = transform.transform.localPosition;
			}
			GameObject obj = UnityEngine.Object.Instantiate(itemPrefab, m_dropSpawnPoint.position + vector, m_dropSpawnPoint.rotation * quaternion);
			obj.GetComponent<ItemDrop>().LoadFromExternalZDO(m_nview.GetZDO());
			obj.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
			m_effects.Create(m_dropSpawnPoint.position, Quaternion.identity);
		}
		m_nview.GetZDO().Set(ZDOVars.s_item, "");
		m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", "", 0, 0);
	}

	private Transform GetAttach(ItemDrop.ItemData item)
	{
		return m_attachOther;
	}

	private void UpdateAttach()
	{
		if (m_nview.IsOwner())
		{
			CancelInvoke("UpdateAttach");
			Player localPlayer = Player.m_localPlayer;
			if (m_queuedItem != null && localPlayer != null && localPlayer.GetInventory().ContainsItem(m_queuedItem) && !HaveAttachment())
			{
				ItemDrop.ItemData itemData = m_queuedItem.Clone();
				itemData.m_stack = 1;
				m_nview.GetZDO().Set(ZDOVars.s_item, m_queuedItem.m_dropPrefab.name);
				ItemDrop.SaveToZDO(itemData, m_nview.GetZDO());
				localPlayer.UnequipItem(m_queuedItem);
				localPlayer.GetInventory().RemoveOneItem(m_queuedItem);
				m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", itemData.m_dropPrefab.name, itemData.m_variant, itemData.m_quality);
				Transform attach = GetAttach(m_queuedItem);
				m_effects.Create(attach.transform.position, Quaternion.identity);
				Game.instance.IncrementPlayerStat(PlayerStatType.ItemStandUses);
			}
			m_queuedItem = null;
		}
	}

	private void RPC_RequestOwn(long sender)
	{
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().SetOwner(sender);
		}
	}

	private void UpdateVisual()
	{
		if (!(m_nview == null) && m_nview.IsValid())
		{
			string @string = m_nview.GetZDO().GetString(ZDOVars.s_item);
			int @int = m_nview.GetZDO().GetInt(ZDOVars.s_variant);
			int int2 = m_nview.GetZDO().GetInt(ZDOVars.s_quality, 1);
			SetVisualItem(@string, @int, int2);
		}
	}

	private void RPC_SetVisualItem(long sender, string itemName, int variant, int quality)
	{
		SetVisualItem(itemName, variant, quality);
	}

	private void SetVisualItem(string itemName, int variant, int quality)
	{
		if (m_visualName == itemName && m_visualVariant == variant)
		{
			return;
		}
		m_visualName = itemName;
		m_visualVariant = variant;
		m_currentItemName = "";
		if (m_visualName == "")
		{
			UnityEngine.Object.Destroy(m_visualItem);
			return;
		}
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(itemName);
		if (itemPrefab == null)
		{
			ZLog.LogWarning("Missing item prefab " + itemName);
			return;
		}
		GameObject attachPrefab = GetAttachPrefab(itemPrefab);
		if (attachPrefab == null)
		{
			ZLog.LogWarning("Failed to get attach prefab for item " + itemName);
			return;
		}
		ItemDrop component = itemPrefab.GetComponent<ItemDrop>();
		m_currentItemName = component.m_itemData.m_shared.m_name;
		Transform attach = GetAttach(component.m_itemData);
		GameObject attachGameObject = GetAttachGameObject(attachPrefab);
		m_visualItem = UnityEngine.Object.Instantiate(attachGameObject, attach.position, attach.rotation, attach);
		m_visualItem.transform.localPosition = attachPrefab.transform.localPosition;
		m_visualItem.transform.localRotation = attachPrefab.transform.localRotation;
		m_visualItem.transform.localScale = Vector3.Scale(attachPrefab.transform.localScale, component.m_itemData.GetScale(quality));
		m_visualItem.GetComponentInChildren<IEquipmentVisual>()?.Setup(m_visualVariant);
	}

	public static GameObject GetAttachPrefab(GameObject item)
	{
		Transform transform = item.transform.Find("attach");
		if ((bool)transform)
		{
			return transform.gameObject;
		}
		return null;
	}

	public static GameObject GetAttachGameObject(GameObject prefab)
	{
		Transform transform = prefab.transform.Find("attachobj");
		if (!(transform != null))
		{
			return prefab;
		}
		return transform.gameObject;
	}

	private bool CanAttach(ItemDrop.ItemData item)
	{
		if (GetAttachPrefab(item.m_dropPrefab) == null)
		{
			return false;
		}
		if (IsUnsupported(item))
		{
			return false;
		}
		if (!IsSupported(item))
		{
			return false;
		}
		if (m_supportedTypes.Count == 0)
		{
			return true;
		}
		return m_supportedTypes.Contains(item.m_shared.m_itemType);
	}

	public bool IsUnsupported(ItemDrop.ItemData item)
	{
		foreach (ItemDrop unsupportedItem in m_unsupportedItems)
		{
			if (unsupportedItem.m_itemData.m_shared.m_name == item.m_shared.m_name)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsSupported(ItemDrop.ItemData item)
	{
		if (m_supportedItems.Count == 0)
		{
			return true;
		}
		foreach (ItemDrop supportedItem in m_supportedItems)
		{
			if (supportedItem.m_itemData.m_shared.m_name == item.m_shared.m_name)
			{
				return true;
			}
		}
		return false;
	}

	public bool HaveAttachment()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetString(ZDOVars.s_item) != "";
	}

	public string GetAttachedItem()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		return m_nview.GetZDO().GetString(ZDOVars.s_item);
	}
}
