using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;

public class PlatformGamepadDefinition
{
	public delegate bool CheckGamepadConnectedFunc();

	public delegate GamepadType CheckConnectedGamepadTypeFunc(GamepadType externalType);

	public readonly GamepadType m_gamepadType;

	private Dictionary<GamepadInput, InputDefinition> m_inputDefinitions = new Dictionary<GamepadInput, InputDefinition>();

	private CheckGamepadConnectedFunc m_checkGamepadConnectedFunc;

	private CheckConnectedGamepadTypeFunc m_checkConnectedGamepadTypeFunc;

	public bool IsGamepadConnected
	{
		get
		{
			if (m_checkGamepadConnectedFunc != null)
			{
				return m_checkGamepadConnectedFunc();
			}
			return true;
		}
	}

	private PlatformGamepadDefinition(GamepadType gamepad)
	{
		m_gamepadType = gamepad;
	}

	public GamepadType GetConnectedGamepadType()
	{
		if (!IsGamepadConnected)
		{
			return GamepadType.None;
		}
		if (m_checkConnectedGamepadTypeFunc != null)
		{
			return m_checkConnectedGamepadTypeFunc(m_gamepadType);
		}
		return m_gamepadType;
	}

	public void SetGamepadConnectedCheck(CheckGamepadConnectedFunc checkGamepadConnectedFunc)
	{
		m_checkGamepadConnectedFunc = checkGamepadConnectedFunc;
	}

	public void SetGamepadTypeCheck(CheckConnectedGamepadTypeFunc checkConnectedGamepadTypeFunc)
	{
		m_checkConnectedGamepadTypeFunc = checkConnectedGamepadTypeFunc;
	}

	public void AddDefinition(GamepadInput gamepadInput, AdvancedInputMap advancedMap)
	{
		m_inputDefinitions.Add(gamepadInput, new InputDefinition(advancedMap, this));
	}

	public void AddDefinition(GamepadInput gamepadInput, string axis)
	{
		m_inputDefinitions.Add(gamepadInput, new InputDefinition(axis, this));
	}

	public void AddDefinition(GamepadInput gamepadInput, string axis, FloatRange axisRange, FloatRange outputRange)
	{
		m_inputDefinitions.Add(gamepadInput, new InputDefinition(axis, axisRange, outputRange, this));
	}

	public void AddDefinition(GamepadInput gamepadInput, KeyCode button)
	{
		m_inputDefinitions.Add(gamepadInput, new InputDefinition(button, this));
	}

	public void AddDefinition(GamepadInput gamepadInput, KeyCode button, FloatRange outputRange)
	{
		m_inputDefinitions.Add(gamepadInput, new InputDefinition(button, outputRange, this));
	}

	public InputDefinition GetDefinition(GamepadInput gamepadInput)
	{
		return m_inputDefinitions[gamepadInput];
	}

