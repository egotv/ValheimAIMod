using System;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : BaseAI
{
	private float m_lastDespawnInDayCheck = -9999f;

	private float m_lastEventCreatureCheck = -9999f;

	public Action<ItemDrop> m_onConsumedItem;

	private const float m_giveUpTime = 30f;

	private const float m_updateTargetFarRange = 50f;

	private const float m_updateTargetIntervalNear = 2f;

	private const float m_updateTargetIntervalFar = 6f;

	private const float m_updateWeaponInterval = 1f;

	private const float m_unableToAttackTargetDuration = 15f;

	[Header("Monster AI")]
	public float m_alertRange = 9999f;

	public bool m_fleeIfHurtWhenTargetCantBeReached = true;

	public float m_fleeUnreachableSinceAttacking = 30f;

	public float m_fleeUnreachableSinceHurt = 20f;

	public bool m_fleeIfNotAlerted;

	public float m_fleeIfLowHealth;

	public float m_fleeTimeSinceHurt = 20f;

	public bool m_fleeInLava = true;

	public bool m_circulateWhileCharging;

	public bool m_circulateWhileChargingFlying;

	public bool m_enableHuntPlayer;

	public bool m_attackPlayerObjects = true;

	public int m_privateAreaTriggerTreshold = 4;

	public float m_interceptTimeMax;

	public float m_interceptTimeMin;

	public float m_maxChaseDistance;

	public float m_minAttackInterval;

	[Header("Circle target")]
	public float m_circleTargetInterval;

	public float m_circleTargetDuration = 5f;

	public float m_circleTargetDistance = 10f;

	[Header("Sleep")]
	public bool m_sleeping;

	public float m_wakeupRange = 5f;

	public bool m_noiseWakeup;

	public float m_maxNoiseWakeupRange = 50f;

	public EffectList m_wakeupEffects = new EffectList();

	public float m_wakeUpDelayMin;

	public float m_wakeUpDelayMax;

	[Header("Other")]
	public bool m_avoidLand;

	[Header("Consume items")]
	public List<ItemDrop> m_consumeItems;

	public float m_consumeRange = 2f;

	public float m_consumeSearchRange = 5f;

	public float m_consumeSearchInterval = 10f;

	private ItemDrop m_consumeTarget;

	private float m_consumeSearchTimer;

	private static int m_itemMask = 0;

	private bool m_despawnInDay;

	private bool m_eventCreature;

	private Character m_targetCreature;

	private Vector3 m_lastKnownTargetPos = Vector3.zero;

	private bool m_beenAtLastPos;

	private StaticTarget m_targetStatic;

	private float m_timeSinceAttacking;

	private float m_timeSinceSensedTargetCreature;

	private float m_updateTargetTimer;

	private float m_updateWeaponTimer;

	private float m_interceptTime;

	private float m_sleepDelay = 0.5f;

	private float m_pauseTimer;

	private float m_sleepTimer;

	private float m_unableToAttackTargetTimer;

	private GameObject m_follow;

	private int m_privateAreaAttacks;

	private static readonly int s_sleeping = ZSyncAnimation.GetHash("sleeping");

	protected override void Awake()
	{
		base.Awake();
		m_despawnInDay = m_nview.GetZDO().GetBool(ZDOVars.s_despawnInDay, m_despawnInDay);
		m_eventCreature = m_nview.GetZDO().GetBool(ZDOVars.s_eventCreature, m_eventCreature);
		m_sleeping = m_nview.GetZDO().GetBool(ZDOVars.s_sleeping, m_sleeping);
		m_animator.SetBool(s_sleeping, IsSleeping());
		m_interceptTime = UnityEngine.Random.Range(m_interceptTimeMin, m_interceptTimeMax);
		m_pauseTimer = UnityEngine.Random.Range(0f, m_circleTargetInterval);
		m_updateTargetTimer = UnityEngine.Random.Range(0f, 2f);
		if (m_wakeUpDelayMin > 0f || m_wakeUpDelayMax > 0f)
		{
			m_sleepDelay = UnityEngine.Random.Range(m_wakeUpDelayMin, m_wakeUpDelayMax);
		}
		if (m_enableHuntPlayer)
		{
			SetHuntPlayer(hunt: true);
		}
		m_nview.Register("RPC_Wakeup", RPC_Wakeup);
	}

	private void Start()
	{
		if ((bool)m_nview && m_nview.IsValid() && m_nview.IsOwner())
		{
			Humanoid humanoid = m_character as Humanoid;
			if ((bool)humanoid)
			{
				humanoid.EquipBestWeapon(null, null, null, null);
			}
		}
	}

	protected override void OnDamaged(float damage, Character attacker)
	{
		base.OnDamaged(damage, attacker);
		Wakeup();
		SetAlerted(alert: true);
		SetTarget(attacker);
	}

	private void SetTarget(Character attacker)
	{
		if (attacker != null && m_targetCreature == null && (!attacker.IsPlayer() || !m_character.IsTamed()))
		{
			m_targetCreature = attacker;
			m_lastKnownTargetPos = attacker.transform.position;
			m_beenAtLastPos = false;
			m_targetStatic = null;
		}
	}

	protected override void RPC_OnNearProjectileHit(long sender, Vector3 center, float range, ZDOID attackerID)
	{
		if (!m_nview.IsOwner() || ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs))
		{
			return;
		}
		SetAlerted(alert: true);
		if (m_fleeIfNotAlerted)
		{
			return;
		}
		GameObject gameObject = ZNetScene.instance.FindInstance(attackerID);
		if (gameObject != null)
		{
			Character component = gameObject.GetComponent<Character>();
			if ((bool)component)
			{
				SetTarget(component);
			}
		}
	}

	public void MakeTame()
	{
		m_character.SetTamed(tamed: true);
		SetAlerted(alert: false);
		m_targetCreature = null;
		m_targetStatic = null;
	}

	private void UpdateTarget(Humanoid humanoid, float dt, out bool canHearTarget, out bool canSeeTarget)
	{
		m_unableToAttackTargetTimer -= dt;
		m_updateTargetTimer -= dt;
		if (m_updateTargetTimer <= 0f && !m_character.InAttack())
		{
			bool flag = Player.IsPlayerInRange(base.transform.position, 50f);
			m_updateTargetTimer = (flag ? 2f : 6f);
			Character character = FindEnemy();
			if ((bool)character)
			{
				m_targetCreature = character;
				m_targetStatic = null;
			}
			bool flag2 = m_targetCreature != null && m_targetCreature.IsPlayer();
			bool flag3 = m_targetCreature != null && m_unableToAttackTargetTimer > 0f && !HavePath(m_targetCreature.transform.position);
			if (m_attackPlayerObjects && (!m_aggravatable || IsAggravated()) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs) && (m_targetCreature == null || flag3) && !m_character.IsTamed())
			{
				StaticTarget staticTarget = FindClosestStaticPriorityTarget();
				if ((bool)staticTarget)
				{
					m_targetStatic = staticTarget;
					m_targetCreature = null;
				}
				bool flag4 = false;
				if (m_targetStatic != null)
				{
					Vector3 target = m_targetStatic.FindClosestPoint(m_character.transform.position);
					flag4 = HavePath(target);
				}
				if ((m_targetStatic == null || !flag4) && IsAlerted() && flag2)
				{
					StaticTarget staticTarget2 = FindRandomStaticTarget(10f);
					if ((bool)staticTarget2)
					{
						m_targetStatic = staticTarget2;
						m_targetCreature = null;
					}
				}
			}
		}
		if ((bool)m_targetCreature && m_character.IsTamed())
		{
			if (GetPatrolPoint(out var point))
			{
				if (Vector3.Distance(m_targetCreature.transform.position, point) > m_alertRange)
				{
					m_targetCreature = null;
				}
			}
			else if ((bool)m_follow && Vector3.Distance(m_targetCreature.transform.position, m_follow.transform.position) > m_alertRange)
			{
				m_targetCreature = null;
			}
		}
		if ((bool)m_targetCreature)
		{
			if (m_targetCreature.IsDead())
			{
				m_targetCreature = null;
			}
			else if (!IsEnemy(m_targetCreature))
			{
				m_targetCreature = null;
			}
			else if (m_skipLavaTargets && m_targetCreature.AboveOrInLava())
			{
				m_targetCreature = null;
			}
		}
		canHearTarget = false;
		canSeeTarget = false;
		if ((bool)m_targetCreature)
		{
			canHearTarget = CanHearTarget(m_targetCreature);
			canSeeTarget = CanSeeTarget(m_targetCreature);
			if (canSeeTarget | canHearTarget)
			{
				m_timeSinceSensedTargetCreature = 0f;
			}
			if (m_targetCreature.IsPlayer())
			{
				m_targetCreature.OnTargeted(canSeeTarget | canHearTarget, IsAlerted());
			}
			SetTargetInfo(m_targetCreature.GetZDOID());
		}
		else
		{
			SetTargetInfo(ZDOID.None);
		}
		m_timeSinceSensedTargetCreature += dt;
		if (IsAlerted() || m_targetCreature != null)
		{
			m_timeSinceAttacking += dt;
			float num = 60f;
			float num2 = Vector3.Distance(m_spawnPoint, base.transform.position);
			bool flag5 = HuntPlayer() && (bool)m_targetCreature && m_targetCreature.IsPlayer();
			if (m_timeSinceSensedTargetCreature > 30f || (!flag5 && (m_timeSinceAttacking > num || (m_maxChaseDistance > 0f && m_timeSinceSensedTargetCreature > 1f && num2 > m_maxChaseDistance))))
			{
				SetAlerted(alert: false);
				m_targetCreature = null;
				m_targetStatic = null;
				m_timeSinceAttacking = 0f;
				m_updateTargetTimer = 5f;
			}
		}
	}

	public override bool UpdateAI(float dt)
	{
		if (!base.UpdateAI(dt))
		{
			return false;
		}
		if (IsSleeping())
		{
			UpdateSleep(dt);
			return true;
		}
		Humanoid humanoid = m_character as Humanoid;
		if (HuntPlayer())
		{
			SetAlerted(alert: true);
		}
		UpdateTarget(humanoid, dt, out var canHearTarget, out var canSeeTarget);
		if ((bool)m_tamable && (bool)m_tamable.m_saddle && m_tamable.m_saddle.UpdateRiding(dt))
		{
			return true;
		}
		if (m_avoidLand && !m_character.IsSwimming())
		{
			MoveToWater(dt, 20f);
			return true;
		}
		if (DespawnInDay() && EnvMan.IsDay() && (m_targetCreature == null || !canSeeTarget))
		{
			MoveAwayAndDespawn(dt, run: true);
			return true;
		}
		if (IsEventCreature() && !RandEventSystem.HaveActiveEvent())
		{
			SetHuntPlayer(hunt: false);
			if (m_targetCreature == null && !IsAlerted())
			{
				MoveAwayAndDespawn(dt, run: false);
				return true;
			}
		}
		if (m_fleeIfNotAlerted && !HuntPlayer() && (bool)m_targetCreature && !IsAlerted() && Vector3.Distance(m_targetCreature.transform.position, base.transform.position) - m_targetCreature.GetRadius() > m_alertRange)
		{
			Flee(dt, m_targetCreature.transform.position);
			return true;
		}
		if (m_fleeIfLowHealth > 0f && m_timeSinceHurt < m_fleeTimeSinceHurt && m_targetCreature != null && m_character.GetHealthPercentage() < m_fleeIfLowHealth)
		{
			Flee(dt, m_targetCreature.transform.position);
			return true;
		}
		if (m_fleeInLava && m_character.InLava() && (m_targetCreature == null || m_targetCreature.AboveOrInLava()))
		{
			Flee(dt, m_character.transform.position - m_character.transform.forward);
			return true;
		}
		if ((m_afraidOfFire || m_avoidFire) && AvoidFire(dt, m_targetCreature, m_afraidOfFire))
		{
			if (m_afraidOfFire)
			{
				m_targetStatic = null;
				m_targetCreature = null;
			}
			return true;
		}
		if (!m_character.IsTamed())
		{
			if (m_targetCreature != null)
			{
				if ((bool)EffectArea.IsPointInsideNoMonsterArea(m_targetCreature.transform.position))
				{
					Flee(dt, m_targetCreature.transform.position);
					return true;
				}
			}
			else
			{
				EffectArea effectArea = EffectArea.IsPointCloseToNoMonsterArea(base.transform.position);
				if (effectArea != null)
				{
					Flee(dt, effectArea.transform.position);
					return true;
				}
			}
		}
		if (m_fleeIfHurtWhenTargetCantBeReached && m_targetCreature != null && m_timeSinceAttacking > 30f && m_timeSinceHurt < 20f)
		{
			Flee(dt, m_targetCreature.transform.position);
			m_lastKnownTargetPos = base.transform.position;
			m_updateTargetTimer = 1f;
			return true;
		}
		if ((!IsAlerted() || (m_targetStatic == null && m_targetCreature == null)) && UpdateConsumeItem(humanoid, dt))
		{
			return true;
		}
		if (m_circleTargetInterval > 0f && (bool)m_targetCreature)
		{
			m_pauseTimer += dt;
			if (m_pauseTimer > m_circleTargetInterval)
			{
				if (m_pauseTimer > m_circleTargetInterval + m_circleTargetDuration)
				{
					m_pauseTimer = UnityEngine.Random.Range(0f, m_circleTargetInterval / 10f);
				}
				RandomMovementArroundPoint(dt, m_targetCreature.transform.position, m_circleTargetDistance, IsAlerted());
				return true;
			}
		}
		ItemDrop.ItemData itemData = SelectBestAttack(humanoid, dt);
		bool flag = itemData != null && Time.time - itemData.m_lastAttackTime > itemData.m_shared.m_aiAttackInterval && m_character.GetTimeSinceLastAttack() >= m_minAttackInterval && !IsTakingOff();
		if (!IsCharging() && (m_targetStatic != null || m_targetCreature != null) && itemData != null && flag && !m_character.InAttack() && itemData.m_shared.m_attack != null && !itemData.m_shared.m_attack.IsDone() && !string.IsNullOrEmpty(itemData.m_shared.m_attack.m_chargeAnimationBool))
		{
			ChargeStart(itemData.m_shared.m_attack.m_chargeAnimationBool);
		}
		if ((m_character.IsFlying() ? m_circulateWhileChargingFlying : m_circulateWhileCharging) && (m_targetStatic != null || m_targetCreature != null) && itemData != null && !flag && !m_character.InAttack())
		{
			Vector3 point = (m_targetCreature ? m_targetCreature.transform.position : m_targetStatic.transform.position);
			RandomMovementArroundPoint(dt, point, m_randomMoveRange, IsAlerted());
			return true;
		}
		if ((m_targetStatic == null && m_targetCreature == null) || itemData == null)
		{
			if ((bool)m_follow)
			{
				Follow(m_follow, dt);
			}
			else
			{
				IdleMovement(dt);
			}
			ChargeStop();
			return true;
		}
		if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
		{
			if ((bool)m_targetStatic)
			{
				Vector3 vector = m_targetStatic.FindClosestPoint(base.transform.position);
				if (Vector3.Distance(vector, base.transform.position) < itemData.m_shared.m_aiAttackRange && CanSeeTarget(m_targetStatic))
				{
					LookAt(m_targetStatic.GetCenter());
					if (itemData.m_shared.m_aiAttackMaxAngle == 0f)
					{
						ZLog.LogError("AI Attack Max Angle for " + itemData.m_shared.m_name + " is 0!");
					}
					if (IsLookingAt(m_targetStatic.GetCenter(), itemData.m_shared.m_aiAttackMaxAngle, itemData.m_shared.m_aiInvertAngleCheck) && flag)
					{
						DoAttack(null, isFriend: false);
					}
					else
					{
						StopMoving();
					}
				}
				else
				{
					MoveTo(dt, vector, 0f, IsAlerted());
					ChargeStop();
				}
			}
			else if ((bool)m_targetCreature)
			{
				if (canHearTarget || canSeeTarget || (HuntPlayer() && m_targetCreature.IsPlayer()))
				{
					m_beenAtLastPos = false;
					m_lastKnownTargetPos = m_targetCreature.transform.position;
					float num = Vector3.Distance(m_lastKnownTargetPos, base.transform.position) - m_targetCreature.GetRadius();
					float num2 = m_alertRange * m_targetCreature.GetStealthFactor();
					if (canSeeTarget && num < num2)
					{
						SetAlerted(alert: true);
					}
					bool num3 = num < itemData.m_shared.m_aiAttackRange;
					if (!num3 || !canSeeTarget || itemData.m_shared.m_aiAttackRangeMin < 0f || !IsAlerted())
					{
						Vector3 velocity = m_targetCreature.GetVelocity();
						Vector3 vector2 = velocity * m_interceptTime;
						Vector3 lastKnownTargetPos = m_lastKnownTargetPos;
						if (num > vector2.magnitude / 4f)
						{
							lastKnownTargetPos += velocity * m_interceptTime;
						}
						MoveTo(dt, lastKnownTargetPos, 0f, IsAlerted());
						if (m_timeSinceAttacking > 15f)
						{
							m_unableToAttackTargetTimer = 15f;
						}
					}
					else
					{
						StopMoving();
					}
					if (num3 && canSeeTarget && IsAlerted())
					{
						LookAt(m_targetCreature.GetTopPoint());
						if (flag && IsLookingAt(m_lastKnownTargetPos, itemData.m_shared.m_aiAttackMaxAngle, itemData.m_shared.m_aiInvertAngleCheck))
						{
							DoAttack(m_targetCreature, isFriend: false);
						}
					}
				}
				else
				{
					ChargeStop();
					if (m_beenAtLastPos)
					{
						RandomMovement(dt, m_lastKnownTargetPos);
						if (m_timeSinceAttacking > 15f)
						{
							m_unableToAttackTargetTimer = 15f;
						}
					}
					else if (MoveTo(dt, m_lastKnownTargetPos, 0f, IsAlerted()))
					{
						m_beenAtLastPos = true;
					}
				}
			}
		}
		else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt || itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Friend)
		{
			Character character = ((itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt) ? HaveHurtFriendInRange(m_viewRange) : HaveFriendInRange(m_viewRange));
			if ((bool)character)
			{
				if (Vector3.Distance(character.transform.position, base.transform.position) < itemData.m_shared.m_aiAttackRange)
				{
					if (flag)
					{
						StopMoving();
						LookAt(character.transform.position);
						DoAttack(character, isFriend: true);
					}
					else
					{
						RandomMovement(dt, character.transform.position);
					}
				}
				else
				{
					MoveTo(dt, character.transform.position, 0f, IsAlerted());
				}
			}
			else
			{
				RandomMovement(dt, base.transform.position, snapToGround: true);
			}
		}
		return true;
	}

	private bool UpdateConsumeItem(Humanoid humanoid, float dt)
	{
		if (m_consumeItems == null || m_consumeItems.Count == 0)
		{
			return false;
		}
		m_consumeSearchTimer += dt;
		if (m_consumeSearchTimer > m_consumeSearchInterval)
		{
			m_consumeSearchTimer = 0f;
			if ((bool)m_tamable && !m_tamable.IsHungry())
			{
				return false;
			}
			m_consumeTarget = FindClosestConsumableItem(m_consumeSearchRange);
		}
		if ((bool)m_consumeTarget)
		{
			if (MoveTo(dt, m_consumeTarget.transform.position, m_consumeRange, run: false))
			{
				LookAt(m_consumeTarget.transform.position);
				if (IsLookingAt(m_consumeTarget.transform.position, 20f) && m_consumeTarget.RemoveOne())
				{
					if (m_onConsumedItem != null)
					{
						m_onConsumedItem(m_consumeTarget);
					}
					humanoid.m_consumeItemEffects.Create(base.transform.position, Quaternion.identity);
					m_animator.SetTrigger("consume");
					m_consumeTarget = null;
				}
			}
			return true;
		}
		return false;
	}

	private ItemDrop FindClosestConsumableItem(float maxRange)
	{
		if (m_itemMask == 0)
		{
			m_itemMask = LayerMask.GetMask("item");
		}
		Collider[] array = Physics.OverlapSphere(base.transform.position, maxRange, m_itemMask);
		ItemDrop itemDrop = null;
		float num = 999999f;
		Collider[] array2 = array;
		foreach (Collider collider in array2)
		{
			if (!collider.attachedRigidbody)
			{
				continue;
			}
			ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
			if (!(component == null) && component.GetComponent<ZNetView>().IsValid() && CanConsume(component.m_itemData))
			{
				float num2 = Vector3.Distance(component.transform.position, base.transform.position);
				if (itemDrop == null || num2 < num)
				{
					itemDrop = component;
					num = num2;
				}
			}
		}
		if ((bool)itemDrop && HavePath(itemDrop.transform.position))
		{
			return itemDrop;
		}
		return null;
	}

	private bool CanConsume(ItemDrop.ItemData item)
	{
		foreach (ItemDrop consumeItem in m_consumeItems)
		{
			if (consumeItem.m_itemData.m_shared.m_name == item.m_shared.m_name)
			{
				return true;
			}
		}
		return false;
	}

	private ItemDrop.ItemData SelectBestAttack(Humanoid humanoid, float dt)
	{
		if ((bool)m_targetCreature || (bool)m_targetStatic)
		{
			m_updateWeaponTimer -= dt;
			if (m_updateWeaponTimer <= 0f && !m_character.InAttack())
			{
				m_updateWeaponTimer = 1f;
				HaveFriendsInRange(m_viewRange, out var hurtFriend, out var friend);
				humanoid.EquipBestWeapon(m_targetCreature, m_targetStatic, hurtFriend, friend);
			}
		}
		return humanoid.GetCurrentWeapon();
	}

	private bool DoAttack(Character target, bool isFriend)
	{
		ItemDrop.ItemData currentWeapon = (m_character as Humanoid).GetCurrentWeapon();
		if (currentWeapon == null)
		{
			return false;
		}
		if (!CanUseAttack(currentWeapon))
		{
			return false;
		}
		bool num = m_character.StartAttack(target, charge: false);
		if (num)
		{
			m_timeSinceAttacking = 0f;
		}
		return num;
	}

	public void SetDespawnInDay(bool despawn)
	{
		m_despawnInDay = despawn;
		m_nview.GetZDO().Set(ZDOVars.s_despawnInDay, despawn);
	}

	public bool DespawnInDay()
	{
		if (Time.time - m_lastDespawnInDayCheck > 4f)
		{
			m_lastDespawnInDayCheck = Time.time;
			m_despawnInDay = m_nview.GetZDO().GetBool(ZDOVars.s_despawnInDay, m_despawnInDay);
		}
		return m_despawnInDay;
	}

	public void SetEventCreature(bool despawn)
	{
		m_eventCreature = despawn;
		m_nview.GetZDO().Set(ZDOVars.s_eventCreature, despawn);
	}

	public bool IsEventCreature()
	{
		if (Time.time - m_lastEventCreatureCheck > 4f)
		{
			m_lastEventCreatureCheck = Time.time;
			m_eventCreature = m_nview.GetZDO().GetBool(ZDOVars.s_eventCreature, m_eventCreature);
		}
		return m_eventCreature;
	}

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
		DrawAILabel();
	}

	private void OnDrawGizmos()
	{
		if (Terminal.m_showTests)
		{
			DrawAILabel();
		}
	}

	private void DrawAILabel()
	{
	}

	public override Character GetTargetCreature()
	{
		return m_targetCreature;
	}

	public StaticTarget GetStaticTarget()
	{
		return m_targetStatic;
	}

	private void UpdateSleep(float dt)
	{
		if (!IsSleeping())
		{
			return;
		}
		m_sleepTimer += dt;
		if (m_sleepTimer < m_sleepDelay)
		{
			return;
		}
		if (HuntPlayer())
		{
			Wakeup();
			return;
		}
		if (m_wakeupRange > 0f)
		{
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, m_wakeupRange);
			if ((bool)closestPlayer && !closestPlayer.InGhostMode() && !closestPlayer.IsDebugFlying())
			{
				Wakeup();
				return;
			}
		}
		if (m_noiseWakeup)
		{
			Player playerNoiseRange = Player.GetPlayerNoiseRange(base.transform.position, m_maxNoiseWakeupRange);
			if ((bool)playerNoiseRange && !playerNoiseRange.InGhostMode() && !playerNoiseRange.IsDebugFlying())
			{
				Wakeup();
			}
		}
	}

	public void OnPrivateAreaAttacked(Character attacker, bool destroyed)
	{
		if (attacker.IsPlayer() && IsAggravatable() && !IsAggravated())
		{
			m_privateAreaAttacks++;
			if (m_privateAreaAttacks > m_privateAreaTriggerTreshold || destroyed)
			{
				SetAggravated(aggro: true, AggravatedReason.Damage);
			}
		}
	}

	private void RPC_Wakeup(long sender)
	{
		if (!m_nview.GetZDO().IsOwner())
		{
			m_sleeping = false;
		}
	}

	private void Wakeup()
	{
		if (IsSleeping())
		{
			m_animator.SetBool(s_sleeping, value: false);
			m_nview.GetZDO().Set(ZDOVars.s_sleeping, value: false);
			m_wakeupEffects.Create(base.transform.position, base.transform.rotation);
			m_sleeping = false;
			m_nview.InvokeRPC(ZNetView.Everybody, "RPC_Wakeup");
		}
	}

	public override bool IsSleeping()
	{
		return m_sleeping;
	}

	protected override void SetAlerted(bool alert)
	{
		if (alert)
		{
			m_timeSinceSensedTargetCreature = 0f;
		}
		base.SetAlerted(alert);
	}

	public override bool HuntPlayer()
	{
		if (base.HuntPlayer())
		{
			if (IsEventCreature() && !RandEventSystem.InEvent())
			{
				return false;
			}
			if (DespawnInDay() && EnvMan.IsDay())
			{
				return false;
			}
			return true;
		}
		return false;
	}

	public GameObject GetFollowTarget()
	{
		return m_follow;
	}

	public void SetFollowTarget(GameObject go)
	{
		m_follow = go;
	}
}
