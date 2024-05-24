using System;
using System.Collections;
using LlamAcademy.Spring;
using UnityEngine;

public class ConditionalObject : MonoBehaviour, Hoverable
{
	private float m_delayTimer;

	[NonSerialized]
	public float m_dropTimer;

	private SpringVector3 m_scaleSpring;

	private bool m_springActive;

	private Vector3 m_startScale;

	private float m_startHeight;

	public GameObject m_enableObject;

	public string m_hoverName = "Oddity";

	public string m_globalKeyCondition = "";

	public float m_appearDelay;

	public string m_animatorBool;

	public EffectList m_showEffects = new EffectList();

	[Header("Drop Settings")]
	public bool m_dropEnabled;

	public float m_dropHeight = 1f;

	public float m_dropTime = 0.5f;

	public float m_dropTimeVariance;

	private float m_dropTimeActual;

	public AnimationCurve m_dropCurve = AnimationCurve.Linear(0f, 1f, 0f, 1f);

	[Header("Spring Settings")]
	public bool m_springEnabled;

	public float m_springDisableTime = 3f;

	[Min(0f)]
	public float m_springDamping = 8f;

	[Min(0f)]
	public float m_springStiffness = 180f;

	public Vector3 m_startSpringVelocity = new Vector3(1.5f, -1f, 1.5f);

	private void Awake()
	{
		m_startScale = m_enableObject.transform.localScale;
		m_startHeight = m_enableObject.transform.position.y;
		m_scaleSpring = new SpringVector3
		{
			Damping = m_springDamping,
			Stiffness = m_springStiffness,
			StartValue = m_startScale,
			EndValue = m_startScale,
			InitialVelocity = m_startSpringVelocity
		};
		if (ShouldBeVisible() && !string.IsNullOrEmpty(m_globalKeyCondition))
		{
			m_enableObject.SetActive(value: true);
			if (!string.IsNullOrEmpty(m_animatorBool))
			{
				m_enableObject.GetComponentInChildren<Animator>()?.SetBool(m_animatorBool, value: true);
			}
			m_springActive = false;
			m_dropTimer = float.PositiveInfinity;
		}
		else
		{
			m_enableObject.SetActive(value: false);
		}
		m_dropTimeActual = m_dropTime + UnityEngine.Random.Range(0f, m_dropTimeVariance);
	}

	private void Update()
	{
		if (!m_enableObject.activeInHierarchy && ShouldBeVisible())
		{
			m_delayTimer += Time.deltaTime;
			if (m_delayTimer > m_appearDelay)
			{
				if (m_dropEnabled)
				{
					m_enableObject.transform.position = m_enableObject.transform.position + Vector3.up * m_dropHeight;
				}
				else if (m_springEnabled)
				{
					ActivateSpring();
				}
				m_enableObject.SetActive(value: true);
				m_showEffects.Create(base.transform.position, base.transform.rotation, base.transform);
				if (!string.IsNullOrEmpty(m_animatorBool))
				{
					Animator componentInChildren = m_enableObject.GetComponentInChildren<Animator>();
					if ((object)componentInChildren != null)
					{
						componentInChildren.SetBool(m_animatorBool, value: true);
					}
					else
					{
						ZLog.LogError("Object '" + base.name + "' trying to set animation trigger '" + m_animatorBool + "' but no animator was found!");
					}
				}
			}
		}
		if (!m_enableObject.activeInHierarchy)
		{
			return;
		}
		if (m_springEnabled && m_springActive)
		{
			m_enableObject.transform.localScale = m_scaleSpring.Evaluate(Time.deltaTime);
		}
		if (m_dropEnabled)
		{
			if (m_dropTimer <= m_dropTimeActual)
			{
				m_dropTimer += Time.deltaTime;
				Vector3 position = m_enableObject.transform.position;
				float num = (1f - m_dropCurve.Evaluate(m_dropTimer / m_dropTimeActual)) * m_dropHeight;
				position.y = m_startHeight + num;
				m_enableObject.transform.position = position;
			}
			if (m_dropTimer > m_dropTimeActual && !m_springActive)
			{
				ActivateSpring();
			}
		}
	}

	private bool ShouldBeVisible()
	{
		if (!string.IsNullOrEmpty(m_globalKeyCondition))
		{
			if ((bool)ZoneSystem.instance)
			{
				return ZoneSystem.instance.GetGlobalKey(m_globalKeyCondition);
			}
			return false;
		}
		return true;
	}

	private void ActivateSpring()
	{
		StartCoroutine(DisableSpring());
		m_springActive = true;
	}

	private IEnumerator DisableSpring()
	{
		yield return new WaitForSeconds(m_springDisableTime);
		m_springActive = false;
		m_enableObject.transform.localScale = m_startScale;
	}

	public string GetHoverText()
	{
		return Localization.instance.Localize(m_hoverName);
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(m_hoverName);
	}
}
