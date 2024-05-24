using System;
using System.Collections.Generic;
using System.IO;
using GUIFramework;
using PlatformTools.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerList : MonoBehaviour
{
	private class ServerListElement
	{
		public GameObject m_element;

		public ServerStatus m_serverStatus;

		public Button m_button;

		public RectTransform m_rectTransform;

		public TMP_Text m_serverName;

		public TMP_Text m_modifiers;

		public UITooltip m_tooltip;

		public TMP_Text m_version;

		public TMP_Text m_players;

		public Image m_status;

		public Transform m_crossplay;

		public Transform m_private;

		public RectTransform m_selected;

		public ServerListElement(GameObject element, ServerStatus serverStatus)
		{
			m_element = element;
			m_serverStatus = serverStatus;
			m_button = m_element.GetComponent<Button>();
			m_rectTransform = m_element.transform as RectTransform;
			m_serverName = m_element.GetComponentInChildren<TMP_Text>();
			m_modifiers = m_element.transform.Find("modifiers").GetComponent<TMP_Text>();
			m_tooltip = m_element.GetComponentInChildren<UITooltip>();
			m_version = m_element.transform.Find("version").GetComponent<TMP_Text>();
			m_players = m_element.transform.Find("players").GetComponent<TMP_Text>();
			m_status = m_element.transform.Find("status").GetComponent<Image>();
			m_crossplay = m_element.transform.Find("crossplay");
			m_private = m_element.transform.Find("Private");
			m_selected = m_element.transform.Find("selected") as RectTransform;
		}
	}

	private struct StorageLocation
	{
		public string path;

		public FileHelpers.FileSource source;

		public StorageLocation(string path, FileHelpers.FileSource source)
		{
			this.path = path;
			this.source = source;
		}
	}

	public enum SaveStatusCode
	{
		Succeess,
		UnsupportedServerListType,
		UnknownServerBackend,
		CloudQuotaExceeded,
		FailedUnknownReason
	}

	private static ServerList instance = null;

	private ServerListType currentServerList;

	[SerializeField]
	private Button m_favoriteButton;

	[SerializeField]
	private Button m_removeButton;

	[SerializeField]
	private Button m_upButton;

	[SerializeField]
	private Button m_downButton;

	[SerializeField]
	private FejdStartup m_startup;

	[SerializeField]
	private Sprite connectUnknown;

	[SerializeField]
	private Sprite connectTrying;

	[SerializeField]
	private Sprite connectSuccess;

	[SerializeField]
	private Sprite connectFailed;

	[Header("Join")]
	public float m_serverListElementStep = 32f;

	public RectTransform m_serverListRoot;

	public GameObject m_serverListElementSteamCrossplay;

	public GameObject m_serverListElement;

	public ScrollRectEnsureVisible m_serverListEnsureVisible;

	public Button m_serverRefreshButton;

	public TextMeshProUGUI m_serverCount;

	public int m_serverPlayerLimit = 10;

	public GuiInputField m_filterInputField;

	public RectTransform m_tooltipAnchor;

	public Button m_addServerButton;

	public GameObject m_addServerPanel;

	public Button m_addServerConfirmButton;

	public Button m_addServerCancelButton;

	public GuiInputField m_addServerTextInput;

	public TabHandler m_serverListTabHandler;

	private bool isAwaitingServerAdd;

	public Button m_joinGameButton;

	private float m_serverListBaseSize;

	private int m_serverListRevision = -1;

	private float m_lastServerListRequesTime = -999f;

	private bool m_doneInitialServerListRequest;

	private bool buttonsOutdated = true;

	private bool initialized;

	private static int maxRecentServers = 11;

	private List<ServerStatus> m_steamMatchmakingServerList = new List<ServerStatus>();

	private readonly List<ServerStatus> m_crossplayMatchmakingServerList = new List<ServerStatus>();

	private bool m_localServerListsLoaded;

	private Dictionary<ServerJoinData, ServerStatus> m_allLoadedServerData = new Dictionary<ServerJoinData, ServerStatus>();

	private List<ServerStatus> m_recentServerList = new List<ServerStatus>();

	private List<ServerStatus> m_favoriteServerList = new List<ServerStatus>();

	private bool filteredListOutdated;

	private List<ServerStatus> m_filteredList = new List<ServerStatus>();

	private List<ServerListElement> m_serverListElements = new List<ServerListElement>();

	private double serverListLastUpdatedTime;

	private bool m_playFabServerSearchOngoing;

	private bool m_playFabServerSearchQueued;

	private readonly Dictionary<PlayFabMatchmakingServerData, PlayFabMatchmakingServerData> m_playFabTemporarySearchServerList = new Dictionary<PlayFabMatchmakingServerData, PlayFabMatchmakingServerData>();

	private float m_whenToSearchPlayFab = -1f;

	private const uint serverListVersion = 1u;

	public static Action<Privilege> ResolvePrivilege;

	public bool currentServerListIsLocal
	{
		get
		{
			if (currentServerList != ServerListType.recent)
			{
				return currentServerList == ServerListType.favorite;
			}
			return true;
		}
	}

	private List<ServerStatus> CurrentServerListFiltered
	{
		get
		{
			if (filteredListOutdated)
			{
				FilterList();
				filteredListOutdated = false;
			}
			return m_filteredList;
		}
	}

	private static string GetServerListFolder(FileHelpers.FileSource fileSource)
	{
		if (fileSource != FileHelpers.FileSource.Local)
		{
			return "/serverlist/";
		}
		return "/serverlist_local/";
	}

	private static string GetServerListFolderPath(FileHelpers.FileSource fileSource)
	{
		return Utils.GetSaveDataPath(fileSource) + GetServerListFolder(fileSource);
	}

	private static string GetFavoriteListFile(FileHelpers.FileSource fileSource)
	{
		return GetServerListFolderPath(fileSource) + "favorite";
	}

	private static string GetRecentListFile(FileHelpers.FileSource fileSource)
	{
		return GetServerListFolderPath(fileSource) + "recent";
	}

	private void Awake()
	{
		InitializeIfNot();
	}

	private void OnEnable()
	{
		if (instance != null && instance != this)
		{
			ZLog.LogError("More than one instance of ServerList!");
			return;
		}
		instance = this;
		OnServerListTab();
	}

	private void OnDestroy()
	{
		if (instance != this)
		{
			ZLog.LogError("ServerList instance was not this!");
			return;
		}
		instance = null;
		FlushLocalServerLists();
	}

	private void Update()
	{
		if (m_addServerPanel.activeInHierarchy)
		{
			m_addServerConfirmButton.interactable = m_addServerTextInput.text.Length > 0 && !isAwaitingServerAdd;
			m_addServerCancelButton.interactable = !isAwaitingServerAdd;
		}
		switch (currentServerList)
		{
		case ServerListType.friends:
		case ServerListType.community:
			if (Time.timeAsDouble >= serverListLastUpdatedTime + 0.5)
			{
				UpdateMatchmakingServerList();
				UpdateServerCount();
			}
			break;
		case ServerListType.favorite:
		case ServerListType.recent:
			if (Time.timeAsDouble >= serverListLastUpdatedTime + 0.5)
			{
				UpdateLocalServerListStatus();
				UpdateServerCount();
			}
			break;
		}
		if (!GetComponent<UIGamePad>().IsBlocked())
		{
			UpdateGamepad();
		}
		m_serverRefreshButton.interactable = Time.time - m_lastServerListRequesTime > 1f;
		if (buttonsOutdated)
		{
			buttonsOutdated = false;
			UpdateButtons();
		}
	}

	private void InitializeIfNot()
	{
		if (!initialized)
		{
			initialized = true;
			m_favoriteButton.onClick.AddListener(delegate
			{
				OnFavoriteServerButton();
			});
			m_removeButton.onClick.AddListener(delegate
			{
				OnRemoveServerButton();
			});
			m_upButton.onClick.AddListener(delegate
			{
				OnMoveServerUpButton();
			});
			m_downButton.onClick.AddListener(delegate
			{
				OnMoveServerDownButton();
			});
			m_filterInputField.onValueChanged.AddListener(delegate
			{
				OnServerFilterChanged(isTyping: true);
			});
			m_addServerButton.gameObject.SetActive(value: true);
			if (PlayerPrefs.HasKey("LastIPJoined"))
			{
				PlayerPrefs.DeleteKey("LastIPJoined");
			}
			m_serverListBaseSize = m_serverListRoot.rect.height;
			OnServerListTab();
		}
	}

	public static uint[] FairSplit(uint[] entryCounts, uint maxEntries)
	{
		uint num = 0u;
		uint num2 = 0u;
		for (int i = 0; i < entryCounts.Length; i++)
		{
			num += entryCounts[i];
			if (entryCounts[i] != 0)
			{
				num2++;
			}
		}
		if (num <= maxEntries)
		{
			return entryCounts;
		}
		uint[] array = new uint[entryCounts.Length];
		while (num2 != 0)
		{
			uint num3 = maxEntries / num2;
			if (num3 != 0)
			{
				for (int j = 0; j < entryCounts.Length; j++)
				{
					if (entryCounts[j] != 0)
					{
						if (entryCounts[j] > num3)
						{
							array[j] += num3;
							maxEntries -= num3;
							entryCounts[j] -= num3;
						}
						else
						{
							array[j] += entryCounts[j];
							maxEntries -= entryCounts[j];
							entryCounts[j] = 0u;
							num2--;
						}
					}
				}
				continue;
			}
			uint num4 = 0u;
			for (int k = 0; k < maxEntries; k++)
			{
				if (entryCounts[num4] != 0)
				{
					array[num4]++;
				}
				else
				{
					k--;
				}
				num4++;
			}
			maxEntries = 0u;
			break;
		}
		return array;
	}

	public void FilterList()
	{
		if (currentServerListIsLocal)
		{
			List<ServerStatus> list;
			if (currentServerList == ServerListType.favorite)
			{
				list = m_favoriteServerList;
			}
			else
			{
				if (currentServerList != ServerListType.recent)
				{
					ZLog.LogError("Can't filter invalid server list!");
					return;
				}
				list = m_recentServerList;
			}
			m_filteredList = new List<ServerStatus>();
			for (int i = 0; i < list.Count; i++)
			{
				if (m_filterInputField.text.Length <= 0 || list[i].m_joinData.m_serverName.ToLowerInvariant().Contains(m_filterInputField.text.ToLowerInvariant()))
				{
					m_filteredList.Add(list[i]);
				}
			}
			return;
		}
		List<ServerStatus> list2 = new List<ServerStatus>();
		if (currentServerList == ServerListType.community)
		{
			for (int j = 0; j < m_crossplayMatchmakingServerList.Count; j++)
			{
				if (m_filterInputField.text.Length <= 0 || m_crossplayMatchmakingServerList[j].m_joinData.m_serverName.ToLowerInvariant().Contains(m_filterInputField.text.ToLowerInvariant()))
				{
					list2.Add(m_crossplayMatchmakingServerList[j]);
				}
			}
		}
		uint[] array = FairSplit(new uint[2]
		{
			(uint)list2.Count,
			(uint)m_steamMatchmakingServerList.Count
		}, 200u);
		m_filteredList = new List<ServerStatus>();
		if (array[0] != 0)
		{
			m_filteredList.AddRange(list2.GetRange(0, (int)array[0]));
		}
		if (array[1] != 0)
		{
			for (int k = 0; k < m_steamMatchmakingServerList.Count; k++)
			{
				if ((long)m_filteredList.Count >= 200L)
				{
					break;
				}
				if (m_steamMatchmakingServerList[k].IsCrossplay)
				{
					bool flag = false;
					for (int l = 0; l < m_filteredList.Count; l++)
					{
						if (m_steamMatchmakingServerList[k].m_joinData == m_filteredList[l].m_joinData)
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						m_filteredList.Add(m_steamMatchmakingServerList[k]);
					}
				}
				else
				{
					m_filteredList.Add(m_steamMatchmakingServerList[k]);
				}
			}
		}
		m_filteredList.Sort((ServerStatus a, ServerStatus b) => a.m_joinData.m_serverName.CompareTo(b.m_joinData.m_serverName));
	}

	private void UpdateButtons()
	{
		int selectedServer = GetSelectedServer();
		bool flag = selectedServer >= 0;
		bool flag2 = false;
		if (flag)
		{
			for (int i = 0; i < m_favoriteServerList.Count; i++)
			{
				if (m_favoriteServerList[i].m_joinData == CurrentServerListFiltered[selectedServer].m_joinData)
				{
					flag2 = true;
					break;
				}
			}
		}
		switch (currentServerList)
		{
		case ServerListType.friends:
		case ServerListType.community:
			m_favoriteButton.interactable = flag && !flag2;
			break;
		case ServerListType.recent:
			m_favoriteButton.interactable = flag && !flag2;
			m_removeButton.interactable = flag;
			break;
		case ServerListType.favorite:
			m_upButton.interactable = flag && selectedServer != 0;
			m_downButton.interactable = flag && selectedServer != CurrentServerListFiltered.Count - 1;
			m_removeButton.interactable = flag;
			m_favoriteButton.interactable = flag && (m_removeButton == null || !m_removeButton.gameObject.activeSelf);
			break;
		}
		m_joinGameButton.interactable = flag;
	}

	public void OnFavoriteServersTab()
	{
		InitializeIfNot();
		if (currentServerList != ServerListType.favorite)
		{
			currentServerList = ServerListType.favorite;
			m_filterInputField.text = "";
			OnServerFilterChanged();
			if (m_doneInitialServerListRequest)
			{
				PlayerPrefs.SetInt("serverListTab", m_serverListTabHandler.GetActiveTab());
			}
			ResetListManipulationButtons();
			m_removeButton.gameObject.SetActive(value: true);
			UpdateLocalServerListStatus();
			UpdateLocalServerListSelection();
		}
	}

	public void OnRecentServersTab()
	{
		InitializeIfNot();
		if (currentServerList != ServerListType.recent)
		{
			currentServerList = ServerListType.recent;
			m_filterInputField.text = "";
			OnServerFilterChanged();
			if (m_doneInitialServerListRequest)
			{
				PlayerPrefs.SetInt("serverListTab", m_serverListTabHandler.GetActiveTab());
			}
			ResetListManipulationButtons();
			m_favoriteButton.gameObject.SetActive(value: true);
			UpdateLocalServerListStatus();
			UpdateLocalServerListSelection();
		}
	}

	public void OnFriendsServersTab()
	{
		InitializeIfNot();
		if (currentServerList != ServerListType.friends)
		{
			currentServerList = ServerListType.friends;
			if (m_doneInitialServerListRequest)
			{
				PlayerPrefs.SetInt("serverListTab", m_serverListTabHandler.GetActiveTab());
			}
			ResetListManipulationButtons();
			m_favoriteButton.gameObject.SetActive(value: true);
			m_filterInputField.text = "";
			OnServerFilterChanged();
			UpdateMatchmakingServerList();
			UpdateServerListGui(centerSelection: true);
			UpdateServerCount();
		}
	}

	public void OnCommunityServersTab()
	{
		InitializeIfNot();
		if (currentServerList != ServerListType.community)
		{
			currentServerList = ServerListType.community;
			if (m_doneInitialServerListRequest)
			{
				PlayerPrefs.SetInt("serverListTab", m_serverListTabHandler.GetActiveTab());
			}
			ResetListManipulationButtons();
			m_favoriteButton.gameObject.SetActive(value: true);
			m_filterInputField.text = "";
			OnServerFilterChanged();
			UpdateMatchmakingServerList();
			UpdateServerListGui(centerSelection: true);
			UpdateServerCount();
		}
	}

	public void OnFavoriteServerButton()
	{
		if ((m_removeButton == null || !m_removeButton.gameObject.activeSelf) && currentServerList == ServerListType.favorite)
		{
			OnRemoveServerButton();
			return;
		}
		int selectedServer = GetSelectedServer();
		ServerStatus serverStatus = CurrentServerListFiltered[selectedServer];
		if (m_allLoadedServerData.TryGetValue(serverStatus.m_joinData, out var value))
		{
			m_favoriteServerList.Add(value);
		}
		else
		{
			m_favoriteServerList.Add(serverStatus);
			m_allLoadedServerData.Add(serverStatus.m_joinData, serverStatus);
		}
		SetButtonsOutdated();
	}

	public void OnRemoveServerButton()
	{
		int selectedServer = GetSelectedServer();
		UnifiedPopup.Push(new YesNoPopup("$menu_removeserver", CensorShittyWords.FilterUGC(CurrentServerListFiltered[selectedServer].m_joinData.m_serverName, UGCType.ServerName, CurrentServerListFiltered[selectedServer].m_hostId, 0L), delegate
		{
			OnRemoveServerConfirm();
		}, delegate
		{
			UnifiedPopup.Pop();
		}));
	}

	public void OnMoveServerUpButton()
	{
		List<ServerStatus> favoriteServerList = m_favoriteServerList;
		int selectedServer = GetSelectedServer();
		ServerStatus value = favoriteServerList[selectedServer - 1];
		favoriteServerList[selectedServer - 1] = favoriteServerList[selectedServer];
		favoriteServerList[selectedServer] = value;
		filteredListOutdated = true;
		UpdateServerListGui(centerSelection: true);
	}

	public void OnMoveServerDownButton()
	{
		List<ServerStatus> favoriteServerList = m_favoriteServerList;
		int selectedServer = GetSelectedServer();
		ServerStatus value = favoriteServerList[selectedServer + 1];
		favoriteServerList[selectedServer + 1] = favoriteServerList[selectedServer];
		favoriteServerList[selectedServer] = value;
		filteredListOutdated = true;
		UpdateServerListGui(centerSelection: true);
	}

	private void OnRemoveServerConfirm()
	{
		if (currentServerList == ServerListType.favorite)
		{
			List<ServerStatus> favoriteServerList = m_favoriteServerList;
			int selectedServer = GetSelectedServer();
			ServerStatus item = CurrentServerListFiltered[selectedServer];
			int index = favoriteServerList.IndexOf(item);
			favoriteServerList.RemoveAt(index);
			filteredListOutdated = true;
			if (CurrentServerListFiltered.Count <= 0 && m_filterInputField.text != "")
			{
				m_filterInputField.text = "";
				OnServerFilterChanged();
				m_startup.SetServerToJoin(null);
			}
			else
			{
				UpdateLocalServerListSelection();
				SetSelectedServer(selectedServer, centerSelection: true);
			}
			UnifiedPopup.Pop();
		}
		else
		{
			ZLog.LogError("Can't remove server from invalid list!");
		}
	}

	private void ResetListManipulationButtons()
	{
		m_favoriteButton.gameObject.SetActive(value: false);
		m_removeButton.gameObject.SetActive(value: false);
		m_favoriteButton.interactable = false;
		m_upButton.interactable = false;
		m_downButton.interactable = false;
		m_removeButton.interactable = false;
	}

	private void SetButtonsOutdated()
	{
		buttonsOutdated = true;
	}

	private void UpdateServerListGui(bool centerSelection)
	{
		new List<ServerStatus>();
		List<ServerListElement> list = new List<ServerListElement>();
		Dictionary<ServerJoinData, ServerListElement> dictionary = new Dictionary<ServerJoinData, ServerListElement>();
		for (int i = 0; i < m_serverListElements.Count; i++)
		{
			if (dictionary.TryGetValue(m_serverListElements[i].m_serverStatus.m_joinData, out var _))
			{
				ZLog.LogWarning("Join data " + m_serverListElements[i].m_serverStatus.m_joinData.ToString() + " already has a server list element, even though duplicates are not allowed! Discarding this element.\nWhile this warning itself is fine, it might be an indication of a bug that may cause navigation issues in the server list.");
				UnityEngine.Object.Destroy(m_serverListElements[i].m_element);
			}
			else
			{
				dictionary.Add(m_serverListElements[i].m_serverStatus.m_joinData, m_serverListElements[i]);
			}
		}
		float num = 0f;
		for (int j = 0; j < CurrentServerListFiltered.Count; j++)
		{
			ServerListElement serverListElement;
			if (dictionary.ContainsKey(CurrentServerListFiltered[j].m_joinData))
			{
				serverListElement = dictionary[CurrentServerListFiltered[j].m_joinData];
				list.Add(serverListElement);
				dictionary.Remove(CurrentServerListFiltered[j].m_joinData);
			}
			else
			{
				GameObject obj = UnityEngine.Object.Instantiate(m_serverListElementSteamCrossplay, m_serverListRoot);
				obj.SetActive(value: true);
				serverListElement = new ServerListElement(obj, CurrentServerListFiltered[j]);
				ServerStatus selectedStatus = CurrentServerListFiltered[j];
				serverListElement.m_button.onClick.AddListener(delegate
				{
					OnSelectedServer(selectedStatus);
				});
				list.Add(serverListElement);
			}
			serverListElement.m_rectTransform.anchoredPosition = new Vector2(0f, 0f - num);
			num += serverListElement.m_rectTransform.sizeDelta.y;
			ServerStatus serverStatus = CurrentServerListFiltered[j];
			serverListElement.m_serverName.text = CensorShittyWords.FilterUGC(serverStatus.m_joinData.m_serverName, UGCType.ServerName, serverStatus.m_hostId, 0L);
			bool flag = serverStatus.m_modifiers != null && serverStatus.m_modifiers.Count > 0;
			serverListElement.m_modifiers.text = (flag ? Localization.instance.Localize(ServerOptionsGUI.GetWorldModifierSummary(serverStatus.m_modifiers, alwaysShort: true)) : "");
			string text = (flag ? ServerOptionsGUI.GetWorldModifierSummary(serverStatus.m_modifiers, alwaysShort: false, "\n") : "");
			string text2 = "";
			if (serverStatus.m_joinData is ServerJoinDataSteamUser)
			{
				text2 = ((!flag) ? ("- \n\n" + serverStatus.m_joinData.ToString() + "\n(Steam)") : (text + "\n\n" + serverStatus.m_joinData.ToString() + "\n(Steam)"));
			}
			if (serverStatus.m_joinData is ServerJoinDataPlayFabUser)
			{
				text2 = ((!flag) ? "- \n\n(PlayFab)" : (text + "\n\n(PlayFab)"));
			}
			if (serverStatus.m_joinData is ServerJoinDataDedicated)
			{
				text2 = ((!flag) ? ("- \n\n" + serverStatus.m_joinData.ToString() + "\n(Dedicated)") : (text + "\n\n" + serverStatus.m_joinData.ToString() + "\n(Dedicated)"));
			}
			serverListElement.m_tooltip.Set("$menu_serveroptions", text2, m_tooltipAnchor);
			if (serverStatus.IsJoinable || serverStatus.PlatformRestriction == PrivilegeManager.Platform.Unknown)
			{
				serverListElement.m_version.text = serverStatus.m_gameVersion.ToString();
				if (serverStatus.OnlineStatus == OnlineStatus.Online)
				{
					serverListElement.m_players.text = serverStatus.m_playerCount + " / " + m_serverPlayerLimit;
				}
				else
				{
					serverListElement.m_players.text = "";
				}
				switch (serverStatus.PingStatus)
				{
				case ServerPingStatus.NotStarted:
					serverListElement.m_status.sprite = connectUnknown;
					break;
				case ServerPingStatus.AwaitingResponse:
					serverListElement.m_status.sprite = connectTrying;
					break;
				case ServerPingStatus.Success:
					serverListElement.m_status.sprite = connectSuccess;
					break;
				default:
					serverListElement.m_status.sprite = connectFailed;
					break;
				}
				if (serverListElement.m_crossplay != null)
				{
					if (serverStatus.IsCrossplay)
					{
						serverListElement.m_crossplay.gameObject.SetActive(value: true);
					}
					else
					{
						serverListElement.m_crossplay.gameObject.SetActive(value: false);
					}
				}
				serverListElement.m_private.gameObject.SetActive(serverStatus.m_isPasswordProtected);
			}
			else
			{
				serverListElement.m_version.text = "";
				serverListElement.m_players.text = "";
				serverListElement.m_status.sprite = connectFailed;
				if (serverListElement.m_crossplay != null)
				{
					serverListElement.m_crossplay.gameObject.SetActive(value: false);
				}
				serverListElement.m_private.gameObject.SetActive(value: false);
			}
			bool flag2 = m_startup.HasServerToJoin() && m_startup.GetServerToJoin().Equals(serverStatus.m_joinData);
			if (flag2)
			{
				m_startup.SetServerToJoin(serverStatus);
			}
			serverListElement.m_selected.gameObject.SetActive(flag2);
			if (flag2 && !m_filterInputField.isFocused)
			{
				serverListElement.m_button.Select();
			}
			if (centerSelection && flag2)
			{
				m_serverListEnsureVisible.CenterOnItem(serverListElement.m_selected);
			}
		}
		foreach (KeyValuePair<ServerJoinData, ServerListElement> item in dictionary)
		{
			UnityEngine.Object.Destroy(item.Value.m_element);
		}
		m_serverListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(num, m_serverListBaseSize));
		m_serverListElements = list;
		SetButtonsOutdated();
	}

	private void UpdateServerCount()
	{
		int num = 0;
		if (currentServerListIsLocal)
		{
			num += CurrentServerListFiltered.Count;
		}
		else
		{
			num += ZSteamMatchmaking.instance.GetTotalNrOfServers();
			num += m_crossplayMatchmakingServerList.Count;
		}
		int num2 = 0;
		for (int i = 0; i < CurrentServerListFiltered.Count; i++)
		{
			if (CurrentServerListFiltered[i].PingStatus != 0 && CurrentServerListFiltered[i].PingStatus != ServerPingStatus.AwaitingResponse)
			{
				num2++;
			}
		}
		m_serverCount.text = num2 + " / " + num;
	}

	private void OnSelectedServer(ServerStatus selected)
	{
		m_startup.SetServerToJoin(selected);
		UpdateServerListGui(centerSelection: false);
	}

	private void SetSelectedServer(int index, bool centerSelection)
	{
		if (CurrentServerListFiltered.Count == 0)
		{
			if (m_startup.HasServerToJoin())
			{
				ZLog.Log("Serverlist is empty, clearing selection");
			}
			ClearSelectedServer();
		}
		else
		{
			index = Mathf.Clamp(index, 0, CurrentServerListFiltered.Count - 1);
			m_startup.SetServerToJoin(CurrentServerListFiltered[index]);
			UpdateServerListGui(centerSelection);
		}
	}

	private int GetSelectedServer()
	{
		if (!m_startup.HasServerToJoin())
		{
			return -1;
		}
		for (int i = 0; i < CurrentServerListFiltered.Count; i++)
		{
			if (m_startup.GetServerToJoin() == CurrentServerListFiltered[i].m_joinData)
			{
				return i;
			}
		}
		return -1;
	}

	private void ClearSelectedServer()
	{
		m_startup.SetServerToJoin(null);
		SetButtonsOutdated();
	}

	private int FindSelectedServer(GameObject button)
	{
		for (int i = 0; i < m_serverListElements.Count; i++)
		{
			if (m_serverListElements[i].m_element == button)
			{
				return i;
			}
		}
		return -1;
	}

	private void UpdateLocalServerListStatus()
	{
		serverListLastUpdatedTime = Time.timeAsDouble;
		List<ServerStatus> list;
		if (currentServerList == ServerListType.favorite)
		{
			list = m_favoriteServerList;
		}
		else
		{
			if (currentServerList != ServerListType.recent)
			{
				ZLog.LogError("Can't update status of invalid server list!");
				return;
			}
			list = m_recentServerList;
		}
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].PingStatus != ServerPingStatus.Success && list[i].PingStatus != ServerPingStatus.CouldNotReach)
			{
				if (list[i].PingStatus == ServerPingStatus.NotStarted)
				{
					list[i].Ping();
					flag = true;
				}
				if (list[i].PingStatus == ServerPingStatus.AwaitingResponse && list[i].TryGetResult())
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			UpdateServerListGui(centerSelection: false);
			UpdateServerCount();
		}
	}

	private void UpdateMatchmakingServerList()
	{
		serverListLastUpdatedTime = Time.timeAsDouble;
		if (m_serverListRevision == ZSteamMatchmaking.instance.GetServerListRevision())
		{
			return;
		}
		m_serverListRevision = ZSteamMatchmaking.instance.GetServerListRevision();
		m_steamMatchmakingServerList.Clear();
		ZSteamMatchmaking.instance.GetServers(m_steamMatchmakingServerList);
		if (!currentServerListIsLocal && m_whenToSearchPlayFab >= 0f && m_whenToSearchPlayFab <= Time.time)
		{
			m_whenToSearchPlayFab = -1f;
			RequestPlayFabServerList();
		}
		bool flag = false;
		filteredListOutdated = true;
		for (int i = 0; i < CurrentServerListFiltered.Count; i++)
		{
			if (CurrentServerListFiltered[i].m_joinData == m_startup.GetServerToJoin())
			{
				flag = true;
				break;
			}
		}
		if (m_startup.HasServerToJoin() && !flag)
		{
			ZLog.Log("Serverlist does not contain selected server, clearing");
			if (CurrentServerListFiltered.Count > 0)
			{
				SetSelectedServer(0, centerSelection: true);
			}
			else
			{
				ClearSelectedServer();
			}
		}
		UpdateServerListGui(centerSelection: false);
		UpdateServerCount();
	}

	private void UpdateLocalServerListSelection()
	{
		if (GetSelectedServer() < 0)
		{
			ClearSelectedServer();
			UpdateServerListGui(centerSelection: true);
		}
	}

	public void OnServerListTab()
	{
		if (PlayerPrefs.HasKey("publicfilter"))
		{
			PlayerPrefs.DeleteKey("publicfilter");
		}
		int @int = PlayerPrefs.GetInt("serverListTab", 0);
		m_serverListTabHandler.SetActiveTab(@int);
		if (!m_doneInitialServerListRequest)
		{
			m_doneInitialServerListRequest = true;
			RequestServerList();
		}
		UpdateServerListGui(centerSelection: true);
	}

	public void OnRefreshButton()
	{
		RequestServerList();
		UpdateServerListGui(centerSelection: true);
		UpdateServerCount();
	}

	public static void Refresh()
	{
		if (!(instance == null))
		{
			instance.OnRefreshButton();
		}
	}

	public static void UpdateServerListGuiStatic()
	{
		if (!(instance == null))
		{
			instance.UpdateServerListGui(centerSelection: false);
		}
	}

	private void RequestPlayFabServerListIfUnchangedIn(float time)
	{
		if (time < 0f)
		{
			m_whenToSearchPlayFab = -1f;
			RequestPlayFabServerList();
		}
		else
		{
			m_whenToSearchPlayFab = Time.time + time;
		}
	}

	private void RequestPlayFabServerList()
	{
		if (!PlayFabManager.IsLoggedIn)
		{
			m_playFabServerSearchQueued = true;
			if (PlayFabManager.instance != null)
			{
				PlayFabManager.instance.LoginFinished += delegate
				{
					RequestPlayFabServerList();
				};
			}
		}
		else if (m_playFabServerSearchOngoing)
		{
			m_playFabServerSearchQueued = true;
		}
		else
		{
			m_playFabServerSearchQueued = false;
			m_playFabServerSearchOngoing = true;
			m_crossplayMatchmakingServerList.Clear();
			ZPlayFabMatchmaking.ListServers(m_filterInputField.text, PlayFabServerFound, PlayFabServerSearchDone, currentServerList == ServerListType.friends);
			ZLog.DevLog("PlayFab server search started!");
		}
	}

	public void PlayFabServerFound(PlayFabMatchmakingServerData serverData)
	{
		MonoBehaviour.print("Found PlayFab server with name: " + serverData.serverName);
		if (!PlayFabDisplayEntry(serverData))
		{
			return;
		}
		if (m_playFabTemporarySearchServerList.TryGetValue(serverData, out var value))
		{
			if (serverData.tickCreated > value.tickCreated)
			{
				m_playFabTemporarySearchServerList.Remove(serverData);
				m_playFabTemporarySearchServerList.Add(serverData, serverData);
			}
		}
		else
		{
			m_playFabTemporarySearchServerList.Add(serverData, serverData);
		}
	}

	private bool PlayFabDisplayEntry(PlayFabMatchmakingServerData serverData)
	{
		if (serverData == null)
		{
			return false;
		}
		if (currentServerList == ServerListType.community)
		{
			return true;
		}
		return false;
	}

	public void PlayFabServerSearchDone(ZPLayFabMatchmakingFailReason failedReason)
	{
		ZLog.DevLog("PlayFab server search done!");
		if (m_playFabServerSearchQueued)
		{
			m_playFabServerSearchQueued = false;
			m_playFabServerSearchOngoing = true;
			ZPlayFabMatchmaking.ListServers(m_filterInputField.text, PlayFabServerFound, PlayFabServerSearchDone, currentServerList == ServerListType.friends);
			ZLog.DevLog("PlayFab server search started!");
			return;
		}
		m_playFabServerSearchOngoing = false;
		if (currentServerList != ServerListType.friends)
		{
			m_crossplayMatchmakingServerList.Clear();
		}
		foreach (KeyValuePair<PlayFabMatchmakingServerData, PlayFabMatchmakingServerData> playFabTemporarySearchServer in m_playFabTemporarySearchServerList)
		{
			ServerStatus serverStatus;
			if (playFabTemporarySearchServer.Value.isDedicatedServer && !string.IsNullOrEmpty(playFabTemporarySearchServer.Value.serverIp))
			{
				ServerJoinDataDedicated serverJoinDataDedicated = new ServerJoinDataDedicated(playFabTemporarySearchServer.Value.serverIp);
				if (serverJoinDataDedicated.IsValid())
				{
					serverStatus = new ServerStatus(serverJoinDataDedicated);
				}
				else
				{
					ZLog.Log("Dedicated server with invalid IP address - fallback to PlayFab ID");
					serverStatus = new ServerStatus(new ServerJoinDataPlayFabUser(playFabTemporarySearchServer.Value.remotePlayerId));
				}
			}
			else
			{
				serverStatus = new ServerStatus(new ServerJoinDataPlayFabUser(playFabTemporarySearchServer.Value.remotePlayerId));
			}
			if (!playFabTemporarySearchServer.Value.gameVersion.IsValid())
			{
				ZLog.LogWarning("Failed to parse version string! Skipping server entry with name \"" + serverStatus.m_joinData.m_serverName + "\".");
				continue;
			}
			PrivilegeManager.Platform platform = PrivilegeManager.Platform.Unknown;
			serverStatus.UpdateStatus(platformRestriction: (!(playFabTemporarySearchServer.Value.gameVersion >= Version.FirstVersionWithPlatformRestriction)) ? PrivilegeManager.Platform.None : PrivilegeManager.ParsePlatform(playFabTemporarySearchServer.Value.platformRestriction), onlineStatus: OnlineStatus.Online, serverName: playFabTemporarySearchServer.Value.serverName, playerCount: playFabTemporarySearchServer.Value.numPlayers, gameVersion: playFabTemporarySearchServer.Value.gameVersion, modifiers: playFabTemporarySearchServer.Value.modifiers, networkVersion: playFabTemporarySearchServer.Value.networkVersion, isPasswordProtected: playFabTemporarySearchServer.Value.havePassword, host: playFabTemporarySearchServer.Value.xboxUserId);
			m_crossplayMatchmakingServerList.Add(serverStatus);
		}
		m_playFabTemporarySearchServerList.Clear();
		filteredListOutdated = true;
	}

	public void RequestServerList()
	{
		ZLog.DevLog("Request serverlist");
		if (!m_serverRefreshButton.interactable)
		{
			ZLog.DevLog("Server queue already running");
			return;
		}
		m_serverRefreshButton.interactable = false;
		m_lastServerListRequesTime = Time.time;
		m_steamMatchmakingServerList.Clear();
		ZSteamMatchmaking.instance.RequestServerlist();
		RequestPlayFabServerListIfUnchangedIn(0f);
		ReloadLocalServerLists();
		filteredListOutdated = true;
		if (currentServerListIsLocal)
		{
			UpdateLocalServerListStatus();
		}
	}

	private void ReloadLocalServerLists()
	{
		if (!m_localServerListsLoaded)
		{
			LoadServerListFromDisk(ServerListType.favorite, ref m_favoriteServerList);
			LoadServerListFromDisk(ServerListType.recent, ref m_recentServerList);
			m_localServerListsLoaded = true;
			return;
		}
		foreach (ServerStatus value in m_allLoadedServerData.Values)
		{
			value.Reset();
		}
	}

	public void FlushLocalServerLists()
	{
		if (m_localServerListsLoaded)
		{
			SaveServerListToDisk(ServerListType.favorite, m_favoriteServerList);
			SaveServerListToDisk(ServerListType.recent, m_recentServerList);
			m_favoriteServerList.Clear();
			m_recentServerList.Clear();
			m_allLoadedServerData.Clear();
			m_localServerListsLoaded = false;
			filteredListOutdated = true;
		}
	}

	public void OnServerFilterChanged(bool isTyping = false)
	{
		ZSteamMatchmaking.instance.SetNameFilter(m_filterInputField.text);
		ZSteamMatchmaking.instance.SetFriendFilter(currentServerList == ServerListType.friends);
		if (!currentServerListIsLocal)
		{
			RequestPlayFabServerListIfUnchangedIn(isTyping ? 0.5f : 0f);
		}
		filteredListOutdated = true;
		if (currentServerListIsLocal)
		{
			UpdateServerListGui(centerSelection: false);
			UpdateServerCount();
		}
	}

	private void UpdateGamepad()
	{
		if (ZInput.IsGamepadActive())
		{
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				SetSelectedServer(GetSelectedServer() + 1, centerSelection: true);
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				SetSelectedServer(GetSelectedServer() - 1, centerSelection: true);
			}
		}
	}

	private void UpdateKeyboard()
	{
		if (ZInput.GetKeyDown(KeyCode.UpArrow))
		{
			SetSelectedServer(GetSelectedServer() - 1, centerSelection: true);
		}
		if (ZInput.GetKeyDown(KeyCode.DownArrow))
		{
			SetSelectedServer(GetSelectedServer() + 1, centerSelection: true);
		}
		int num = 0;
		num += (ZInput.GetKeyDown(KeyCode.W) ? (-1) : 0);
		num += (ZInput.GetKeyDown(KeyCode.S) ? 1 : 0);
		int selectedServer = GetSelectedServer();
		if (num != 0 && !m_filterInputField.isFocused && m_favoriteServerList.Count == m_filteredList.Count && currentServerList == ServerListType.favorite && selectedServer >= 0 && selectedServer + num >= 0 && selectedServer + num < m_favoriteServerList.Count)
		{
			if (num > 0)
			{
				OnMoveServerDownButton();
			}
			else
			{
				OnMoveServerUpButton();
			}
		}
	}

	public static void AddToRecentServersList(ServerJoinData data)
	{
		if (instance != null)
		{
			instance.AddToRecentServersListCached(data);
			return;
		}
		if (data == null)
		{
			ZLog.LogError("Couldn't add server to server list, server data was null");
			return;
		}
		List<ServerJoinData> destination = new List<ServerJoinData>();
		if (!LoadServerListFromDisk(ServerListType.recent, ref destination))
		{
			ZLog.Log("Server list doesn't exist yet");
		}
		for (int i = 0; i < destination.Count; i++)
		{
			if (destination[i] == data)
			{
				destination.RemoveAt(i);
				i--;
			}
		}
		destination.Insert(0, data);
		int num = ((maxRecentServers > 0) ? Mathf.Max(destination.Count - maxRecentServers, 0) : 0);
		for (int j = 0; j < num; j++)
		{
			destination.RemoveAt(destination.Count - 1);
		}
		switch (SaveServerListToDisk(ServerListType.recent, destination))
		{
		case SaveStatusCode.CloudQuotaExceeded:
			ZLog.LogWarning("Couln't add server with name " + data.m_serverName + " to server list, cloud quota exceeded.");
			break;
		case SaveStatusCode.UnsupportedServerListType:
			ZLog.LogError("Couln't add server with name " + data.m_serverName + " to server list, tried to save an unsupported server list type");
			break;
		case SaveStatusCode.UnknownServerBackend:
			ZLog.LogError("Couln't add server with name " + data.m_serverName + " to server list, tried to save a server entry with an unknown server backend");
			break;
		default:
			ZLog.LogError("Couln't add server with name " + data.m_serverName + " to server list, unknown issue when saving to disk");
			break;
		case SaveStatusCode.Succeess:
			ZLog.Log("Added server with name " + data.m_serverName + " to server list");
			break;
		}
	}

	private void AddToRecentServersListCached(ServerJoinData data)
	{
		if (data == null)
		{
			ZLog.LogError("Couldn't add server to server list, server data was null");
			return;
		}
		ServerStatus serverStatus = null;
		for (int i = 0; i < m_recentServerList.Count; i++)
		{
			if (m_recentServerList[i].m_joinData == data)
			{
				serverStatus = m_recentServerList[i];
				m_recentServerList.RemoveAt(i);
				i--;
			}
		}
		if (serverStatus == null)
		{
			if (m_allLoadedServerData.TryGetValue(data, out var value))
			{
				m_recentServerList.Insert(0, value);
			}
			else
			{
				ServerStatus serverStatus2 = new ServerStatus(data);
				m_allLoadedServerData.Add(data, serverStatus2);
				m_recentServerList.Insert(0, serverStatus2);
			}
		}
		else
		{
			m_recentServerList.Insert(0, serverStatus);
		}
		int num = ((maxRecentServers > 0) ? Mathf.Max(m_recentServerList.Count - maxRecentServers, 0) : 0);
		for (int j = 0; j < num; j++)
		{
			m_recentServerList.RemoveAt(m_recentServerList.Count - 1);
		}
		ZLog.Log("Added server with name " + data.m_serverName + " to server list");
	}

	public bool LoadServerListFromDisk(ServerListType listType, ref List<ServerStatus> list)
	{
		List<ServerJoinData> destination = new List<ServerJoinData>();
		if (!LoadServerListFromDisk(listType, ref destination))
		{
			return false;
		}
		list.Clear();
		for (int i = 0; i < destination.Count; i++)
		{
			if (m_allLoadedServerData.TryGetValue(destination[i], out var value))
			{
				list.Add(value);
				continue;
			}
			ServerStatus serverStatus = new ServerStatus(destination[i]);
			m_allLoadedServerData.Add(destination[i], serverStatus);
			list.Add(serverStatus);
		}
		return true;
	}

	private static List<StorageLocation> GetServerListFileLocations(ServerListType listType)
	{
		List<StorageLocation> list = new List<StorageLocation>();
		switch (listType)
		{
		case ServerListType.favorite:
			list.Add(new StorageLocation(GetFavoriteListFile(FileHelpers.FileSource.Local), FileHelpers.FileSource.Local));
			if (FileHelpers.m_cloudEnabled)
			{
				list.Add(new StorageLocation(GetFavoriteListFile(FileHelpers.FileSource.Cloud), FileHelpers.FileSource.Cloud));
			}
			break;
		case ServerListType.recent:
			list.Add(new StorageLocation(GetRecentListFile(FileHelpers.FileSource.Local), FileHelpers.FileSource.Local));
			if (FileHelpers.m_cloudEnabled)
			{
				list.Add(new StorageLocation(GetRecentListFile(FileHelpers.FileSource.Cloud), FileHelpers.FileSource.Cloud));
			}
			break;
		default:
			return null;
		}
		return list;
	}

	private static bool LoadUniqueServerListEntriesIntoList(StorageLocation location, ref List<ServerJoinData> joinData)
	{
		HashSet<ServerJoinData> hashSet = new HashSet<ServerJoinData>();
		for (int i = 0; i < joinData.Count; i++)
		{
			hashSet.Add(joinData[i]);
		}
		FileReader fileReader;
		try
		{
			fileReader = new FileReader(location.path, location.source);
		}
		catch (Exception ex)
		{
			ZLog.Log("Failed to load: " + location.path + " (" + ex.Message + ")");
			return false;
		}
		byte[] data;
		try
		{
			BinaryReader binary = fileReader.m_binary;
			int count = binary.ReadInt32();
			data = binary.ReadBytes(count);
		}
		catch (Exception ex2)
		{
			ZLog.LogError($"error loading player.dat. Source: {location.source}, Path: {location.path}, Error: {ex2.Message}");
			fileReader.Dispose();
			return false;
		}
		fileReader.Dispose();
		ZPackage zPackage = new ZPackage(data);
		try
		{
			uint num = zPackage.ReadUInt();
			if (num != 0 && num != 1)
			{
				ZLog.LogError("Couldn't read list of version " + num);
				return false;
			}
			int num2 = zPackage.ReadInt();
			for (int j = 0; j < num2; j++)
			{
				ServerJoinData serverJoinData = null;
				string text = zPackage.ReadString();
				string serverName = zPackage.ReadString();
				switch (text)
				{
				case "Steam user":
					serverJoinData = new ServerJoinDataSteamUser(zPackage.ReadULong());
					break;
				case "PlayFab user":
					serverJoinData = new ServerJoinDataPlayFabUser(zPackage.ReadString());
					break;
				case "Dedicated":
					serverJoinData = ((num == 0) ? new ServerJoinDataDedicated(zPackage.ReadUInt(), (ushort)zPackage.ReadUInt()) : new ServerJoinDataDedicated(zPackage.ReadString(), (ushort)zPackage.ReadUInt()));
					break;
				default:
					ZLog.LogError("Unsupported backend! This should be an impossible code path if the server list was saved and loaded properly.");
					return false;
				}
				if (serverJoinData != null)
				{
					serverJoinData.m_serverName = serverName;
					if (!hashSet.Contains(serverJoinData))
					{
						joinData.Add(serverJoinData);
					}
				}
			}
		}
		catch (EndOfStreamException ex3)
		{
			ZLog.LogWarning($"Something is wrong with the server list at path {location.path} and source {location.source}, reached the end of the stream unexpectedly! Entries that have successfully been read so far have been added to the server list. \n" + ex3.StackTrace);
		}
		return true;
	}

	public static bool LoadServerListFromDisk(ServerListType listType, ref List<ServerJoinData> destination)
	{
		List<StorageLocation> serverListFileLocations = GetServerListFileLocations(listType);
		if (serverListFileLocations == null)
		{
			ZLog.LogError("Can't load a server list of unsupported type");
			return false;
		}
		for (int i = 0; i < serverListFileLocations.Count; i++)
		{
			if (!FileHelpers.Exists(serverListFileLocations[i].path, serverListFileLocations[i].source))
			{
				serverListFileLocations.RemoveAt(i);
				i--;
			}
		}
		if (serverListFileLocations.Count <= 0)
		{
			ZLog.Log("No list saved! Aborting load operation");
			return false;
		}
		SortedList<DateTime, List<StorageLocation>> sortedList = new SortedList<DateTime, List<StorageLocation>>();
		for (int j = 0; j < serverListFileLocations.Count; j++)
		{
			DateTime lastWriteTime = FileHelpers.GetLastWriteTime(serverListFileLocations[j].path, serverListFileLocations[j].source);
			if (sortedList.ContainsKey(lastWriteTime))
			{
				sortedList[lastWriteTime].Add(serverListFileLocations[j]);
				continue;
			}
			List<StorageLocation> list = new List<StorageLocation>();
			list.Add(serverListFileLocations[j]);
			sortedList.Add(lastWriteTime, list);
		}
		List<ServerJoinData> joinData = new List<ServerJoinData>();
		for (int num = sortedList.Count - 1; num >= 0; num--)
		{
			for (int k = 0; k < sortedList.Values[num].Count; k++)
			{
				if (!LoadUniqueServerListEntriesIntoList(sortedList.Values[num][k], ref joinData))
				{
					ZLog.Log("Failed to load list entries! Aborting load operation.");
					return false;
				}
			}
		}
		destination = joinData;
		return true;
	}

	public static SaveStatusCode SaveServerListToDisk(ServerListType listType, List<ServerStatus> list)
	{
		List<ServerJoinData> list2 = new List<ServerJoinData>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			list2.Add(list[i].m_joinData);
		}
		return SaveServerListToDisk(listType, list2);
	}

	private static SaveStatusCode SaveServerListEntries(StorageLocation location, List<ServerJoinData> list)
	{
		string oldFile = location.path + ".old";
		string text = location.path + ".new";
		ZPackage zPackage = new ZPackage();
		zPackage.Write(1u);
		zPackage.Write(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			ServerJoinData serverJoinData = list[i];
			zPackage.Write(serverJoinData.GetDataName());
			zPackage.Write(serverJoinData.m_serverName);
			switch (serverJoinData.GetDataName())
			{
			case "Steam user":
				zPackage.Write((ulong)(serverJoinData as ServerJoinDataSteamUser).m_joinUserID);
				break;
			case "PlayFab user":
				zPackage.Write((serverJoinData as ServerJoinDataPlayFabUser).m_remotePlayerId.ToString());
				break;
			case "Dedicated":
				zPackage.Write((serverJoinData as ServerJoinDataDedicated).m_host);
				zPackage.Write((uint)(serverJoinData as ServerJoinDataDedicated).m_port);
				break;
			default:
				ZLog.LogError("Unsupported backend! Aborting save operation.");
				return SaveStatusCode.UnknownServerBackend;
			}
		}
		if (FileHelpers.m_cloudEnabled && location.source == FileHelpers.FileSource.Cloud)
		{
			ulong num = 0uL;
			if (FileHelpers.FileExistsCloud(location.path))
			{
				num += FileHelpers.GetFileSize(location.path, location.source);
			}
			num = Math.Max(4uL + (ulong)zPackage.Size(), num);
			num *= 2;
			if (FileHelpers.OperationExceedsCloudCapacity(num))
			{
				ZLog.LogWarning("Saving server list to cloud would exceed the cloud storage quota. Therefore the operation has been aborted!");
				return SaveStatusCode.CloudQuotaExceeded;
			}
		}
		byte[] array = zPackage.GetArray();
		FileWriter fileWriter = new FileWriter(text, FileHelpers.FileHelperType.Binary, location.source);
		fileWriter.m_binary.Write(array.Length);
		fileWriter.m_binary.Write(array);
		fileWriter.Finish();
		FileHelpers.ReplaceOldFile(location.path, text, oldFile, location.source);
		return SaveStatusCode.Succeess;
	}

	public static SaveStatusCode SaveServerListToDisk(ServerListType listType, List<ServerJoinData> list)
	{
		List<StorageLocation> serverListFileLocations = GetServerListFileLocations(listType);
		if (serverListFileLocations == null)
		{
			ZLog.LogError("Can't save a server list of unsupported type");
			return SaveStatusCode.UnsupportedServerListType;
		}
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < serverListFileLocations.Count; i++)
		{
			switch (SaveServerListEntries(serverListFileLocations[i], list))
			{
			case SaveStatusCode.Succeess:
				flag = true;
				break;
			case SaveStatusCode.CloudQuotaExceeded:
				flag2 = true;
				break;
			default:
				ZLog.LogError("Unknown error when saving server list");
				break;
			case SaveStatusCode.UnknownServerBackend:
				break;
			}
		}
		if (flag)
		{
			return SaveStatusCode.Succeess;
		}
		if (flag2)
		{
			return SaveStatusCode.CloudQuotaExceeded;
		}
		return SaveStatusCode.FailedUnknownReason;
	}

	public void OnAddServerOpen()
	{
		if (!m_filterInputField.isFocused)
		{
			m_addServerPanel.SetActive(value: true);
		}
	}

	public void OnAddServerClose()
	{
		m_addServerPanel.SetActive(value: false);
	}

	public void OnAddServer()
	{
		m_addServerPanel.SetActive(value: true);
		string text = m_addServerTextInput.text;
		string[] array = text.Split(':', StringSplitOptions.None);
		if (array.Length == 0)
		{
			return;
		}
		if (array.Length == 1)
		{
			string text2 = array[0];
			if (ZPlayFabMatchmaking.IsJoinCode(text2))
			{
				if (PlayFabManager.IsLoggedIn)
				{
					OnManualAddToFavoritesStart();
					ZPlayFabMatchmaking.ResolveJoinCode(text2, OnPlayFabJoinCodeSuccess, OnJoinCodeFailed);
				}
				else
				{
					OnJoinCodeFailed(ZPLayFabMatchmakingFailReason.NotLoggedIn);
				}
				return;
			}
		}
		if (array.Length == 1 || array.Length == 2)
		{
			ServerJoinDataDedicated newServerListEntryDedicated = new ServerJoinDataDedicated(text);
			OnManualAddToFavoritesStart();
			newServerListEntryDedicated.IsValidAsync(delegate(bool result)
			{
				if (result)
				{
					OnManualAddToFavoritesSuccess(new ServerStatus(newServerListEntryDedicated));
				}
				else
				{
					if (newServerListEntryDedicated.AddressVariant == ServerJoinDataDedicated.AddressType.URL)
					{
						UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfaileddnslookup", delegate
						{
							UnifiedPopup.Pop();
						}));
					}
					else
					{
						UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedincorrectformatting", delegate
						{
							UnifiedPopup.Pop();
						}));
					}
					isAwaitingServerAdd = false;
				}
			});
		}
		else
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedincorrectformatting", delegate
			{
				UnifiedPopup.Pop();
			}));
		}
	}

	private void OnManualAddToFavoritesStart()
	{
		isAwaitingServerAdd = true;
	}

	private void OnManualAddToFavoritesSuccess(ServerStatus newServerListEntry)
	{
		ServerStatus serverStatus = null;
		for (int i = 0; i < m_favoriteServerList.Count; i++)
		{
			if (m_favoriteServerList[i].m_joinData == newServerListEntry.m_joinData)
			{
				serverStatus = m_favoriteServerList[i];
				break;
			}
		}
		if (serverStatus == null)
		{
			serverStatus = newServerListEntry;
			if (m_allLoadedServerData.TryGetValue(serverStatus.m_joinData, out var value))
			{
				m_favoriteServerList.Add(value);
			}
			else
			{
				m_favoriteServerList.Add(serverStatus);
				m_allLoadedServerData.Add(serverStatus.m_joinData, serverStatus);
			}
			filteredListOutdated = true;
		}
		m_serverListTabHandler.SetActiveTab(0);
		m_startup.SetServerToJoin(serverStatus);
		SetSelectedServer(GetSelectedServer(), centerSelection: true);
		OnAddServerClose();
		m_addServerTextInput.text = "";
		isAwaitingServerAdd = false;
	}

	private void OnPlayFabJoinCodeSuccess(PlayFabMatchmakingServerData serverData)
	{
		if (serverData == null)
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$error_incompatibleversion", delegate
			{
				UnifiedPopup.Pop();
			}));
			isAwaitingServerAdd = false;
		}
		else if (serverData.platformRestriction != "None" && serverData.platformRestriction != PrivilegeManager.GetCurrentPlatform().ToString())
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$error_platformexcluded", delegate
			{
				UnifiedPopup.Pop();
			}));
			isAwaitingServerAdd = false;
		}
		else if (!PrivilegeManager.CanCrossplay && serverData.platformRestriction != PrivilegeManager.GetCurrentPlatform().ToString())
		{
			UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$xbox_error_crossplayprivilege", delegate
			{
				UnifiedPopup.Pop();
			}));
			isAwaitingServerAdd = false;
		}
		else
		{
			ZPlayFabMatchmaking.JoinCode = serverData.joinCode;
			ServerJoinData joinData = ((!serverData.isDedicatedServer || string.IsNullOrEmpty(serverData.serverIp)) ? ((ServerJoinData)new ServerJoinDataPlayFabUser(serverData.remotePlayerId)) : ((ServerJoinData)new ServerJoinDataDedicated(serverData.serverIp)));
			ServerStatus serverStatus = new ServerStatus(joinData);
			serverStatus.UpdateStatus(OnlineStatus.Online, serverData.serverName, serverData.numPlayers, serverData.gameVersion, serverData.modifiers, serverData.networkVersion, serverData.havePassword, PrivilegeManager.ParsePlatform(serverData.platformRestriction), serverData.xboxUserId);
			OnManualAddToFavoritesSuccess(serverStatus);
		}
	}

	private void OnJoinCodeFailed(ZPLayFabMatchmakingFailReason failReason)
	{
		ZLog.Log("Failed to resolve join code for the following reason: " + failReason);
		isAwaitingServerAdd = false;
		UnifiedPopup.Push(new WarningPopup("$menu_addserverfailed", "$menu_addserverfailedresolvejoincode", delegate
		{
			UnifiedPopup.Pop();
		}));
	}
}
