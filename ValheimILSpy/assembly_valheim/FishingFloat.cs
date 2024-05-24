using System.Collections.Generic;
using UnityEngine;

public class FishingFloat : MonoBehaviour, IProjectile
{
	public float m_maxDistance = 30f;

	public float m_moveForce = 10f;

	public float m_pullLineSpeed = 1f;

	public float m_pullLineSpeedMaxSkill = 2f;

	public float m_pullStaminaUse = 10f;

	public float m_pullStaminaUseMaxSkillMultiplier = 0.2f;

	public float m_hookedStaminaPerSec = 1f;

	public float m_hookedStaminaPerSecMaxSkill = 0.2f;

	private float m_fishingSkillImproveTimer;

	private float m_fishingSkillImproveHookedMultiplier = 2f;

	private bool m_baitConsumed;

	public float m_breakDistance = 4f;

	public float m_range = 10f;

	public float m_nibbleForce = 10f;

	public EffectList m_nibbleEffect = new EffectList();

	public EffectList m_lineBreakEffect = new EffectList();

	public float m_maxLineSlack = 0.3f;

	public LineConnect m_rodLine;

	public LineConnect m_hookLine;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private Floating m_floating;

	private float m_lineLength;

	private float m_msgTime;

	private Fish m_nibbler;

	private float m_nibbleTime;

