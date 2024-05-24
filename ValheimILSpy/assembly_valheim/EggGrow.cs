using UnityEngine;

public class EggGrow : MonoBehaviour, Hoverable
{
	public float m_growTime = 60f;

	public GameObject m_grownPrefab;

	public bool m_tamed;

	public float m_updateInterval = 5f;

	public bool m_requireNearbyFire = true;

	public bool m_requireUnderRoof = true;

	public float m_requireCoverPercentige = 0.7f;

	public EffectList m_hatchEffect;

	public GameObject m_growingObject;

	public GameObject m_notGrowingObject;

	private ZNetView m_nview;

	private ItemDrop m_item;

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		m_item = GetComponent<ItemDrop>();
		InvokeRepeating("GrowUpdate", Random.Range(m_updateInterval, m_updateInterval * 2f), m_updateInterval);
		if ((bool)m_growingObject)
		{
			m_growingObject.SetActive(value: false);
		}
		if ((bool)m_notGrowingObject)
		{
			m_notGrowingObject.SetActive(value: true);
		}
	}

	private void GrowUpdate()
	{
		float num = m_nview.GetZDO().GetFloat(ZDOVars.s_growStart);
		if (!m_nview.IsValid() || !m_nview.IsOwner() || m_item.m_itemData.m_stack > 1)
		{
			UpdateEffects(num);
			return;
		}
		if (CanGrow())
		{
			if (num == 0f)
			{
				num = (float)ZNet.instance.GetTimeSeconds();
			}
		}
		else
		{
			num = 0f;
		}
		m_nview.GetZDO().Set(ZDOVars.s_growStart, num);
		UpdateEffects(num);
		if (num > 0f && ZNet.instance.GetTimeSeconds() > (double)(num + m_growTime))
		{
			Character component = Object.Instantiate(m_grownPrefab, base.transform.position, base.transform.rotation).GetComponent<Character>();
			m_hatchEffect.Create(base.transform.position, base.transform.rotation);
			if ((bool)component)
			{
				component.SetTamed(m_tamed);
				component.SetLevel(m_item.m_itemData.m_quality);
			}
			m_nview.Destroy();
		}
	}

	private bool CanGrow()
	{
		if (m_item.m_itemData.m_stack > 1)
		{
			return false;
		}
		if (m_requireNearbyFire && !EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.Heat, 0.5f))
		{
			return false;
		}
		if (m_requireUnderRoof)
		{
			Cover.GetCoverForPoint(base.transform.position, out var coverPercentage, out var underRoof, 0.1f);
			if (!underRoof || coverPercentage < m_requireCoverPercentige)
			{
				return false;
			}
		}
		return true;
	}

	private void UpdateEffects(float grow)
	{
		if ((bool)m_growingObject)
		{
			m_growingObject.SetActive(grow > 0f);
		}
		if ((bool)m_notGrowingObject)
		{
			m_notGrowingObject.SetActive(grow == 0f);
		}
	}

	public string GetHoverText()
	{
		if (!m_item)
		{
			return "";
		}
		if (!m_nview || !m_nview.IsValid())
		{
			return m_item.GetHoverText();
		}
		bool flag = m_nview.GetZDO().GetFloat(ZDOVars.s_growStart) > 0f;
		string text = ((m_item.m_itemData.m_stack > 1) ? "$item_chicken_egg_stacked" : (flag ? "$item_chicken_egg_warm" : "$item_chicken_egg_cold"));
		string hoverText = m_item.GetHoverText();
		int num = hoverText.IndexOf('\n');
		if (num > 0)
		{
			return hoverText.Substring(0, num) + " " + Localization.instance.Localize(text) + hoverText.Substring(num);
		}
		return m_item.GetHoverText();
	}

	public string GetHoverName()
	{
		return m_item.GetHoverName();
	}
}
