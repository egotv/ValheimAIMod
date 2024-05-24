using System.Text;
using PlayFab;
using PlayFab.ClientModels;
using Steamworks;

public static class PlayFabAuthWithSteam
{
	private static CallResult<EncryptedAppTicketResponse_t> OnEncryptedAppTicketResponseCallResult = CallResult<EncryptedAppTicketResponse_t>.Create(OnEncryptedAppTicketResponse);

	private static CallResult<GetTicketForWebApiResponse_t> OnGetTicketForWebApiResponseCallResult = CallResult<GetTicketForWebApiResponse_t>.Create(OnGetTicketForWebApiResponse);

	private static void OnEncryptedAppTicketResponse(EncryptedAppTicketResponse_t param, bool bIOFailure)
	{
		if (bIOFailure)
		{
			ZLog.LogError("OnEncryptedAppTicketResponse: Failed to get Steam encrypted app ticket - IO Failure");
		}
		else if (param.m_eResult != EResult.k_EResultOK && param.m_eResult != EResult.k_EResultLimitExceeded && param.m_eResult != EResult.k_EResultDuplicateRequest)
		{
			ZLog.LogError("OnEncryptedAppTicketResponse: Failed to get Steam encrypted app ticket - " + param.m_eResult);
		}
		else
		{
			GetSteamAuthTicket();
		}
	}

	private static void OnGetTicketForWebApiResponse(GetTicketForWebApiResponse_t param, bool bIOFailure)
	{
		if (bIOFailure)
		{
			ZLog.LogError("OnEncryptedAppTicketResponse: Failed to get Steam encrypted app ticket - IO Failure");
			return;
		}
		ZLog.Log($"PlayFab Steam auth using ticket {param.m_hAuthTicket} of length {param.m_cubTicket}");
		StringBuilder stringBuilder = new StringBuilder();
		byte[] rgubTicket = param.m_rgubTicket;
		foreach (byte b in rgubTicket)
		{
			stringBuilder.AppendFormat("{0:x2}", b);
		}
		string steamTicket = stringBuilder.ToString();
		PlayFabClientAPI.LoginWithSteam(new LoginWithSteamRequest
		{
			CreateAccount = true,
			SteamTicket = steamTicket
		}, OnSteamLoginSuccess, OnSteamLoginFailed);
	}

	public static void GetSteamAuthTicket()
	{
		SteamUser.GetAuthTicketForWebApi(null);
	}

	private static void OnSteamLoginFailed(PlayFabError error)
	{
		ZLog.LogError("Failed to logged in PlayFab user via Steam encrypted app ticket: " + error.GenerateErrorReport());
	}

	private static void OnSteamLoginSuccess(LoginResult result)
	{
		ZLog.Log("Logged in PlayFab user via Steam encrypted app ticket");
	}

	public static void Login()
	{
		SteamAPICall_t hAPICall = SteamUser.RequestEncryptedAppTicket(null, 0);
		OnEncryptedAppTicketResponseCallResult.Set(hAPICall);
	}
}