	private static List<FishingFloat> m_allInstances = new List<FishingFloat>();

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_floating = GetComponent<Floating>();
		m_nview.Register<ZDOID, bool>("RPC_Nibble", RPC_Nibble);
		m_allInstances.Add(this);
	}

	private void OnDestroy()
	{
		m_allInstances.Remove(this);
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		FishingFloat fishingFloat = FindFloat(owner);
		if ((bool)fishingFloat)
		{
			ZNetScene.instance.Destroy(fishingFloat.gameObject);
		}
		long userID = owner.GetZDOID().UserID;
		m_nview.GetZDO().Set(ZDOVars.s_rodOwner, userID);
		m_nview.GetZDO().Set(ZDOVars.s_bait, ammo.m_dropPrefab.name);
		Transform rodTop = GetRodTop(owner);
		if (rodTop == null)
		{
			ZLog.LogWarning("Failed to find fishing rod top");
			return;
		}
		m_rodLine.SetPeer(owner.GetZDOID());
		m_lineLength = Vector3.Distance(rodTop.position, base.transform.position);
		owner.Message(MessageHud.MessageType.Center, m_lineLength.ToString("0m"));
	}

	private Character GetOwner()
	{
		if (!m_nview.IsValid())
		{
			return null;
		}
		long @long = m_nview.GetZDO().GetLong(ZDOVars.s_rodOwner, 0L);
		foreach (ZNet.PlayerInfo player in ZNet.instance.GetPlayerList())
		{
			ZDOID characterID = player.m_characterID;
			if (characterID.UserID == @long)
			{
				GameObject gameObject = ZNetScene.instance.FindInstance(player.m_characterID);
				if (gameObject == null)
				{
					return null;
				}
				return gameObject.GetComponent<Character>();
			}
		}
		return null;
	}

	private Transform GetRodTop(Character owner)
	{
		Transform transform = Utils.FindChild(owner.transform, "_RodTop");
		if (transform == null)
		{
			ZLog.LogWarning("Failed to find fishing rod top");
			return null;
		}
		return transform;
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		Character owner = GetOwner();
		if (!owner)
		{
			ZLog.LogWarning("Fishing rod not found, destroying fishing float");
			m_nview.Destroy();
			return;
		}
		Transform rodTop = GetRodTop(owner);
		if (!rodTop)
		{
			ZLog.LogWarning("Fishing rod not found, destroying fishing float");
			m_nview.Destroy();
			return;
		}
		Fish fish = GetCatch();
		if (owner.InAttack() || owner.IsDrawingBow())
		{
			ReturnBait();
			if ((bool)fish)
			{
				fish.OnHooked(null);
			}
			m_nview.Destroy();
			return;
		}
		float magnitude = (rodTop.transform.position - base.transform.position).magnitude;
		ItemDrop itemDrop = (fish ? fish.gameObject.GetComponent<ItemDrop>() : null);
		if (!owner.HaveStamina() && fish != null)
		{
			SetCatch(null);
			fish = null;
			Message("$msg_fishing_lost", prioritized: true);
		}
		float skillFactor = owner.GetSkillFactor(Skills.SkillType.Fishing);
		float num = Mathf.Lerp(m_hookedStaminaPerSec, m_hookedStaminaPerSecMaxSkill, skillFactor);
		if ((bool)fish)
		{
			owner.UseStamina(num * fixedDeltaTime);
		}
		if (!fish && Utils.LengthXZ(m_body.velocity) > 2f)
		{
			TryToHook();
		}
		if (owner.IsBlocking() && owner.HaveStamina())
		{
			float num2 = m_pullStaminaUse;
			if (fish != null)
			{
				num2 += fish.GetStaminaUse() * (float)((itemDrop == null) ? 1 : itemDrop.m_itemData.m_quality);
			}
			num2 = Mathf.Lerp(num2, num2 * m_pullStaminaUseMaxSkillMultiplier, skillFactor);
			owner.UseStamina(num2 * fixedDeltaTime);
			if (m_lineLength > magnitude - 0.2f)
			{
				float lineLength = m_lineLength;
				float num3 = Mathf.Lerp(m_pullLineSpeed, m_pullLineSpeedMaxSkill, skillFactor);
				if ((bool)fish && fish.IsEscaping())
				{
					num3 /= 2f;
				}
				m_lineLength -= fixedDeltaTime * num3;
				m_fishingSkillImproveTimer += fixedDeltaTime * ((fish == null) ? 1f : m_fishingSkillImproveHookedMultiplier);
				if (m_fishingSkillImproveTimer > 1f)
				{
					m_fishingSkillImproveTimer = 0f;
					owner.RaiseSkill(Skills.SkillType.Fishing);
				}
				TryToHook();
				if ((int)m_lineLength != (int)lineLength)
				{
					Message(m_lineLength.ToString("0m"));
				}
			}
			if (m_lineLength <= 0.5f)
			{
				if ((bool)fish)
				{
					string msg = Catch(fish, owner);
					Message(msg, prioritized: true);
					SetCatch(null);
					fish.OnHooked(null);
					m_nview.Destroy();
				}
				else
				{
					ReturnBait();
					m_nview.Destroy();
				}
				return;
			}
		}
		m_rodLine.SetSlack((1f - Utils.LerpStep(m_lineLength / 2f, m_lineLength, magnitude)) * m_maxLineSlack);
		if (magnitude - m_lineLength > m_breakDistance || magnitude > m_maxDistance)
		{
			Message("$msg_fishing_linebroke", prioritized: true);
			if ((bool)fish)
			{
				fish.OnHooked(null);
			}
			m_nview.Destroy();
			m_lineBreakEffect.Create(base.transform.position, Quaternion.identity);
		}
		else
		{
			if ((bool)fish)
			{
				Utils.Pull(m_body, fish.transform.position, 0.5f, m_moveForce, 0.5f, 0.3f);
			}
			Utils.Pull(m_body, rodTop.transform.position, m_lineLength, m_moveForce, 1f, 0.3f);
		}
	}

	public static string Catch(Fish fish, Character owner)
	{
		Humanoid humanoid = owner as Humanoid;
		ItemDrop itemDrop = (fish ? fish.gameObject.GetComponent<ItemDrop>() : null);
		if ((bool)itemDrop)
		{
			itemDrop.Pickup(humanoid);
		}
		else
		{
			fish.Pickup(humanoid);
		}
		string text = "$msg_fishing_catched " + fish.GetHoverName();
		if (!fish.m_extraDrops.IsEmpty())
		{
			foreach (ItemDrop.ItemData dropListItem in fish.m_extraDrops.GetDropListItems())
			{
				text = text + " & " + dropListItem.m_shared.m_name;
				if (humanoid.GetInventory().CanAddItem(dropListItem.m_dropPrefab, dropListItem.m_stack))
				{
					ZLog.Log($"picking up {dropListItem.m_stack}x {dropListItem.m_dropPrefab.name}");
					humanoid.GetInventory().AddItem(dropListItem.m_dropPrefab, dropListItem.m_stack);
				}
				else
				{
					ZLog.Log($"no room, dropping {dropListItem.m_stack}x {dropListItem.m_dropPrefab.name}");
					Object.Instantiate(dropListItem.m_dropPrefab, fish.transform.position, Quaternion.Euler(0f, Random.Range(0, 360), 0f)).GetComponent<ItemDrop>().SetStack(dropListItem.m_stack);
					Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$inventory_full"));
				}
			}
		}
		return text;
	}

	private void ReturnBait()
	{
		if (!m_baitConsumed)
		{
			Character owner = GetOwner();
			string bait = GetBait();
			GameObject prefab = ZNetScene.instance.GetPrefab(bait);
			if ((bool)prefab && owner is Player player)
			{
				player.GetInventory().AddItem(prefab, 1);
			}
		}
	}

	private void TryToHook()
	{
		if (m_nibbler != null && Time.time - m_nibbleTime < 0.5f && GetCatch() == null)
		{
			Message("$msg_fishing_hooked", prioritized: true);
			SetCatch(m_nibbler);
			m_nibbler = null;
		}
	}

	private void SetCatch(Fish fish)
	{
		if ((bool)fish)
		{
			m_nview.GetZDO().Set(ZDOVars.s_sessionCatchID, fish.GetZDOID());
			m_hookLine.SetPeer(fish.GetZDOID());
			fish.OnHooked(this);
			m_baitConsumed = true;
		}
		else
		{
			m_nview.GetZDO().Set(ZDOVars.s_sessionCatchID, ZDOID.None);
			m_hookLine.SetPeer(ZDOID.None);
		}
	}

	public Fish GetCatch()
	{
		if (!m_nview.IsValid())
		{
			return null;
		}
		ZDOID zDOID = m_nview.GetZDO().GetZDOID(ZDOVars.s_sessionCatchID);
		if (!zDOID.IsNone())
		{
			GameObject gameObject = ZNetScene.instance.FindInstance(zDOID);
			if ((bool)gameObject)
			{
				return gameObject.GetComponent<Fish>();
			}
		}
		return null;
	}

	public string GetBait()
	{
		if (m_nview == null || m_nview.GetZDO() == null)
		{
			return null;
		}
		return m_nview.GetZDO().GetString(ZDOVars.s_bait);
	}

	public bool IsInWater()
	{
		return m_floating.HaveLiquidLevel();
	}

	public void Nibble(Fish fish, bool correctBait)
	{
		m_nview.InvokeRPC("RPC_Nibble", fish.GetZDOID(), correctBait);
	}

	public void RPC_Nibble(long sender, ZDOID fishID, bool correctBait)
	{
		if (Time.time - m_nibbleTime < 1f || GetCatch() != null)
		{
			return;
		}
		if (correctBait)
		{
			m_nibbleEffect.Create(base.transform.position, Quaternion.identity, base.transform);
			m_body.AddForce(Vector3.down * m_nibbleForce, ForceMode.VelocityChange);
			GameObject gameObject = ZNetScene.instance.FindInstance(fishID);
			if ((bool)gameObject)
			{
				m_nibbler = gameObject.GetComponent<Fish>();
				m_nibbleTime = Time.time;
			}
		}
		else
		{
			m_body.AddForce(Vector3.down * m_nibbleForce * 0.5f, ForceMode.VelocityChange);
			Message("$msg_fishing_wrongbait", prioritized: true);
		}
	}

	public static List<FishingFloat> GetAllInstances()
	{
		return m_allInstances;
	}

	private static FishingFloat FindFloat(Character owner)
	{
		foreach (FishingFloat allInstance in m_allInstances)
		{
			if (owner == allInstance.GetOwner())
			{
				return allInstance;
			}
		}
		return null;
	}

	public static FishingFloat FindFloat(Fish fish)
	{
		foreach (FishingFloat allInstance in m_allInstances)
		{
			if (allInstance.GetCatch() == fish)
			{
				return allInstance;
			}
		}
		return null;
	}

	private void Message(string msg, bool prioritized = false)
	{
		if (prioritized || !(Time.time - m_msgTime < 1f))
		{
			m_msgTime = Time.time;
			Character owner = GetOwner();
			if ((bool)owner)
			{
				owner.Message(MessageHud.MessageType.Center, Localization.instance.Localize(msg));
			}
		}
	}
}
