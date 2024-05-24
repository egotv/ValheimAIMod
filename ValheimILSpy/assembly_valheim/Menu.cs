using System;
using System.Collections;
using System.Collections.Generic;
using SoftReferenceableAssets.SceneManagement;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valheim.SettingsGui;

public class Menu : MonoBehaviour
{
	public delegate void CloudStorageFullOkCallback();

	private enum CloseMenuState : byte
	{
		SettingsOpen,
		Blocked,
		CanBeClosed
	}

	private bool m_cloudStorageWarningShown;

	private List<CloudStorageFullOkCallback> cloudStorageFullOkCallbackList = new List<CloudStorageFullOkCallback>();

	[SerializeField]
	private GameObject CurrentPlayersPrefab;

	private GameObject m_currentPlayersInstance;

	public Button menuCurrentPlayersListButton;

	private GameObject m_settingsInstance;

	public Button saveButton;

	public TMP_Text lastSaveText;

	private DateTime m_lastSavedDate = DateTime.MinValue;

	public RectTransform menuEntriesParent;

	private static Menu m_instance;

	public Transform m_root;

	public Transform m_menuDialog;

	public Transform m_quitDialog;

	public Transform m_logoutDialog;

	public GameObject m_cloudStorageWarning;

	public GameObject m_cloudStorageWarningNextSave;

	public GameObject m_settingsPrefab;

	public GameObject m_feedbackPrefab;

	public GameObject m_gamepadRoot;

	public GamepadMapController m_gamepadMapController;

	public SceneReference m_startScene;

	private int m_hiddenFrames;

	public GameObject m_skipButton;

	private int m_updateLocalizationTimer;

	private int m_manualSaveCooldownUntil;

	private const int ManualSavingCooldownTime = 60;

	private bool m_rebuildLayout;

	private bool m_saveOnLogout = true;

	private bool m_loadStartSceneOnLogout = true;

	private CloseMenuState m_closeMenuState = CloseMenuState.CanBeClosed;

	private Button m_firstMenuButton;

	public static bool ExceedCloudStorageTest;

	public static Menu instance => m_instance;

	public bool PlayerListActive
	{
		get
		{
			if (m_currentPlayersInstance != null)
			{
				return m_currentPlayersInstance.activeSelf;
			}
			return false;
		}
	}

	private void Start()
	{
		m_instance = this;
		Hide();
		UpdateNavigation();
		m_rebuildLayout = true;
		if (ZNet.GetWorldIfIsHost() == null)
		{
			PlayerProfile.SavingFinished = (Action)Delegate.Combine(PlayerProfile.SavingFinished, new Action(SaveFinished));
		}
		else
		{
			ZNet.WorldSaveFinished = (Action)Delegate.Combine(ZNet.WorldSaveFinished, new Action(SaveFinished));
		}
	}

	private void HandleInputLayoutChanged()
	{
		UpdateCursor();
		if (!ZInput.IsGamepadActive())
		{
			m_gamepadRoot.gameObject.SetActive(value: false);
			return;
		}
		m_gamepadRoot.gameObject.SetActive(value: true);
		m_gamepadMapController.Show(ZInput.InputLayout, GamepadMapController.GetType(ZInput.PlayStationGlyphs, Settings.IsSteamRunningOnSteamDeck()));
	}

	private void UpdateNavigation()
	{
		Button component = m_menuDialog.Find("MenuEntries/Logout").GetComponent<Button>();
		Button component2 = m_menuDialog.Find("MenuEntries/Exit").GetComponent<Button>();
		Button component3 = m_menuDialog.Find("MenuEntries/Continue").GetComponent<Button>();
		Button component4 = m_menuDialog.Find("MenuEntries/Settings").GetComponent<Button>();
		Button component5 = m_menuDialog.Find("MenuEntries/SkipIntro").GetComponent<Button>();
		m_firstMenuButton = component3;
		List<Button> list = new List<Button>();
		list.Add(component3);
		if (component5.gameObject.activeSelf)
		{
			list.Add(component5);
		}
		if (saveButton.interactable)
		{
			list.Add(saveButton);
		}
		if (menuCurrentPlayersListButton.gameObject.activeSelf)
		{
			list.Add(menuCurrentPlayersListButton);
		}
		list.Add(component4);
		list.Add(component);
		if (component2.gameObject.activeSelf)
		{
			list.Add(component2);
		}
		for (int i = 0; i < list.Count; i++)
		{
			Navigation navigation = list[i].navigation;
			if (i > 0)
			{
				navigation.selectOnUp = list[i - 1];
			}
			else
			{
				navigation.selectOnUp = list[list.Count - 1];
			}
			if (i < list.Count - 1)
			{
				navigation.selectOnDown = list[i + 1];
			}
			else
			{
				navigation.selectOnDown = list[0];
			}
			navigation.mode = Navigation.Mode.Explicit;
			list[i].navigation = navigation;
		}
	}

