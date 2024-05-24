using System;
using UnityEngine;

public class MeteorSmash : MonoBehaviour
{
	[Tooltip("Should be a child of this object.")]
	public GameObject m_meteorObject;

	[Tooltip("Should be a child of this object.")]
	public GameObject m_landingEffect;

	[Header("Timing")]
	public AnimationCurve m_speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public AnimationCurve m_scaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public float m_timeToLand = 10f;

	[Header("Spawn Position")]
	public float m_spawnDistance = 500f;

	public float m_spawnAngle = 45f;

	private float m_timer;

	private bool m_crashed;

	private Vector3 m_startPos;

	private Vector3 m_originalScale;

	private void Start()
	{
		Vector3 vector = Vector3.RotateTowards(Vector3.forward, Vector3.up, (float)Math.PI / 180f * m_spawnAngle, 0f);
		vector = (Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f) * vector).normalized * m_spawnDistance;
		m_startPos = base.transform.position + vector;
		m_originalScale = m_meteorObject.transform.localScale;
		m_meteorObject.SetActive(value: true);
		m_landingEffect.SetActive(value: false);
		m_meteorObject.transform.position = Vector3.Lerp(m_startPos, base.transform.position, m_speedCurve.Evaluate(0f));
		m_meteorObject.transform.localScale = Vector3.Lerp(Vector3.zero, m_originalScale, m_scaleCurve.Evaluate(0f));
		m_meteorObject.transform.LookAt(base.transform.position);
	}

	private void Update()
	{
		if (!m_crashed)
		{
			m_timer += Time.deltaTime;
			float time = m_timer / m_timeToLand;
			m_meteorObject.transform.position = Vector3.Lerp(m_startPos, base.transform.position, m_speedCurve.Evaluate(time));
			m_meteorObject.transform.localScale = Vector3.Lerp(Vector3.zero, m_originalScale, m_scaleCurve.Evaluate(time));
			if (!(m_timer < m_timeToLand) || m_crashed)
			{
				m_crashed = true;
				m_landingEffect.SetActive(value: true);
			}
		}
	}
}
