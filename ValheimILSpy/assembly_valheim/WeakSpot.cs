using System;
using UnityEngine;

public class WeakSpot : MonoBehaviour
{
	public HitData.DamageModifiers m_damageModifiers;

	[NonSerialized]
	public Collider m_collider;

	private void Awake()
	{
		m_collider = GetComponent<Collider>();
	}
}
