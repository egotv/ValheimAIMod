using UnityEngine;

public class ReflectionUpdate : MonoBehaviour
{
	private static ReflectionUpdate m_instance;

	public ReflectionProbe m_probe1;

	public ReflectionProbe m_probe2;

	public float m_interval = 3f;

	public float m_reflectionHeight = 5f;

	public float m_transitionDuration = 3f;

	public float m_power = 1f;

	private ReflectionProbe m_current;

	private int m_renderID;

	private float m_updateTimer;

	public static ReflectionUpdate instance => m_instance;

	private void Start()
	{
		m_instance = this;
		m_current = m_probe1;
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	public void UpdateReflection()
	{
		Vector3 referencePosition = ZNet.instance.GetReferencePosition();
		referencePosition += Vector3.up * m_reflectionHeight;
		m_current = ((m_current == m_probe1) ? m_probe2 : m_probe1);
		m_current.transform.position = referencePosition;
		m_renderID = m_current.RenderProbe();
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		m_updateTimer += deltaTime;
		if (m_updateTimer > m_interval)
		{
			m_updateTimer = 0f;
			UpdateReflection();
		}
		if (m_current.IsFinishedRendering(m_renderID))
		{
			float f = Mathf.Clamp01(m_updateTimer / m_transitionDuration);
			f = Mathf.Pow(f, m_power);
			if (m_probe1 == m_current)
			{
				m_probe1.importance = 1;
				m_probe2.importance = 0;
				Vector3 size = m_probe1.size;
				size.x = 2000f * f;
				size.y = 1000f * f;
				size.z = 2000f * f;
				m_probe1.size = size;
				m_probe2.size = new Vector3(2001f, 1001f, 2001f);
			}
			else
			{
				m_probe1.importance = 0;
				m_probe2.importance = 1;
				Vector3 size2 = m_probe2.size;
				size2.x = 2000f * f;
				size2.y = 1000f * f;
				size2.z = 2000f * f;
				m_probe2.size = size2;
				m_probe1.size = new Vector3(2001f, 1001f, 2001f);
			}
		}
	}
}
