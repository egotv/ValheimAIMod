using System.Collections.Generic;

public static class MonoUpdatersExtra
{
	public static void UpdateAI(this List<IUpdateAI> container, List<IUpdateAI> source, string profileScope, float deltaTime)
	{
		container.AddRange(source);
		foreach (IUpdateAI item in container)
		{
			item.UpdateAI(deltaTime);
		}
		container.Clear();
	}

	public static void CustomFixedUpdate(this List<IMonoUpdater> container, List<IMonoUpdater> source, string profileScope, float deltaTime)
	{
		container.AddRange(source);
		foreach (IMonoUpdater item in container)
		{
			item.CustomFixedUpdate(deltaTime);
		}
		container.Clear();
	}

	public static void CustomUpdate(this List<IMonoUpdater> container, List<IMonoUpdater> source, string profileScope, float deltaTime, float time)
	{
		container.AddRange(source);
		foreach (IMonoUpdater item in container)
		{
			item.CustomUpdate(deltaTime, time);
		}
		container.Clear();
	}

	public static void CustomLateUpdate(this List<IMonoUpdater> container, List<IMonoUpdater> source, string profileScope, float deltaTime)
	{
		container.AddRange(source);
		foreach (IMonoUpdater item in container)
		{
			item.CustomLateUpdate(deltaTime);
		}
		container.Clear();
	}
}
