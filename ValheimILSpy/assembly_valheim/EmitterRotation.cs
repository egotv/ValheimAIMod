using UnityEngine;

public class EmitterRotation : MonoBehaviour
{
	public float m_maxSpeed = 10f;

	public float m_rotSpeed = 90f;

	private Vector3 m_lastPos;

	private ParticleSystem m_ps;

	private void Start()
	{
		m_lastPos = base.transform.position;
		m_ps = GetComponentInChildren<ParticleSystem>();
	}

	private void Update()
	{
		if (m_ps.emission.enabled)
		{
			Vector3 position = base.transform.position;
			Vector3 vector = position - m_lastPos;
			m_lastPos = position;
			float t = Mathf.Clamp01(vector.magnitude / Time.deltaTime / m_maxSpeed);
			if (vector == Vector3.zero)
			{
				vector = Vector3.up;
			}
			Quaternion a = Quaternion.LookRotation(Vector3.up);
			Quaternion b = Quaternion.LookRotation(vector);
			Quaternion to = Quaternion.Lerp(a, b, t);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Time.deltaTime * m_rotSpeed);
		}
	}
}
