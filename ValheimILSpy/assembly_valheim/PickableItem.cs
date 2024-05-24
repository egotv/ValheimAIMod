using System;
using UnityEngine;

public class PickableItem : MonoBehaviour, Hoverable, Interactable
{
	[Serializable]
	public struct RandomItem
	{
		public ItemDrop m_itemPrefab;

		public int m_stackMin;

		public int m_stackMax;
	}

	public ItemDrop m_itemPrefab;

	public int m_stack;

	public RandomItem[] m_randomItemPrefabs = Array.Empty<RandomItem>();

	public EffectList m_pickEffector = new EffectList();

	private ZNetView m_nview;

	private GameObject m_instance;

	private bool m_picked;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			SetupRandomPrefab();
			m_nview.Register("Pick", RPC_Pick);
			SetupItem(enabled: true);
		}
	}

	private void SetupRandomPrefab()
	{
		if (!(m_itemPrefab == null) || m_randomItemPrefabs.Length == 0)
		{
			return;
		}
		int @int = m_nview.GetZDO().GetInt(ZDOVars.s_itemPrefab);
		if (@int == 0)
		{
			if (m_nview.IsOwner())
			{
				RandomItem randomItem = m_randomItemPrefabs[UnityEngine.Random.Range(0, m_randomItemPrefabs.Length)];
				m_itemPrefab = randomItem.m_itemPrefab;
				m_stack = Game.instance.ScaleDrops(randomItem.m_itemPrefab.m_itemData, randomItem.m_stackMin, randomItem.m_stackMax + 1);
				int prefabHash = ObjectDB.instance.GetPrefabHash(m_itemPrefab.gameObject);
				m_nview.GetZDO().Set(ZDOVars.s_itemPrefab, prefabHash);
				m_nview.GetZDO().Set(ZDOVars.s_itemStack, m_stack);
			}
		}
		else
		{
			GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(@int);
			if (itemPrefab == null)
			{
				ZLog.LogError("Failed to find saved prefab " + @int + " in PickableItem " + base.gameObject.name);
				return;
			}
			m_itemPrefab = itemPrefab.GetComponent<ItemDrop>();
			m_stack = m_nview.GetZDO().GetInt(ZDOVars.s_itemStack);
		}
	}

	public string GetHoverText()
	{
		if (m_picked)
		{
			return "";
		}
		return Localization.instance.Localize(GetHoverName() + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
	}

	public string GetHoverName()
	{
		if ((bool)m_itemPrefab)
		{
			int stackSize = GetStackSize();
			if (stackSize > 1)
			{
				return m_itemPrefab.m_itemData.m_shared.m_name + " x " + stackSize;
			}
			return m_itemPrefab.m_itemData.m_shared.m_name;
		}
		return "None";
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		m_nview.InvokeRPC("Pick");
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void RPC_Pick(long sender)
	{
		if (m_nview.IsOwner() && !m_picked)
		{
			m_picked = true;
			m_pickEffector.Create(base.transform.position, Quaternion.identity);
			Drop();
			m_nview.Destroy();
		}
	}

	private void Drop()
	{
		Vector3 position = base.transform.position + Vector3.up * 0.2f;
		GameObject obj = UnityEngine.Object.Instantiate(m_itemPrefab.gameObject, position, base.transform.rotation);
		ItemDrop component = obj.GetComponent<ItemDrop>();
		if ((object)component != null)
		{
			component.m_itemData.m_stack = GetStackSize();
			ItemDrop.OnCreateNew(component);
		}
		obj.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
	}

	private int GetStackSize()
	{
		return Mathf.Clamp((m_stack > 0) ? m_stack : m_itemPrefab.m_itemData.m_stack, 1, (int)Math.Round((float)m_itemPrefab.m_itemData.m_shared.m_maxStackSize * Game.m_resourceRate));
	}

	private GameObject GetAttachPrefab()
	{
		Transform transform = m_itemPrefab.transform.Find("attach");
		if ((bool)transform)
		{
			return transform.gameObject;
		}
		return null;
	}

	private void SetupItem(bool enabled)
	{
		if (!enabled)
		{
			if ((bool)m_instance)
			{
				UnityEngine.Object.Destroy(m_instance);
				m_instance = null;
			}
		}
		else if (!m_instance && !(m_itemPrefab == null))
		{
			GameObject attachPrefab = GetAttachPrefab();
			if (attachPrefab == null)
			{
				ZLog.LogWarning("Failed to get attach prefab for item " + m_itemPrefab.name);
				return;
			}
			m_instance = UnityEngine.Object.Instantiate(attachPrefab, base.transform.position, base.transform.rotation, base.transform);
			m_instance.transform.localPosition = attachPrefab.transform.localPosition;
			m_instance.transform.localRotation = attachPrefab.transform.localRotation;
		}
	}

	private bool DrawPrefabMesh(ItemDrop prefab)
	{
		if (prefab == null)
		{
			return false;
		}
		bool result = false;
		Gizmos.color = Color.yellow;
		MeshFilter[] componentsInChildren = prefab.gameObject.GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter meshFilter in componentsInChildren)
		{
			if ((bool)meshFilter && (bool)meshFilter.sharedMesh)
			{
				Vector3 position = prefab.transform.position;
				Quaternion quaternion = Quaternion.Inverse(prefab.transform.rotation);
				Vector3 vector = meshFilter.transform.position - position;
				Vector3 position2 = base.transform.position + base.transform.rotation * vector;
				Quaternion quaternion2 = quaternion * meshFilter.transform.rotation;
				Quaternion rotation = base.transform.rotation * quaternion2;
				Gizmos.DrawMesh(meshFilter.sharedMesh, position2, rotation, meshFilter.transform.lossyScale);
				result = true;
			}
		}
		return result;
	}
}
