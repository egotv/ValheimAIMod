using TMPro;
using UnityEngine;

public class Console : Terminal
{
	private static Console m_instance;

	private static bool m_consoleEnabled;

	public TMP_Text m_devTest;

	public static Console instance => m_instance;

	protected override Terminal m_terminalInstance => m_instance;

	public override void Awake()
	{
		LoadQuickSelect();
		base.Awake();
		m_instance = this;
		AddString("Valheim " + Version.GetVersionString() + " (network version " + 27u + ")");
		AddString("");
		AddString("type \"help\" - for commands");
		AddString("");
		m_chatWindow.gameObject.SetActive(value: false);
	}

	public override void Update()
	{
		m_focused = false;
		if ((bool)ZNet.instance && ZNet.instance.InPasswordDialog())
		{
			m_chatWindow.gameObject.SetActive(value: false);
		}
		else
		{
			if (!IsConsoleEnabled())
			{
				return;
			}
			if (ZInput.GetButtonDown("Console") || (IsVisible() && ZInput.GetKeyDown(KeyCode.Escape)) || (IsVisible() && ZInput.GetButtonDown("JoyButtonB")) || (ZInput.GetButton("JoyLTrigger") && ZInput.GetButton("JoyLBumper") && ZInput.GetButtonDown("JoyStart")))
			{
				m_chatWindow.gameObject.SetActive(!m_chatWindow.gameObject.activeSelf);
				if (ZInput.IsGamepadActive())
				{
					AddString("Gamepad console controls:\n   A: Enter text when empty (only in big picture mode), or send text when not.\n   LB: Erase.\n   DPad up/down: Cycle history.\n   DPad right: Autocomplete.\n   DPad left: Show commands (help).\n   Left Stick: Scroll.\n   RStick + LStick: show/hide console.\n   X+DPad: Save quick select option.\n   Y+DPad: Load quick select option.");
				}
				if (m_chatWindow.gameObject.activeInHierarchy)
				{
					m_input.ActivateInputField();
				}
			}
			if (m_chatWindow.gameObject.activeInHierarchy)
			{
				m_focused = true;
			}
			if (m_focused)
			{
				if (ZInput.GetButtonDown("JoyTabLeft") && m_input.text.Length > 0)
				{
					m_input.text = m_input.text.Substring(0, m_input.text.Length - 1);
				}
				else if (ZInput.GetButtonDown("JoyDPadLeft"))
				{
					TryRunCommand("help");
				}
			}
			if ((bool)instance && Terminal.m_threadSafeConsoleLog.TryDequeue(out var result))
			{
				instance.AddString(result);
			}
			if ((bool)Player.m_localPlayer && Terminal.m_threadSafeMessages.TryDequeue(out var result2))
			{
				Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, result2);
			}
			base.Update();
			if (ZInput.GetButtonDown("JoyDPadLeft") && !ZInput.GetButton("JoyButtonX") && !ZInput.GetButton("JoyButtonY"))
			{
				TryRunCommand("help");
			}
		}
	}

	public static bool IsVisible()
	{
		if ((bool)m_instance)
		{
			return m_instance.m_chatWindow.gameObject.activeInHierarchy;
		}
		return false;
	}

	public void Print(string text)
	{
		AddString(text);
	}

	public bool IsConsoleEnabled()
	{
		return m_consoleEnabled;
	}

	public static void SetConsoleEnabled(bool enabled)
	{
		m_consoleEnabled = enabled;
	}
}
