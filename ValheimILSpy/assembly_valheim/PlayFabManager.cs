using System;
using System.Collections;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Party;
using UnityEngine;

public class PlayFabManager : MonoBehaviour
{
	public const string TitleId = "6E223";

	private LoginState m_loginState;

	private string m_entityToken;

	private DateTime? m_tokenExpiration;

	private float m_refreshThresh;

	private int m_loginAttempts;

	private const float EntityTokenUpdateDurationMin = 420f;

	private const float EntityTokenUpdateDurationMax = 840f;

	private const float LoginRetryDelay = 1f;

	private const float LoginRetryDelayMax = 30f;

	private const float LoginRetryJitterFactor = 0.125f;

	private static string m_customId;

	private Coroutine m_updateEntityTokenCoroutine;

	public static bool IsLoggedIn
	{
		get
		{
			if (instance == null)
			{
				return false;
			}
			return instance.m_loginState == LoginState.LoggedIn;
		}
	}

	public static LoginState CurrentLoginState
	{
		get
		{
			if (instance == null)
			{
				return LoginState.NotLoggedIn;
			}
			return instance.m_loginState;
		}
	}

	public static DateTime NextRetryUtc { get; private set; } = DateTime.MinValue;


	public EntityKey Entity { get; private set; }

	public static PlayFabManager instance { get; private set; }

	public event LoginFinishedCallback LoginFinished;

	public static void SetCustomId(PrivilegeManager.Platform platform, string id)
	{
		m_customId = PrivilegeManager.GetPlatformPrefix(platform) + id;
		ZLog.Log($"PlayFab custom ID set to \"{m_customId}\"");
		if (instance != null && CurrentLoginState == LoginState.NotLoggedIn)
		{
			instance.Login();
		}
	}

	public static void Initialize()
	{
		if (instance == null)
		{
			Application.logMessageReceived += HandleLog;
			new GameObject("PlayFabManager").AddComponent<PlayFabManager>();
			new GameObject("PlayFabMultiplayerManager").AddComponent<PlayFabMultiplayerManager>();
		}
	}

	public void Start()
	{
		if (instance != null)
		{
			ZLog.LogError("Tried to create another PlayFabManager when one already exists! Ignoring and destroying the new one.");
			UnityEngine.Object.Destroy(this);
			return;
		}
		instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		Login();
		Invoke("StopListeningToLogMsgs", 5f);
	}

	private void Login()
	{
		m_loginAttempts++;
		ZLog.Log($"Sending PlayFab login request (attempt {m_loginAttempts})");
		if (m_customId != null)
		{
			LoginWithCustomId();
		}
		else
		{
			ZLog.Log("Login postponed until ID has been set.");
		}
	}

