using System;
using System.Collections.Generic;
using System.Text;
using PlatformTools.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UserManagement;

public class Chat : Terminal
{
	public class WorldTextInstance
	{
		public UserInfo m_userInfo;

		public long m_talkerID;

		public GameObject m_go;

		public Vector3 m_position;

		public float m_timer;

		public GameObject m_gui;

		public TextMeshProUGUI m_textMeshField;

		public Talker.Type m_type;

		public string m_text = "";

		public string m_name => m_userInfo.GetDisplayName(m_userInfo.NetworkUserId);
	}

	public class NpcText
	{
		public string m_topic;

		public string m_text;

		public GameObject m_go;

		public Vector3 m_offset = Vector3.zero;

		public float m_cullDistance = 20f;

		public GameObject m_gui;

		public Animator m_animator;

		public TextMeshProUGUI m_textField;

		public TextMeshProUGUI m_topicField;

		public float m_ttl;

		public bool m_timeout;

		public void SetVisible(bool visible)
		{
			m_animator.SetBool("visible", visible);
		}

		public bool IsVisible()
		{
			if (m_animator.GetCurrentAnimatorStateInfo(0).IsTag("visible"))
			{
				return true;
			}
			return m_animator.GetBool("visible");
		}

		public void UpdateText()
		{
			if (m_topic.Length > 0)
			{
				m_textField.text = "<color=orange>" + Localization.instance.Localize(m_topic) + "</color>\n" + Localization.instance.Localize(m_text);
			}
			else
			{
				m_textField.text = Localization.instance.Localize(m_text);
			}
		}
	}

	private static Chat m_instance;

	public float m_hideDelay = 10f;

	public float m_worldTextTTL = 5f;

	public GameObject m_worldTextBase;

	public GameObject m_npcTextBase;

	public GameObject m_npcTextBaseLarge;

	[Tooltip("If true the player has to open chat twice to enter input mode.")]
	[SerializeField]
	protected bool m_doubleOpenForVirtualKeyboard = true;

	private List<WorldTextInstance> m_worldTexts = new List<WorldTextInstance>();

	private List<NpcText> m_npcTexts = new List<NpcText>();

	private float m_hideTimer = 9999f;

	public bool m_wasFocused;

	public static Chat instance => m_instance;

	public List<WorldTextInstance> WorldTexts => m_worldTexts;

	protected override Terminal m_terminalInstance => m_instance;

	private void OnDestroy()
	{
		UserInfo.PlatformUnregisterForProfileUpdates?.Invoke(OnProfileUpdate);
		Localization.OnLanguageChange = (Action)Delegate.Remove(Localization.OnLanguageChange, new Action(OnLanguageChanged));
	}

	public override void Awake()
	{
		base.Awake();
		m_instance = this;
		ZRoutedRpc.instance.Register<Vector3, int, UserInfo, string, string>("ChatMessage", RPC_ChatMessage);
		ZRoutedRpc.instance.Register<Vector3, Quaternion, bool>("RPC_TeleportPlayer", RPC_TeleportPlayer);
		AddString(Localization.instance.Localize("/w [text] - $chat_whisper"));
		AddString(Localization.instance.Localize("/s [text] - $chat_shout"));
		AddString(Localization.instance.Localize("/die - $chat_kill"));
		AddString(Localization.instance.Localize("/resetspawn - $chat_resetspawn"));
		AddString(Localization.instance.Localize("/[emote]"));
		StringBuilder stringBuilder = new StringBuilder("Emotes: ");
		for (int i = 0; i < 20; i++)
		{
			Emotes emotes = (Emotes)i;
			stringBuilder.Append(emotes.ToString().ToLower());
			if (i + 1 < 20)
			{
				stringBuilder.Append(", ");
			}
		}
		AddString(Localization.instance.Localize(stringBuilder.ToString()));
		AddString("");
		m_input.gameObject.SetActive(value: false);
		m_worldTextBase.SetActive(value: false);
		m_tabPrefix = '/';
		m_maxVisibleBufferLength = 20;
		Terminal.m_bindList = new List<string>(PlayerPrefs.GetString("ConsoleBindings", "").Split('\n', StringSplitOptions.None));
		if (Terminal.m_bindList.Count == 0)
		{
			TryRunCommand("resetbinds");
		}
		Terminal.updateBinds();
		m_autoCompleteSecrets = true;
		UserInfo.PlatformRegisterForProfileUpdates?.Invoke(OnProfileUpdate);
		BlockList.UpdateAvoidList();
		Localization.OnLanguageChange = (Action)Delegate.Combine(Localization.OnLanguageChange, new Action(OnLanguageChanged));
	}

