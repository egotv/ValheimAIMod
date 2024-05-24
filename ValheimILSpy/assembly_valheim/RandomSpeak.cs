using UnityEngine;

public class RandomSpeak : MonoBehaviour
{
	public float m_interval = 5f;

	public float m_chance = 0.5f;

	public float m_triggerDistance = 5f;

	public float m_cullDistance = 10f;

	public float m_ttl = 10f;

	public Vector3 m_offset = new Vector3(0f, 0f, 0f);

	public EffectList m_speakEffects = new EffectList();

	public bool m_useLargeDialog;

	public bool m_onlyOnce;

	public bool m_onlyOnItemStand;

	public string m_topic = "";

	public string[] m_texts = new string[0];

	private void Start()
	{
		InvokeRepeating("Speak", Random.Range(0f, m_interval), m_interval);
	}

	private void Speak()
	{
		if (!(Random.value > m_chance) && m_texts.Length != 0 && !(Player.m_localPlayer == null) && !(Vector3.Distance(base.transform.position, Player.m_localPlayer.transform.position) > m_triggerDistance) && (!m_onlyOnItemStand || (bool)base.gameObject.GetComponentInParent<ItemStand>()))
		{
			m_speakEffects.Create(base.transform.position, base.transform.rotation);
			string text = m_texts[Random.Range(0, m_texts.Length)];
			Chat.instance.SetNpcText(base.gameObject, m_offset, m_cullDistance, m_ttl, m_topic, text, m_useLargeDialog);
			if (m_onlyOnce)
			{
				CancelInvoke("Speak");
			}
		}
	}
}
