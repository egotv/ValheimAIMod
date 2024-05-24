using TMPro;
using UnityEngine;

namespace Valheim.SettingsGui;

public class GamepadMap : MonoBehaviour
{
	[Header("Face Buttons")]
	[SerializeField]
	private GamepadMapLabel joyButton0;

	[SerializeField]
	private GamepadMapLabel joyButton1;

	[SerializeField]
	private GamepadMapLabel joyButton2;

	[SerializeField]
	private GamepadMapLabel joyButton3;

	[Header("Bumpers")]
	[SerializeField]
	private GamepadMapLabel joyButton4;

	[SerializeField]
	private GamepadMapLabel joyButton5;

	[Header("Center")]
	[SerializeField]
	private GamepadMapLabel joyButton6;

	[SerializeField]
	private GamepadMapLabel joyButton7;

	[Header("Triggers")]
	[SerializeField]
	private GamepadMapLabel joyAxis9;

	[SerializeField]
	private GamepadMapLabel joyAxis10;

	[SerializeField]
	private GamepadMapLabel joyAxis9And10;

	[Header("Sticks")]
	[SerializeField]
	private GamepadMapLabel joyButton8;

	[SerializeField]
	private GamepadMapLabel joyButton9;

	[SerializeField]
	private GamepadMapLabel joyAxis1And2;

	[SerializeField]
	private GamepadMapLabel joyAxis4And5;

	[Header("Dpad")]
	[SerializeField]
	private GamepadMapLabel joyAxis6And7;

	[SerializeField]
	private GamepadMapLabel joyAxis6Left;

	[SerializeField]
	private GamepadMapLabel joyAxis6Right;

	[SerializeField]
	private GamepadMapLabel joyAxis6LeftRight;

	[SerializeField]
	private GamepadMapLabel joyAxis7Up;

	[SerializeField]
	private GamepadMapLabel joyAxis7Down;

	[SerializeField]
	private TextMeshProUGUI alternateButtonLabel;

	public void UpdateMap(InputLayout layout)
	{
		joyButton0.Label.text = GetText(GamepadInput.FaceButtonA);
		joyButton1.Label.text = GetText(GamepadInput.FaceButtonB);
		joyButton2.Label.text = GetText(GamepadInput.FaceButtonX);
		joyButton3.Label.text = GetText(GamepadInput.FaceButtonY);
		joyButton4.Label.text = GetText(GamepadInput.BumperL);
		joyButton5.Label.text = GetText(GamepadInput.BumperR);
		joyButton6.Label.text = GetText(GamepadInput.Select);
		joyButton7.Label.text = GetText(GamepadInput.Start);
		joyAxis9.Label.text = GetText(GamepadInput.TriggerL);
		joyAxis10.Label.text = GetText(GamepadInput.TriggerR);
		joyAxis9And10.gameObject.SetActive(layout == InputLayout.Alternative1 || layout == InputLayout.Alternative2);
		joyAxis9And10.Label.text = Localization.instance.Localize("$settings_gp");
		joyAxis1And2.Label.text = Localization.instance.Localize("$settings_move");
		joyAxis4And5.Label.text = Localization.instance.Localize("$settings_look");
		joyButton8.Label.text = GetText(GamepadInput.StickLButton);
		joyButton9.Label.text = GetText(GamepadInput.StickRButton);
		joyAxis6LeftRight.Label.text = GetText(GamepadInput.DPadRight);
		joyAxis7Up.Label.text = GetText(GamepadInput.DPadUp);
		joyAxis7Down.Label.text = GetText(GamepadInput.DPadDown);
		alternateButtonLabel.text = Localization.instance.Localize("$alternate_key_label ") + ZInput.instance.GetBoundKeyString("JoyAltKeys");
	}

	private static string GetText(GamepadInput gamepadInput, FloatRange? floatRange = null)
	{
		string boundActionString = ZInput.instance.GetBoundActionString(gamepadInput, floatRange);
		return Localization.instance.Localize(boundActionString);
	}

	private static string GetText(KeyCode keyboardKey)
	{
		string boundActionString = ZInput.instance.GetBoundActionString(keyboardKey);
		return Localization.instance.Localize(boundActionString);
	}
}
