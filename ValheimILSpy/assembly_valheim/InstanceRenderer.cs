using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InstanceRenderer : MonoBehaviour, IMonoUpdater
{
	public Mesh m_mesh;

	public Material m_material;

	public Vector3 m_scale = Vector3.one;

	public bool m_frustumCull = true;

	public bool m_useLod;

	public bool m_useXZLodDistance = true;

	public float m_lodMinDistance = 5f;

	public float m_lodMaxDistance = 20f;

	public ShadowCastingMode m_shadowCasting;

	private bool m_dirtyBounds = true;

	private BoundingSphere m_bounds;

	private float m_lodCount;

	private Matrix4x4[] m_instances = new Matrix4x4[1024];

	private int m_instanceCount;

	private bool m_firstFrame = true;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	public void CustomUpdate(float deltaTime, float time)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (m_instanceCount == 0 || mainCamera == null)
		{
			return;
		}
		if (m_frustumCull)
		{
			if (m_dirtyBounds)
			{
				UpdateBounds();
			}
			if (!Utils.InsideMainCamera(m_bounds))
			{
				return;
			}
		}
		if (m_useLod)
		{
			float num = (m_useXZLodDistance ? Utils.DistanceXZ(mainCamera.transform.position, base.transform.position) : Vector3.Distance(mainCamera.transform.position, base.transform.position));
			int num2 = (int)((1f - Utils.LerpStep(m_lodMinDistance, m_lodMaxDistance, num)) * (float)m_instanceCount);
			float maxDelta = deltaTime * (float)m_instanceCount;
			m_lodCount = Mathf.MoveTowards(m_lodCount, num2, maxDelta);
			if (m_firstFrame)
			{
				if (num < m_lodMinDistance)
				{
					m_lodCount = num2;
				}
				m_firstFrame = false;
			}
			m_lodCount = Mathf.Min(m_lodCount, m_instanceCount);
			int num3 = (int)m_lodCount;
			if (num3 > 0)
			{
				Graphics.DrawMeshInstanced(m_mesh, 0, m_material, m_instances, num3, null, m_shadowCasting);
			}
		}
		else
		{
			Graphics.DrawMeshInstanced(m_mesh, 0, m_material, m_instances, m_instanceCount, null, m_shadowCasting);
		}
	}

	private void UpdateBounds()
	{
		m_dirtyBounds = false;
		Vector3 vector = new Vector3(9999999f, 9999999f, 9999999f);
		Vector3 vector2 = new Vector3(-9999999f, -9999999f, -9999999f);
		float magnitude = m_mesh.bounds.extents.magnitude;
		for (int i = 0; i < m_instanceCount; i++)
		{
			Matrix4x4 matrix4x = m_instances[i];
			Vector3 vector3 = new Vector3(matrix4x[0, 3], matrix4x[1, 3], matrix4x[2, 3]);
			Vector3 lossyScale = matrix4x.lossyScale;
			float num = Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z);
			Vector3 vector4 = new Vector3(num * magnitude, num * magnitude, num * magnitude);
			vector2 = Vector3.Max(vector2, vector3 + vector4);
			vector = Vector3.Min(vector, vector3 - vector4);
		}
		m_bounds.position = (vector2 + vector) * 0.5f;
		m_bounds.radius = Vector3.Distance(vector2, m_bounds.position);
	}

	public void AddInstance(Vector3 pos, Quaternion rot, float scale)
	{
		Matrix4x4 m = Matrix4x4.TRS(pos, rot, m_scale * scale);
		AddInstance(m);
	}

	public void AddInstance(Vector3 pos, Quaternion rot)
	{
		Matrix4x4 m = Matrix4x4.TRS(pos, rot, m_scale);
		AddInstance(m);
	}

	public void AddInstance(Matrix4x4 m)
	{
		if (m_instanceCount < 1023)
		{
			m_instances[m_instanceCount] = m;
			m_instanceCount++;
			m_dirtyBounds = true;
		}
	}

	public void Clear()
	{
		m_instanceCount = 0;
		m_dirtyBounds = true;
	}

	public void SetInstance(int index, Vector3 pos, Quaternion rot, float scale)
	{
		Matrix4x4 matrix4x = Matrix4x4.TRS(pos, rot, m_scale * scale);
		m_instances[index] = matrix4x;
		m_dirtyBounds = true;
	}

	private void Resize(int instances)
	{
		m_instanceCount = instances;
		m_dirtyBounds = true;
	}

	public void SetInstances(List<Transform> transforms, bool faceCamera = false)
	{
		Resize(transforms.Count);
		for (int i = 0; i < transforms.Count; i++)
		{
			Transform transform = transforms[i];
			m_instances[i] = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
		}
		m_dirtyBounds = true;
	}

	public void SetInstancesBillboard(List<Vector4> points)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if (!(mainCamera == null))
		{
			Vector3 forward = -mainCamera.transform.forward;
			Resize(points.Count);
			for (int i = 0; i < points.Count; i++)
			{
				Vector4 vector = points[i];
				Vector3 pos = new Vector3(vector.x, vector.y, vector.z);
				float w = vector.w;
				Quaternion q = Quaternion.LookRotation(forward);
				m_instances[i] = Matrix4x4.TRS(pos, q, w * m_scale);
			}
			m_dirtyBounds = true;
		}
	}

	private void OnDrawGizmosSelected()
	{
	}
}
