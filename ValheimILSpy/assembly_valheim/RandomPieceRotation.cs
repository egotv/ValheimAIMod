using UnityEngine;

public class RandomPieceRotation : MonoBehaviour
{
	public bool m_rotateX;

	public bool m_rotateY;

	public bool m_rotateZ;

	public int m_stepsX = 4;

	public int m_stepsY = 4;

	public int m_stepsZ = 4;

	private void Awake()
	{
		Vector3 position = base.transform.position;
		int seed = (int)position.x * (int)(position.y * 10f) * (int)(position.z * 100f);
		Random.State state = Random.state;
		Random.InitState(seed);
		float x = (m_rotateX ? ((float)Random.Range(0, m_stepsX) * 360f / (float)m_stepsX) : 0f);
		float y = (m_rotateY ? ((float)Random.Range(0, m_stepsY) * 360f / (float)m_stepsY) : 0f);
		float z = (m_rotateZ ? ((float)Random.Range(0, m_stepsZ) * 360f / (float)m_stepsZ) : 0f);
		base.transform.localRotation = Quaternion.Euler(x, y, z);
		Random.state = state;
	}
}
