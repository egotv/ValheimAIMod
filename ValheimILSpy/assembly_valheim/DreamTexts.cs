using System;
using System.Collections.Generic;
using UnityEngine;

public class DreamTexts : MonoBehaviour
{
	[Serializable]
	public class DreamText
	{
		public string m_text = "Fluffy sheep";

		public float m_chanceToDream = 0.1f;

		public List<string> m_trueKeys = new List<string>();

		public List<string> m_falseKeys = new List<string>();
	}

	public List<DreamText> m_texts = new List<DreamText>();

	public DreamText GetRandomDreamText()
	{
		List<DreamText> list = new List<DreamText>();
		foreach (DreamText text in m_texts)
		{
			if (HaveGlobalKeys(text))
			{
				list.Add(text);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		DreamText dreamText = list[UnityEngine.Random.Range(0, list.Count)];
		if (UnityEngine.Random.value <= dreamText.m_chanceToDream)
		{
			return dreamText;
		}
		return null;
	}

	private bool HaveGlobalKeys(DreamText dream)
	{
		foreach (string trueKey in dream.m_trueKeys)
		{
			if (!ZoneSystem.instance.GetGlobalKey(trueKey))
			{
				return false;
			}
		}
		foreach (string falseKey in dream.m_falseKeys)
		{
			if (ZoneSystem.instance.GetGlobalKey(falseKey))
			{
				return false;
			}
		}
		return true;
	}
}
