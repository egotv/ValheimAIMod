using UnityEngine;

public class RandomMovement : MonoBehaviour
{
	public float m_frequency = 10f;

	public float m_movement = 0.1f;

	private Vector3 m_basePosition = Vector3.zero;

	private void Start()
	{
		m_basePosition = base.transform.localPosition;
	}

	private void Update()
	{
		float num = Time.time * m_frequency;
		Vector3 vector = new Vector3(Mathf.Sin(num) * Mathf.Sin(num * 0.56436f), Mathf.Sin(num * 0.56436f) * Mathf.Sin(num * 0.688742f), Mathf.Cos(num * 0.758348f) * Mathf.Cos(num * 0.4563696f)) * m_movement;
		base.transform.localPosition = m_basePosition + vector;
	}
}
