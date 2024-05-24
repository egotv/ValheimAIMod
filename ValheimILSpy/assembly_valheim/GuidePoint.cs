using UnityEngine;

public class GuidePoint : MonoBehaviour
{
	public Raven.RavenText m_text = new Raven.RavenText();

	public GameObject m_ravenPrefab;

	private void Start()
	{
		if (!Raven.IsInstantiated())
		{
			Object.Instantiate(m_ravenPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
		}
		m_text.m_static = true;
		m_text.m_guidePoint = this;
		Raven.RegisterStaticText(m_text);
	}

	private void OnDestroy()
	{
		Raven.UnregisterStaticText(m_text);
	}

	private void OnDrawGizmos()
	{
	}
}
