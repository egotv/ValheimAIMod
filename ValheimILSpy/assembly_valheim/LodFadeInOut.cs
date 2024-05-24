using UnityEngine;

public class LodFadeInOut : MonoBehaviour
{
	private Vector3 m_originalLocalRef;

	private LODGroup m_lodGroup;

	private const float m_minTriggerDistance = 20f;

	private void Awake()
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!(mainCamera == null) && Vector3.Distance(mainCamera.transform.position, base.transform.position) > 20f)
		{
			m_lodGroup = GetComponent<LODGroup>();
			if ((bool)m_lodGroup)
			{
				m_originalLocalRef = m_lodGroup.localReferencePoint;
				m_lodGroup.localReferencePoint = new Vector3(999999f, 999999f, 999999f);
				Invoke("FadeIn", Random.Range(0.1f, 0.3f));
			}
		}
	}

	private void FadeIn()
	{
		m_lodGroup.localReferencePoint = m_originalLocalRef;
	}
}
