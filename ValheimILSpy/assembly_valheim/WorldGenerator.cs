using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class WorldGenerator
{
	public class River
	{
		public Vector2 p0;

		public Vector2 p1;

		public Vector2 center;

		public float widthMin;

		public float widthMax;

		public float curveWidth;

		public float curveWavelength;
	}

	public struct RiverPoint
	{
		public Vector2 p;

		public float w;

		public float w2;

		public RiverPoint(Vector2 p_p, float p_w)
		{
			p = p_p;
			w = p_w;
			w2 = (float)((double)p_w * (double)p_w);
		}
	}

	private const float m_waterTreshold = 0.05f;

	private static WorldGenerator m_instance = null;

	private World m_world;

	private int m_version;

	private float m_offset0;

	private float m_offset1;

	private float m_offset2;

	private float m_offset3;

	private float m_offset4;

	private int m_riverSeed;

	private int m_streamSeed;

	private List<Vector2> m_lakes;

	private List<River> m_rivers = new List<River>();

	private List<River> m_streams = new List<River>();

	private Dictionary<Vector2i, RiverPoint[]> m_riverPoints = new Dictionary<Vector2i, RiverPoint[]>();

	private RiverPoint[] m_cachedRiverPoints;

	private Vector2i m_cachedRiverGrid = new Vector2i(-999999, -999999);

	private ReaderWriterLockSlim m_riverCacheLock = new ReaderWriterLockSlim();

	private List<Heightmap.Biome> m_biomes = new List<Heightmap.Biome>();

	private static FastNoise m_noiseGen;

	private const float c_HeightMultiplier = 200f;

	private const float riverGridSize = 64f;

	private const float minRiverWidth = 60f;

	private const float maxRiverWidth = 100f;

	private const float minRiverCurveWidth = 50f;

	private const float maxRiverCurveWidth = 80f;

	private const float minRiverCurveWaveLength = 50f;

	private const float maxRiverCurveWaveLength = 70f;

	private const int streams = 3000;

	private const float streamWidth = 20f;

	private const float meadowsMaxDistance = 5000f;

	private const float minDeepForestNoise = 0.4f;

	private const float minDeepForestDistance = 600f;

	private const float maxDeepForestDistance = 6000f;

	private const float deepForestForestFactorMax = 0.9f;

	private const float marshBiomeScale = 0.001f;

	private const float minMarshNoise = 0.6f;

	private const float minMarshDistance = 2000f;

	private float maxMarshDistance = 6000f;

	private const float minMarshHeight = 0.05f;

	private const float maxMarshHeight = 0.25f;

	private const float heathBiomeScale = 0.001f;

	private const float minHeathNoise = 0.4f;

	private const float minHeathDistance = 3000f;

	private const float maxHeathDistance = 8000f;

	private const float darklandBiomeScale = 0.001f;

	private float minDarklandNoise = 0.4f;

	private const float minDarklandDistance = 6000f;

	private const float maxDarklandDistance = 10000f;

	private const float oceanBiomeScale = 0.0005f;

	private const float oceanBiomeMinNoise = 0.4f;

	private const float oceanBiomeMaxNoise = 0.6f;

	private const float oceanBiomeMinDistance = 1000f;

	private const float oceanBiomeMinDistanceBuffer = 256f;

	private float m_minMountainDistance = 1000f;

	private const float mountainBaseHeightMin = 0.4f;

	private const float deepNorthMinDistance = 12000f;

	private const float deepNorthYOffset = 4000f;

	public static readonly float ashlandsMinDistance = 12000f;

	public static readonly float ashlandsYOffset = -4000f;

	public const float worldSize = 10000f;

	public const float waterEdge = 10500f;

	public static WorldGenerator instance => m_instance;

	public static void Initialize(World world)
	{
		m_instance = new WorldGenerator(world);
	}

	public static void Deitialize()
	{
		m_instance = null;
	}

	private WorldGenerator(World world)
	{
		m_world = world;
		m_version = m_world.m_worldGenVersion;
		VersionSetup(m_version);
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(m_world.m_seed);
		if (m_noiseGen == null)
		{
			m_noiseGen = new FastNoise(m_world.m_seed);
			m_noiseGen.SetNoiseType(FastNoise.NoiseType.Cellular);
			m_noiseGen.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Euclidean);
			m_noiseGen.SetCellularReturnType(FastNoise.CellularReturnType.Distance);
			m_noiseGen.SetFractalOctaves(2);
		}
		m_offset0 = UnityEngine.Random.Range(-10000, 10000);
		m_offset1 = UnityEngine.Random.Range(-10000, 10000);
		m_offset2 = UnityEngine.Random.Range(-10000, 10000);
		m_offset3 = UnityEngine.Random.Range(-10000, 10000);
		m_riverSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
		m_streamSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
		m_offset4 = UnityEngine.Random.Range(-10000, 10000);
		if (!m_world.m_menu)
		{
			Pregenerate();
		}
		UnityEngine.Random.state = state;
	}

	private void VersionSetup(int version)
	{
		ZLog.Log("Worldgenerator version setup:" + version);
		if (version <= 0)
		{
			m_minMountainDistance = 1500f;
		}
		if (version <= 1)
		{
			minDarklandNoise = 0.5f;
			maxMarshDistance = 8000f;
		}
	}

	private void Pregenerate()
	{
		FindLakes();
		m_rivers = PlaceRivers();
		m_streams = PlaceStreams();
	}

	public List<Vector2> GetLakes()
	{
		return m_lakes;
	}

	public List<River> GetRivers()
	{
		return m_rivers;
	}

	public List<River> GetStreams()
	{
		return m_streams;
	}

	private void FindLakes()
	{
		DateTime now = DateTime.Now;
		List<Vector2> list = new List<Vector2>();
		for (float num = -10000f; num <= 10000f; num = (float)((double)num + 128.0))
		{
			for (float num2 = -10000f; num2 <= 10000f; num2 = (float)((double)num2 + 128.0))
			{
				if (!(new Vector2(num2, num).magnitude > 10000f) && GetBaseHeight(num2, num, menuTerrain: false) < 0.05f)
				{
					list.Add(new Vector2(num2, num));
				}
			}
		}
		m_lakes = MergePoints(list, 800f);
		_ = DateTime.Now - now;
	}

	private List<Vector2> MergePoints(List<Vector2> points, float range)
	{
		List<Vector2> list = new List<Vector2>();
		while (points.Count > 0)
		{
			Vector2 vector = points[0];
			points.RemoveAt(0);
			while (points.Count > 0)
			{
				int num = FindClosest(points, vector, range);
				if (num == -1)
				{
					break;
				}
				vector = (vector + points[num]) * 0.5f;
				points[num] = points[points.Count - 1];
				points.RemoveAt(points.Count - 1);
			}
			list.Add(vector);
		}
		return list;
	}

	private int FindClosest(List<Vector2> points, Vector2 p, float maxDistance)
	{
		int result = -1;
		float num = 99999f;
		for (int i = 0; i < points.Count; i++)
		{
			if (!(points[i] == p))
			{
				float num2 = Vector2.Distance(p, points[i]);
				if (num2 < maxDistance && num2 < num)
				{
					result = i;
					num = num2;
				}
			}
		}
		return result;
	}

	private List<River> PlaceStreams()
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(m_streamSeed);
		List<River> list = new List<River>();
		int num = 0;
		DateTime now = DateTime.Now;
		for (int i = 0; i < 3000; i++)
		{
			if (FindStreamStartPoint(100, 26f, 31f, out var p, out var _) && FindStreamEndPoint(100, 36f, 44f, p, 80f, 200f, out var end))
			{
				Vector2 center = (p + end) * 0.5f;
				float pregenerationHeight = GetPregenerationHeight(center.x, center.y);
				if (!(pregenerationHeight < 26f) && !(pregenerationHeight > 44f))
				{
					River river = new River();
					river.p0 = p;
					river.p1 = end;
					river.center = center;
					river.widthMax = 20f;
					river.widthMin = 20f;
					float num2 = Vector2.Distance(river.p0, river.p1);
					river.curveWidth = (float)((double)num2 / 15.0);
					river.curveWavelength = (float)((double)num2 / 20.0);
					list.Add(river);
					num++;
				}
			}
		}
		RenderRivers(list);
		UnityEngine.Random.state = state;
		_ = DateTime.Now - now;
		return list;
	}

	private bool FindStreamEndPoint(int iterations, float minHeight, float maxHeight, Vector2 start, float minLength, float maxLength, out Vector2 end)
	{
		float num = (float)(((double)maxLength - (double)minLength) / (double)iterations);
		float num2 = maxLength;
		for (int i = 0; i < iterations; i++)
		{
			num2 = (float)((double)num2 - (double)num);
			float f = UnityEngine.Random.Range(0f, (float)Math.PI * 2f);
			Vector2 vector = start + new Vector2(Mathf.Sin(f), Mathf.Cos(f)) * num2;
			float pregenerationHeight = GetPregenerationHeight(vector.x, vector.y);
			if (pregenerationHeight > minHeight && pregenerationHeight < maxHeight)
			{
				end = vector;
				return true;
			}
		}
		end = Vector2.zero;
		return false;
	}

	private bool FindStreamStartPoint(int iterations, float minHeight, float maxHeight, out Vector2 p, out float starth)
	{
		for (int i = 0; i < iterations; i++)
		{
			float num = UnityEngine.Random.Range(-10000f, 10000f);
			float num2 = UnityEngine.Random.Range(-10000f, 10000f);
			float pregenerationHeight = GetPregenerationHeight(num, num2);
			if (pregenerationHeight > minHeight && pregenerationHeight < maxHeight)
			{
				p = new Vector2(num, num2);
				starth = pregenerationHeight;
				return true;
			}
		}
		p = Vector2.zero;
		starth = 0f;
		return false;
	}

	private List<River> PlaceRivers()
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(m_riverSeed);
		DateTime now = DateTime.Now;
		List<River> list = new List<River>();
		List<Vector2> list2 = new List<Vector2>(m_lakes);
		while (list2.Count > 1)
		{
			Vector2 vector = list2[0];
			int num = FindRandomRiverEnd(list, m_lakes, vector, 2000f, 0.4f, 128f);
			if (num == -1 && !HaveRiver(list, vector))
			{
				num = FindRandomRiverEnd(list, m_lakes, vector, 5000f, 0.4f, 128f);
			}
			if (num != -1)
			{
				River river = new River();
				river.p0 = vector;
				river.p1 = m_lakes[num];
				river.center = (river.p0 + river.p1) * 0.5f;
				river.widthMax = UnityEngine.Random.Range(60f, 100f);
				river.widthMin = UnityEngine.Random.Range(60f, river.widthMax);
				float num2 = Vector2.Distance(river.p0, river.p1);
				river.curveWidth = (float)((double)num2 / 15.0);
				river.curveWavelength = (float)((double)num2 / 20.0);
				list.Add(river);
			}
			else
			{
				list2.RemoveAt(0);
			}
		}
		RenderRivers(list);
		_ = DateTime.Now - now;
		UnityEngine.Random.state = state;
		return list;
	}

	private int FindClosestRiverEnd(List<River> rivers, List<Vector2> points, Vector2 p, float maxDistance, float heightLimit, float checkStep)
	{
		int result = -1;
		float num = 99999f;
		for (int i = 0; i < points.Count; i++)
		{
			if (!(points[i] == p))
			{
				float num2 = Vector2.Distance(p, points[i]);
				if (num2 < maxDistance && num2 < num && !HaveRiver(rivers, p, points[i]) && IsRiverAllowed(p, points[i], checkStep, heightLimit))
				{
					result = i;
					num = num2;
				}
			}
		}
		return result;
	}

	private int FindRandomRiverEnd(List<River> rivers, List<Vector2> points, Vector2 p, float maxDistance, float heightLimit, float checkStep)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < points.Count; i++)
		{
			if (!(points[i] == p) && Vector2.Distance(p, points[i]) < maxDistance && !HaveRiver(rivers, p, points[i]) && IsRiverAllowed(p, points[i], checkStep, heightLimit))
			{
				list.Add(i);
			}
		}
		if (list.Count == 0)
		{
			return -1;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private bool HaveRiver(List<River> rivers, Vector2 p0)
	{
		foreach (River river in rivers)
		{
			if (river.p0 == p0 || river.p1 == p0)
			{
				return true;
			}
		}
		return false;
	}

	private bool HaveRiver(List<River> rivers, Vector2 p0, Vector2 p1)
	{
		foreach (River river in rivers)
		{
			if ((river.p0 == p0 && river.p1 == p1) || (river.p0 == p1 && river.p1 == p0))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsRiverAllowed(Vector2 p0, Vector2 p1, float step, float heightLimit)
	{
		float num = Vector2.Distance(p0, p1);
		Vector2 normalized = (p1 - p0).normalized;
		bool flag = true;
		for (float num2 = step; num2 <= (float)((double)num - (double)step); num2 = (float)((double)num2 + (double)step))
		{
			Vector2 vector = p0 + normalized * num2;
			float baseHeight = GetBaseHeight(vector.x, vector.y, menuTerrain: false);
			if (baseHeight > heightLimit)
			{
				return false;
			}
			if (baseHeight > 0.05f)
			{
				flag = false;
			}
		}
		if (flag)
		{
			return false;
		}
		return true;
	}

	private void RenderRivers(List<River> rivers)
	{
		DateTime now = DateTime.Now;
		Dictionary<Vector2i, List<RiverPoint>> dictionary = new Dictionary<Vector2i, List<RiverPoint>>();
		foreach (River river in rivers)
		{
			float num = (float)((double)river.widthMin / 8.0);
			Vector2 normalized = (river.p1 - river.p0).normalized;
			Vector2 vector = new Vector2(0f - normalized.y, normalized.x);
			float num2 = Vector2.Distance(river.p0, river.p1);
			for (float num3 = 0f; num3 <= num2; num3 = (float)((double)num3 + (double)num))
			{
				float num4 = (float)((double)num3 / (double)river.curveWavelength);
				float num5 = (float)(Math.Sin(num4) * Math.Sin((double)num4 * 0.634119987487793) * Math.Sin((double)num4 * 0.3341200053691864) * (double)river.curveWidth);
				float r = UnityEngine.Random.Range(river.widthMin, river.widthMax);
				Vector2 p = river.p0 + normalized * num3 + vector * num5;
				AddRiverPoint(dictionary, p, r, river);
			}
		}
		foreach (KeyValuePair<Vector2i, List<RiverPoint>> item in dictionary)
		{
			if (m_riverPoints.TryGetValue(item.Key, out var value))
			{
				List<RiverPoint> list = new List<RiverPoint>(value);
				list.AddRange(item.Value);
				m_riverPoints[item.Key] = list.ToArray();
			}
			else
			{
				RiverPoint[] value2 = item.Value.ToArray();
				m_riverPoints.Add(item.Key, value2);
			}
		}
		_ = DateTime.Now - now;
	}

	private void AddRiverPoint(Dictionary<Vector2i, List<RiverPoint>> riverPoints, Vector2 p, float r, River river)
	{
		Vector2i riverGrid = GetRiverGrid(p.x, p.y);
		int num = Mathf.CeilToInt((float)((double)r / 64.0));
		for (int i = riverGrid.y - num; i <= riverGrid.y + num; i++)
		{
			for (int j = riverGrid.x - num; j <= riverGrid.x + num; j++)
			{
				Vector2i grid = new Vector2i(j, i);
				if (InsideRiverGrid(grid, p, r))
				{
					AddRiverPoint(riverPoints, grid, p, r, river);
				}
			}
		}
	}

	private void AddRiverPoint(Dictionary<Vector2i, List<RiverPoint>> riverPoints, Vector2i grid, Vector2 p, float r, River river)
	{
		if (riverPoints.TryGetValue(grid, out var value))
		{
			value.Add(new RiverPoint(p, r));
			return;
		}
		value = new List<RiverPoint>();
		value.Add(new RiverPoint(p, r));
		riverPoints.Add(grid, value);
	}

	public bool InsideRiverGrid(Vector2i grid, Vector2 p, float r)
	{
		Vector2 vector = new Vector2((float)((double)grid.x * 64.0), (float)((double)grid.y * 64.0));
		Vector2 vector2 = p - vector;
		if (Math.Abs(vector2.x) < (float)((double)r + 32.0))
		{
			return Math.Abs(vector2.y) < (float)((double)r + 32.0);
		}
		return false;
	}

	public Vector2i GetRiverGrid(float wx, float wy)
	{
		int x = Mathf.FloorToInt((float)(((double)wx + 32.0) / 64.0));
		int y = Mathf.FloorToInt((float)(((double)wy + 32.0) / 64.0));
		return new Vector2i(x, y);
	}

	private void GetRiverWeight(float wx, float wy, out float weight, out float width)
	{
		Vector2i riverGrid = GetRiverGrid(wx, wy);
		m_riverCacheLock.EnterReadLock();
		if (riverGrid == m_cachedRiverGrid)
		{
			if (m_cachedRiverPoints != null)
			{
				GetWeight(m_cachedRiverPoints, wx, wy, out weight, out width);
				m_riverCacheLock.ExitReadLock();
			}
			else
			{
				weight = 0f;
				width = 0f;
				m_riverCacheLock.ExitReadLock();
			}
			return;
		}
		m_riverCacheLock.ExitReadLock();
		if (m_riverPoints.TryGetValue(riverGrid, out var value))
		{
			GetWeight(value, wx, wy, out weight, out width);
			m_riverCacheLock.EnterWriteLock();
			m_cachedRiverGrid = riverGrid;
			m_cachedRiverPoints = value;
			m_riverCacheLock.ExitWriteLock();
		}
		else
		{
			m_riverCacheLock.EnterWriteLock();
			m_cachedRiverGrid = riverGrid;
			m_cachedRiverPoints = null;
			m_riverCacheLock.ExitWriteLock();
			weight = 0f;
			width = 0f;
		}
	}

	private void GetWeight(RiverPoint[] points, float wx, float wy, out float weight, out float width)
	{
		Vector2 vector = new Vector2(wx, wy);
		weight = 0f;
		width = 0f;
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < points.Length; i++)
		{
			RiverPoint riverPoint = points[i];
			float num3 = Vector2.SqrMagnitude(riverPoint.p - vector);
			if (num3 < riverPoint.w2)
			{
				float num4 = (float)Math.Sqrt(num3);
				float num5 = (float)(1.0 - (double)num4 / (double)riverPoint.w);
				if (num5 > weight)
				{
					weight = num5;
				}
				num = (float)((double)num + (double)riverPoint.w * (double)num5);
				num2 = (float)((double)num2 + (double)num5);
			}
		}
		if (num2 > 0f)
		{
			width = (float)((double)num / (double)num2);
		}
	}

	private void GenerateBiomes()
	{
		m_biomes = new List<Heightmap.Biome>();
		int num = 400000000;
		for (int i = 0; i < num; i++)
		{
			m_biomes[i] = Heightmap.Biome.Meadows;
		}
	}

	public Heightmap.BiomeArea GetBiomeArea(Vector3 point)
	{
		Heightmap.Biome biome = GetBiome(point);
		Heightmap.Biome biome2 = GetBiome(point - new Vector3(-64f, 0f, -64f));
		Heightmap.Biome biome3 = GetBiome(point - new Vector3(64f, 0f, -64f));
		Heightmap.Biome biome4 = GetBiome(point - new Vector3(64f, 0f, 64f));
		Heightmap.Biome biome5 = GetBiome(point - new Vector3(-64f, 0f, 64f));
		Heightmap.Biome biome6 = GetBiome(point - new Vector3(-64f, 0f, 0f));
		Heightmap.Biome biome7 = GetBiome(point - new Vector3(64f, 0f, 0f));
		Heightmap.Biome biome8 = GetBiome(point - new Vector3(0f, 0f, -64f));
		Heightmap.Biome biome9 = GetBiome(point - new Vector3(0f, 0f, 64f));
		if (biome == biome2 && biome == biome3 && biome == biome4 && biome == biome5 && biome == biome6 && biome == biome7 && biome == biome8 && biome == biome9)
		{
			return Heightmap.BiomeArea.Median;
		}
		return Heightmap.BiomeArea.Edge;
	}

	public Heightmap.Biome GetBiome(Vector3 point)
	{
		return GetBiome(point.x, point.z);
	}

	public static bool IsAshlands(float x, float y)
	{
		double num = (double)WorldAngle(x, y) * 100.0;
		return (double)DUtils.Length(x, (float)((double)y + (double)ashlandsYOffset)) > (double)ashlandsMinDistance + num;
	}

	public static float GetAshlandsOceanGradient(float x, float y)
	{
		double num = (double)WorldAngle(x, y + ashlandsYOffset) * 100.0;
		return (float)(((double)DUtils.Length(x, y + ashlandsYOffset) - ((double)ashlandsMinDistance + num)) / 300.0);
	}

	public static float GetAshlandsOceanGradient(Vector2 pos)
	{
		return GetAshlandsOceanGradient(pos.x, pos.y);
	}

	public static float GetAshlandsOceanGradient(Vector3 pos)
	{
		return GetAshlandsOceanGradient(pos.x, pos.z);
	}

	public static bool IsDeepnorth(float x, float y)
	{
		float num = (float)((double)WorldAngle(x, y) * 100.0);
		return new Vector2(x, (float)((double)y + 4000.0)).magnitude > (float)(12000.0 + (double)num);
	}

	public Heightmap.Biome GetBiome(float wx, float wy, float oceanLevel = 0.02f, bool waterAlwaysOcean = false)
	{
		if (m_world.m_menu)
		{
			if (GetBaseHeight(wx, wy, menuTerrain: true) >= 0.4f)
			{
				return Heightmap.Biome.Mountain;
			}
			return Heightmap.Biome.BlackForest;
		}
		float num = DUtils.Length(wx, wy);
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		float num2 = (float)((double)WorldAngle(wx, wy) * 100.0);
		if (waterAlwaysOcean && GetHeight(wx, wy) <= oceanLevel)
		{
			return Heightmap.Biome.Ocean;
		}
		if (IsAshlands(wx, wy))
		{
			return Heightmap.Biome.AshLands;
		}
		if (!waterAlwaysOcean && baseHeight <= oceanLevel)
		{
			return Heightmap.Biome.Ocean;
		}
		if (IsDeepnorth(wx, wy))
		{
			if (baseHeight > 0.4f)
			{
				return Heightmap.Biome.Mountain;
			}
			return Heightmap.Biome.DeepNorth;
		}
		if (baseHeight > 0.4f)
		{
			return Heightmap.Biome.Mountain;
		}
		if (DUtils.PerlinNoise((double)(float)((double)m_offset0 + (double)wx) * 0.0010000000474974513, (double)(float)((double)m_offset0 + (double)wy) * 0.0010000000474974513) > 0.6f && num > 2000f && num < maxMarshDistance && baseHeight > 0.05f && baseHeight < 0.25f)
		{
			return Heightmap.Biome.Swamp;
		}
		if (DUtils.PerlinNoise((double)(float)((double)m_offset4 + (double)wx) * 0.0010000000474974513, (double)(float)((double)m_offset4 + (double)wy) * 0.0010000000474974513) > minDarklandNoise && num > (float)(6000.0 + (double)num2) && num < 10000f)
		{
			return Heightmap.Biome.Mistlands;
		}
		if (DUtils.PerlinNoise((double)(float)((double)m_offset1 + (double)wx) * 0.0010000000474974513, (double)(float)((double)m_offset1 + (double)wy) * 0.0010000000474974513) > 0.4f && num > (float)(3000.0 + (double)num2) && num < 8000f)
		{
			return Heightmap.Biome.Plains;
		}
		if (DUtils.PerlinNoise((double)(float)((double)m_offset2 + (double)wx) * 0.0010000000474974513, (double)(float)((double)m_offset2 + (double)wy) * 0.0010000000474974513) > 0.4f && num > (float)(600.0 + (double)num2) && num < 6000f)
		{
			return Heightmap.Biome.BlackForest;
		}
		if (num > (float)(5000.0 + (double)num2))
		{
			return Heightmap.Biome.BlackForest;
		}
		return Heightmap.Biome.Meadows;
	}

	public static float WorldAngle(float wx, float wy)
	{
		return (float)Math.Sin((float)((double)(float)Math.Atan2(wx, wy) * 20.0));
	}

	private float GetBaseHeight(float wx, float wy, bool menuTerrain)
	{
		if (menuTerrain)
		{
			double num = wx;
			double num2 = wy;
			num += 100000.0 + (double)m_offset0;
			num2 += 100000.0 + (double)m_offset1;
			float num3 = 0f;
			num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.0020000000949949026 * 0.5, num2 * 0.0020000000949949026 * 0.5) * (double)DUtils.PerlinNoise(num * 0.003000000026077032 * 0.5, num2 * 0.003000000026077032 * 0.5) * 1.0);
			num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.0020000000949949026 * 1.0, num2 * 0.0020000000949949026 * 1.0) * (double)DUtils.PerlinNoise(num * 0.003000000026077032 * 1.0, num2 * 0.003000000026077032 * 1.0) * (double)num3 * 0.8999999761581421);
			num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.004999999888241291 * 1.0, num2 * 0.004999999888241291 * 1.0) * (double)DUtils.PerlinNoise(num * 0.009999999776482582 * 1.0, num2 * 0.009999999776482582 * 1.0) * 0.5 * (double)num3);
			return (float)((double)num3 - 0.07000000029802322);
		}
		float num4 = DUtils.Length(wx, wy);
		double num5 = wx;
		double num6 = wy;
		num5 += 100000.0 + (double)m_offset0;
		num6 += 100000.0 + (double)m_offset1;
		float num7 = 0f;
		num7 = (float)((double)num7 + (double)DUtils.PerlinNoise(num5 * 0.0020000000949949026 * 0.5, num6 * 0.0020000000949949026 * 0.5) * (double)DUtils.PerlinNoise(num5 * 0.003000000026077032 * 0.5, num6 * 0.003000000026077032 * 0.5) * 1.0);
		num7 = (float)((double)num7 + (double)DUtils.PerlinNoise(num5 * 0.0020000000949949026 * 1.0, num6 * 0.0020000000949949026 * 1.0) * (double)DUtils.PerlinNoise(num5 * 0.003000000026077032 * 1.0, num6 * 0.003000000026077032 * 1.0) * (double)num7 * 0.8999999761581421);
		num7 = (float)((double)num7 + (double)DUtils.PerlinNoise(num5 * 0.004999999888241291 * 1.0, num6 * 0.004999999888241291 * 1.0) * (double)DUtils.PerlinNoise(num5 * 0.009999999776482582 * 1.0, num6 * 0.009999999776482582 * 1.0) * 0.5 * (double)num7);
		num7 = (float)((double)num7 - 0.07000000029802322);
		float num8 = DUtils.PerlinNoise(num5 * 0.0020000000949949026 * 0.25 + 0.12300000339746475, num6 * 0.0020000000949949026 * 0.25 + 0.15123000741004944);
		float num9 = DUtils.PerlinNoise(num5 * 0.0020000000949949026 * 0.25 + 0.32100000977516174, num6 * 0.0020000000949949026 * 0.25 + 0.23100000619888306);
		float v = Mathf.Abs((float)((double)num8 - (double)num9));
		float num10 = (float)(1.0 - (double)DUtils.LerpStep(0.02f, 0.12f, v));
		num10 = (float)((double)num10 * (double)DUtils.SmoothStep(744f, 1000f, num4));
		num7 = (float)((double)num7 * (1.0 - (double)num10));
		if (num4 > 10000f)
		{
			float t = DUtils.LerpStep(10000f, 10500f, num4);
			num7 = DUtils.Lerp(num7, -0.2f, t);
			float num11 = 10490f;
			if (num4 > num11)
			{
				float t2 = Utils.LerpStep(num11, 10500f, num4);
				num7 = DUtils.Lerp(num7, -2f, t2);
			}
			return num7;
		}
		if (num4 < m_minMountainDistance && num7 > 0.28f)
		{
			float t3 = (float)DUtils.Clamp01(((double)num7 - 0.2800000011920929) / 0.09999999403953552);
			num7 = DUtils.Lerp(DUtils.Lerp(0.28f, 0.38f, t3), num7, DUtils.LerpStep((float)((double)m_minMountainDistance - 400.0), m_minMountainDistance, num4));
		}
		return num7;
	}

	private float AddRivers(float wx, float wy, float h)
	{
		GetRiverWeight(wx, wy, out var weight, out var width);
		if (weight <= 0f)
		{
			return h;
		}
		float t = DUtils.LerpStep(20f, 60f, width);
		float num = DUtils.Lerp(0.14f, 0.12f, t);
		float num2 = DUtils.Lerp(0.139f, 0.128f, t);
		if (h > num)
		{
			h = DUtils.Lerp(h, num, weight);
		}
		if (h > num2)
		{
			float t2 = DUtils.LerpStep(0.85f, 1f, weight);
			h = DUtils.Lerp(h, num2, t2);
		}
		return h;
	}

	public float GetHeight(float wx, float wy)
	{
		Heightmap.Biome biome = GetBiome(wx, wy);
		Color mask;
		return GetBiomeHeight(biome, wx, wy, out mask);
	}

	public float GetHeight(float wx, float wy, out Color mask)
	{
		Heightmap.Biome biome = GetBiome(wx, wy);
		return GetBiomeHeight(biome, wx, wy, out mask);
	}

	public float GetPregenerationHeight(float wx, float wy)
	{
		Heightmap.Biome biome = GetBiome(wx, wy);
		Color mask;
		return GetBiomeHeight(biome, wx, wy, out mask, preGeneration: true);
	}

	public float GetBiomeHeight(Heightmap.Biome biome, float wx, float wy, out Color mask, bool preGeneration = false)
	{
		float num = ((!preGeneration) ? ((float)((double)GetHeightMultiplier() * CreateAshlandsGap(wx, wy) * CreateDeepNorthGap(wx, wy))) : GetHeightMultiplier());
		mask = Color.black;
		if (m_world.m_menu)
		{
			if (biome == Heightmap.Biome.Mountain)
			{
				return (float)((double)GetSnowMountainHeight(wx, wy, menu: true) * (double)num);
			}
			return (float)((double)GetMenuHeight(wx, wy) * (double)num);
		}
		if (DUtils.Length(wx, wy) > 10500f)
		{
			return -2f * GetHeightMultiplier();
		}
		switch (biome)
		{
		case Heightmap.Biome.Swamp:
			return (float)((double)GetMarshHeight(wx, wy) * (double)num);
		case Heightmap.Biome.DeepNorth:
			return (float)((double)GetDeepNorthHeight(wx, wy) * (double)num);
		case Heightmap.Biome.Mountain:
			return (float)((double)GetSnowMountainHeight(wx, wy, menu: false) * (double)num);
		case Heightmap.Biome.BlackForest:
			return (float)((double)GetForestHeight(wx, wy) * (double)num);
		case Heightmap.Biome.Ocean:
			return (float)((double)GetOceanHeight(wx, wy) * (double)num);
		case Heightmap.Biome.AshLands:
			if (preGeneration)
			{
				return (float)((double)GetAshlandsHeightPregenerate(wx, wy) * (double)num);
			}
			return (float)((double)GetAshlandsHeight(wx, wy, out mask) * (double)num);
		case Heightmap.Biome.Plains:
			return (float)((double)GetPlainsHeight(wx, wy) * (double)num);
		case Heightmap.Biome.Meadows:
			return (float)((double)GetMeadowsHeight(wx, wy) * (double)num);
		case Heightmap.Biome.Mistlands:
			if (preGeneration)
			{
				return (float)((double)GetForestHeight(wx, wy) * (double)num);
			}
			return (float)((double)GetMistlandsHeight(wx, wy, out mask) * (double)num);
		default:
			return 0f;
		}
	}

	private float GetMarshHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float num = 0.137f;
		wx = (float)((double)wx + 100000.0);
		wy = (float)((double)wy + 100000.0);
		double num2 = wx;
		double num3 = wy;
		float num4 = (float)((double)DUtils.PerlinNoise(num2 * 0.03999999910593033, num3 * 0.03999999910593033) * (double)DUtils.PerlinNoise(num2 * 0.07999999821186066, num3 * 0.07999999821186066));
		num = (float)((double)num + (double)num4 * 0.029999999329447746);
		num = AddRivers(wx2, wy2, num);
		num = (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.10000000149011612, num3 * 0.10000000149011612) * 0.009999999776482582);
		return (float)((double)num + (double)DUtils.PerlinNoise(num2 * 0.4000000059604645, num3 * 0.4000000059604645) * 0.003000000026077032);
	}

	private float GetMeadowsHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx = (float)((double)wx + 100000.0 + (double)m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)m_offset3);
		double num = wx;
		double num2 = wy;
		float num3 = (float)((double)DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164));
		num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * (double)num3 * 0.5);
		float num4 = baseHeight;
		num4 = (float)((double)num4 + (double)num3 * 0.10000000149011612);
		float num5 = 0.15f;
		float num6 = (float)((double)num4 - (double)num5);
		float num7 = (float)DUtils.Clamp01((double)baseHeight / 0.4000000059604645);
		if (num6 > 0f)
		{
			num4 = (float)((double)num4 - (double)num6 * ((1.0 - (double)num7) * 0.75));
		}
		num4 = AddRivers(wx2, wy2, num4);
		num4 = (float)((double)num4 + (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * 0.009999999776482582);
		return (float)((double)num4 + (double)DUtils.PerlinNoise(num * 0.4000000059604645, num2 * 0.4000000059604645) * 0.003000000026077032);
	}

	private float GetForestHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx = (float)((double)wx + 100000.0 + (double)m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)m_offset3);
		double num = wx;
		double num2 = wy;
		float num3 = (float)((double)DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164));
		num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * (double)num3 * 0.5);
		baseHeight = (float)((double)baseHeight + (double)num3 * 0.10000000149011612);
		baseHeight = AddRivers(wx2, wy2, baseHeight);
		baseHeight = (float)((double)baseHeight + (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * 0.009999999776482582);
		return (float)((double)baseHeight + (double)DUtils.PerlinNoise(num * 0.4000000059604645, num2 * 0.4000000059604645) * 0.003000000026077032);
	}

	private float GetMistlandsHeight(float wx, float wy, out Color mask)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx = (float)((double)wx + 100000.0 + (double)m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)m_offset3);
		double num = wx;
		double num2 = wy;
		float num3 = DUtils.PerlinNoise(num * 0.019999999552965164 * 0.699999988079071, num2 * 0.019999999552965164 * 0.699999988079071) * DUtils.PerlinNoise(num * 0.03999999910593033 * 0.699999988079071, num2 * 0.03999999910593033 * 0.699999988079071);
		num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.029999999329447746 * 0.699999988079071, num2 * 0.029999999329447746 * 0.699999988079071) * (double)DUtils.PerlinNoise(num * 0.05000000074505806 * 0.699999988079071, num2 * 0.05000000074505806 * 0.699999988079071) * (double)num3 * 0.5);
		num3 = ((num3 > 0f) ? ((float)Math.Pow(num3, 1.5)) : num3);
		baseHeight = (float)((double)baseHeight + (double)num3 * 0.4000000059604645);
		baseHeight = AddRivers(wx2, wy2, baseHeight);
		float num4 = (float)DUtils.Clamp01((double)num3 * 7.0);
		baseHeight = (float)((double)baseHeight + (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * 0.029999999329447746 * (double)num4);
		baseHeight = (float)((double)baseHeight + (double)DUtils.PerlinNoise(num * 0.4000000059604645, num2 * 0.4000000059604645) * 0.009999999776482582 * (double)num4);
		float num5 = (float)(1.0 - (double)num4 * 1.2000000476837158);
		num5 = (float)((double)num5 - (1.0 - (double)DUtils.LerpStep(0.1f, 0.3f, num4)));
		float a = (float)((double)baseHeight + (double)DUtils.PerlinNoise(num * 0.4000000059604645, num2 * 0.4000000059604645) * 0.0020000000949949026);
		float num6 = baseHeight;
		num6 = (float)((double)num6 * 400.0);
		num6 = Mathf.Ceil(num6);
		num6 = (float)((double)num6 / 400.0);
		baseHeight = DUtils.Lerp(a, num6, num4);
		mask = new Color(0f, 0f, 0f, num5);
		return baseHeight;
	}

	private float GetPlainsHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx = (float)((double)wx + 100000.0 + (double)m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)m_offset3);
		double num = wx;
		double num2 = wy;
		float num3 = (float)((double)DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164));
		num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * (double)num3 * 0.5);
		float num4 = baseHeight;
		num4 = (float)((double)num4 + (double)num3 * 0.10000000149011612);
		float num5 = 0.15f;
		float num6 = num4 - num5;
		float num7 = (float)DUtils.Clamp01((double)baseHeight / 0.4000000059604645);
		if (num6 > 0f)
		{
			num4 = (float)((double)num4 - (double)num6 * (1.0 - (double)num7) * 0.75);
		}
		num4 = AddRivers(wx2, wy2, num4);
		num4 = (float)((double)num4 + (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * 0.009999999776482582);
		return (float)((double)num4 + (double)DUtils.PerlinNoise(num * 0.4000000059604645, num2 * 0.4000000059604645) * 0.003000000026077032);
	}

	private float GetMenuHeight(float wx, float wy)
	{
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: true);
		wx = (float)((double)wx + 100000.0 + (double)m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)m_offset3);
		double num = wx;
		double num2 = wy;
		float num3 = DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164);
		num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * (double)num3 * 0.5);
		return (float)((double)(float)((double)(float)((double)baseHeight + (double)num3 * 0.10000000149011612) + (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * 0.009999999776482582) + (double)DUtils.PerlinNoise(num * 0.4000000059604645, num2 * 0.4000000059604645) * 0.003000000026077032);
	}

	private float GetAshlandsHeightPregenerate(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx = (float)((double)wx + 100000.0 + (double)m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)m_offset3);
		double num = wx;
		double num2 = wy;
		float num3 = (float)((double)DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164));
		num3 = (float)((double)num3 + (double)DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * (double)num3 * 0.5);
		baseHeight = (float)((double)baseHeight + (double)num3 * 0.10000000149011612);
		baseHeight = (float)((double)baseHeight + 0.10000000149011612);
		baseHeight = (float)((double)baseHeight + (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * 0.009999999776482582);
		baseHeight = (float)((double)baseHeight + (double)DUtils.PerlinNoise(num * 0.4000000059604645, num2 * 0.4000000059604645) * 0.003000000026077032);
		return AddRivers(wx2, wy2, baseHeight);
	}

	public float GetAshlandsHeight(float wx, float wy, out Color mask, bool cheap = false)
	{
		double num = wx;
		double num2 = wy;
		double a = GetBaseHeight((float)num, (float)num2, menuTerrain: false);
		double num3 = (double)WorldAngle((float)num, (float)num2) * 100.0;
		double value = DUtils.Length(num, num2 + (double)ashlandsYOffset - (double)ashlandsYOffset * 0.3) - ((double)ashlandsMinDistance + num3);
		value = Math.Abs(value) / 1000.0;
		value = 1.0 - DUtils.Clamp01(value);
		value = DUtils.MathfLikeSmoothStep(0.1, 1.0, value);
		double num4 = Math.Abs(num);
		num4 = 1.0 - DUtils.Clamp01(num4 / 7500.0);
		value *= num4;
		double num5 = DUtils.Length(num, num2) - 10150.0;
		num5 = 1.0 - DUtils.Clamp01(num5 / 600.0);
		num += (double)(100000f + m_offset3);
		num2 += (double)(100000f + m_offset3);
		double num6 = 0.0;
		double num7 = 1.0;
		double num8 = 0.33000001311302185;
		int num9 = (cheap ? 2 : 5);
		for (int i = 0; i < num9; i++)
		{
			num6 += num7 * DUtils.MathfLikeSmoothStep(0.0, 1.0, m_noiseGen.GetCellular(num * num8, num2 * num8));
			num8 *= 2.0;
			num7 *= 0.5;
		}
		num6 = DUtils.Remap(num6, -1.0, 1.0, 0.0, 1.0);
		double num10 = DUtils.Lerp(value, DUtils.BlendOverlay(value, num6), 0.5);
		double num11 = DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164);
		num11 += (double)(DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612)) * num11 * 0.5;
		double num12 = DUtils.Lerp(a, 0.15000000596046448, 0.75);
		num12 += num10 * 0.5;
		num12 = DUtils.Lerp(-1.0, num12, DUtils.MathfLikeSmoothStep(0.0, 1.0, num5));
		double num13 = 0.15;
		double num14 = 0.0;
		double num15 = 1.0;
		double num16 = 8.0;
		int num17 = (cheap ? 2 : 3);
		for (int j = 0; j < num17; j++)
		{
			num14 += num15 * m_noiseGen.GetCellular(num * num16, num2 * num16);
			num16 *= 2.0;
			num15 *= 0.5;
		}
		num14 = DUtils.Remap(num14, -1.0, 1.0, 0.0, 1.0);
		num14 = DUtils.Clamp01(Math.Pow(num14, 4.0) * 2.0);
		double simplexFractal = m_noiseGen.GetSimplexFractal(num * 0.075, num2 * 0.075);
		simplexFractal = DUtils.Remap(simplexFractal, -1.0, 1.0, 0.0, 1.0);
		simplexFractal = Math.Pow(simplexFractal, 1.399999976158142);
		num12 *= simplexFractal;
		double num18 = DUtils.Fbm(new Vector2((float)(num * 0.009999999776482582), (float)(num2 * 0.009999999776482582)), 3, 2.0, 0.5);
		num18 *= DUtils.Clamp01(DUtils.Remap(value, 0.0, 0.5, 0.5, 1.0));
		num18 = DUtils.LerpStep(0.699999988079071, 1.0, num18);
		num18 = Math.Pow(num18, 2.0);
		double num19 = DUtils.BlendOverlay(num18, num14);
		num19 *= DUtils.Clamp01((num12 - num13 - 0.02) / 0.01);
		double x = DUtils.PerlinNoise(num * 0.05 + 5124.0, num2 * 0.05 + 5000.0);
		x = Math.Pow(x, 2.0);
		x = DUtils.Remap(x, 0.0, 1.0, 0.009999999776482582, 0.054999999701976776);
		double b = Mathf.Clamp((float)(num12 - x), (float)(num13 + 0.009999999776482582), 5000f);
		num12 = DUtils.Lerp(num12, b, num19);
		mask = new Color(0f, 0f, 0f, (float)num19);
		return (float)num12;
	}

	private float GetEdgeHeight(float wx, float wy)
	{
		float num = DUtils.Length(wx, wy);
		float num2 = 10490f;
		if (num > num2)
		{
			float num3 = DUtils.LerpStep(num2, 10500f, num);
			return (float)(-2.0 * (double)num3);
		}
		float t = DUtils.LerpStep(10000f, 10100f, num);
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		baseHeight = DUtils.Lerp(baseHeight, 0f, t);
		return AddRivers(wx, wy, baseHeight);
	}

	private float GetOceanHeight(float wx, float wy)
	{
		return GetBaseHeight(wx, wy, menuTerrain: false);
	}

	private float BaseHeightTilt(float wx, float wy)
	{
		float baseHeight = GetBaseHeight((float)((double)wx - 1.0), wy, menuTerrain: false);
		float baseHeight2 = GetBaseHeight((float)((double)wx + 1.0), wy, menuTerrain: false);
		float baseHeight3 = GetBaseHeight(wx, (float)((double)wy - 1.0), menuTerrain: false);
		float baseHeight4 = GetBaseHeight(wx, (float)((double)wy + 1.0), menuTerrain: false);
		return (float)((double)Mathf.Abs((float)((double)baseHeight2 - (double)baseHeight)) + (double)Mathf.Abs((float)((double)baseHeight3 - (double)baseHeight4)));
	}

	private float GetSnowMountainHeight(float wx, float wy, bool menu)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menu);
		float num = BaseHeightTilt(wx, wy);
		wx = (float)((double)wx + 100000.0 + (double)m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)m_offset3);
		double num2 = wx;
		double num3 = wy;
		float num4 = (float)((double)baseHeight - 0.4000000059604645);
		baseHeight = (float)((double)baseHeight + (double)num4);
		float num5 = (float)((double)DUtils.PerlinNoise(num2 * 0.009999999776482582, num3 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num2 * 0.019999999552965164, num3 * 0.019999999552965164));
		num5 = (float)((double)num5 + (double)DUtils.PerlinNoise(num2 * 0.05000000074505806, num3 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num2 * 0.10000000149011612, num3 * 0.10000000149011612) * (double)num5 * 0.5);
		baseHeight = (float)((double)baseHeight + (double)num5 * 0.20000000298023224);
		baseHeight = AddRivers(wx2, wy2, baseHeight);
		baseHeight = (float)((double)baseHeight + (double)DUtils.PerlinNoise(num2 * 0.10000000149011612, num3 * 0.10000000149011612) * 0.009999999776482582);
		baseHeight = (float)((double)baseHeight + (double)DUtils.PerlinNoise(num2 * 0.4000000059604645, num3 * 0.4000000059604645) * 0.003000000026077032);
		return (float)((double)baseHeight + (double)DUtils.PerlinNoise(num2 * 0.20000000298023224, num3 * 0.20000000298023224) * 2.0 * (double)num);
	}

	private float GetDeepNorthHeight(float wx, float wy)
	{
		float wx2 = wx;
		float wy2 = wy;
		float baseHeight = GetBaseHeight(wx, wy, menuTerrain: false);
		wx = (float)((double)wx + 100000.0 + (double)m_offset3);
		wy = (float)((double)wy + 100000.0 + (double)m_offset3);
		double num = wx;
		double num2 = wy;
		float num3 = Mathf.Max(0f, (float)((double)baseHeight - 0.4000000059604645));
		baseHeight = (float)((double)baseHeight + (double)num3);
		float num4 = (float)((double)DUtils.PerlinNoise(num * 0.009999999776482582, num2 * 0.009999999776482582) * (double)DUtils.PerlinNoise(num * 0.019999999552965164, num2 * 0.019999999552965164));
		num4 = (float)((double)num4 + (double)DUtils.PerlinNoise(num * 0.05000000074505806, num2 * 0.05000000074505806) * (double)DUtils.PerlinNoise(num * 0.10000000149011612, num2 * 0.10000000149011612) * (double)num4 * 0.5);
		baseHeight = (float)((double)baseHeight + (double)num4 * 0.20000000298023224);
		baseHeight = (float)((double)baseHeight * 1.2000000476837158);
		baseHeight = AddRivers(wx2, wy2, baseHeight);
		baseHeight = (float)((double)baseHeight + (double)DUtils.PerlinNoise(wx * 0.1f, wy * 0.1f) * 0.009999999776482582);
		return (float)((double)baseHeight + (double)DUtils.PerlinNoise(wx * 0.4f, wy * 0.4f) * 0.003000000026077032);
	}

	private double CreateAshlandsGap(float wx, float wy)
	{
		double num = (double)WorldAngle(wx, wy) * 100.0;
		double value = (double)DUtils.Length(wx, wy + ashlandsYOffset) - ((double)ashlandsMinDistance + num);
		value = DUtils.Clamp01(Math.Abs(value) / 400.0);
		return DUtils.MathfLikeSmoothStep(0.0, 1.0, (float)value);
	}

	private double CreateDeepNorthGap(float wx, float wy)
	{
		double num = (double)WorldAngle(wx, wy) * 100.0;
		double value = (double)DUtils.Length(wx, wy + 4000f) - (12000.0 + num);
		value = DUtils.Clamp01(Math.Abs(value) / 400.0);
		return DUtils.MathfLikeSmoothStep(0.0, 1.0, (float)value);
	}

	public static bool InForest(Vector3 pos)
	{
		return GetForestFactor(pos) < 1.15f;
	}

	public static float GetForestFactor(Vector3 pos)
	{
		float num = 0.4f;
		return DUtils.Fbm(pos * 0.01f * num, 3, 1.6f, 0.7f);
	}

	public void GetTerrainDelta(Vector3 center, float radius, out float delta, out Vector3 slopeDirection)
	{
		int num = 10;
		float num2 = -999999f;
		float num3 = 999999f;
		Vector3 vector = center;
		Vector3 vector2 = center;
		for (int i = 0; i < num; i++)
		{
			Vector2 vector3 = UnityEngine.Random.insideUnitCircle * radius;
			Vector3 vector4 = center + new Vector3(vector3.x, 0f, vector3.y);
			float height = GetHeight(vector4.x, vector4.z);
			if (height < num3)
			{
				num3 = height;
				vector2 = vector4;
			}
			if (height > num2)
			{
				num2 = height;
				vector = vector4;
			}
		}
		delta = (float)((double)num2 - (double)num3);
		slopeDirection = Vector3.Normalize(vector2 - vector);
	}

	public int GetSeed()
	{
		return m_world.m_seed;
	}

	public static float GetHeightMultiplier()
	{
		return 200f;
	}
}