	private void OnLanguageChanged()
	{
		foreach (NpcText npcText in m_npcTexts)
		{
			npcText.UpdateText();
		}
	}

	public bool HasFocus()
	{
		if (m_chatWindow != null && m_chatWindow.gameObject.activeInHierarchy)
		{
			return m_input.isFocused;
		}
		return false;
	}

	public bool IsChatDialogWindowVisible()
	{
		return m_chatWindow.gameObject.activeSelf;
	}

	public override void Update()
	{
		m_focused = false;
		m_hideTimer += Time.deltaTime;
		m_chatWindow.gameObject.SetActive(m_hideTimer < m_hideDelay);
		if (!m_wasFocused)
		{
			if (Player.m_localPlayer != null && !Console.IsVisible() && !TextInput.IsVisible() && !Minimap.InTextInput() && !Menu.IsVisible() && !InventoryGui.IsVisible())
			{
				bool flag = ZInput.InputLayout == InputLayout.Alternative1;
				bool button = ZInput.GetButton("JoyLBumper");
				bool button2 = ZInput.GetButton("JoyLTrigger");
				if (ZInput.GetButtonDown("Chat") || (ZInput.GetButtonDown("JoyChat") && ZInput.GetButton("JoyAltKeys") && !(flag && button2) && !(!flag && button)))
				{
					m_hideTimer = 0f;
					m_chatWindow.gameObject.SetActive(value: true);
					m_input.gameObject.SetActive(value: true);
					if (m_doubleOpenForVirtualKeyboard && Application.isConsolePlatform)
					{
						m_input.Select();
					}
					else
					{
						m_input.ActivateInputField();
					}
				}
			}
		}
		else if (m_wasFocused)
		{
			m_hideTimer = 0f;
			m_focused = true;
			if (ZInput.GetKeyDown(KeyCode.Mouse0) || ZInput.GetKey(KeyCode.Mouse1) || ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyLStickDown"))
			{
				EventSystem.current.SetSelectedGameObject(null);
				m_input.gameObject.SetActive(value: false);
				m_focused = false;
			}
		}
		m_wasFocused = m_input.isFocused;
		if (!m_input.isFocused && (Console.instance == null || !Console.instance.m_chatWindow.gameObject.activeInHierarchy))
		{
			foreach (KeyValuePair<KeyCode, List<string>> bind in Terminal.m_binds)
			{
				if (!ZInput.GetKeyDown(bind.Key))
				{
					continue;
				}
				foreach (string item in bind.Value)
				{
					TryRunCommand(item, silentFail: true, skipAllowedCheck: true);
				}
			}
		}
		base.Update();
	}

	public new void SendInput()
	{
		base.SendInput();
		m_input.gameObject.SetActive(value: false);
	}

	public void Hide()
	{
		m_hideTimer = m_hideDelay;
	}

	private void LateUpdate()
	{
		UpdateWorldTexts(Time.deltaTime);
		UpdateNpcTexts(Time.deltaTime);
	}

	public void OnNewChatMessage(GameObject go, long senderID, Vector3 pos, Talker.Type type, UserInfo user, string text, string senderNetworkUserId)
	{
		if (BlockList.IsBlocked(senderNetworkUserId))
		{
			return;
		}
		PrivilegeManager.CanCommunicateWith(senderNetworkUserId, delegate(PrivilegeManager.Result access)
		{
			OnCanCommunicateWithResult(access, delegate
			{
				if (this == null)
				{
					Debug.LogError("Chat has already been destroyed!");
				}
				else if (ZNet.instance.IsDedicated())
				{
					AddString(user.GetDisplayName(senderNetworkUserId), text, type);
				}
				else
				{
					UserInfo.GetProfile(PrivilegeManager.ParseUser(senderNetworkUserId), delegate(Profile profile)
					{
						user.UpdateGamertag(profile.UniqueGamertag);
						text = text.Replace('<', ' ');
						text = text.Replace('>', ' ');
						text = CensorShittyWords.FilterUGC(text, UGCType.Chat, senderNetworkUserId, 0L);
						if (type != Talker.Type.Ping)
						{
							m_hideTimer = 0f;
							AddString(user.GetDisplayName(senderNetworkUserId), text, type);
						}
						if (!Minimap.instance || !Player.m_localPlayer || Minimap.instance.m_mode != 0 || !(Vector3.Distance(Player.m_localPlayer.transform.position, pos) > Minimap.instance.m_nomapPingDistance))
						{
							AddInworldText(go, senderID, pos, type, user, text);
						}
					});
				}
			});
		});
	}

	private void OnProfileUpdate(Profile profileOld, Profile profileNew)
	{
		UpdateDisplayName(UserInfo.GamertagSuffix(profileOld.UniqueGamertag), UserInfo.GamertagSuffix(profileNew.UniqueGamertag));
	}

	private void OnCanCommunicateWithResult(PrivilegeManager.Result access, Action displayChatMessage)
	{
		if (access == PrivilegeManager.Result.Allowed)
		{
			displayChatMessage();
		}
	}

	private void UpdateWorldTexts(float dt)
	{
		WorldTextInstance worldTextInstance = null;
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		foreach (WorldTextInstance worldText in m_worldTexts)
		{
			worldText.m_timer += dt;
			if (worldText.m_timer > m_worldTextTTL && worldTextInstance == null)
			{
				worldTextInstance = worldText;
			}
			worldText.m_position.y += dt * 0.15f;
			Vector3 zero = Vector3.zero;
			if ((bool)worldText.m_go)
			{
				Character component = worldText.m_go.GetComponent<Character>();
				zero = ((!component) ? (worldText.m_go.transform.position + Vector3.up * 0.3f) : (component.GetHeadPoint() + Vector3.up * 0.3f));
			}
			else
			{
				zero = worldText.m_position + Vector3.up * 0.3f;
			}
			Vector3 position = mainCamera.WorldToScreenPointScaled(zero);
			if (position.x < 0f || position.x > (float)Screen.width || position.y < 0f || position.y > (float)Screen.height || position.z < 0f)
			{
				Vector3 vector = zero - mainCamera.transform.position;
				bool flag = Vector3.Dot(mainCamera.transform.right, vector) < 0f;
				Vector3 vector2 = vector;
				vector2.y = 0f;
				float magnitude = vector2.magnitude;
				float y = vector.y;
				Vector3 forward = mainCamera.transform.forward;
				forward.y = 0f;
				forward.Normalize();
				forward *= magnitude;
				Vector3 vector3 = forward + Vector3.up * y;
				position = mainCamera.WorldToScreenPointScaled(mainCamera.transform.position + vector3);
				position.x = ((!flag) ? Screen.width : 0);
			}
			RectTransform rectTransform = worldText.m_gui.transform as RectTransform;
			position.x = Mathf.Clamp(position.x, rectTransform.rect.width / 2f, (float)Screen.width - rectTransform.rect.width / 2f);
			position.y = Mathf.Clamp(position.y, rectTransform.rect.height / 2f, (float)Screen.height - rectTransform.rect.height);
			position.z = Mathf.Min(position.z, 100f);
			worldText.m_gui.transform.position = position;
		}
		if (worldTextInstance != null)
		{
			UnityEngine.Object.Destroy(worldTextInstance.m_gui);
			m_worldTexts.Remove(worldTextInstance);
		}
	}

	private void AddInworldText(GameObject go, long senderID, Vector3 position, Talker.Type type, UserInfo user, string text)
	{
		WorldTextInstance worldTextInstance = FindExistingWorldText(senderID);
		if (worldTextInstance == null)
		{
			worldTextInstance = new WorldTextInstance();
			worldTextInstance.m_talkerID = senderID;
			worldTextInstance.m_gui = UnityEngine.Object.Instantiate(m_worldTextBase, base.transform);
			worldTextInstance.m_gui.gameObject.SetActive(value: true);
			Transform transform = worldTextInstance.m_gui.transform.Find("Text");
			worldTextInstance.m_textMeshField = transform.GetComponent<TextMeshProUGUI>();
			m_worldTexts.Add(worldTextInstance);
		}
		worldTextInstance.m_userInfo = user;
		worldTextInstance.m_type = type;
		worldTextInstance.m_go = go;
		worldTextInstance.m_position = position;
		Color color;
		switch (type)
		{
		case Talker.Type.Shout:
			color = Color.yellow;
			text = text.ToUpper();
			break;
		case Talker.Type.Whisper:
			color = new Color(1f, 1f, 1f, 0.75f);
			text = text.ToLowerInvariant();
			break;
		case Talker.Type.Ping:
			color = new Color(0.6f, 0.7f, 1f, 1f);
			text = "PING";
			break;
		default:
			color = Color.white;
			break;
		}
		worldTextInstance.m_textMeshField.color = color;
		worldTextInstance.m_timer = 0f;
		worldTextInstance.m_text = text;
		UpdateWorldTextField(worldTextInstance);
	}

	private void UpdateWorldTextField(WorldTextInstance wt)
	{
		string text = "";
		if (wt.m_type == Talker.Type.Shout || wt.m_type == Talker.Type.Ping)
		{
			text = wt.m_name + ": ";
		}
		text += wt.m_text;
		wt.m_textMeshField.text = text;
	}

	private WorldTextInstance FindExistingWorldText(long senderID)
	{
		foreach (WorldTextInstance worldText in m_worldTexts)
		{
			if (worldText.m_talkerID == senderID)
			{
				return worldText;
			}
		}
		return null;
	}

	protected override bool isAllowedCommand(ConsoleCommand cmd)
	{
		if (cmd.IsCheat)
		{
			return false;
		}
		return base.isAllowedCommand(cmd);
	}

	protected override void InputText()
	{
		string text = m_input.text;
		if (text.Length != 0)
		{
			text = ((text[0] != '/') ? ("say " + text) : text.Substring(1));
			TryRunCommand(text, this);
		}
	}

	public void TeleportPlayer(long targetPeerID, Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC(targetPeerID, "RPC_TeleportPlayer", pos, rot, distantTeleport);
	}

	private void RPC_TeleportPlayer(long sender, Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (Player.m_localPlayer != null)
		{
			Player.m_localPlayer.TeleportTo(pos, rot, distantTeleport);
		}
	}

	private void RPC_ChatMessage(long sender, Vector3 position, int type, UserInfo userInfo, string text, string senderAccountId)
	{
		OnNewChatMessage(null, sender, position, (Talker.Type)type, userInfo, text, senderAccountId);
	}

	public void SendText(Talker.Type type, string text)
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			if (type == Talker.Type.Shout)
			{
				ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", localPlayer.GetHeadPoint(), 2, UserInfo.GetLocalUser(), text, PrivilegeManager.GetNetworkUserId());
			}
			else
			{
				localPlayer.GetComponent<Talker>().Say(type, text);
			}
		}
	}

	public void SendPing(Vector3 position)
	{
		Player localPlayer = Player.m_localPlayer;
		if ((bool)localPlayer)
		{
			Vector3 vector = position;
			vector.y = localPlayer.transform.position.y;
			ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "ChatMessage", vector, 3, UserInfo.GetLocalUser(), "", PrivilegeManager.GetNetworkUserId());
			if (Player.m_debugMode && Console.instance != null && Console.instance.IsCheatsEnabled() && Console.instance != null)
			{
				Console.instance.AddString($"Pinged at: {vector.x}, {vector.z}");
			}
		}
	}

	public void GetShoutWorldTexts(List<WorldTextInstance> texts)
	{
		foreach (WorldTextInstance worldText in m_worldTexts)
		{
			if (worldText.m_type == Talker.Type.Shout)
			{
				texts.Add(worldText);
			}
		}
	}

	public void GetPingWorldTexts(List<WorldTextInstance> texts)
	{
		foreach (WorldTextInstance worldText in m_worldTexts)
		{
			if (worldText.m_type == Talker.Type.Ping)
			{
				texts.Add(worldText);
			}
		}
	}

	private void UpdateNpcTexts(float dt)
	{
		NpcText npcText = null;
		Camera mainCamera = Utils.GetMainCamera();
		foreach (NpcText npcText2 in m_npcTexts)
		{
			if (!npcText2.m_go)
			{
				npcText2.m_gui.SetActive(value: false);
				if (npcText == null)
				{
					npcText = npcText2;
				}
				continue;
			}
			if (npcText2.m_timeout)
			{
				npcText2.m_ttl -= dt;
				if (npcText2.m_ttl <= 0f)
				{
					npcText2.SetVisible(visible: false);
					if (!npcText2.IsVisible())
					{
						npcText = npcText2;
					}
					continue;
				}
			}
			Vector3 vector = npcText2.m_go.transform.position + npcText2.m_offset;
			Vector3 position = mainCamera.WorldToScreenPointScaled(vector);
			if (position.x < 0f || position.x > (float)Screen.width || position.y < 0f || position.y > (float)Screen.height || position.z < 0f)
			{
				npcText2.SetVisible(visible: false);
			}
			else
			{
				npcText2.SetVisible(visible: true);
				RectTransform rectTransform = npcText2.m_gui.transform as RectTransform;
				position.x = Mathf.Clamp(position.x, rectTransform.rect.width / 2f, (float)Screen.width - rectTransform.rect.width / 2f);
				position.y = Mathf.Clamp(position.y, rectTransform.rect.height / 2f, (float)Screen.height - rectTransform.rect.height);
				npcText2.m_gui.transform.position = position;
			}
			if (Vector3.Distance(mainCamera.transform.position, vector) > npcText2.m_cullDistance)
			{
				npcText2.SetVisible(visible: false);
				if (npcText == null && !npcText2.IsVisible())
				{
					npcText = npcText2;
				}
			}
		}
		if (npcText != null)
		{
			ClearNpcText(npcText);
		}
		if (Hud.instance.m_userHidden && m_npcTexts.Count > 0)
		{
			HideAllNpcTexts();
		}
	}

	public void HideAllNpcTexts()
	{
		for (int num = m_npcTexts.Count - 1; num >= 0; num--)
		{
			m_npcTexts[num].SetVisible(visible: false);
			ClearNpcText(m_npcTexts[num]);
		}
	}

	public void SetNpcText(GameObject talker, Vector3 offset, float cullDistance, float ttl, string topic, string text, bool large)
	{
		if (!Hud.instance.m_userHidden)
		{
			NpcText npcText = FindNpcText(talker);
			if (npcText != null)
			{
				ClearNpcText(npcText);
			}
			npcText = new NpcText();
			npcText.m_topic = topic;
			npcText.m_text = text;
			npcText.m_go = talker;
			npcText.m_gui = UnityEngine.Object.Instantiate(large ? m_npcTextBaseLarge : m_npcTextBase, base.transform);
			npcText.m_gui.SetActive(value: true);
			npcText.m_animator = npcText.m_gui.GetComponent<Animator>();
			npcText.m_topicField = npcText.m_gui.transform.Find("Topic").GetComponent<TextMeshProUGUI>();
			npcText.m_textField = npcText.m_gui.transform.Find("Text").GetComponent<TextMeshProUGUI>();
			npcText.m_ttl = ttl;
			npcText.m_timeout = ttl > 0f;
			npcText.m_offset = offset;
			npcText.m_cullDistance = cullDistance;
			npcText.UpdateText();
			m_npcTexts.Add(npcText);
		}
	}

	public int CurrentNpcTexts()
	{
		return m_npcTexts.Count;
	}

	public bool IsDialogVisible(GameObject talker)
	{
		return FindNpcText(talker)?.IsVisible() ?? false;
	}

	public void ClearNpcText(GameObject talker)
	{
		NpcText npcText = FindNpcText(talker);
		if (npcText != null)
		{
			ClearNpcText(npcText);
		}
	}

	private void ClearNpcText(NpcText npcText)
	{
		UnityEngine.Object.Destroy(npcText.m_gui);
		m_npcTexts.Remove(npcText);
	}

	private NpcText FindNpcText(GameObject go)
	{
		foreach (NpcText npcText in m_npcTexts)
		{
			if (npcText.m_go == go)
			{
				return npcText;
			}
		}
		return null;
	}
}
