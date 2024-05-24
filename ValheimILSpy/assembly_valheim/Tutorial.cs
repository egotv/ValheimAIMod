using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
	[Serializable]
	public class TutorialText
	{
		public string m_name;

		[Tooltip("If this global key is set, this tutorial will be shown (is saved in knowntutorials as this global key name as well)")]
		public string m_globalKeyTrigger;

		[Tooltip("If the specified tutorial has been seen, will trigger this tutorial. (You could chain multiple birds like this, or use together with a location discoverLabel when the exact location cant be set, like for the hildir tower)")]
		public string m_tutorialTrigger;

		public string m_topic = "";

		public string m_label = "";

		public bool m_isMunin;

		[TextArea]
		public string m_text = "";
	}

	public List<TutorialText> m_texts = new List<TutorialText>();

	public int m_GlobalKeyCheckRateSec = 10;

	public RectTransform m_windowRoot;

	public TMP_Text m_topic;

	public TMP_Text m_text;

	public GameObject m_ravenPrefab;

	private static Tutorial m_instance;

	private Queue<string> m_tutQueue = new Queue<string>();

	private double m_lastGlobalKeyCheck;

	public static Tutorial instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		m_windowRoot.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		double timeSeconds = ZNet.instance.GetTimeSeconds();
		if (!ZoneSystem.instance || !Player.m_localPlayer || !(timeSeconds > m_lastGlobalKeyCheck + (double)m_GlobalKeyCheckRateSec))
		{
			return;
		}
		m_lastGlobalKeyCheck = timeSeconds;
		foreach (TutorialText text in m_texts)
		{
			if (!string.IsNullOrEmpty(text.m_globalKeyTrigger) && ZoneSystem.instance.GetGlobalKey(text.m_globalKeyTrigger))
			{
				Player.m_localPlayer.ShowTutorial(text.m_globalKeyTrigger);
			}
			if (!string.IsNullOrEmpty(text.m_tutorialTrigger) && Player.m_localPlayer.HaveSeenTutorial(text.m_tutorialTrigger))
			{
				Player.m_localPlayer.ShowTutorial(text.m_name);
			}
		}
	}

	public void ShowText(string name, bool force)
	{
		TutorialText tutorialText = m_texts.Find((TutorialText x) => x.m_name == name);
		if (tutorialText != null)
		{
			SpawnRaven(tutorialText.m_name, tutorialText.m_topic, tutorialText.m_text, tutorialText.m_label, tutorialText.m_isMunin);
		}
		else
		{
			Debug.Log("Missing tutorial text for: " + name);
		}
	}

	private void SpawnRaven(string key, string topic, string text, string label, bool munin)
	{
		if (!Raven.IsInstantiated())
		{
			UnityEngine.Object.Instantiate(m_ravenPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
		}
		Raven.AddTempText(key, topic, text, label, munin);
	}
}