	private void LoginWithCustomId()
	{
		if (m_loginState == LoginState.NotLoggedIn || m_loginState == LoginState.WaitingForRetry)
		{
			m_loginState = LoginState.AttemptingLogin;
			PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
			{
				CustomId = m_customId,
				CreateAccount = true
			}, OnLoginSuccess, OnLoginFailure);
			return;
		}
		ZLog.LogError("Tried to log in while in the " + m_loginState.ToString() + " state! Can only log in when in the " + LoginState.NotLoggedIn.ToString() + " or " + LoginState.WaitingForRetry.ToString() + " state!");
	}

	public void OnLoginSuccess(LoginResult result)
	{
		if (IsPlayFab(m_customId) && !IsLoggedIn)
		{
			PrivilegeData privilegeData = default(PrivilegeData);
			privilegeData.platformCanAccess = delegate(PrivilegeManager.Permission permission, PrivilegeManager.User targetSteamId, CanAccessResult canAccessCb)
			{
				canAccessCb(PrivilegeManager.Result.Allowed);
			};
			privilegeData.platformUserId = Convert.ToUInt64(result.EntityToken.Entity.Id, 16);
			privilegeData.canAccessOnlineMultiplayer = true;
			privilegeData.canViewUserGeneratedContentAll = true;
			privilegeData.canCrossplay = true;
			PrivilegeManager.SetPrivilegeData(privilegeData);
		}
		Entity = result.EntityToken.Entity;
		m_entityToken = result.EntityToken.EntityToken;
		m_tokenExpiration = result.EntityToken.TokenExpiration;
		if (!m_tokenExpiration.HasValue)
		{
			ZLog.LogError("Token expiration time was null!");
			m_loginState = LoginState.LoggedIn;
			return;
		}
		m_refreshThresh = (float)(m_tokenExpiration.Value - DateTime.UtcNow).TotalSeconds / 2f;
		if (IsLoggedIn)
		{
			ZLog.Log($"PlayFab local entity ID {Entity.Id} lifetime extended ");
			this.LoginFinished?.Invoke(LoginType.Refresh);
		}
		else
		{
			if (m_customId != null)
			{
				ZLog.Log($"PlayFab logged in as \"{m_customId}\"");
			}
			ZLog.Log("PlayFab local entity ID is " + Entity.Id);
			m_loginState = LoginState.LoggedIn;
			this.LoginFinished?.Invoke(LoginType.Success);
		}
		if (m_updateEntityTokenCoroutine == null)
		{
			m_updateEntityTokenCoroutine = StartCoroutine(UpdateEntityTokenCoroutine());
		}
		ZPlayFabMatchmaking.OnLogin();
		static bool IsPlayFab(string id)
		{
			if (m_customId != null)
			{
				return id.StartsWith(PrivilegeManager.GetPlatformPrefix(PrivilegeManager.Platform.PlayFab));
			}
			return false;
		}
	}

	public void OnLoginFailure(PlayFabError error)
	{
		ZLog.LogError(error.GenerateErrorReport());
		RetryLoginAfterDelay(GetRetryDelay(m_loginAttempts));
	}

	private float GetRetryDelay(int attemptCount)
	{
		return Mathf.Min(1f * Mathf.Pow(2f, attemptCount - 1), 30f) * UnityEngine.Random.Range(0.875f, 1.125f);
	}

	private void RetryLoginAfterDelay(float delay)
	{
		m_loginState = LoginState.WaitingForRetry;
		ZLog.Log($"Retrying login in {delay}s");
		StartCoroutine(DelayThenLoginCoroutine(delay));
		IEnumerator DelayThenLoginCoroutine(float delay)
		{
			ZLog.Log($"PlayFab login failed! Retrying in {delay}s, total attempts: {m_loginAttempts}");
			NextRetryUtc = DateTime.UtcNow + TimeSpan.FromSeconds(delay);
			while (DateTime.UtcNow < NextRetryUtc)
			{
				yield return null;
			}
			Login();
		}
	}

	private IEnumerator UpdateEntityTokenCoroutine()
	{
		while (true)
		{
			yield return new WaitForSecondsRealtime(420f);
			ZLog.Log("Update PlayFab entity token");
			PlayFabMultiplayerManager.Get().UpdateEntityToken(m_entityToken);
			if (!m_tokenExpiration.HasValue)
			{
				break;
			}
			if ((float)(m_tokenExpiration.Value - DateTime.UtcNow).TotalSeconds <= m_refreshThresh)
			{
				ZLog.Log("Renew PlayFab entity token");
				m_refreshThresh /= 1.5f;
				if (m_customId != null)
				{
					PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
					{
						CustomId = m_customId
					}, OnLoginSuccess, OnLoginFailure);
				}
			}
			yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(420f, 840f));
		}
		ZLog.LogError("Token expiration time was null!");
		m_updateEntityTokenCoroutine = null;
	}

	private static void HandleLog(string logString, string stackTrace, LogType type)
	{
		if (type == LogType.Exception && logString.ToLower().Contains("DllNotFoundException: Party", StringComparison.InvariantCultureIgnoreCase))
		{
			ZLog.LogError("DLL Not Found: This error usually occurs when you do not have the correct dependencies installed, and will prevent crossplay from working. The dependencies are different depending on which platform you play on.\n For windows: You need VC++ Redistributables. https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170Linux: You need Pulse Audio. https://learn.microsoft.com/it-it/gaming/playfab/features/multiplayer/networking/linux-specific-requirementsSteam deck: Try using Proton Compatability Layer.Other platforms: If the issue persists, please report it as a bug.");
			UnityEngine.Object.FindObjectOfType<PlayFabManager>().WaitForPopupEnabled();
		}
	}

	private void StopListeningToLogMsgs()
	{
		Application.logMessageReceived -= HandleLog;
	}

	private void WaitForPopupEnabled()
	{
		if (UnifiedPopup.IsAvailable())
		{
			DelayedVCRedistWarningPopup();
		}
		else
		{
			UnifiedPopup.OnPopupEnabled += DelayedVCRedistWarningPopup;
		}
	}

	private void DelayedVCRedistWarningPopup()
	{
		string playFabErrorBodyText = GetPlayFabErrorBodyText();
		UnifiedPopup.Push(new WarningPopup("$playfab_couldnotloadplayfabparty_header", playFabErrorBodyText, delegate
		{
			UnifiedPopup.Pop();
		}));
		UnifiedPopup.OnPopupEnabled -= DelayedVCRedistWarningPopup;
		Application.logMessageReceived -= HandleLog;
	}

	private string GetPlayFabErrorBodyText()
	{
		if (Settings.IsSteamRunningOnSteamDeck())
		{
			return "$playfab_couldnotloadplayfabparty_text_linux_steamdeck";
		}
		if (!Settings.IsSteamRunningOnSteamDeck() && (Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.WindowsServer || Application.platform == RuntimePlatform.WindowsEditor))
		{
			return "$playfab_couldnotloadplayfabparty_text_linux";
		}
		if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsServer || Application.platform == RuntimePlatform.WindowsEditor)
		{
			return "$playfab_couldnotloadplayfabparty_text_windows";
		}
		return "$playfab_couldnotloadplayfabparty_text_otherplatforms";
	}

	public void LoginFailed()
	{
		RetryLoginAfterDelay(GetRetryDelay(m_loginAttempts));
	}

	private void Update()
	{
		ZPlayFabMatchmaking.instance?.Update(Time.unscaledDeltaTime);
	}
}
