using System;
using System.Collections.Generic;
using UnityEngine;

public class Trader : MonoBehaviour, Hoverable, Interactable
{
	[Serializable]
	public class TradeItem
	{
		public ItemDrop m_prefab;

		public int m_stack = 1;

		public int m_price = 100;

		public string m_requiredGlobalKey;
	}

	[Serializable]
	public class TraderUseItem
	{
		public ItemDrop m_prefab;

		public string m_setsGlobalKey;

		public bool m_removesItem;

		public string m_dialog;
	}

	[Serializable]
	public class ConditionalDialog
	{
		public List<string> m_keyConditions = new List<string>();

		[Tooltip("Default unchecked will run when they keys are set in the world, check this to run when keys are NOT set.")]
		public bool m_whenKeyNotSet;

		[Tooltip("Which places this text will be used.")]
		public TalkPlacement m_textPlacement;

		public KeySetType m_keyCheck;

		public GameKeyType m_keyType;

		public List<string> m_dialog;
	}

	public enum TalkPlacement
	{
		ReplaceRandomTalk,
		ReplaceGreetAndRandomTalk,
		ReplaceGreet
	}

	public string m_name = "Haldor";

	public float m_standRange = 15f;

	public float m_greetRange = 5f;

	public float m_byeRange = 5f;

	public List<TradeItem> m_items = new List<TradeItem>();

	public List<TraderUseItem> m_useItems = new List<TraderUseItem>();

	[Header("Dialog")]
	public float m_hideDialogDelay = 5f;

	public float m_randomTalkInterval = 30f;

	public List<string> m_randomTalk = new List<string>();

	public List<string> m_randomGreets = new List<string>();

	public List<string> m_randomGoodbye = new List<string>();

	public List<string> m_randomStartTrade = new List<string>();

	public List<string> m_randomBuy = new List<string>();

	public List<string> m_randomSell = new List<string>();

	public List<string> m_randomGiveItemNo = new List<string>();

	public List<string> m_randomUseItemAlreadyRecieved = new List<string>();

	[Tooltip("These will be used instead of random talk if any of the conditions are met")]
	public List<ConditionalDialog> m_randomTalkConditionals = new List<ConditionalDialog>();

	public EffectList m_randomTalkFX = new EffectList();

	public EffectList m_randomGreetFX = new EffectList();

	public EffectList m_randomGoodbyeFX = new EffectList();

	public EffectList m_randomStartTradeFX = new EffectList();

	public EffectList m_randomBuyFX = new EffectList();

	public EffectList m_randomSellFX = new EffectList();

	private bool m_didGreet;

	private bool m_didGoodbye;

	private Animator m_animator;

	private LookAt m_lookAt;

	private void Start()
	{
		m_animator = GetComponentInChildren<Animator>();
		m_lookAt = GetComponentInChildren<LookAt>();
		SnapToGround component = GetComponent<SnapToGround>();
		if ((bool)component)
		{
			component.Snap();
		}
		InvokeRepeating("RandomTalk", m_randomTalkInterval, m_randomTalkInterval);
	}

