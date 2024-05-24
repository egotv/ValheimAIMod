using System;
using UnityEngine;

public class SpawnOnDamaged : MonoBehaviour
{
	public GameObject m_spawnOnDamage;

	private void Start()
	{
		WearNTear component = GetComponent<WearNTear>();
		if ((bool)component)
		{
			component.m_onDamaged = (Action)Delegate.Combine(component.m_onDamaged, new Action(OnDamaged));
		}
		Destructible component2 = GetComponent<Destructible>();
		if ((bool)component2)
		{
			component2.m_onDamaged = (Action)Delegate.Combine(component2.m_onDamaged, new Action(OnDamaged));
		}
	}

	private void OnDamaged()
	{
		if ((bool)m_spawnOnDamage)
		{
			UnityEngine.Object.Instantiate(m_spawnOnDamage, base.transform.position, Quaternion.identity);
		}
	}
}
