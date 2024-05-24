using TMPro;
using UnityEngine;

public class Sign : MonoBehaviour, Hoverable, Interactable, TextReceiver
{
	public TextMeshProUGUI m_textWidget;

	public string m_name = "Sign";

	public string m_defaultText = "Sign";

	public int m_characterLimit = 50;

	private ZNetView m_nview;

	private bool m_isViewable = true;

	private string m_currentText;

	private void Awake()
	{
		m_currentText = m_defaultText;
		m_nview = GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			UpdateText();
			InvokeRepeating("UpdateText", 2f, 2f);
		}
	}

	public string GetHoverText()
	{
		string text = (m_isViewable ? ("\"" + GetText().RemoveRichTextTags() + "\"") : "[TEXT HIDDEN DUE TO UGC SETTINGS]");
		if (!PrivateArea.CheckAccess(base.transform.position, 0f, flash: false))
		{
			return text;
		}
		return text + "\n" + Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		if (!PrivateArea.CheckAccess(base.transform.position))
		{
			return false;
		}
		TextInput.instance.RequestText(this, "$piece_sign_input", m_characterLimit);
		return true;
	}

	private void UpdateText()
	{
		string text = m_nview.GetZDO().GetString(ZDOVars.s_text, m_defaultText);
		string @string = m_nview.GetZDO().GetString(ZDOVars.s_author);
		text = CensorShittyWords.FilterUGC(text, UGCType.Text, @string, 0L);
		if (m_currentText == text)
		{
			return;
		}
		PrivilegeManager.CanViewUserGeneratedContent(@string, delegate(PrivilegeManager.Result access)
		{
			switch (access)
			{
			case PrivilegeManager.Result.Allowed:
				m_currentText = text;
				m_textWidget.text = m_currentText;
				m_isViewable = true;
				break;
			case PrivilegeManager.Result.NotAllowed:
				m_currentText = "";
				m_textWidget.text = "ᚬᛏᛁᛚᛚᚴᛅᚾᚴᛚᛁᚴ";
				m_isViewable = false;
				break;
			default:
				m_currentText = "";
				m_textWidget.text = "ᚬᛏᛁᛚᛚᚴᛅᚾᚴᛚᛁᚴ";
				m_isViewable = false;
				ZLog.LogError("Failed to check UGC privilege");
				break;
			}
		});
	}

	public string GetText()
	{
		return m_currentText;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void SetText(string text)
	{
		if (PrivateArea.CheckAccess(base.transform.position))
		{
			m_nview.ClaimOwnership();
			m_nview.GetZDO().Set(ZDOVars.s_text, text);
			m_nview.GetZDO().Set(ZDOVars.s_author, PrivilegeManager.GetNetworkUserId());
			UpdateText();
		}
	}
}
