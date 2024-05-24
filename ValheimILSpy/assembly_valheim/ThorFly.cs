using UnityEngine;

public class ThorFly : MonoBehaviour
{
	public float m_speed = 100f;

	public float m_ttl = 10f;

	private float m_timer;

	private void Start()
	{
	}

	private void Update()
	{
		base.transform.position = base.transform.position + base.transform.forward * m_speed * Time.deltaTime;
		m_timer += Time.deltaTime;
		if (m_timer > m_ttl)
		{
			Object.Destroy(base.gameObject);
		}
	}
}
