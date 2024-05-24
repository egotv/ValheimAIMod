using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowUpdater : MonoBehaviour
{
	private const int m_updatesPerFrame = 100;

	private void Awake()
	{
		StartCoroutine("UpdateLoop");
	}

	private IEnumerator UpdateLoop()
	{
		while (true)
		{
			List<SlowUpdate> instances = SlowUpdate.GetAllInstaces();
			int index = 0;
			while (index < instances.Count)
			{
				for (int i = 0; i < 100; i++)
				{
					if (instances.Count == 0)
					{
						break;
					}
					if (index >= instances.Count)
					{
						break;
					}
					instances[index].SUpdate();
					int num = index + 1;
					index = num;
				}
				yield return null;
			}
			yield return new WaitForSeconds(0.1f);
		}
	}
}