	private void OnDestroy()
	{
		PlayerProfile.SavingFinished = (Action)Delegate.Remove(PlayerProfile.SavingFinished, new Action(SaveFinished));
		ZNet.WorldSaveFinished = (Action)Delegate.Remove(ZNet.WorldSaveFinished, new Action(SaveFinished));
		ZInput.OnInputLayoutChanged -= HandleInputLayoutChanged;
	}

	private void SaveFinished()
	{
		m_lastSavedDate = DateTime.Now;
		m_rebuildLayout = true;
		if (ZNet.instance != null && !ZNet.instance.IsSaving() && (!CanSaveToCloudStorage() || ExceedCloudStorageTest))
		{
			ShowCloudStorageLowNextSaveWarning();
		}
	}

	private static bool CanSaveToCloudStorage()
	{
		return SaveSystem.CanSaveToCloudStorage(ZNet.GetWorldIfIsHost(), Game.instance.GetPlayerProfile());
	}

	public void Show()
	{
		Gogan.LogEvent("Screen", "Enter", "Menu", 0L);
		m_root.gameObject.SetActive(value: true);
		m_menuDialog.gameObject.SetActive(value: true);
		m_skipButton.gameObject.SetActive((bool)Player.m_localPlayer && Player.m_localPlayer.InIntro());
		m_logoutDialog.gameObject.SetActive(value: false);
		m_quitDialog.gameObject.SetActive(value: false);
		menuCurrentPlayersListButton.gameObject.SetActive(!ZNet.IsSinglePlayer);
		saveButton.gameObject.SetActive(value: true);
		lastSaveText.gameObject.SetActive(m_lastSavedDate > DateTime.MinValue);
		if (Player.m_localPlayer != null)
		{
			Game.Pause();
		}
		if (Chat.instance.IsChatDialogWindowVisible())
		{
			Chat.instance.Hide();
		}
		JoinCode.Show();
		UpdateNavigation();
		m_rebuildLayout = true;
		m_saveOnLogout = true;
		m_loadStartSceneOnLogout = true;
		ZInput.OnInputLayoutChanged -= HandleInputLayoutChanged;
		ZInput.OnInputLayoutChanged += HandleInputLayoutChanged;
		HandleInputLayoutChanged();
	}

	private IEnumerator SelectEntry(GameObject entry)
	{
		yield return null;
		yield return null;
		EventSystem.current.SetSelectedGameObject(entry);
		UpdateCursor();
	}

	public void Hide()
	{
		m_root.gameObject.SetActive(value: false);
		JoinCode.Hide();
		Game.Unpause();
		if (ZInput.IsGamepadActive())
		{
			PlayerController.SetTakeInputDelay(0.1f);
		}
		ZInput.OnInputLayoutChanged -= UpdateCursor;
	}

	private void UpdateCursor()
	{
		Cursor.lockState = ((!ZInput.IsMouseActive()) ? CursorLockMode.Locked : CursorLockMode.None);
		Cursor.visible = ZInput.IsMouseActive();
	}

	public static bool IsVisible()
	{
		if (m_instance == null)
		{
			return false;
		}
		if (m_instance.m_hiddenFrames > 2)
		{
			return UnifiedPopup.WasVisibleThisFrame();
		}
		return true;
	}

	public static bool IsActive()
	{
		if (m_instance == null)
		{
			return false;
		}
		if (!m_instance.m_root.gameObject.activeSelf)
		{
			return UnifiedPopup.WasVisibleThisFrame();
		}
		return true;
	}

