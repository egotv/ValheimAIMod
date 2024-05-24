using UnityEngine;

public class CharacterTimedDestruction : MonoBehaviour
{
	public float m_timeoutMin = 1f;

	public float m_timeoutMax = 1f;

	public bool m_triggerOnAwake;

	private ZNetView m_nview;

	private Character m_character;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_triggerOnAwake)
		{
			Trigger();
		}
	}

	public void Trigger()
	{
		InvokeRepeating("DestroyNow", Random.Range(m_timeoutMin, m_timeoutMax), 1f);
	}

	public void Trigger(float timeout)
	{
		InvokeRepeating("DestroyNow", timeout, 1f);
	}

	private void DestroyNow()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			GetComponent<Character>().ApplyDamage(new HitData
			{
				m_damage = 
				{
					m_damage = 99999f
				},
				m_point = base.transform.position
			}, showDamageText: false, triggerEffects: true);
		}
	}
}
