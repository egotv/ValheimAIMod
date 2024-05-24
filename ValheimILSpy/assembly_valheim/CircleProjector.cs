using System;
using System.Collections.Generic;
using UnityEngine;

public class CircleProjector : MonoBehaviour
{
	public float m_radius = 5f;

	public int m_nrOfSegments = 20;

	public float m_speed = 0.1f;

	public float m_turns = 1f;

	public float m_start;

	public bool m_sliceLines;

	private float m_calcStart;

	private float m_calcTurns;

	public GameObject m_prefab;

	public LayerMask m_mask;

	private List<GameObject> m_segments = new List<GameObject>();

	private void Start()
	{
		CreateSegments();
	}

	private void Update()
	{
		CreateSegments();
		bool flag = m_turns == 1f;
		float num = (float)Math.PI * 2f * m_turns / (float)(m_nrOfSegments - ((!flag) ? 1 : 0));
		float num2 = ((flag && !m_sliceLines) ? (Time.time * m_speed) : 0f);
		for (int i = 0; i < m_nrOfSegments; i++)
		{
			float f = (float)Math.PI / 180f * m_start + (float)i * num + num2;
			Vector3 vector = base.transform.position + new Vector3(Mathf.Sin(f) * m_radius, 0f, Mathf.Cos(f) * m_radius);
			GameObject obj = m_segments[i];
			if (Physics.Raycast(vector + Vector3.up * 500f, Vector3.down, out var hitInfo, 1000f, m_mask.value))
			{
				vector.y = hitInfo.point.y;
			}
			obj.transform.position = vector;
		}
		for (int j = 0; j < m_nrOfSegments; j++)
		{
			GameObject gameObject = m_segments[j];
			GameObject gameObject2;
			GameObject gameObject3;
			if (flag)
			{
				gameObject2 = ((j == 0) ? m_segments[m_nrOfSegments - 1] : m_segments[j - 1]);
				gameObject3 = ((j == m_nrOfSegments - 1) ? m_segments[0] : m_segments[j + 1]);
			}
			else
			{
				gameObject2 = ((j == 0) ? gameObject : m_segments[j - 1]);
				gameObject3 = ((j == m_nrOfSegments - 1) ? gameObject : m_segments[j + 1]);
			}
			Vector3 normalized = (gameObject3.transform.position - gameObject2.transform.position).normalized;
			gameObject.transform.rotation = Quaternion.LookRotation(normalized, Vector3.up);
		}
		for (int k = m_nrOfSegments; k < m_segments.Count; k++)
		{
			Vector3 position = m_segments[k].transform.position;
			if (Physics.Raycast(position + Vector3.up * 500f, Vector3.down, out var hitInfo2, 1000f, m_mask.value))
			{
				position.y = hitInfo2.point.y;
			}
			m_segments[k].transform.position = position;
		}
	}

	private void CreateSegments()
	{
		if ((!m_sliceLines && m_segments.Count == m_nrOfSegments) || (m_sliceLines && m_calcStart == m_start && m_calcTurns == m_turns))
		{
			return;
		}
		foreach (GameObject segment in m_segments)
		{
			UnityEngine.Object.Destroy(segment);
		}
		m_segments.Clear();
		for (int i = 0; i < m_nrOfSegments; i++)
		{
			GameObject item = UnityEngine.Object.Instantiate(m_prefab, base.transform.position, Quaternion.identity, base.transform);
			m_segments.Add(item);
		}
		m_calcStart = m_start;
		m_calcTurns = m_turns;
		if (m_sliceLines)
		{
			float start = m_start;
			float angle2 = m_start + (float)Math.PI * 2f * m_turns * 57.29578f;
			float num = 2f * m_radius * (float)Math.PI * m_turns / (float)m_nrOfSegments;
			int count2 = (int)(m_radius / num) - 2;
			placeSlices(start, count2);
			placeSlices(angle2, count2);
		}
		void placeSlices(float angle, int count)
		{
			for (int j = 0; j < count; j++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(m_prefab, base.transform.position, Quaternion.Euler(0f, angle, 0f), base.transform);
				gameObject.transform.position += gameObject.transform.forward * m_radius * ((float)(j + 1) / (float)(count + 1));
				m_segments.Add(gameObject);
			}
		}
	}
}
