using UnityEngine;

public class MovementDamage : MonoBehaviour
{
	public GameObject m_runDamageObject;

	public float m_speedTreshold = 6f;

	private Character m_character;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private void Awake()
	{
		m_character = GetComponent<Character>();
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		Aoe component = m_runDamageObject.GetComponent<Aoe>();
		if ((bool)component)
		{
			component.Setup(m_character, Vector3.zero, 0f, null, null, null);
		}
	}

	private void Update()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			m_runDamageObject.SetActive(value: false);
			return;
		}
		bool active = m_body.velocity.magnitude > m_speedTreshold;
		m_runDamageObject.SetActive(active);
	}
}
