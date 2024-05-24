using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreGui : MonoBehaviour
{
	private static StoreGui m_instance;

	public GameObject m_rootPanel;

	public Button m_buyButton;

	public Button m_sellButton;

	public RectTransform m_listRoot;

	public GameObject m_listElement;

	public Scrollbar m_listScroll;

	public ScrollRectEnsureVisible m_itemEnsureVisible;

	public TMP_Text m_coinText;

	public EffectList m_buyEffects = new EffectList();

	public EffectList m_sellEffects = new EffectList();

	public float m_hideDistance = 5f;

	public float m_itemSpacing = 64f;

	public ItemDrop m_coinPrefab;

	private List<GameObject> m_itemList = new List<GameObject>();

	private Trader.TradeItem m_selectedItem;

	private Trader m_trader;

	private float m_itemlistBaseSize;

	private int m_hiddenFrames;

	private List<ItemDrop.ItemData> m_tempItems = new List<ItemDrop.ItemData>();

	public RectTransform m_tooltipAnchor;

	public static StoreGui instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_rootPanel.SetActive(value: false);
		m_itemlistBaseSize = m_listRoot.rect.height;
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	private void Update()
	{
		if (!m_rootPanel.activeSelf)
		{
			m_hiddenFrames++;
			return;
		}
		m_hiddenFrames = 0;
		if (!m_trader)
		{
			Hide();
			return;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer == null || localPlayer.IsDead() || localPlayer.InCutscene())
		{
			Hide();
			return;
		}
		if (Vector3.Distance(m_trader.transform.position, Player.m_localPlayer.transform.position) > m_hideDistance)
		{
			Hide();
			return;
		}
		if (InventoryGui.IsVisible() || Minimap.IsOpen())
		{
			Hide();
			return;
		}
		if ((Chat.instance == null || !Chat.instance.HasFocus()) && !Console.IsVisible() && !Menu.IsVisible() && (bool)TextViewer.instance && !TextViewer.instance.IsVisible() && !localPlayer.InCutscene() && (ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("Use")))
		{
			ZInput.ResetButtonStatus("JoyButtonB");
			Hide();
		}
		UpdateBuyButton();
		UpdateSellButton();
		UpdateRecipeGamepadInput();
		m_coinText.text = GetPlayerCoins().ToString();
	}

	public void Show(Trader trader)
	{
		if (!(m_trader == trader) || !IsVisible())
		{
			m_trader = trader;
			m_rootPanel.SetActive(value: true);
			FillList();
		}
	}

	public void Hide()
	{
		m_trader = null;
		m_rootPanel.SetActive(value: false);
	}

	public static bool IsVisible()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_hiddenFrames <= 1;
		}
		return false;
	}

	public void OnBuyItem()
	{
		BuySelectedItem();
	}

	private void BuySelectedItem()
	{
		if (m_selectedItem != null && CanAfford(m_selectedItem))
		{
			int stack = Mathf.Min(m_selectedItem.m_stack, m_selectedItem.m_prefab.m_itemData.m_shared.m_maxStackSize);
			int quality = m_selectedItem.m_prefab.m_itemData.m_quality;
			int variant = m_selectedItem.m_prefab.m_itemData.m_variant;
			if (Player.m_localPlayer.GetInventory().AddItem(m_selectedItem.m_prefab.name, stack, quality, variant, 0L, "") != null)
			{
				Player.m_localPlayer.GetInventory().RemoveItem(m_coinPrefab.m_itemData.m_shared.m_name, m_selectedItem.m_price);
				m_trader.OnBought(m_selectedItem);
				m_buyEffects.Create(base.transform.position, Quaternion.identity);
				Player.m_localPlayer.ShowPickupMessage(m_selectedItem.m_prefab.m_itemData, m_selectedItem.m_prefab.m_itemData.m_stack);
				FillList();
				Gogan.LogEvent("Game", "BoughtItem", m_selectedItem.m_prefab.name, 0L);
			}
		}
	}

	public void OnSellItem()
	{
		SellItem();
	}

	private void SellItem()
	{
		ItemDrop.ItemData sellableItem = GetSellableItem();
		if (sellableItem != null)
		{
			int stack = sellableItem.m_shared.m_value * sellableItem.m_stack;
			Player.m_localPlayer.GetInventory().RemoveItem(sellableItem);
			Player.m_localPlayer.GetInventory().AddItem(m_coinPrefab.gameObject.name, stack, m_coinPrefab.m_itemData.m_quality, m_coinPrefab.m_itemData.m_variant, 0L, "");
			string text = "";
			text = ((sellableItem.m_stack <= 1) ? sellableItem.m_shared.m_name : (sellableItem.m_stack + "x" + sellableItem.m_shared.m_name));
			m_sellEffects.Create(base.transform.position, Quaternion.identity);
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_sold", text, stack.ToString()), 0, sellableItem.m_shared.m_icons[0]);
			m_trader.OnSold();
			FillList();
			Gogan.LogEvent("Game", "SoldItem", text, 0L);
		}
	}

	private int GetPlayerCoins()
	{
		return Player.m_localPlayer.GetInventory().CountItems(m_coinPrefab.m_itemData.m_shared.m_name);
	}

	private bool CanAfford(Trader.TradeItem item)
	{
		int playerCoins = GetPlayerCoins();
		return item.m_price <= playerCoins;
	}

	private void FillList()
	{
		int playerCoins = GetPlayerCoins();
		int num = GetSelectedItemIndex();
		List<Trader.TradeItem> availableItems = m_trader.GetAvailableItems();
		foreach (GameObject item in m_itemList)
		{
			Object.Destroy(item);
		}
		m_itemList.Clear();
		float b = (float)availableItems.Count * m_itemSpacing;
		b = Mathf.Max(m_itemlistBaseSize, b);
		m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, b);
		for (int i = 0; i < availableItems.Count; i++)
		{
			Trader.TradeItem tradeItem = availableItems[i];
			GameObject element = Object.Instantiate(m_listElement, m_listRoot);
			element.SetActive(value: true);
			RectTransform rectTransform = element.transform as RectTransform;
			float num2 = (m_listRoot.rect.width - rectTransform.rect.width) / 2f;
			rectTransform.anchoredPosition = new Vector2(num2, (float)i * (0f - m_itemSpacing) - num2);
			bool flag = tradeItem.m_price <= playerCoins;
			Image component = element.transform.Find("icon").GetComponent<Image>();
			component.sprite = tradeItem.m_prefab.m_itemData.m_shared.m_icons[0];
			component.color = (flag ? Color.white : new Color(1f, 0f, 1f, 0f));
			string text = Localization.instance.Localize(tradeItem.m_prefab.m_itemData.m_shared.m_name);
			if (tradeItem.m_stack > 1)
			{
				text = text + " x" + tradeItem.m_stack;
			}
			TMP_Text component2 = element.transform.Find("name").GetComponent<TMP_Text>();
			component2.text = text;
			component2.color = (flag ? Color.white : Color.grey);
			element.GetComponent<UITooltip>().Set(tradeItem.m_prefab.m_itemData.m_shared.m_name, tradeItem.m_prefab.m_itemData.GetTooltip(), m_tooltipAnchor);
			TMP_Text component3 = Utils.FindChild(element.transform, "price").GetComponent<TMP_Text>();
			component3.text = tradeItem.m_price.ToString();
			if (!flag)
			{
				component3.color = Color.grey;
			}
			element.GetComponent<Button>().onClick.AddListener(delegate
			{
				OnSelectedItem(element);
			});
			m_itemList.Add(element);
		}
		if (num < 0)
		{
			num = 0;
		}
		SelectItem(num, center: false);
	}

	private void OnSelectedItem(GameObject button)
	{
		int index = FindSelectedRecipe(button);
		SelectItem(index, center: false);
	}

	private int FindSelectedRecipe(GameObject button)
	{
		for (int i = 0; i < m_itemList.Count; i++)
		{
			if (m_itemList[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	private void SelectItem(int index, bool center)
	{
		ZLog.Log("Setting selected recipe " + index);
		for (int i = 0; i < m_itemList.Count; i++)
		{
			bool active = i == index;
			m_itemList[i].transform.Find("selected").gameObject.SetActive(active);
		}
		if (center && index >= 0)
		{
			m_itemEnsureVisible.CenterOnItem(m_itemList[index].transform as RectTransform);
		}
		if (index < 0)
		{
			m_selectedItem = null;
		}
		else
		{
			m_selectedItem = m_trader.GetAvailableItems()[index];
		}
	}

	private void UpdateSellButton()
	{
		m_sellButton.interactable = GetSellableItem() != null;
	}

	private ItemDrop.ItemData GetSellableItem()
	{
		m_tempItems.Clear();
		Player.m_localPlayer.GetInventory().GetValuableItems(m_tempItems);
		foreach (ItemDrop.ItemData tempItem in m_tempItems)
		{
			if (tempItem.m_shared.m_name != m_coinPrefab.m_itemData.m_shared.m_name)
			{
				return tempItem;
			}
		}
		return null;
	}

	private int GetSelectedItemIndex()
	{
		int result = 0;
		List<Trader.TradeItem> availableItems = m_trader.GetAvailableItems();
		for (int i = 0; i < availableItems.Count; i++)
		{
			if (availableItems[i] == m_selectedItem)
			{
				result = i;
			}
		}
		return result;
	}

	private void UpdateBuyButton()
	{
		UITooltip component = m_buyButton.GetComponent<UITooltip>();
		if (m_selectedItem != null)
		{
			bool flag = CanAfford(m_selectedItem);
			bool flag2 = Player.m_localPlayer.GetInventory().HaveEmptySlot();
			m_buyButton.interactable = flag && flag2;
			if (!flag)
			{
				component.m_text = Localization.instance.Localize("$msg_missingrequirement");
			}
			else if (!flag2)
			{
				component.m_text = Localization.instance.Localize("$inventory_full");
			}
			else
			{
				component.m_text = "";
			}
		}
		else
		{
			m_buyButton.interactable = false;
			component.m_text = "";
		}
	}

	private void UpdateRecipeGamepadInput()
	{
		if (m_itemList.Count > 0)
		{
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				SelectItem(Mathf.Min(m_itemList.Count - 1, GetSelectedItemIndex() + 1), center: true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				SelectItem(Mathf.Max(0, GetSelectedItemIndex() - 1), center: true);
			}
		}
	}
}
