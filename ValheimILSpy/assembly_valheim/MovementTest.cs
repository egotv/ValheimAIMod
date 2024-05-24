using UnityEngine;

public class MovementTest : MonoBehaviour
{
	public float m_speed = 10f;

	private float m_timer;

	private Rigidbody m_body;

	private Vector3 m_center;

	private Vector3 m_vel;

	private void Start()
	{
		m_body = GetComponent<Rigidbody>();
		m_center = base.transform.position;
	}

	private void FixedUpdate()
	{
		m_timer += Time.fixedDeltaTime;
		float num = 5f;
		Vector3 vector = m_center + new Vector3(Mathf.Sin(m_timer * m_speed) * num, 0f, Mathf.Cos(m_timer * m_speed) * num);
		m_vel = (vector - m_body.position) / Time.fixedDeltaTime;
		m_body.position = vector;
		m_body.velocity = m_vel;
	}
}
