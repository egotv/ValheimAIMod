using System;
using UnityEngine;

namespace Valheim.SettingsGui;

public class GamepadMapController : MonoBehaviour
{
	[SerializeField]
	private GamepadMap xboxMapPrefab;

	[SerializeField]
	private GamepadMap psMapPrefab;

	[SerializeField]
	private GamepadMap steamDeckXboxMapPrefab;

	[SerializeField]
	private GamepadMap steamDeckPSMapPrefab;

	[SerializeField]
	private RectTransform root;

	private GamepadMap xboxMapInstance;

	private GamepadMap psMapInstance;

	private GamepadMap steamDeckXboxMapInstance;

	private GamepadMap steamDeckPSMapInstance;

	private GamepadMapType visibleType;

	private InputLayout visibleLayout;

	public InputLayout VisibleLayout => visibleLayout;

	private void Start()
	{
		Localization.OnLanguageChange = (Action)Delegate.Combine(Localization.OnLanguageChange, new Action(OnLanguageChange));
	}

	private void OnDestroy()
	{
		Localization.OnLanguageChange = (Action)Delegate.Remove(Localization.OnLanguageChange, new Action(OnLanguageChange));
	}

	public void Show(InputLayout layout, GamepadMapType type = GamepadMapType.Default)
	{
		visibleType = type;
		visibleLayout = layout;
		switch (type)
		{
		case GamepadMapType.PS:
			if (psMapInstance == null)
			{
				psMapInstance = UnityEngine.Object.Instantiate(psMapPrefab, root);
			}
			break;
		case GamepadMapType.SteamXbox:
			if (steamDeckXboxMapInstance == null)
			{
				steamDeckXboxMapInstance = UnityEngine.Object.Instantiate(steamDeckXboxMapPrefab, root);
			}
			break;
		case GamepadMapType.SteamPS:
			if (steamDeckPSMapInstance == null)
			{
				steamDeckPSMapInstance = UnityEngine.Object.Instantiate(steamDeckPSMapPrefab, root);
			}
			break;
		default:
			if (xboxMapInstance == null)
			{
				xboxMapInstance = UnityEngine.Object.Instantiate(xboxMapPrefab, root);
			}
			break;
		}
		UpdateGamepadMap();
	}

	private void OnLanguageChange()
	{
		UpdateGamepadMap();
	}

	private void UpdateGamepadMap()
	{
		if (psMapInstance != null)
		{
			psMapInstance.gameObject.SetActive(visibleType == GamepadMapType.PS);
			if (visibleType == GamepadMapType.PS)
			{
				psMapInstance.UpdateMap(visibleLayout);
			}
		}
		if (steamDeckXboxMapInstance != null)
		{
			steamDeckXboxMapInstance.gameObject.SetActive(visibleType == GamepadMapType.SteamXbox);
			if (visibleType == GamepadMapType.SteamXbox)
			{
				steamDeckXboxMapInstance.UpdateMap(visibleLayout);
			}
		}
		if (steamDeckPSMapInstance != null)
		{
			steamDeckPSMapInstance.gameObject.SetActive(visibleType == GamepadMapType.SteamPS);
			if (visibleType == GamepadMapType.SteamPS)
			{
				steamDeckPSMapInstance.UpdateMap(visibleLayout);
			}
		}
		if (xboxMapInstance != null)
		{
			xboxMapInstance.gameObject.SetActive(visibleType == GamepadMapType.Default);
			if (visibleType == GamepadMapType.Default)
			{
				xboxMapInstance.UpdateMap(visibleLayout);
			}
		}
	}

	public static string GetLayoutStringId(InputLayout layout)
	{
		return layout switch
		{
			InputLayout.Default => "$settings_controller_classic", 
			InputLayout.Alternative2 => "$settings_controller_default 2", 
			_ => "$settings_controller_default", 
		};
	}

	public static InputLayout NextLayout(InputLayout mode)
	{
		if (mode + 1 < InputLayout.Count)
		{
			return mode + 1;
		}
		return InputLayout.Default;
	}

	public static InputLayout PrevLayout(InputLayout mode)
	{
		if (mode - 1 >= InputLayout.Default)
		{
			return mode - 1;
		}
		return InputLayout.Alternative2;
	}

	public static GamepadMapType GetType(bool alternativeGlyphs = false, bool steamDeck = false)
	{
		if (!alternativeGlyphs)
		{
			if (!steamDeck)
			{
				return GamepadMapType.Default;
			}
			return GamepadMapType.SteamXbox;
		}
		if (!steamDeck)
		{
			return GamepadMapType.PS;
		}
		return GamepadMapType.SteamPS;
	}
}
