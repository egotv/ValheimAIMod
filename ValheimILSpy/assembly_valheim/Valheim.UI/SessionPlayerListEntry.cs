using System;
using PlatformTools.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UserManagement;

namespace Valheim.UI;

public class SessionPlayerListEntry : MonoBehaviour
{
	[SerializeField]
	private Button _button;

	[SerializeField]
	private Selectable _focusPoint;

	[SerializeField]
	private Image _selection;

	[SerializeField]
	private GameObject _viewPlayerCard;

	[SerializeField]
	private Image _outline;

	[Header("Player")]
	[SerializeField]
	private Image _hostIcon;

	[SerializeField]
	private Image _gamerpic;

	[SerializeField]
	private Sprite otherPlatformPlayerPic;

	[SerializeField]
	private TextMeshProUGUI _gamertagText;

	[SerializeField]
	private TextMeshProUGUI _characterNameText;

	[Header("Mute")]
	[SerializeField]
	private Button _muteButton;

	[SerializeField]
	private Image _muteButtonImage;

	[SerializeField]
	private Sprite _muteSprite;

	[SerializeField]
	private Sprite _unmuteSprite;

	[Header("Block")]
	[SerializeField]
	private Button _blockButton;

	[SerializeField]
	private Image _blockButtonImage;

	[SerializeField]
	private Sprite _blockSprite;

	[SerializeField]
	private Sprite _unblockSprite;

	[Header("Kick")]
	[SerializeField]
	private Button _kickButton;

	[SerializeField]
	private Image _kickButtonImage;

	private PrivilegeManager.User _user;

	private string _gamertag;

	private string _characterName;

	public bool IsSelected => _selection.enabled;

	public Selectable FocusObject => _focusPoint;

	public Selectable MuteButton => _muteButton;

	public Selectable BlockButton => _blockButton;

	public Selectable KickButton => _kickButton;

	public PrivilegeManager.User User => _user;

	public bool HasFocusObject => _focusPoint.gameObject.activeSelf;

	public bool HasMute => _muteButtonImage.gameObject.activeSelf;

	public bool HasBlock => _blockButtonImage.gameObject.activeSelf;

	public bool HasKick => _kickButtonImage.gameObject.activeSelf;

	public bool HasActivatedButtons
	{
		get
		{
			if (!_muteButtonImage.gameObject.activeSelf && !_blockButtonImage.gameObject.activeSelf)
			{
				return _kickButtonImage.gameObject.activeSelf;
			}
			return true;
		}
	}

	private bool IsXbox => _user.platform == PrivilegeManager.Platform.Xbox;

	private bool IsSteam => _user.platform == PrivilegeManager.Platform.Steam;

	public bool IsOwnPlayer
	{
		get
		{
			return _outline.gameObject.activeSelf;
		}
		set
		{
			_outline.gameObject.SetActive(value);
		}
	}

	public bool IsHost
	{
		get
		{
			return _hostIcon.gameObject.activeSelf;
		}
		set
		{
			_hostIcon.gameObject.SetActive(value);
		}
	}

	private bool CanBeKicked
	{
		get
		{
			return _kickButtonImage.gameObject.activeSelf;
		}
		set
		{
			_kickButtonImage.gameObject.SetActive(value && !IsHost);
		}
	}

	private bool CanBeBlocked
	{
		get
		{
			return _blockButtonImage.gameObject.activeSelf;
		}
		set
		{
			_blockButtonImage.gameObject.SetActive(value);
		}
	}

	private bool CanBeMuted
	{
		get
		{
			return _muteButtonImage.gameObject.activeSelf;
		}
		set
		{
			_muteButtonImage.gameObject.SetActive(value);
		}
	}

	public string Gamertag
	{
		get
		{
			return _gamertag;
		}
		set
		{
			_gamertag = value;
			_gamertagText.text = _gamertag + ((IsHost && IsXbox) ? " (Host)" : "");
		}
	}

	public string CharacterName
	{
		get
		{
			return _characterName;
		}
		set
		{
			_characterName = (IsOwnPlayer ? value : CensorShittyWords.FilterUGC(value, UGCType.CharacterName, _user.id.ToString(), 0L));
			_characterNameText.text = _characterName + ((IsHost && !IsXbox) ? " (Host)" : "");
		}
	}

