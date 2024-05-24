using System;

public class AdvancedInputMap
{
	public delegate float ReadAsAnalogueFunc(InputDefinition[] definitions);

	public delegate bool ReadAsDigitalFunc(InputDefinition[] definitions);

	private readonly InputDefinition[] m_definitions;

	private ReadAsAnalogueFunc m_readAsAnalogueFunc;

	private ReadAsDigitalFunc m_readAsDigitalFunc;

	public bool IsDigital => m_readAsDigitalFunc != null;

	public bool IsAnalogue => m_readAsAnalogueFunc != null;

	public AdvancedInputMap(ReadAsAnalogueFunc readFunc)
	{
		if (readFunc == null)
		{
			throw new ArgumentException("readFunc can't be null!");
		}
		m_definitions = null;
		m_readAsAnalogueFunc = readFunc;
		m_readAsDigitalFunc = null;
	}

	public AdvancedInputMap(InputDefinition[] definitions, ReadAsAnalogueFunc readFunc)
	{
		if (readFunc == null)
		{
			throw new ArgumentException("readFunc can't be null!");
		}
		m_definitions = definitions;
		m_readAsAnalogueFunc = readFunc;
		m_readAsDigitalFunc = null;
	}

	public AdvancedInputMap(ReadAsDigitalFunc readFunc)
	{
		if (readFunc == null)
		{
			throw new ArgumentException("readFunc can't be null!");
		}
		m_definitions = null;
		m_readAsAnalogueFunc = null;
		m_readAsDigitalFunc = readFunc;
	}

	public AdvancedInputMap(InputDefinition[] definitions, ReadAsDigitalFunc readFunc)
	{
		if (readFunc == null)
		{
			throw new ArgumentException("readFunc can't be null!");
		}
		m_definitions = definitions;
		m_readAsAnalogueFunc = null;
		m_readAsDigitalFunc = readFunc;
	}

	public AdvancedInputMap(ReadAsAnalogueFunc readAnalogueFunc, ReadAsDigitalFunc readDigitalFunc)
	{
		if (readAnalogueFunc == null)
		{
			throw new ArgumentException("readAnalogueFunc can't be null!");
		}
		if (readAnalogueFunc == null)
		{
			throw new ArgumentException("readDigitalFunc can't be null!");
		}
		m_definitions = null;
		m_readAsAnalogueFunc = readAnalogueFunc;
		m_readAsDigitalFunc = readDigitalFunc;
	}

	public AdvancedInputMap(InputDefinition[] definitions, ReadAsAnalogueFunc readAnalogueFunc, ReadAsDigitalFunc readDigitalFunc)
	{
		if (readAnalogueFunc == null)
		{
			throw new ArgumentException("readAnalogueFunc can't be null!");
		}
		if (readAnalogueFunc == null)
		{
			throw new ArgumentException("readDigitalFunc can't be null!");
		}
		m_definitions = definitions;
		m_readAsAnalogueFunc = readAnalogueFunc;
		m_readAsDigitalFunc = readDigitalFunc;
	}

	public float ReadAsAnalogue(FloatRange outputRange)
	{
		if (m_readAsAnalogueFunc != null)
		{
			return outputRange.NormalizedToRange(m_readAsAnalogueFunc(m_definitions));
		}
		if (!m_readAsDigitalFunc(m_definitions))
		{
			return outputRange.m_min;
		}
		return outputRange.m_max;
	}

	public bool ReadAsDigital(FloatRange outputRange, float deadzone, FloatRange? mappedRange)
	{
		if (m_readAsDigitalFunc != null)
		{
			return m_readAsDigitalFunc(m_definitions);
		}
		float num = outputRange.NormalizedToRange(m_readAsAnalogueFunc(m_definitions));
		if (mappedRange.HasValue)
		{
			return mappedRange.Value.RangeToNormalized(num) > deadzone;
		}
		return num > deadzone;
	}
}
