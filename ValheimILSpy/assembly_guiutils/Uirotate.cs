using UnityEngine;

public class Uirotate : MonoBehaviour
{
	public float m_speed = 1f;

	private void Update()
	{
		base.transform.Rotate(0f, 0f, Time.deltaTime * m_speed);
	}
}
