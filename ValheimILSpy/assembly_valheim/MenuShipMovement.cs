using UnityEngine;

public class MenuShipMovement : MonoBehaviour
{
	public float m_freq = 1f;

	public float m_xAngle = 5f;

	public float m_zAngle = 5f;

	private float m_time;

	private void Start()
	{
		m_time = Random.Range(0, 10);
	}

	private void Update()
	{
		m_time += Time.deltaTime;
		base.transform.rotation = Quaternion.Euler(Mathf.Sin(m_time * m_freq) * m_xAngle, 0f, Mathf.Sin(m_time * 1.5341234f * m_freq) * m_zAngle);
	}
}
