public static class PlayFabAttrKeyExtension
{
	public static string ToKeyString(this PlayFabAttrKey key)
	{
		return key switch
		{
			PlayFabAttrKey.HavePassword => "PASSWORD", 
			PlayFabAttrKey.NetworkId => "NETWORK", 
			PlayFabAttrKey.WorldName => "WORLD", 
			_ => null, 
		};
	}
}
