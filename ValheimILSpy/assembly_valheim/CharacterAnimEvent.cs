using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimEvent : MonoBehaviour, IMonoUpdater
{
	[Serializable]
	public class Foot
	{
		public Transform m_transform;

		public AvatarIKGoal m_ikHandle;

		public float m_footDownMax = 0.4f;

		public float m_footOffset = 0.1f;

		public float m_footStepHeight = 1f;

		public float m_stabalizeDistance;

		[NonSerialized]
		public float m_ikWeight;

		[NonSerialized]
		public Vector3 m_plantPosition = Vector3.zero;

		[NonSerialized]
		public Vector3 m_plantNormal = Vector3.up;

		[NonSerialized]
		public bool m_isPlanted;

		public Foot(Transform t, AvatarIKGoal handle)
		{
			m_transform = t;
			m_ikHandle = handle;
			m_ikWeight = 0f;
		}
	}

	[Header("Foot IK")]
	public bool m_footIK;

	public float m_footDownMax = 0.4f;

	public float m_footOffset = 0.1f;

	public float m_footStepHeight = 1f;

	public float m_stabalizeDistance;

	public bool m_useFeetValues;

	public Foot[] m_feets = Array.Empty<Foot>();

	[Header("Head/eye rotation")]
	public bool m_headRotation = true;

	public Transform[] m_eyes;

	public float m_lookWeight = 0.5f;

	public float m_bodyLookWeight = 0.1f;

	public float m_headLookWeight = 1f;

	public float m_eyeLookWeight;

	public float m_lookClamp = 0.5f;

	private const float m_headRotationSmoothness = 0.1f;

	public Transform m_lookAt;

	[Header("Player Female hack")]
	public bool m_femaleHack;

	public Transform m_leftShoulder;

	public Transform m_rightShoulder;

	public float m_femaleOffset = 0.0004f;

	public float m_maleOffset = 0.0007651657f;

	private Character m_character;

	private Animator m_animator;

	private ZNetView m_nview;

	private MonsterAI m_monsterAI;

	private VisEquipment m_visEquipment;

	private FootStep m_footStep;

	private float m_pauseTimer;

	private float m_pauseSpeed = 1f;

	private float m_sendTimer;

	private Vector3 m_headLookDir;

	private float m_lookAtWeight;

	private Transform m_head;

	private bool m_chain;

	private static int s_ikGroundMask = 0;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		m_character = GetComponentInParent<Character>();
		m_nview = m_character.GetComponent<ZNetView>();
		m_animator = GetComponent<Animator>();
		m_monsterAI = m_character.GetComponent<MonsterAI>();
		m_visEquipment = m_character.GetComponent<VisEquipment>();
		m_footStep = m_character.GetComponent<FootStep>();
		m_head = Utils.GetBoneTransform(m_animator, HumanBodyBones.Head);
		m_headLookDir = m_character.transform.forward;
		if (s_ikGroundMask == 0)
		{
			s_ikGroundMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
		}
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	private void OnAnimatorMove()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			m_character.AddRootMotion(m_animator.deltaPosition);
		}
	}

	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (!(m_character == null) && m_nview.IsValid())
		{
			if (!m_character.InAttack() && !m_character.InMinorAction() && !m_character.InEmote() && m_character.CanMove())
			{
				m_animator.speed = 1f;
			}
			UpdateFreezeFrame(fixedDeltaTime);
		}
	}

	public bool CanChain()
	{
		return m_chain;
	}

	public void FreezeFrame(float delay)
	{
		if (delay <= 0f)
		{
			return;
		}
		if (m_pauseTimer > 0f)
		{
			m_pauseTimer = delay;
			return;
		}
		m_pauseTimer = delay;
		m_pauseSpeed = m_animator.speed;
		m_animator.speed = 0.0001f;
		if (m_pauseSpeed <= 0.01f)
		{
			m_pauseSpeed = 1f;
		}
	}

	private void UpdateFreezeFrame(float dt)
	{
		if (m_pauseTimer > 0f)
		{
			m_pauseTimer -= dt;
			if (m_pauseTimer <= 0f)
			{
				m_animator.speed = m_pauseSpeed;
			}
		}
		if (m_animator.speed < 0.01f && m_pauseTimer <= 0f)
		{
			m_animator.speed = 1f;
		}
	}

	public void Speed(float speedScale)
	{
		m_animator.speed = speedScale;
	}

	public void Chain()
	{
		m_chain = true;
	}

	public void ResetChain()
	{
		m_chain = false;
	}

	public void FootStep(AnimationEvent e)
	{
		if (!((double)e.animatorClipInfo.weight < 0.33) && (bool)m_footStep)
		{
			if (e.stringParameter.Length > 0)
			{
				m_footStep.OnFoot(e.stringParameter);
			}
			else
			{
				m_footStep.OnFoot();
			}
		}
	}

	public void Hit()
	{
		m_character.OnAttackTrigger();
	}

	public void OnAttackTrigger()
	{
		m_character.OnAttackTrigger();
	}

	public void Jump()
	{
		m_character.Jump(force: true);
	}

	public void Land()
	{
		if (m_character.IsFlying())
		{
			m_character.Land();
		}
	}

	public void TakeOff()
	{
		if (!m_character.IsFlying())
		{
			m_character.TakeOff();
		}
	}

	public void Stop(AnimationEvent e)
	{
		m_character.OnStopMoving();
	}

	public void DodgeMortal()
	{
		Player player = m_character as Player;
		if ((bool)player)
		{
			player.OnDodgeMortal();
		}
	}

	public void TrailOn()
	{
		if ((bool)m_visEquipment)
		{
			m_visEquipment.SetWeaponTrails(enabled: true);
		}
		m_character.OnWeaponTrailStart();
	}

	public void TrailOff()
	{
		if ((bool)m_visEquipment)
		{
			m_visEquipment.SetWeaponTrails(enabled: false);
		}
	}

	public void GPower()
	{
		Player player = m_character as Player;
		if ((bool)player)
		{
			player.ActivateGuardianPower();
		}
	}

	private void OnAnimatorIK(int layerIndex)
	{
		if (m_nview.IsValid())
		{
			UpdateLookat();
			UpdateFootIK();
		}
	}

	public void CustomLateUpdate(float deltaTime)
	{
		UpdateHeadRotation(deltaTime);
		if (m_femaleHack)
		{
			_ = m_character;
			float num = ((m_visEquipment.GetModelIndex() == 1) ? m_femaleOffset : m_maleOffset);
			Vector3 localPosition = m_leftShoulder.localPosition;
			localPosition.x = 0f - num;
			m_leftShoulder.localPosition = localPosition;
			Vector3 localPosition2 = m_rightShoulder.localPosition;
			localPosition2.x = num;
			m_rightShoulder.localPosition = localPosition2;
		}
	}

	private void UpdateLookat()
	{
		if (m_headRotation && (bool)m_head)
		{
			float target = m_lookWeight;
			if (m_headLookDir != Vector3.zero)
			{
				m_animator.SetLookAtPosition(m_head.position + m_headLookDir * 10f);
			}
			if (m_character.InAttack() || (!m_character.IsPlayer() && !m_character.CanMove()))
			{
				target = 0f;
			}
			m_lookAtWeight = Mathf.MoveTowards(m_lookAtWeight, target, Time.deltaTime);
			float bodyWeight = (m_character.IsAttached() ? 0f : m_bodyLookWeight);
			m_animator.SetLookAtWeight(m_lookAtWeight, bodyWeight, m_headLookWeight, m_eyeLookWeight, m_lookClamp);
		}
	}

	private void UpdateFootIK()
	{
		if (!m_footIK)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null || Vector3.Distance(base.transform.position, mainCamera.transform.position) > 64f)
		{
			return;
		}
		if ((m_character.IsFlying() && !m_character.IsOnGround()) || (m_character.IsSwimming() && !m_character.IsOnGround()) || m_character.IsSitting())
		{
			for (int i = 0; i < m_feets.Length; i++)
			{
				Foot foot = m_feets[i];
				m_animator.SetIKPositionWeight(foot.m_ikHandle, 0f);
				m_animator.SetIKRotationWeight(foot.m_ikHandle, 0f);
			}
			return;
		}
		bool flag = m_character.IsSitting();
		float deltaTime = Time.deltaTime;
		for (int j = 0; j < m_feets.Length; j++)
		{
			Foot foot2 = m_feets[j];
			Vector3 position = foot2.m_transform.position;
			AvatarIKGoal ikHandle = foot2.m_ikHandle;
			float num = (m_useFeetValues ? foot2.m_footDownMax : m_footDownMax);
			float num2 = (m_useFeetValues ? foot2.m_footOffset : m_footOffset);
			float num3 = (m_useFeetValues ? foot2.m_footStepHeight : m_footStepHeight);
			float num4 = (m_useFeetValues ? foot2.m_stabalizeDistance : m_stabalizeDistance);
			if (flag)
			{
				num3 /= 4f;
			}
			float target = 1f - Mathf.Clamp01(base.transform.InverseTransformPoint(position - base.transform.up * num2).y / num);
			foot2.m_ikWeight = Mathf.MoveTowards(foot2.m_ikWeight, target, deltaTime * 10f);
			m_animator.SetIKPositionWeight(ikHandle, foot2.m_ikWeight);
			m_animator.SetIKRotationWeight(ikHandle, foot2.m_ikWeight * 0.5f);
			if (!(foot2.m_ikWeight > 0f))
			{
				continue;
			}
			if (Physics.Raycast(position + Vector3.up * num3, Vector3.down, out var hitInfo, num3 * 4f, s_ikGroundMask))
			{
				Vector3 vector = hitInfo.point + Vector3.up * num2;
				Vector3 normal = hitInfo.normal;
				if (num4 > 0f)
				{
					if (foot2.m_ikWeight >= 1f)
					{
						if (!foot2.m_isPlanted)
						{
							foot2.m_plantPosition = vector;
							foot2.m_plantNormal = normal;
							foot2.m_isPlanted = true;
						}
						else if (Vector3.Distance(foot2.m_plantPosition, vector) > num4)
						{
							foot2.m_isPlanted = false;
						}
						else
						{
							vector = foot2.m_plantPosition;
							normal = foot2.m_plantNormal;
						}
					}
					else
					{
						foot2.m_isPlanted = false;
					}
				}
				m_animator.SetIKPosition(ikHandle, vector);
				Quaternion goalRotation = Quaternion.LookRotation(Vector3.Cross(m_animator.GetIKRotation(ikHandle) * Vector3.right, hitInfo.normal), hitInfo.normal);
				m_animator.SetIKRotation(ikHandle, goalRotation);
			}
			else
			{
				foot2.m_ikWeight = Mathf.MoveTowards(foot2.m_ikWeight, 0f, deltaTime * 4f);
				m_animator.SetIKPositionWeight(ikHandle, foot2.m_ikWeight);
				m_animator.SetIKRotationWeight(ikHandle, foot2.m_ikWeight * 0.5f);
			}
		}
	}

	private void UpdateHeadRotation(float dt)
	{
		if (m_nview == null || !m_nview.IsValid() || !m_headRotation || !m_head)
		{
			return;
		}
		Vector3 lookFromPos = GetLookFromPos();
		Vector3 vector = Vector3.zero;
		if (m_nview.IsOwner())
		{
			if (m_monsterAI != null)
			{
				Character targetCreature = m_monsterAI.GetTargetCreature();
				if (targetCreature != null)
				{
					vector = targetCreature.GetEyePoint();
				}
			}
			else
			{
				vector = lookFromPos + m_character.GetLookDir() * 100f;
			}
			if (m_lookAt != null)
			{
				vector = m_lookAt.position;
			}
			m_sendTimer += Time.deltaTime;
			if (m_sendTimer > 0.2f)
			{
				m_sendTimer = 0f;
				m_nview.GetZDO().Set(ZDOVars.s_lookTarget, vector);
			}
		}
		else
		{
			vector = m_nview.GetZDO().GetVec3(ZDOVars.s_lookTarget, Vector3.zero);
		}
		if (vector != Vector3.zero)
		{
			Vector3 b = Vector3.Normalize(vector - lookFromPos);
			m_headLookDir = Vector3.Lerp(m_headLookDir, b, 0.1f);
		}
		else
		{
			m_headLookDir = m_character.transform.forward;
		}
	}

	private Vector3 GetLookFromPos()
	{
		if (m_eyes != null && m_eyes.Length != 0)
		{
			Vector3 zero = Vector3.zero;
			Transform[] eyes = m_eyes;
			foreach (Transform transform in eyes)
			{
				zero += transform.position;
			}
			return zero / m_eyes.Length;
		}
		return m_head.position;
	}

	public void FindJoints()
	{
		ZLog.Log("Finding joints");
		List<Transform> list = new List<Transform>();
		Transform transform = Utils.FindChild(base.transform, "LeftEye");
		Transform transform2 = Utils.FindChild(base.transform, "RightEye");
		if ((bool)transform)
		{
			list.Add(transform);
		}
		if ((bool)transform2)
		{
			list.Add(transform2);
		}
		m_eyes = list.ToArray();
		Transform transform3 = Utils.FindChild(base.transform, "LeftFootFront");
		Transform transform4 = Utils.FindChild(base.transform, "RightFootFront");
		Transform transform5 = Utils.FindChild(base.transform, "LeftFoot");
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "LeftFootBack");
		}
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "l_foot");
		}
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "Foot.l");
		}
		if (transform5 == null)
		{
			transform5 = Utils.FindChild(base.transform, "foot.l");
		}
		Transform transform6 = Utils.FindChild(base.transform, "RightFoot");
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "RightFootBack");
		}
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "r_foot");
		}
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "Foot.r");
		}
		if (transform6 == null)
		{
			transform6 = Utils.FindChild(base.transform, "foot.r");
		}
		List<Foot> list2 = new List<Foot>();
		if ((bool)transform3)
		{
			list2.Add(new Foot(transform3, AvatarIKGoal.LeftHand));
		}
		if ((bool)transform4)
		{
			list2.Add(new Foot(transform4, AvatarIKGoal.RightHand));
		}
		if ((bool)transform5)
		{
			list2.Add(new Foot(transform5, AvatarIKGoal.LeftFoot));
		}
		if ((bool)transform6)
		{
			list2.Add(new Foot(transform6, AvatarIKGoal.RightFoot));
		}
		m_feets = list2.ToArray();
	}

	private void OnDrawGizmosSelected()
	{
		if (!m_footIK)
		{
			return;
		}
		Foot[] feets = m_feets;
		foreach (Foot foot in feets)
		{
			float num = (m_useFeetValues ? foot.m_footDownMax : m_footDownMax);
			float num2 = (m_useFeetValues ? foot.m_footOffset : m_footOffset);
			float num3 = (m_useFeetValues ? foot.m_footStepHeight : m_footStepHeight);
			float num4 = (m_useFeetValues ? foot.m_stabalizeDistance : m_stabalizeDistance);
			Vector3 vector = foot.m_transform.position - base.transform.up * num2;
			Gizmos.color = ((vector.y > base.transform.position.y) ? Color.red : Color.white);
			Gizmos.DrawWireSphere(vector, 0.1f);
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireCube(new Vector3(vector.x, base.transform.position.y, vector.z) + Vector3.up * num, new Vector3(1f, 0.01f, 1f));
			Gizmos.color = Color.red;
			Gizmos.DrawLine(vector, vector + Vector3.up * num3);
			if (num4 > 0f)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawWireSphere(vector, num4);
				Gizmos.matrix = Matrix4x4.identity;
			}
			if (foot.m_isPlanted)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawWireCube(vector, new Vector3(0.4f, 0.3f, 0.4f));
			}
		}
	}
}
