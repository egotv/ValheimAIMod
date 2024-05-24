using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryGrid : MonoBehaviour
{
	private class Element
	{
		public Vector2i m_pos;

		public GameObject m_go;

		public Image m_icon;

		public TMP_Text m_amount;

		public TMP_Text m_quality;

		public Image m_equiped;

		public Image m_queued;

		public GameObject m_selected;

		public Image m_noteleport;

		public Image m_food;

		public UITooltip m_tooltip;

		public GuiBar m_durability;

		public bool m_used;
	}

	public enum Modifier
	{
		Select,
		Split,
		Move,
		Drop
	}

	public Action<InventoryGrid, ItemDrop.ItemData, Vector2i, Modifier> m_onSelected;

	public Action<InventoryGrid, ItemDrop.ItemData, Vector2i> m_onRightClick;

	public RectTransform m_tooltipAnchor;

	public Action<Vector2i> OnMoveToUpperInventoryGrid;

	public Action<Vector2i> OnMoveToLowerInventoryGrid;

	public GameObject m_elementPrefab;

	public RectTransform m_gridRoot;

	public Scrollbar m_scrollbar;

	public UIGroupHandler m_uiGroup;

	public float m_elementSpace = 10f;

	private int m_width = 4;

	private int m_height = 4;

	private Vector2i m_selected = new Vector2i(0, 0);

	private Inventory m_inventory;

	private List<Element> m_elements = new List<Element>();

	private bool jumpToNextContainer;

	private readonly Color m_foodEitrColor = new Color(0.6f, 0.6f, 1f, 1f);

	private readonly Color m_foodHealthColor = new Color(1f, 0.5f, 0.5f, 1f);

	private readonly Color m_foodStaminaColor = new Color(1f, 1f, 0.5f, 1f);

	internal int GridWidth => m_width;

	internal Vector2i SelectionGridPosition => m_selected;

	protected void Awake()
	{
	}

	public void ResetView()
	{
		RectTransform rectTransform = base.transform as RectTransform;
		if (m_gridRoot.rect.height > rectTransform.rect.height)
		{
			m_gridRoot.pivot = new Vector2(m_gridRoot.pivot.x, 1f);
		}
		else
		{
			m_gridRoot.pivot = new Vector2(m_gridRoot.pivot.x, 0.5f);
		}
		m_gridRoot.anchoredPosition = new Vector2(0f, 0f);
	}

	public void UpdateInventory(Inventory inventory, Player player, ItemDrop.ItemData dragItem)
	{
		m_inventory = inventory;
		UpdateGamepad();
		UpdateGui(player, dragItem);
	}

	private void UpdateGamepad()
	{
		if (!m_uiGroup.IsActive || Console.IsVisible())
		{
			return;
		}
		if (ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyLStickLeft"))
		{
			m_selected.x = Mathf.Max(0, m_selected.x - 1);
		}
		if (ZInput.GetButtonDown("JoyDPadRight") || ZInput.GetButtonDown("JoyLStickRight"))
		{
			m_selected.x = Mathf.Min(m_width - 1, m_selected.x + 1);
		}
		if (ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyLStickUp"))
		{
			if (m_selected.y - 1 < 0)
			{
				if (!jumpToNextContainer)
				{
					return;
				}
				OnMoveToUpperInventoryGrid?.Invoke(m_selected);
			}
			else
			{
				m_selected.y = Mathf.Max(0, m_selected.y - 1);
				jumpToNextContainer = false;
			}
		}
		if (!ZInput.GetButton("JoyDPadUp") && !ZInput.GetButton("JoyLStickUp") && m_selected.y - 1 <= 0)
		{
			jumpToNextContainer = true;
		}
		if (ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyLStickDown"))
		{
			if (m_selected.y + 1 > m_height - 1)
			{
				if (!jumpToNextContainer)
				{
					return;
				}
				OnMoveToLowerInventoryGrid?.Invoke(m_selected);
			}
			else
			{
				m_selected.y = Mathf.Min(m_width - 1, m_selected.y + 1);
				jumpToNextContainer = false;
			}
		}
		if (!ZInput.GetButton("JoyDPadDown") && !ZInput.GetButton("JoyLStickDown") && m_selected.y + 1 >= m_height - 1)
		{
			jumpToNextContainer = true;
		}
		if (ZInput.GetButtonDown("JoyButtonA"))
		{
			Modifier arg = Modifier.Select;
			if (ZInput.GetButton("JoyLTrigger"))
			{
				arg = Modifier.Split;
			}
			if (ZInput.GetButton("JoyRTrigger"))
			{
				arg = Modifier.Drop;
			}
			ItemDrop.ItemData gamepadSelectedItem = GetGamepadSelectedItem();
			m_onSelected(this, gamepadSelectedItem, m_selected, arg);
		}
		if (ZInput.GetButtonDown("JoyButtonX"))
		{
			ItemDrop.ItemData gamepadSelectedItem2 = GetGamepadSelectedItem();
			if (ZInput.GetButton("JoyLTrigger"))
			{
				m_onSelected(this, gamepadSelectedItem2, m_selected, Modifier.Move);
			}
			else
			{
				m_onRightClick(this, gamepadSelectedItem2, m_selected);
			}
		}
	}

	private void UpdateGui(Player player, ItemDrop.ItemData dragItem)
	{
		RectTransform rectTransform = base.transform as RectTransform;
		int width = m_inventory.GetWidth();
		int height = m_inventory.GetHeight();
		if (m_selected.x >= width - 1)
		{
			m_selected.x = width - 1;
		}
		if (m_selected.y >= height - 1)
		{
			m_selected.y = height - 1;
		}
		if (m_width != width || m_height != height)
		{
			m_width = width;
			m_height = height;
			foreach (Element element4 in m_elements)
			{
				UnityEngine.Object.Destroy(element4.m_go);
			}
			m_elements.Clear();
			Vector2 widgetSize = GetWidgetSize();
			Vector2 vector = new Vector2(rectTransform.rect.width / 2f, 0f) - new Vector2(widgetSize.x, 0f) * 0.5f;
			for (int i = 0; i < height; i++)
			{
				for (int j = 0; j < width; j++)
				{
					Vector2 vector2 = new Vector3((float)j * m_elementSpace, (float)i * (0f - m_elementSpace));
					GameObject gameObject = UnityEngine.Object.Instantiate(m_elementPrefab, m_gridRoot);
					(gameObject.transform as RectTransform).anchoredPosition = vector + vector2;
					UIInputHandler componentInChildren = gameObject.GetComponentInChildren<UIInputHandler>();
					componentInChildren.m_onRightDown = (Action<UIInputHandler>)Delegate.Combine(componentInChildren.m_onRightDown, new Action<UIInputHandler>(OnRightClick));
					componentInChildren.m_onLeftDown = (Action<UIInputHandler>)Delegate.Combine(componentInChildren.m_onLeftDown, new Action<UIInputHandler>(OnLeftClick));
					TMP_Text component = gameObject.transform.Find("binding").GetComponent<TMP_Text>();
					if ((bool)player && i == 0)
					{
						component.text = (j + 1).ToString();
					}
					else
					{
						component.enabled = false;
					}
					Element element = new Element();
					element.m_pos = new Vector2i(j, i);
					element.m_go = gameObject;
					element.m_icon = gameObject.transform.Find("icon").GetComponent<Image>();
					element.m_amount = gameObject.transform.Find("amount").GetComponent<TMP_Text>();
					element.m_quality = gameObject.transform.Find("quality").GetComponent<TMP_Text>();
					element.m_equiped = gameObject.transform.Find("equiped").GetComponent<Image>();
					element.m_queued = gameObject.transform.Find("queued").GetComponent<Image>();
					element.m_noteleport = gameObject.transform.Find("noteleport").GetComponent<Image>();
					element.m_food = gameObject.transform.Find("foodicon").GetComponent<Image>();
					element.m_selected = gameObject.transform.Find("selected").gameObject;
					element.m_tooltip = gameObject.GetComponent<UITooltip>();
					element.m_durability = gameObject.transform.Find("durability").GetComponent<GuiBar>();
					m_elements.Add(element);
				}
			}
		}
		foreach (Element element5 in m_elements)
		{
			element5.m_used = false;
		}
		bool flag = m_uiGroup.IsActive && ZInput.IsGamepadActive();
		List<ItemDrop.ItemData> allItems = m_inventory.GetAllItems();
		Element element2 = (flag ? GetElement(m_selected.x, m_selected.y, width) : GetHoveredElement());
		foreach (ItemDrop.ItemData item in allItems)
		{
			Element element3 = GetElement(item.m_gridPos.x, item.m_gridPos.y, width);
			element3.m_used = true;
			element3.m_icon.enabled = true;
			element3.m_icon.sprite = item.GetIcon();
			element3.m_icon.color = ((item == dragItem) ? Color.grey : Color.white);
			bool flag2 = item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability();
			element3.m_durability.gameObject.SetActive(flag2);
			if (flag2)
			{
				if (item.m_durability <= 0f)
				{
					element3.m_durability.SetValue(1f);
					element3.m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
				}
				else
				{
					element3.m_durability.SetValue(item.GetDurabilityPercentage());
					element3.m_durability.ResetColor();
				}
			}
			element3.m_equiped.enabled = (bool)player && item.m_equipped;
			element3.m_queued.enabled = (bool)player && player.IsEquipActionQueued(item);
			element3.m_noteleport.enabled = !item.m_shared.m_teleportable && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll);
			if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && (item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f || item.m_shared.m_foodEitr > 0f))
			{
				element3.m_food.enabled = true;
				if (item.m_shared.m_food < item.m_shared.m_foodEitr / 2f && item.m_shared.m_foodStamina < item.m_shared.m_foodEitr / 2f)
				{
					element3.m_food.color = m_foodEitrColor;
				}
				else if (item.m_shared.m_foodStamina < item.m_shared.m_food / 2f)
				{
					element3.m_food.color = m_foodHealthColor;
				}
				else if (item.m_shared.m_food < item.m_shared.m_foodStamina / 2f)
				{
					element3.m_food.color = m_foodStaminaColor;
				}
				else
				{
					element3.m_food.color = Color.white;
				}
			}
			else
			{
				element3.m_food.enabled = false;
			}
			if (element2 == element3)
			{
				CreateItemTooltip(item, element3.m_tooltip);
			}
			element3.m_quality.enabled = item.m_shared.m_maxQuality > 1;
			if (item.m_shared.m_maxQuality > 1)
			{
				element3.m_quality.text = item.m_quality.ToString();
			}
			element3.m_amount.enabled = item.m_shared.m_maxStackSize > 1;
			if (item.m_shared.m_maxStackSize > 1)
			{
				element3.m_amount.text = $"{item.m_stack}/{item.m_shared.m_maxStackSize}";
			}
		}
		foreach (Element element6 in m_elements)
		{
			element6.m_selected.SetActive(flag && element6.m_pos == m_selected);
			if (!element6.m_used)
			{
				element6.m_durability.gameObject.SetActive(value: false);
				element6.m_icon.enabled = false;
				element6.m_amount.enabled = false;
				element6.m_quality.enabled = false;
				element6.m_equiped.enabled = false;
				element6.m_queued.enabled = false;
				element6.m_noteleport.enabled = false;
				element6.m_food.enabled = false;
				element6.m_tooltip.m_text = "";
				element6.m_tooltip.m_topic = "";
			}
		}
		float size = (float)height * m_elementSpace;
		m_gridRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
	}

	private void CreateItemTooltip(ItemDrop.ItemData item, UITooltip tooltip)
	{
		tooltip.Set(item.m_shared.m_name, item.GetTooltip(), m_tooltipAnchor);
	}

	public Vector2 GetWidgetSize()
	{
		return new Vector2((float)m_width * m_elementSpace, (float)m_height * m_elementSpace);
	}

	private void OnRightClick(UIInputHandler element)
	{
		GameObject go = element.gameObject;
		Vector2i buttonPos = GetButtonPos(go);
		ItemDrop.ItemData itemAt = m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
		if (m_onRightClick != null)
		{
			m_onRightClick(this, itemAt, buttonPos);
		}
	}

	private void OnLeftClick(UIInputHandler clickHandler)
	{
		GameObject go = clickHandler.gameObject;
		Vector2i buttonPos = GetButtonPos(go);
		ItemDrop.ItemData itemAt = m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
		Modifier arg = Modifier.Select;
		if (ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift))
		{
			arg = Modifier.Split;
		}
		else if (ZInput.GetKey(KeyCode.LeftControl) || ZInput.GetKey(KeyCode.RightControl))
		{
			arg = Modifier.Move;
		}
		if (m_onSelected != null)
		{
			m_onSelected(this, itemAt, buttonPos, arg);
		}
	}

	private Element GetElement(int x, int y, int width)
	{
		int index = y * width + x;
		return m_elements[index];
	}

	private Element GetHoveredElement()
	{
		foreach (Element element in m_elements)
		{
			RectTransform obj = element.m_go.transform as RectTransform;
			Vector2 point = obj.InverseTransformPoint(ZInput.mousePosition);
			if (obj.rect.Contains(point))
			{
				return element;
			}
		}
		return null;
	}

	private Vector2i GetButtonPos(GameObject go)
	{
		for (int i = 0; i < m_elements.Count; i++)
		{
			if (m_elements[i].m_go == go)
			{
				int num = i / m_width;
				return new Vector2i(i - num * m_width, num);
			}
		}
		return new Vector2i(-1, -1);
	}

	public bool DropItem(Inventory fromInventory, ItemDrop.ItemData item, int amount, Vector2i pos)
	{
		ItemDrop.ItemData itemAt = m_inventory.GetItemAt(pos.x, pos.y);
		if (itemAt == item)
		{
			return true;
		}
		if (itemAt != null && (itemAt.m_shared.m_name != item.m_shared.m_name || (item.m_shared.m_maxQuality > 1 && itemAt.m_quality != item.m_quality) || itemAt.m_shared.m_maxStackSize == 1) && item.m_stack == amount)
		{
			fromInventory.RemoveItem(item);
			fromInventory.MoveItemToThis(m_inventory, itemAt, itemAt.m_stack, item.m_gridPos.x, item.m_gridPos.y);
			m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
			return true;
		}
		return m_inventory.MoveItemToThis(fromInventory, item, amount, pos.x, pos.y);
	}

	public ItemDrop.ItemData GetItem(Vector2i cursorPosition)
	{
		foreach (Element element in m_elements)
		{
			if (RectTransformUtility.RectangleContainsScreenPoint(element.m_go.transform as RectTransform, cursorPosition.ToVector2()))
			{
				Vector2i buttonPos = GetButtonPos(element.m_go);
				return m_inventory.GetItemAt(buttonPos.x, buttonPos.y);
			}
		}
		return null;
	}

	public Inventory GetInventory()
	{
		return m_inventory;
	}

	public void SetSelection(Vector2i pos)
	{
		m_selected = pos;
	}

	public ItemDrop.ItemData GetGamepadSelectedItem()
	{
		if (!m_uiGroup.IsActive)
		{
			return null;
		}
		if (m_inventory == null)
		{
			return null;
		}
		return m_inventory.GetItemAt(m_selected.x, m_selected.y);
	}

	public RectTransform GetGamepadSelectedElement()
	{
		if (!m_uiGroup.IsActive)
		{
			return null;
		}
		if (m_selected.x < 0 || m_selected.x >= m_width || m_selected.y < 0 || m_selected.y >= m_height)
		{
			return null;
		}
		return GetElement(m_selected.x, m_selected.y, m_width).m_go.transform as RectTransform;
	}
}
