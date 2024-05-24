using UnityEngine;

public class AlphaMotion : MonoBehaviour
{
	public float m_rotSpeed = 100f;

	public float m_rotAngle = 0.2f;

	private void Start()
	{
	}

	private void Update()
	{
		float time = Time.time;
		base.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * m_rotSpeed) * m_rotAngle);
	}
}
