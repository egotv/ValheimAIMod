using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextsDialog : MonoBehaviour
{
	public class TextInfo
	{
		public string m_topic;

		public string m_text;

		public GameObject m_listElement;

		public GameObject m_selected;

		public TextInfo(string topic, string text)
		{
			m_topic = topic;
			m_text = text;
		}
	}

	public RectTransform m_listRoot;

	public ScrollRect m_leftScrollRect;

	public Scrollbar m_leftScrollbar;

	public Scrollbar m_rightScrollbar;

	public GameObject m_elementPrefab;

	public TMP_Text m_totalSkillText;

	public float m_spacing = 80f;

	public TMP_Text m_textAreaTopic;

	public TMP_Text m_textArea;

	public ScrollRectEnsureVisible m_recipeEnsureVisible;

	private List<TextInfo> m_texts = new List<TextInfo>();

	private float m_baseListSize;

	private int m_selectionIndex;

	private float m_inputDelayTimer;

	private const float InputDelay = 0.1f;

	private void Awake()
	{
		m_baseListSize = m_listRoot.rect.height;
	}

	public void Setup(Player player)
	{
		base.gameObject.SetActive(value: true);
		FillTextList();
		if (m_texts.Count > 0)
		{
			ShowText(0);
			return;
		}
		m_textAreaTopic.text = "";
		m_textArea.text = "";
	}

	private void Update()
	{
		UpdateGamepadInput();
		if (m_texts.Count > 0)
		{
			RectTransform rectTransform = m_leftScrollRect.transform as RectTransform;
			RectTransform listRoot = m_listRoot;
			m_leftScrollbar.size = rectTransform.rect.height / listRoot.rect.height;
		}
	}

	private IEnumerator FocusOnCurrentLevel(ScrollRect scrollRect, RectTransform listRoot, RectTransform element)
	{
		yield return null;
		yield return null;
		Canvas.ForceUpdateCanvases();
		SnapTo(scrollRect, m_listRoot, element);
	}

	private void SnapTo(ScrollRect scrollRect, RectTransform listRoot, RectTransform target)
	{
		Canvas.ForceUpdateCanvases();
		listRoot.anchoredPosition = (Vector2)scrollRect.transform.InverseTransformPoint(listRoot.position) - (Vector2)scrollRect.transform.InverseTransformPoint(target.position) - new Vector2(target.sizeDelta.x / 2f, 0f);
	}

	private void FillTextList()
	{
		foreach (TextInfo text2 in m_texts)
		{
			Object.Destroy(text2.m_listElement);
		}
		m_texts.Clear();
		UpdateTextsList();
		for (int i = 0; i < m_texts.Count; i++)
		{
			TextInfo text = m_texts[i];
			GameObject gameObject = Object.Instantiate(m_elementPrefab, Vector3.zero, Quaternion.identity, m_listRoot);
			gameObject.SetActive(value: true);
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)(-i) * m_spacing);
			Utils.FindChild(gameObject.transform, "name").GetComponent<TMP_Text>().text = Localization.instance.Localize(text.m_topic);
			text.m_listElement = gameObject;
			text.m_selected = Utils.FindChild(gameObject.transform, "selected").gameObject;
			text.m_selected.SetActive(value: false);
			gameObject.GetComponent<Button>().onClick.AddListener(delegate
			{
				OnSelectText(text);
			});
		}
		float size = Mathf.Max(m_baseListSize, (float)m_texts.Count * m_spacing);
		m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		if (m_texts.Count > 0)
		{
			m_recipeEnsureVisible.CenterOnItem(m_texts[0].m_listElement.transform as RectTransform);
		}
	}

	private void UpdateGamepadInput()
	{
		if (m_inputDelayTimer > 0f)
		{
			m_inputDelayTimer -= Time.unscaledDeltaTime;
		}
		else if (ZInput.IsGamepadActive() && m_texts.Count > 0)
		{
			float joyRightStickY = ZInput.GetJoyRightStickY();
			float joyLeftStickY = ZInput.GetJoyLeftStickY();
			bool buttonDown = ZInput.GetButtonDown("JoyDPadUp");
			bool num = joyLeftStickY < -0.1f;
			bool buttonDown2 = ZInput.GetButtonDown("JoyDPadDown");
			bool flag = joyLeftStickY > 0.1f;
			if ((buttonDown2 || flag) && m_selectionIndex < m_texts.Count - 1)
			{
				ShowText(Mathf.Min(m_texts.Count - 1, GetSelectedText() + 1));
				m_inputDelayTimer = 0.1f;
			}
			if ((num || buttonDown) && m_selectionIndex > 0)
			{
				ShowText(Mathf.Max(0, GetSelectedText() - 1));
				m_inputDelayTimer = 0.1f;
			}
			if (m_rightScrollbar.gameObject.activeSelf && (joyRightStickY < -0.1f || joyRightStickY > 0.1f))
			{
				m_rightScrollbar.value = Mathf.Clamp01(m_rightScrollbar.value - joyRightStickY * 10f * Time.deltaTime * (1f - m_rightScrollbar.size));
				m_inputDelayTimer = 0.1f;
			}
		}
	}

	private void OnSelectText(TextInfo text)
	{
		ShowText(text);
	}

	private int GetSelectedText()
	{
		for (int i = 0; i < m_texts.Count; i++)
		{
			if (m_texts[i].m_selected.activeSelf)
			{
				return i;
			}
		}
		return 0;
	}

	private void ShowText(int i)
	{
		m_selectionIndex = i;
		ShowText(m_texts[i]);
	}

	private void ShowText(TextInfo text)
	{
		m_textAreaTopic.text = Localization.instance.Localize(text.m_topic);
		m_textArea.text = Localization.instance.Localize(text.m_text);
		foreach (TextInfo text2 in m_texts)
		{
			text2.m_selected.SetActive(value: false);
		}
		text.m_selected.SetActive(value: true);
		StartCoroutine(FocusOnCurrentLevel(m_leftScrollRect, m_listRoot, text.m_selected.transform as RectTransform));
	}

	public void OnClose()
	{
		base.gameObject.SetActive(value: false);
	}

	private void UpdateTextsList()
	{
		m_texts.Clear();
		foreach (KeyValuePair<string, string> knownText in Player.m_localPlayer.GetKnownTexts())
		{
			m_texts.Add(new TextInfo(Localization.instance.Localize(knownText.Key.Replace("\u0016", "")), Localization.instance.Localize(knownText.Value.Replace("\u0016", ""))));
		}
		m_texts.Sort((TextInfo a, TextInfo b) => a.m_topic.CompareTo(b.m_topic));
		AddLog();
		AddActiveEffects();
	}

	private void AddLog()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string item in MessageHud.instance.GetLog())
		{
			stringBuilder.Append(item + "\n\n");
		}
		m_texts.Insert(0, new TextInfo(Localization.instance.Localize("$inventory_logs"), stringBuilder.ToString()));
	}

	private void AddActiveEffects()
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		List<StatusEffect> list = new List<StatusEffect>();
		Player.m_localPlayer.GetSEMan().GetHUDStatusEffects(list);
		StringBuilder stringBuilder = new StringBuilder(256);
		foreach (StatusEffect item in list)
		{
			stringBuilder.Append("<color=orange>" + Localization.instance.Localize(item.m_name) + "</color>\n");
			stringBuilder.Append(Localization.instance.Localize(item.GetTooltipString()));
			stringBuilder.Append("\n\n");
		}
		Player.m_localPlayer.GetGuardianPowerHUD(out var se, out var _);
		if ((bool)se)
		{
			stringBuilder.Append("<color=yellow>" + Localization.instance.Localize("$inventory_selectedgp") + "</color>\n");
			stringBuilder.Append("<color=orange>" + Localization.instance.Localize(se.m_name) + "</color>\n");
			stringBuilder.Append(Localization.instance.Localize(se.GetTooltipString()));
		}
		m_texts.Insert(0, new TextInfo(Localization.instance.Localize("$inventory_activeeffects"), stringBuilder.ToString()));
	}
}
