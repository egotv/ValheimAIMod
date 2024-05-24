using UnityEngine;

public class FloatingTerrainDummy : MonoBehaviour
{
	public FloatingTerrain m_parent;

	private void OnCollisionStay(Collision collision)
	{
		if (!m_parent)
		{
			Object.Destroy(this);
		}
		m_parent.OnDummyCollision(collision);
	}
}
