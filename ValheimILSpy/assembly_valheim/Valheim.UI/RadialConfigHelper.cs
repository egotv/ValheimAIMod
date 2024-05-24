using UnityEngine;

namespace Valheim.UI;

public static class RadialConfigHelper
{
	public static bool MouseMode => true;

	public static void SetXYControls(this DynamicRadialMenu radial)
	{
		radial.GetXY = (Vector2 last) => new Vector2(ZInput.GetAxis("JoyAxis 4"), 0f - ZInput.GetAxis("JoyAxis 5")) + GetMousePosition(last);
	}

	private static Vector2 GetMousePosition(Vector2 lastState)
	{
		Vector2 result = Vector2.zero;
		if (!ZInput.IsGamepadActive())
		{
			if (MouseMode)
			{
				Vector2 mouseDelta = ZInput.GetMouseDelta();
				if (mouseDelta.magnitude > 0.1f)
				{
					result = lastState + mouseDelta;
					result.Normalize();
				}
				else
				{
					result = Vector2.Lerp(lastState, Vector2.zero, Hud.instance.m_radialMenu.Inertia * 0.1f);
				}
			}
			else
			{
				result = Camera.main.ScreenToViewportPoint(Input.mousePosition);
				result -= new Vector2(0.5f, 0.5f);
				result *= 2f;
				result.x *= Camera.main.aspect;
			}
		}
		return result;
	}

	public static void SetPaginationControls(this DynamicRadialMenu radial)
	{
		radial.GetPrevious = () => ZInput.GetButtonUp("JoyDPadLeft") || ZInput.GetKeyDown(KeyCode.LeftArrow) || ZInput.GetMouseScrollWheel() < -0.01f;
		radial.GetNext = () => ZInput.GetButtonUp("JoyDPadRight") || ZInput.GetKeyDown(KeyCode.RightArrow) || ZInput.GetMouseScrollWheel() > 0.01f;
	}

	public static void SetItemInteractionControls(this DynamicRadialMenu radial)
	{
		radial.GetSecondaryConfirm = () => (ZInput.GetButtonLastPressedTimer("JoyRadialInteract") > 0.33f && ZInput.GetButtonUp("JoyRadialInteract")) || ZInput.GetMouseButtonUp(2);
		radial.GetConfirm = () => (ZInput.GetButtonLastPressedTimer("JoyRadialInteract") < 0.33f && ZInput.GetButtonUp("JoyRadialInteract")) || ZInput.GetMouseButtonUp(0);
		radial.GetBack = () => ZInput.GetButtonUp("JoyRadialBack") || ZInput.GetMouseButtonUp(1);
	}

	public static void SetClose(this DynamicRadialMenu radial, bool useBackButtonToClose)
	{
		radial.GetClose = () => ZInput.GetButtonUp("JoyRadialClose") || ZInput.GetButtonDown("JoyRadial") || (useBackButtonToClose && ZInput.GetButtonDown("JoyRadialBack")) || (useBackButtonToClose && ZInput.GetMouseButtonDown(1)) || ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetKeyDown(KeyCode.BackQuote);
	}
}
