using UnityEngine;

public class SpawnPrefab : MonoBehaviour
{
	public GameObject m_prefab;

	private ZNetView m_nview;

	private void Start()
	{
		m_nview = GetComponentInParent<ZNetView>();
		if (m_nview == null)
		{
			ZLog.LogWarning("SpawnerPrefab cant find netview " + base.gameObject.name);
		}
		else
		{
			InvokeRepeating("TrySpawn", 1f, 1f);
		}
	}

	private void TrySpawn()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			string text = "HasSpawned_" + base.gameObject.name;
			if (!m_nview.GetZDO().GetBool(text))
			{
				ZLog.Log("SpawnPrefab " + base.gameObject.name + " SPAWNING " + m_prefab.name);
				Object.Instantiate(m_prefab, base.transform.position, base.transform.rotation);
				m_nview.GetZDO().Set(text, value: true);
			}
			CancelInvoke("TrySpawn");
		}
	}

	private void OnDrawGizmos()
	{
	}
}
