using System.Collections.Generic;
using UnityEngine;

public class ProximityState : MonoBehaviour
{
	public bool m_playerOnly = true;

	public Animator m_animator;

	public EffectList m_movingClose = new EffectList();

	public EffectList m_movingAway = new EffectList();

	private List<Collider> m_near = new List<Collider>();

	private void Start()
	{
		m_animator.SetBool("near", value: false);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (m_playerOnly)
		{
			Character component = other.GetComponent<Character>();
			if (!component || !component.IsPlayer())
			{
				return;
			}
		}
		if (!m_near.Contains(other))
		{
			m_near.Add(other);
			if (!m_animator.GetBool("near"))
			{
				m_animator.SetBool("near", value: true);
				m_movingClose.Create(base.transform.position, base.transform.rotation);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		m_near.Remove(other);
		if (m_near.Count == 0 && m_animator.GetBool("near"))
		{
			m_animator.SetBool("near", value: false);
			m_movingAway.Create(base.transform.position, base.transform.rotation);
		}
	}
}
