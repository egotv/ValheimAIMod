using UnityEngine;

public class AnimalAI : BaseAI
{
	private const float m_updateTargetFarRange = 32f;

	private const float m_updateTargetIntervalNear = 2f;

	private const float m_updateTargetIntervalFar = 10f;

	public float m_timeToSafe = 4f;

	private Character m_target;

	private float m_inDangerTimer;

	private float m_updateTargetTimer;

	protected override void Awake()
	{
		base.Awake();
		m_updateTargetTimer = Random.Range(0f, 2f);
	}

	protected override void OnDamaged(float damage, Character attacker)
	{
		base.OnDamaged(damage, attacker);
		SetAlerted(alert: true);
	}

	public override bool UpdateAI(float dt)
	{
		if (!base.UpdateAI(dt))
		{
			return false;
		}
		if (m_afraidOfFire && AvoidFire(dt, null, superAfraid: true))
		{
			return true;
		}
		m_updateTargetTimer -= dt;
		if (m_updateTargetTimer <= 0f)
		{
			m_updateTargetTimer = (Character.IsCharacterInRange(base.transform.position, 32f) ? 2f : 10f);
			Character character = FindEnemy();
			if ((bool)character)
			{
				m_target = character;
			}
		}
		if ((bool)m_target && m_target.IsDead())
		{
			m_target = null;
		}
		if ((bool)m_target)
		{
			bool num = CanSenseTarget(m_target);
			SetTargetInfo(m_target.GetZDOID());
			if (num)
			{
				SetAlerted(alert: true);
			}
		}
		else
		{
			SetTargetInfo(ZDOID.None);
		}
		if (IsAlerted())
		{
			m_inDangerTimer += dt;
			if (m_inDangerTimer > m_timeToSafe)
			{
				m_target = null;
				SetAlerted(alert: false);
			}
		}
		if ((bool)m_target)
		{
			Flee(dt, m_target.transform.position);
			m_target.OnTargeted(sensed: false, alerted: false);
		}
		else
		{
			IdleMovement(dt);
		}
		return true;
	}

	protected override void SetAlerted(bool alert)
	{
		if (alert)
		{
			m_inDangerTimer = 0f;
		}
		base.SetAlerted(alert);
	}
}
