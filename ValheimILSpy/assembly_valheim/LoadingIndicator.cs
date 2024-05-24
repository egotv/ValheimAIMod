using UnityEngine;
using UnityEngine.UI;

public class LoadingIndicator : MonoBehaviour
{
	public static LoadingIndicator s_instance;

	[SerializeField]
	public bool m_showProgressIndicator = true;

	[SerializeField]
	private bool m_visibleInitially;

	[SerializeField]
	private float m_visibilityFadeTime = 0.2f;

	[SerializeField]
	private float m_maxDeltaTime = 1f / 30f;

	[SerializeField]
	private Image m_spinner;

	[SerializeField]
	private Image m_progressIndicator;

	[SerializeField]
	private Image m_background;

	private bool m_visible;

	private float m_progress;

	private float m_visibility;

	private float m_progressSmoothVelocity;

	private Color m_progressIndicatorOriginalColor;

	private Color m_spinnerOriginalColor;

	private Color m_backgroundOriginalColor;

	public static bool IsCompletelyInvisible
	{
		get
		{
			if (s_instance == null)
			{
				return true;
			}
			return s_instance.m_visibility == 0f;
		}
	}

	private void Awake()
	{
		if (s_instance == null)
		{
			s_instance = this;
		}
		m_visible = m_visibleInitially;
		m_visibility = (m_visible ? 1f : 0f);
		m_progressIndicatorOriginalColor = m_progressIndicator.color;
		m_spinnerOriginalColor = m_spinner.color;
		m_backgroundOriginalColor = m_background.color;
		UpdateGUIVisibility();
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
		float num = Mathf.Min(Time.deltaTime, m_maxDeltaTime);
		float num2 = (m_visible ? 1f : 0f);
		if (m_visibility != num2)
		{
			if (m_visibilityFadeTime <= 0f)
			{
				m_visibility = num2;
			}
			else
			{
				m_visibility = Mathf.MoveTowards(m_visibility, num2, num / m_visibilityFadeTime);
			}
			UpdateGUIVisibility();
		}
		m_spinner.transform.Rotate(0f, 0f, num * -180f);
		float target = ((m_progress < 1f) ? m_progress : 1.05f);
		m_progressIndicator.fillAmount = Mathf.Min(1f, Mathf.SmoothDamp(m_progressIndicator.fillAmount, target, ref m_progressSmoothVelocity, 0.2f, float.PositiveInfinity, num));
	}

	private void UpdateGUIVisibility()
	{
		Color spinnerOriginalColor = m_spinnerOriginalColor;
		spinnerOriginalColor.a *= m_visibility;
		m_spinner.color = spinnerOriginalColor;
		spinnerOriginalColor = m_progressIndicatorOriginalColor;
		spinnerOriginalColor.a *= (m_showProgressIndicator ? m_visibility : 0f);
		m_progressIndicator.color = spinnerOriginalColor;
		spinnerOriginalColor = m_backgroundOriginalColor;
		spinnerOriginalColor.a *= (m_showProgressIndicator ? m_visibility : 0f);
		m_background.color = spinnerOriginalColor;
	}

	public static void SetVisibility(bool visible)
	{
		if (!(s_instance == null))
		{
			s_instance.m_visible = visible;
		}
	}

	public static void SetProgress(float progress)
	{
		if (!(s_instance == null))
		{
			s_instance.m_progress = progress;
		}
	}
}
