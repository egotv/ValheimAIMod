using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillsDialog : MonoBehaviour
{
	public RectTransform m_listRoot;

	[SerializeField]
	private ScrollRect skillListScrollRect;

	[SerializeField]
	private Scrollbar scrollbar;

	public RectTransform m_tooltipAnchor;

	public GameObject m_elementPrefab;

	public TMP_Text m_totalSkillText;

	public float m_spacing = 80f;

	public float m_inputDelay = 0.1f;

	private int m_selectionIndex;

	private float m_inputDelayTimer;

	private float m_baseListSize;

	private readonly List<GameObject> m_elements = new List<GameObject>();

	private void Awake()
	{
		m_baseListSize = m_listRoot.rect.height;
	}

	private IEnumerator SelectFirstEntry()
	{
		yield return null;
		yield return null;
		if (m_elements.Count > 0)
		{
			m_selectionIndex = 0;
			EventSystem.current.SetSelectedGameObject(m_elements[m_selectionIndex]);
			StartCoroutine(FocusOnCurrentLevel(m_elements[m_selectionIndex].transform as RectTransform));
			skillListScrollRect.verticalNormalizedPosition = 1f;
		}
		yield return null;
	}

	private IEnumerator FocusOnCurrentLevel(RectTransform element)
	{
		yield return null;
		yield return null;
		Canvas.ForceUpdateCanvases();
		SnapTo(element);
	}

	private void SnapTo(RectTransform target)
	{
		Canvas.ForceUpdateCanvases();
		m_listRoot.anchoredPosition = (Vector2)skillListScrollRect.transform.InverseTransformPoint(m_listRoot.position) - (Vector2)skillListScrollRect.transform.InverseTransformPoint(target.position) - new Vector2(target.sizeDelta.x / 2f, 0f);
	}

	private void Update()
	{
		if (m_inputDelayTimer > 0f)
		{
			m_inputDelayTimer -= Time.unscaledDeltaTime;
			return;
		}
		if (ZInput.IsGamepadActive() && m_elements.Count > 0)
		{
			float joyRightStickY = ZInput.GetJoyRightStickY();
			float joyLeftStickY = ZInput.GetJoyLeftStickY();
			bool buttonDown = ZInput.GetButtonDown("JoyDPadUp");
			bool flag = joyLeftStickY < -0.1f || joyRightStickY < -0.1f;
			bool buttonDown2 = ZInput.GetButtonDown("JoyDPadDown");
			bool flag2 = joyLeftStickY > 0.1f || joyRightStickY > 0.1f;
			if ((flag || buttonDown) && m_selectionIndex > 0)
			{
				m_selectionIndex--;
			}
			if ((buttonDown2 || flag2) && m_selectionIndex < m_elements.Count - 1)
			{
				m_selectionIndex++;
			}
			GameObject gameObject = m_elements[m_selectionIndex];
			EventSystem.current.SetSelectedGameObject(gameObject);
			StartCoroutine(FocusOnCurrentLevel(gameObject.transform as RectTransform));
			gameObject.GetComponentInChildren<UITooltip>().OnHoverStart(gameObject);
			if (flag || flag2)
			{
				m_inputDelayTimer = m_inputDelay;
			}
		}
		if (m_elements.Count > 0)
		{
			RectTransform rectTransform = skillListScrollRect.transform as RectTransform;
			RectTransform listRoot = m_listRoot;
			scrollbar.size = rectTransform.rect.height / listRoot.rect.height;
		}
	}

	public void Setup(Player player)
	{
		base.gameObject.SetActive(value: true);
		List<Skills.Skill> skillList = player.GetSkills().GetSkillList();
		int num = skillList.Count - m_elements.Count;
		for (int i = 0; i < num; i++)
		{
			GameObject item = UnityEngine.Object.Instantiate(m_elementPrefab, Vector3.zero, Quaternion.identity, m_listRoot);
			m_elements.Add(item);
		}
		for (int j = 0; j < skillList.Count; j++)
		{
			Skills.Skill skill = skillList[j];
			GameObject obj = m_elements[j];
			obj.SetActive(value: true);
			RectTransform rectTransform = obj.transform as RectTransform;
			rectTransform.anchoredPosition = new Vector2(0f, (float)(-j) * m_spacing);
			obj.GetComponentInChildren<UITooltip>().Set("", skill.m_info.m_description, m_tooltipAnchor, new Vector2(0f, Math.Min(255f, rectTransform.localPosition.y + 10f)));
			Utils.FindChild(obj.transform, "icon").GetComponent<Image>().sprite = skill.m_info.m_icon;
			Utils.FindChild(obj.transform, "name").GetComponent<TMP_Text>().text = Localization.instance.Localize("$skill_" + skill.m_info.m_skill.ToString().ToLower());
			float skillLevel = player.GetSkills().GetSkillLevel(skill.m_info.m_skill);
			Utils.FindChild(obj.transform, "leveltext").GetComponent<TMP_Text>().text = ((int)skill.m_level).ToString();
			TMP_Text component = Utils.FindChild(obj.transform, "bonustext").GetComponent<TMP_Text>();
			bool flag = skillLevel != Mathf.Floor(skill.m_level);
			component.gameObject.SetActive(flag);
			if (flag)
			{
				component.text = (skillLevel - skill.m_level).ToString("+0");
			}
			Utils.FindChild(obj.transform, "levelbar_total").GetComponent<GuiBar>().SetValue(skillLevel / 100f);
			Utils.FindChild(obj.transform, "levelbar").GetComponent<GuiBar>().SetValue(skill.m_level / 100f);
			Utils.FindChild(obj.transform, "currentlevel").GetComponent<GuiBar>().SetValue(skill.GetLevelPercentage());
		}
		for (int k = skillList.Count; k < m_elements.Count; k++)
		{
			m_elements[k].SetActive(value: false);
		}
		float size = Mathf.Max(m_baseListSize, (float)skillList.Count * m_spacing);
		m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
		m_totalSkillText.text = "<color=orange>" + player.GetSkills().GetTotalSkill().ToString("0") + "</color><color=white> / </color><color=orange>" + player.GetSkills().GetTotalSkillCap().ToString("0") + "</color>";
		StartCoroutine(SelectFirstEntry());
	}

	public void OnClose()
	{
		base.gameObject.SetActive(value: false);
	}

	public void SkillClicked(GameObject selectedObject)
	{
		m_selectionIndex = m_elements.IndexOf(selectedObject);
	}
}