	public event Action<SessionPlayerListEntry> OnKicked;

	private void Awake()
	{
		_selection.enabled = false;
		_viewPlayerCard.SetActive(value: false);
		if (_button != null)
		{
			_button.enabled = true;
		}
	}

	private void Update()
	{
		if (EventSystem.current != null && (EventSystem.current.currentSelectedGameObject == _focusPoint.gameObject || EventSystem.current.currentSelectedGameObject == _muteButton.gameObject || EventSystem.current.currentSelectedGameObject == _blockButton.gameObject || EventSystem.current.currentSelectedGameObject == _kickButton.gameObject || EventSystem.current.currentSelectedGameObject == _button.gameObject))
		{
			SelectEntry();
		}
		else
		{
			Deselect();
		}
		UpdateFocusPoint();
	}

	public void SelectEntry()
	{
		_selection.enabled = true;
		_viewPlayerCard.SetActive(IsSteam);
	}

	public void Deselect()
	{
		_selection.enabled = false;
		_viewPlayerCard.SetActive(value: false);
	}

	public void OnMute()
	{
		if (MuteList.IsMuted(_user.ToString()))
		{
			MuteList.Unmute(_user.ToString());
		}
		else
		{
			MuteList.Mute(_user.ToString());
		}
		UpdateMuteButton();
	}

	public void OnBlock()
	{
		if (BlockList.IsPlatformBlocked(_user.ToString()))
		{
			OnViewCard();
			return;
		}
		if (BlockList.IsGameBlocked(_user.ToString()))
		{
			BlockList.Unblock(_user.ToString());
		}
		else
		{
			BlockList.Block(_user.ToString());
		}
		UpdateBlockButton();
	}

	private void UpdateButtons()
	{
		UpdateMuteButton();
		UpdateBlockButton();
		UpdateFocusPoint();
	}

	private void UpdateFocusPoint()
	{
		_focusPoint.gameObject.SetActive(!HasActivatedButtons);
	}

	private void UpdateMuteButton()
	{
		_muteButtonImage.sprite = (MuteList.IsMuted(_user.ToString()) ? _unmuteSprite : _muteSprite);
	}

	public void UpdateBlockButton()
	{
		_blockButtonImage.sprite = (BlockList.IsBlocked(_user.ToString()) ? _unblockSprite : _blockSprite);
	}

	public void OnKick()
	{
		if (ZNet.instance != null)
		{
			UnifiedPopup.Push(new YesNoPopup("$menu_kick_player_title", Localization.instance.Localize("$menu_kick_player", CharacterName), delegate
			{
				ZNet.instance.Kick(CharacterName);
				this.OnKicked?.Invoke(this);
				UnifiedPopup.Pop();
			}, delegate
			{
				UnifiedPopup.Pop();
			}));
		}
	}

	public void SetValues(string characterName, PrivilegeManager.User user, bool isHost, bool canBeBlocked, bool canBeKicked, bool canBeMuted)
	{
		_user = user;
		IsHost = isHost;
		CharacterName = characterName;
		Gamertag = "";
		CanBeKicked = !isHost && canBeKicked;
		CanBeBlocked = canBeBlocked;
		CanBeMuted = false;
		if (IsXbox || IsSteam)
		{
			PlatformManager.Instance.ProfileManager.GetProfile(_user.id, UpdateProfile);
		}
		else
		{
			_gamerpic.sprite = otherPlatformPlayerPic;
		}
		UpdateButtons();
	}

	private void UpdateProfile(ulong _, Profile userProfile)
	{
		bool flag = false;
		if (IsXbox)
		{
			Gamertag = userProfile.UniqueGamertag;
			flag = true;
		}
		else if (IsSteam)
		{
			Gamertag = userProfile.UniqueGamertag;
			_gamerpic.SetSpriteFromSteamImageId(int.Parse(userProfile.Picture));
		}
		if (flag)
		{
			_gamerpic.sprite = otherPlatformPlayerPic;
		}
		UpdateButtons();
	}

	public void OnViewCard()
	{
		if (IsSteam)
		{
			PlatformManager.Instance.ProfileManager.ShowProfileCard(_user.id);
		}
	}

	public void RemoveCallbacks()
	{
		PlatformManager.Instance.ProfileManager.UnregisterProfileCallback(_user.id, UpdateProfile);
	}
}
