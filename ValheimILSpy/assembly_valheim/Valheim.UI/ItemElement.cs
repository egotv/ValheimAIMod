using TMPro;
using UnityEngine;

namespace Valheim.UI;

public class ItemElement : RadialMenuElement
{
	public GameObject m_go;

	public GuiBar m_durability;

	public TMP_Text m_amount;

	public int m_stackText = -1;

	public float Queue
	{
		get
		{
			return base.BackgroundMaterial.GetFloat("_Queued");
		}
		set
		{
			base.BackgroundMaterial.SetFloat("_Queued", value);
		}
	}

	public void Init(ItemDrop.ItemData item)
	{
		base.Name = "";
		base.Interact = null;
		base.Description = "";
		m_go.SetActive(item != null);
		if (item != null)
		{
			SetInteraction(item);
			base.Name = Localization.instance.Localize(item.m_shared.m_name);
			SetDescription(item);
			m_icon.sprite = item?.GetIcon();
			base.Activated = item.m_equipped;
			SetAmount(item);
			SetDurability(item);
			Queue = ((Player.m_localPlayer != null && Player.m_localPlayer.IsEquipActionQueued(item)) ? 1f : 0f);
		}
	}

	protected virtual void SetInteraction(ItemDrop.ItemData item)
	{
		base.Interact = delegate
		{
			if ((bool)Player.m_localPlayer)
			{
				Player.m_localPlayer.UseItem(null, item, fromInventoryGui: false);
			}
			return true;
		};
	}

	private void SetDescription(ItemDrop.ItemData item)
	{
		int num = Mathf.CeilToInt(item.GetWeight());
		int num2 = Mathf.CeilToInt(Player.m_localPlayer.GetInventory().GetTotalWeight());
		int num3 = Mathf.CeilToInt(Player.m_localPlayer.GetMaxCarryWeight());
		string arg = ((item.m_shared.m_maxStackSize > 1) ? $"{item.GetNonStackedWeight()} ({num} $item_total)" : num.ToString());
		string text = ((num2 <= num3 || !(Mathf.Sin(Time.time * 10f) > 0f)) ? $"{arg} \n{num2}/{num3}" : $"{arg} \n<color=red>{num2}</color>/{num3}");
		base.Description = Localization.instance.Localize(text);
	}

	private void SetAmount(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_maxStackSize > 1)
		{
			m_amount.gameObject.SetActive(value: true);
			if (m_stackText != item.m_stack)
			{
				m_amount.text = $"{item.m_stack} / {item.m_shared.m_maxStackSize}";
				m_stackText = item.m_stack;
			}
		}
		else
		{
			m_amount.gameObject.SetActive(value: false);
		}
	}

	private void SetDurability(ItemDrop.ItemData item)
	{
		bool flag = item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability();
		m_durability.gameObject.SetActive(flag);
		if (flag)
		{
			if (item.m_durability <= 0f)
			{
				m_durability.SetValue(1f);
				m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
			}
			else
			{
				m_durability.SetValue(item.GetDurabilityPercentage());
				m_durability.ResetColor();
			}
		}
	}
}
