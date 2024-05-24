using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Incinerator : MonoBehaviour
{
	[Serializable]
	public class IncineratorConversion
	{
		public List<Requirement> m_requirements;

		public ItemDrop m_result;

		public int m_resultAmount = 1;

		public int m_priority;

		[Tooltip("True: Requires only one of the list of ingredients to be able to produce the result. False: All of the ingredients are required.")]
		public bool m_requireOnlyOneIngredient;

		public int AttemptCraft(Inventory inv, List<ItemDrop> toAdd)
		{
			int num = int.MaxValue;
			int num2 = 0;
			Requirement requirement = null;
			foreach (Requirement requirement2 in m_requirements)
			{
				int num3 = inv.CountItems(requirement2.m_resItem.m_itemData.m_shared.m_name) / requirement2.m_amount;
				if (num3 == 0 && !m_requireOnlyOneIngredient)
				{
					return 0;
				}
				if (num3 > num2)
				{
					num2 = num3;
					requirement = requirement2;
				}
				if (num3 < num)
				{
					num = num3;
				}
			}
			int num4 = (m_requireOnlyOneIngredient ? num2 : num);
			if (num4 == 0)
			{
				return 0;
			}
			if (m_requireOnlyOneIngredient)
			{
				inv.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, requirement.m_amount * num4);
			}
			else
			{
				foreach (Requirement requirement3 in m_requirements)
				{
					inv.RemoveItem(requirement3.m_resItem.m_itemData.m_shared.m_name, requirement3.m_amount * num4);
				}
			}
			num4 *= m_resultAmount;
			for (int i = 0; i < num4; i++)
			{
				toAdd.Add(m_result);
			}
			return num4;
		}
	}

	[Serializable]
	public class Requirement
	{
		public ItemDrop m_resItem;

		public int m_amount = 1;
	}

	public enum Response
	{
		Fail,
		Success,
		Conversion,
		Empty
	}

	public Switch m_incinerateSwitch;

	public Container m_container;

	public Animator m_leverAnim;

	public GameObject m_lightingAOEs;

	public EffectList m_leverEffects = new EffectList();

	public float m_effectDelayMin = 5f;

	public float m_effectDelayMax = 7f;

	[Header("Conversion")]
	public List<IncineratorConversion> m_conversions;

	public ItemDrop m_defaultResult;

	public int m_defaultCost = 1;

	private ZNetView m_nview;

	private bool isInUse;

	private void Awake()
	{
		Switch incinerateSwitch = m_incinerateSwitch;
		incinerateSwitch.m_onUse = (Switch.Callback)Delegate.Combine(incinerateSwitch.m_onUse, new Switch.Callback(OnIncinerate));
		Switch incinerateSwitch2 = m_incinerateSwitch;
		incinerateSwitch2.m_onHover = (Switch.TooltipCallback)Delegate.Combine(incinerateSwitch2.m_onHover, new Switch.TooltipCallback(GetLeverHoverText));
		m_conversions.Sort((IncineratorConversion a, IncineratorConversion b) => b.m_priority.CompareTo(a.m_priority));
		m_nview = GetComponent<ZNetView>();
		if (!(m_nview == null) && m_nview.GetZDO() != null)
		{
			m_nview.Register<long>("RPC_RequestIncinerate", RPC_RequestIncinerate);
			m_nview.Register<int>("RPC_IncinerateRespons", RPC_IncinerateRespons);
			m_nview.Register("RPC_AnimateLever", RPC_AnimateLever);
			m_nview.Register("RPC_AnimateLeverReturn", RPC_AnimateLeverReturn);
		}
	}

	private void StopAOE()
	{
		isInUse = false;
	}

	public string GetLeverHoverText()
	{
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return Localization.instance.Localize("$piece_incinerator\n$piece_noaccess");
		}
		return Localization.instance.Localize("[<color=yellow><b>$KEY_Use</b></color>] $piece_pulllever");
	}

	private bool OnIncinerate(Switch sw, Humanoid user, ItemDrop.ItemData item)
	{
		if (!m_nview.IsValid() || !m_nview.HasOwner())
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return false;
		}
		long playerID = Game.instance.GetPlayerProfile().GetPlayerID();
		m_nview.InvokeRPC("RPC_RequestIncinerate", playerID);
		return true;
	}

	private void RPC_RequestIncinerate(long uid, long playerID)
	{
		ZLog.Log("Player " + uid + " wants to incinerate " + base.gameObject.name + "   im: " + ZDOMan.GetSessionID());
		if (!m_nview.IsOwner())
		{
			ZLog.Log("  but im not the owner");
		}
		else if (m_container.IsInUse() || isInUse)
		{
			m_nview.InvokeRPC(uid, "RPC_IncinerateRespons", 0);
			ZLog.Log("  but it's in use");
		}
		else if (m_container.GetInventory().NrOfItems() == 0)
		{
			m_nview.InvokeRPC(uid, "RPC_IncinerateRespons", 3);
			ZLog.Log("  but it's empty");
		}
		else
		{
			StartCoroutine(Incinerate(uid));
		}
	}

	private IEnumerator Incinerate(long uid)
	{
		isInUse = true;
		m_nview.InvokeRPC(ZNetView.Everybody, "RPC_AnimateLever");
		m_leverEffects.Create(base.transform.position, base.transform.rotation);
		yield return new WaitForSeconds(UnityEngine.Random.Range(m_effectDelayMin, m_effectDelayMax));
		m_nview.InvokeRPC(ZNetView.Everybody, "RPC_AnimateLeverReturn");
		if (!m_nview.IsValid() || !m_nview.IsOwner() || m_container.IsInUse())
		{
			isInUse = false;
			yield break;
		}
		Invoke("StopAOE", 4f);
		UnityEngine.Object.Instantiate(m_lightingAOEs, base.transform.position, base.transform.rotation);
		Inventory inventory = m_container.GetInventory();
		List<ItemDrop> list = new List<ItemDrop>();
		int num = 0;
		foreach (IncineratorConversion conversion in m_conversions)
		{
			num += conversion.AttemptCraft(inventory, list);
		}
		if (m_defaultResult != null && m_defaultCost > 0)
		{
			int num2 = inventory.NrOfItemsIncludingStacks() / m_defaultCost;
			num += num2;
			for (int i = 0; i < num2; i++)
			{
				list.Add(m_defaultResult);
			}
		}
		inventory.RemoveAll();
		foreach (ItemDrop item in list)
		{
			inventory.AddItem(item.gameObject, 1);
		}
		m_nview.InvokeRPC(uid, "RPC_IncinerateRespons", (num <= 0) ? 1 : 2);
	}

	private void RPC_IncinerateRespons(long uid, int r)
	{
		if ((bool)Player.m_localPlayer)
		{
			string text = null;
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, (Response)r switch
			{
				Response.Success => "$piece_incinerator_success", 
				Response.Conversion => "$piece_incinerator_conversion", 
				Response.Empty => "$piece_incinerator_empty", 
				_ => "$piece_incinerator_fail", 
			});
		}
	}

	private void RPC_AnimateLever(long uid)
	{
		ZLog.Log("DO THE THING WITH THE LEVER!");
		m_leverAnim.SetBool("Pulled", value: true);
	}

	private void RPC_AnimateLeverReturn(long uid)
	{
		ZLog.Log("Lever return");
		m_leverAnim.SetBool("Pulled", value: false);
	}
}
