using System.Collections.Generic;
using PlatformTools.Core;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using UserManagement;

namespace Valheim.UI;

public class SessionPlayerList : MonoBehaviour
{
	[SerializeField]
	protected SessionPlayerListEntry _ownPlayer;

	[SerializeField]
	protected ScrollRect _scrollRect;

	[SerializeField]
	protected Button _backButton;

	private List<ZNet.PlayerInfo> _players;

	private readonly List<SessionPlayerListEntry> _remotePlayers = new List<SessionPlayerListEntry>();

	private readonly List<SessionPlayerListEntry> _allPlayers = new List<SessionPlayerListEntry>();

	protected void Awake()
	{
		BlockList.Load(Init);
	}

	private void OnDestroy()
	{
		PlatformManager.Instance.SuspendAndConstrainManager.OnConstrainedModeActivated -= OnConstrainedModeStateChanged;
	}

	private void Init()
	{
		SetEntries();
		foreach (SessionPlayerListEntry allPlayer in _allPlayers)
		{
			allPlayer.OnKicked += OnPlayerWasKicked;
		}
		_ownPlayer.FocusObject.Select();
		PlatformManager.Instance.SuspendAndConstrainManager.OnConstrainedModeActivated -= OnConstrainedModeStateChanged;
		PlatformManager.Instance.SuspendAndConstrainManager.OnConstrainedModeActivated += OnConstrainedModeStateChanged;
		UpdateBlockList();
	}

	public void OnConstrainedModeStateChanged(bool constrained)
	{
		if (!constrained)
		{
			UpdateBlockList();
		}
	}

	public void UpdateBlockList()
	{
		BlockList.UpdateAvoidList(UpdateBlockButtons);
	}

	private void UpdateBlockButtons()
	{
		foreach (SessionPlayerListEntry allPlayer in _allPlayers)
		{
			allPlayer.UpdateBlockButton();
		}
	}

	private void OnPlayerWasKicked(SessionPlayerListEntry player)
	{
		player.OnKicked -= OnPlayerWasKicked;
		_allPlayers.Remove(player);
		_remotePlayers.Remove(player);
		Object.Destroy(player.gameObject);
		UpdateNavigation();
	}

	private void SetEntries()
	{
		_allPlayers.Add(_ownPlayer);
		ZDOID localPlayerCharacterID = ZNet.instance.LocalPlayerCharacterID;
		_players = ZNet.instance.GetPlayerList();
		ZNetPeer serverPeer = ZNet.instance.GetServerPeer();
		if (!ZNet.instance.PlayerIsHost && _players.TryFindPlayerByPlayername(serverPeer.m_playerName, out var playerInfo))
		{
			if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
			{
				PrivilegeManager.User user = PrivilegeManager.ParseUser(playerInfo.Value.m_host);
				CreatePlayerEntry(user, playerInfo.Value.m_name, isHost: true);
			}
			else
			{
				PrivilegeManager.User user2 = PrivilegeManager.ParseUser(serverPeer.m_socket.GetEndPointString());
				CreatePlayerEntry(user2, serverPeer.m_playerName, isHost: true);
			}
		}
		for (int i = 0; i < _players.Count; i++)
		{
			ZNet.PlayerInfo playerInfo2 = _players[i];
			if (playerInfo2.m_characterID == localPlayerCharacterID)
			{
				PrivilegeManager.User user3 = new PrivilegeManager.User(PrivilegeManager.Platform.Steam, (ulong)SteamUser.GetSteamID());
				SetOwnPlayer(user3, ZNet.instance.PlayerIsHost);
			}
			else if (serverPeer == null || playerInfo2.m_name != serverPeer.m_playerName)
			{
				PrivilegeManager.User user4 = PrivilegeManager.ParseUser(playerInfo2.m_host);
				CreatePlayerEntry(user4, playerInfo2.m_name);
			}
		}
		UpdateNavigation();
	}

