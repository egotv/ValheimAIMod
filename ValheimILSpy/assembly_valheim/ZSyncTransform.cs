using System.Collections.Generic;
using UnityEngine;

public class ZSyncTransform : MonoBehaviour, IMonoUpdater
{
	public bool m_syncPosition = true;

	public bool m_syncRotation = true;

	public bool m_syncScale;

	public bool m_syncBodyVelocity;

	public bool m_characterParentSync;

	private const float m_smoothnessPos = 0.2f;

	private const float m_smoothnessRot = 0.5f;

	private bool m_isKinematicBody;

	private bool m_useGravity = true;

	private Vector3 m_tempRelPos;

	private bool m_haveTempRelPos;

	private float m_targetPosTimer;

	private uint m_posRevision = uint.MaxValue;

	private int m_lastUpdateFrame = -1;

	private bool m_wasOwner;

	private ZNetView m_nview;

	private Rigidbody m_body;

	private Projectile m_projectile;

	private Character m_character;

	private ZDOID m_tempParent;

	private string m_tempAttachJoint;

	private Vector3 m_tempRelativePos;

	private Quaternion m_tempRelativeRot;

	private Vector3 m_tempRelativeVel;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_projectile = GetComponent<Projectile>();
		m_character = GetComponent<Character>();
		if (m_nview.GetZDO() == null)
		{
			base.enabled = false;
			return;
		}
		if ((bool)m_body)
		{
			m_isKinematicBody = m_body.isKinematic;
			m_useGravity = m_body.useGravity;
		}
		m_wasOwner = m_nview.GetZDO().IsOwner();
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	private Vector3 GetVelocity()
	{
		if (m_body != null)
		{
			return m_body.velocity;
		}
		if (m_projectile != null)
		{
			return m_projectile.GetVelocity();
		}
		return Vector3.zero;
	}

	private Vector3 GetPosition()
	{
		if (!m_body)
		{
			return base.transform.position;
		}
		return m_body.position;
	}

