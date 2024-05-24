public struct PrivilegeData
{
	public ulong platformUserId;

	public bool canAccessOnlineMultiplayer;

	public bool canViewUserGeneratedContentAll;

	public bool canCrossplay;

	public CanAccessCallback platformCanAccess;
}
