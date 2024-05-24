using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinCode : MonoBehaviour
{
	private static JoinCode m_instance;

	public GameObject m_root;

	public Button m_btn;

	public TMP_Text m_text;

	public CanvasRenderer m_darken;

	public float m_firstShowDuration = 7f;

	public float m_fadeOutDuration = 3f;

	private string m_joinCode = "";

	private float m_textAlpha;

	private float m_darkenAlpha;

	private float m_isVisible;

	private bool m_inMenu;

	private bool m_inputBlocked;

	public static void Show(bool firstSpawn = false)
	{
		if (m_instance != null)
		{
			if (firstSpawn)
			{
				m_instance.Init();
			}
			m_instance.Activate(firstSpawn);
		}
	}

	public static void Hide()
	{
		if (m_instance != null)
		{
			m_instance.Deactivate();
		}
	}

	private void Start()
	{
		m_instance = this;
		m_textAlpha = m_text.color.a;
		m_darkenAlpha = m_darken.GetAlpha();
		Deactivate();
	}

	private void Init()
	{
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			m_joinCode = ZPlayFabMatchmaking.JoinCode;
			base.gameObject.SetActive(m_joinCode.Length > 0);
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
	}

	private void Activate(bool firstSpawn)
	{
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			m_joinCode = ZPlayFabMatchmaking.JoinCode;
		}
		ResetAlpha();
		m_root.SetActive(m_joinCode.Length > 0);
		m_inMenu = !firstSpawn;
		m_isVisible = (firstSpawn ? m_firstShowDuration : 0f);
	}

	public void Deactivate()
	{
		m_root.SetActive(value: false);
		m_inMenu = false;
		m_isVisible = 0f;
	}

	private void ResetAlpha()
	{
		Color color = m_text.color;
		color.a = m_textAlpha;
		m_text.color = color;
		m_darken.SetAlpha(m_darkenAlpha);
	}

	private void Update()
	{
		if (!m_inMenu && !(m_isVisible > 0f))
		{
			return;
		}
		m_btn.gameObject.GetComponentInChildren<TMP_Text>().text = Localization.instance.Localize("$menu_joincode", m_joinCode);
		if (m_inMenu)
		{
			m_root.SetActive(Hud.instance.IsVisible());
			if (Settings.instance == null && (Menu.instance == null || (!Menu.instance.m_logoutDialog.gameObject.activeSelf && !Menu.instance.PlayerListActive)) && m_inputBlocked)
			{
				m_inputBlocked = false;
				return;
			}
			m_inputBlocked = Settings.instance != null || (Menu.instance != null && (Menu.instance.m_logoutDialog.gameObject.activeSelf || Menu.instance.PlayerListActive));
			if (!m_inputBlocked && Settings.instance == null && (ZInput.GetButtonDown("JoyButtonX") || ZInput.GetKeyDown(KeyCode.J)))
			{
				CopyJoinCodeToClipboard();
			}
			return;
		}
		m_isVisible -= Time.deltaTime;
		if (m_isVisible < 0f)
		{
			Hide();
		}
		else if (m_isVisible < m_fadeOutDuration)
		{
			float t = m_isVisible / m_fadeOutDuration;
			float a = Mathf.Lerp(0f, m_textAlpha, t);
			float alpha = Mathf.Lerp(0f, m_darkenAlpha, t);
			Color color = m_text.color;
			color.a = a;
			m_text.color = color;
			m_darken.SetAlpha(alpha);
		}
	}

	public void OnClick()
	{
		CopyJoinCodeToClipboard();
	}

	private void CopyJoinCodeToClipboard()
	{
		Gogan.LogEvent("Screen", "CopyToClipboard", "JoinCode", 0L);
		GUIUtility.systemCopyBuffer = m_joinCode;
		if (MessageHud.instance != null)
		{
			MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, "$menu_joincode_copied");
		}
	}
}
