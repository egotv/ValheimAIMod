using System.Collections;
using UnityEngine;

public class BossStone : MonoBehaviour
{
	public ItemStand m_itemStand;

	public GameObject m_activeEffect;

	public EffectList m_activateStep1 = new EffectList();

	public EffectList m_activateStep2 = new EffectList();

	public EffectList m_activateStep3 = new EffectList();

	public string m_completedMessage = "";

	public MeshRenderer m_mesh;

	public int m_emissiveMaterialIndex;

	public Color m_activeEmissiveColor = Color.white;

	private bool m_active;

	private ZNetView m_nview;

	private void Start()
	{
		if (m_mesh.materials[m_emissiveMaterialIndex].HasProperty("_EmissionColor"))
		{
			m_mesh.materials[m_emissiveMaterialIndex].SetColor("_EmissionColor", Color.black);
		}
		if ((bool)m_activeEffect)
		{
			m_activeEffect.SetActive(value: false);
		}
		SetActivated(m_itemStand.HaveAttachment(), triggerEffect: false);
		InvokeRepeating("UpdateVisual", 1f, 1f);
	}

	private void UpdateVisual()
	{
		SetActivated(m_itemStand.HaveAttachment(), triggerEffect: true);
	}

	private void SetActivated(bool active, bool triggerEffect)
	{
		if (active == m_active)
		{
			return;
		}
		m_active = active;
		if (triggerEffect && active)
		{
			Invoke("DelayedAttachEffects_Step1", 1f);
			Invoke("DelayedAttachEffects_Step2", 5f);
			Invoke("DelayedAttachEffects_Step3", 11f);
			return;
		}
		if ((bool)m_activeEffect)
		{
			m_activeEffect.SetActive(active);
		}
		StopCoroutine("FadeEmission");
		StartCoroutine("FadeEmission");
	}

	private void DelayedAttachEffects_Step1()
	{
		m_activateStep1.Create(m_itemStand.transform.position, base.transform.rotation);
	}

	private void DelayedAttachEffects_Step2()
	{
		m_activateStep2.Create(base.transform.position, base.transform.rotation);
	}

	private void DelayedAttachEffects_Step3()
	{
		if ((bool)m_activeEffect)
		{
			m_activeEffect.SetActive(value: true);
		}
		m_activateStep3.Create(base.transform.position, base.transform.rotation);
		StopCoroutine("FadeEmission");
		StartCoroutine("FadeEmission");
		Player.MessageAllInRange(base.transform.position, 20f, MessageHud.MessageType.Center, m_completedMessage);
	}

	private IEnumerator FadeEmission()
	{
		if ((bool)m_mesh && m_mesh.materials[m_emissiveMaterialIndex].HasProperty("_EmissionColor"))
		{
			Color startColor = m_mesh.materials[m_emissiveMaterialIndex].GetColor("_EmissionColor");
			Color targetColor = (m_active ? m_activeEmissiveColor : Color.black);
			for (float t = 0f; t < 1f; t += Time.deltaTime)
			{
				Color value = Color.Lerp(startColor, targetColor, t / 1f);
				m_mesh.materials[m_emissiveMaterialIndex].SetColor("_EmissionColor", value);
				yield return null;
			}
		}
		ZLog.Log("Done fading color");
	}

	public bool IsActivated()
	{
		return m_active;
	}
}
