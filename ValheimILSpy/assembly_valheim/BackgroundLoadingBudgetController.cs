using System.Collections.Generic;
using UnityEngine;

public class BackgroundLoadingBudgetController
{
	private const ThreadPriority c_defaultBudget = ThreadPriority.Low;

	private static List<ThreadPriority> m_budgetRequests = new List<ThreadPriority>();

	[RuntimeInitializeOnLoadMethod]
	private static void OnLoad()
	{
		ApplyBudget();
	}

	public static ThreadPriority RequestLoadingBudget(ThreadPriority priority)
	{
		AddRequest(priority);
		ApplyBudget();
		return priority;
	}

	public static ThreadPriority UpdateLoadingBudgetRequest(ThreadPriority oldPriority, ThreadPriority newPriority)
	{
		RemoveRequest(oldPriority);
		AddRequest(newPriority);
		ApplyBudget();
		return newPriority;
	}

	public static void ReleaseLoadingBudgetRequest(ThreadPriority priority)
	{
		RemoveRequest(priority);
		ApplyBudget();
	}

	private static void AddRequest(ThreadPriority priority)
	{
		int num = m_budgetRequests.BinarySearch(priority);
		if (num < 0)
		{
			num = ~num;
		}
		m_budgetRequests.Insert(num, priority);
	}

	private static void RemoveRequest(ThreadPriority priority)
	{
		int num = m_budgetRequests.BinarySearch(priority);
		if (num >= 0)
		{
			m_budgetRequests.RemoveAt(num);
		}
		else
		{
			ZLog.LogError($"Failed to remove loading budget request {priority}");
		}
	}

	private static void ApplyBudget()
	{
		ThreadPriority threadPriority2 = (Application.backgroundLoadingPriority = ((m_budgetRequests.Count > 0 && m_budgetRequests[m_budgetRequests.Count - 1] >= ThreadPriority.Low) ? m_budgetRequests[m_budgetRequests.Count - 1] : ThreadPriority.Low));
		ZLog.Log($"Set background loading budget to {threadPriority2}");
	}
}