	private void UpdateNavigation()
	{
		Navigation navigation = default(Navigation);
		navigation.mode = Navigation.Mode.Explicit;
		Navigation navigation2 = navigation;
		int count = _allPlayers.Count;
		for (int i = 0; i < count; i++)
		{
			SessionPlayerListEntry sessionPlayerListEntry = _allPlayers[i];
			SessionPlayerListEntry sessionPlayerListEntry2 = ((i < count - 1) ? _allPlayers[i + 1] : null);
			Navigation navigation3 = sessionPlayerListEntry.MuteButton.navigation;
			navigation3.mode = (sessionPlayerListEntry.HasMute ? Navigation.Mode.Explicit : Navigation.Mode.None);
			Navigation navigation4 = sessionPlayerListEntry.BlockButton.navigation;
			navigation4.mode = (sessionPlayerListEntry.HasBlock ? Navigation.Mode.Explicit : Navigation.Mode.None);
			Navigation navigation5 = sessionPlayerListEntry.KickButton.navigation;
			navigation5.mode = (sessionPlayerListEntry.HasKick ? Navigation.Mode.Explicit : Navigation.Mode.None);
			Navigation navigation6 = sessionPlayerListEntry.FocusObject.navigation;
			navigation6.mode = (sessionPlayerListEntry.HasFocusObject ? Navigation.Mode.Explicit : Navigation.Mode.None);
			if (sessionPlayerListEntry2 != null)
			{
				if (!sessionPlayerListEntry.HasActivatedButtons && !sessionPlayerListEntry2.HasActivatedButtons)
				{
					navigation6.selectOnDown = sessionPlayerListEntry2.FocusObject;
				}
				else if (!sessionPlayerListEntry.HasActivatedButtons && sessionPlayerListEntry2.HasActivatedButtons)
				{
					if (sessionPlayerListEntry2.HasMute)
					{
						navigation6.selectOnDown = sessionPlayerListEntry2.MuteButton;
					}
					else if (sessionPlayerListEntry2.HasBlock)
					{
						navigation6.selectOnDown = sessionPlayerListEntry2.BlockButton;
					}
					else if (sessionPlayerListEntry2.HasKick)
					{
						navigation6.selectOnDown = sessionPlayerListEntry2.KickButton;
					}
				}
				else if (sessionPlayerListEntry.HasActivatedButtons && !sessionPlayerListEntry2.HasActivatedButtons)
				{
					if (sessionPlayerListEntry.HasMute)
					{
						navigation3.selectOnDown = sessionPlayerListEntry2.FocusObject;
					}
					if (sessionPlayerListEntry.HasBlock)
					{
						navigation4.selectOnDown = sessionPlayerListEntry2.FocusObject;
					}
					if (sessionPlayerListEntry.HasKick)
					{
						navigation5.selectOnDown = sessionPlayerListEntry2.FocusObject;
					}
				}
				else
				{
					if (sessionPlayerListEntry.HasMute)
					{
						if (sessionPlayerListEntry2.HasMute)
						{
							navigation3.selectOnDown = sessionPlayerListEntry2.MuteButton;
						}
						else if (sessionPlayerListEntry2.HasBlock)
						{
							navigation3.selectOnDown = sessionPlayerListEntry2.BlockButton;
						}
						else if (sessionPlayerListEntry2.HasKick)
						{
							navigation3.selectOnDown = sessionPlayerListEntry2.KickButton;
						}
					}
					if (sessionPlayerListEntry.HasBlock)
					{
						if (sessionPlayerListEntry2.HasBlock)
						{
							navigation4.selectOnDown = sessionPlayerListEntry2.BlockButton;
						}
						else if (sessionPlayerListEntry2.HasMute)
						{
							navigation4.selectOnDown = sessionPlayerListEntry2.MuteButton;
						}
						else if (sessionPlayerListEntry2.HasKick)
						{
							navigation4.selectOnDown = sessionPlayerListEntry2.KickButton;
						}
					}
					if (sessionPlayerListEntry.HasKick)
					{
						if (sessionPlayerListEntry2.HasKick)
						{
							navigation5.selectOnDown = sessionPlayerListEntry2.KickButton;
						}
						else if (sessionPlayerListEntry2.HasMute)
						{
							navigation5.selectOnDown = sessionPlayerListEntry2.MuteButton;
						}
						else if (sessionPlayerListEntry2.HasBlock)
						{
							navigation5.selectOnDown = sessionPlayerListEntry2.BlockButton;
						}
					}
				}
			}
			else if (sessionPlayerListEntry.HasActivatedButtons)
			{
				if (sessionPlayerListEntry.HasMute)
				{
					navigation2.selectOnUp = sessionPlayerListEntry.MuteButton;
				}
				else if (sessionPlayerListEntry.HasBlock)
				{
					navigation2.selectOnUp = sessionPlayerListEntry.BlockButton;
				}
				else if (sessionPlayerListEntry.HasKick)
				{
					navigation2.selectOnUp = sessionPlayerListEntry.KickButton;
				}
				if (sessionPlayerListEntry.HasMute)
				{
					navigation3.selectOnDown = _backButton;
				}
				if (sessionPlayerListEntry.HasBlock)
				{
					navigation4.selectOnDown = _backButton;
				}
				if (sessionPlayerListEntry.HasKick)
				{
					navigation5.selectOnDown = _backButton;
				}
			}
			else
			{
				navigation6.selectOnDown = _backButton;
				navigation2.selectOnUp = sessionPlayerListEntry.FocusObject;
			}
			sessionPlayerListEntry.MuteButton.navigation = navigation3;
			sessionPlayerListEntry.BlockButton.navigation = navigation4;
			sessionPlayerListEntry.KickButton.navigation = navigation5;
			sessionPlayerListEntry.FocusObject.navigation = navigation6;
		}
		for (int num = count - 1; num >= 0; num--)
		{
			SessionPlayerListEntry sessionPlayerListEntry3 = _allPlayers[num];
			SessionPlayerListEntry sessionPlayerListEntry4 = ((num > 0) ? _allPlayers[num - 1] : null);
			Navigation navigation7 = sessionPlayerListEntry3.MuteButton.navigation;
			Navigation navigation8 = sessionPlayerListEntry3.BlockButton.navigation;
			Navigation navigation9 = sessionPlayerListEntry3.KickButton.navigation;
			Navigation navigation10 = sessionPlayerListEntry3.FocusObject.navigation;
			if (sessionPlayerListEntry4 != null)
			{
				if (!sessionPlayerListEntry3.HasActivatedButtons && !sessionPlayerListEntry4.HasActivatedButtons)
				{
					navigation10.selectOnUp = sessionPlayerListEntry4.FocusObject;
				}
				else if (!sessionPlayerListEntry3.HasActivatedButtons && sessionPlayerListEntry4.HasActivatedButtons)
				{
					if (sessionPlayerListEntry4.HasMute)
					{
						navigation10.selectOnUp = sessionPlayerListEntry4.MuteButton;
					}
					else if (sessionPlayerListEntry4.HasBlock)
					{
						navigation10.selectOnUp = sessionPlayerListEntry4.BlockButton;
					}
					else if (sessionPlayerListEntry4.HasKick)
					{
						navigation10.selectOnUp = sessionPlayerListEntry4.KickButton;
					}
				}
				else if (sessionPlayerListEntry3.HasActivatedButtons && !sessionPlayerListEntry4.HasActivatedButtons)
				{
					if (sessionPlayerListEntry3.HasMute)
					{
						navigation7.selectOnUp = sessionPlayerListEntry4.FocusObject;
					}
					if (sessionPlayerListEntry3.HasBlock)
					{
						navigation8.selectOnUp = sessionPlayerListEntry4.FocusObject;
					}
					if (sessionPlayerListEntry3.HasKick)
					{
						navigation9.selectOnUp = sessionPlayerListEntry4.FocusObject;
					}
				}
				else
				{
					if (sessionPlayerListEntry3.HasMute)
					{
						if (sessionPlayerListEntry4.HasMute)
						{
							navigation7.selectOnUp = sessionPlayerListEntry4.MuteButton;
						}
						else if (sessionPlayerListEntry4.HasBlock)
						{
							navigation7.selectOnUp = sessionPlayerListEntry4.BlockButton;
						}
						else if (sessionPlayerListEntry4.HasKick)
						{
							navigation7.selectOnUp = sessionPlayerListEntry4.KickButton;
						}
					}
					if (sessionPlayerListEntry3.HasBlock)
					{
						if (sessionPlayerListEntry4.HasBlock)
						{
							navigation8.selectOnUp = sessionPlayerListEntry4.BlockButton;
						}
						else if (sessionPlayerListEntry4.HasMute)
						{
							navigation8.selectOnUp = sessionPlayerListEntry4.MuteButton;
						}
						else if (sessionPlayerListEntry4.HasKick)
						{
							navigation8.selectOnUp = sessionPlayerListEntry4.KickButton;
						}
					}
					if (sessionPlayerListEntry3.HasKick)
					{
						if (sessionPlayerListEntry4.HasKick)
						{
							navigation9.selectOnUp = sessionPlayerListEntry4.KickButton;
						}
						else if (sessionPlayerListEntry4.HasMute)
						{
							navigation9.selectOnUp = sessionPlayerListEntry4.MuteButton;
						}
						else if (sessionPlayerListEntry4.HasBlock)
						{
							navigation9.selectOnUp = sessionPlayerListEntry4.BlockButton;
						}
					}
				}
			}
			sessionPlayerListEntry3.MuteButton.navigation = navigation7;
			sessionPlayerListEntry3.BlockButton.navigation = navigation8;
			sessionPlayerListEntry3.KickButton.navigation = navigation9;
			sessionPlayerListEntry3.FocusObject.navigation = navigation10;
		}
		_backButton.navigation = navigation2;
	}

