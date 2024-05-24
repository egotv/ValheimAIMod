using System;
using System.Collections.Generic;
using UnityEngine;

public class RenderGroupSystem : MonoBehaviour
{
	public delegate void GroupChangedHandler(bool shouldRender);

	private class RenderGroupState
	{
		private bool active;

		public bool Active
		{
			get
			{
				return active;
			}
			set
			{
				if (active != value)
				{
					active = value;
					this.GroupChanged?.Invoke(active);
				}
			}
		}

		public event GroupChangedHandler GroupChanged;
	}

	private static RenderGroupSystem s_instance;

	private Dictionary<RenderGroup, RenderGroupState> m_renderGroups = new Dictionary<RenderGroup, RenderGroupState>();

	private void Awake()
	{
		if (s_instance != null)
		{
			ZLog.LogError("Instance already set!");
			return;
		}
		s_instance = this;
		foreach (RenderGroup value in Enum.GetValues(typeof(RenderGroup)))
		{
			m_renderGroups.Add(value, new RenderGroupState());
		}
	}

	private void OnDestroy()
	{
		if (s_instance == this)
		{
			s_instance = null;
		}
	}

	private void LateUpdate()
	{
		bool flag = Player.m_localPlayer != null && Player.m_localPlayer.InInterior();
		m_renderGroups[RenderGroup.Always].Active = true;
		m_renderGroups[RenderGroup.Overworld].Active = !flag;
		m_renderGroups[RenderGroup.Interior].Active = flag;
	}

	public static bool Register(RenderGroup group, GroupChangedHandler subscriber)
	{
		if (!s_instance)
		{
			return false;
		}
		RenderGroupState renderGroupState = s_instance.m_renderGroups[group];
		renderGroupState.GroupChanged += subscriber;
		subscriber(renderGroupState.Active);
		return true;
	}

	public static bool Unregister(RenderGroup group, GroupChangedHandler subscriber)
	{
		if (!s_instance)
		{
			return false;
		}
		s_instance.m_renderGroups[group].GroupChanged -= subscriber;
		return true;
	}

	public static bool IsGroupActive(RenderGroup group)
	{
		if (s_instance == null)
		{
			return true;
		}
		return s_instance.m_renderGroups[group].Active;
	}
}
