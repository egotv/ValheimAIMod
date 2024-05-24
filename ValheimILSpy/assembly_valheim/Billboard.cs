using UnityEngine;

public class Billboard : MonoBehaviour
{
	public bool m_vertical = true;

	public bool m_invert;

	private Vector3 m_normal;

	private void Awake()
	{
		m_normal = base.transform.up;
	}

	private void LateUpdate()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!(mainCamera == null))
		{
			Vector3 vector = mainCamera.transform.position;
			if (m_invert)
			{
				vector = base.transform.position - (vector - base.transform.position);
			}
			if (m_vertical)
			{
				vector.y = base.transform.position.y;
				base.transform.LookAt(vector, m_normal);
			}
			else
			{
				base.transform.LookAt(vector);
			}
		}
	}
}
