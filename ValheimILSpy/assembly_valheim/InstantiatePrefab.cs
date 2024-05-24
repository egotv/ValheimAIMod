using UnityEngine;

public class InstantiatePrefab : MonoBehaviour
{
	public GameObject m_prefab;

	public bool m_attach = true;

	public bool m_moveToTop;

	private void Awake()
	{
		if (m_attach)
		{
			Object.Instantiate(m_prefab, base.transform).transform.SetAsFirstSibling();
		}
		else
		{
			Object.Instantiate(m_prefab);
		}
	}
}
