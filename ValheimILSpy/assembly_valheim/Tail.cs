using System;
using System.Collections.Generic;
using UnityEngine;

public class Tail : MonoBehaviour, IMonoUpdater
{
	private class TailSegment
	{
		public Transform transform;

		public Vector3 pos;

		public Quaternion rot;

		public float distance;
	}

	public List<Transform> m_tailJoints = new List<Transform>();

	public float m_maxAngle = 80f;

	public float m_gravity = 2f;

	public float m_gravityInWater = 0.1f;

	public bool m_waterSurfaceCheck;

	public bool m_groundCheck;

	public float m_smoothness = 0.1f;

	public float m_tailRadius;

	public Character m_character;

	public Rigidbody m_characterBody;

	public Rigidbody m_tailBody;

	private readonly List<TailSegment> m_positions = new List<TailSegment>();

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		foreach (Transform tailJoint in m_tailJoints)
		{
			float distance = Vector3.Distance(tailJoint.parent.position, tailJoint.position);
			Vector3 position = tailJoint.position;
			TailSegment tailSegment = new TailSegment();
			tailSegment.transform = tailJoint;
			tailSegment.pos = position;
			tailSegment.rot = tailJoint.rotation;
			tailSegment.distance = distance;
			m_positions.Add(tailSegment);
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

	public void CustomLateUpdate(float dt)
	{
		for (int i = 0; i < m_positions.Count; i++)
		{
			TailSegment tailSegment = m_positions[i];
			if (m_waterSurfaceCheck)
			{
				float liquidLevel = Floating.GetLiquidLevel(tailSegment.pos);
				if (tailSegment.pos.y + m_tailRadius > liquidLevel)
				{
					tailSegment.pos.y -= m_gravity * dt;
				}
				else
				{
					tailSegment.pos.y -= m_gravityInWater * dt;
				}
			}
			else
			{
				tailSegment.pos.y -= m_gravity * dt;
			}
			Vector3 vector = tailSegment.transform.parent.position + tailSegment.transform.parent.up * tailSegment.distance * 0.5f;
			Vector3 target = Vector3.Normalize(vector - tailSegment.pos);
			target = Vector3.RotateTowards(-tailSegment.transform.parent.up, target, (float)Math.PI / 180f * m_maxAngle, 1f);
			Vector3 vector2 = vector - target * tailSegment.distance * 0.5f;
			if (m_groundCheck)
			{
				float groundHeight = ZoneSystem.instance.GetGroundHeight(vector2);
				if (vector2.y - m_tailRadius < groundHeight)
				{
					vector2.y = groundHeight + m_tailRadius;
				}
			}
			vector2 = Vector3.Lerp(tailSegment.pos, vector2, m_smoothness);
			if (vector == vector2)
			{
				break;
			}
			Vector3 normalized = (vector - vector2).normalized;
			Vector3 rhs = Vector3.Cross(Vector3.up, -normalized);
			Quaternion b = Quaternion.LookRotation(Vector3.Cross(-normalized, rhs), -normalized);
			b = Quaternion.Slerp(tailSegment.rot, b, m_smoothness);
			tailSegment.transform.position = vector2;
			tailSegment.transform.rotation = b;
			tailSegment.pos = vector2;
			tailSegment.rot = b;
		}
	}
}
