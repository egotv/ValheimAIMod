using System.Collections.Generic;
using UnityEngine;

public class WearNTearUpdater : MonoBehaviour
{
	private int m_index;

	private float m_sleepUntil;

	private const int c_UpdatesPerFrame = 50;

	private const float c_SleepTime = 0.5f;

	public Texture3D m_ashlandsWearTexture;

	private void Update()
	{
		float time = Time.time;
		if (time < m_sleepUntil)
		{
			return;
		}
		Shader.SetGlobalTexture("_AshlandsWearTexture", m_ashlandsWearTexture);
		List<WearNTear> allInstances = WearNTear.GetAllInstances();
		float deltaTime = Time.deltaTime;
		foreach (WearNTear item in allInstances)
		{
			item.UpdateCover(deltaTime);
		}
		int num = m_index;
		for (int i = 0; i < 50; i++)
		{
			if (allInstances.Count == 0)
			{
				break;
			}
			if (num >= allInstances.Count)
			{
				break;
			}
			allInstances[num].UpdateWear(time);
			num++;
		}
		m_index = ((num < allInstances.Count) ? num : 0);
		if (m_index == 0)
		{
			m_sleepUntil = time + 0.5f;
		}
	}
}
