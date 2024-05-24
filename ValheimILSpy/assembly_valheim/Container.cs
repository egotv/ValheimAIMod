using System;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour, Hoverable, Interactable
{
	public enum PrivacySetting
	{
		Private,
		Group,
		Public
	}

	private float m_lastTakeAllTime;

	public Action m_onTakeAllSuccess;

	public string m_name = "Container";

	public Sprite m_bkg;

	public int m_width = 3;

	public int m_height = 2;

	public PrivacySetting m_privacy = PrivacySetting.Public;

	public bool m_checkGuardStone;

	public bool m_autoDestroyEmpty;

	public DropTable m_defaultItems = new DropTable();

	public GameObject m_open;

	public GameObject m_closed;

	public EffectList m_openEffects = new EffectList();

	public EffectList m_closeEffects = new EffectList();

	public ZNetView m_rootObjectOverride;

	public Vagon m_wagon;

	public GameObject m_destroyedLootPrefab;

	private Inventory m_inventory;

	private ZNetView m_nview;

	private Piece m_piece;

	private bool m_inUse;

	private bool m_loading;

	private uint m_lastRevision = uint.MaxValue;

	private string m_lastDataString = "";

	private void Awake()
	{
		m_nview = (m_rootObjectOverride ? m_rootObjectOverride.GetComponent<ZNetView>() : GetComponent<ZNetView>());
		if (m_nview.GetZDO() != null)
		{
			m_inventory = new Inventory(m_name, m_bkg, m_width, m_height);
			Inventory inventory = m_inventory;
			inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(OnContainerChanged));
			m_piece = GetComponent<Piece>();
			if ((bool)m_nview)
			{
				m_nview.Register<long>("RequestOpen", RPC_RequestOpen);
				m_nview.Register<bool>("OpenRespons", RPC_OpenRespons);
				m_nview.Register<long>("RPC_RequestStack", RPC_RequestStack);
				m_nview.Register<bool>("RPC_StackResponse", RPC_StackResponse);
				m_nview.Register<long>("RequestTakeAll", RPC_RequestTakeAll);
				m_nview.Register<bool>("TakeAllRespons", RPC_TakeAllRespons);
			}
			WearNTear wearNTear = (m_rootObjectOverride ? m_rootObjectOverride.GetComponent<WearNTear>() : GetComponent<WearNTear>());
			if ((bool)wearNTear)
			{
				wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(OnDestroyed));
			}
			Destructible destructible = (m_rootObjectOverride ? m_rootObjectOverride.GetComponent<Destructible>() : GetComponent<Destructible>());
			if ((bool)destructible)
			{
				destructible.m_onDestroyed = (Action)Delegate.Combine(destructible.m_onDestroyed, new Action(OnDestroyed));
			}
			if (m_nview.IsOwner() && !m_nview.GetZDO().GetBool(ZDOVars.s_addedDefaultItems))
			{
				AddDefaultItems();
				m_nview.GetZDO().Set(ZDOVars.s_addedDefaultItems, value: true);
			}
			InvokeRepeating("CheckForChanges", 0f, 1f);
		}
	}

	private void AddDefaultItems()
	{
		foreach (ItemDrop.ItemData dropListItem in m_defaultItems.GetDropListItems())
		{
			m_inventory.AddItem(dropListItem);
		}
	}

	private void DropAllItems(GameObject lootContainerPrefab)
	{
		while (m_inventory.NrOfItems() > 0)
		{
			Vector3 position = base.transform.position + UnityEngine.Random.insideUnitSphere * 1f;
			UnityEngine.Object.Instantiate(lootContainerPrefab, position, UnityEngine.Random.rotation).GetComponent<Container>().GetInventory()
				.MoveAll(m_inventory);
		}
	}

	private void DropAllItems()
	{
		List<ItemDrop.ItemData> allItems = m_inventory.GetAllItems();
		int num = 1;
		foreach (ItemDrop.ItemData item in allItems)
		{
			Vector3 position = base.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
			Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
			ItemDrop.DropItem(item, 0, position, rotation);
			num++;
		}
		m_inventory.RemoveAll();
		Save();
	}

	private void OnDestroyed()
	{
		if (m_nview.IsOwner())
		{
			if ((bool)m_destroyedLootPrefab)
			{
				DropAllItems(m_destroyedLootPrefab);
			}
			else
			{
				DropAllItems();
			}
		}
	}

	private void CheckForChanges()
	{
		if (m_nview.IsValid())
		{
			Load();
			UpdateUseVisual();
			if (m_autoDestroyEmpty && m_nview.IsOwner() && !IsInUse() && m_inventory.NrOfItems() == 0)
			{
				m_nview.Destroy();
			}
		}
	}

	private void UpdateUseVisual()
	{
		bool flag;
		if (m_nview.IsOwner())
		{
			flag = m_inUse;
			m_nview.GetZDO().Set(ZDOVars.s_inUse, m_inUse ? 1 : 0);
		}
		else
		{
			flag = m_nview.GetZDO().GetInt(ZDOVars.s_inUse) == 1;
		}
		if ((bool)m_open)
		{
			m_open.SetActive(flag);
		}
		if ((bool)m_closed)
		{
			m_closed.SetActive(!flag);
		}
	}

	public string GetHoverText()
	{
		if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return Localization.instance.Localize(m_name + "\n$piece_noaccess");
		}
		string text = ((m_inventory.NrOfItems() != 0) ? m_name : (m_name + " ( $piece_container_empty )"));
		text += "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_container_open $msg_stackall_hover";
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position))
		{
			return true;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		if (!CheckAccess(playerID))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_cantopen");
			return true;
		}
		m_nview.InvokeRPC("RequestOpen", playerID);
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public bool CanBeRemoved()
	{
		if (m_privacy == PrivacySetting.Private && GetInventory().NrOfItems() > 0)
		{
			return false;
		}
		return true;
	}

	private bool CheckAccess(long playerID)
	{
		return m_privacy switch
		{
			PrivacySetting.Public => true, 
			PrivacySetting.Private => m_piece.GetCreator() == playerID, 
			PrivacySetting.Group => false, 
			_ => false, 
		};
	}

	public bool IsOwner()
	{
		return m_nview.IsOwner();
	}

	public bool IsInUse()
	{
		return m_inUse;
	}

	public void SetInUse(bool inUse)
	{
		if (m_nview.IsOwner() && m_inUse != inUse)
		{
			m_inUse = inUse;
			UpdateUseVisual();
			if (inUse)
			{
				m_openEffects.Create(base.transform.position, base.transform.rotation);
			}
			else
			{
				m_closeEffects.Create(base.transform.position, base.transform.rotation);
			}
		}
	}

	public Inventory GetInventory()
	{
		return m_inventory;
	}

	private void RPC_RequestOpen(long uid, long playerID)
	{
		ZLog.Log("Player " + uid + " wants to open " + base.gameObject.name + "   im: " + ZDOMan.GetSessionID());
		if (!m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
		}
		else if ((IsInUse() || ((bool)m_wagon && m_wagon.InUse())) && uid != ZNet.GetUID())
		{
			ZLog.Log("  in use");
			m_nview.InvokeRPC(uid, "OpenRespons", false);
		}
		else if (!CheckAccess(playerID))
		{
			ZLog.Log("  not yours");
			m_nview.InvokeRPC(uid, "OpenRespons", false);
		}
		else
		{
			ZDOMan.instance.ForceSendZDO(uid, m_nview.GetZDO().m_uid);
			m_nview.GetZDO().SetOwner(uid);
			m_nview.InvokeRPC(uid, "OpenRespons", true);
		}
	}

	private void RPC_OpenRespons(long uid, bool granted)
	{
		if ((bool)Player.m_localPlayer)
		{
			if (granted)
			{
				InventoryGui.instance.Show(this);
			}
			else
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
			}
		}
	}

	public void StackAll()
	{
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		m_nview.InvokeRPC("RPC_RequestStack", playerID);
	}

	private void RPC_RequestStack(long uid, long playerID)
	{
		ZLog.Log("Player " + uid + " wants to stack all in " + base.gameObject.name + "   im: " + ZDOMan.GetSessionID());
		if (!m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
		}
		else if ((IsInUse() || ((bool)m_wagon && m_wagon.InUse())) && uid != ZNet.GetUID())
		{
			ZLog.Log("  in use");
			m_nview.InvokeRPC(uid, "RPC_StackResponse", false);
		}
		else if (!CheckAccess(playerID))
		{
			ZLog.Log("  not yours");
			m_nview.InvokeRPC(uid, "RPC_StackResponse", false);
		}
		else
		{
			ZDOMan.instance.ForceSendZDO(uid, m_nview.GetZDO().m_uid);
			m_nview.GetZDO().SetOwner(uid);
			m_nview.InvokeRPC(uid, "RPC_StackResponse", true);
		}
	}

	private void RPC_StackResponse(long uid, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			if (m_inventory.StackAll(Player.m_localPlayer.GetInventory(), message: true) > 0)
			{
				InventoryGui.instance.m_moveItemEffects.Create(base.transform.position, Quaternion.identity);
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
		}
	}

	public bool TakeAll(Humanoid character)
	{
		if (m_checkGuardStone && !PrivateArea.CheckAccess(base.transform.position))
		{
			return false;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		if (!CheckAccess(playerID))
		{
			character.Message(MessageHud.MessageType.Center, "$msg_cantopen");
			return false;
		}
		m_nview.InvokeRPC("RequestTakeAll", playerID);
		return true;
	}

	private void RPC_RequestTakeAll(long uid, long playerID)
	{
		ZLog.Log("Player " + uid + " wants to takeall from " + base.gameObject.name + "   im: " + ZDOMan.GetSessionID());
		if (!m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
		}
		else if ((IsInUse() || ((bool)m_wagon && m_wagon.InUse())) && uid != ZNet.GetUID())
		{
			ZLog.Log("  in use");
			m_nview.InvokeRPC(uid, "TakeAllRespons", false);
		}
		else if (!CheckAccess(playerID))
		{
			ZLog.Log("  not yours");
			m_nview.InvokeRPC(uid, "TakeAllRespons", false);
		}
		else if (!(Time.time - m_lastTakeAllTime < 2f))
		{
			m_lastTakeAllTime = Time.time;
			m_nview.InvokeRPC(uid, "TakeAllRespons", true);
		}
	}

	private void RPC_TakeAllRespons(long uid, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			m_nview.ClaimOwnership();
			ZDOMan.instance.ForceSendZDO(uid, m_nview.GetZDO().m_uid);
			Player.m_localPlayer.GetInventory().MoveAll(m_inventory);
			if (m_onTakeAllSuccess != null)
			{
				m_onTakeAllSuccess();
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
		}
	}

	private void OnContainerChanged()
	{
		if (!m_loading && IsOwner())
		{
			Save();
		}
	}

	private void Save()
	{
		ZPackage zPackage = new ZPackage();
		m_inventory.Save(zPackage);
		string @base = zPackage.GetBase64();
		m_nview.GetZDO().Set(ZDOVars.s_items, @base);
		m_lastRevision = m_nview.GetZDO().DataRevision;
		m_lastDataString = @base;
	}

	private void Load()
	{
		if (m_nview.GetZDO().DataRevision != m_lastRevision)
		{
			string @string = m_nview.GetZDO().GetString(ZDOVars.s_items);
			if (!string.IsNullOrEmpty(@string) && !(@string == m_lastDataString))
			{
				ZPackage pkg = new ZPackage(@string);
				m_loading = true;
				m_inventory.Load(pkg);
				m_loading = false;
				m_lastRevision = m_nview.GetZDO().DataRevision;
				m_lastDataString = @string;
			}
		}
	}
}