	private void Update()
	{
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, Mathf.Max(m_byeRange + 3f, m_standRange));
		if ((bool)closestPlayer)
		{
			float num = Vector3.Distance(closestPlayer.transform.position, base.transform.position);
			if (num < m_standRange)
			{
				m_animator.SetBool("Stand", value: true);
				m_lookAt.SetLoockAtTarget(closestPlayer.GetHeadPoint());
			}
			if (!m_didGreet && num < m_greetRange)
			{
				m_didGreet = true;
				List<string> texts = CheckConditionals(m_randomGreets, isGreet: true);
				Say(texts, "Greet");
				m_randomGreetFX.Create(base.transform.position, Quaternion.identity);
			}
			if (m_didGreet && !m_didGoodbye && num > m_byeRange)
			{
				m_didGoodbye = true;
				Say(m_randomGoodbye, "Greet");
				m_randomGoodbyeFX.Create(base.transform.position, Quaternion.identity);
			}
		}
		else
		{
			m_animator.SetBool("Stand", value: false);
			m_lookAt.ResetTarget();
		}
	}

	private void RandomTalk()
	{
		if (m_animator.GetBool("Stand") && !StoreGui.IsVisible() && Player.IsPlayerInRange(base.transform.position, m_greetRange))
		{
			List<string> texts = CheckConditionals(m_randomTalk, isGreet: false);
			Say(texts, "Talk");
			m_randomTalkFX.Create(base.transform.position, Quaternion.identity);
		}
	}

	private List<string> CheckConditionals(List<string> defaultList, bool isGreet)
	{
		foreach (ConditionalDialog randomTalkConditional in m_randomTalkConditionals)
		{
			if ((isGreet && randomTalkConditional.m_textPlacement == TalkPlacement.ReplaceRandomTalk) || (!isGreet && randomTalkConditional.m_textPlacement == TalkPlacement.ReplaceGreet))
			{
				continue;
			}
			if (randomTalkConditional.m_keyCheck == KeySetType.All)
			{
				bool flag = true;
				foreach (string keyCondition in randomTalkConditional.m_keyConditions)
				{
					if (!ZoneSystem.instance.CheckKey(keyCondition, randomTalkConditional.m_keyType, !randomTalkConditional.m_whenKeyNotSet))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					return randomTalkConditional.m_dialog;
				}
				continue;
			}
			if (randomTalkConditional.m_keyCheck == KeySetType.Exlusive)
			{
				bool flag2 = false;
				bool flag3 = false;
				foreach (string keyCondition2 in randomTalkConditional.m_keyConditions)
				{
					if (ZoneSystem.instance.CheckKey(keyCondition2, randomTalkConditional.m_keyType, !randomTalkConditional.m_whenKeyNotSet))
					{
						flag2 = true;
					}
					else
					{
						flag3 = true;
					}
				}
				if (flag2 && flag3)
				{
					return randomTalkConditional.m_dialog;
				}
				continue;
			}
			bool flag4 = false;
			foreach (string keyCondition3 in randomTalkConditional.m_keyConditions)
			{
				if (ZoneSystem.instance.CheckKey(keyCondition3, randomTalkConditional.m_keyType, !randomTalkConditional.m_whenKeyNotSet))
				{
					flag4 = true;
					break;
				}
			}
			if ((flag4 && randomTalkConditional.m_keyCheck == KeySetType.Any) || (!flag4 && randomTalkConditional.m_keyCheck == KeySetType.None))
			{
				return randomTalkConditional.m_dialog;
			}
		}
		return defaultList;
	}

	public string GetHoverText()
	{
		string text = m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact";
		if (m_useItems.Count > 0)
		{
			text += "\n[<color=yellow><b>1-8</b></color>] $npc_giveitem";
		}
		return Localization.instance.Localize(text);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(m_name);
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		StoreGui.instance.Show(this);
		Say(m_randomStartTrade, "Talk");
		m_randomStartTradeFX.Create(base.transform.position, Quaternion.identity);
		return false;
	}

	private void DiscoverItems(Player player)
	{
		foreach (TradeItem availableItem in GetAvailableItems())
		{
			player.AddKnownItem(availableItem.m_prefab.m_itemData);
		}
	}

	private void Say(List<string> texts, string trigger)
	{
		Say(texts[UnityEngine.Random.Range(0, texts.Count)], trigger);
	}

	private void Say(string text, string trigger)
	{
		Chat.instance.SetNpcText(base.gameObject, Vector3.up * 1.5f, 20f, m_hideDialogDelay, "", text, large: false);
		if (trigger.Length > 0)
		{
			m_animator.SetTrigger(trigger);
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (m_useItems.Count > 0)
		{
			foreach (TraderUseItem useItem in m_useItems)
			{
				if (item.m_shared.m_name == useItem.m_prefab.m_itemData.m_shared.m_name)
				{
					if (!string.IsNullOrEmpty(useItem.m_setsGlobalKey) && ZoneSystem.instance.GetGlobalKey(useItem.m_setsGlobalKey))
					{
						Say(m_randomUseItemAlreadyRecieved, "Talk");
						return true;
					}
					if (!string.IsNullOrEmpty(useItem.m_dialog))
					{
						Say(useItem.m_dialog, "Talk");
					}
					if (!string.IsNullOrEmpty(useItem.m_setsGlobalKey))
					{
						ZoneSystem.instance.SetGlobalKey(useItem.m_setsGlobalKey);
					}
					if (useItem.m_removesItem)
					{
						user.GetInventory().RemoveItem(item, 1);
						user.ShowRemovedMessage(item, 1);
					}
					return true;
				}
			}
			Say(m_randomGiveItemNo, "Talk");
			return true;
		}
		return false;
	}

	public void OnBought(TradeItem item)
	{
		Say(m_randomBuy, "Buy");
		m_randomBuyFX.Create(base.transform.position, Quaternion.identity);
	}

	public void OnSold()
	{
		Say(m_randomSell, "Sell");
		m_randomSellFX.Create(base.transform.position, Quaternion.identity);
	}

	public List<TradeItem> GetAvailableItems()
	{
		List<TradeItem> list = new List<TradeItem>();
		foreach (TradeItem item in m_items)
		{
			if (string.IsNullOrEmpty(item.m_requiredGlobalKey) || ZoneSystem.instance.GetGlobalKey(item.m_requiredGlobalKey))
			{
				list.Add(item);
			}
		}
		return list;
	}
}
