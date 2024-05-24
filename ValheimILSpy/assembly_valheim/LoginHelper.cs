public static class LoginHelper
{
	public static bool IsDone;

	public static event OnLoginDoneCallback OnLoginDone;

	public static void SetDone()
	{
		IsDone = true;
		LoginHelper.OnLoginDone?.Invoke();
	}
}
