using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class RandomAnimation : MonoBehaviour
{
	[Serializable]
	public class RandomValue
	{
		public string m_name;

		public int m_values;

		public float m_interval;

		public bool m_floatValue;

		public float m_floatTransition = 1f;

		[NonSerialized]
		public float m_timer;

		[NonSerialized]
		public int m_value;

		[NonSerialized]
		public List<int> m_hashValues = new List<int>();
	}

	public List<RandomValue> m_values = new List<RandomValue>();

	private Animator m_anim;

	private ZNetView m_nview;

	private readonly StringBuilder m_sb = new StringBuilder();

	private void Start()
	{
		m_anim = GetComponentInChildren<Animator>();
		m_nview = GetComponent<ZNetView>();
	}

	private void FixedUpdate()
	{
		if (m_nview != null && !m_nview.IsValid())
		{
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		foreach (RandomValue value in m_values)
		{
			m_sb.Clear();
			m_sb.Append("RA_");
			m_sb.Append(value.m_name);
			if (m_nview == null || m_nview.IsOwner())
			{
				value.m_timer += fixedDeltaTime;
				if (value.m_timer > value.m_interval)
				{
					value.m_timer = 0f;
					value.m_value = UnityEngine.Random.Range(0, value.m_values);
					if ((bool)m_nview)
					{
						m_nview.GetZDO().Set(m_sb.ToString(), value.m_value);
					}
					if (!value.m_floatValue)
					{
						m_anim.SetInteger(value.m_name, value.m_value);
					}
				}
			}
			if ((bool)m_nview && !m_nview.IsOwner())
			{
				int @int = m_nview.GetZDO().GetInt(m_sb.ToString());
				if (@int != value.m_value)
				{
					value.m_value = @int;
					if (!value.m_floatValue)
					{
						m_anim.SetInteger(value.m_name, value.m_value);
					}
				}
			}
			if (!value.m_floatValue)
			{
				continue;
			}
			if (value.m_hashValues.Count != value.m_values)
			{
				value.m_hashValues.Resize(value.m_values);
				for (int i = 0; i < value.m_values; i++)
				{
					m_sb.Clear();
					m_sb.Append(value.m_name);
					m_sb.Append(i.ToString());
					value.m_hashValues[i] = ZSyncAnimation.GetHash(m_sb.ToString());
				}
			}
			for (int j = 0; j < value.m_values; j++)
			{
				float @float = m_anim.GetFloat(value.m_hashValues[j]);
				@float = ((j != value.m_value) ? Mathf.MoveTowards(@float, 0f, fixedDeltaTime / value.m_floatTransition) : Mathf.MoveTowards(@float, 1f, fixedDeltaTime / value.m_floatTransition));
				m_anim.SetFloat(value.m_hashValues[j], @float);
			}
		}
	}
}
