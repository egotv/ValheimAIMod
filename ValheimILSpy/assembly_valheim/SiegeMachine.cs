using System;
using System.Collections.Generic;
using Dynamics;
using UnityEngine;

public class SiegeMachine : MonoBehaviour
{
	[Serializable]
	public class SiegePart
	{
		public GameObject m_gameobject;

		public Transform m_effectPoint;

		[NonSerialized]
		public float m_position;

		[NonSerialized]
		public FloatDynamics m_floatDynamics;

		[NonSerialized]
		public float m_dynamicsPosition;

		[NonSerialized]
		public Vector3 m_originalPosition;
	}

	private enum AnimPhase
	{
		Charging,
		Firing
	}

	public Smelter m_engine;

	public Vagon m_wagon;

	public bool m_enabledWhenAttached = true;

	public List<SiegePart> m_movingParts = new List<SiegePart>();

	public DynamicsParameters m_dynamicsParameters;

	private ZNetView m_nview;

	private bool m_wasDisabledLastUpdate = true;

	public float m_chargeTime = 4f;

	public float m_hitDelay = 2f;

	public float m_chargeOffsetDistance = 2f;

	public AnimationCurve m_armAnimCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public GameObject m_aoe;

	public EffectList m_punchEffect;

	public EffectList m_chargeEffect;

	private int m_currentPart;

	private float m_firingTimer;

	private float m_aoeActiveTimer;

	private AnimPhase m_animPhase;

	private void Awake()
	{
		foreach (SiegePart movingPart in m_movingParts)
		{
			movingPart.m_position = 0f;
			movingPart.m_originalPosition = movingPart.m_gameobject.transform.localPosition;
			movingPart.m_dynamicsPosition = 0f;
			movingPart.m_floatDynamics = new FloatDynamics(m_dynamicsParameters, movingPart.m_dynamicsPosition);
		}
		m_aoe.gameObject.SetActive(value: false);
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() == null)
		{
			base.enabled = false;
		}
	}

	private void Update()
	{
		if (m_nview.IsValid())
		{
			UpdateSiege(Time.deltaTime);
		}
	}

	private void UpdateAnimPhase()
	{
		m_currentPart = (m_currentPart + 1) % m_movingParts.Count;
		if (m_currentPart == 0)
		{
			m_animPhase = ((m_animPhase == AnimPhase.Charging) ? AnimPhase.Firing : AnimPhase.Charging);
		}
	}

	private void UpdateSiege(float dt)
	{
		bool flag = (object)m_nview != null && m_nview.IsValid() && m_nview.IsOwner();
		foreach (SiegePart movingPart in m_movingParts)
		{
			movingPart.m_dynamicsPosition = movingPart.m_floatDynamics.Update(dt, m_armAnimCurve.Evaluate(movingPart.m_position / m_chargeTime));
			movingPart.m_gameobject.transform.localPosition = Vector3.Lerp(movingPart.m_originalPosition, movingPart.m_originalPosition + Vector3.back * m_chargeOffsetDistance, movingPart.m_dynamicsPosition);
		}
		if (((bool)m_engine && !m_engine.IsActive()) || ((bool)m_wagon && (m_enabledWhenAttached ^ m_wagon.IsAttached())))
		{
			m_aoe.gameObject.SetActive(value: false);
			m_animPhase = AnimPhase.Charging;
			m_currentPart = 0;
			foreach (SiegePart movingPart2 in m_movingParts)
			{
				movingPart2.m_position = Mathf.MoveTowards(movingPart2.m_position, 0f, dt / 0.5f);
				if ((double)movingPart2.m_position < 0.02)
				{
					movingPart2.m_position = 0f;
				}
			}
			m_wasDisabledLastUpdate = true;
			return;
		}
		if (m_wasDisabledLastUpdate)
		{
			foreach (SiegePart movingPart3 in m_movingParts)
			{
				movingPart3.m_position = 0f;
			}
		}
		if (flag)
		{
			if (m_aoeActiveTimer > 0f)
			{
				m_aoeActiveTimer -= dt;
			}
			m_aoe.gameObject.SetActive(m_aoeActiveTimer >= 0f);
		}
		SiegePart siegePart = m_movingParts[m_currentPart];
		AnimPhase animPhase = m_animPhase;
		if (animPhase == AnimPhase.Charging || animPhase != AnimPhase.Firing)
		{
			if (siegePart.m_position == 0f && flag)
			{
				m_chargeEffect.Create(siegePart.m_gameobject.transform.position, Quaternion.identity);
			}
			siegePart.m_position += dt;
			if (siegePart.m_position >= m_chargeTime)
			{
				UpdateAnimPhase();
			}
		}
		else
		{
			m_firingTimer += dt;
			if (m_firingTimer > m_hitDelay)
			{
				m_firingTimer = 0f;
				Terminal.Log("Firing!");
				siegePart.m_position = 0f;
				if (flag)
				{
					m_punchEffect.Create(siegePart.m_effectPoint.position, siegePart.m_effectPoint.rotation);
					m_aoeActiveTimer = 0.05f;
					m_aoe.gameObject.SetActive(value: true);
				}
				UpdateAnimPhase();
			}
		}
		m_wasDisabledLastUpdate = false;
	}
}
