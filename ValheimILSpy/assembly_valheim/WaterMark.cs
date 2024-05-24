using TMPro;
using UnityEngine;

public class WaterMark : MonoBehaviour
{
	public TMP_Text m_text;

	private void Awake()
	{
		m_text.text = "Version: " + Version.GetVersionString();
	}
}
