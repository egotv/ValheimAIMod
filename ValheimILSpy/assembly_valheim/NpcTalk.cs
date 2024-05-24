using System;
using System.Collections.Generic;
using UnityEngine;

public class NpcTalk : MonoBehaviour
{
	private class QueuedSay
	{
		public string text;

		public string trigger;

		public EffectList m_effect;
	}

	private float m_lastTargetUpdate;

	public string m_name = "Haldor";

	public float m_maxRange = 15f;

	public float m_greetRange = 10f;

	public float m_byeRange = 15f;

	public float m_offset = 2f;

	public float m_minTalkInterval = 1.5f;

	private const int m_maxQueuedTexts = 3;

	public float m_hideDialogDelay = 5f;

	public float m_randomTalkInterval = 10f;

	public float m_randomTalkChance = 1f;

	public List<string> m_randomTalk = new List<string>();

	public List<string> m_randomTalkInFactionBase = new List<string>();

	public List<string> m_randomGreets = new List<string>();

	public List<string> m_randomGoodbye = new List<string>();

	public List<string> m_privateAreaAlarm = new List<string>();

	public List<string> m_aggravated = new List<string>();

	public EffectList m_randomTalkFX = new EffectList();

	public EffectList m_randomGreetFX = new EffectList();

	public EffectList m_randomGoodbyeFX = new EffectList();

	private bool m_didGreet;

	private bool m_didGoodbye;

	private MonsterAI m_monsterAI;

	private Animator m_animator;

	private Character m_character;

	private ZNetView m_nview;

	private Player m_targetPlayer;

	private bool m_seeTarget;

	private bool m_hearTarget;

	private Queue<QueuedSay> m_queuedTexts = new Queue<QueuedSay>();

	private static float m_lastTalkTime;

	private void Start()
	{
		m_character = GetComponentInChildren<Character>();
		m_monsterAI = GetComponent<MonsterAI>();
		m_animator = GetComponentInChildren<Animator>();
		m_nview = GetComponent<ZNetView>();
		MonsterAI monsterAI = m_monsterAI;
		monsterAI.m_onBecameAggravated = (Action<BaseAI.AggravatedReason>)Delegate.Combine(monsterAI.m_onBecameAggravated, new Action<BaseAI.AggravatedReason>(OnBecameAggravated));
		InvokeRepeating("RandomTalk", UnityEngine.Random.Range(m_randomTalkInterval / 5f, m_randomTalkInterval), m_randomTalkInterval);
	}

	private void Update()
	{
		if (m_monsterAI.GetTargetCreature() != null || m_monsterAI.GetStaticTarget() != null || !m_nview.IsValid())
		{
			return;
		}
		UpdateTarget();
		if ((bool)m_targetPlayer)
		{
			if (m_nview.IsOwner() && m_character.GetVelocity().magnitude < 0.5f)
			{
				Vector3 normalized = (m_targetPlayer.GetEyePoint() - m_character.GetEyePoint()).normalized;
				m_character.SetLookDir(normalized);
			}
			if (m_seeTarget)
			{
				float num = Vector3.Distance(m_targetPlayer.transform.position, base.transform.position);
				if (!m_didGreet && num < m_greetRange)
				{
					m_didGreet = true;
					QueueSay(m_randomGreets, "Greet", m_randomGreetFX);
				}
				if (m_didGreet && !m_didGoodbye && num > m_byeRange)
				{
					m_didGoodbye = true;
					QueueSay(m_randomGoodbye, "Greet", m_randomGoodbyeFX);
				}
			}
		}
		UpdateSayQueue();
	}

	private void UpdateTarget()
	{
		if (!(Time.time - m_lastTargetUpdate > 1f))
		{
			return;
		}
		m_lastTargetUpdate = Time.time;
		m_targetPlayer = null;
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, m_maxRange);
		if (!(closestPlayer == null) && !m_monsterAI.IsEnemy(closestPlayer))
		{
			m_seeTarget = m_monsterAI.CanSeeTarget(closestPlayer);
			m_hearTarget = m_monsterAI.CanHearTarget(closestPlayer);
			if (m_seeTarget || m_hearTarget)
			{
				m_targetPlayer = closestPlayer;
			}
		}
	}

	private void OnBecameAggravated(BaseAI.AggravatedReason reason)
	{
		QueueSay(m_aggravated, "Aggravated", null);
	}

	public void OnPrivateAreaAttacked(Character attacker)
	{
		if (attacker.IsPlayer() && m_monsterAI.IsAggravatable() && !m_monsterAI.IsAggravated() && Vector3.Distance(base.transform.position, attacker.transform.position) < m_maxRange)
		{
			QueueSay(m_privateAreaAlarm, "Angry", null);
		}
	}

	private void RandomTalk()
	{
		if (!(Time.time - m_lastTalkTime < m_minTalkInterval) && !(UnityEngine.Random.Range(0f, 1f) > m_randomTalkChance))
		{
			UpdateTarget();
			if ((bool)m_targetPlayer && m_seeTarget)
			{
				List<string> texts = (InFactionBase() ? m_randomTalkInFactionBase : m_randomTalk);
				QueueSay(texts, "Talk", m_randomTalkFX);
			}
		}
	}

	private void QueueSay(List<string> texts, string trigger, EffectList effect)
	{
		if (texts.Count != 0 && m_queuedTexts.Count < 3)
		{
			QueuedSay queuedSay = new QueuedSay();
			queuedSay.text = texts[UnityEngine.Random.Range(0, texts.Count)];
			queuedSay.trigger = trigger;
			queuedSay.m_effect = effect;
			m_queuedTexts.Enqueue(queuedSay);
		}
	}

	private void UpdateSayQueue()
	{
		if (m_queuedTexts.Count != 0 && !(Time.time - m_lastTalkTime < m_minTalkInterval))
		{
			QueuedSay queuedSay = m_queuedTexts.Dequeue();
			Say(queuedSay.text, queuedSay.trigger);
			if (queuedSay.m_effect != null)
			{
				queuedSay.m_effect.Create(base.transform.position, Quaternion.identity);
			}
		}
	}

	private void Say(string text, string trigger)
	{
		m_lastTalkTime = Time.time;
		Chat.instance.SetNpcText(base.gameObject, Vector3.up * m_offset, 20f, m_hideDialogDelay, "", text, large: false);
		if (trigger.Length > 0)
		{
			m_animator.SetTrigger(trigger);
		}
	}

	private bool InFactionBase()
	{
		return PrivateArea.InsideFactionArea(base.transform.position, m_character.GetFaction());
	}
}
