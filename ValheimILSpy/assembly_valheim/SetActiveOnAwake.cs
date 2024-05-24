using UnityEngine;

public class SetActiveOnAwake : MonoBehaviour
{
	[SerializeField]
	private GameObject m_objectToSetActive;

	private void Awake()
	{
		if (m_objectToSetActive != null)
		{
			m_objectToSetActive.SetActive(value: true);
		}
	}
}
