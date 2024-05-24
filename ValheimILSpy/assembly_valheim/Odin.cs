using UnityEngine;

public class Odin : MonoBehaviour
{
	public float m_despawnCloseDistance = 20f;

	public float m_despawnFarDistance = 50f;

	public EffectList m_despawn = new EffectList();

	public float m_ttl = 300f;

	private float m_time;

	private ZNetView m_nview;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
	}

	private void Update()
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, m_despawnFarDistance);
		if (closestPlayer == null)
		{
			m_despawn.Create(base.transform.position, base.transform.rotation);
			m_nview.Destroy();
			ZLog.Log("No player in range, despawning");
			return;
		}
		Vector3 forward = closestPlayer.transform.position - base.transform.position;
		forward.y = 0f;
		forward.Normalize();
		base.transform.rotation = Quaternion.LookRotation(forward);
		if (Vector3.Distance(closestPlayer.transform.position, base.transform.position) < m_despawnCloseDistance)
		{
			m_despawn.Create(base.transform.position, base.transform.rotation);
			m_nview.Destroy();
			ZLog.Log("Player go too close,despawning");
			return;
		}
		m_time += Time.deltaTime;
		if (m_time > m_ttl)
		{
			m_despawn.Create(base.transform.position, base.transform.rotation);
			m_nview.Destroy();
			ZLog.Log("timeout " + m_time + " , despawning");
		}
	}
}
