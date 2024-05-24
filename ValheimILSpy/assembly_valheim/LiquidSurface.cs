using System.Collections.Generic;
using UnityEngine;

public class LiquidSurface : MonoBehaviour
{
	private LiquidVolume m_liquid;

	private readonly List<IWaterInteractable> m_inWater = new List<IWaterInteractable>();

	private static readonly List<int> s_inWaterRemoveIndices = new List<int>();

	private void Awake()
	{
		m_liquid = GetComponentInParent<LiquidVolume>();
	}

	private void FixedUpdate()
	{
		UpdateFloaters();
	}

	public LiquidType GetLiquidType()
	{
		return m_liquid.m_liquidType;
	}

	public float GetSurface(Vector3 p)
	{
		return m_liquid.GetSurface(p);
	}

	private void OnTriggerEnter(Collider collider)
	{
		IWaterInteractable component = collider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null)
		{
			component.Increment(m_liquid.m_liquidType);
			if (!m_inWater.Contains(component))
			{
				m_inWater.Add(component);
			}
		}
	}

	private void UpdateFloaters()
	{
		if (m_inWater.Count == 0)
		{
			return;
		}
		s_inWaterRemoveIndices.Clear();
		for (int i = 0; i < m_inWater.Count; i++)
		{
			IWaterInteractable waterInteractable = m_inWater[i];
			if (waterInteractable == null)
			{
				s_inWaterRemoveIndices.Add(i);
				continue;
			}
			Transform transform = waterInteractable.GetTransform();
			if ((bool)transform)
			{
				float surface = m_liquid.GetSurface(transform.position);
				waterInteractable.SetLiquidLevel(surface, m_liquid.m_liquidType, this);
			}
			else
			{
				s_inWaterRemoveIndices.Add(i);
			}
		}
		for (int num = s_inWaterRemoveIndices.Count - 1; num >= 0; num--)
		{
			m_inWater.RemoveAt(s_inWaterRemoveIndices[num]);
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		IWaterInteractable component = collider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null)
		{
			if (component.Decrement(m_liquid.m_liquidType) == 0)
			{
				component.SetLiquidLevel(-10000f, m_liquid.m_liquidType, this);
			}
			m_inWater.Remove(component);
		}
	}

	private void OnDestroy()
	{
		foreach (IWaterInteractable item in m_inWater)
		{
			if (item != null && item.Decrement(m_liquid.m_liquidType) == 0)
			{
				item.SetLiquidLevel(-10000f, m_liquid.m_liquidType, this);
			}
		}
		m_inWater.Clear();
	}
}
