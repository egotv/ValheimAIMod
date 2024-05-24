using TMPro;
using UnityEngine;

public class TextViewer : MonoBehaviour
{
	public enum Style
	{
		Rune,
		Intro,
		Raven
	}

	private static TextViewer m_instance;

	private Animator m_animator;

	private Animator m_animatorIntro;

	private Animator m_animatorRaven;

	[Header("Rune")]
	public GameObject m_root;

	public TMP_Text m_topic;

	public TMP_Text m_text;

	public TMP_Text m_runeText;

	public GameObject m_closeText;

	[Header("Intro")]
	public GameObject m_introRoot;

	public TMP_Text m_introTopic;

	public TMP_Text m_introText;

	[Header("Raven")]
	public GameObject m_ravenRoot;

	public TMP_Text m_ravenTopic;

	public TMP_Text m_ravenText;

	private static readonly int s_visibleID = ZSyncAnimation.GetHash("visible");

	private static readonly int s_animatorTagVisible = ZSyncAnimation.GetHash("visible");

	private float m_showTime;

	private bool m_autoHide;

	private Vector3 m_openPlayerPos = Vector3.zero;

	public static TextViewer instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_root.SetActive(value: true);
		m_introRoot.SetActive(value: true);
		m_ravenRoot.SetActive(value: true);
		m_animator = m_root.GetComponent<Animator>();
		m_animatorIntro = m_introRoot.GetComponent<Animator>();
		m_animatorRaven = m_ravenRoot.GetComponent<Animator>();
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void LateUpdate()
	{
		if (!IsVisible())
		{
			return;
		}
		m_showTime += Time.deltaTime;
		if (m_showTime > 0.2f)
		{
			if (m_autoHide && (bool)Player.m_localPlayer && Vector3.Distance(Player.m_localPlayer.transform.position, m_openPlayerPos) > 3f)
			{
				Hide();
			}
			if (ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse") || ZInput.GetKeyDown(KeyCode.Escape))
			{
				Hide();
			}
		}
	}

	public void ShowText(Style style, string topic, string textId, bool autoHide)
	{
		if (!(Player.m_localPlayer == null))
		{
			topic = Localization.instance.Localize(topic);
			string text = Localization.instance.Localize(textId);
			switch (style)
			{
			case Style.Rune:
				m_topic.text = topic;
				m_text.text = text;
				m_runeText.text = Localization.instance.TranslateSingleId(textId, "English");
				m_animator.SetBool(s_visibleID, value: true);
				break;
			case Style.Intro:
				m_introTopic.text = topic;
				m_introText.text = text;
				m_animatorIntro.gameObject.SetActive(value: true);
				m_animatorIntro.SetTrigger("play");
				ZLog.Log("Show intro " + Time.frameCount);
				break;
			case Style.Raven:
				m_ravenTopic.text = topic;
				m_ravenText.text = text;
				m_animatorRaven.SetBool(s_visibleID, value: true);
				break;
			}
			m_autoHide = autoHide;
			m_openPlayerPos = Player.m_localPlayer.transform.position;
			m_showTime = 0f;
			ZLog.Log("Show text " + topic + ":" + text);
		}
	}

	public void Hide()
	{
		m_autoHide = false;
		m_animator.SetBool(s_visibleID, value: false);
		m_animatorRaven.SetBool(s_visibleID, value: false);
	}

	public void HideIntro()
	{
		m_animatorIntro.gameObject.SetActive(value: false);
	}

	public bool IsVisible()
	{
		if (m_instance.m_animatorIntro.GetCurrentAnimatorStateInfo(0).tagHash == s_animatorTagVisible)
		{
			return true;
		}
		if (!m_animator.GetBool(s_visibleID) && !m_animatorIntro.GetBool(s_visibleID))
		{
			return m_animatorRaven.GetBool(s_visibleID);
		}
		return true;
	}

	public static bool IsShowingIntro()
	{
		if (m_instance != null)
		{
			return m_instance.m_animatorIntro.GetCurrentAnimatorStateInfo(0).tagHash == s_animatorTagVisible;
		}
		return false;
	}
}
