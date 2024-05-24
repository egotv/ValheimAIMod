using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CookingStation : MonoBehaviour, Interactable, Hoverable
{
	[Serializable]
	public class ItemConversion
	{
		public ItemDrop m_from;

		public ItemDrop m_to;

		public float m_cookTime = 10f;
	}

	[Serializable]
	public class ItemMessage
	{
		public ItemDrop m_item;

		public string m_message;
	}

	private enum Status
	{
		NotDone,
		Done,
		Burnt
	}

	public Switch m_addFoodSwitch;

	public Switch m_addFuelSwitch;

	public EffectList m_addEffect = new EffectList();

	public EffectList m_doneEffect = new EffectList();

	public EffectList m_overcookedEffect = new EffectList();

	public EffectList m_pickEffector = new EffectList();

	public string m_addItemTooltip = "$piece_cstand_cook";

	public Transform m_spawnPoint;

	public float m_spawnForce = 5f;

	public ItemDrop m_overCookedItem;

	public List<ItemConversion> m_conversion = new List<ItemConversion>();

	public List<ItemMessage> m_incompatibleItems = new List<ItemMessage>();

	public Transform[] m_slots;

	public ParticleSystem[] m_donePS;

	public ParticleSystem[] m_burntPS;

	public string m_name = "";

	public bool m_requireFire = true;

	public Transform[] m_fireCheckPoints;

	public float m_fireCheckRadius = 0.25f;

	public bool m_useFuel;

	public ItemDrop m_fuelItem;

	public int m_maxFuel = 10;

	public int m_secPerFuel = 5000;

	public EffectList m_fuelAddedEffects = new EffectList();

	public GameObject m_haveFuelObject;

	public GameObject m_haveFireObject;

	private ZNetView m_nview;

	private ParticleSystem[] m_ps;

	private AudioSource[] m_as;

	private void Awake()
	{
		m_nview = base.gameObject.GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			m_ps = new ParticleSystem[m_slots.Length];
			m_as = new AudioSource[m_slots.Length];
			for (int i = 0; i < m_slots.Length; i++)
			{
				m_ps[i] = m_slots[i].GetComponent<ParticleSystem>();
				m_as[i] = m_slots[i].GetComponent<AudioSource>();
			}
			m_nview.Register<Vector3>("RPC_RemoveDoneItem", RPC_RemoveDoneItem);
			m_nview.Register<string>("RPC_AddItem", RPC_AddItem);
			m_nview.Register("RPC_AddFuel", RPC_AddFuel);
			m_nview.Register<int, string>("RPC_SetSlotVisual", RPC_SetSlotVisual);
			if ((bool)m_addFoodSwitch)
			{
				m_addFoodSwitch.m_onUse = OnAddFoodSwitch;
				m_addFoodSwitch.m_hoverText = HoverText();
			}
			if ((bool)m_addFuelSwitch)
			{
				m_addFuelSwitch.m_onUse = OnAddFuelSwitch;
				m_addFuelSwitch.m_onHover = OnHoverFuelSwitch;
			}
			WearNTear component = GetComponent<WearNTear>();
			if ((bool)component)
			{
				component.m_onDestroyed = (Action)Delegate.Combine(component.m_onDestroyed, new Action(OnDestroyed));
			}
			InvokeRepeating("UpdateCooking", 0f, 1f);
		}
	}

	private void DropAllItems()
	{
		if (m_fuelItem != null)
		{
			float fuel = GetFuel();
			for (int i = 0; i < (int)fuel; i++)
			{
				drop(m_fuelItem);
			}
			SetFuel(0f);
		}
		for (int j = 0; j < m_slots.Length; j++)
		{
			GetSlot(j, out var itemName, out var _, out var status);
			if (!(itemName != ""))
			{
				continue;
			}
			switch (status)
			{
			case Status.Done:
				drop(GetItemConversion(itemName).m_to);
				break;
			case Status.Burnt:
				drop(m_overCookedItem);
				break;
			case Status.NotDone:
			{
				GameObject prefab = ZNetScene.instance.GetPrefab(itemName);
				if (prefab != null)
				{
					ItemDrop component = prefab.GetComponent<ItemDrop>();
					if ((bool)component)
					{
						drop(component);
					}
				}
				break;
			}
			}
			SetSlot(j, "", 0f, Status.NotDone);
		}
		void drop(ItemDrop item)
		{
			Vector3 position = base.transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.3f;
			Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
			ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate(item.gameObject, position, rotation));
		}
	}

	private void OnDestroyed()
	{
		if (m_nview.IsOwner())
		{
			DropAllItems();
		}
	}

	private void UpdateCooking()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		bool flag = (m_requireFire && IsFireLit()) || (m_useFuel && GetFuel() > 0f);
		if (m_nview.IsOwner())
		{
			float deltaTime = GetDeltaTime();
			if (flag)
			{
				UpdateFuel(deltaTime);
				for (int i = 0; i < m_slots.Length; i++)
				{
					GetSlot(i, out var itemName, out var cookedTime, out var status);
					if (!(itemName != "") || status == Status.Burnt)
					{
						continue;
					}
					ItemConversion itemConversion = GetItemConversion(itemName);
					if (itemConversion == null)
					{
						SetSlot(i, "", 0f, Status.NotDone);
						continue;
					}
					cookedTime += deltaTime;
					if (cookedTime > itemConversion.m_cookTime * 2f)
					{
						m_overcookedEffect.Create(m_slots[i].position, Quaternion.identity);
						SetSlot(i, m_overCookedItem.name, cookedTime, Status.Burnt);
					}
					else if (cookedTime > itemConversion.m_cookTime && itemName == itemConversion.m_from.name)
					{
						m_doneEffect.Create(m_slots[i].position, Quaternion.identity);
						SetSlot(i, itemConversion.m_to.name, cookedTime, Status.Done);
					}
					else
					{
						SetSlot(i, itemName, cookedTime, status);
					}
				}
			}
		}
		UpdateVisual(flag);
	}

	private float GetDeltaTime()
	{
		DateTime time = ZNet.instance.GetTime();
		DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong(ZDOVars.s_startTime, time.Ticks));
		double totalSeconds = (time - dateTime).TotalSeconds;
		m_nview.GetZDO().Set(ZDOVars.s_startTime, time.Ticks);
		return (float)totalSeconds;
	}

	private void UpdateFuel(float dt)
	{
		if (m_useFuel)
		{
			float num = dt / (float)m_secPerFuel;
			float fuel = GetFuel();
			fuel -= num;
			if (fuel < 0f)
			{
				fuel = 0f;
			}
			SetFuel(fuel);
		}
	}

	private void UpdateVisual(bool fireLit)
	{
		for (int i = 0; i < m_slots.Length; i++)
		{
			GetSlot(i, out var itemName, out var _, out var status);
			SetSlotVisual(i, itemName, fireLit, status);
		}
		if (m_useFuel)
		{
			bool active = GetFuel() > 0f;
			if ((bool)m_haveFireObject)
			{
				m_haveFireObject.SetActive(fireLit);
			}
			if ((bool)m_haveFuelObject)
			{
				m_haveFuelObject.SetActive(active);
			}
		}
	}

	private void RPC_SetSlotVisual(long sender, int slot, string item)
	{
		SetSlotVisual(slot, item, fireLit: false, Status.NotDone);
	}

	private void SetSlotVisual(int i, string item, bool fireLit, Status status)
	{
		if (item == "")
		{
			ParticleSystem.EmissionModule emission = m_ps[i].emission;
			emission.enabled = false;
			if (m_burntPS.Length != 0)
			{
				ParticleSystem.EmissionModule emission2 = m_burntPS[i].emission;
				emission2.enabled = false;
			}
			if (m_donePS.Length != 0)
			{
				ParticleSystem.EmissionModule emission3 = m_donePS[i].emission;
				emission3.enabled = false;
			}
			m_as[i].mute = true;
			if (m_slots[i].childCount > 0)
			{
				UnityEngine.Object.Destroy(m_slots[i].GetChild(0).gameObject);
			}
			return;
		}
		ParticleSystem.EmissionModule emission4 = m_ps[i].emission;
		emission4.enabled = fireLit && status != Status.Burnt;
		if (m_burntPS.Length != 0)
		{
			ParticleSystem.EmissionModule emission5 = m_burntPS[i].emission;
			emission5.enabled = fireLit && status == Status.Burnt;
		}
		if (m_donePS.Length != 0)
		{
			ParticleSystem.EmissionModule emission6 = m_donePS[i].emission;
			emission6.enabled = fireLit && status == Status.Done;
		}
		m_as[i].mute = !fireLit;
		if (m_slots[i].childCount == 0 || m_slots[i].GetChild(0).name != item)
		{
			if (m_slots[i].childCount > 0)
			{
				UnityEngine.Object.Destroy(m_slots[i].GetChild(0).gameObject);
			}
			Transform obj = ObjectDB.instance.GetItemPrefab(item).transform.Find("attach");
			Transform transform = m_slots[i];
			GameObject obj2 = UnityEngine.Object.Instantiate(obj.gameObject, transform.position, transform.rotation, transform);
			obj2.name = item;
			Renderer[] componentsInChildren = obj2.GetComponentsInChildren<Renderer>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				componentsInChildren[j].shadowCastingMode = ShadowCastingMode.Off;
			}
		}
	}

	private void RPC_RemoveDoneItem(long sender, Vector3 userPoint)
	{
		for (int i = 0; i < m_slots.Length; i++)
		{
			GetSlot(i, out var itemName, out var _, out var _);
			if (itemName != "" && IsItemDone(itemName))
			{
				SpawnItem(itemName, i, userPoint);
				SetSlot(i, "", 0f, Status.NotDone);
				m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", i, "");
				break;
			}
		}
	}

	private bool HaveDoneItem()
	{
		for (int i = 0; i < m_slots.Length; i++)
		{
			GetSlot(i, out var itemName, out var _, out var _);
			if (itemName != "" && IsItemDone(itemName))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsItemDone(string itemName)
	{
		if (itemName == m_overCookedItem.name)
		{
			return true;
		}
		ItemConversion itemConversion = GetItemConversion(itemName);
		if (itemConversion == null)
		{
			return false;
		}
		if (itemName == itemConversion.m_to.name)
		{
			return true;
		}
		return false;
	}

	private void SpawnItem(string name, int slot, Vector3 userPoint)
	{
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(name);
		Vector3 vector;
		Vector3 vector2;
		if (m_spawnPoint != null)
		{
			vector = m_spawnPoint.position;
			vector2 = m_spawnPoint.forward;
		}
		else
		{
			Vector3 position = m_slots[slot].position;
			Vector3 vector3 = userPoint - position;
			vector3.y = 0f;
			vector3.Normalize();
			vector = position + vector3 * 0.5f;
			vector2 = vector3;
		}
		Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
		GameObject obj = UnityEngine.Object.Instantiate(itemPrefab, vector, rotation);
		ItemDrop.OnCreateNew(obj);
		obj.GetComponent<Rigidbody>().velocity = vector2 * m_spawnForce;
		m_pickEffector.Create(vector, Quaternion.identity);
	}

	public string GetHoverText()
	{
		if (m_addFoodSwitch != null)
		{
			return "";
		}
		return Localization.instance.Localize(HoverText());
	}

	private string HoverText()
	{
		return m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] " + m_addItemTooltip + (ZInput.GamepadActive ? "" : ("\n[<color=yellow><b>1-8</b></color>] " + m_addItemTooltip));
	}

	public string GetHoverName()
	{
		return m_name;
	}

	private bool OnAddFuelSwitch(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (item != null && item.m_shared.m_name != m_fuelItem.m_itemData.m_shared.m_name)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_wrongitem");
			return false;
		}
		if (GetFuel() > (float)(m_maxFuel - 1))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_itsfull");
			return false;
		}
		if (!user.GetInventory().HaveItem(m_fuelItem.m_itemData.m_shared.m_name))
		{
			user.Message(MessageHud.MessageType.Center, "$msg_donthaveany " + m_fuelItem.m_itemData.m_shared.m_name);
			return false;
		}
		user.Message(MessageHud.MessageType.Center, "$msg_added " + m_fuelItem.m_itemData.m_shared.m_name);
		user.GetInventory().RemoveItem(m_fuelItem.m_itemData.m_shared.m_name, 1);
		m_nview.InvokeRPC("RPC_AddFuel");
		return true;
	}

	private void RPC_AddFuel(long sender)
	{
		if (m_nview.IsOwner())
		{
			ZLog.Log("Add fuel");
			float fuel = GetFuel();
			SetFuel(fuel + 1f);
			m_fuelAddedEffects.Create(base.transform.position, base.transform.rotation, base.transform);
		}
	}

	private string OnHoverFuelSwitch()
	{
		float fuel = GetFuel();
		Localization instance = Localization.instance;
		string[] obj = new string[9]
		{
			m_name,
			" (",
			m_fuelItem.m_itemData.m_shared.m_name,
			" ",
			Mathf.Ceil(fuel).ToString(),
			"/",
			null,
			null,
			null
		};
		int maxFuel = m_maxFuel;
		obj[6] = maxFuel.ToString();
		obj[7] = ")\n[<color=yellow><b>$KEY_Use</b></color>] $piece_smelter_add ";
		obj[8] = m_fuelItem.m_itemData.m_shared.m_name;
		return instance.Localize(string.Concat(obj));
	}

	private bool OnAddFoodSwitch(Switch caller, Humanoid user, ItemDrop.ItemData item)
	{
		ZLog.Log("add food switch");
		if (item != null)
		{
			return OnUseItem(user, item);
		}
		return OnInteract(user);
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (m_addFoodSwitch != null)
		{
			return false;
		}
		return OnInteract(user);
	}

	private bool OnInteract(Humanoid user)
	{
		if (HaveDoneItem())
		{
			m_nview.InvokeRPC("RPC_RemoveDoneItem", user.transform.position);
			return true;
		}
		ItemDrop.ItemData itemData = FindCookableItem(user.GetInventory());
		if (itemData == null)
		{
			ItemMessage itemMessage = FindIncompatibleItem(user.GetInventory());
			if (itemMessage != null)
			{
				user.Message(MessageHud.MessageType.Center, itemMessage.m_message + " " + itemMessage.m_item.m_itemData.m_shared.m_name);
			}
			else
			{
				user.Message(MessageHud.MessageType.Center, "$msg_nocookitems");
			}
			return false;
		}
		return OnUseItem(user, itemData);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (m_addFoodSwitch != null)
		{
			return false;
		}
		return OnUseItem(user, item);
	}

	private bool OnUseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (m_requireFire && !IsFireLit())
		{
			user.Message(MessageHud.MessageType.Center, "$msg_needfire");
			return false;
		}
		if (GetFreeSlot() == -1)
		{
			user.Message(MessageHud.MessageType.Center, "$msg_nocookroom");
			return false;
		}
		return CookItem(user, item);
	}

	private bool IsFireLit()
	{
		if (m_fireCheckPoints != null && m_fireCheckPoints.Length != 0)
		{
			Transform[] fireCheckPoints = m_fireCheckPoints;
			for (int i = 0; i < fireCheckPoints.Length; i++)
			{
				if (!EffectArea.IsPointInsideArea(fireCheckPoints[i].position, EffectArea.Type.Burning, m_fireCheckRadius))
				{
					return false;
				}
			}
			return true;
		}
		return EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Burning, m_fireCheckRadius);
	}

	private ItemDrop.ItemData FindCookableItem(Inventory inventory)
	{
		foreach (ItemConversion item2 in m_conversion)
		{
			ItemDrop.ItemData item = inventory.GetItem(item2.m_from.m_itemData.m_shared.m_name);
			if (item != null)
			{
				return item;
			}
		}
		return null;
	}

	private ItemMessage FindIncompatibleItem(Inventory inventory)
	{
		foreach (ItemMessage incompatibleItem in m_incompatibleItems)
		{
			if (inventory.GetItem(incompatibleItem.m_item.m_itemData.m_shared.m_name) != null)
			{
				return incompatibleItem;
			}
		}
		return null;
	}

	private bool CookItem(Humanoid user, ItemDrop.ItemData item)
	{
		string text = item.m_dropPrefab.name;
		if (!m_nview.HasOwner())
		{
			m_nview.ClaimOwnership();
		}
		foreach (ItemMessage incompatibleItem in m_incompatibleItems)
		{
			if (incompatibleItem.m_item.m_itemData.m_shared.m_name == item.m_shared.m_name)
			{
				user.Message(MessageHud.MessageType.Center, incompatibleItem.m_message + " " + incompatibleItem.m_item.m_itemData.m_shared.m_name);
				return true;
			}
		}
		if (!IsItemAllowed(item))
		{
			return false;
		}
		if (GetFreeSlot() == -1)
		{
			return false;
		}
		user.GetInventory().RemoveOneItem(item);
		m_nview.InvokeRPC("RPC_AddItem", text);
		return true;
	}

	private void RPC_AddItem(long sender, string itemName)
	{
		if (IsItemAllowed(itemName))
		{
			int freeSlot = GetFreeSlot();
			if (freeSlot != -1)
			{
				SetSlot(freeSlot, itemName, 0f, Status.NotDone);
				m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetSlotVisual", freeSlot, itemName);
				m_addEffect.Create(m_slots[freeSlot].position, Quaternion.identity);
			}
		}
	}

	private void SetSlot(int slot, string itemName, float cookedTime, Status status)
	{
		if (m_nview.IsValid())
		{
			m_nview.GetZDO().Set("slot" + slot, itemName);
			m_nview.GetZDO().Set("slot" + slot, cookedTime);
			m_nview.GetZDO().Set("slotstatus" + slot, (int)status);
		}
	}

	private void GetSlot(int slot, out string itemName, out float cookedTime, out Status status)
	{
		if (!m_nview.IsValid())
		{
			itemName = "";
			status = Status.NotDone;
			cookedTime = 0f;
		}
		else
		{
			itemName = m_nview.GetZDO().GetString("slot" + slot);
			cookedTime = m_nview.GetZDO().GetFloat("slot" + slot);
			status = (Status)m_nview.GetZDO().GetInt("slotstatus" + slot);
		}
	}

	private bool IsEmpty()
	{
		for (int i = 0; i < m_slots.Length; i++)
		{
			if (m_nview.GetZDO().GetString("slot" + i) != "")
			{
				return false;
			}
		}
		return true;
	}

	private int GetFreeSlot()
	{
		for (int i = 0; i < m_slots.Length; i++)
		{
			if (m_nview.GetZDO().GetString("slot" + i) == "")
			{
				return i;
			}
		}
		return -1;
	}

	private bool IsItemAllowed(ItemDrop.ItemData item)
	{
		return IsItemAllowed(item.m_dropPrefab.name);
	}

	private bool IsItemAllowed(string itemName)
	{
		foreach (ItemConversion item in m_conversion)
		{
			if (item.m_from.gameObject.name == itemName)
			{
				return true;
			}
		}
		return false;
	}

	private ItemConversion GetItemConversion(string itemName)
	{
		foreach (ItemConversion item in m_conversion)
		{
			if (item.m_from.gameObject.name == itemName || item.m_to.gameObject.name == itemName)
			{
				return item;
			}
		}
		return null;
	}

	private void SetFuel(float fuel)
	{
		m_nview.GetZDO().Set(ZDOVars.s_fuel, fuel);
	}

	private float GetFuel()
	{
		return m_nview.GetZDO().GetFloat(ZDOVars.s_fuel);
	}

	private void OnDrawGizmosSelected()
	{
		if (!m_requireFire)
		{
			return;
		}
		if (m_fireCheckPoints != null && m_fireCheckPoints.Length != 0)
		{
			Transform[] fireCheckPoints = m_fireCheckPoints;
			foreach (Transform obj in fireCheckPoints)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(obj.position, m_fireCheckRadius);
			}
		}
		else
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(base.transform.position, m_fireCheckRadius);
		}
	}
}