	private void SetOwnPlayer(PrivilegeManager.User user, bool isHost)
	{
		UserInfo localUser = UserInfo.GetLocalUser();
		_ownPlayer.IsOwnPlayer = true;
		_ownPlayer.SetValues(localUser.Name, user, isHost, canBeBlocked: false, canBeKicked: false, canBeMuted: false);
	}

	private void CreatePlayerEntry(PrivilegeManager.User user, string name, bool isHost = false)
	{
		SessionPlayerListEntry sessionPlayerListEntry = Object.Instantiate(_ownPlayer, _scrollRect.content);
		sessionPlayerListEntry.IsOwnPlayer = false;
		sessionPlayerListEntry.SetValues(name, user, isHost, canBeBlocked: true, !isHost && ZNet.instance.LocalPlayerIsAdminOrHost(), canBeMuted: false);
		if (!isHost)
		{
			_remotePlayers.Add(sessionPlayerListEntry);
		}
		_allPlayers.Add(sessionPlayerListEntry);
	}

	public void OnBack()
	{
		foreach (SessionPlayerListEntry allPlayer in _allPlayers)
		{
			allPlayer.RemoveCallbacks();
		}
		BlockList.Persist();
		Object.Destroy(base.gameObject);
	}

	private void Update()
	{
		UpdateScrollPosition();
		if (ZInput.GetKeyDown(KeyCode.Escape))
		{
			OnBack();
		}
	}

	private void UpdateScrollPosition()
	{
		if (!_scrollRect.verticalScrollbar.gameObject.activeSelf)
		{
			return;
		}
		foreach (SessionPlayerListEntry allPlayer in _allPlayers)
		{
			if (allPlayer.IsSelected && !_scrollRect.IsVisible(allPlayer.transform as RectTransform))
			{
				_scrollRect.SnapToChild(allPlayer.transform as RectTransform);
				break;
			}
		}
	}
}
