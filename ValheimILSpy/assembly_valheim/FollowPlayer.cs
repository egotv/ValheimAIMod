using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
	public enum Type
	{
		Player,
		Camera,
		Average
	}

	public Type m_follow = Type.Camera;

	public bool m_lockYPos;

	public float m_maxYPos = 1000000f;

	private void LateUpdate()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!(Player.m_localPlayer == null) && !(mainCamera == null))
		{
			Vector3 zero = Vector3.zero;
			zero = ((m_follow == Type.Camera || GameCamera.InFreeFly()) ? mainCamera.transform.position : ((m_follow != Type.Average) ? Player.m_localPlayer.transform.position : ((!GameCamera.InFreeFly()) ? ((mainCamera.transform.position + Player.m_localPlayer.transform.position) * 0.5f) : mainCamera.transform.position)));
			if (m_lockYPos)
			{
				zero.y = base.transform.position.y;
			}
			if (zero.y > m_maxYPos)
			{
				zero.y = m_maxYPos;
			}
			base.transform.position = zero;
		}
	}
}
