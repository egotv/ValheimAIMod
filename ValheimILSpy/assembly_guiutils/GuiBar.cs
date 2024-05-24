using UnityEngine;
using UnityEngine.UI;

public class GuiBar : MonoBehaviour
{
	public RectTransform m_bar;

	public bool m_smoothDrain;

	public bool m_smoothFill;

	public float m_smoothSpeed = 1f;

	public float m_changeDelay;

	private float m_value = 1f;

	private float m_maxValue = 1f;

	private float m_width;

	private float m_smoothValue = 1f;

	private float m_delayTimer;

	private bool m_firstSet = true;

	private Image m_barImage;

	private Color m_originalColor;

	private void Awake()
	{
		m_width = m_bar.sizeDelta.x;
		m_barImage = m_bar.GetComponent<Image>();
		if ((bool)m_barImage)
		{
			m_originalColor = m_barImage.color;
		}
	}

	private void OnEnable()
	{
		m_delayTimer = 0f;
		m_smoothValue = Mathf.Clamp01(m_value / m_maxValue);
	}

	public void SetWidth(float width)
	{
		if (m_width != width)
		{
			m_width = width;
			m_bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_width * m_smoothValue);
		}
	}

	public void SetValue(float value)
	{
		if (m_value == value && !m_firstSet)
		{
			return;
		}
		if (m_firstSet)
		{
			m_firstSet = false;
			m_width = m_bar.sizeDelta.x;
			m_value = value;
			m_smoothValue = Mathf.Clamp01(m_value / m_maxValue);
			SetBar(m_smoothValue);
		}
		else
		{
			if ((value < m_value && m_smoothDrain) || (value > m_value && m_smoothFill))
			{
				m_delayTimer = m_changeDelay;
			}
			m_value = value;
		}
	}

	public float GetSmoothValue()
	{
		return m_smoothValue;
	}

	public void SetMaxValue(float value)
	{
		if (m_maxValue != value)
		{
			float num = m_smoothValue * m_maxValue;
			m_maxValue = value;
			m_smoothValue = Mathf.Clamp01(num / m_maxValue);
		}
	}

	private void LateUpdate()
	{
		m_delayTimer -= Time.deltaTime;
		if (m_delayTimer <= 0f)
		{
			float num = Mathf.Clamp01(m_value / m_maxValue);
			if (num > m_smoothValue && m_smoothFill)
			{
				m_smoothValue = Mathf.Clamp01(Mathf.MoveTowards(m_smoothValue, num, m_smoothSpeed * Time.deltaTime));
			}
			else if (num < m_smoothValue && m_smoothDrain)
			{
				m_smoothValue = Mathf.Clamp01(Mathf.MoveTowards(m_smoothValue, num, m_smoothSpeed * Time.deltaTime));
			}
			else
			{
				m_smoothValue = num;
			}
			SetBar(m_smoothValue);
		}
	}

	private void SetBar(float i)
	{
		m_bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_width * i);
	}

	public void SetColor(Color color)
	{
		if ((bool)m_barImage)
		{
			m_barImage.color = color;
		}
	}

	public Color GetColor()
	{
		if ((bool)m_barImage)
		{
			return m_barImage.color;
		}
		return Color.white;
	}

	public void ResetColor()
	{
		if ((bool)m_barImage)
		{
			m_barImage.color = m_originalColor;
		}
	}
}
