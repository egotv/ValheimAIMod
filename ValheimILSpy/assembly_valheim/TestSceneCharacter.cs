using System.Threading;
using UnityEngine;

public class TestSceneCharacter : MonoBehaviour
{
	public float m_speed = 5f;

	public float m_cameraDistance = 10f;

	private Rigidbody m_body;

	private Quaternion m_lookYaw = Quaternion.identity;

	private float m_lookPitch;

	private void Start()
	{
		m_body = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		Thread.Sleep(30);
		HandleInput(Time.deltaTime);
	}

	private void HandleInput(float dt)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector2 zero = Vector2.zero;
		zero = ZInput.GetMouseDelta();
		if (ZInput.GetKey(KeyCode.Mouse1) || Cursor.lockState != 0)
		{
			m_lookYaw *= Quaternion.Euler(0f, zero.x, 0f);
			m_lookPitch = Mathf.Clamp(m_lookPitch - zero.y, -89f, 89f);
		}
		if (ZInput.GetKeyDown(KeyCode.F1))
		{
			if (Cursor.lockState == CursorLockMode.None)
			{
				Cursor.lockState = CursorLockMode.Locked;
			}
			else
			{
				Cursor.lockState = CursorLockMode.None;
			}
		}
		Vector3 zero2 = Vector3.zero;
		if (ZInput.GetKey(KeyCode.A))
		{
			zero2 -= base.transform.right * m_speed;
		}
		if (ZInput.GetKey(KeyCode.D))
		{
			zero2 += base.transform.right * m_speed;
		}
		if (ZInput.GetKey(KeyCode.W))
		{
			zero2 += base.transform.forward * m_speed;
		}
		if (ZInput.GetKey(KeyCode.S))
		{
			zero2 -= base.transform.forward * m_speed;
		}
		if (ZInput.GetKeyDown(KeyCode.Space))
		{
			m_body.AddForce(Vector3.up * 10f, ForceMode.VelocityChange);
		}
		Vector3 force = zero2 - m_body.velocity;
		force.y = 0f;
		m_body.AddForce(force, ForceMode.VelocityChange);
		base.transform.rotation = m_lookYaw;
		Quaternion quaternion = m_lookYaw * Quaternion.Euler(m_lookPitch, 0f, 0f);
		mainCamera.transform.position = base.transform.position - quaternion * Vector3.forward * m_cameraDistance;
		mainCamera.transform.LookAt(base.transform.position + Vector3.up);
	}
}
