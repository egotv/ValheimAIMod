using System.Collections;
using UnityEngine;

public class Radiator : MonoBehaviour
{
	public GameObject m_projectile;

	public Collider m_emitFrom;

	public float m_rateMin = 2f;

	public float m_rateMax = 5f;

	public float m_velocity = 10f;

	public float m_offset = 0.1f;

	private ZNetView m_nview;

	private void Start()
	{
		m_nview = GetComponentInParent<ZNetView>();
	}

	private void OnEnable()
	{
		StartCoroutine("UpdateLoop");
	}

	private IEnumerator UpdateLoop()
	{
		while (true)
		{
			yield return new WaitForSeconds(Random.Range(m_rateMin, m_rateMax));
			if (m_nview.IsValid() && m_nview.IsOwner())
			{
				Vector3 onUnitSphere = Random.onUnitSphere;
				Vector3 position = base.transform.position;
				if (onUnitSphere.y < 0f)
				{
					onUnitSphere.y = 0f - onUnitSphere.y;
				}
				if ((bool)m_emitFrom)
				{
					position = m_emitFrom.ClosestPoint(m_emitFrom.transform.position + onUnitSphere * 1000f) + onUnitSphere * m_offset;
				}
				Object.Instantiate(m_projectile, position, Quaternion.LookRotation(onUnitSphere, Vector3.up)).GetComponent<Projectile>().Setup(null, onUnitSphere * m_velocity, 0f, null, null, null);
			}
		}
	}
}
