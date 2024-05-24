using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class Heightmap : MonoBehaviour, IMonoUpdater
{
	[Flags]
	public enum Biome
	{
		None = 0,
		Meadows = 1,
		Swamp = 2,
		Mountain = 4,
		BlackForest = 8,
		Plains = 0x10,
		AshLands = 0x20,
		DeepNorth = 0x40,
		Ocean = 0x100,
		Mistlands = 0x200,
		All = 0x37F
	}

	[Flags]
	public enum BiomeArea
	{
		Edge = 1,
		Median = 2,
		Everything = 3
	}

	private static readonly Dictionary<Biome, int> s_biomeToIndex = new Dictionary<Biome, int>
	{
		{
			Biome.None,
			0
		},
		{
			Biome.Meadows,
			1
		},
		{
			Biome.Swamp,
			2
		},
		{
			Biome.Mountain,
			3
		},
		{
			Biome.BlackForest,
			4
		},
		{
			Biome.Plains,
			5
		},
		{
			Biome.AshLands,
			6
		},
		{
			Biome.DeepNorth,
			7
		},
		{
			Biome.Ocean,
			8
		},
		{
			Biome.Mistlands,
			9
		}
	};

	private static readonly Biome[] s_indexToBiome = new Biome[10]
	{
		Biome.None,
		Biome.Meadows,
		Biome.Swamp,
		Biome.Mountain,
		Biome.BlackForest,
		Biome.Plains,
		Biome.AshLands,
		Biome.DeepNorth,
		Biome.Ocean,
		Biome.Mistlands
	};

	private static readonly float[] s_tempBiomeWeights = new float[Enum.GetValues(typeof(Biome)).Length];

	public GameObject m_terrainCompilerPrefab;

	public int m_width = 32;

	public float m_scale = 1f;

	public Material m_material;

	public const float c_LevelMaxDelta = 8f;

	public const float c_SmoothMaxDelta = 1f;

	[SerializeField]
	private bool m_isDistantLod;

	private ShadowCastingMode m_shadowMode = ShadowCastingMode.ShadowsOnly;

	private bool m_receiveShadows;

	public bool m_distantLodEditorHax;

	private static readonly List<Heightmap> s_tempHmaps = new List<Heightmap>();

	private readonly List<float> m_heights = new List<float>();

	private HeightmapBuilder.HMBuildData m_buildData;

	private Texture2D m_paintMask;

	private Material m_materialInstance;

	private MeshCollider m_collider;

	private MeshFilter m_meshFilter;

	private MeshRenderer m_meshRenderer;

	private RenderGroupSubscriber m_renderGroupSubscriber;

	private readonly float[] m_oceanDepth = new float[4];

	private Biome[] m_cornerBiomes = new Biome[4]
	{
		Biome.Meadows,
		Biome.Meadows,
		Biome.Meadows,
		Biome.Meadows
	};

	private Bounds m_bounds;

	private BoundingSphere m_boundingSphere;

	private Mesh m_collisionMesh;

	private Mesh m_renderMesh;

	private bool m_doLateUpdate;

	private static readonly List<Heightmap> s_heightmaps = new List<Heightmap>();

	private static readonly List<Vector3> s_tempVertices = new List<Vector3>();

	private static readonly List<Vector2> s_tempUVs = new List<Vector2>();

	private static readonly List<int> s_tempIndices = new List<int>();

	private static readonly List<Color32> s_tempColors = new List<Color32>();

	public static Color m_paintMaskDirt = new Color(1f, 0f, 0f, 1f);

	public static Color m_paintMaskCultivated = new Color(0f, 1f, 0f, 1f);

	public static Color m_paintMaskPaved = new Color(0f, 0f, 1f, 1f);

	public static Color m_paintMaskNothing = new Color(0f, 0f, 0f, 1f);

	public static Color m_paintMaskClearVegetation = new Color(0f, 0f, 0f, 0f);

	private static bool s_enableDistantTerrainShadows = false;

	private static int s_shaderPropertyClearedMaskTex = 0;

	public const RenderGroup c_RenderGroup = RenderGroup.Overworld;

	public bool IsDistantLod
	{
		get
		{
			return m_isDistantLod;
		}
		set
		{
			if (m_isDistantLod != value)
			{
				if (value)
				{
					s_heightmaps.Remove(this);
				}
				else
				{
					s_heightmaps.Add(this);
				}
				m_isDistantLod = value;
				UpdateShadowSettings();
			}
		}
	}

	public static bool EnableDistantTerrainShadows
	{
		get
		{
			return s_enableDistantTerrainShadows;
		}
		set
		{
			if (s_enableDistantTerrainShadows == value)
			{
				return;
			}
			s_enableDistantTerrainShadows = value;
			foreach (Heightmap instance in Instances)
			{
				instance.UpdateShadowSettings();
			}
		}
	}

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	public event Action m_clearConnectedWearNTearCache;

	private void Awake()
	{
		if (!m_isDistantLod)
		{
			s_heightmaps.Add(this);
		}
		if (s_shaderPropertyClearedMaskTex == 0)
		{
			s_shaderPropertyClearedMaskTex = Shader.PropertyToID("_ClearedMaskTex");
		}
		m_collider = GetComponent<MeshCollider>();
		m_meshFilter = GetComponent<MeshFilter>();
		if (!m_meshFilter)
		{
			m_meshFilter = base.gameObject.AddComponent<MeshFilter>();
		}
		m_meshRenderer = GetComponent<MeshRenderer>();
		if (!m_meshRenderer)
		{
			m_meshRenderer = base.gameObject.AddComponent<MeshRenderer>();
		}
		m_meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;
		m_renderGroupSubscriber = GetComponent<RenderGroupSubscriber>();
		if (!m_renderGroupSubscriber)
		{
			m_renderGroupSubscriber = base.gameObject.AddComponent<RenderGroupSubscriber>();
		}
		m_renderGroupSubscriber.Group = RenderGroup.Overworld;
		if (m_material == null)
		{
			base.enabled = false;
		}
		UpdateShadowSettings();
	}

	private void OnDestroy()
	{
		if (!m_isDistantLod)
		{
			s_heightmaps.Remove(this);
		}
		if ((bool)m_materialInstance)
		{
			UnityEngine.Object.DestroyImmediate(m_materialInstance);
		}
		if ((bool)m_collisionMesh)
		{
			UnityEngine.Object.DestroyImmediate(m_collisionMesh);
		}
		if ((bool)m_renderMesh)
		{
			UnityEngine.Object.DestroyImmediate(m_renderMesh);
		}
		if ((bool)m_paintMask)
		{
			UnityEngine.Object.DestroyImmediate(m_paintMask);
		}
	}

	private void OnEnable()
	{
		Instances.Add(this);
		UpdateShadowSettings();
		if (!m_isDistantLod || !Application.isPlaying || m_distantLodEditorHax)
		{
			Regenerate();
		}
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	public void CustomLateUpdate(float deltaTime)
	{
		if (m_doLateUpdate)
		{
			m_doLateUpdate = false;
			Regenerate();
		}
	}

	private void UpdateShadowSettings()
	{
		if (m_isDistantLod)
		{
			m_meshRenderer.shadowCastingMode = (EnableDistantTerrainShadows ? ShadowCastingMode.On : ShadowCastingMode.Off);
			m_meshRenderer.receiveShadows = false;
		}
		else
		{
			m_meshRenderer.shadowCastingMode = (EnableDistantTerrainShadows ? ShadowCastingMode.On : ShadowCastingMode.TwoSided);
			m_meshRenderer.receiveShadows = true;
		}
	}

	public static void ForceGenerateAll()
	{
		foreach (Heightmap s_heightmap in s_heightmaps)
		{
			if (s_heightmap.HaveQueuedRebuild())
			{
				ZLog.Log("Force generating hmap " + s_heightmap.transform.position.ToString());
				s_heightmap.Regenerate();
			}
		}
	}

	public void Poke(bool delayed)
	{
		if (delayed)
		{
			m_doLateUpdate = true;
		}
		else
		{
			Regenerate();
		}
	}

	public bool HaveQueuedRebuild()
	{
		return m_doLateUpdate;
	}

	public void Regenerate()
	{
		m_doLateUpdate = false;
		if (Generate())
		{
			RebuildCollisionMesh();
			UpdateCornerDepths();
			m_materialInstance.SetTexture(s_shaderPropertyClearedMaskTex, m_paintMask);
			RebuildRenderMesh();
			this.m_clearConnectedWearNTearCache?.Invoke();
		}
	}

	private void UpdateCornerDepths()
	{
		float num = 30f;
		m_oceanDepth[0] = GetHeight(0, m_width);
		m_oceanDepth[1] = GetHeight(m_width, m_width);
		m_oceanDepth[2] = GetHeight(m_width, 0);
		m_oceanDepth[3] = GetHeight(0, 0);
		m_oceanDepth[0] = Mathf.Max(0f, (float)((double)num - (double)m_oceanDepth[0]));
		m_oceanDepth[1] = Mathf.Max(0f, (float)((double)num - (double)m_oceanDepth[1]));
		m_oceanDepth[2] = Mathf.Max(0f, (float)((double)num - (double)m_oceanDepth[2]));
		m_oceanDepth[3] = Mathf.Max(0f, (float)((double)num - (double)m_oceanDepth[3]));
		m_materialInstance.SetFloatArray("_depth", m_oceanDepth);
	}

	public float[] GetOceanDepth()
	{
		return m_oceanDepth;
	}

	public float GetOceanDepth(Vector3 worldPos)
	{
		WorldToVertex(worldPos, out var x, out var y);
		float t = (float)((double)x / (double)(float)m_width);
		float t2 = (float)y / (float)m_width;
		float a = DUtils.Lerp(m_oceanDepth[3], m_oceanDepth[2], t);
		float b = DUtils.Lerp(m_oceanDepth[0], m_oceanDepth[1], t);
		return DUtils.Lerp(a, b, t2);
	}

	private void Initialize()
	{
		int num = m_width + 1;
		int num2 = num * num;
		if (m_heights.Count != num2)
		{
			m_heights.Clear();
			for (int i = 0; i < num2; i++)
			{
				m_heights.Add(0f);
			}
			m_paintMask = new Texture2D(num, num);
			m_paintMask.name = "_Heightmap m_paintMask";
			m_paintMask.wrapMode = TextureWrapMode.Clamp;
			m_materialInstance = new Material(m_material);
			m_materialInstance.SetTexture(s_shaderPropertyClearedMaskTex, m_paintMask);
			m_meshRenderer.sharedMaterial = m_materialInstance;
		}
	}

	private bool Generate()
	{
		if (HeightmapBuilder.instance == null)
		{
			return false;
		}
		if (WorldGenerator.instance == null)
		{
			ZLog.LogError("The WorldGenerator instance was null");
			throw new NullReferenceException("The WorldGenerator instance was null");
		}
		Initialize();
		int num = m_width + 1;
		int num2 = num * num;
		Vector3 position = base.transform.position;
		if (m_buildData == null || m_buildData.m_baseHeights.Count != num2 || m_buildData.m_center != position || m_buildData.m_scale != m_scale || m_buildData.m_worldGen != WorldGenerator.instance)
		{
			m_buildData = HeightmapBuilder.instance.RequestTerrainSync(position, m_width, m_scale, m_isDistantLod, WorldGenerator.instance);
			m_cornerBiomes = m_buildData.m_cornerBiomes;
		}
		for (int i = 0; i < num2; i++)
		{
			m_heights[i] = m_buildData.m_baseHeights[i];
		}
		m_paintMask.SetPixels(m_buildData.m_baseMask);
		ApplyModifiers();
		return true;
	}

	private static float Distance(float x, float y, float rx, float ry)
	{
		float num = (float)((double)x - (double)rx);
		float num2 = (float)((double)y - (double)ry);
		float num3 = Mathf.Sqrt((float)((double)num * (double)num + (double)num2 * (double)num2));
		float num4 = (float)(1.4140000343322754 - (double)num3);
		return (float)((double)num4 * (double)num4 * (double)num4);
	}

	public bool HaveBiome(Biome biome)
	{
		if ((m_cornerBiomes[0] & biome) == 0 && (m_cornerBiomes[1] & biome) == 0 && (m_cornerBiomes[2] & biome) == 0)
		{
			return (m_cornerBiomes[3] & biome) != 0;
		}
		return true;
	}

	public Biome GetBiome(Vector3 point, float oceanLevel = 0.02f, bool waterAlwaysOcean = false)
	{
		if (m_isDistantLod || waterAlwaysOcean)
		{
			return WorldGenerator.instance.GetBiome(point.x, point.z, oceanLevel, waterAlwaysOcean);
		}
		if (m_cornerBiomes[0] == m_cornerBiomes[1] && m_cornerBiomes[0] == m_cornerBiomes[2] && m_cornerBiomes[0] == m_cornerBiomes[3])
		{
			return m_cornerBiomes[0];
		}
		float x = point.x;
		float y = point.z;
		WorldToNormalizedHM(point, out x, out y);
		for (int i = 1; i < s_tempBiomeWeights.Length; i++)
		{
			s_tempBiomeWeights[i] = 0f;
		}
		s_tempBiomeWeights[s_biomeToIndex[m_cornerBiomes[0]]] += Distance(x, y, 0f, 0f);
		s_tempBiomeWeights[s_biomeToIndex[m_cornerBiomes[1]]] += Distance(x, y, 1f, 0f);
		s_tempBiomeWeights[s_biomeToIndex[m_cornerBiomes[2]]] += Distance(x, y, 0f, 1f);
		s_tempBiomeWeights[s_biomeToIndex[m_cornerBiomes[3]]] += Distance(x, y, 1f, 1f);
		int num = s_biomeToIndex[Biome.None];
		float num2 = -99999f;
		for (int j = 1; j < s_tempBiomeWeights.Length; j++)
		{
			if (s_tempBiomeWeights[j] > num2)
			{
				num = j;
				num2 = s_tempBiomeWeights[j];
			}
		}
		return s_indexToBiome[num];
	}

	public BiomeArea GetBiomeArea()
	{
		if (!IsBiomeEdge())
		{
			return BiomeArea.Median;
		}
		return BiomeArea.Edge;
	}

	public bool IsBiomeEdge()
	{
		if (m_cornerBiomes[0] == m_cornerBiomes[1] && m_cornerBiomes[0] == m_cornerBiomes[2])
		{
			return m_cornerBiomes[0] != m_cornerBiomes[3];
		}
		return true;
	}

	private void ApplyModifiers()
	{
		List<TerrainModifier> allInstances = TerrainModifier.GetAllInstances();
		float[] array = null;
		float[] array2 = null;
		foreach (TerrainModifier item in allInstances)
		{
			if (item.enabled && TerrainVSModifier(item))
			{
				if (item.m_playerModifiction && array == null)
				{
					array = m_heights.ToArray();
					array2 = m_heights.ToArray();
				}
				ApplyModifier(item, array, array2);
			}
		}
		TerrainComp terrainComp = (m_isDistantLod ? null : TerrainComp.FindTerrainCompiler(base.transform.position));
		if ((bool)terrainComp)
		{
			if (array == null)
			{
				array = m_heights.ToArray();
				array2 = m_heights.ToArray();
			}
			terrainComp.ApplyToHeightmap(m_paintMask, m_heights, array, array2, this);
		}
		m_paintMask.Apply();
	}

	private void ApplyModifier(TerrainModifier modifier, float[] baseHeights, float[] levelOnly)
	{
		if (modifier.m_level)
		{
			LevelTerrain(modifier.transform.position + Vector3.up * modifier.m_levelOffset, modifier.m_levelRadius, modifier.m_square, baseHeights, levelOnly, modifier.m_playerModifiction);
		}
		if (modifier.m_smooth)
		{
			SmoothTerrain2(modifier.transform.position + Vector3.up * modifier.m_levelOffset, modifier.m_smoothRadius, modifier.m_square, levelOnly, modifier.m_smoothPower, modifier.m_playerModifiction);
		}
		if (modifier.m_paintCleared)
		{
			PaintCleared(modifier.transform.position, modifier.m_paintRadius, modifier.m_paintType, modifier.m_paintHeightCheck, apply: false);
		}
	}

	public bool CheckTerrainModIsContained(TerrainModifier modifier)
	{
		Vector3 position = modifier.transform.position;
		float num = modifier.GetRadius() + 0.1f;
		Vector3 position2 = base.transform.position;
		float num2 = (float)m_width * m_scale * 0.5f;
		if (position.x + num > position2.x + num2)
		{
			return false;
		}
		if (position.x - num < position2.x - num2)
		{
			return false;
		}
		if (position.z + num > position2.z + num2)
		{
			return false;
		}
		if (position.z - num < position2.z - num2)
		{
			return false;
		}
		return true;
	}

	public bool TerrainVSModifier(TerrainModifier modifier)
	{
		Vector3 position = modifier.transform.position;
		float num = modifier.GetRadius() + 4f;
		Vector3 position2 = base.transform.position;
		float num2 = (float)m_width * m_scale * 0.5f;
		if (position.x + num < position2.x - num2)
		{
			return false;
		}
		if (position.x - num > position2.x + num2)
		{
			return false;
		}
		if (position.z + num < position2.z - num2)
		{
			return false;
		}
		if (position.z - num > position2.z + num2)
		{
			return false;
		}
		return true;
	}

	private Vector3 CalcVertex(int x, int y)
	{
		int num = m_width + 1;
		return new Vector3((float)((double)m_width * (double)m_scale * -0.5), 0f, (float)((double)m_width * (double)m_scale * -0.5)) + new Vector3(y: m_heights[y * num + x], x: (float)((double)x * (double)m_scale), z: (float)((double)y * (double)m_scale));
	}

	private Color GetBiomeColor(float ix, float iy)
	{
		if (m_cornerBiomes[0] == m_cornerBiomes[1] && m_cornerBiomes[0] == m_cornerBiomes[2] && m_cornerBiomes[0] == m_cornerBiomes[3])
		{
			return GetBiomeColor(m_cornerBiomes[0]);
		}
		Color32 biomeColor = GetBiomeColor(m_cornerBiomes[0]);
		Color32 biomeColor2 = GetBiomeColor(m_cornerBiomes[1]);
		Color32 biomeColor3 = GetBiomeColor(m_cornerBiomes[2]);
		Color32 biomeColor4 = GetBiomeColor(m_cornerBiomes[3]);
		Color32 a = Color32.Lerp(biomeColor, biomeColor2, ix);
		Color32 b = Color32.Lerp(biomeColor3, biomeColor4, ix);
		return Color32.Lerp(a, b, iy);
	}

	public static Color32 GetBiomeColor(Biome biome)
	{
		return biome switch
		{
			Biome.Swamp => new Color32(byte.MaxValue, 0, 0, 0), 
			Biome.Mountain => new Color32(0, byte.MaxValue, 0, 0), 
			Biome.BlackForest => new Color32(0, 0, byte.MaxValue, 0), 
			Biome.Plains => new Color32(0, 0, 0, byte.MaxValue), 
			Biome.AshLands => new Color32(byte.MaxValue, 0, 0, byte.MaxValue), 
			Biome.DeepNorth => new Color32(0, byte.MaxValue, 0, 0), 
			Biome.Mistlands => new Color32(0, 0, byte.MaxValue, byte.MaxValue), 
			_ => new Color32(0, 0, 0, 0), 
		};
	}

	private void RebuildCollisionMesh()
	{
		if (m_collisionMesh == null)
		{
			m_collisionMesh = new Mesh();
			m_collisionMesh.name = "___Heightmap m_collisionMesh";
		}
		int num = m_width + 1;
		float num2 = -999999f;
		float num3 = 999999f;
		s_tempVertices.Clear();
		for (int i = 0; i < num; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector3 item = CalcVertex(j, i);
				s_tempVertices.Add(item);
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
		m_collisionMesh.SetVertices(s_tempVertices);
		int num4 = (num - 1) * (num - 1) * 6;
		if (m_collisionMesh.GetIndexCount(0) != num4)
		{
			s_tempIndices.Clear();
			for (int k = 0; k < num - 1; k++)
			{
				for (int l = 0; l < num - 1; l++)
				{
					int item2 = k * num + l;
					int item3 = k * num + l + 1;
					int item4 = (k + 1) * num + l + 1;
					int item5 = (k + 1) * num + l;
					s_tempIndices.Add(item2);
					s_tempIndices.Add(item5);
					s_tempIndices.Add(item3);
					s_tempIndices.Add(item3);
					s_tempIndices.Add(item5);
					s_tempIndices.Add(item4);
				}
			}
			m_collisionMesh.SetIndices(s_tempIndices.ToArray(), MeshTopology.Triangles, 0);
		}
		if ((bool)m_collider)
		{
			m_collider.sharedMesh = m_collisionMesh;
		}
		float num5 = (float)m_width * m_scale * 0.5f;
		m_bounds.SetMinMax(base.transform.position + new Vector3(0f - num5, num3, 0f - num5), base.transform.position + new Vector3(num5, num2, num5));
		m_boundingSphere.position = m_bounds.center;
		m_boundingSphere.radius = Vector3.Distance(m_boundingSphere.position, m_bounds.max);
	}

	private void RebuildRenderMesh()
	{
		if (m_renderMesh == null)
		{
			m_renderMesh = new Mesh();
			m_renderMesh.name = "___Heightmap m_renderMesh";
		}
		WorldGenerator instance = WorldGenerator.instance;
		int num = m_width + 1;
		Vector3 vector = base.transform.position + new Vector3((float)((double)m_width * (double)m_scale * -0.5), 0f, (float)((double)m_width * (double)m_scale * -0.5));
		s_tempVertices.Clear();
		s_tempUVs.Clear();
		s_tempIndices.Clear();
		s_tempColors.Clear();
		for (int i = 0; i < num; i++)
		{
			float iy = DUtils.SmoothStep(0f, 1f, (float)((double)i / (double)m_width));
			for (int j = 0; j < num; j++)
			{
				float ix = DUtils.SmoothStep(0f, 1f, (float)((double)j / (double)m_width));
				s_tempUVs.Add(new Vector2((float)((double)j / (double)m_width), (float)((double)i / (double)m_width)));
				if (m_isDistantLod)
				{
					float wx = (float)((double)vector.x + (double)j * (double)m_scale);
					float wy = (float)((double)vector.z + (double)i * (double)m_scale);
					Biome biome = instance.GetBiome(wx, wy);
					s_tempColors.Add(GetBiomeColor(biome));
				}
				else
				{
					s_tempColors.Add(GetBiomeColor(ix, iy));
				}
			}
		}
		m_collisionMesh.GetVertices(s_tempVertices);
		m_collisionMesh.GetIndices(s_tempIndices, 0);
		m_renderMesh.Clear();
		m_renderMesh.SetVertices(s_tempVertices);
		m_renderMesh.SetColors(s_tempColors);
		m_renderMesh.SetUVs(0, s_tempUVs);
		m_renderMesh.SetIndices(s_tempIndices, MeshTopology.Triangles, 0);
		m_renderMesh.RecalculateNormals();
		m_renderMesh.RecalculateTangents();
		m_renderMesh.RecalculateBounds();
		m_meshFilter.mesh = m_renderMesh;
	}

	private void SmoothTerrain2(Vector3 worldPos, float radius, bool square, float[] levelOnlyHeights, float power, bool playerModifiction)
	{
		WorldToVertex(worldPos, out var x, out var y);
		float b = (float)(double)(worldPos.y - base.transform.position.y);
		float num = (float)(double)(radius / m_scale);
		int num2 = Mathf.CeilToInt(num);
		Vector2 a = new Vector2(x, y);
		int num3 = m_width + 1;
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				float num4 = Vector2.Distance(a, new Vector2(j, i));
				if (num4 > num)
				{
					continue;
				}
				float num5 = num4 / num;
				if (j >= 0 && i >= 0 && j < num3 && i < num3)
				{
					num5 = ((power != 3f) ? Mathf.Pow(num5, power) : ((float)((double)num5 * (double)num5 * (double)num5)));
					float height = GetHeight(j, i);
					float t = (float)(1.0 - (double)num5);
					float num6 = DUtils.Lerp(height, b, t);
					if (playerModifiction)
					{
						float num7 = levelOnlyHeights[i * num3 + j];
						num6 = Mathf.Clamp(num6, (float)((double)num7 - 1.0), (float)((double)num7 + 1.0));
					}
					SetHeight(j, i, num6);
				}
			}
		}
	}

	private bool AtMaxWorldLevelDepth(Vector3 worldPos)
	{
		GetWorldHeight(worldPos, out var height);
		GetWorldBaseHeight(worldPos, out var height2);
		return Mathf.Max(0f - (float)((double)height - (double)height2), 0f) >= 7.95f;
	}

	private bool GetWorldBaseHeight(Vector3 worldPos, out float height)
	{
		WorldToVertex(worldPos, out var x, out var y);
		int num = m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			height = 0f;
			return false;
		}
		height = (float)((double)m_buildData.m_baseHeights[y * num + x] + (double)base.transform.position.y);
		return true;
	}

	private bool GetWorldHeight(Vector3 worldPos, out float height)
	{
		WorldToVertex(worldPos, out var x, out var y);
		int num = m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			height = 0f;
			return false;
		}
		height = (float)((double)m_heights[y * num + x] + (double)base.transform.position.y);
		return true;
	}

	public static bool AtMaxLevelDepth(Vector3 worldPos)
	{
		Heightmap heightmap = FindHeightmap(worldPos);
		if ((bool)heightmap)
		{
			return heightmap.AtMaxWorldLevelDepth(worldPos);
		}
		return false;
	}

	public static bool GetHeight(Vector3 worldPos, out float height)
	{
		Heightmap heightmap = FindHeightmap(worldPos);
		if ((bool)heightmap && heightmap.GetWorldHeight(worldPos, out height))
		{
			return true;
		}
		height = 0f;
		return false;
	}

	private void PaintCleared(Vector3 worldPos, float radius, TerrainModifier.PaintType paintType, bool heightCheck, bool apply)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		float num = worldPos.y - base.transform.position.y;
		WorldToVertexMask(worldPos, out var x, out var y);
		float num2 = radius / m_scale;
		int num3 = Mathf.CeilToInt(num2);
		Vector2 a = new Vector2(x, y);
		for (int i = y - num3; i <= y + num3; i++)
		{
			for (int j = x - num3; j <= x + num3; j++)
			{
				if (j >= 0 && i >= 0 && j < m_paintMask.width + 1 && i < m_paintMask.height + 1 && (!heightCheck || !(GetHeight(j, i) > num)))
				{
					float num4 = Vector2.Distance(a, new Vector2(j, i));
					float f = 1f - Mathf.Clamp01(num4 / num2);
					f = Mathf.Pow(f, 0.1f);
					Color color = m_paintMask.GetPixel(j, i);
					float a2 = color.a;
					switch (paintType)
					{
					case TerrainModifier.PaintType.Dirt:
						color = Color.Lerp(color, m_paintMaskDirt, f);
						break;
					case TerrainModifier.PaintType.Cultivate:
						color = Color.Lerp(color, m_paintMaskCultivated, f);
						break;
					case TerrainModifier.PaintType.Paved:
						color = Color.Lerp(color, m_paintMaskPaved, f);
						break;
					case TerrainModifier.PaintType.Reset:
						color = Color.Lerp(color, m_paintMaskNothing, f);
						break;
					case TerrainModifier.PaintType.ClearVegetation:
						color = Color.Lerp(color, m_paintMaskClearVegetation, f);
						break;
					}
					if (paintType != TerrainModifier.PaintType.ClearVegetation)
					{
						color.a = a2;
					}
					m_paintMask.SetPixel(j, i, color);
				}
			}
		}
		if (apply)
		{
			m_paintMask.Apply();
		}
	}

	public float GetVegetationMask(Vector3 worldPos)
	{
		worldPos.x -= 0.5f;
		worldPos.z -= 0.5f;
		WorldToVertexMask(worldPos, out var x, out var y);
		return m_paintMask.GetPixel(x, y).a;
	}

	public bool IsCleared(Vector3 worldPos)
	{
		worldPos.x = (float)((double)worldPos.x - 0.5);
		worldPos.z = (float)((double)worldPos.z - 0.5);
		WorldToVertexMask(worldPos, out var x, out var y);
		Color pixel = m_paintMask.GetPixel(x, y);
		if (!(pixel.r > 0.5f) && !(pixel.g > 0.5f))
		{
			return pixel.b > 0.5f;
		}
		return true;
	}

	public bool IsCultivated(Vector3 worldPos)
	{
		WorldToVertexMask(worldPos, out var x, out var y);
		return m_paintMask.GetPixel(x, y).g > 0.5f;
	}

	public bool IsLava(Vector3 worldPos, float lavaValue = 0.6f)
	{
		if (GetBiome(worldPos) != Biome.AshLands || IsBiomeEdge())
		{
			return false;
		}
		if (GetVegetationMask(worldPos) > lavaValue)
		{
			return true;
		}
		return false;
	}

	public float GetLava(Vector3 worldPos)
	{
		if (GetBiome(worldPos) != Biome.AshLands || IsBiomeEdge())
		{
			return 0f;
		}
		return GetVegetationMask(worldPos);
	}

	public float GetHeightOffset(Vector3 worldPos)
	{
		if (GetBiome(worldPos) == Biome.AshLands)
		{
			if (IsBiomeEdge())
			{
				return GetGroundMaterialOffset(FootStep.GroundMaterial.Ashlands);
			}
			float vegetationMask = GetVegetationMask(worldPos);
			return Mathf.Lerp(GetGroundMaterialOffset(FootStep.GroundMaterial.Ashlands), GetGroundMaterialOffset(FootStep.GroundMaterial.Lava), vegetationMask);
		}
		return 0f;
	}

	public void WorldToVertex(Vector3 worldPos, out int x, out int y)
	{
		Vector3 vector = worldPos - base.transform.position;
		int num = m_width / 2;
		x = Mathf.FloorToInt(vector.x / m_scale + 0.5f) + num;
		y = Mathf.FloorToInt(vector.z / m_scale + 0.5f) + num;
	}

	public void WorldToVertexMask(Vector3 worldPos, out int x, out int y)
	{
		Vector3 vector = worldPos - base.transform.position;
		int num = (m_width + 1) / 2;
		x = Mathf.FloorToInt(vector.x / m_scale + 0.5f) + num;
		y = Mathf.FloorToInt(vector.z / m_scale + 0.5f) + num;
	}

	private void WorldToNormalizedHM(Vector3 worldPos, out float x, out float y)
	{
		float num = (float)m_width * m_scale;
		Vector3 vector = worldPos - base.transform.position;
		x = vector.x / num + 0.5f;
		y = vector.z / num + 0.5f;
	}

	private void LevelTerrain(Vector3 worldPos, float radius, bool square, float[] baseHeights, float[] levelOnly, bool playerModifiction)
	{
		WorldToVertexMask(worldPos, out var x, out var y);
		Vector3 vector = worldPos - base.transform.position;
		float num = (float)((double)radius / (double)m_scale);
		int num2 = Mathf.CeilToInt(num);
		int num3 = m_width + 1;
		Vector2 a = new Vector2(x, y);
		for (int i = y - num2; i <= y + num2; i++)
		{
			for (int j = x - num2; j <= x + num2; j++)
			{
				if ((square || !(Vector2.Distance(a, new Vector2(j, i)) > num)) && j >= 0 && i >= 0 && j < num3 && i < num3)
				{
					float num4 = vector.y;
					if (playerModifiction)
					{
						float num5 = baseHeights[i * num3 + j];
						num4 = (levelOnly[i * num3 + j] = Mathf.Clamp(num4, (float)((double)num5 - 8.0), (float)((double)num5 + 8.0)));
					}
					SetHeight(j, i, num4);
				}
			}
		}
	}

	public Color GetPaintMask(int x, int y)
	{
		if (x < 0 || y < 0 || x >= m_paintMask.width || y >= m_paintMask.height)
		{
			return Color.black;
		}
		return m_paintMask.GetPixel(x, y);
	}

	public Texture2D GetPaintMask()
	{
		return m_paintMask;
	}

	private void SetPaintMask(int x, int y, Color paint)
	{
		if (x >= 0 && y >= 0 && x < m_width && y < m_width)
		{
			m_paintMask.SetPixel(x, y, paint);
		}
	}

	public float GetHeight(int x, int y)
	{
		int num = m_width + 1;
		if (x < 0 || y < 0 || x >= num || y >= num)
		{
			return 0f;
		}
		return m_heights[y * num + x];
	}

	public void SetHeight(int x, int y, float h)
	{
		int num = m_width + 1;
		if (x >= 0 && y >= 0 && x < num && y < num)
		{
			m_heights[y * num + x] = h;
		}
	}

	public bool IsPointInside(Vector3 point, float radius = 0f)
	{
		float num = (float)((double)m_width * (double)m_scale * 0.5);
		Vector3 position = base.transform.position;
		if ((float)((double)point.x + (double)radius) >= (float)((double)position.x - (double)num) && (float)((double)point.x - (double)radius) <= (float)((double)position.x + (double)num) && (float)((double)point.z + (double)radius) >= (float)((double)position.z - (double)num) && (float)((double)point.z - (double)radius) <= (float)((double)position.z + (double)num))
		{
			return true;
		}
		return false;
	}

	public static List<Heightmap> GetAllHeightmaps()
	{
		return s_heightmaps;
	}

	public static Heightmap FindHeightmap(Vector3 point)
	{
		foreach (Heightmap s_heightmap in s_heightmaps)
		{
			if (s_heightmap.IsPointInside(point))
			{
				return s_heightmap;
			}
		}
		return null;
	}

	public static void FindHeightmap(Vector3 point, float radius, List<Heightmap> heightmaps)
	{
		foreach (Heightmap s_heightmap in s_heightmaps)
		{
			if (s_heightmap.IsPointInside(point, radius))
			{
				heightmaps.Add(s_heightmap);
			}
		}
	}

	public static Biome FindBiome(Vector3 point)
	{
		Heightmap heightmap = FindHeightmap(point);
		if (!heightmap)
		{
			return Biome.None;
		}
		return heightmap.GetBiome(point);
	}

	public static bool HaveQueuedRebuild(Vector3 point, float radius)
	{
		s_tempHmaps.Clear();
		FindHeightmap(point, radius, s_tempHmaps);
		foreach (Heightmap s_tempHmap in s_tempHmaps)
		{
			if (s_tempHmap.HaveQueuedRebuild())
			{
				return true;
			}
		}
		return false;
	}

	public static void UpdateTerrainAlpha()
	{
		if (Player.m_localPlayer == null)
		{
			return;
		}
		List<Heightmap> list = new List<Heightmap>();
		FindHeightmap(Player.m_localPlayer.transform.position, 150f, list);
		bool flag = false;
		foreach (Heightmap item in list)
		{
			if (UpdateTerrainAlpha(item))
			{
				flag = true;
			}
		}
		if (!flag)
		{
			Console.instance.Print("Nothing to update");
		}
		else
		{
			Console.instance.Print("Updated terrain alpha");
		}
	}

	public static bool UpdateTerrainAlpha(Heightmap hmap)
	{
		HeightmapBuilder.HMBuildData hMBuildData = HeightmapBuilder.instance.RequestTerrainSync(hmap.transform.position, hmap.m_width, hmap.m_scale, hmap.IsDistantLod, WorldGenerator.instance);
		int num = 0;
		for (int i = 0; i < hmap.m_width; i++)
		{
			for (int j = 0; j < hmap.m_width; j++)
			{
				int num2 = i * hmap.m_width + j;
				float a = hMBuildData.m_baseMask[num2].a;
				Color paintMask = hmap.GetPaintMask(j, i);
				if (a != paintMask.a)
				{
					paintMask.a = a;
					hmap.SetPaintMask(j, i, paintMask);
					num++;
				}
			}
		}
		if (num > 0)
		{
			hmap.GetAndCreateTerrainCompiler().UpdatePaintMask(hmap);
		}
		return num > 0;
	}

	public FootStep.GroundMaterial GetGroundMaterial(Vector3 groundNormal, Vector3 point, float lavaValue = 0.6f)
	{
		float num = Mathf.Acos(Mathf.Clamp01(groundNormal.y)) * 57.29578f;
		switch (GetBiome(point))
		{
		case Biome.Mountain:
		case Biome.DeepNorth:
			if (num < 40f && !IsCleared(point))
			{
				return FootStep.GroundMaterial.Snow;
			}
			break;
		case Biome.Swamp:
			if (num < 40f)
			{
				return FootStep.GroundMaterial.Mud;
			}
			break;
		case Biome.Meadows:
		case Biome.BlackForest:
			if (num < 25f)
			{
				return FootStep.GroundMaterial.Grass;
			}
			break;
		case Biome.AshLands:
			if (IsLava(point, lavaValue))
			{
				return FootStep.GroundMaterial.Lava;
			}
			return FootStep.GroundMaterial.Ashlands;
		}
		return FootStep.GroundMaterial.GenericGround;
	}

	public static float GetGroundMaterialOffset(FootStep.GroundMaterial material)
	{
		return material switch
		{
			FootStep.GroundMaterial.Snow => 0.1f, 
			FootStep.GroundMaterial.Ashlands => 0.1f, 
			FootStep.GroundMaterial.Lava => 0.8f, 
			_ => 0f, 
		};
	}

	public static Biome FindBiomeClutter(Vector3 point)
	{
		if ((bool)ZoneSystem.instance && !ZoneSystem.instance.IsZoneLoaded(point))
		{
			return Biome.None;
		}
		Heightmap heightmap = FindHeightmap(point);
		if ((bool)heightmap)
		{
			return heightmap.GetBiome(point);
		}
		return Biome.None;
	}

	public void Clear()
	{
		m_heights.Clear();
		m_paintMask = null;
		m_materialInstance = null;
		m_buildData = null;
		if ((bool)m_collisionMesh)
		{
			m_collisionMesh.Clear();
		}
		if ((bool)m_renderMesh)
		{
			m_renderMesh.Clear();
		}
		if ((bool)m_collider)
		{
			m_collider.sharedMesh = null;
		}
	}

	public TerrainComp GetAndCreateTerrainCompiler()
	{
		TerrainComp terrainComp = TerrainComp.FindTerrainCompiler(base.transform.position);
		if ((bool)terrainComp)
		{
			return terrainComp;
		}
		return UnityEngine.Object.Instantiate(m_terrainCompilerPrefab, base.transform.position, Quaternion.identity).GetComponent<TerrainComp>();
	}
}