	private void OwnerSync()
	{
		ZDO zDO = m_nview.GetZDO();
		bool flag = zDO.IsOwner();
		bool flag2 = !m_wasOwner && flag;
		m_wasOwner = flag;
		if (!flag)
		{
			return;
		}
		if (flag2)
		{
			bool flag3 = false;
			if (m_syncPosition)
			{
				base.transform.position = zDO.GetPosition();
				flag3 = true;
			}
			if (m_syncRotation)
			{
				base.transform.rotation = zDO.GetRotation();
				flag3 = true;
			}
			if (m_syncBodyVelocity && (bool)m_body)
			{
				m_body.velocity = zDO.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero);
				m_body.angularVelocity = zDO.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
			}
			if (flag3 && (bool)m_body)
			{
				Physics.SyncTransforms();
			}
		}
		if (base.transform.position.y < -5000f)
		{
			if ((bool)m_body)
			{
				m_body.velocity = Vector3.zero;
			}
			ZLog.Log("Object fell out of world:" + base.gameObject.name);
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			Vector3 position = base.transform.position;
			position.y = groundHeight + 1f;
			base.transform.position = position;
			if ((bool)m_body)
			{
				Physics.SyncTransforms();
			}
			return;
		}
		if (m_syncPosition)
		{
			zDO.SetPosition(GetPosition());
			zDO.Set(ZDOVars.s_velHash, GetVelocity());
			if (m_characterParentSync)
			{
				if (GetRelativePosition(zDO, out m_tempParent, out m_tempAttachJoint, out m_tempRelativePos, out m_tempRelativeRot, out m_tempRelativeVel))
				{
					zDO.SetConnection(ZDOExtraData.ConnectionType.SyncTransform, m_tempParent);
					zDO.Set(ZDOVars.s_attachJointHash, m_tempAttachJoint);
					zDO.Set(ZDOVars.s_relPosHash, m_tempRelativePos);
					zDO.Set(ZDOVars.s_relRotHash, m_tempRelativeRot);
					zDO.Set(ZDOVars.s_velHash, m_tempRelativeVel);
				}
				else
				{
					zDO.UpdateConnection(ZDOExtraData.ConnectionType.SyncTransform, ZDOID.None);
					zDO.Set(ZDOVars.s_attachJointHash, "");
				}
			}
		}
		if (m_syncRotation && base.transform.hasChanged)
		{
			Quaternion rotation = (m_body ? m_body.rotation : base.transform.rotation);
			zDO.SetRotation(rotation);
		}
		if (m_syncScale && base.transform.hasChanged)
		{
			if (Mathf.Approximately(base.transform.localScale.x, base.transform.localScale.y) && Mathf.Approximately(base.transform.localScale.x, base.transform.localScale.z))
			{
				zDO.RemoveVec3(ZDOVars.s_scaleHash);
				zDO.Set(ZDOVars.s_scaleScalarHash, base.transform.localScale.x);
			}
			else
			{
				zDO.RemoveFloat(ZDOVars.s_scaleScalarHash);
				zDO.Set(ZDOVars.s_scaleHash, base.transform.localScale);
			}
		}
		if ((bool)m_body)
		{
			if (m_syncBodyVelocity)
			{
				m_nview.GetZDO().Set(ZDOVars.s_bodyVelHash, m_body.velocity);
				m_nview.GetZDO().Set(ZDOVars.s_bodyAVelHash, m_body.angularVelocity);
			}
			m_body.useGravity = m_useGravity;
		}
		base.transform.hasChanged = false;
	}

	private bool GetRelativePosition(ZDO zdo, out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		if ((bool)m_character)
		{
			return m_character.GetRelativePosition(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
		}
		if ((bool)base.transform.parent)
		{
			ZNetView zNetView = (base.transform.parent ? base.transform.parent.GetComponent<ZNetView>() : null);
			if ((bool)zNetView && zNetView.IsValid())
			{
				parent = zNetView.GetZDO().m_uid;
				attachJoint = "";
				relativePos = base.transform.localPosition;
				relativeRot = base.transform.localRotation;
				relativeVel = Vector3.zero;
				return true;
			}
		}
		parent = ZDOID.None;
		attachJoint = "";
		relativePos = Vector3.zero;
		relativeRot = Quaternion.identity;
		relativeVel = Vector3.zero;
		return false;
	}

	private void SyncPosition(ZDO zdo, float dt, out bool usedLocalRotation)
	{
		usedLocalRotation = false;
		if (m_characterParentSync && zdo.HasOwner())
		{
			ZDOID connectionZDOID = zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.SyncTransform);
			if (!connectionZDOID.IsNone())
			{
				GameObject gameObject = ZNetScene.instance.FindInstance(connectionZDOID);
				if ((bool)gameObject)
				{
					ZSyncTransform component = gameObject.GetComponent<ZSyncTransform>();
					if ((bool)component)
					{
						component.ClientSync(dt);
					}
					string @string = zdo.GetString(ZDOVars.s_attachJointHash);
					Vector3 vector = zdo.GetVec3(ZDOVars.s_relPosHash, Vector3.zero);
					Quaternion quaternion = zdo.GetQuaternion(ZDOVars.s_relRotHash, Quaternion.identity);
					Vector3 vec = zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero);
					bool flag = false;
					if (zdo.DataRevision != m_posRevision)
					{
						m_posRevision = zdo.DataRevision;
						m_targetPosTimer = 0f;
					}
					if (@string.Length > 0)
					{
						Transform transform = Utils.FindChild(gameObject.transform, @string);
						if ((bool)transform)
						{
							base.transform.position = transform.position;
							flag = true;
						}
					}
					else
					{
						m_targetPosTimer += dt;
						m_targetPosTimer = Mathf.Min(m_targetPosTimer, 2f);
						vector += vec * m_targetPosTimer;
						if (!m_haveTempRelPos)
						{
							m_haveTempRelPos = true;
							m_tempRelPos = vector;
						}
						if (Vector3.Distance(m_tempRelPos, vector) > 0.001f)
						{
							m_tempRelPos = Vector3.Lerp(m_tempRelPos, vector, 0.2f);
							vector = m_tempRelPos;
						}
						Vector3 vector2 = gameObject.transform.TransformPoint(vector);
						if (Vector3.Distance(base.transform.position, vector2) > 0.001f)
						{
							base.transform.position = vector2;
							flag = true;
						}
					}
					Quaternion a = Quaternion.Inverse(gameObject.transform.rotation) * base.transform.rotation;
					if (Quaternion.Angle(a, quaternion) > 0.001f)
					{
						Quaternion quaternion2 = Quaternion.Slerp(a, quaternion, 0.5f);
						base.transform.rotation = gameObject.transform.rotation * quaternion2;
						flag = true;
					}
					usedLocalRotation = true;
					if (flag && (bool)m_body)
					{
						Physics.SyncTransforms();
					}
					return;
				}
			}
		}
		m_haveTempRelPos = false;
		Vector3 position = zdo.GetPosition();
		if (zdo.DataRevision != m_posRevision)
		{
			m_posRevision = zdo.DataRevision;
			m_targetPosTimer = 0f;
		}
		if (zdo.HasOwner())
		{
			m_targetPosTimer += dt;
			m_targetPosTimer = Mathf.Min(m_targetPosTimer, 2f);
			Vector3 vec2 = zdo.GetVec3(ZDOVars.s_velHash, Vector3.zero);
			position += vec2 * m_targetPosTimer;
		}
		float num = Vector3.Distance(base.transform.position, position);
		if (num > 0.001f)
		{
			base.transform.position = ((num < 5f) ? Vector3.Lerp(base.transform.position, position, 0.2f) : position);
			if ((bool)m_body)
			{
				Physics.SyncTransforms();
			}
		}
	}

	private void ClientSync(float dt)
	{
		ZDO zDO = m_nview.GetZDO();
		if (zDO.IsOwner())
		{
			return;
		}
		int frameCount = Time.frameCount;
		if (m_lastUpdateFrame == frameCount)
		{
			return;
		}
		m_lastUpdateFrame = frameCount;
		if (m_isKinematicBody)
		{
			if (m_syncPosition)
			{
				Vector3 vector = zDO.GetPosition();
				if (Vector3.Distance(m_body.position, vector) > 5f)
				{
					m_body.position = vector;
				}
				else
				{
					if (Vector3.Distance(m_body.position, vector) > 0.01f)
					{
						vector = Vector3.Lerp(m_body.position, vector, 0.2f);
					}
					m_body.MovePosition(vector);
				}
			}
			if (m_syncRotation)
			{
				Quaternion rotation = zDO.GetRotation();
				if (Quaternion.Angle(m_body.rotation, rotation) > 45f)
				{
					m_body.rotation = rotation;
				}
				else
				{
					m_body.MoveRotation(rotation);
				}
			}
		}
		else
		{
			bool usedLocalRotation = false;
			if (m_syncPosition)
			{
				SyncPosition(zDO, dt, out usedLocalRotation);
			}
			if (m_syncRotation && !usedLocalRotation)
			{
				Quaternion rotation2 = zDO.GetRotation();
				if (Quaternion.Angle(base.transform.rotation, rotation2) > 0.001f)
				{
					base.transform.rotation = Quaternion.Slerp(base.transform.rotation, rotation2, 0.5f);
				}
			}
			if ((bool)m_body)
			{
				m_body.useGravity = false;
				if (m_syncBodyVelocity && m_nview.HasOwner())
				{
					Vector3 vec = zDO.GetVec3(ZDOVars.s_bodyVelHash, Vector3.zero);
					Vector3 vec2 = zDO.GetVec3(ZDOVars.s_bodyAVelHash, Vector3.zero);
					if (vec.magnitude > 0.01f || vec2.magnitude > 0.01f)
					{
						m_body.velocity = vec;
						m_body.angularVelocity = vec2;
					}
					else
					{
						m_body.Sleep();
					}
				}
				else if (!m_body.IsSleeping())
				{
					m_body.velocity = Vector3.zero;
					m_body.angularVelocity = Vector3.zero;
					m_body.Sleep();
				}
			}
		}
		if (!m_syncScale)
		{
			return;
		}
		Vector3 vec3 = zDO.GetVec3(ZDOVars.s_scaleHash, Vector3.zero);
		if (vec3 != Vector3.zero)
		{
			base.transform.localScale = vec3;
			return;
		}
		float @float = zDO.GetFloat(ZDOVars.s_scaleScalarHash, base.transform.localScale.x);
		if (!base.transform.localScale.x.Equals(@float))
		{
			base.transform.localScale = new Vector3(@float, @float, @float);
		}
	}

	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		if (m_nview.IsValid())
		{
			ClientSync(fixedDeltaTime);
		}
	}

	public void CustomLateUpdate(float deltaTime)
	{
		if (m_nview.IsValid())
		{
			OwnerSync();
		}
	}

	public void SyncNow()
	{
		if (m_nview.IsValid())
		{
			OwnerSync();
		}
	}
}
