using System;
using UnityEngine;

public class FloatingTerrain : MonoBehaviour
{
	public float m_padding;

	public float m_waveMinOffset;

	public float m_waveFreq;

	public float m_waveAmp;

	public FloatingTerrainDummy m_dummy;

	public float m_maxCorrectionSpeed = 0.025f;

	public bool m_copyLayer = true;

	private Rigidbody m_body;

	[NonSerialized]
	public Rigidbody m_dummyBody;

	private BoxCollider m_collider;

	private BoxCollider m_dummyCollider;

	private Heightmap m_lastHeightmap;

	private Vector3 m_lastGroundNormal;

	private float m_targetOffset;

	private float m_currentOffset;

	private float m_lastHeightmapTime;

	private float m_waveTime;

	private void Start()
	{
		m_body = GetComponent<Rigidbody>();
		m_collider = GetComponentInChildren<BoxCollider>();
		InvokeRepeating("UpdateTerrain", UnityEngine.Random.Range(0.1f, 0.4f), 0.24f);
		UpdateTerrain();
	}

	private void UpdateTerrain()
	{
		if (!m_lastHeightmap)
		{
			m_targetOffset = 0f;
			return;
		}
		m_targetOffset = m_lastHeightmap.GetHeightOffset(base.transform.position) + m_padding;
		if (!m_dummy)
		{
			GameObject gameObject = new GameObject();
			if (m_copyLayer)
			{
				gameObject.layer = base.gameObject.layer;
			}
			m_dummy = gameObject.AddComponent<FloatingTerrainDummy>();
			m_dummy.m_parent = this;
			m_dummyBody = gameObject.AddComponent<Rigidbody>();
			m_dummyBody.mass = m_body.mass;
			m_dummyBody.drag = m_body.drag;
			m_dummyBody.angularDrag = m_body.angularDrag;
			m_dummyBody.constraints = m_body.constraints;
			m_dummyCollider = gameObject.AddComponent<BoxCollider>();
			m_dummyCollider.center = m_collider.center;
			m_dummyCollider.size = m_collider.size;
			if (m_collider.gameObject != this)
			{
				m_dummyCollider.size = Vector3.Scale(m_collider.size, m_collider.transform.localScale);
				m_dummyCollider.center = Vector3.Scale(m_collider.center, m_collider.transform.localScale);
				m_dummyCollider.center -= m_collider.transform.localPosition;
			}
			gameObject.transform.parent = base.transform.parent;
			gameObject.transform.position = base.transform.position;
			m_collider.isTrigger = true;
			UnityEngine.Object.Destroy(m_body);
		}
	}

	private void FixedUpdate()
	{
		if ((bool)m_dummy)
		{
			float maxCorrectionSpeed = m_maxCorrectionSpeed;
			float value = m_targetOffset - m_currentOffset;
			m_currentOffset += Mathf.Clamp(value, 0f - maxCorrectionSpeed, maxCorrectionSpeed);
			float num = m_currentOffset;
			if (m_waveFreq > 0f && num > m_waveMinOffset)
			{
				m_waveTime += Time.fixedDeltaTime;
				num += Mathf.Cos(m_waveTime * m_waveFreq) * m_waveAmp;
			}
			base.transform.position = m_dummy.transform.position + new Vector3(0f, num, 0f);
			base.transform.rotation = m_dummy.transform.rotation;
		}
	}

	public void OnDummyCollision(Collision collision)
	{
		OnCollisionStay(collision);
	}

	private void OnCollisionStay(Collision collision)
	{
		Heightmap component = collision.gameObject.GetComponent<Heightmap>();
		if ((object)component != null)
		{
			m_lastGroundNormal = collision.contacts[0].normal;
			m_lastHeightmapTime = Time.time;
			if (m_lastHeightmap != component)
			{
				m_lastHeightmap = component;
				UpdateTerrain();
			}
		}
		else if (m_lastHeightmapTime > 0.2f)
		{
			m_lastHeightmap = null;
		}
	}

	private void OnDrawGizmos()
	{
		if ((bool)m_dummyCollider && m_dummyCollider.enabled)
		{
			Gizmos.color = Color.yellow;
			Gizmos.matrix = Matrix4x4.TRS(m_dummyCollider.transform.position, m_dummyCollider.transform.rotation, m_dummyCollider.transform.lossyScale);
			Gizmos.DrawWireCube(m_dummyCollider.center, m_dummyCollider.size);
		}
		if (m_dummy != null)
		{
			Gizmos.DrawLine(base.transform.position, base.transform.position + new Vector3(0f, m_currentOffset, 0f));
		}
	}

	private void OnDestroy()
	{
		if ((bool)m_dummy)
		{
			UnityEngine.Object.Destroy(m_dummy.gameObject);
		}
	}

	public static Rigidbody GetBody(GameObject obj)
	{
		FloatingTerrain component = obj.GetComponent<FloatingTerrain>();
		if ((object)component != null && (bool)component.m_dummy && (bool)component.m_dummyBody)
		{
			return component.m_dummyBody;
		}
		return null;
	}
}
