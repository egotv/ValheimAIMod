using UnityEngine;

public class WeaponLoadState : MonoBehaviour
{
	public GameObject m_unloaded;

	public GameObject m_loaded;

	private Player m_owner;

	private void Start()
	{
		m_owner = GetComponentInParent<Player>();
	}

	private void Update()
	{
		if ((bool)m_owner)
		{
			bool flag = m_owner.IsWeaponLoaded();
			m_unloaded.SetActive(!flag);
			m_loaded.SetActive(flag);
		}
	}
}
