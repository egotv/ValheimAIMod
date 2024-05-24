using UnityEngine;

public struct FloatRange
{
	public float m_min;

	public float m_max;

	public bool IsNegative => m_min > m_max;

	public FloatRange(float min, float max)
	{
		m_min = min;
		m_max = max;
	}

	public float RangeToNormalized(float value)
	{
		return (value - m_min) / (m_max - m_min);
	}

	public float RangeToNormalized(float value, bool clamp)
	{
		float num = RangeToNormalized(value);
		if (clamp)
		{
			num = Mathf.Clamp01(num);
		}
		return num;
	}

	public float NormalizedToRange(float value)
	{
		return m_min + value * (m_max - m_min);
	}

	public float NormalizedToRange(float value, bool clamp)
	{
		float num = NormalizedToRange(value);
		if (clamp)
		{
			num = ((!IsNegative) ? Mathf.Clamp(num, m_min, m_max) : Mathf.Clamp(num, m_max, m_min));
		}
		return num;
	}

	public static float Remap(float value, FloatRange inputRange, FloatRange outputRange)
	{
		return outputRange.NormalizedToRange(inputRange.RangeToNormalized(value));
	}

	public static float Remap(float value, FloatRange inputRange, FloatRange outputRange, bool clamp = false)
	{
		return outputRange.NormalizedToRange(inputRange.RangeToNormalized(value), clamp);
	}

	public override bool Equals(object obj)
	{
		if (obj is FloatRange floatRange && m_min == floatRange.m_min)
		{
			return m_max == floatRange.m_max;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (1703365192 * -1521134295 + m_min.GetHashCode()) * -1521134295 + m_max.GetHashCode();
	}

	public static bool operator ==(FloatRange left, FloatRange right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(FloatRange left, FloatRange right)
	{
		return !(left == right);
	}
}
