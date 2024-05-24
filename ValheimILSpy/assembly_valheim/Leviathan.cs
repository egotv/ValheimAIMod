using System;
using UnityEngine;

public class Leviathan : MonoBehaviour
{
	public float m_waveScale = 0.5f;

	public float m_floatOffset;

	public float m_movementSpeed = 0.1f;

	public float m_maxSpeed = 1f;

	public MineRock m_mineRock;

	public float m_hitReactionChance = 0.25f;

	public int m_leaveDelay = 5;

	public EffectList m_reactionEffects = new EffectList();

	public EffectList m_leaveEffects = new EffectList();

	public bool m_alignToWaterLevel = true;

	private Rigidbody m_body;

	private ZNetView m_nview;

	private ZSyncAnimation m_zanimator;

	private Animator m_animator;

	private bool m_left;

	private void Awake()
	{
		m_body = GetComponent<Rigidbody>();
		m_nview = GetComponent<ZNetView>();
		m_zanimator = GetComponent<ZSyncAnimation>();
		m_animator = GetComponentInChildren<Animator>();
		if ((bool)GetComponent<MineRock>())
		{
			MineRock mineRock = m_mineRock;
			mineRock.m_onHit = (Action)Delegate.Combine(mineRock.m_onHit, new Action(OnHit));
		}
		if (m_nview.IsValid() && m_nview.IsOwner() && m_nview.GetZDO().GetBool(ZDOVars.s_dead))
		{
			m_nview.Destroy();
		}
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		float liquidLevel = Floating.GetLiquidLevel(base.transform.position, m_waveScale);
		if (m_alignToWaterLevel)
		{
			if (liquidLevel > -100f)
			{
				Vector3 position = m_body.position;
				float num = Mathf.Clamp((liquidLevel - (position.y + m_floatOffset)) * m_movementSpeed * Time.fixedDeltaTime, 0f - m_maxSpeed, m_maxSpeed);
				position.y += num;
				m_body.MovePosition(position);
			}
			else
			{
				Vector3 position2 = m_body.position;
				position2.y = 0f;
				m_body.MovePosition(Vector3.MoveTowards(m_body.position, position2, Time.deltaTime));
			}
		}
		if (m_animator.GetCurrentAnimatorStateInfo(0).IsTag("submerged"))
		{
			m_nview.Destroy();
		}
	}

	private void OnHit()
	{
		if (UnityEngine.Random.value <= m_hitReactionChance && !m_left)
		{
			m_reactionEffects.Create(base.transform.position, base.transform.rotation);
			m_zanimator.SetTrigger("shake");
			Invoke("Leave", m_leaveDelay);
		}
	}

	private void Leave()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && !m_left)
		{
			m_left = true;
			m_leaveEffects.Create(base.transform.position, base.transform.rotation);
			m_zanimator.SetTrigger("dive");
			m_nview.GetZDO().Set(ZDOVars.s_dead, value: true);
		}
	}

	private void OnDestroy()
	{
		if (m_left && m_nview.IsValid() && !m_nview.IsOwner() && Player.GetPlayersInRangeXZ(base.transform.position, 40f) == 0)
		{
			m_nview.Destroy();
		}
	}
}
