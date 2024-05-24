namespace Valheim.SettingsGui;

public static class ConsoleGraphicsQualityModeHelper
{
	public static int ToInt(this GraphicsQualityMode mode)
	{
		return (int)mode;
	}

	public static GraphicsQualityMode ToGraphicQualityMode(this int mode)
	{
		return (GraphicsQualityMode)mode;
	}
}
