using UnityEngine;

public class DropProjectileOverDistance : MonoBehaviour
{
	public GameObject m_projectilePrefab;

	public float m_distancePerProjectile = 5f;

	public float m_spawnHeight = 1f;

	public bool m_snapToGround;

	[Tooltip("If higher than 0, will force a spawn if nothing has spawned in that amount of time.")]
	public float m_timeToForceSpawn = -1f;

	public float m_minVelocity;

	public float m_maxVelocity;

	private Character m_character;

	private ZNetView m_nview;

	private Vector3 lastPosition;

	private float m_distanceAccumulator;

	private float m_spawnTimer;

	private const int c_MaxSpawnsPerFrame = 3;

	private void Awake()
	{
		m_character = GetComponent<Character>();
		m_nview = GetComponent<ZNetView>();
		if ((object)m_projectilePrefab == null)
		{
			base.enabled = false;
		}
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		Vector3 vector = base.transform.position.Horizontal();
		m_distanceAccumulator += Vector3.Distance(lastPosition, vector);
		Vector3 vector2 = lastPosition.DirTo(vector);
		if (lastPosition != vector)
		{
			lastPosition = vector;
		}
		if (m_timeToForceSpawn > 0f)
		{
			m_spawnTimer += Time.deltaTime;
			if (m_spawnTimer > m_timeToForceSpawn)
			{
				SpawnProjectile(base.transform.position, vector2);
				m_distanceAccumulator -= m_distancePerProjectile;
				m_distanceAccumulator = Mathf.Max(m_distanceAccumulator, 0f);
			}
		}
		if (!(m_distanceAccumulator < m_distancePerProjectile))
		{
			int num = Mathf.FloorToInt(m_distanceAccumulator / m_distancePerProjectile);
			for (int i = 0; i < Mathf.Min(3, num); i++)
			{
				SpawnProjectile(base.transform.position - vector2 * i, vector2);
				m_distanceAccumulator -= m_distancePerProjectile;
				num--;
			}
			m_distanceAccumulator -= m_distancePerProjectile * (float)num;
		}
	}

	private void SpawnProjectile(Vector3 point, Vector3 travelDirection)
	{
		m_spawnTimer = 0f;
		if (m_projectilePrefab.GetComponent<IProjectile>() == null)
		{
			ZLog.LogWarning("Attempted to spawn non-projectile");
		}
		point.y += m_spawnHeight;
		if (m_snapToGround)
		{
			ZoneSystem.instance.GetSolidHeight(point, out var height);
			point.y = height;
		}
		Object.Instantiate(m_projectilePrefab, point, Quaternion.LookRotation(travelDirection)).GetComponent<IProjectile>().Setup(m_character, travelDirection * Random.Range(m_minVelocity, m_maxVelocity), -1f, null, null, null);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(0.76f, 0.52f, 0.55f);
		Gizmos.DrawLine(base.transform.position, base.transform.position + Vector3.up * m_spawnHeight);
		Vector3 vector = base.transform.position + base.transform.forward * m_distancePerProjectile;
		Gizmos.DrawLine(base.transform.position + Vector3.up * 0.5f * m_spawnHeight, vector + Vector3.up * 0.5f * m_spawnHeight);
		Gizmos.DrawLine(vector, vector + Vector3.up * m_spawnHeight);
	}
}
