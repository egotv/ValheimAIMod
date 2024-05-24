using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class LiquidVolume : MonoBehaviour
{
	private const int liquidSaveVersion = 2;

	private float updateHeightTimer = -1000f;

	private List<Vector3> m_tempVertises = new List<Vector3>();

	private List<Vector3> m_tempNormals = new List<Vector3>();

	private List<Vector2> m_tempUVs = new List<Vector2>();

	private List<int> m_tempIndices = new List<int>();

	private List<Color32> m_tempColors = new List<Color32>();

	private List<Vector3> m_tempColliderVertises = new List<Vector3>();

	private List<int> m_tempColliderIndices = new List<int>();

	public int m_width = 32;

	public float m_scale = 1f;

	public float m_maxDepth = 10f;

	public LiquidType m_liquidType = LiquidType.Tar;

	public float m_physicsOffset = -2f;

	public float m_initialVolume = 1000f;

	public int m_initialArea = 8;

	public float m_viscocity = 1f;

	public float m_noiseHeight = 0.1f;

	public float m_noiseFrequency = 1f;

	public float m_noiseSpeed = 1f;

	public bool m_castShadow = true;

	public LayerMask m_groundLayer;

	public MeshCollider m_collider;

	public float m_saveInterval = 4f;

	public float m_randomEffectInterval = 3f;

	public EffectList m_randomEffectList = new EffectList();

	private List<float> m_heights;

	private List<float> m_depths;

	private float m_randomEffectTimer;

	private bool m_haveHeights;

	private bool m_needsSaving;

	private float m_timeSinceSaving;

	private bool m_dirty = true;

	private bool m_dirtyMesh;

	private Mesh m_mesh;

	private MeshFilter m_meshFilter;

	private Thread m_builder;

	private Mutex m_meshDataLock = new Mutex();

	private bool m_stopThread;

	private Mutex m_timerLock = new Mutex();

	private float m_timeToSimulate;

	private float m_updateDelayTimer;

	private ZNetView m_nview;

	private uint m_lastDataRevision = uint.MaxValue;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_meshFilter = GetComponent<MeshFilter>();
		base.transform.rotation = Quaternion.identity;
		int num = m_width + 1;
		int num2 = num * num;
		m_depths = new List<float>(num2);
		m_heights = new List<float>(num2);
		for (int i = 0; i < num2; i++)
		{
			m_depths.Add(0f);
			m_heights.Add(0f);
		}
		m_mesh = new Mesh();
		m_mesh.name = "___LiquidVolume m_mesh";
		if (HaveSavedData())
		{
			CheckLoad();
		}
		else
		{
			InitializeLevels();
		}
		m_builder = new Thread(UpdateThread);
		m_builder.Start();
	}

	private void OnDestroy()
	{
		m_stopThread = true;
		m_builder.Join();
		m_timerLock.Close();
		m_meshDataLock.Close();
		Object.Destroy(m_mesh);
	}

	private void InitializeLevels()
	{
		int num = m_width / 2;
		int initialArea = m_initialArea;
		int num2 = m_width + 1;
		float value = m_initialVolume / (float)(initialArea * initialArea);
		for (int i = num - initialArea / 2; i <= num + initialArea / 2; i++)
		{
			for (int j = num - initialArea / 2; j <= num + initialArea / 2; j++)
			{
				m_depths[i * num2 + j] = value;
			}
		}
	}

	private void CheckSave(float dt)
	{
		m_timeSinceSaving += dt;
		if (m_needsSaving && m_timeSinceSaving > m_saveInterval)
		{
			m_needsSaving = false;
			m_timeSinceSaving = 0f;
			Save();
		}
	}

	private void Save()
	{
		if (!(m_nview == null) && m_nview.IsValid() && m_nview.IsOwner())
		{
			m_meshDataLock.WaitOne();
			ZPackage zPackage = new ZPackage();
			zPackage.Write(2);
			float num = 0f;
			zPackage.Write(m_depths.Count);
			for (int i = 0; i < m_depths.Count; i++)
			{
				float num2 = m_depths[i];
				short data = (short)(num2 * 100f);
				zPackage.Write(data);
				num += num2;
			}
			zPackage.Write(num);
			byte[] array = Utils.Compress(zPackage.GetArray());
			m_nview.GetZDO().Set(ZDOVars.s_liquidData, array);
			m_lastDataRevision = m_nview.GetZDO().DataRevision;
			m_meshDataLock.ReleaseMutex();
			ZLog.Log("Saved liquid:" + array.Length + " bytes  total:" + num);
		}
	}

	private void CheckLoad()
	{
		if (!(m_nview == null) && m_nview.IsValid() && m_nview.GetZDO().DataRevision != m_lastDataRevision)
		{
			Load();
		}
	}

	private bool HaveSavedData()
	{
		if (m_nview == null || !m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetByteArray(ZDOVars.s_liquidData) != null;
	}

	private void Load()
	{
		m_lastDataRevision = m_nview.GetZDO().DataRevision;
		m_needsSaving = false;
		byte[] byteArray = m_nview.GetZDO().GetByteArray(ZDOVars.s_liquidData);
		if (byteArray == null)
		{
			return;
		}
		ZPackage zPackage = new ZPackage(Utils.Decompress(byteArray));
		int num = zPackage.ReadInt();
		int num2 = zPackage.ReadInt();
		m_meshDataLock.WaitOne();
		if (num2 != m_depths.Count)
		{
			ZLog.LogWarning("Depth array size missmatch");
			return;
		}
		float num3 = 0f;
		int num4 = 0;
		for (int i = 0; i < m_depths.Count; i++)
		{
			float num5 = (float)zPackage.ReadShort() / 100f;
			m_depths[i] = num5;
			num3 += num5;
			if (num5 > 0f)
			{
				num4++;
			}
		}
		if (num >= 2)
		{
			float num6 = zPackage.ReadSingle();
			if (num4 > 0)
			{
				float num7 = (num6 - num3) / (float)num4;
				for (int j = 0; j < m_depths.Count; j++)
				{
					float num8 = m_depths[j];
					if (num8 > 0f)
					{
						m_depths[j] = num8 + num7;
					}
				}
			}
		}
		m_meshDataLock.ReleaseMutex();
	}

	private void UpdateThread()
	{
		while (!m_stopThread)
		{
			m_timerLock.WaitOne();
			bool flag = false;
			if (m_timeToSimulate >= 0.05f && m_haveHeights)
			{
				m_timeToSimulate = 0f;
				flag = true;
			}
			m_timerLock.ReleaseMutex();
			if (flag)
			{
				m_meshDataLock.WaitOne();
				UpdateLiquid(0.05f);
				if (m_dirty)
				{
					m_dirty = false;
					PrebuildMesh();
				}
				m_meshDataLock.ReleaseMutex();
			}
			Thread.Sleep(1);
		}
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		if (m_nview != null)
		{
			if (!m_nview.IsValid())
			{
				return;
			}
			CheckLoad();
			if (m_nview.IsOwner())
			{
				CheckSave(deltaTime);
			}
		}
		m_updateDelayTimer += deltaTime;
		if (m_updateDelayTimer > 1f)
		{
			m_timerLock.WaitOne();
			m_timeToSimulate += deltaTime;
			m_timerLock.ReleaseMutex();
		}
		updateHeightTimer -= deltaTime;
		if (updateHeightTimer <= 0f && m_meshDataLock.WaitOne(0))
		{
			UpdateHeights();
			m_haveHeights = true;
			m_meshDataLock.ReleaseMutex();
			updateHeightTimer = 1f;
		}
		if (m_dirtyMesh && m_meshDataLock.WaitOne(0))
		{
			m_dirtyMesh = false;
			PostBuildMesh();
			m_meshDataLock.ReleaseMutex();
		}
		UpdateEffects(deltaTime);
	}

	private void UpdateLiquid(float dt)
	{
		float num = 0f;
		for (int i = 0; i < m_depths.Count; i++)
		{
			num += m_depths[i];
		}
		int num2 = m_width + 1;
		float maxD = dt * m_viscocity;
		for (int j = 0; j < num2 - 1; j++)
		{
			for (int k = 0; k < num2 - 1; k++)
			{
				int index = j * num2 + k;
				int index2 = j * num2 + k + 1;
				EvenDepth(index, index2, maxD);
			}
		}
		for (int l = 0; l < num2 - 1; l++)
		{
			for (int m = 0; m < num2 - 1; m++)
			{
				int index3 = m * num2 + l;
				int index4 = (m + 1) * num2 + l;
				EvenDepth(index3, index4, maxD);
			}
		}
		float num3 = 0f;
		int num4 = 0;
		for (int n = 0; n < m_depths.Count; n++)
		{
			float num5 = m_depths[n];
			num3 += num5;
			if (num5 > 0f)
			{
				num4++;
			}
		}
		float num6 = num - num3;
		if (num6 != 0f && num4 > 0)
		{
			float num7 = num6 / (float)num4;
			for (int num8 = 0; num8 < m_depths.Count; num8++)
			{
				float num9 = m_depths[num8];
				if (num9 > 0f)
				{
					m_depths[num8] = num9 + num7;
				}
			}
		}
		for (int num10 = 0; num10 < num2; num10++)
		{
			m_depths[num10] = 0f;
			m_depths[m_width * num2 + num10] = 0f;
			m_depths[num10 * num2] = 0f;
			m_depths[num10 * num2 + m_width] = 0f;
		}
	}

	private void EvenDepth(int index0, int index1, float maxD)
	{
		float num = m_depths[index0];
		float num2 = m_depths[index1];
		if (num == 0f && num2 == 0f)
		{
			return;
		}
		float num3 = m_heights[index0];
		float num4 = m_heights[index1];
		float num5 = num3 + num;
		float num6 = num4 + num2;
		if (Mathf.Abs(num6 - num5) < 0.001f)
		{
			return;
		}
		if (num5 > num6)
		{
			if (num <= 0f)
			{
				return;
			}
			float num7 = num5 - num6;
			float f = num7 * m_viscocity;
			f = Mathf.Pow(f, 0.5f);
			f = Mathf.Min(f, num7 * 0.5f);
			f = Mathf.Min(f, num);
			num -= f;
			num2 += f;
		}
		else
		{
			if (num2 <= 0f)
			{
				return;
			}
			float num8 = num6 - num5;
			float f2 = num8 * m_viscocity;
			f2 = Mathf.Pow(f2, 0.5f);
			f2 = Mathf.Min(f2, num8 * 0.5f);
			f2 = Mathf.Min(f2, num2);
			num2 -= f2;
			num += f2;
		}
		m_depths[index0] = Mathf.Max(0f, num);
		m_depths[index1] = Mathf.Max(0f, num2);
		m_dirty = true;
		m_needsSaving = true;
	}

	private void UpdateHeights()
	{
		int value = m_groundLayer.value;
		int num = m_width + 1;
		float num2 = 0f - m_maxDepth;
		float y = base.transform.position.y;
		float maxDistance = m_maxDepth * 2f;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 origin = CalcMaxVertex(j, i);
				float value2 = num2;
				if (Physics.Raycast(origin, Vector3.down, out var hitInfo, maxDistance, value))
				{
					value2 = hitInfo.point.y - y;
				}
				float value3 = Mathf.Clamp(value2, 0f - m_maxDepth, m_maxDepth);
				m_heights[i * num + j] = value3;
			}
		}
	}

	private void PrebuildMesh()
	{
		int num = m_width + 1;
		m_tempVertises.Clear();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 item = CalcVertex(j, i, collider: false);
				m_tempVertises.Add(item);
			}
		}
		m_tempNormals.Clear();
		for (int k = 0; k < num; k++)
		{
			for (int l = 0; l < num; l++)
			{
				if (l == num - 1 || k == num - 1)
				{
					m_tempNormals.Add(Vector3.up);
					continue;
				}
				Vector3 vector = m_tempVertises[k * num + l];
				Vector3 vector2 = m_tempVertises[k * num + l + 1];
				Vector3 normalized = Vector3.Cross(m_tempVertises[(k + 1) * num + l] - vector, vector2 - vector).normalized;
				m_tempNormals.Add(normalized);
			}
		}
		m_tempColors.Clear();
		Color color = new Color(1f, 1f, 1f, 0f);
		Color color2 = new Color(1f, 1f, 1f, 1f);
		for (int m = 0; m < m_depths.Count; m++)
		{
			if (m_depths[m] < 0.001f)
			{
				m_tempColors.Add(color);
			}
			else
			{
				m_tempColors.Add(color2);
			}
		}
		if (m_tempIndices.Count == 0)
		{
			m_tempUVs.Clear();
			for (int n = 0; n < num; n++)
			{
				for (int num2 = 0; num2 < num; num2++)
				{
					m_tempUVs.Add(new Vector2((float)num2 / (float)m_width, (float)n / (float)m_width));
				}
			}
			m_tempIndices.Clear();
			for (int num3 = 0; num3 < num - 1; num3++)
			{
				for (int num4 = 0; num4 < num - 1; num4++)
				{
					int item2 = num3 * num + num4;
					int item3 = num3 * num + num4 + 1;
					int item4 = (num3 + 1) * num + num4 + 1;
					int item5 = (num3 + 1) * num + num4;
					m_tempIndices.Add(item2);
					m_tempIndices.Add(item5);
					m_tempIndices.Add(item3);
					m_tempIndices.Add(item3);
					m_tempIndices.Add(item5);
					m_tempIndices.Add(item4);
				}
			}
		}
		m_tempColliderVertises.Clear();
		int num5 = m_width / 2;
		int num6 = num5 + 1;
		for (int num7 = 0; num7 < num6; num7++)
		{
			for (int num8 = 0; num8 < num6; num8++)
			{
				Vector3 item6 = CalcVertex(num8 * 2, num7 * 2, collider: true);
				m_tempColliderVertises.Add(item6);
			}
		}
		if (m_tempColliderIndices.Count == 0)
		{
			m_tempColliderIndices.Clear();
			for (int num9 = 0; num9 < num5; num9++)
			{
				for (int num10 = 0; num10 < num5; num10++)
				{
					int item7 = num9 * num6 + num10;
					int item8 = num9 * num6 + num10 + 1;
					int item9 = (num9 + 1) * num6 + num10 + 1;
					int item10 = (num9 + 1) * num6 + num10;
					m_tempColliderIndices.Add(item7);
					m_tempColliderIndices.Add(item10);
					m_tempColliderIndices.Add(item8);
					m_tempColliderIndices.Add(item8);
					m_tempColliderIndices.Add(item10);
					m_tempColliderIndices.Add(item9);
				}
			}
		}
		m_dirtyMesh = true;
	}

	private void SmoothNormals(List<Vector3> normals, float yScale)
	{
		int num = m_width + 1;
		for (int i = 1; i < num - 1; i++)
		{
			for (int j = 1; j < num - 1; j++)
			{
				Vector3 vector = normals[i * num + j];
				Vector3 vector2 = normals[(i - 1) * num + j];
				Vector3 vector3 = normals[(i + 1) * num + j];
				vector2.y *= yScale;
				vector3.y *= yScale;
				vector = (vector + vector2 + vector3).normalized;
				normals[i * num + j] = vector;
			}
		}
		for (int k = 1; k < num - 1; k++)
		{
			for (int l = 1; l < num - 1; l++)
			{
				Vector3 vector4 = normals[k * num + l];
				Vector3 vector5 = normals[k * num + l - 1];
				Vector3 vector6 = normals[k * num + l + 1];
				vector5.y *= yScale;
				vector6.y *= yScale;
				vector4 = (vector4 + vector5 + vector6).normalized;
				normals[k * num + l] = vector4;
			}
		}
	}

	private void PostBuildMesh()
	{
		_ = Time.realtimeSinceStartup;
		m_mesh.SetVertices(m_tempVertises);
		m_mesh.SetNormals(m_tempNormals);
		m_mesh.SetColors(m_tempColors);
		if (m_mesh.GetIndexCount(0) == 0)
		{
			m_mesh.SetUVs(0, m_tempUVs);
			m_mesh.SetIndices(m_tempIndices.ToArray(), MeshTopology.Triangles, 0);
		}
		m_mesh.RecalculateBounds();
		if ((bool)m_meshFilter)
		{
			m_meshFilter.sharedMesh = m_mesh;
		}
	}

	private void RebuildMesh()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		int num = m_width + 1;
		float num2 = -999999f;
		float num3 = 999999f;
		m_tempVertises.Clear();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 item = CalcVertex(j, i, collider: false);
				m_tempVertises.Add(item);
				if (item.y > num2)
				{
					num2 = item.y;
				}
				if (item.y < num3)
				{
					num3 = item.y;
				}
			}
		}
		m_mesh.SetVertices(m_tempVertises);
		m_tempColors.Clear();
		Color color = new Color(1f, 1f, 1f, 0f);
		Color color2 = new Color(1f, 1f, 1f, 1f);
		for (int k = 0; k < m_depths.Count; k++)
		{
			if (m_depths[k] < 0.001f)
			{
				m_tempColors.Add(color);
			}
			else
			{
				m_tempColors.Add(color2);
			}
		}
		m_mesh.SetColors(m_tempColors);
		int num4 = (num - 1) * (num - 1) * 6;
		if (m_mesh.GetIndexCount(0) != num4)
		{
			m_tempUVs.Clear();
			for (int l = 0; l < num; l++)
			{
				for (int m = 0; m < num; m++)
				{
					m_tempUVs.Add(new Vector2((float)m / (float)m_width, (float)l / (float)m_width));
				}
			}
			m_mesh.SetUVs(0, m_tempUVs);
			m_tempIndices.Clear();
			for (int n = 0; n < num - 1; n++)
			{
				for (int num5 = 0; num5 < num - 1; num5++)
				{
					int item2 = n * num + num5;
					int item3 = n * num + num5 + 1;
					int item4 = (n + 1) * num + num5 + 1;
					int item5 = (n + 1) * num + num5;
					m_tempIndices.Add(item2);
					m_tempIndices.Add(item5);
					m_tempIndices.Add(item3);
					m_tempIndices.Add(item3);
					m_tempIndices.Add(item5);
					m_tempIndices.Add(item4);
				}
			}
			m_mesh.SetIndices(m_tempIndices.ToArray(), MeshTopology.Triangles, 0);
		}
		ZLog.Log("Update mesh1 " + (Time.realtimeSinceStartup - realtimeSinceStartup) * 1000f);
		realtimeSinceStartup = Time.realtimeSinceStartup;
		m_mesh.RecalculateNormals();
		ZLog.Log("Update mesh2 " + (Time.realtimeSinceStartup - realtimeSinceStartup) * 1000f);
		realtimeSinceStartup = Time.realtimeSinceStartup;
		m_mesh.RecalculateTangents();
		ZLog.Log("Update mesh3 " + (Time.realtimeSinceStartup - realtimeSinceStartup) * 1000f);
		realtimeSinceStartup = Time.realtimeSinceStartup;
		m_mesh.RecalculateBounds();
		ZLog.Log("Update mesh4 " + (Time.realtimeSinceStartup - realtimeSinceStartup) * 1000f);
		realtimeSinceStartup = Time.realtimeSinceStartup;
		if ((bool)m_collider)
		{
			m_collider.sharedMesh = m_mesh;
		}
		ZLog.Log("Update mesh5 " + (Time.realtimeSinceStartup - realtimeSinceStartup) * 1000f);
		realtimeSinceStartup = Time.realtimeSinceStartup;
		if ((bool)m_meshFilter)
		{
			m_meshFilter.sharedMesh = m_mesh;
		}
		ZLog.Log("Update mesh6 " + (Time.realtimeSinceStartup - realtimeSinceStartup) * 1000f);
		realtimeSinceStartup = Time.realtimeSinceStartup;
	}

	private Vector3 CalcMaxVertex(int x, int y)
	{
		int num = m_width + 1;
		Vector3 vector = new Vector3((float)m_width * m_scale * -0.5f, m_maxDepth, (float)m_width * m_scale * -0.5f);
		return base.transform.position + vector + new Vector3((float)x * m_scale, 0f, (float)y * m_scale);
	}

	private void ClampHeight(int x, int y, ref float height)
	{
		if (x >= 0 && y >= 0 && x < m_width + 1 && y < m_width + 1)
		{
			int num = m_width + 1;
			int index = y * num + x;
			float num2 = m_depths[index];
			if (!((double)num2 <= 0.0))
			{
				float num3 = m_heights[index];
				height = num3 + num2;
				height -= 0.1f;
			}
		}
	}

	private bool HasTarNeighbour(int cx, int cy)
	{
		int num = m_width + 1;
		for (int i = cy - 2; i <= cy + 2; i++)
		{
			for (int j = cx - 2; j <= cx + 2; j++)
			{
				if (j >= 0 && i >= 0 && j < num && i < num && m_depths[i * num + j] > 0f)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ClampToNeighbourSurface(int x, int y, ref float d)
	{
		ClampHeight(x - 1, y - 1, ref d);
		ClampHeight(x, y - 1, ref d);
		ClampHeight(x + 1, y - 1, ref d);
		ClampHeight(x - 1, y + 1, ref d);
		ClampHeight(x, y + 1, ref d);
		ClampHeight(x + 1, y + 1, ref d);
		ClampHeight(x - 1, y, ref d);
		ClampHeight(x + 1, y, ref d);
	}

	private Vector3 CalcVertex(int x, int y, bool collider)
	{
		int num = m_width + 1;
		int index = y * num + x;
		float num2 = m_heights[index];
		float num3 = m_depths[index];
		if (!collider)
		{
			if (num3 > 0f)
			{
				num3 = Mathf.Max(0.1f, num3);
				num2 += num3;
			}
		}
		else
		{
			num2 = ((!(num3 < 0.001f)) ? (num2 + num3) : (num2 - 1f));
			num2 += m_physicsOffset;
		}
		return new Vector3((float)m_width * m_scale * -0.5f, 0f, (float)m_width * m_scale * -0.5f) + new Vector3((float)x * m_scale, num2, (float)y * m_scale);
	}

	public float GetSurface(Vector3 p)
	{
		Vector2 vector = WorldToLocal(p);
		float depth = GetDepth(vector.x, vector.y);
		float height = GetHeight(vector.x, vector.y);
		depth = ((!((double)depth <= 0.001)) ? (depth + Mathf.Sin(p.x * m_noiseFrequency + Time.time * m_noiseSpeed) * Mathf.Sin(p.z * m_noiseFrequency + Time.time * 0.78521f * m_noiseSpeed) * m_noiseHeight) : (depth - 0.5f));
		return base.transform.position.y + height + depth;
	}

	private float GetDepth(float x, float y)
	{
		x = Mathf.Clamp(x, 0f, m_width);
		x = Mathf.Clamp(x, 0f, m_width);
		int num = (int)x;
		int num2 = (int)y;
		float t = x - (float)num;
		float t2 = y - (float)num2;
		float a = Mathf.Lerp(GetDepth(num, num2), GetDepth(num + 1, num2), t);
		float b = Mathf.Lerp(GetDepth(num, num2 + 1), GetDepth(num + 1, num2 + 1), t);
		return Mathf.Lerp(a, b, t2);
	}

	private float GetHeight(float x, float y)
	{
		x = Mathf.Clamp(x, 0f, m_width);
		x = Mathf.Clamp(x, 0f, m_width);
		int num = (int)x;
		int num2 = (int)y;
		float t = x - (float)num;
		float t2 = y - (float)num2;
		float a = Mathf.Lerp(GetHeight(num, num2), GetHeight(num + 1, num2), t);
		float b = Mathf.Lerp(GetHeight(num, num2 + 1), GetHeight(num + 1, num2 + 1), t);
		return Mathf.Lerp(a, b, t2);
	}

	private float GetDepth(int x, int y)
	{
		int num = m_width + 1;
		x = Mathf.Clamp(x, 0, m_width);
		y = Mathf.Clamp(y, 0, m_width);
		return m_depths[y * num + x];
	}

	private float GetHeight(int x, int y)
	{
		int num = m_width + 1;
		x = Mathf.Clamp(x, 0, m_width);
		y = Mathf.Clamp(y, 0, m_width);
		return m_heights[y * num + x];
	}

	private Vector2 WorldToLocal(Vector3 v)
	{
		Vector3 position = base.transform.position;
		float num = (float)m_width * m_scale * -0.5f;
		Vector2 result = new Vector2(v.x, v.z);
		result.x -= position.x + num;
		result.y -= position.z + num;
		result.x /= m_scale;
		result.y /= m_scale;
		return result;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(base.transform.position, new Vector3((float)m_width * m_scale, m_maxDepth * 2f, (float)m_width * m_scale));
	}

	private void UpdateEffects(float dt)
	{
		m_randomEffectTimer += dt;
		if (!(m_randomEffectTimer < m_randomEffectInterval))
		{
			m_randomEffectTimer = 0f;
			Vector2Int vector2Int = new Vector2Int(Random.Range(0, m_width), Random.Range(0, m_width));
			if (!(GetDepth(vector2Int.x, vector2Int.y) < 0.2f))
			{
				Vector3 basePos = CalcVertex(vector2Int.x, vector2Int.y, collider: false) + base.transform.position;
				m_randomEffectList.Create(basePos, Quaternion.identity);
			}
		}
	}
}