	private void Update()
	{
		if (Game.instance.IsShuttingDown())
		{
			Hide();
			return;
		}
		if (m_root.gameObject.activeSelf)
		{
			m_hiddenFrames = 0;
			if ((ZInput.GetKeyDown(KeyCode.Escape) || (ZInput.GetButtonDown("JoyMenu") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper")) && !ZInput.GetButton("JoyAltKeys")) || ZInput.GetButtonDown("JoyButtonB")) && !m_settingsInstance && !m_currentPlayersInstance && !Feedback.IsVisible() && !UnifiedPopup.IsVisible())
			{
				if (m_quitDialog.gameObject.activeSelf)
				{
					OnQuitNo();
				}
				else if (m_logoutDialog.gameObject.activeSelf)
				{
					OnLogoutNo();
				}
				else
				{
					if (m_closeMenuState == CloseMenuState.SettingsOpen && ZInput.GetButtonDown("JoyButtonB"))
					{
						m_closeMenuState = CloseMenuState.Blocked;
					}
					if (m_closeMenuState != CloseMenuState.Blocked)
					{
						Hide();
					}
				}
			}
			if (m_closeMenuState == CloseMenuState.Blocked && ZInput.GetButtonUp("JoyButtonB"))
			{
				m_closeMenuState = CloseMenuState.CanBeClosed;
			}
			if (ZInput.IsGamepadActive() && base.gameObject.activeInHierarchy && EventSystem.current.currentSelectedGameObject == null && m_firstMenuButton != null)
			{
				StartCoroutine(SelectEntry(m_firstMenuButton.gameObject));
			}
			if (m_lastSavedDate > DateTime.MinValue)
			{
				int minutes = (DateTime.Now - m_lastSavedDate).Minutes;
				string text = minutes.ToString();
				if (minutes < 1)
				{
					text = "<1";
				}
				lastSaveText.text = Localization.instance.Localize("$menu_manualsavetime", text);
			}
			if ((saveButton.interactable && (float)m_manualSaveCooldownUntil > Time.unscaledTime) || (!saveButton.interactable && (float)m_manualSaveCooldownUntil < Time.unscaledTime))
			{
				saveButton.interactable = (float)m_manualSaveCooldownUntil < Time.unscaledTime;
				UpdateNavigation();
			}
			if (m_rebuildLayout)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(menuEntriesParent);
				lastSaveText.gameObject.SetActive(m_lastSavedDate > DateTime.MinValue);
				m_rebuildLayout = false;
				StartCoroutine(SelectEntry(m_firstMenuButton.gameObject));
			}
		}
		else
		{
			m_hiddenFrames++;
			bool flag = !InventoryGui.IsVisible() && !Minimap.IsOpen() && !Console.IsVisible() && !TextInput.IsVisible() && !ZNet.instance.InPasswordDialog() && !StoreGui.IsVisible() && !Hud.IsPieceSelectionVisible() && !UnifiedPopup.IsVisible() && !PlayerCustomizaton.IsBarberGuiVisible() && !Hud.InRadial();
			if ((ZInput.GetKeyDown(KeyCode.Escape) || (ZInput.GetButtonDown("JoyMenu") && (!ZInput.GetButton("JoyLTrigger") || !ZInput.GetButton("JoyLBumper")) && !ZInput.GetButton("JoyAltKeys"))) && flag && !Chat.instance.m_wasFocused)
			{
				Show();
			}
		}
		if (m_updateLocalizationTimer > 30)
		{
			Localization.instance.ReLocalizeVisible(base.transform);
			m_updateLocalizationTimer = 0;
		}
		else
		{
			m_updateLocalizationTimer++;
		}
	}

	public void OnSkip()
	{
		Game.instance.RequestRespawn(0f);
		if ((bool)Valkyrie.m_instance)
		{
			Valkyrie.m_instance.DropPlayer(destroy: true);
		}
		Hide();
	}

	public void OnSettings()
	{
		Gogan.LogEvent("Screen", "Enter", "Settings", 0L);
		m_settingsInstance = UnityEngine.Object.Instantiate(m_settingsPrefab, base.transform);
		m_closeMenuState = CloseMenuState.SettingsOpen;
	}

	public void OnQuit()
	{
		m_quitDialog.gameObject.SetActive(value: true);
		m_menuDialog.gameObject.SetActive(value: false);
	}

	public void OnCurrentPlayers()
	{
		if (m_currentPlayersInstance == null)
		{
			m_currentPlayersInstance = UnityEngine.Object.Instantiate(CurrentPlayersPrefab, base.transform);
		}
		else
		{
			m_currentPlayersInstance.SetActive(value: true);
		}
	}

	public void OnManualSave()
	{
		if ((float)m_manualSaveCooldownUntil >= Time.unscaledTime)
		{
			return;
		}
		if (!CanSaveToCloudStorage())
		{
			m_logoutDialog.gameObject.SetActive(value: false);
			ShowCloudStorageFullWarning(Logout);
		}
		else
		{
			if (!(ZNet.instance != null))
			{
				return;
			}
			if (ZNet.IsSinglePlayer || ZNet.instance.GetPeerConnections() < 1)
			{
				if (!ZNet.instance.EnoughDiskSpaceAvailable(out var _))
				{
					return;
				}
				Game.instance.SavePlayerProfile(setLogoutPoint: true);
				ZNet.instance.Save(sync: true);
			}
			else
			{
				ZNet.instance.SaveWorldAndPlayerProfiles();
			}
			m_manualSaveCooldownUntil = (int)Time.unscaledTime + 60;
			m_saveOnLogout = ZNet.instance != null && ZNet.instance.IsServer();
		}
	}

	public void OnQuitYes()
	{
		ZNet.instance.EnoughDiskSpaceAvailable(out var exitGamePopupShown, exitGamePrompt: true, delegate(bool exit)
		{
			if (exit)
			{
				QuitGame();
			}
		});
		if (exitGamePopupShown)
		{
			return;
		}
		if (!CanSaveToCloudStorage())
		{
			m_quitDialog.gameObject.SetActive(value: false);
			if (FileHelpers.m_cloudOnly)
			{
				m_saveOnLogout = false;
			}
			ShowCloudStorageFullWarning(QuitGame);
		}
		else
		{
			QuitGame();
		}
	}

	private void QuitGame()
	{
		Gogan.LogEvent("Game", "Quit", "", 0L);
		Application.Quit();
	}

	public void OnQuitNo()
	{
		m_quitDialog.gameObject.SetActive(value: false);
		m_menuDialog.gameObject.SetActive(value: true);
	}

	public void OnLogout()
	{
		m_menuDialog.gameObject.SetActive(value: false);
		m_logoutDialog.gameObject.SetActive(value: true);
	}

	public void OnLogoutYes()
	{
		if (m_saveOnLogout && !CanSaveToCloudStorage())
		{
			m_logoutDialog.gameObject.SetActive(value: false);
			if (FileHelpers.m_cloudOnly)
			{
				m_saveOnLogout = false;
			}
			ShowCloudStorageFullWarning(Logout);
		}
		else
		{
			Logout();
		}
	}

	public void Logout()
	{
		Gogan.LogEvent("Game", "LogOut", "", 0L);
		Game.instance.Logout(m_saveOnLogout, m_loadStartSceneOnLogout);
	}

	public void OnLogoutNo()
	{
		m_logoutDialog.gameObject.SetActive(value: false);
		m_menuDialog.gameObject.SetActive(value: true);
	}

	public void OnClose()
	{
		Gogan.LogEvent("Screen", "Exit", "Menu", 0L);
		Hide();
	}

	public void OnButtonFeedback()
	{
		UnityEngine.Object.Instantiate(m_feedbackPrefab, base.transform);
	}

	public void ShowCloudStorageFullWarning(CloudStorageFullOkCallback okCallback)
	{
		if (m_cloudStorageWarningShown)
		{
			okCallback?.Invoke();
			return;
		}
		if (okCallback != null)
		{
			cloudStorageFullOkCallbackList.Add(okCallback);
		}
		m_cloudStorageWarning.SetActive(value: true);
	}

	public void OnCloudStorageFullWarningOk()
	{
		int count = cloudStorageFullOkCallbackList.Count;
		while (count-- > 0)
		{
			cloudStorageFullOkCallbackList[count]();
		}
		cloudStorageFullOkCallbackList.Clear();
		m_cloudStorageWarningShown = true;
		m_cloudStorageWarning.SetActive(value: false);
	}

	public void ShowCloudStorageLowNextSaveWarning()
	{
		m_saveOnLogout = false;
		m_loadStartSceneOnLogout = false;
		Logout();
		m_cloudStorageWarningNextSave.SetActive(value: true);
	}

	public void OnCloudStorageLowNextSaveWarningOk()
	{
		SceneManager.LoadScene(m_startScene);
	}
}
