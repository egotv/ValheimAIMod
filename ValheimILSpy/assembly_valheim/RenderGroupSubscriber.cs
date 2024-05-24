using UnityEngine;

public class RenderGroupSubscriber : MonoBehaviour
{
	private MeshRenderer m_renderer;

	[SerializeField]
	private RenderGroup m_group;

	private bool m_isRegistered;

	public RenderGroup Group
	{
		get
		{
			return m_group;
		}
		set
		{
			Unregister();
			m_group = value;
			Register();
		}
	}

	private void Start()
	{
		Register();
	}

	private void OnEnable()
	{
		Register();
	}

	private void OnDisable()
	{
		Unregister();
	}

	private void Register()
	{
		if (m_isRegistered)
		{
			return;
		}
		if (m_renderer == null)
		{
			m_renderer = GetComponent<MeshRenderer>();
			if (m_renderer == null)
			{
				return;
			}
		}
		m_isRegistered = RenderGroupSystem.Register(m_group, OnGroupChanged);
	}

	private void Unregister()
	{
		if (m_isRegistered)
		{
			RenderGroupSystem.Unregister(m_group, OnGroupChanged);
			m_isRegistered = false;
		}
	}

	private void OnGroupChanged(bool shouldRender)
	{
		m_renderer.enabled = shouldRender;
	}
}
