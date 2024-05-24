using System.Collections.Generic;
using UnityEngine;

public class TerrainComp : MonoBehaviour
{
	private const int terrainCompVersion = 1;

	private static readonly List<TerrainComp> s_instances = new List<TerrainComp>();

	private bool m_initialized;

	private int m_width;

	private float m_size;

	private int m_operations;

	private bool[] m_modifiedHeight;

	private float[] m_levelDelta;

	private float[] m_smoothDelta;

	private bool[] m_modifiedPaint;

	private Color[] m_paintMask;

	private Heightmap m_hmap;

	private ZNetView m_nview;

	private uint m_lastDataRevision = uint.MaxValue;

	private Vector3 m_lastOpPoint;

	private float m_lastOpRadius;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_hmap = Heightmap.FindHeightmap(base.transform.position);
		if (m_hmap == null)
		{
			ZLog.LogWarning("Terrain compiler could not find hmap");
			return;
		}
		TerrainComp terrainComp = FindTerrainCompiler(base.transform.position);
		if ((bool)terrainComp)
		{
			ZLog.LogWarning("Found another terrain compiler in this area, removing it");
			ZNetScene.instance.Destroy(terrainComp.gameObject);
		}
		s_instances.Add(this);
		m_nview.Register<ZPackage>("ApplyOperation", RPC_ApplyOperation);
		Initialize();
		CheckLoad();
	}

	private void OnDestroy()
	{
		s_instances.Remove(this);
	}

	private void Update()
	{
		if (m_nview.IsValid())
		{
			CheckLoad();
		}
	}

	private void Initialize()
	{
		m_initialized = true;
		m_width = m_hmap.m_width;
		m_size = (float)m_width * m_hmap.m_scale;
		int num = m_width + 1;
		m_modifiedHeight = new bool[num * num];
		m_levelDelta = new float[num * num];
		m_smoothDelta = new float[num * num];
		m_modifiedPaint = new bool[num * num];
		m_paintMask = new Color[num * num];
	}

	private void Save()
	{
		if (!m_initialized || !m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		ZPackage zPackage = new ZPackage();
		zPackage.Write(1);
		zPackage.Write(m_operations);
		zPackage.Write(m_lastOpPoint);
		zPackage.Write(m_lastOpRadius);
		zPackage.Write(m_modifiedHeight.Length);
		for (int i = 0; i < m_modifiedHeight.Length; i++)
		{
			zPackage.Write(m_modifiedHeight[i]);
			if (m_modifiedHeight[i])
			{
				zPackage.Write(m_levelDelta[i]);
				zPackage.Write(m_smoothDelta[i]);
			}
		}
		zPackage.Write(m_modifiedPaint.Length);
		for (int j = 0; j < m_modifiedPaint.Length; j++)
		{
			zPackage.Write(m_modifiedPaint[j]);
			if (m_modifiedPaint[j])
			{
				zPackage.Write(m_paintMask[j].r);
				zPackage.Write(m_paintMask[j].g);
				zPackage.Write(m_paintMask[j].b);
				zPackage.Write(m_paintMask[j].a);
			}
		}
		byte[] bytes = Utils.Compress(zPackage.GetArray());
		m_nview.GetZDO().Set(ZDOVars.s_TCData, bytes);
		m_lastDataRevision = m_nview.GetZDO().DataRevision;
	}

	private void CheckLoad()
	{
		if (m_nview.GetZDO().DataRevision == m_lastDataRevision)
		{
			return;
		}
		int operations = m_operations;
		if (!Load())
		{
			return;
		}
		m_hmap.Poke(delayed: false);
		if ((bool)ClutterSystem.instance)
		{
			if (m_operations == operations + 1)
			{
				ClutterSystem.instance.ResetGrass(m_lastOpPoint, m_lastOpRadius);
			}
			else
			{
				ClutterSystem.instance.ResetGrass(m_hmap.transform.position, (float)m_hmap.m_width * m_hmap.m_scale / 2f);
			}
		}
	}

	private bool Load()
	{
		m_lastDataRevision = m_nview.GetZDO().DataRevision;
		byte[] byteArray = m_nview.GetZDO().GetByteArray(ZDOVars.s_TCData);
		if (byteArray == null)
		{
			return false;
		}
		ZPackage zPackage = new ZPackage(Utils.Decompress(byteArray));
		zPackage.ReadInt();
		m_operations = zPackage.ReadInt();
		m_lastOpPoint = zPackage.ReadVector3();
		m_lastOpRadius = zPackage.ReadSingle();
		int num = zPackage.ReadInt();
		if (num != m_modifiedHeight.Length)
		{
			ZLog.LogWarning("Terrain data load error, height array missmatch");
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			m_modifiedHeight[i] = zPackage.ReadBool();
			if (m_modifiedHeight[i])
			{
				m_levelDelta[i] = zPackage.ReadSingle();
				m_smoothDelta[i] = zPackage.ReadSingle();
			}
			else
			{
				m_levelDelta[i] = 0f;
				m_smoothDelta[i] = 0f;
			}
		}
		int num2 = zPackage.ReadInt();
		for (int j = 0; j < num2; j++)
		{
			m_modifiedPaint[j] = zPackage.ReadBool();
			if (m_modifiedPaint[j])
			{
				Color color = default(Color);
				color.r = zPackage.ReadSingle();
				color.g = zPackage.ReadSingle();
				color.b = zPackage.ReadSingle();
				color.a = zPackage.ReadSingle();
				m_paintMask[j] = color;
			}
			else
			{
				m_paintMask[j] = Color.black;
			}
		}
		if (num2 == m_width * m_width)
		{
			Color[] array = new Color[m_paintMask.Length];
			m_paintMask.CopyTo(array, 0);
			bool[] array2 = new bool[m_modifiedPaint.Length];
			m_modifiedPaint.CopyTo(array2, 0);
			int num3 = m_width + 1;
			for (int k = 0; k < m_paintMask.Length; k++)
			{
				int num4 = k / num3;
				int num5 = (k + 1) / num3;
				int num6 = k - num4;
				if (num4 == m_width)
				{
					num6 -= m_width;
				}
				if (k > 0 && (k - num4) % m_width == 0 && (k + 1 - num5) % m_width == 0)
				{
					num6--;
				}
				m_paintMask[k] = array[num6];
				m_modifiedPaint[k] = array2[num6];
			}
		}
		return true;
	}

	public static TerrainComp FindTerrainCompiler(Vector3 pos)
	{
		foreach (TerrainComp s_instance in s_instances)
		{
			float num = s_instance.m_size / 2f;
			Vector3 position = s_instance.transform.position;
			if (pos.x >= position.x - num && pos.x <= position.x + num && pos.z >= position.z - num && pos.z <= position.z + num)
			{
				return s_instance;
			}
		}
		return null;
	}

	public void ApplyToHeightmap(Texture2D clearedMask, List<float> heights, float[] baseHeights, float[] levelOnlyHeights, Heightmap hm)
	{
		if (!m_initialized)
		{
			return;
		}
		int num = m_width + 1;
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				int num2 = i * num + j;
				float num3 = m_levelDelta[num2];
				float num4 = m_smoothDelta[num2];
				if (num3 != 0f || num4 != 0f)
				{
					float num5 = heights[num2];
					float num6 = baseHeights[num2];
					float value = num5 + num3 + num4;
					value = Mathf.Clamp(value, num6 - 8f, num6 + 8f);
					heights[num2] = value;
				}
			}
		}
		for (int k = 0; k < num; k++)
		{
			for (int l = 0; l < num; l++)
			{
				int num7 = k * num + l;
				if (m_modifiedPaint[num7])
				{
					clearedMask.SetPixel(l, k, m_paintMask[num7]);
				}
			}
		}
	}

	public void ApplyOperation(TerrainOp modifier)
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write(modifier.transform.position);
		modifier.m_settings.Serialize(zPackage);
		m_nview.InvokeRPC("ApplyOperation", zPackage);
	}

	private void RPC_ApplyOperation(long sender, ZPackage pkg)
	{
		if (m_nview.IsOwner())
		{
			TerrainOp.Settings settings = new TerrainOp.Settings();
			Vector3 pos = pkg.ReadVector3();
			settings.Deserialize(pkg);
			DoOperation(pos, settings);
		}
	}

	private void DoOperation(Vector3 pos, TerrainOp.Settings modifier)
	{
		if (m_initialized)
		{
			InternalDoOperation(pos, modifier);
			Save();
			m_hmap.Poke(delayed: false);
			if ((bool)ClutterSystem.instance)
			{
				ClutterSystem.instance.ResetGrass(pos, modifier.GetRadius());
			}
		}
	}

	private void InternalDoOperation(Vector3 pos, TerrainOp.Settings modifier)
	{
		if (modifier.m_level)
		{
			LevelTerrain(pos + Vector3.up * modifier.m_levelOffset, modifier.m_levelRadius, modifier.m_square);
		}
		if (modifier.m_raise)
		{
			RaiseTerrain(pos, modifier.m_raiseRadius, modifier.m_raiseDelta, modifier.m_square, modifier.m_raisePower);
		}
		if (modifier.m_smooth)
		{
			SmoothTerrain(pos + Vector3.up * modifier.m_levelOffset, modifier.m_smoothRadius, modifier.m_square, modifier.m_smoothPower);
		}
		if (modifier.m_paintCleared)
		{
			PaintCleared(pos, modifier.m_paintRadius, modifier.m_paintType, modifier.m_paintHeightCheck, apply: false);
		}
		m_operations++;
		m_lastOpPoint = pos;
		m_lastOpRadius = modifier.GetRadius();
	}

	private void LevelTerrain(Vector3 worldPos, float radius, bool square)
	{
		m_hmap.WorldToVertex(worldPos, out var x, out var y);
		Vector3 vector = worldPos - base.transform.position;
		float num = radius / m_hmap.m_scale;
		int num2 = Mathf.CeilToInt(num);
		int num3 = m_width + 1;
		Vector2 a = new Vector2(x, y);
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				if ((square || !(Vector2.Distance(a, new Vector2(j, i)) > num)) && j >= 0 && i >= 0 && j < num3 && i < num3)
				{
					float height = m_hmap.GetHeight(j, i);
					float num4 = vector.y - height;
					int num5 = i * num3 + j;
					num4 += m_smoothDelta[num5];
					m_smoothDelta[num5] = 0f;
					m_levelDelta[num5] += num4;
					m_levelDelta[num5] = Mathf.Clamp(m_levelDelta[num5], -8f, 8f);
					m_modifiedHeight[num5] = true;
				}
			}
		}
	}

	private void RaiseTerrain(Vector3 worldPos, float radius, float delta, bool square, float power)
	{
		m_hmap.WorldToVertex(worldPos, out var x, out var y);
		Vector3 vector = worldPos - base.transform.position;
		float num = radius / m_hmap.m_scale;
		int num2 = Mathf.CeilToInt(num);
		int num3 = m_width + 1;
		Vector2 a = new Vector2(x, y);
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				if (j < 0 || i < 0 || j >= num3 || i >= num3)
				{
					continue;
				}
				float num4 = 1f;
				if (!square)
				{
					float num5 = Vector2.Distance(a, new Vector2(j, i));
					if (num5 > num)
					{
						continue;
					}
					if (power > 0f)
					{
						num4 = num5 / num;
						num4 = 1f - num4;
						if (power != 1f)
						{
							num4 = Mathf.Pow(num4, power);
						}
					}
				}
				float height = m_hmap.GetHeight(j, i);
				float num6 = delta * num4;
				float num7 = vector.y + num6;
				if (delta < 0f && num7 > height)
				{
					continue;
				}
				if (delta > 0f)
				{
					if (num7 < height)
					{
						continue;
					}
					if (num7 > height + num6)
					{
						num7 = height + num6;
					}
				}
				int num8 = i * num3 + j;
				float num9 = num7 - height + m_smoothDelta[num8];
				m_smoothDelta[num8] = 0f;
				m_levelDelta[num8] += num9;
				m_levelDelta[num8] = Mathf.Clamp(m_levelDelta[num8], -8f, 8f);
				m_modifiedHeight[num8] = true;
			}
		}
	}

	private void SmoothTerrain(Vector3 worldPos, float radius, bool square, float power)
	{
		m_hmap.WorldToVertex(worldPos, out var x, out var y);
		float b = worldPos.y - base.transform.position.y;
		float num = radius / m_hmap.m_scale;
		int num2 = Mathf.CeilToInt(num);
		Vector2 a = new Vector2(x, y);
		int num3 = m_width + 1;
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				float num4 = Vector2.Distance(a, new Vector2(j, i));
				if (!(num4 > num) && j >= 0 && i >= 0 && j < num3 && i < num3)
				{
					float num5 = num4 / num;
					num5 = ((power != 3f) ? Mathf.Pow(num5, power) : (num5 * num5 * num5));
					float height = m_hmap.GetHeight(j, i);
					float t = 1f - num5;
					float num6 = Mathf.Lerp(height, b, t) - height;
					int num7 = i * num3 + j;
					m_smoothDelta[num7] += num6;
					m_smoothDelta[num7] = Mathf.Clamp(m_smoothDelta[num7], -1f, 1f);
					m_modifiedHeight[num7] = true;
				}
			}
		}
	}

	private void PaintCleared(Vector3 worldPos, float radius, TerrainModifier.PaintType paintType, bool heightCheck, bool apply)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		float num = worldPos.y - base.transform.position.y;
		m_hmap.WorldToVertexMask(worldPos, out var x, out var y);
		float num2 = radius / m_hmap.m_scale;
		int num3 = Mathf.CeilToInt(num2);
		Vector2 a = new Vector2(x, y);
		for (int i = y - num3; i <= y + num3; i++)
		{
			for (int j = x - num3; j <= x + num3; j++)
			{
				float num4 = Vector2.Distance(a, new Vector2(j, i));
				int num5 = m_width + 1;
				if (j >= 0 && i >= 0 && j < num5 && i < num5 && (!heightCheck || !(m_hmap.GetHeight(j, i) > num)))
				{
					float f = 1f - Mathf.Clamp01(num4 / num2);
					f = Mathf.Pow(f, 0.1f);
					Color color = m_hmap.GetPaintMask(j, i);
					float a2 = color.a;
					switch (paintType)
					{
					case TerrainModifier.PaintType.Dirt:
						color = Color.Lerp(color, Heightmap.m_paintMaskDirt, f);
						break;
					case TerrainModifier.PaintType.Cultivate:
						color = Color.Lerp(color, Heightmap.m_paintMaskCultivated, f);
						break;
					case TerrainModifier.PaintType.Paved:
						color = Color.Lerp(color, Heightmap.m_paintMaskPaved, f);
						break;
					case TerrainModifier.PaintType.Reset:
						color = Color.Lerp(color, Heightmap.m_paintMaskNothing, f);
						break;
					}
					color.a = a2;
					m_modifiedPaint[i * num5 + j] = true;
					m_paintMask[i * num5 + j] = color;
				}
			}
		}
	}

	public bool IsOwner()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.IsOwner();
	}

	public void UpdatePaintMask(Heightmap hmap)
	{
		for (int i = 0; i < m_width; i++)
		{
			for (int j = 0; j < m_width; j++)
			{
				int num = i * m_width + j;
				if (m_modifiedPaint[num])
				{
					Color color = m_paintMask[num];
					color.a = hmap.GetPaintMask(j, i).a;
					m_paintMask[num] = color;
				}
			}
		}
		Save();
		hmap.Poke(delayed: false);
	}

	public static void UpgradeTerrain()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		List<Heightmap> list = new List<Heightmap>();
		Heightmap.FindHeightmap(Player.m_localPlayer.transform.position, 150f, list);
		bool flag = false;
		foreach (Heightmap item in list)
		{
			if (UpgradeTerrain(item))
			{
				flag = true;
			}
		}
		if (!flag)
		{
			Console.instance.Print("Nothing to optimize");
		}
		else
		{
			Console.instance.Print("Optimized terrain");
		}
	}

	public static bool UpgradeTerrain(Heightmap hmap)
	{
		List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
		int num = 0;
		List<TerrainModifier> list = new List<TerrainModifier>();
		foreach (TerrainModifier item in allInstances)
		{
			ZNetView component = item.GetComponent<ZNetView>();
			if (!(component == null) && component.IsValid() && component.IsOwner() && item.m_playerModifiction)
			{
				if (!hmap.CheckTerrainModIsContained(item))
				{
					num++;
				}
				else
				{
					list.Add(item);
				}
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		TerrainComp andCreateTerrainCompiler = hmap.GetAndCreateTerrainCompiler();
		if (!andCreateTerrainCompiler.IsOwner())
		{
			Console.instance.Print("Skipping terrain at " + hmap.transform.position.ToString() + " ( another player is currently the owner )");
			return false;
		}
		int num2 = andCreateTerrainCompiler.m_width + 1;
		float[] array = new float[andCreateTerrainCompiler.m_modifiedHeight.Length];
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				array[i * num2 + j] = hmap.GetHeight(j, i);
			}
		}
		Color[] array2 = new Color[andCreateTerrainCompiler.m_paintMask.Length];
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < num2; l++)
			{
				array2[k * num2 + l] = hmap.GetPaintMask(l, k);
			}
		}
		foreach (TerrainModifier item2 in list)
		{
			item2.enabled = false;
			item2.GetComponent<ZNetView>().Destroy();
		}
		hmap.Poke(delayed: false);
		int num3 = 0;
		for (int m = 0; m < num2; m++)
		{
			for (int n = 0; n < num2; n++)
			{
				int num4 = m * num2 + n;
				float num5 = array[num4];
				float height = hmap.GetHeight(n, m);
				float num6 = num5 - height;
				if (!(Mathf.Abs(num6) < 0.001f))
				{
					andCreateTerrainCompiler.m_modifiedHeight[num4] = true;
					andCreateTerrainCompiler.m_levelDelta[num4] += num6;
					num3++;
				}
			}
		}
		int num7 = 0;
		for (int num8 = 0; num8 < num2; num8++)
		{
			for (int num9 = 0; num9 < num2; num9++)
			{
				int num10 = num8 * num2 + num9;
				Color color = array2[num10];
				Color paintMask = hmap.GetPaintMask(num9, num8);
				if (!(color == paintMask))
				{
					andCreateTerrainCompiler.m_modifiedPaint[num10] = true;
					andCreateTerrainCompiler.m_paintMask[num10] = color;
					num7++;
				}
			}
		}
		andCreateTerrainCompiler.Save();
		hmap.Poke(delayed: false);
		if ((bool)ClutterSystem.instance)
		{
			ClutterSystem.instance.ResetGrass(hmap.transform.position, (float)hmap.m_width * hmap.m_scale / 2f);
		}
		Console.instance.Print("Operations optimized:" + list.Count + "  height changes:" + num3 + "  paint changes:" + num7);
		return true;
	}
}
