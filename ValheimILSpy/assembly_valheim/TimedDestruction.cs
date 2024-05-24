using UnityEngine;

public class TimedDestruction : MonoBehaviour
{
	public float m_timeout = 1f;

	public bool m_triggerOnAwake;

	[Tooltip("If there are objects that you always want to destroy, even if there is no owner, check this. For instance, fires in the ashlands may be created by cinder rain outside of ownership-zones, so they must be deleted even if no owner exists.")]
	public bool m_forceTakeOwnershipAndDestroy;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_triggerOnAwake)
		{
			Trigger();
		}
	}

	public void Trigger()
	{
		InvokeRepeating("DestroyNow", m_timeout, 1f);
	}

	public void Trigger(float timeout)
	{
		InvokeRepeating("DestroyNow", timeout, 1f);
	}

	private void DestroyNow()
	{
		if ((bool)m_nview)
		{
			if (m_nview.IsValid())
			{
				if (!m_nview.HasOwner() && m_forceTakeOwnershipAndDestroy)
				{
					m_nview.ClaimOwnership();
				}
				if (m_nview.IsOwner())
				{
					ZNetScene.instance.Destroy(base.gameObject);
				}
			}
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}
}
