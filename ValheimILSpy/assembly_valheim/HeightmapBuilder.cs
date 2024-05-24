using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public class HeightmapBuilder
{
	public class HMBuildData
	{
		public Vector3 m_center;

		public int m_width;

		public float m_scale;

		public bool m_distantLod;

		public bool m_menu;

		public WorldGenerator m_worldGen;

		public Heightmap.Biome[] m_cornerBiomes;

		public List<float> m_baseHeights;

		public Color[] m_baseMask;

		public HMBuildData(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
		{
			m_center = center;
			m_width = width;
			m_scale = scale;
			m_distantLod = distantLod;
			m_worldGen = worldGen;
		}

		public bool IsEqual(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
		{
			if (m_center == center && m_width == width && m_scale == scale && m_distantLod == distantLod)
			{
				return m_worldGen == worldGen;
			}
			return false;
		}
	}

	private static bool hasBeenDisposed;

	private static HeightmapBuilder m_instance;

	private const int m_maxReadyQueue = 16;

	private List<HMBuildData> m_toBuild = new List<HMBuildData>();

	private List<HMBuildData> m_ready = new List<HMBuildData>();

	private Thread m_builder;

	private Mutex m_lock = new Mutex();

	private bool m_stop;

	public static HeightmapBuilder instance
	{
		get
		{
			if (hasBeenDisposed)
			{
				ZLog.LogWarning("Tried to get instance of heightmap builder after heightmap builder has been disposed!");
				return null;
			}
			if (m_instance == null)
			{
				m_instance = new HeightmapBuilder();
			}
			return m_instance;
		}
	}

	private HeightmapBuilder()
	{
		m_instance = this;
		m_builder = new Thread(BuildThread);
		m_builder.Start();
	}

	public void Dispose()
	{
		if (!hasBeenDisposed)
		{
			hasBeenDisposed = true;
			if (m_builder != null)
			{
				ZLog.Log("Stopping build thread");
				m_lock.WaitOne();
				m_stop = true;
				m_lock.ReleaseMutex();
				m_builder.Join();
				m_builder = null;
			}
			if (m_lock != null)
			{
				m_lock.Close();
				m_lock = null;
			}
		}
	}

	private void BuildThread()
	{
		ZLog.Log("Builder started");
		bool flag = false;
		while (!flag)
		{
			m_lock.WaitOne();
			bool num = m_toBuild.Count > 0;
			m_lock.ReleaseMutex();
			if (num)
			{
				m_lock.WaitOne();
				HMBuildData hMBuildData = m_toBuild[0];
				m_lock.ReleaseMutex();
				new Stopwatch().Start();
				Build(hMBuildData);
				m_lock.WaitOne();
				m_toBuild.Remove(hMBuildData);
				m_ready.Add(hMBuildData);
				while (m_ready.Count > 16)
				{
					m_ready.RemoveAt(0);
				}
				m_lock.ReleaseMutex();
			}
			Thread.Sleep(10);
			m_lock.WaitOne();
			flag = m_stop;
			m_lock.ReleaseMutex();
		}
	}

	private void Build(HMBuildData data)
	{
		int num = data.m_width + 1;
		int num2 = num * num;
		Vector3 vector = data.m_center + new Vector3((float)data.m_width * data.m_scale * -0.5f, 0f, (float)data.m_width * data.m_scale * -0.5f);
		WorldGenerator worldGen = data.m_worldGen;
		data.m_cornerBiomes = new Heightmap.Biome[4];
		data.m_cornerBiomes[0] = worldGen.GetBiome(vector.x, vector.z);
		data.m_cornerBiomes[1] = worldGen.GetBiome((float)((double)vector.x + (double)data.m_width * (double)data.m_scale), vector.z);
		data.m_cornerBiomes[2] = worldGen.GetBiome(vector.x, (float)((double)vector.z + (double)data.m_width * (double)data.m_scale));
		data.m_cornerBiomes[3] = worldGen.GetBiome((float)((double)vector.x + (double)data.m_width * (double)data.m_scale), (float)((double)vector.z + (double)data.m_width * (double)data.m_scale));
		Heightmap.Biome biome = data.m_cornerBiomes[0];
		Heightmap.Biome biome2 = data.m_cornerBiomes[1];
		Heightmap.Biome biome3 = data.m_cornerBiomes[2];
		Heightmap.Biome biome4 = data.m_cornerBiomes[3];
		data.m_baseHeights = new List<float>(num * num);
		for (int i = 0; i < num2; i++)
		{
			data.m_baseHeights.Add(0f);
		}
		int num3 = num * num;
		data.m_baseMask = new Color[num3];
		for (int j = 0; j < num3; j++)
		{
			data.m_baseMask[j] = new Color(0f, 0f, 0f, 0f);
		}
		for (int k = 0; k < num; k++)
		{
			float wy = (float)((double)vector.z + (double)k * (double)data.m_scale);
			float t = DUtils.SmoothStep(0f, 1f, (float)((double)k / (double)data.m_width));
			for (int l = 0; l < num; l++)
			{
				float wx = (float)((double)vector.x + (double)l * (double)data.m_scale);
				float t2 = DUtils.SmoothStep(0f, 1f, (float)((double)l / (double)data.m_width));
				float num4 = 0f;
				Color mask = Color.black;
				if (data.m_distantLod)
				{
					Heightmap.Biome biome5 = worldGen.GetBiome(wx, wy);
					num4 = worldGen.GetBiomeHeight(biome5, wx, wy, out mask);
				}
				else if (biome3 == biome && biome2 == biome && biome4 == biome)
				{
					num4 = worldGen.GetBiomeHeight(biome, wx, wy, out mask);
				}
				else
				{
					Color[] array = new Color[4];
					float biomeHeight = worldGen.GetBiomeHeight(biome, wx, wy, out array[0]);
					float biomeHeight2 = worldGen.GetBiomeHeight(biome2, wx, wy, out array[1]);
					float biomeHeight3 = worldGen.GetBiomeHeight(biome3, wx, wy, out array[2]);
					float biomeHeight4 = worldGen.GetBiomeHeight(biome4, wx, wy, out array[3]);
					float a = DUtils.Lerp(biomeHeight, biomeHeight2, t2);
					float b = DUtils.Lerp(biomeHeight3, biomeHeight4, t2);
					num4 = DUtils.Lerp(a, b, t);
					Color a2 = Color.Lerp(array[0], array[1], t2);
					Color b2 = Color.Lerp(array[2], array[3], t2);
					mask = Color.Lerp(a2, b2, t);
				}
				data.m_baseHeights[k * num + l] = num4;
				data.m_baseMask[k * num + l] = mask;
			}
		}
		if (!data.m_distantLod)
		{
			return;
		}
		for (int m = 0; m < 4; m++)
		{
			List<float> list = new List<float>(data.m_baseHeights);
			for (int n = 1; n < num - 1; n++)
			{
				for (int num5 = 1; num5 < num - 1; num5++)
				{
					float num6 = list[n * num + num5];
					float num7 = list[(n - 1) * num + num5];
					float num8 = list[(n + 1) * num + num5];
					float num9 = list[n * num + num5 - 1];
					float num10 = list[n * num + num5 + 1];
					if (Mathf.Abs(num6 - num7) > 10f)
					{
						num6 = (num6 + num7) * 0.5f;
					}
					if (Mathf.Abs(num6 - num8) > 10f)
					{
						num6 = (num6 + num8) * 0.5f;
					}
					if (Mathf.Abs(num6 - num9) > 10f)
					{
						num6 = (num6 + num9) * 0.5f;
					}
					if (Mathf.Abs(num6 - num10) > 10f)
					{
						num6 = (num6 + num10) * 0.5f;
					}
					data.m_baseHeights[n * num + num5] = num6;
				}
			}
		}
	}

	public HMBuildData RequestTerrainSync(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		HMBuildData hMBuildData;
		do
		{
			hMBuildData = RequestTerrain(center, width, scale, distantLod, worldGen);
		}
		while (hMBuildData == null);
		return hMBuildData;
	}

	private HMBuildData RequestTerrain(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		m_lock.WaitOne();
		for (int i = 0; i < m_ready.Count; i++)
		{
			HMBuildData hMBuildData = m_ready[i];
			if (hMBuildData.IsEqual(center, width, scale, distantLod, worldGen))
			{
				m_ready.RemoveAt(i);
				m_lock.ReleaseMutex();
				return hMBuildData;
			}
		}
		for (int j = 0; j < m_toBuild.Count; j++)
		{
			if (m_toBuild[j].IsEqual(center, width, scale, distantLod, worldGen))
			{
				m_lock.ReleaseMutex();
				return null;
			}
		}
		m_toBuild.Add(new HMBuildData(center, width, scale, distantLod, worldGen));
		m_lock.ReleaseMutex();
		return null;
	}

	public bool IsTerrainReady(Vector3 center, int width, float scale, bool distantLod, WorldGenerator worldGen)
	{
		m_lock.WaitOne();
		for (int i = 0; i < m_ready.Count; i++)
		{
			if (m_ready[i].IsEqual(center, width, scale, distantLod, worldGen))
			{
				m_lock.ReleaseMutex();
				return true;
			}
		}
		for (int j = 0; j < m_toBuild.Count; j++)
		{
			if (m_toBuild[j].IsEqual(center, width, scale, distantLod, worldGen))
			{
				m_lock.ReleaseMutex();
				return false;
			}
		}
		m_toBuild.Add(new HMBuildData(center, width, scale, distantLod, worldGen));
		m_lock.ReleaseMutex();
		return false;
	}
}
