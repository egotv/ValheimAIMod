public static class OnlineBackendTypeExtentions
{
	public static string ConvertToString(this OnlineBackendType backend)
	{
		return backend switch
		{
			OnlineBackendType.Steamworks => "steamworks", 
			OnlineBackendType.EOS => "eos", 
			OnlineBackendType.PlayFab => "playfab", 
			OnlineBackendType.CustomSocket => "socket", 
			_ => "none", 
		};
	}

	public static OnlineBackendType ConvertFromString(string backend)
	{
		return backend switch
		{
			"steamworks" => OnlineBackendType.Steamworks, 
			"eos" => OnlineBackendType.EOS, 
			"playfab" => OnlineBackendType.PlayFab, 
			"socket" => OnlineBackendType.CustomSocket, 
			_ => OnlineBackendType.None, 
		};
	}
}
