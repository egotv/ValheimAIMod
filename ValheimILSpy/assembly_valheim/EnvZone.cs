using UnityEngine;

public class EnvZone : MonoBehaviour
{
	public string m_environment = "";

	public bool m_force = true;

	public MeshRenderer m_exteriorMesh;

	private static EnvZone s_triggered;

	private void Awake()
	{
		if ((bool)m_exteriorMesh)
		{
			m_exteriorMesh.forceRenderingOff = true;
		}
	}

	private void OnTriggerStay(Collider collider)
	{
		Player component = collider.GetComponent<Player>();
		if (!(component == null) && !(Player.m_localPlayer != component))
		{
			if (m_force && string.IsNullOrEmpty(EnvMan.instance.m_debugEnv))
			{
				EnvMan.instance.SetForceEnvironment(m_environment);
			}
			s_triggered = this;
			if ((bool)m_exteriorMesh)
			{
				m_exteriorMesh.forceRenderingOff = false;
			}
		}
	}

	private void OnTriggerExit(Collider collider)
	{
		if (s_triggered != this)
		{
			return;
		}
		Player component = collider.GetComponent<Player>();
		if (!(component == null) && !(Player.m_localPlayer != component))
		{
			if (m_force)
			{
				EnvMan.instance.SetForceEnvironment("");
			}
			s_triggered = null;
		}
	}

	public static string GetEnvironment()
	{
		if ((bool)s_triggered && !s_triggered.m_force)
		{
			return s_triggered.m_environment;
		}
		return null;
	}

	private void Update()
	{
		if ((bool)m_exteriorMesh)
		{
			m_exteriorMesh.forceRenderingOff = s_triggered != this;
		}
	}
}
