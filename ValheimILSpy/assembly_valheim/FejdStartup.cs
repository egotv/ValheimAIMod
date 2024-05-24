using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using GUIFramework;
using PlatformTools.Core;
using SoftReferenceableAssets.SceneManagement;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FejdStartup : MonoBehaviour
{
	private delegate void ContinueAction();

	public struct StartGameEventArgs
	{
		public bool isHost;

		public StartGameEventArgs(bool isHost)
		{
			this.isHost = isHost;
		}
	}

	public delegate void StartGameEventHandler(object sender, StartGameEventArgs e);

	private Vector3 camSpeed = Vector3.zero;

	private Vector3 camRotSpeed = Vector3.zero;

	private const int maxRetries = 50;

	private static int retries = 0;

	private static FejdStartup m_instance;

	[Header("Start")]
	public Animator m_menuAnimator;

	public GameObject m_worldVersionPanel;

	public GameObject m_playerVersionPanel;

	public GameObject m_newGameVersionPanel;

	public GameObject m_connectionFailedPanel;

	public TMP_Text m_connectionFailedError;

	public TMP_Text m_newVersionName;

	public GameObject m_loading;

	public GameObject m_pleaseWait;

	public TMP_Text m_versionLabel;

	public GameObject m_mainMenu;

	public GameObject m_ndaPanel;

	public GameObject m_betaText;

	public GameObject m_moddedText;

	public Scrollbar m_patchLogScroll;

	public GameObject m_characterSelectScreen;

	public GameObject m_selectCharacterPanel;

	public GameObject m_newCharacterPanel;

	public GameObject m_creditsPanel;

	public GameObject m_startGamePanel;

	public GameObject m_createWorldPanel;

	public ServerOptionsGUI m_serverOptions;

	public Button m_serverOptionsButton;

	public GameObject m_menuList;

	private Button[] m_menuButtons;

	private Button m_menuSelectedButton;

	public RectTransform m_creditsList;

	public float m_creditsSpeed = 100f;

	public SceneReference m_startScene;

	public SceneReference m_mainScene;

	[Header("Camera")]
	public GameObject m_mainCamera;

	public Transform m_cameraMarkerStart;

	public Transform m_cameraMarkerMain;

	public Transform m_cameraMarkerCharacter;

	public Transform m_cameraMarkerCredits;

	public Transform m_cameraMarkerGame;

	public Transform m_cameraMarkerSaves;

	public float m_cameraMoveSpeed = 1.5f;

	public float m_cameraMoveSpeedStart = 1.5f;

	[Header("Join")]
	public GameObject m_serverListPanel;

	public Toggle m_publicServerToggle;

	public Toggle m_openServerToggle;

	public Toggle m_crossplayServerToggle;

	public Color m_toggleColor = new Color(1f, 0.6308316f, 0.2352941f);

	public GuiInputField m_serverPassword;

	public TMP_Text m_passwordError;

	public int m_minimumPasswordLength = 5;

	public float m_characterRotateSpeed = 4f;

	public float m_characterRotateSpeedGamepad = 200f;

	public int m_joinHostPort = 2456;

	[Header("World")]
	public GameObject m_worldListPanel;

	public RectTransform m_worldListRoot;

	public GameObject m_worldListElement;

	public ScrollRectEnsureVisible m_worldListEnsureVisible;

	public float m_worldListElementStep = 28f;

	public TextMeshProUGUI m_worldSourceInfo;

	public GameObject m_worldSourceInfoPanel;

	public GuiInputField m_newWorldName;

	public GuiInputField m_newWorldSeed;

	public Button m_newWorldDone;

	public Button m_worldStart;

	public Button m_worldRemove;

	public GameObject m_removeWorldDialog;

	public TMP_Text m_removeWorldName;

	public GameObject m_removeCharacterDialog;

	public TMP_Text m_removeCharacterName;

	public RectTransform m_tooltipAnchor;

	public RectTransform m_tooltipSecondaryAnchor;

	[Header("Character selection")]
	public Button m_csStartButton;

	public Button m_csNewBigButton;

	public Button m_csNewButton;

	public Button m_csRemoveButton;

	public Button m_csLeftButton;

	public Button m_csRightButton;

	public Button m_csNewCharacterDone;

	public Button m_csNewCharacterCancel;

	public GameObject m_newCharacterError;

	public TMP_Text m_csName;

	public TMP_Text m_csFileSource;

	public TMP_Text m_csSourceInfo;

	public GuiInputField m_csNewCharacterName;

	[Header("Misc")]
	public Transform m_characterPreviewPoint;

	public GameObject m_playerPrefab;

	public GameObject m_objectDBPrefab;

	public GameObject m_settingsPrefab;

	public GameObject m_consolePrefab;

	public GameObject m_feedbackPrefab;

	public GameObject m_changeEffectPrefab;

	public ManageSavesMenu m_manageSavesMenu;

	public GameObject m_cloudStorageWarningNextSave;

	private GameObject m_settingsPopup;

	private string m_downloadUrl = "";

	[TextArea]
	public string m_versionXmlUrl = "https://dl.dropboxusercontent.com/s/5ibm05oelbqt8zq/fejdversion.xml?dl=0";

	private World m_world;

	private bool m_startingWorld;

	private ServerStatus m_joinServer;

	private ServerJoinData m_queuedJoinServer;

	private float m_worldListBaseSize;

	private List<PlayerProfile> m_profiles;

	private int m_profileIndex;

	private string m_tempRemoveCharacterName = "";

	private FileHelpers.FileSource m_tempRemoveCharacterSource;

	private int m_tempRemoveCharacterIndex = -1;

	private BackgroundWorker m_moveFileWorker;

	private List<GameObject> m_worldListElements = new List<GameObject>();

	private List<World> m_worlds;

	private GameObject m_playerInstance;

	private static bool m_firstStartup = true;

	public static Action HandlePendingInvite;

	public static Action ResetPendingInvite;

	public static Action<Privilege> ResolvePrivilege;

	private static GameObject s_monoUpdaters = null;

	public static FejdStartup instance => m_instance;

	public static string InstanceId { get; private set; } = null;


	public static event StartGameEventHandler startGameEvent;

	private void Awake()
	{
		m_instance = this;
		ParseArguments();
		m_crossplayServerToggle.gameObject.SetActive(value: true);
		if (AwakePlatforms())
		{
			FileHelpers.UpdateCloudEnabledStatus();
			Settings.SetPlatformDefaultPrefs();
			QualitySettings.maxQueuedFrames = 2;
			ZLog.Log("Valheim version: " + Version.GetVersionString() + " (network version " + 27u + ")");
			Settings.ApplyStartupSettings();
			WorldGenerator.Initialize(World.GetMenuWorld());
			if (!Console.instance)
			{
				UnityEngine.Object.Instantiate(m_consolePrefab);
			}
			m_mainCamera.transform.position = m_cameraMarkerMain.transform.position;
			m_mainCamera.transform.rotation = m_cameraMarkerMain.transform.rotation;
			ZLog.Log("Render threading mode:" + SystemInfo.renderingThreadingMode);
			Gogan.StartSession();
			Gogan.LogEvent("Game", "Version", Version.GetVersionString(), 0L);
			Gogan.LogEvent("Game", "SteamID", SteamManager.APP_ID.ToString(), 0L);
			Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
			if (Settings.IsSteamRunningOnSteamDeck())
			{
				m_menuList.transform.Find("Menu")?.Find("showlog")?.gameObject.SetActive(value: false);
			}
			m_menuButtons = m_menuList.GetComponentsInChildren<Button>();
			TabHandler[] enabledComponentsInChildren = Utils.GetEnabledComponentsInChildren<TabHandler>(m_startGamePanel.gameObject);
			TabHandler[] array = enabledComponentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
			m_startGamePanel.gameObject.SetActive(value: true);
			m_serverOptions.gameObject.SetActive(value: true);
			m_serverOptions.gameObject.SetActive(value: false);
			m_startGamePanel.gameObject.SetActive(value: false);
			array = enabledComponentsInChildren;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			Game.Unpause();
			Time.timeScale = 1f;
			ZInput.Initialize();
			ZInput.OnInputLayoutChanged += UpdateCursor;
			UpdateCursor();
		}
	}

	public static bool AwakePlatforms()
	{
		if (s_monoUpdaters == null)
		{
			s_monoUpdaters = new GameObject();
			s_monoUpdaters.AddComponent<MonoUpdaters>();
			UnityEngine.Object.DontDestroyOnLoad(s_monoUpdaters);
		}
		if (!AwakeSteam() || !AwakePlayFab() || !AwakeCustom())
		{
			ZLog.LogError("Awake of network backend failed");
			return false;
		}
		return true;
	}

	private static bool AwakePlayFab()
	{
		PlayFabManager.Initialize();
		PlayFabManager.SetCustomId(PrivilegeManager.Platform.Steam, SteamUser.GetSteamID().ToString());
		return true;
	}

	private static bool AwakeSteam()
	{
		if (!InitializeSteam())
		{
			return false;
		}
		return true;
	}

	private static bool AwakeCustom()
	{
		return true;
	}

	private void OnDestroy()
	{
		SaveSystem.ClearWorldListCache(reload: false);
		m_instance = null;
		ZInput.OnInputLayoutChanged -= UpdateCursor;
		Localization.OnLanguageChange = (Action)Delegate.Remove(Localization.OnLanguageChange, new Action(OnLanguageChange));
	}

	private void OnApplicationQuit()
	{
		HeightmapBuilder.instance.Dispose();
	}

	private void OnEnable()
	{
		startGameEvent += AddToServerList;
	}

	private void OnDisable()
	{
		startGameEvent -= AddToServerList;
	}

	private void AddToServerList(object sender, StartGameEventArgs e)
	{
		if (!e.isHost)
		{
			ServerList.AddToRecentServersList(GetServerToJoin());
		}
	}

	private void Start()
	{
		SetupGui();
		SetupObjectDB();
		m_openServerToggle.onValueChanged.AddListener(OnOpenServerToggleClicked);
		MusicMan.instance.Reset();
		MusicMan.instance.TriggerMusic("menu");
		ShowConnectError();
		ZSteamMatchmaking.Initialize();
		if (m_firstStartup)
		{
			HandleStartupJoin();
		}
		m_menuAnimator.SetBool("FirstStartup", m_firstStartup);
		m_firstStartup = false;
		string @string = PlayerPrefs.GetString("profile");
		if (@string.Length > 0)
		{
			SetSelectedProfile(@string);
		}
		else
		{
			m_profiles = SaveSystem.GetAllPlayerProfiles();
			if (m_profiles.Count > 0)
			{
				SetSelectedProfile(m_profiles[0].GetFilename());
			}
			else
			{
				UpdateCharacterList();
			}
		}
		HandlePendingInvite?.Invoke();
		CensorShittyWords.UGCPopupShown = (Action)Delegate.Remove(CensorShittyWords.UGCPopupShown, new Action(OnUGCPopupShown));
		CensorShittyWords.UGCPopupShown = (Action)Delegate.Combine(CensorShittyWords.UGCPopupShown, new Action(OnUGCPopupShown));
		SaveSystem.ClearWorldListCache(reload: true);
		Player.m_debugMode = false;
	}

	private void SetupGui()
	{
		HideAll();
		m_mainMenu.SetActive(value: true);
		if (SteamManager.APP_ID == 1223920)
		{
			m_betaText.SetActive(value: true);
			if (!Debug.isDebugBuild && !AcceptedNDA())
			{
				m_ndaPanel.SetActive(value: true);
				m_mainMenu.SetActive(value: false);
			}
		}
		m_moddedText.SetActive(Game.isModded);
		m_worldListBaseSize = m_worldListRoot.rect.height;
		m_versionLabel.text = $"Version {Version.GetVersionString()} (n-{27u})";
		Localization.instance.Localize(base.transform);
		Localization.OnLanguageChange = (Action)Delegate.Combine(Localization.OnLanguageChange, new Action(OnLanguageChange));
	}

	private void HideAll()
	{
		m_worldVersionPanel.SetActive(value: false);
		m_playerVersionPanel.SetActive(value: false);
		m_newGameVersionPanel.SetActive(value: false);
		m_loading.SetActive(value: false);
		m_pleaseWait.SetActive(value: false);
		m_characterSelectScreen.SetActive(value: false);
		m_creditsPanel.SetActive(value: false);
		m_startGamePanel.SetActive(value: false);
		m_createWorldPanel.SetActive(value: false);
		m_serverOptions.gameObject.SetActive(value: false);
		m_mainMenu.SetActive(value: false);
		m_ndaPanel.SetActive(value: false);
		m_betaText.SetActive(value: false);
	}

	public static bool InitializeSteam()
	{
		if (SteamManager.Initialize())
		{
			string personaName = SteamFriends.GetPersonaName();
			ZLog.Log("Steam initialized, persona:" + personaName);
			PrivilegeData privilegeData = default(PrivilegeData);
			privilegeData.platformUserId = (ulong)SteamUser.GetSteamID();
			privilegeData.platformCanAccess = OnSteamCanAccess;
			privilegeData.canAccessOnlineMultiplayer = true;
			privilegeData.canViewUserGeneratedContentAll = true;
			privilegeData.canCrossplay = true;
			PrivilegeManager.SetPrivilegeData(privilegeData);
			return true;
		}
		ZLog.LogError("Steam is not initialized");
		Application.Quit();
		return false;
	}

	private void HandleStartupJoin()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string text = commandLineArgs[i];
			if (text == "+connect" && i < commandLineArgs.Length - 1)
			{
				string text2 = commandLineArgs[i + 1];
				ZLog.Log("JOIN " + text2);
				ZSteamMatchmaking.instance.QueueServerJoin(text2);
			}
			else if (text == "+connect_lobby" && i < commandLineArgs.Length - 1)
			{
				string s = commandLineArgs[i + 1];
				CSteamID lobbyID = new CSteamID(ulong.Parse(s));
				ZSteamMatchmaking.instance.QueueLobbyJoin(lobbyID);
			}
		}
	}

	private static void OnSteamCanAccess(PrivilegeManager.Permission permission, PrivilegeManager.User user, CanAccessResult cb)
	{
		if (user.platform == PrivilegeManager.Platform.Steam)
		{
			EFriendRelationship friendRelationship = SteamFriends.GetFriendRelationship((CSteamID)user.id);
			if (friendRelationship == EFriendRelationship.k_EFriendRelationshipIgnored || friendRelationship == EFriendRelationship.k_EFriendRelationshipIgnoredFriend)
			{
				cb(PrivilegeManager.Result.NotAllowed);
				return;
			}
		}
		cb(PrivilegeManager.Result.Allowed);
	}

	private void ParseArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (commandLineArgs[i] == "-console")
			{
				Console.SetConsoleEnabled(enabled: true);
			}
		}
	}

	private bool ParseServerArguments()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		string text = "Dedicated";
		string password = "";
		string text2 = "";
		int num = 2456;
		bool flag = true;
		ZNet.m_backupCount = 4;
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			string text3 = commandLineArgs[i].ToLower();
			switch (text3)
			{
			case "-world":
			{
				string text7 = commandLineArgs[i + 1];
				if (text7 != "")
				{
					text = text7;
				}
				i++;
				continue;
			}
			case "-name":
			{
				string text6 = commandLineArgs[i + 1];
				if (text6 != "")
				{
					text2 = text6;
				}
				i++;
				continue;
			}
			case "-port":
			{
				string text4 = commandLineArgs[i + 1];
				if (text4 != "")
				{
					num = int.Parse(text4);
				}
				i++;
				continue;
			}
			case "-password":
				password = commandLineArgs[i + 1];
				i++;
				continue;
			case "-savedir":
			{
				string text5 = commandLineArgs[i + 1];
				Utils.SetSaveDataPath(text5);
				ZLog.Log("Setting -savedir to: " + text5);
				i++;
				continue;
			}
			case "-public":
			{
				string text8 = commandLineArgs[i + 1];
				if (text8 != "")
				{
					flag = text8 == "1";
				}
				i++;
				continue;
			}
			}
			int result;
			int result2;
			int result3;
			int result4;
			if (text3.ToLower() == "-logfile")
			{
				ZLog.Log("Setting -logfile to: " + commandLineArgs[i + 1]);
			}
			else if (text3 == "-crossplay")
			{
				ZNet.m_onlineBackend = OnlineBackendType.PlayFab;
			}
			else if (text3 == "-instanceid" && commandLineArgs.Length > i + 1)
			{
				InstanceId = commandLineArgs[i + 1];
				i++;
			}
			else if (text3.ToLower() == "-backups" && int.TryParse(commandLineArgs[i + 1], out result))
			{
				ZNet.m_backupCount = result;
			}
			else if (text3 == "-backupshort" && int.TryParse(commandLineArgs[i + 1], out result2))
			{
				ZNet.m_backupShort = Mathf.Max(5, result2);
			}
			else if (text3 == "-backuplong" && int.TryParse(commandLineArgs[i + 1], out result3))
			{
				ZNet.m_backupLong = Mathf.Max(5, result3);
			}
			else if (text3 == "-saveinterval" && int.TryParse(commandLineArgs[i + 1], out result4))
			{
				Game.m_saveInterval = Mathf.Max(5, result4);
			}
		}
		if (text2 == "")
		{
			text2 = text;
		}
		World createWorld = World.GetCreateWorld(text, FileHelpers.FileSource.Local);
		if (!ServerOptionsGUI.m_instance)
		{
			UnityEngine.Object.Instantiate(m_serverOptions).gameObject.SetActive(value: true);
		}
		for (int j = 0; j < commandLineArgs.Length; j++)
		{
			string text9 = commandLineArgs[j].ToLower();
			if (text9 == "-resetmodifiers")
			{
				createWorld.m_startingGlobalKeys.Clear();
				createWorld.m_startingKeysChanged = true;
				ZLog.Log("Resetting world modifiers");
			}
			else if (text9 == "-preset" && commandLineArgs.Length > j + 1)
			{
				string text10 = commandLineArgs[j + 1];
				if (Enum.TryParse<WorldPresets>(text10, ignoreCase: true, out var result5))
				{
					createWorld.m_startingGlobalKeys.Clear();
					createWorld.m_startingKeysChanged = true;
					ServerOptionsGUI.m_instance.ReadKeys(createWorld);
					ServerOptionsGUI.m_instance.SetPreset(createWorld, result5);
					ServerOptionsGUI.m_instance.SetKeys(createWorld);
					ZLog.Log("Setting world modifier preset: " + text10);
				}
				else
				{
					ZLog.LogError("Could not parse '" + text10 + "' as a world modifier preset.");
				}
			}
			else if (text9 == "-modifier" && commandLineArgs.Length > j + 2)
			{
				string text11 = commandLineArgs[j + 1];
				string text12 = commandLineArgs[j + 2];
				if (Enum.TryParse<WorldModifiers>(text11, ignoreCase: true, out var result6) && Enum.TryParse<WorldModifierOption>(text12, ignoreCase: true, out var result7))
				{
					ServerOptionsGUI.m_instance.ReadKeys(createWorld);
					ServerOptionsGUI.m_instance.SetPreset(createWorld, result6, result7);
					ServerOptionsGUI.m_instance.SetKeys(createWorld);
					ZLog.Log("Setting world modifier: " + text11 + "->" + text12);
				}
				else
				{
					ZLog.LogError("Could not parse '" + text11 + "' with a value of '" + text12 + "' as a world modifier.");
				}
			}
			else if (text9 == "-setkey" && commandLineArgs.Length > j + 1)
			{
				string text13 = commandLineArgs[j + 1];
				if (!createWorld.m_startingGlobalKeys.Contains(text13))
				{
					createWorld.m_startingGlobalKeys.Add(text13.ToLower());
				}
			}
		}
		if (flag && !IsPublicPasswordValid(password, createWorld))
		{
			string publicPasswordError = GetPublicPasswordError(password, createWorld);
			ZLog.LogError("Error bad password:" + publicPasswordError);
			Application.Quit();
			return false;
		}
		ZNet.SetServer(server: true, openServer: true, flag, text2, password, createWorld);
		ZNet.ResetServerHost();
		SteamManager.SetServerPort(num);
		ZSteamSocket.SetDataPort(num);
		ZPlayFabMatchmaking.SetDataPort(num);
		if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
		{
			ZPlayFabMatchmaking.LookupPublicIP();
		}
		return true;
	}

	private void SetupObjectDB()
	{
		ObjectDB objectDB = base.gameObject.AddComponent<ObjectDB>();
		ObjectDB component = m_objectDBPrefab.GetComponent<ObjectDB>();
		objectDB.CopyOtherDB(component);
	}

	private void ShowConnectError(ZNet.ConnectionStatus statusOverride = ZNet.ConnectionStatus.None)
	{
		ZNet.ConnectionStatus connectionStatus = ((statusOverride == ZNet.ConnectionStatus.None) ? ZNet.GetConnectionStatus() : statusOverride);
		if (ZNet.m_loadError)
		{
			m_connectionFailedPanel.SetActive(value: true);
			m_connectionFailedError.text = Localization.instance.Localize("$error_worldfileload");
		}
		if (ZNet.m_loadError)
		{
			m_connectionFailedPanel.SetActive(value: true);
			m_connectionFailedError.text = Localization.instance.Localize("$error_worldfileload");
		}
		if (connectionStatus != ZNet.ConnectionStatus.Connected && connectionStatus != ZNet.ConnectionStatus.Connecting && connectionStatus != 0)
		{
			m_connectionFailedPanel.SetActive(value: true);
			switch (connectionStatus)
			{
			case ZNet.ConnectionStatus.ErrorVersion:
				m_connectionFailedError.text = Localization.instance.Localize("$error_incompatibleversion");
				break;
			case ZNet.ConnectionStatus.ErrorConnectFailed:
				m_connectionFailedError.text = Localization.instance.Localize("$error_failedconnect");
				break;
			case ZNet.ConnectionStatus.ErrorDisconnected:
				m_connectionFailedError.text = Localization.instance.Localize("$error_disconnected");
				break;
			case ZNet.ConnectionStatus.ErrorPassword:
				m_connectionFailedError.text = Localization.instance.Localize("$error_password");
				break;
			case ZNet.ConnectionStatus.ErrorAlreadyConnected:
				m_connectionFailedError.text = Localization.instance.Localize("$error_alreadyconnected");
				break;
			case ZNet.ConnectionStatus.ErrorBanned:
				m_connectionFailedError.text = Localization.instance.Localize("$error_banned");
				break;
			case ZNet.ConnectionStatus.ErrorFull:
				m_connectionFailedError.text = Localization.instance.Localize("$error_serverfull");
				break;
			case ZNet.ConnectionStatus.ErrorPlatformExcluded:
				m_connectionFailedError.text = Localization.instance.Localize("$error_platformexcluded");
				break;
			case ZNet.ConnectionStatus.ErrorCrossplayPrivilege:
				m_connectionFailedError.text = Localization.instance.Localize("$xbox_error_crossplayprivilege");
				break;
			case ZNet.ConnectionStatus.ErrorKicked:
				m_connectionFailedError.text = Localization.instance.Localize("$error_kicked");
				break;
			}
		}
	}

	public void OnNewVersionButtonDownload()
	{
		Application.OpenURL(m_downloadUrl);
		Application.Quit();
	}

	public void OnNewVersionButtonContinue()
	{
		m_newGameVersionPanel.SetActive(value: false);
	}

	public void OnStartGame()
	{
		Gogan.LogEvent("Screen", "Enter", "StartGame", 0L);
		m_mainMenu.SetActive(value: false);
		ShowCharacterSelection();
	}

	private void ShowStartGame()
	{
		m_mainMenu.SetActive(value: false);
		m_createWorldPanel.SetActive(value: false);
		m_serverOptions.gameObject.SetActive(value: false);
		m_startGamePanel.SetActive(value: true);
		RefreshWorldSelection();
	}

	public void OnSelectWorldTab()
	{
		RefreshWorldSelection();
	}

	private void RefreshWorldSelection()
	{
		UpdateWorldList(centerSelection: true);
		if (m_world != null)
		{
			m_world = FindWorld(m_world.m_name);
			if (m_world != null)
			{
				UpdateWorldList(centerSelection: true);
			}
		}
		if (m_world == null)
		{
			string @string = PlayerPrefs.GetString("world");
			if (@string.Length > 0)
			{
				m_world = FindWorld(@string);
			}
			if (m_world == null)
			{
				m_world = ((m_worlds.Count > 0) ? m_worlds[0] : null);
			}
			if (m_world != null)
			{
				UpdateWorldList(centerSelection: true);
			}
			m_crossplayServerToggle.isOn = PlayerPrefs.GetInt("crossplay", 1) == 1;
		}
	}

	public void OnServerListTab()
	{
		if (!PrivilegeManager.CanAccessOnlineMultiplayer)
		{
			m_startGamePanel.transform.GetChild(0).GetComponent<TabHandler>().SetActiveTab(0);
			ShowOnlineMultiplayerPrivilegeWarning();
		}
	}

	private void OnOpenServerToggleClicked(bool value)
	{
		if (value && !PrivilegeManager.CanAccessOnlineMultiplayer)
		{
			m_openServerToggle.isOn = false;
			ShowOnlineMultiplayerPrivilegeWarning();
		}
	}

	private void ShowOnlineMultiplayerPrivilegeWarning()
	{
		if (!PrivilegeManager.HasPrivilegeData || PlayFabManager.CurrentLoginState != LoginState.LoggedIn)
		{
			UnifiedPopup.Push(new WarningPopup("$menu_logintext", "$menu_loginfailedtext", delegate
			{
				RefreshWorldSelection();
				UnifiedPopup.Pop();
			}));
		}
		else
		{
			UnifiedPopup.Push(new WarningPopup("$menu_privilegerequiredheader", "$menu_onlineprivilegetext", delegate
			{
				RefreshWorldSelection();
				UnifiedPopup.Pop();
			}));
		}
	}

	private void OnUGCPopupShown()
	{
		RefreshWorldSelection();
	}

	private World FindWorld(string name)
	{
		foreach (World world in m_worlds)
		{
			if (world.m_name == name)
			{
				return world;
			}
		}
		return null;
	}

	private void UpdateWorldList(bool centerSelection)
	{
		m_worlds = SaveSystem.GetWorldList();
		float b = (float)m_worlds.Count * m_worldListElementStep;
		b = Mathf.Max(m_worldListBaseSize, b);
		m_worldListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, b);
		for (int i = 0; i < m_worlds.Count; i++)
		{
			World world = m_worlds[i];
			GameObject gameObject;
			if (i < m_worldListElements.Count)
			{
				gameObject = m_worldListElements[i];
			}
			else
			{
				gameObject = UnityEngine.Object.Instantiate(m_worldListElement, m_worldListRoot);
				m_worldListElements.Add(gameObject);
				gameObject.SetActive(value: true);
			}
			(gameObject.transform as RectTransform).anchoredPosition = new Vector2(0f, (float)i * (0f - m_worldListElementStep));
			Button component = gameObject.GetComponent<Button>();
			component.onClick.RemoveAllListeners();
			int index = i;
			component.onClick.AddListener(delegate
			{
				OnSelectWorld(index);
			});
			TMP_Text component2 = gameObject.transform.Find("seed").GetComponent<TMP_Text>();
			component2.text = world.m_seedName;
			gameObject.transform.Find("modifiers").GetComponent<TMP_Text>().text = Localization.instance.Localize(ServerOptionsGUI.GetWorldModifierSummary(world.m_startingGlobalKeys, alwaysShort: true));
			TMP_Text component3 = gameObject.transform.Find("name").GetComponent<TMP_Text>();
			if (world.m_name == world.m_fileName)
			{
				component3.text = world.m_name;
			}
			else
			{
				component3.text = world.m_name + " (" + world.m_fileName + ")";
			}
			gameObject.transform.Find("source_cloud")?.gameObject.SetActive(world.m_fileSource == FileHelpers.FileSource.Cloud);
			gameObject.transform.Find("source_local")?.gameObject.SetActive(world.m_fileSource == FileHelpers.FileSource.Local);
			gameObject.transform.Find("source_legacy")?.gameObject.SetActive(world.m_fileSource == FileHelpers.FileSource.Legacy);
			switch (world.m_dataError)
			{
			case World.SaveDataError.BadVersion:
				component2.text = " [BAD VERSION]";
				break;
			case World.SaveDataError.LoadError:
				component2.text = " [LOAD ERROR]";
				break;
			case World.SaveDataError.Corrupt:
				component2.text = " [CORRUPT]";
				break;
			case World.SaveDataError.MissingMeta:
				component2.text = " [MISSING META]";
				break;
			case World.SaveDataError.MissingDB:
				component2.text = " [MISSING DB]";
				break;
			default:
				component2.text = $" [{world.m_dataError}]";
				break;
			case World.SaveDataError.None:
				break;
			}
			RectTransform rectTransform = gameObject.transform.Find("selected") as RectTransform;
			bool flag = m_world != null && world.m_fileName == m_world.m_fileName;
			if (flag && m_world != world)
			{
				m_world = world;
			}
			rectTransform.gameObject.SetActive(flag);
			if (flag)
			{
				component.Select();
			}
			if (flag && centerSelection)
			{
				m_worldListEnsureVisible.CenterOnItem(rectTransform);
			}
		}
		for (int num = m_worldListElements.Count - 1; num >= m_worlds.Count; num--)
		{
			UnityEngine.Object.Destroy(m_worldListElements[num]);
			m_worldListElements.RemoveAt(num);
		}
		m_worldSourceInfo.text = "";
		m_worldSourceInfoPanel.SetActive(value: false);
		if (m_world != null)
		{
			m_worldSourceInfo.text = Localization.instance.Localize(((m_world.m_fileSource == FileHelpers.FileSource.Legacy) ? "$menu_legacynotice \n\n$menu_legacynotice_worlds \n\n" : "") + ((!FileHelpers.m_cloudEnabled) ? "$menu_cloudsavesdisabled" : ""));
			m_worldSourceInfoPanel.SetActive(m_worldSourceInfo.text.Length > 0);
		}
		for (int j = 0; j < m_worlds.Count; j++)
		{
			World world2 = m_worlds[j];
			UITooltip componentInChildren = m_worldListElements[j].GetComponentInChildren<UITooltip>();
			if ((object)componentInChildren != null)
			{
				string worldModifierSummary = ServerOptionsGUI.GetWorldModifierSummary(world2.m_startingGlobalKeys, alwaysShort: false, "\n");
				componentInChildren.Set(string.IsNullOrEmpty(worldModifierSummary) ? "" : "$menu_serveroptions", worldModifierSummary, m_worldSourceInfoPanel.activeSelf ? m_tooltipSecondaryAnchor : m_tooltipAnchor);
			}
		}
	}

	public void OnWorldRemove()
	{
		if (m_world != null)
		{
			m_removeWorldName.text = m_world.m_fileName;
			m_removeWorldDialog.SetActive(value: true);
		}
	}

	public void OnButtonRemoveWorldYes()
	{
		World.RemoveWorld(m_world.m_fileName, m_world.m_fileSource);
		m_world = null;
		m_worlds = SaveSystem.GetWorldList();
		SetSelectedWorld(0, centerSelection: true);
		m_removeWorldDialog.SetActive(value: false);
	}

	public void OnButtonRemoveWorldNo()
	{
		m_removeWorldDialog.SetActive(value: false);
	}

	private void OnSelectWorld(int index)
	{
		SetSelectedWorld(index, centerSelection: false);
	}

	private void SetSelectedWorld(int index, bool centerSelection)
	{
		if (m_worlds.Count != 0)
		{
			index = Mathf.Clamp(index, 0, m_worlds.Count - 1);
			m_world = m_worlds[index];
		}
		UpdateWorldList(centerSelection);
	}

	private int GetSelectedWorld()
	{
		if (m_world == null)
		{
			return -1;
		}
		for (int i = 0; i < m_worlds.Count; i++)
		{
			if (m_worlds[i].m_fileName == m_world.m_fileName)
			{
				return i;
			}
		}
		return -1;
	}

	private int FindSelectedWorld(GameObject button)
	{
		for (int i = 0; i < m_worldListElements.Count; i++)
		{
			if (m_worldListElements[i] == button)
			{
				return i;
			}
		}
		return -1;
	}

	private FileHelpers.FileSource GetMoveTarget(FileHelpers.FileSource source)
	{
		if (source == FileHelpers.FileSource.Cloud)
		{
			return FileHelpers.FileSource.Local;
		}
		return FileHelpers.FileSource.Cloud;
	}

	public void OnWorldNew()
	{
		m_createWorldPanel.SetActive(value: true);
		m_newWorldName.text = "";
		m_newWorldSeed.text = World.GenerateSeed();
	}

	public void OnNewWorldDone(bool forceLocal)
	{
		string text = m_newWorldName.text;
		string text2 = m_newWorldSeed.text;
		if (World.HaveWorld(text))
		{
			UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$menu_newworldalreadyexists"), Localization.instance.Localize("$menu_newworldalreadyexistsmessage", text), delegate
			{
				UnifiedPopup.Pop();
			}, localizeText: false));
			return;
		}
		m_world = new World(text, text2);
		m_world.m_fileSource = ((!FileHelpers.m_cloudEnabled || forceLocal) ? FileHelpers.FileSource.Local : FileHelpers.FileSource.Cloud);
		m_world.m_needsDB = false;
		if (m_world.m_fileSource == FileHelpers.FileSource.Cloud && FileHelpers.OperationExceedsCloudCapacity(2097152uL))
		{
			ShowCloudQuotaWorldDialog();
			ZLog.LogWarning("This operation may exceed the cloud save quota and has therefore been aborted! Prompt shown to user.");
			return;
		}
		m_world.SaveWorldMetaData(DateTime.Now);
		UpdateWorldList(centerSelection: true);
		ShowStartGame();
		Gogan.LogEvent("Menu", "NewWorld", text, 0L);
	}

	public void OnNewWorldBack()
	{
		ShowStartGame();
	}

	public void OnServerOptions()
	{
		RefreshWorldSelection();
		m_serverOptions.gameObject.SetActive(value: true);
		m_serverOptions.ReadKeys(m_world);
		EventSystem.current.SetSelectedGameObject(m_serverOptions.m_doneButton);
		if (PlatformPrefs.GetInt("ServerOptionsDisclaimer") == 0)
		{
			UnifiedPopup.Push(new WarningPopup("$menu_modifier_popup_title", "$menu_modifier_popup_text", delegate
			{
				UnifiedPopup.Pop();
			}));
			PlatformPrefs.SetInt("ServerOptionsDisclaimer", 1);
		}
	}

	public void OnServerOptionsDone()
	{
		m_world.m_startingGlobalKeys.Clear();
		m_world.m_startingKeysChanged = true;
		m_serverOptions.SetKeys(m_world);
		DateTime now = DateTime.Now;
		if (!SaveSystem.TryGetSaveByName(m_world.m_fileName, SaveDataType.World, out var save) || save.IsDeleted)
		{
			ZLog.LogError("Failed to retrieve world save " + m_world.m_fileName + " by name when modifying server options!");
			ShowStartGame();
			return;
		}
		SaveSystem.CheckMove(m_world.m_fileName, SaveDataType.World, ref m_world.m_fileSource, now, save.PrimaryFile.Size, copyToNewLocation: true);
		m_world.SaveWorldMetaData(now);
		UpdateWorldList(centerSelection: true);
		ShowStartGame();
	}

	public void OnServerOptionsCancel()
	{
		ShowStartGame();
	}

	public void OnMerchStoreButton()
	{
		Application.OpenURL("http://valheim.shop/?game_" + Version.GetPlatformPrefix("win"));
	}

	public void OnCloudStorageLowNextSaveWarningOk()
	{
		m_cloudStorageWarningNextSave.SetActive(value: false);
		RefreshWorldSelection();
	}

	public void OnWorldStart()
	{
		if (!SaveSystem.CanSaveToCloudStorage(m_world, m_profiles[m_profileIndex]) || Menu.ExceedCloudStorageTest)
		{
			m_cloudStorageWarningNextSave.SetActive(value: true);
		}
		else
		{
			if (m_world == null || m_startingWorld)
			{
				return;
			}
			Game.m_serverOptionsSummary = "";
			switch (m_world.m_dataError)
			{
			case World.SaveDataError.LoadError:
			case World.SaveDataError.Corrupt:
			{
				if (!SaveSystem.TryGetSaveByName(m_world.m_name, SaveDataType.World, out var save))
				{
					UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$menu_checklogfile", UnifiedPopup.Pop));
					ZLog.LogError("Failed to restore backup! Couldn't get world " + m_world.m_name + " by name from save system.");
				}
				else if (save.IsDeleted)
				{
					UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$menu_checklogfile", UnifiedPopup.Pop));
					ZLog.LogError("Failed to restore backup! World " + m_world.m_name + " retrieved from save system was deleted.");
				}
				else if (SaveSystem.HasRestorableBackup(save))
				{
					RestoreBackupPrompt(save);
				}
				else
				{
					UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$error_nosuitablebackupfound", UnifiedPopup.Pop));
				}
				break;
			}
			case World.SaveDataError.MissingMeta:
			{
				if (!SaveSystem.TryGetSaveByName(m_world.m_name, SaveDataType.World, out var save2))
				{
					UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$menu_checklogfile", UnifiedPopup.Pop));
					ZLog.LogError("Failed to restore meta file! Couldn't get world " + m_world.m_name + " by name from save system.");
				}
				else if (save2.IsDeleted)
				{
					UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$menu_checklogfile", UnifiedPopup.Pop));
					ZLog.LogError("Failed to restore meta file! World " + m_world.m_name + " retrieved from save system was deleted.");
				}
				else if (SaveSystem.HasBackupWithMeta(save2))
				{
					RestoreMetaFromBackupPrompt(save2);
				}
				else
				{
					UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$error_nosuitablebackupfound", UnifiedPopup.Pop));
				}
				break;
			}
			case World.SaveDataError.BadVersion:
				break;
			case World.SaveDataError.None:
			{
				PlayerPrefs.SetString("world", m_world.m_name);
				if (m_crossplayServerToggle.IsInteractable())
				{
					PlayerPrefs.SetInt("crossplay", m_crossplayServerToggle.isOn ? 1 : 0);
				}
				bool isOn = m_publicServerToggle.isOn;
				bool isOn2 = m_openServerToggle.isOn;
				bool isOn3 = m_crossplayServerToggle.isOn;
				string text = m_serverPassword.text;
				OnlineBackendType onlineBackend = GetOnlineBackend(isOn3);
				if (isOn2 && onlineBackend == OnlineBackendType.PlayFab && !PlayFabManager.IsLoggedIn)
				{
					ContinueWhenLoggedInPopup(OnWorldStart);
					break;
				}
				ZNet.m_onlineBackend = onlineBackend;
				ZSteamMatchmaking.instance.StopServerListing();
				m_startingWorld = true;
				ZNet.SetServer(server: true, isOn2, isOn, m_world.m_name, text, m_world);
				ZNet.ResetServerHost();
				string eventLabel = "open:" + isOn2 + ",public:" + isOn;
				Gogan.LogEvent("Menu", "WorldStart", eventLabel, 0L);
				FejdStartup.startGameEvent?.Invoke(this, new StartGameEventArgs(isHost: true));
				TransitionToMainScene();
				break;
			}
			}
		}
		void RestoreBackupPrompt(SaveWithBackups saveToRestore)
		{
			UnifiedPopup.Push(new YesNoPopup("$menu_restorebackup", "$menu_corruptsaverestore", delegate
			{
				UnifiedPopup.Pop();
				SaveSystem.RestoreBackupResult restoreBackupResult2 = SaveSystem.RestoreMostRecentBackup(saveToRestore);
				switch (restoreBackupResult2)
				{
				case SaveSystem.RestoreBackupResult.Success:
					SaveSystem.ClearWorldListCache(reload: true);
					RefreshWorldSelection();
					break;
				case SaveSystem.RestoreBackupResult.NoBackup:
					UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$error_nosuitablebackupfound", UnifiedPopup.Pop));
					break;
				default:
					UnifiedPopup.Push(new WarningPopup("$error_cantrestorebackup", "$menu_checklogfile", UnifiedPopup.Pop));
					ZLog.LogError($"Failed to restore backup! Result: {restoreBackupResult2}");
					break;
				}
			}, UnifiedPopup.Pop));
		}
		void RestoreMetaFromBackupPrompt(SaveWithBackups saveToRestore)
		{
			UnifiedPopup.Push(new YesNoPopup("$menu_restorebackup", "$menu_missingmetarestore", delegate
			{
				UnifiedPopup.Pop();
				SaveSystem.RestoreBackupResult restoreBackupResult = SaveSystem.RestoreMetaFromMostRecentBackup(saveToRestore.PrimaryFile);
				switch (restoreBackupResult)
				{
				case SaveSystem.RestoreBackupResult.Success:
					RefreshWorldSelection();
					break;
				case SaveSystem.RestoreBackupResult.NoBackup:
					UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$error_nosuitablebackupfound", UnifiedPopup.Pop));
					break;
				default:
					UnifiedPopup.Push(new WarningPopup("$error_cantrestoremeta", "$menu_checklogfile", UnifiedPopup.Pop));
					ZLog.LogError($"Failed to restore meta file! Result: {restoreBackupResult}");
					break;
				}
			}, UnifiedPopup.Pop));
		}
	}

	private void ContinueWhenLoggedInPopup(ContinueAction continueAction)
	{
		string headerText = Localization.instance.Localize("$menu_loginheader");
		string loggingInText = Localization.instance.Localize("$menu_logintext");
		string retryText = "";
		int previousRetryCountdown = -1;
		UnifiedPopup.Push(new CancelableTaskPopup(() => headerText, delegate
		{
			if (PlayFabManager.CurrentLoginState == LoginState.WaitingForRetry)
			{
				int num = Mathf.CeilToInt((float)(PlayFabManager.NextRetryUtc - DateTime.UtcNow).TotalSeconds);
				if (previousRetryCountdown != num)
				{
					previousRetryCountdown = num;
					retryText = Localization.instance.Localize("$menu_loginfailedtext") + "\n" + Localization.instance.Localize("$menu_loginretrycountdowntext", num.ToString());
				}
				return retryText;
			}
			return loggingInText;
		}, delegate
		{
			if (PlayFabManager.IsLoggedIn)
			{
				continueAction?.Invoke();
			}
			return PlayFabManager.IsLoggedIn;
		}, delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	private OnlineBackendType GetOnlineBackend(bool crossplayServer)
	{
		OnlineBackendType result = OnlineBackendType.Steamworks;
		if (crossplayServer)
		{
			result = OnlineBackendType.PlayFab;
		}
		return result;
	}

	private void ShowCharacterSelection()
	{
		Gogan.LogEvent("Screen", "Enter", "CharacterSelection", 0L);
		ZLog.Log("show character selection");
		m_characterSelectScreen.SetActive(value: true);
		m_selectCharacterPanel.SetActive(value: true);
		m_newCharacterPanel.SetActive(value: false);
		if (m_profiles == null)
		{
			m_profiles = SaveSystem.GetAllPlayerProfiles();
		}
		if (m_profileIndex >= m_profiles.Count)
		{
			m_profileIndex = m_profiles.Count - 1;
		}
		if (m_profileIndex >= 0 && m_profileIndex < m_profiles.Count)
		{
			PlayerProfile playerProfile = m_profiles[m_profileIndex];
			m_csFileSource.text = Localization.instance.Localize(FileHelpers.GetSourceString(playerProfile.m_fileSource));
		}
	}

	public void OnJoinStart()
	{
		JoinServer();
	}

	public void JoinServer()
	{
		if (!PlayFabManager.IsLoggedIn && m_joinServer.m_joinData is ServerJoinDataPlayFabUser)
		{
			ContinueWhenLoggedInPopup(JoinServer);
			return;
		}
		if (!PrivilegeManager.CanAccessOnlineMultiplayer)
		{
			ZLog.LogWarning("You should always prevent JoinServer() from being called when user does not have online multiplayer privilege!");
			HideAll();
			m_mainMenu.SetActive(value: true);
			ShowOnlineMultiplayerPrivilegeWarning();
			return;
		}
		if (m_joinServer.OnlineStatus == OnlineStatus.Online && m_joinServer.m_networkVersion != 27)
		{
			UnifiedPopup.Push(new WarningPopup("$error_incompatibleversion", (27 < m_joinServer.m_networkVersion) ? "$error_needslocalupdatetojoin" : "$error_needsserverupdatetojoin", delegate
			{
				UnifiedPopup.Pop();
			}));
			return;
		}
		if (m_joinServer.PlatformRestriction != 0 && !m_joinServer.IsJoinable)
		{
			if (m_joinServer.IsCrossplay)
			{
				UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$error_failedconnect"), Localization.instance.Localize("$xbox_error_crossplayprivilege"), delegate
				{
					UnifiedPopup.Pop();
				}, localizeText: false));
				return;
			}
			if (!m_joinServer.IsRestrictedToOwnPlatform)
			{
				UnifiedPopup.Push(new WarningPopup(Localization.instance.Localize("$error_failedconnect"), Localization.instance.Localize("$xbox_error_crossplayprivilege"), delegate
				{
					UnifiedPopup.Pop();
				}, localizeText: false));
				return;
			}
			ZLog.LogWarning("This part of the code should be unreachable unless the way ServerStatus works has been changed. The connection should've been prevented but it will be tried anyway.");
		}
		ZNet.SetServer(server: false, openServer: false, publicServer: false, "", "", null);
		bool flag = false;
		if (m_joinServer.m_joinData is ServerJoinDataSteamUser)
		{
			ZNet.SetServerHost((ulong)(m_joinServer.m_joinData as ServerJoinDataSteamUser).m_joinUserID);
			flag = true;
		}
		if (m_joinServer.m_joinData is ServerJoinDataPlayFabUser)
		{
			ZNet.SetServerHost((m_joinServer.m_joinData as ServerJoinDataPlayFabUser).m_remotePlayerId);
			flag = true;
		}
		if (m_joinServer.m_joinData is ServerJoinDataDedicated)
		{
			ServerJoinDataDedicated serverJoin = m_joinServer.m_joinData as ServerJoinDataDedicated;
			if (serverJoin.IsValid())
			{
				if (PlayFabManager.IsLoggedIn)
				{
					ZNet.ResetServerHost();
					ZPlayFabMatchmaking.FindHostByIp(serverJoin.GetIPPortString(), delegate(PlayFabMatchmakingServerData result)
					{
						if (result != null)
						{
							ZNet.SetServerHost(result.remotePlayerId);
							ZLog.Log("Determined backend of dedicated server to be PlayFab");
						}
						else
						{
							retries = 50;
						}
					}, delegate
					{
						ZNet.SetServerHost(serverJoin.GetIPString(), serverJoin.m_port, OnlineBackendType.Steamworks);
						ZLog.Log("Determined backend of dedicated server to be Steamworks");
					}, joinLobby: true);
				}
				else
				{
					ZNet.SetServerHost(serverJoin.GetIPString(), serverJoin.m_port, OnlineBackendType.Steamworks);
					ZLog.Log("Determined backend of dedicated server to be Steamworks");
				}
				flag = true;
			}
			else
			{
				flag = false;
			}
		}
		if (!flag)
		{
			Debug.LogError("Couldn't set the server host!");
			return;
		}
		Gogan.LogEvent("Menu", "JoinServer", "", 0L);
		ResetPendingInvite?.Invoke();
		FejdStartup.startGameEvent?.Invoke(this, new StartGameEventArgs(isHost: false));
		TransitionToMainScene();
	}

	public void OnStartGameBack()
	{
		m_startGamePanel.SetActive(value: false);
		ShowCharacterSelection();
	}

	public void OnCredits()
	{
		m_creditsPanel.SetActive(value: true);
		m_mainMenu.SetActive(value: false);
		Gogan.LogEvent("Screen", "Enter", "Credits", 0L);
		m_creditsList.anchoredPosition = new Vector2(0f, 0f);
	}

	public void OnCreditsBack()
	{
		m_mainMenu.SetActive(value: true);
		m_creditsPanel.SetActive(value: false);
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
	}

	public void OnSelelectCharacterBack()
	{
		m_characterSelectScreen.SetActive(value: false);
		m_mainMenu.SetActive(value: true);
		m_queuedJoinServer = null;
		Gogan.LogEvent("Screen", "Enter", "StartMenu", 0L);
	}

	public void OnAbort()
	{
		Application.Quit();
	}

	public void OnWorldVersionYes()
	{
		m_worldVersionPanel.SetActive(value: false);
	}

	public void OnPlayerVersionOk()
	{
		m_playerVersionPanel.SetActive(value: false);
	}

	private void FixedUpdate()
	{
		ZInput.FixedUpdate(Time.fixedDeltaTime);
	}

	private void UpdateCursor()
	{
		Cursor.lockState = ((!ZInput.IsMouseActive()) ? CursorLockMode.Locked : CursorLockMode.None);
		Cursor.visible = ZInput.IsMouseActive();
	}

	private void OnLanguageChange()
	{
		UpdateCharacterList();
	}

	private void Update()
	{
		int num = ((Settings.FPSLimit != 29) ? Mathf.Min(Settings.FPSLimit, 60) : 60);
		Application.targetFrameRate = ((Settings.ReduceBackgroundUsage && !Application.isFocused) ? Mathf.Min(30, num) : num);
		if (Terminal.m_showTests)
		{
			Terminal.m_testList["fps limit"] = Application.targetFrameRate.ToString();
		}
		ZInput.Update(Time.deltaTime);
		Localization.instance.ReLocalizeVisible(base.transform);
		UpdateGamepad();
		UpdateKeyboard();
		CheckPendingSteamJoinRequest();
		if (MasterClient.instance != null)
		{
			MasterClient.instance.Update(Time.deltaTime);
		}
		if (ZBroastcast.instance != null)
		{
			ZBroastcast.instance.Update(Time.deltaTime);
		}
		UpdateCharacterRotation(Time.deltaTime);
		UpdateCamera(Time.deltaTime);
		if (m_newCharacterPanel.activeInHierarchy)
		{
			m_csNewCharacterDone.interactable = m_csNewCharacterName.text.Length >= 3;
			Navigation navigation = m_csNewCharacterName.navigation;
			navigation.selectOnDown = (m_csNewCharacterDone.interactable ? m_csNewCharacterDone : m_csNewCharacterCancel);
			m_csNewCharacterName.navigation = navigation;
		}
		if (m_newCharacterPanel.activeInHierarchy)
		{
			m_csNewCharacterDone.interactable = m_csNewCharacterName.text.Length >= 3;
		}
		if (m_serverOptionsButton.gameObject.activeInHierarchy)
		{
			m_serverOptionsButton.interactable = m_world != null;
		}
		if (m_createWorldPanel.activeInHierarchy)
		{
			m_newWorldDone.interactable = m_newWorldName.text.Length >= 5;
		}
		if (m_startGamePanel.activeInHierarchy)
		{
			m_worldStart.interactable = CanStartServer();
			m_worldRemove.interactable = m_world != null;
			UpdatePasswordError();
		}
		if (m_startGamePanel.activeInHierarchy)
		{
			bool flag = m_openServerToggle.isOn && m_openServerToggle.interactable;
			SetToggleState(m_publicServerToggle, flag);
			SetToggleState(m_crossplayServerToggle, flag);
			m_serverPassword.interactable = flag;
		}
		if (m_creditsPanel.activeInHierarchy)
		{
			RectTransform obj = m_creditsList.parent as RectTransform;
			Vector3[] array = new Vector3[4];
			m_creditsList.GetWorldCorners(array);
			Vector3[] array2 = new Vector3[4];
			obj.GetWorldCorners(array2);
			float num2 = array2[1].y - array2[0].y;
			if ((double)array[3].y < (double)num2 * 0.5)
			{
				Vector3 position = m_creditsList.position;
				position.y += Time.deltaTime * m_creditsSpeed * num2;
				m_creditsList.position = position;
			}
		}
	}

	private void OnGUI()
	{
		ZInput.OnGUI();
	}

	private void SetToggleState(Toggle toggle, bool active)
	{
		toggle.interactable = active;
		Color toggleColor = m_toggleColor;
		TMP_Text componentInChildren = toggle.GetComponentInChildren<TMP_Text>();
		if (!active)
		{
			float num = 0.5f;
			float num2 = toggleColor.linear.r * 0.2126f + toggleColor.linear.g * 0.7152f + toggleColor.linear.b * 0.0722f;
			num2 *= num;
			toggleColor.r = (toggleColor.g = (toggleColor.b = Mathf.LinearToGammaSpace(num2)));
		}
		componentInChildren.color = toggleColor;
	}

	private void LateUpdate()
	{
		if (ZInput.GetKeyDown(KeyCode.F11))
		{
			GameCamera.ScreenShot();
		}
	}

	private void UpdateKeyboard()
	{
		if (ZInput.GetKeyDown(KeyCode.Return) && m_menuList.activeInHierarchy && !m_passwordError.gameObject.activeInHierarchy)
		{
			if (m_menuSelectedButton != null)
			{
				m_menuSelectedButton.OnSubmit(null);
			}
			else
			{
				OnStartGame();
			}
		}
		if (m_worldListPanel.GetComponent<UIGamePad>().IsBlocked())
		{
			return;
		}
		if (ZInput.GetKeyDown(KeyCode.UpArrow))
		{
			if (m_worldListPanel.activeInHierarchy)
			{
				SetSelectedWorld(GetSelectedWorld() - 1, centerSelection: true);
			}
			if (m_menuList.activeInHierarchy)
			{
				if (m_menuSelectedButton == null)
				{
					m_menuSelectedButton = m_menuButtons[0];
					m_menuSelectedButton.Select();
				}
				else
				{
					for (int i = 1; i < m_menuButtons.Length; i++)
					{
						if (m_menuButtons[i] == m_menuSelectedButton)
						{
							m_menuSelectedButton = m_menuButtons[i - 1];
							m_menuSelectedButton.Select();
							break;
						}
					}
				}
			}
		}
		if (!ZInput.GetKeyDown(KeyCode.DownArrow))
		{
			return;
		}
		if (m_worldListPanel.activeInHierarchy)
		{
			SetSelectedWorld(GetSelectedWorld() + 1, centerSelection: true);
		}
		if (!m_menuList.activeInHierarchy)
		{
			return;
		}
		if (m_menuSelectedButton == null)
		{
			m_menuSelectedButton = m_menuButtons[0];
			m_menuSelectedButton.Select();
			return;
		}
		for (int j = 0; j < m_menuButtons.Length - 1; j++)
		{
			if (m_menuButtons[j] == m_menuSelectedButton)
			{
				m_menuSelectedButton = m_menuButtons[j + 1];
				m_menuSelectedButton.Select();
				break;
			}
		}
	}

	private void UpdateGamepad()
	{
		if (ZInput.IsGamepadActive() && m_menuList.activeInHierarchy && EventSystem.current.currentSelectedGameObject == null && m_menuButtons != null && m_menuButtons.Length != 0)
		{
			StartCoroutine(SelectFirstMenuEntry(m_menuButtons[0]));
		}
		if (!ZInput.IsGamepadActive() || m_worldListPanel.GetComponent<UIGamePad>().IsBlocked())
		{
			return;
		}
		if (m_worldListPanel.activeInHierarchy)
		{
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				SetSelectedWorld(GetSelectedWorld() + 1, centerSelection: true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				SetSelectedWorld(GetSelectedWorld() - 1, centerSelection: true);
			}
			if (EventSystem.current.currentSelectedGameObject == null)
			{
				RefreshWorldSelection();
			}
		}
		if (m_characterSelectScreen.activeInHierarchy && !m_newCharacterPanel.activeInHierarchy && m_csLeftButton.interactable && ZInput.GetButtonDown("JoyDPadLeft"))
		{
			OnCharacterLeft();
		}
		if (m_characterSelectScreen.activeInHierarchy && !m_newCharacterPanel.activeInHierarchy && m_csRightButton.interactable && ZInput.GetButtonDown("JoyDPadRight"))
		{
			OnCharacterRight();
		}
		if (m_patchLogScroll.gameObject.activeInHierarchy)
		{
			m_patchLogScroll.value -= ZInput.GetJoyRightStickY() * 0.02f;
		}
	}

	private IEnumerator SelectFirstMenuEntry(Button button)
	{
		if (m_menuList.activeInHierarchy)
		{
			if (Event.current != null)
			{
				Event.current.Use();
			}
			yield return null;
			yield return null;
			if (UnifiedPopup.IsVisible())
			{
				UnifiedPopup.SetFocus();
				yield break;
			}
			m_menuSelectedButton = button;
			m_menuSelectedButton.Select();
		}
	}

	private void CheckPendingSteamJoinRequest()
	{
		if (ZSteamMatchmaking.instance == null || !ZSteamMatchmaking.instance.GetJoinHost(out var joinData))
		{
			return;
		}
		if (PrivilegeManager.CanAccessOnlineMultiplayer)
		{
			m_queuedJoinServer = joinData;
			if (m_serverListPanel.activeInHierarchy)
			{
				m_joinServer = new ServerStatus(m_queuedJoinServer);
				m_queuedJoinServer = null;
				JoinServer();
			}
			else
			{
				HideAll();
				ShowCharacterSelection();
			}
		}
		else
		{
			ShowOnlineMultiplayerPrivilegeWarning();
		}
	}

	private void UpdateCharacterRotation(float dt)
	{
		if (!(m_playerInstance == null) && m_characterSelectScreen.activeInHierarchy)
		{
			if (ZInput.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				float x = ZInput.GetMouseDelta().x;
				m_playerInstance.transform.Rotate(0f, (0f - x) * m_characterRotateSpeed, 0f);
			}
			float joyRightStickX = ZInput.GetJoyRightStickX();
			if (joyRightStickX != 0f)
			{
				m_playerInstance.transform.Rotate(0f, (0f - joyRightStickX) * m_characterRotateSpeedGamepad * dt, 0f);
			}
		}
	}

	private void UpdatePasswordError()
	{
		string text = "";
		if (NeedPassword())
		{
			text = GetPublicPasswordError(m_serverPassword.text, m_world);
		}
		m_passwordError.text = text;
	}

	private bool NeedPassword()
	{
		return (m_publicServerToggle.isOn | m_crossplayServerToggle.isOn) & m_openServerToggle.isOn;
	}

	private string GetPublicPasswordError(string password, World world)
	{
		if (password.Length < m_minimumPasswordLength)
		{
			return Localization.instance.Localize("$menu_passwordshort");
		}
		if (world != null && (world.m_name.Contains(password) || world.m_seedName.Contains(password)))
		{
			return Localization.instance.Localize("$menu_passwordinvalid");
		}
		return "";
	}

	private bool IsPublicPasswordValid(string password, World world)
	{
		if (password.Length < m_minimumPasswordLength)
		{
			return false;
		}
		if (world.m_name.Contains(password))
		{
			return false;
		}
		if (world.m_seedName.Contains(password))
		{
			return false;
		}
		return true;
	}

	private bool CanStartServer()
	{
		if (m_world == null)
		{
			return false;
		}
		switch (m_world.m_dataError)
		{
		default:
			return false;
		case World.SaveDataError.None:
		case World.SaveDataError.LoadError:
		case World.SaveDataError.Corrupt:
		case World.SaveDataError.MissingMeta:
			if (NeedPassword() && !IsPublicPasswordValid(m_serverPassword.text, m_world))
			{
				return false;
			}
			return true;
		}
	}

	private void UpdateCamera(float dt)
	{
		Transform transform = m_cameraMarkerMain;
		if (m_characterSelectScreen.activeSelf)
		{
			transform = m_cameraMarkerCharacter;
		}
		else if (m_creditsPanel.activeSelf)
		{
			transform = m_cameraMarkerCredits;
		}
		else if (m_startGamePanel.activeSelf)
		{
			transform = m_cameraMarkerGame;
		}
		else if (m_manageSavesMenu.IsVisible())
		{
			transform = m_cameraMarkerSaves;
		}
		m_mainCamera.transform.position = Vector3.SmoothDamp(m_mainCamera.transform.position, transform.position, ref camSpeed, 1.5f, 1000f, dt);
		Vector3 forward = Vector3.SmoothDamp(m_mainCamera.transform.forward, transform.forward, ref camRotSpeed, 1.5f, 1000f, dt);
		forward.Normalize();
		m_mainCamera.transform.rotation = Quaternion.LookRotation(forward);
	}

	public void ShowCloudQuotaWarning()
	{
		UnifiedPopup.Push(new WarningPopup("$menu_cloudstoragefull", "$menu_cloudstoragefulloperationfailed", delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	public void ShowCloudQuotaWorldDialog()
	{
		UnifiedPopup.Push(new YesNoPopup("$menu_cloudstoragefull", "$menu_cloudstoragefullworldprompt", delegate
		{
			UnifiedPopup.Pop();
			OnNewWorldDone(forceLocal: true);
		}, delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	public void ShowCloudQuotaCharacterDialog()
	{
		UnifiedPopup.Push(new YesNoPopup("$menu_cloudstoragefull", "$menu_cloudstoragefullcharacterprompt", delegate
		{
			UnifiedPopup.Pop();
			OnNewCharacterDone(forceLocal: true);
		}, delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	public void OnManageSaves(int index)
	{
		HideAll();
		switch (index)
		{
		case 0:
			m_manageSavesMenu.Open(SaveDataType.World, (m_world != null) ? m_world.m_fileName : null, ShowStartGame, OnSavesModified);
			break;
		case 1:
			m_manageSavesMenu.Open(SaveDataType.Character, (m_profileIndex >= 0 && m_profileIndex < m_profiles.Count && m_profiles[m_profileIndex] != null) ? m_profiles[m_profileIndex].m_filename : null, ShowCharacterSelection, OnSavesModified);
			break;
		}
	}

	private void OnSavesModified(SaveDataType dataType)
	{
		switch (dataType)
		{
		case SaveDataType.World:
			SaveSystem.ClearWorldListCache(reload: true);
			RefreshWorldSelection();
			break;
		case SaveDataType.Character:
		{
			string selectedProfile = null;
			if (m_profileIndex < m_profiles.Count && m_profileIndex >= 0)
			{
				selectedProfile = m_profiles[m_profileIndex].GetFilename();
			}
			m_profiles = SaveSystem.GetAllPlayerProfiles();
			SetSelectedProfile(selectedProfile);
			m_manageSavesMenu.Open(dataType, ShowCharacterSelection, OnSavesModified);
			break;
		}
		}
	}

	private void UpdateCharacterList()
	{
		if (m_profiles == null)
		{
			m_profiles = SaveSystem.GetAllPlayerProfiles();
		}
		if (m_profileIndex >= m_profiles.Count)
		{
			m_profileIndex = m_profiles.Count - 1;
		}
		m_csRemoveButton.gameObject.SetActive(m_profiles.Count > 0);
		m_csStartButton.gameObject.SetActive(m_profiles.Count > 0);
		m_csNewButton.gameObject.SetActive(m_profiles.Count > 0);
		m_csNewBigButton.gameObject.SetActive(m_profiles.Count == 0);
		m_csLeftButton.interactable = m_profileIndex > 0;
		m_csRightButton.interactable = m_profileIndex < m_profiles.Count - 1;
		if (m_profileIndex >= 0 && m_profileIndex < m_profiles.Count)
		{
			PlayerProfile playerProfile = m_profiles[m_profileIndex];
			if (playerProfile.GetName().ToLower() == playerProfile.m_filename.ToLower())
			{
				m_csName.text = playerProfile.GetName();
			}
			else
			{
				m_csName.text = playerProfile.GetName() + " (" + playerProfile.m_filename + ")";
			}
			m_csName.gameObject.SetActive(value: true);
			m_csFileSource.gameObject.SetActive(value: true);
			m_csFileSource.text = Localization.instance.Localize(FileHelpers.GetSourceString(playerProfile.m_fileSource));
			m_csSourceInfo.text = Localization.instance.Localize(((playerProfile.m_fileSource == FileHelpers.FileSource.Legacy) ? "$menu_legacynotice \n\n" : "") + ((!FileHelpers.m_cloudEnabled) ? "$menu_cloudsavesdisabled" : ""));
			m_csFileSource.transform.Find("source_cloud")?.gameObject.SetActive(playerProfile.m_fileSource == FileHelpers.FileSource.Cloud);
			m_csFileSource.transform.Find("source_local")?.gameObject.SetActive(playerProfile.m_fileSource == FileHelpers.FileSource.Local);
			m_csFileSource.transform.Find("source_legacy")?.gameObject.SetActive(playerProfile.m_fileSource == FileHelpers.FileSource.Legacy);
			SetupCharacterPreview(playerProfile);
		}
		else
		{
			m_csName.gameObject.SetActive(value: false);
			m_csFileSource.gameObject.SetActive(value: false);
			ClearCharacterPreview();
		}
	}

	private void SetSelectedProfile(string filename)
	{
		if (m_profiles == null)
		{
			m_profiles = SaveSystem.GetAllPlayerProfiles();
		}
		m_profileIndex = 0;
		if (filename != null)
		{
			for (int i = 0; i < m_profiles.Count; i++)
			{
				if (m_profiles[i].GetFilename() == filename)
				{
					m_profileIndex = i;
					break;
				}
			}
		}
		UpdateCharacterList();
	}

	public void OnNewCharacterDone(bool forceLocal)
	{
		string text = m_csNewCharacterName.text;
		string text2 = text.ToLower();
		PlayerProfile playerProfile = new PlayerProfile(text2);
		if (forceLocal)
		{
			playerProfile.m_fileSource = FileHelpers.FileSource.Local;
		}
		if (playerProfile.m_fileSource == FileHelpers.FileSource.Cloud && FileHelpers.OperationExceedsCloudCapacity(1048576uL * 3uL))
		{
			ShowCloudQuotaCharacterDialog();
			ZLog.LogWarning("The character save operation may exceed the cloud save quota and has therefore been aborted! Prompt shown to user.");
			return;
		}
		if (PlayerProfile.HaveProfile(text2))
		{
			m_newCharacterError.SetActive(value: true);
			return;
		}
		Player component = m_playerInstance.GetComponent<Player>();
		component.GiveDefaultItems();
		playerProfile.SetName(text);
		playerProfile.SavePlayerData(component);
		playerProfile.Save();
		m_selectCharacterPanel.SetActive(value: true);
		m_newCharacterPanel.SetActive(value: false);
		m_profiles = null;
		SetSelectedProfile(text2);
		m_csNewCharacterName.text = "";
		Gogan.LogEvent("Menu", "NewCharacter", text, 0L);
	}

	public void OnNewCharacterCancel()
	{
		m_selectCharacterPanel.SetActive(value: true);
		m_newCharacterPanel.SetActive(value: false);
		UpdateCharacterList();
	}

	public void OnCharacterNew()
	{
		m_newCharacterPanel.SetActive(value: true);
		m_selectCharacterPanel.SetActive(value: false);
		m_newCharacterError.SetActive(value: false);
		SetupCharacterPreview(null);
		Gogan.LogEvent("Screen", "Enter", "CreateCharacter", 0L);
	}

	public void OnCharacterRemove()
	{
		if (m_profileIndex >= 0 && m_profileIndex < m_profiles.Count)
		{
			PlayerProfile playerProfile = m_profiles[m_profileIndex];
			m_removeCharacterName.text = playerProfile.GetName() + " (" + Localization.instance.Localize(FileHelpers.GetSourceString(playerProfile.m_fileSource)) + ")";
			m_tempRemoveCharacterName = playerProfile.GetFilename();
			m_tempRemoveCharacterSource = playerProfile.m_fileSource;
			m_tempRemoveCharacterIndex = m_profileIndex;
			m_removeCharacterDialog.SetActive(value: true);
		}
	}

	public void OnButtonRemoveCharacterYes()
	{
		ZLog.Log("Remove character");
		PlayerProfile.RemoveProfile(m_tempRemoveCharacterName, m_tempRemoveCharacterSource);
		m_profiles.RemoveAt(m_tempRemoveCharacterIndex);
		UpdateCharacterList();
		m_removeCharacterDialog.SetActive(value: false);
	}

	public void OnButtonRemoveCharacterNo()
	{
		m_removeCharacterDialog.SetActive(value: false);
	}

	public void OnCharacterLeft()
	{
		if (m_profileIndex > 0)
		{
			m_profileIndex--;
		}
		UpdateCharacterList();
	}

	public void OnCharacterRight()
	{
		if (m_profileIndex < m_profiles.Count - 1)
		{
			m_profileIndex++;
		}
		UpdateCharacterList();
	}

	public void OnCharacterStart()
	{
		ZLog.Log("OnCharacterStart");
		if (m_profileIndex < 0 || m_profileIndex >= m_profiles.Count)
		{
			return;
		}
		PlayerProfile playerProfile = m_profiles[m_profileIndex];
		PlayerPrefs.SetString("profile", playerProfile.GetFilename());
		Game.SetProfile(playerProfile.GetFilename(), playerProfile.m_fileSource);
		m_characterSelectScreen.SetActive(value: false);
		if (m_queuedJoinServer != null)
		{
			m_joinServer = new ServerStatus(m_queuedJoinServer);
			m_queuedJoinServer = null;
			JoinServer();
			return;
		}
		ShowStartGame();
		if (m_worlds.Count == 0)
		{
			OnWorldNew();
		}
	}

	private void TransitionToMainScene()
	{
		m_menuAnimator.SetTrigger("FadeOut");
		retries = 0;
		Invoke("LoadMainSceneIfBackendSelected", 1.5f);
	}

	private void LoadMainSceneIfBackendSelected()
	{
		if (m_startingWorld || ZNet.HasServerHost())
		{
			ZLog.Log("Loading main scene");
			LoadMainScene();
			return;
		}
		retries++;
		if (retries > 50)
		{
			ZLog.Log("Max retries reached, reloading startup scene with connection error");
			ZNet.SetExternalError(ZNet.ConnectionStatus.ErrorConnectFailed);
			m_menuAnimator.SetTrigger("FadeIn");
			ShowConnectError(ZNet.ConnectionStatus.ErrorConnectFailed);
		}
		else
		{
			Invoke("LoadMainSceneIfBackendSelected", 0.25f);
			ZLog.Log("Backend not retreived yet, checking again in 0.25 seconds...");
		}
	}

	private void LoadMainScene()
	{
		m_loading.SetActive(value: true);
		SceneManager.LoadScene(m_mainScene);
		m_startingWorld = false;
	}

	public void OnButtonSettings()
	{
		m_mainMenu.SetActive(value: false);
		m_settingsPopup = UnityEngine.Object.Instantiate(m_settingsPrefab, base.transform);
		m_settingsPopup.GetComponent<Settings>().SettingsPopupDestroyed += delegate
		{
			m_mainMenu?.SetActive(value: true);
		};
	}

	public void OnButtonFeedback()
	{
		UnityEngine.Object.Instantiate(m_feedbackPrefab, base.transform);
	}

	public void OnButtonTwitter()
	{
		Application.OpenURL("https://twitter.com/valheimgame");
	}

	public void OnButtonWebPage()
	{
		Application.OpenURL("http://valheimgame.com/");
	}

	public void OnButtonDiscord()
	{
		Application.OpenURL("https://discord.gg/44qXMJH");
	}

	public void OnButtonFacebook()
	{
		Application.OpenURL("https://www.facebook.com/valheimgame/");
	}

	public void OnButtonShowLog()
	{
		Application.OpenURL(Application.persistentDataPath + "/");
	}

	private bool AcceptedNDA()
	{
		return PlayerPrefs.GetInt("accepted_nda", 0) == 1;
	}

	public void OnButtonNDAAccept()
	{
		PlayerPrefs.SetInt("accepted_nda", 1);
		m_ndaPanel.SetActive(value: false);
		m_mainMenu.SetActive(value: true);
	}

	public void OnButtonNDADecline()
	{
		Application.Quit();
	}

	public void OnConnectionFailedOk()
	{
		m_connectionFailedPanel.SetActive(value: false);
	}

	public Player GetPreviewPlayer()
	{
		if (m_playerInstance != null)
		{
			return m_playerInstance.GetComponent<Player>();
		}
		return null;
	}

	private void ClearCharacterPreview()
	{
		if ((bool)m_playerInstance)
		{
			UnityEngine.Object.Instantiate(m_changeEffectPrefab, m_characterPreviewPoint.position, m_characterPreviewPoint.rotation);
			UnityEngine.Object.Destroy(m_playerInstance);
			m_playerInstance = null;
		}
	}

	private void SetupCharacterPreview(PlayerProfile profile)
	{
		ClearCharacterPreview();
		ZNetView.m_forceDisableInit = true;
		GameObject gameObject = UnityEngine.Object.Instantiate(m_playerPrefab, m_characterPreviewPoint.position, m_characterPreviewPoint.rotation);
		ZNetView.m_forceDisableInit = false;
		UnityEngine.Object.Destroy(gameObject.GetComponent<Rigidbody>());
		Animator[] componentsInChildren = gameObject.GetComponentsInChildren<Animator>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].updateMode = AnimatorUpdateMode.Normal;
		}
		Player component = gameObject.GetComponent<Player>();
		if (profile != null)
		{
			try
			{
				profile.LoadPlayerData(component);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Error loading player data: " + profile.GetPath() + ", error: " + ex.Message);
			}
		}
		m_playerInstance = gameObject;
	}

	public void SetServerToJoin(ServerStatus serverData)
	{
		m_joinServer = serverData;
	}

	public bool HasServerToJoin()
	{
		return m_joinServer != null;
	}

	public ServerJoinData GetServerToJoin()
	{
		if (m_joinServer == null)
		{
			return null;
		}
		return m_joinServer.m_joinData;
	}
}