	public static PlatformGamepadDefinition Get(GamepadType gamepad)
	{
		PlatformGamepadDefinition platformGamepadDefinition = new PlatformGamepadDefinition(gamepad);
		if (gamepad == GamepadType.NewInputSystem)
		{
			platformGamepadDefinition.SetGamepadConnectedCheck(() => Gamepad.current != null);
			platformGamepadDefinition.SetGamepadTypeCheck(delegate(GamepadType externtalType)
			{
				Gamepad current = Gamepad.current;
				if (current is XInputController)
				{
					return GamepadType.XInput;
				}
				if (current is DualShockGamepad)
				{
					return GamepadType.PlayStation;
				}
				if (current is DualSenseGamepadHID)
				{
					return GamepadType.PlayStation;
				}
				return (current is SwitchProControllerHID) ? GamepadType.SwitchPro : externtalType;
			});
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadLeft, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.dpad.left.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadRight, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.dpad.right.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadDown, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.dpad.down.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadUp, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.dpad.up.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonA, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.aButton.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonB, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.bButton.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonX, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.xButton.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonY, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.yButton.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLHorizontal, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.leftStick.ReadUnprocessedValue().x));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLVertical, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.leftStick.ReadUnprocessedValue().y));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLButton, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.leftStickButton.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRHorizontal, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.rightStick.ReadUnprocessedValue().x));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRVertical, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.rightStick.ReadUnprocessedValue().y));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRButton, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.rightStickButton.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.BumperL, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.leftShoulder.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.BumperR, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.rightShoulder.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.TriggerL, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.leftTrigger.ReadUnprocessedValue()));
			platformGamepadDefinition.AddDefinition(GamepadInput.TriggerR, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.rightTrigger.ReadUnprocessedValue()));
			platformGamepadDefinition.AddDefinition(GamepadInput.Select, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.selectButton.isPressed));
			platformGamepadDefinition.AddDefinition(GamepadInput.Start, new AdvancedInputMap((InputDefinition[] _) => Gamepad.current.startButton.isPressed));
			return platformGamepadDefinition;
		}
		platformGamepadDefinition.SetGamepadConnectedCheck(() => Gamepad.current != null);
		platformGamepadDefinition.SetGamepadTypeCheck((GamepadType externtalType) => externtalType);
		switch (gamepad)
		{
		case GamepadType.Generic:
		case GamepadType.XInput:
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadLeft, "JoyAxis 6", new FloatRange(0f, -1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadRight, "JoyAxis 6", new FloatRange(0f, 1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadDown, "JoyAxis 7", new FloatRange(0f, -1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadUp, "JoyAxis 7", new FloatRange(0f, 1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonA, KeyCode.JoystickButton0);
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonB, KeyCode.JoystickButton1);
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonX, KeyCode.JoystickButton2);
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonY, KeyCode.JoystickButton3);
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLHorizontal, "JoyAxis 1", new FloatRange(-1f, 1f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLVertical, "JoyAxis 2", new FloatRange(1f, -1f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLButton, KeyCode.JoystickButton8);
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRHorizontal, "JoyAxis 4", new FloatRange(-1f, 1f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRVertical, "JoyAxis 5", new FloatRange(1f, -1f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRButton, KeyCode.JoystickButton9);
			platformGamepadDefinition.AddDefinition(GamepadInput.BumperL, KeyCode.JoystickButton4);
			platformGamepadDefinition.AddDefinition(GamepadInput.BumperR, KeyCode.JoystickButton5);
			platformGamepadDefinition.AddDefinition(GamepadInput.TriggerL, "JoyAxis 3", new FloatRange(0f, -1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.TriggerR, "JoyAxis 3", new FloatRange(0f, 1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.Select, KeyCode.JoystickButton6);
			platformGamepadDefinition.AddDefinition(GamepadInput.Start, KeyCode.JoystickButton7);
			break;
		case GamepadType.PlayStation:
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadLeft, "JoyAxis 7", new FloatRange(0f, -1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadRight, "JoyAxis 7", new FloatRange(0f, 1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadDown, "JoyAxis 8", new FloatRange(0f, -1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadUp, "JoyAxis 8", new FloatRange(0f, 1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonA, KeyCode.JoystickButton1);
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonB, KeyCode.JoystickButton2);
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonX, KeyCode.JoystickButton0);
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonY, KeyCode.JoystickButton3);
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLHorizontal, "JoyAxis 1", new FloatRange(-1f, 1f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLVertical, "JoyAxis 2", new FloatRange(1f, -1f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLButton, KeyCode.JoystickButton10);
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRHorizontal, "JoyAxis 3", new FloatRange(-1f, 1f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRVertical, "JoyAxis 6", new FloatRange(1f, -1f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRButton, KeyCode.JoystickButton11);
			platformGamepadDefinition.AddDefinition(GamepadInput.BumperL, KeyCode.JoystickButton4);
			platformGamepadDefinition.AddDefinition(GamepadInput.BumperR, KeyCode.JoystickButton5);
			platformGamepadDefinition.AddDefinition(GamepadInput.TriggerL, "JoyAxis 4", new FloatRange(-1f, 1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.TriggerR, "JoyAxis 5", new FloatRange(-1f, 1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.Select, KeyCode.JoystickButton8);
			platformGamepadDefinition.AddDefinition(GamepadInput.Start, KeyCode.JoystickButton9);
			break;
		case GamepadType.SwitchPro:
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadLeft, "JoyAxis 9", new FloatRange(0f, -1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadRight, "JoyAxis 9", new FloatRange(0f, 1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadDown, "JoyAxis 10", new FloatRange(0f, -1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.DPadUp, "JoyAxis 10", new FloatRange(0f, 1f), new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonA, KeyCode.JoystickButton1);
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonB, KeyCode.JoystickButton0);
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonX, KeyCode.JoystickButton3);
			platformGamepadDefinition.AddDefinition(GamepadInput.FaceButtonY, KeyCode.JoystickButton2);
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLHorizontal, "JoyAxis 2", new FloatRange(-0.6f, 0.6f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLVertical, "JoyAxis 4", new FloatRange(0.6f, -0.6f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickLButton, KeyCode.JoystickButton10);
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRHorizontal, "JoyAxis 7", new FloatRange(-0.6f, 0.6f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRVertical, "JoyAxis 8", new FloatRange(0.6f, -0.6f), new FloatRange(-1f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.StickRButton, KeyCode.JoystickButton11);
			platformGamepadDefinition.AddDefinition(GamepadInput.BumperL, KeyCode.JoystickButton4);
			platformGamepadDefinition.AddDefinition(GamepadInput.BumperR, KeyCode.JoystickButton5);
			platformGamepadDefinition.AddDefinition(GamepadInput.TriggerL, KeyCode.JoystickButton6, new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.TriggerR, KeyCode.JoystickButton7, new FloatRange(0f, 1f));
			platformGamepadDefinition.AddDefinition(GamepadInput.Select, KeyCode.JoystickButton8);
			platformGamepadDefinition.AddDefinition(GamepadInput.Start, KeyCode.JoystickButton9);
			break;
		default:
			ZLog.LogError($"Gamepad support not implemented for gamepad {gamepad} on Windows");
			return null;
		}
		return platformGamepadDefinition;
	}
}
