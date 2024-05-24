using System.Collections;
using UnityEngine;

public class CamShaker : MonoBehaviour
{
	public float m_strength = 1f;

	public float m_range = 50f;

	public float m_delay;

	public bool m_continous;

	public float m_continousDuration;

	public bool m_localOnly;

	private void Start()
	{
		if (m_continous)
		{
			if (m_delay <= 0f)
			{
				StartCoroutine("TriggerContinous");
			}
			else
			{
				Invoke("DelayedTriggerContinous", m_delay);
			}
		}
		else if (m_delay <= 0f)
		{
			Trigger();
		}
		else
		{
			Invoke("Trigger", m_delay);
		}
	}

	private void DelayedTriggerContinous()
	{
		StartCoroutine("TriggerContinous");
	}

	private IEnumerator TriggerContinous()
	{
		float t = 0f;
		while (true)
		{
			Trigger();
			t += Time.deltaTime;
			if (m_continousDuration > 0f && t > m_continousDuration)
			{
				break;
			}
			yield return null;
		}
	}

	private void Trigger()
	{
		if (!GameCamera.instance)
		{
			return;
		}
		if (m_localOnly)
		{
			ZNetView component = GetComponent<ZNetView>();
			if ((bool)component && component.IsValid() && !component.IsOwner())
			{
				return;
			}
		}
		GameCamera.instance.AddShake(base.transform.position, m_range, m_strength, m_continous);
	}
}
