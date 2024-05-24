using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Valheim.SettingsGui;

public class ZInput
{
	[Flags]
	public enum InputSource
	{
		None = 0,
		KeyboardMouse = 0xAA,
		Gamepad = 0xB4,
		AutomaticBlocking = 0xDF,
		AutomaticNonBlocking = 0xDE,
		KeyboardMouseOnly = 0xCB,
		GamepadOnly = 0xD5,
		BothKeyboardMouseHints = 0xCE,
		BothGamepadHints = 0xD6,
		BlockingBit = 1,
		AllowKBMInputBit = 2,
		AllowGamepadInputBit = 4,
		AllowKBMHintsBit = 8,
		AllowGamepadHintsBit = 0x10,
		InputSourceBit = 0x20,
		InputSwitchingModeBit = 0x40,
		ValidBit = 0x80,
		AllowedInputMask = 6,
		AllowedHintsMask = 0x18
	}

	public class ButtonDef
	{
		public string m_name;

		public bool m_showHints = true;

		public Key m_key;

		public MouseButton m_mouseButton = MouseButton.Back;

		public GamepadInput m_gamepadInput;

		public FloatRange? m_mappedRange;

		public bool m_bMouseButtonSet;

		public bool m_altKey;

		public bool m_internalPressed;

		public bool m_pressed;

		public bool m_wasPressed;

		public bool m_down;

		public bool m_up;

		public bool m_pressedFixed;

		public bool m_wasPressedFixed;

		public bool m_downFixed;

		public bool m_upFixed;

		public float m_pressedTimer;

		public float m_lastPressedTimer;

		public float m_repeatDelay;

		public float m_repeatInterval;

		private static Dictionary<(GamepadInput, bool), Dictionary<string, string>> m_gamepadSprites = new Dictionary<(GamepadInput, bool), Dictionary<string, string>>
		{
			{
				(GamepadInput.FaceButtonA, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_a" },
					{ "ps5", "button_cross" }
				}
			},
			{
				(GamepadInput.FaceButtonB, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_b" },
					{ "ps5", "button_circle" }
				}
			},
			{
				(GamepadInput.FaceButtonX, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_x" },
					{ "ps5", "button_square" }
				}
			},
			{
				(GamepadInput.FaceButtonY, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_y" },
					{ "ps5", "button_triangle" }
				}
			},
			{
				(GamepadInput.BumperL, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_lb" },
					{ "ps5", "button_l1" }
				}
			},
			{
				(GamepadInput.BumperR, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_rb" },
					{ "ps5", "button_r1" }
				}
			},
			{
				(GamepadInput.Select, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_back" },
					{ "ps5", "button_share" }
				}
			},
			{
				(GamepadInput.Start, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_start" },
					{ "ps5", "button_options" }
				}
			},
			{
				(GamepadInput.StickLButton, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_ls" },
					{ "ps5", "button_l3" }
				}
			},
			{
				(GamepadInput.StickRButton, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_rs" },
					{ "ps5", "button_r3" }
				}
			},
			{
				(GamepadInput.StickLHorizontal, true),
				new Dictionary<string, string>
				{
					{ "xbox", "lstick_left" },
					{ "ps5", "lstick_left" }
				}
			},
			{
				(GamepadInput.StickLHorizontal, false),
				new Dictionary<string, string>
				{
					{ "xbox", "lstick_right" },
					{ "ps5", "lstick_right" }
				}
			},
			{
				(GamepadInput.StickLVertical, false),
				new Dictionary<string, string>
				{
					{ "xbox", "lstick_up" },
					{ "ps5", "lstick_up" }
				}
			},
			{
				(GamepadInput.StickLVertical, true),
				new Dictionary<string, string>
				{
					{ "xbox", "lstick_down" },
					{ "ps5", "lstick_down" }
				}
			},
			{
				(GamepadInput.TriggerL, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_lt" },
					{ "ps5", "button_l2" }
				}
			},
			{
				(GamepadInput.TriggerR, false),
				new Dictionary<string, string>
				{
					{ "xbox", "button_rt" },
					{ "ps5", "button_r2" }
				}
			},
			{
				(GamepadInput.DPadLeft, false),
				new Dictionary<string, string>
				{
					{ "xbox", "dpad_left" },
					{ "ps5", "dpad_left" }
				}
			},
			{
				(GamepadInput.DPadRight, false),
				new Dictionary<string, string>
				{
					{ "xbox", "dpad_right" },
					{ "ps5", "dpad_right" }
				}
			},
			{
				(GamepadInput.DPadDown, false),
				new Dictionary<string, string>
				{
					{ "xbox", "dpad_down" },
					{ "ps5", "dpad_down" }
				}
			},
			{
				(GamepadInput.DPadUp, false),
				new Dictionary<string, string>
				{
					{ "xbox", "dpad_up" },
					{ "ps5", "dpad_up" }
				}
			}
		};

		public static Dictionary<string, Dictionary<string, string>> m_stickSprites = new Dictionary<string, Dictionary<string, string>>
		{
			{
				"DPAD",
				new Dictionary<string, string>
				{
					{ "xbox", "dpad" },
					{ "ps5", "dpad" }
				}
			},
			{
				"LeftStick",
				new Dictionary<string, string>
				{
					{ "xbox", "lstick" },
					{ "ps5", "lstick" }
				}
			},
			{
				"RightStick",
				new Dictionary<string, string>
				{
					{ "xbox", "rstick" },
					{ "ps5", "rstick" }
				}
			}
		};

		public InputSource Source
		{
			get
			{
				if (m_gamepadInput != 0)
				{
					return InputSource.Gamepad;
				}
				return InputSource.KeyboardMouse;
			}
		}

		public ButtonDef(ButtonDef buttonDef)
		{
			m_name = buttonDef.m_name;
			m_gamepadInput = buttonDef.m_gamepadInput;
			m_key = buttonDef.m_key;
			m_bMouseButtonSet = buttonDef.m_bMouseButtonSet;
			m_mouseButton = buttonDef.m_mouseButton;
			m_mappedRange = buttonDef.m_mappedRange;
			m_repeatDelay = buttonDef.m_repeatDelay;
			m_repeatInterval = buttonDef.m_repeatInterval;
			m_showHints = buttonDef.m_showHints;
			m_altKey = buttonDef.m_altKey;
		}

		public ButtonDef(string name, GamepadInput gamepadInput, float repeatDelay, float repeatInterval, bool showHints, bool altKey)
		{
			if (gamepadInput == GamepadInput.None)
			{
				throw new ArgumentException($"Gamepad input can't be {GamepadInput.None}!");
			}
			m_name = name;
			m_gamepadInput = gamepadInput;
			m_key = Key.None;
			m_mouseButton = MouseButton.Back;
			m_mappedRange = null;
			m_repeatDelay = repeatDelay;
			m_repeatInterval = repeatInterval;
			m_showHints = showHints;
			m_altKey = altKey;
		}

		public ButtonDef(string name, GamepadInput gamepadInput, FloatRange mappedRange, float repeatDelay, float repeatInterval, bool showHints, bool altKey)
		{
			if (gamepadInput == GamepadInput.None)
			{
				throw new ArgumentException($"Gamepad input can't be {GamepadInput.None}!");
			}
			m_name = name;
			m_gamepadInput = gamepadInput;
			m_key = Key.None;
			m_mouseButton = MouseButton.Back;
			m_mappedRange = mappedRange;
			m_repeatDelay = repeatDelay;
			m_repeatInterval = repeatInterval;
			m_showHints = showHints;
			m_altKey = altKey;
		}

		public ButtonDef(string name, Key keyboardKey, float repeatDelay, float repeatInterval, bool showHints, bool altKey)
		{
			if (keyboardKey == Key.None)
			{
				throw new ArgumentException($"Keyboard key can't be {KeyCode.None}!");
			}
			m_name = name;
			m_gamepadInput = GamepadInput.None;
			m_key = keyboardKey;
			m_mouseButton = MouseButton.Back;
			m_mappedRange = null;
			m_repeatDelay = repeatDelay;
			m_repeatInterval = repeatInterval;
			m_showHints = showHints;
			m_altKey = altKey;
		}

		public ButtonDef(string name, MouseButton mouseKey, float repeatDelay, float repeatInterval, bool showHints, bool altKey)
		{
			m_name = name;
			m_gamepadInput = GamepadInput.None;
			m_key = Key.None;
			m_mouseButton = mouseKey;
			m_mappedRange = null;
			m_repeatDelay = repeatDelay;
			m_repeatInterval = repeatInterval;
			m_showHints = showHints;
			m_altKey = altKey;
			m_bMouseButtonSet = true;
		}

		public bool TryGetButtonSprite(string controllerPlatform, out string result)
		{
			bool item = m_mappedRange.HasValue && m_mappedRange.Value.IsNegative;
			if (!m_gamepadSprites.ContainsKey((m_gamepadInput, item)) || !m_gamepadSprites[(m_gamepadInput, item)].ContainsKey(controllerPlatform))
			{
				result = string.Empty;
				return false;
			}
			result = "<sprite=\"" + controllerPlatform + "\" name=\"" + m_gamepadSprites[(m_gamepadInput, item)][controllerPlatform] + "\">";
			return true;
		}

		public static bool TryGetStickSprite(string controllerPlatform, string stick, out string result)
		{
			if (!m_stickSprites.ContainsKey(stick) || !m_stickSprites[stick].ContainsKey(controllerPlatform))
			{
				result = string.Empty;
				return false;
			}
			result = "<sprite=\"" + controllerPlatform + "\" name=\"" + m_stickSprites[stick][controllerPlatform] + "\">";
			return true;
		}
	}

	private const string ControllerLayout = "ControllerLayout";

	private const float c_repeatDelay = 0.3f;

	private const float c_repeatInterval = 0.1f;

	private static ZInput m_instance;

	private static bool? s_isInputSwitchingModeValid = null;

	private bool m_mouseInputThisFrame;

	public static InputLayout InputLayout = InputLayout.Default;

	private const float m_stickDeadZone = 0.2f;

	private const float m_gamepadInactiveTimeout = 60f;

	private const float m_axisPressDeadZone = 0.4f;

	private const string m_invertedText = "_inverted";

	private static InputSource s_inputSwitchingMode = InputSource.AutomaticNonBlocking;

	private static InputSource m_inputSource = InputSource.KeyboardMouse;

	private DateTime m_inputTimer = DateTime.Now;

	private DateTime m_inputTimerGamepad = DateTime.Now;

	private DateTime m_inputTimerMouse = DateTime.Now;

	private KeyCode m_startBind;

	public PlatformGamepadDefinition m_definition;

	private Dictionary<string, ButtonDef> m_buttons = new Dictionary<string, ButtonDef>();

	private static ButtonDef m_binding = null;

	public static bool PlayStationGlyphs = false;

	public static bool SwapTriggers = false;

	public static bool ToggleRun = false;

	public static bool ToggleRunState = false;

	private static bool m_virtualKeyboardOpen;

	private static float m_blockGamePadInput;

	public static Func<ZInput> GetZInput;

	private static readonly List<Key> s_keyCodeValues = Enum.GetValues(typeof(Key)).OfType<Key>().ToList();

	public static ZInput instance => m_instance;

	public static Vector3 mousePosition => m_instance.Input_GetMousePosition();

	public static string CompositionString
	{
		get
		{
			BaseInput input = EventSystem.current.currentInputModule.input;
			if (!(input != null))
			{
				return Input.compositionString;
			}
			return input.compositionString;
		}
	}

	public static int CompositionLength => CompositionString.Length;

	public static bool GamepadActive => m_inputSource == InputSource.Gamepad;

	public static bool VirtualKeyboardOpen
	{
		get
		{
			return m_virtualKeyboardOpen;
		}
		set
		{
			if (m_virtualKeyboardOpen && !value)
			{
				m_blockGamePadInput = 0.3f;
			}
			m_virtualKeyboardOpen = value;
		}
	}

	public static event Action OnInputLayoutChanged;

	public static void Initialize()
	{
		if (PlatformPrefs.GetInt("ControllerLayout", -1) >= 0)
		{
			InputLayout = (InputLayout)PlatformPrefs.GetInt("ControllerLayout");
		}
		if (m_instance == null)
		{
			m_instance = new ZInput();
		}
	}

	public ZInput()
	{
		GamepadType gamepadFromCLArgs = GetGamepadFromCLArgs();
		if (gamepadFromCLArgs != 0)
		{
			m_definition = PlatformGamepadDefinition.Get(gamepadFromCLArgs);
		}
		if (m_definition == null)
		{
			gamepadFromCLArgs = GamepadType.NewInputSystem;
			m_definition = PlatformGamepadDefinition.Get(gamepadFromCLArgs);
		}
		Reset();
		Load();
	}

	private GamepadType GetGamepadFromCLArgs()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length - 1; i++)
		{
			if (commandLineArgs[i] == "-gamepad" && Enum.TryParse<GamepadType>(commandLineArgs[i + 1], ignoreCase: true, out var result))
			{
				return result;
			}
		}
		return GamepadType.None;
	}

	private static bool IsInputSwitchingModeValid()
	{
		if (!s_isInputSwitchingModeValid.HasValue)
		{
			s_isInputSwitchingModeValid = Enum.IsDefined(typeof(InputSource), s_inputSwitchingMode) && s_inputSwitchingMode.HasFlag(InputSource.InputSwitchingModeBit | InputSource.ValidBit);
		}
		return s_isInputSwitchingModeValid.Value;
	}

	private static void InvalidateInputSwitchingModeValid()
	{
		s_isInputSwitchingModeValid = null;
	}

	private static bool AcceptInputFromSource(InputSource inputSource)
	{
		if (!IsInputSwitchingModeValid())
		{
			ZLog.LogWarning($"Input switching mode {s_inputSwitchingMode} invalid! Accepting all input!");
			return true;
		}
		if (!s_inputSwitchingMode.HasFlag(InputSource.BlockingBit))
		{
			return true;
		}
		return s_inputSwitchingMode.HasFlag(inputSource & InputSource.AllowedInputMask);
	}

	public static void Update(float dt)
	{
		if (m_instance != null)
		{
			m_instance.InternalUpdate(dt);
		}
	}

	public static void FixedUpdate(float dt)
	{
		if (m_instance != null)
		{
			m_instance.InternalUpdateFixed(dt);
		}
	}

	public static void OnGUI()
	{
		if (m_instance != null)
		{
			m_instance.OnGUIInternal();
		}
	}

	private void InternalUpdate(float dt)
	{
		CheckMouseInput();
		if (m_blockGamePadInput > 0f)
		{
			m_blockGamePadInput -= dt;
			return;
		}
		foreach (ButtonDef value in m_buttons.Values)
		{
			value.m_wasPressed = value.m_pressed;
			bool flag = value.Source switch
			{
				InputSource.KeyboardMouse => value.m_bMouseButtonSet ? GetMouseButtonNew(value.m_mouseButton) : GetKeyNew(value.m_key), 
				InputSource.Gamepad => m_definition.GetDefinition(value.m_gamepadInput).ReadAsDigital(0.4f, value.m_mappedRange) && !m_virtualKeyboardOpen, 
				_ => false, 
			};
			if (!value.m_internalPressed && flag)
			{
				OnInput(value.Source, allowSwitchInputSource: true);
			}
			value.m_internalPressed = flag;
			value.m_pressed = flag && AcceptInputFromSource(value.Source);
			value.m_down = value.m_pressed && !value.m_wasPressed && !m_virtualKeyboardOpen;
			value.m_up = !value.m_pressed && value.m_wasPressed;
			if (value.m_pressed)
			{
				value.m_lastPressedTimer = 0f;
				value.m_pressedTimer += dt;
				if (value.m_repeatDelay > 0f && value.m_pressedTimer > value.m_repeatDelay)
				{
					value.m_down = true;
					value.m_downFixed = true;
					value.m_pressedTimer -= value.m_repeatInterval;
				}
			}
			else
			{
				value.m_lastPressedTimer = value.m_pressedTimer;
				value.m_pressedTimer = 0f;
			}
		}
	}

	private void CheckMouseInput()
	{
		if (GetMouseDelta() != Vector2.zero)
		{
			OnInput(InputSource.KeyboardMouse, allowSwitchInputSource: true);
			m_mouseInputThisFrame = true;
		}
		else
		{
			m_mouseInputThisFrame = false;
		}
	}

	public void OnGUIInternal()
	{
		if (Event.current.isKey && Event.current.keyCode > KeyCode.None && Event.current.keyCode < KeyCode.JoystickButton0)
		{
			OnInput(InputSource.KeyboardMouse, allowSwitchInputSource: true);
		}
	}

	private void OnInput(InputSource inputSource, bool allowSwitchInputSource)
	{
		if (inputSource == InputSource.Gamepad && (m_virtualKeyboardOpen || m_mouseInputThisFrame))
		{
			return;
		}
		if (allowSwitchInputSource && s_inputSwitchingMode.HasFlag(InputSource.AllowedInputMask))
		{
			bool num = m_inputSource != inputSource;
			m_inputSource = inputSource;
			if (num && instance != null)
			{
				ZInput.OnInputLayoutChanged?.Invoke();
			}
		}
		if (m_inputSource == inputSource)
		{
			m_inputTimer = DateTime.Now;
			switch (m_inputSource)
			{
			case InputSource.KeyboardMouse:
				m_inputTimerMouse = m_inputTimer;
				break;
			case InputSource.Gamepad:
				m_inputTimerGamepad = m_inputTimer;
				break;
			}
		}
	}

	public DateTime GetLastInputTimer()
	{
		return m_inputTimer;
	}

	public DateTime GetLastInputTimerGamepad()
	{
		return m_inputTimerGamepad;
	}

	public DateTime GetLastInputTimerMouse()
	{
		return m_inputTimerMouse;
	}

	private void InternalUpdateFixed(float dt)
	{
		foreach (ButtonDef value in m_buttons.Values)
		{
			value.m_wasPressedFixed = value.m_pressedFixed;
			value.m_pressedFixed = value.m_pressed;
			value.m_downFixed = value.m_pressedFixed && !value.m_wasPressedFixed;
			value.m_upFixed = !value.m_pressedFixed && value.m_wasPressedFixed;
		}
	}

	public static bool IsNonClassicFunctionality()
	{
		if (InputLayout != InputLayout.Alternative1)
		{
			return InputLayout == InputLayout.Alternative2;
		}
		return true;
	}

	private void ResetToAlternative1Layout(float repeatDelay, float repeatInterval)
	{
		GamepadInput gamepadInput = (SwapTriggers ? GamepadInput.TriggerR : GamepadInput.TriggerL);
		GamepadInput gamepadInput2 = (SwapTriggers ? GamepadInput.TriggerL : GamepadInput.TriggerR);
		SetGeneralInputMappings(repeatDelay, repeatInterval, gamepadInput, gamepadInput2);
		AddButton("JoyJump", GamepadInput.FaceButtonA);
		AddButton("JoyUse", GamepadInput.FaceButtonX);
		AddButton("JoyInventory", GamepadInput.FaceButtonY);
		AddButton("JoyBlock", GamepadInput.BumperL);
		AddButton("JoyAttack", GamepadInput.BumperR);
		AddButton("JoyHide", gamepadInput);
		AddButton("JoySecondaryAttack", gamepadInput2);
		AddButton("JoyMap", GamepadInput.Select);
		AddButton("JoyChat", GamepadInput.Select, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyMenu", GamepadInput.Start);
		AddButton("JoyToggleHUD", GamepadInput.Start, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyRun", GamepadInput.StickLButton);
		AddButton("JoyCrouch", GamepadInput.StickRButton);
		AddButton("JoyHotbarLeft", GamepadInput.DPadLeft, repeatDelay, repeatInterval);
		AddButton("JoyHotbarRight", GamepadInput.DPadRight, repeatDelay, repeatInterval);
		AddButton("JoyHotbarUse", GamepadInput.DPadUp);
		AddButton("JoySit", GamepadInput.DPadDown);
		if (UISettings.UseRadials)
		{
			AddButton("JoyRadial", gamepadInput);
			AddButton("JoyRadialInteract", GamepadInput.BumperR);
			AddButton("JoyRadialBack", GamepadInput.BumperL);
			AddButton("JoyRadialClose", GamepadInput.FaceButtonB);
		}
		AddButton("JoyAltKeys", GamepadInput.BumperL);
		AddButton("JoyCamZoomIn", GamepadInput.DPadUp, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyCamZoomOut", GamepadInput.DPadDown, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyMapZoomIn", GamepadInput.DPadRight, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyMapZoomOut", GamepadInput.DPadLeft, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyBuildMenu", GamepadInput.FaceButtonB);
		AddButton("JoyPlace", GamepadInput.BumperR);
		AddButton("JoyRemove", GamepadInput.BumperL);
		AddButton("JoyRotate", gamepadInput);
		AddButton("JoyRotateRight", gamepadInput2);
		AddButton("JoyNextSnap", GamepadInput.StickRButton);
		AddButton("JoyPrevSnap", GamepadInput.StickLButton);
		AddButton("JoyAltPlace", GamepadInput.StickRButton, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyDodge", GamepadInput.FaceButtonB, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyTabLeft", GamepadInput.BumperL, 0f, 0f, showHints: false);
		AddButton("JoyTabRight", GamepadInput.BumperR, 0f, 0f, showHints: false);
		AddButton("JoyScrollChatUp", GamepadInput.StickRVertical, new FloatRange(0f, 1f), 0.5f, 0.05f);
		AddButton("JoyScrollChatDown", GamepadInput.StickRVertical, new FloatRange(0f, -1f), 0.5f, 0.05f);
		AddButton("JoyAutoPickup", GamepadInput.StickLButton, 0f, 0f, showHints: true, altKey: true);
	}

	private void ResetToAlternative2Layout(float repeatDelay, float repeatInterval)
	{
		GamepadInput gamepadInput = (SwapTriggers ? GamepadInput.TriggerR : GamepadInput.TriggerL);
		GamepadInput gamepadInput2 = (SwapTriggers ? GamepadInput.TriggerL : GamepadInput.TriggerR);
		SetGeneralInputMappings(repeatDelay, repeatInterval, gamepadInput, gamepadInput2);
		AddButton("JoyJump", GamepadInput.FaceButtonA);
		AddButton("JoyDodge", GamepadInput.FaceButtonB, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyUse", GamepadInput.FaceButtonX);
		AddButton("JoyInventory", GamepadInput.FaceButtonY);
		AddButton("JoyHide", GamepadInput.BumperL);
		AddButton("JoySecondaryAttack", GamepadInput.BumperR);
		AddButton("JoyBlock", gamepadInput);
		AddButton("JoyAttack", gamepadInput2);
		AddButton("JoyMap", GamepadInput.Select);
		AddButton("JoyChat", GamepadInput.Select, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyMenu", GamepadInput.Start);
		AddButton("JoyToggleHUD", GamepadInput.Start, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyRun", GamepadInput.StickLButton);
		AddButton("JoyCrouch", GamepadInput.StickRButton);
		AddButton("JoyHotbarLeft", GamepadInput.DPadLeft, repeatDelay, repeatInterval);
		AddButton("JoyHotbarRight", GamepadInput.DPadRight, repeatDelay, repeatInterval);
		AddButton("JoyHotbarUse", GamepadInput.DPadUp);
		AddButton("JoySit", GamepadInput.DPadDown);
		if (UISettings.UseRadials)
		{
			AddButton("JoyRadial", GamepadInput.BumperL);
			AddButton("JoyRadialInteract", gamepadInput2);
			AddButton("JoyRadialBack", gamepadInput);
			AddButton("JoyRadialClose", GamepadInput.FaceButtonB);
		}
		AddButton("JoyAltKeys", gamepadInput);
		AddButton("JoyCamZoomIn", GamepadInput.DPadUp, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyCamZoomOut", GamepadInput.DPadDown, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyMapZoomIn", GamepadInput.DPadRight, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyMapZoomOut", GamepadInput.DPadLeft, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyBuildMenu", GamepadInput.FaceButtonB);
		AddButton("JoyPlace", GamepadInput.BumperR);
		AddButton("JoyRemove", GamepadInput.BumperL);
		AddButton("JoyRotate", gamepadInput);
		AddButton("JoyRotateRight", gamepadInput2);
		AddButton("JoyNextSnap", GamepadInput.StickRButton);
		AddButton("JoyPrevSnap", GamepadInput.StickLButton);
		AddButton("JoyAltPlace", GamepadInput.StickRButton, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyTabLeft", GamepadInput.BumperL, 0f, 0f, showHints: false);
		AddButton("JoyTabRight", GamepadInput.BumperR, 0f, 0f, showHints: false);
		AddButton("JoyScrollChatUp", GamepadInput.StickRVertical, new FloatRange(0f, 1f), 0.5f, 0.05f);
		AddButton("JoyScrollChatDown", GamepadInput.StickRVertical, new FloatRange(0f, -1f), 0.5f, 0.05f);
		AddButton("JoyAutoPickup", GamepadInput.StickLButton, 0f, 0f, showHints: true, altKey: true);
	}

	public void ChangeLayout(InputLayout inputLayout)
	{
		InputLayout = inputLayout;
		UpdateGamepadInputLayout(0.3f, 0.1f);
		PlatformPrefs.SetInt("ControllerLayout", (int)InputLayout);
		ZInput.OnInputLayoutChanged?.Invoke();
	}

	public void Reset()
	{
		m_buttons.Clear();
		float repeatDelay = 0.3f;
		float repeatInterval = 0.1f;
		AddButton("Attack", MouseButton.Left);
		AddButton("SecondaryAttack", MouseButton.Middle);
		AddButton("Block", MouseButton.Right);
		AddButton("Use", Key.E);
		AddButton("Hide", Key.R);
		AddButton("Jump", Key.Space);
		AddButton("Crouch", Key.LeftCtrl);
		AddButton("Run", Key.LeftShift);
		AddButton("ToggleWalk", Key.C);
		AddButton("AutoRun", Key.Q);
		AddButton("Sit", Key.X);
		AddButton("GP", Key.F);
		AddButton("AltPlace", Key.LeftShift);
		AddButton("CamZoomIn", Key.None);
		AddButton("CamZoomOut", Key.None);
		AddButton("Forward", Key.W, repeatDelay, repeatInterval);
		AddButton("Left", Key.A, repeatDelay, repeatInterval);
		AddButton("Backward", Key.S, repeatDelay, repeatInterval);
		AddButton("Right", Key.D, repeatDelay, repeatInterval);
		AddButton("Inventory", Key.Tab);
		AddButton("Map", Key.M);
		AddButton("MapZoomOut", Key.Comma);
		AddButton("MapZoomIn", Key.Period);
		AddButton("TabLeft", Key.Q);
		AddButton("TabRight", Key.E);
		AddButton("BuildMenu", MouseButton.Right);
		AddButton("Remove", MouseButton.Middle);
		AddButton("AutoPickup", Key.V);
		AddButton("ScrollChatUp", Key.PageUp, 0.5f, 0.05f);
		AddButton("ScrollChatDown", Key.PageDown, 0.5f, 0.05f);
		AddButton("ChatUp", Key.UpArrow, 0.5f, 0.05f);
		AddButton("ChatDown", Key.DownArrow, 0.5f, 0.05f);
		AddButton("Chat", Key.Enter);
		AddButton("Console", Key.F5);
		AddButton("Hotbar1", Key.Digit1);
		AddButton("Hotbar2", Key.Digit2);
		AddButton("Hotbar3", Key.Digit3);
		AddButton("Hotbar4", Key.Digit4);
		AddButton("Hotbar5", Key.Digit5);
		AddButton("Hotbar6", Key.Digit6);
		AddButton("Hotbar7", Key.Digit7);
		AddButton("Hotbar8", Key.Digit8);
		UpdateGamepadInputLayout(repeatDelay, repeatInterval);
	}

	public void ResetTo(ButtonDef btnDef)
	{
		ButtonDef buttonDef = m_buttons[btnDef.m_name];
		if (buttonDef.m_bMouseButtonSet != btnDef.m_bMouseButtonSet || buttonDef.m_key != btnDef.m_key || buttonDef.m_mouseButton != btnDef.m_mouseButton)
		{
			StartBindKey(btnDef.m_name);
			if (btnDef.m_bMouseButtonSet)
			{
				EndBindKey(btnDef.m_mouseButton);
			}
			else
			{
				EndBindKey(btnDef.m_key);
			}
		}
	}

	private void UpdateGamepadInputLayout(float repeatDelay, float repeatInterval)
	{
		ClearGamepadButtons();
		switch (InputLayout)
		{
		case InputLayout.Default:
			ResetToClassicLayout(repeatDelay, repeatInterval);
			break;
		case InputLayout.Alternative1:
			ResetToAlternative1Layout(repeatDelay, repeatInterval);
			break;
		case InputLayout.Alternative2:
			ResetToAlternative2Layout(repeatDelay, repeatInterval);
			break;
		}
	}

	private void ClearGamepadButtons()
	{
		foreach (ButtonDef item in m_buttons.Values.ToList())
		{
			if (m_buttons.ContainsKey(item.m_name) && item.Source == InputSource.Gamepad)
			{
				m_buttons.Remove(item.m_name);
			}
		}
	}

	private void SetGeneralInputMappings(float repeatDelay, float repeatInterval, GamepadInput leftTrigger, GamepadInput rightTrigger)
	{
		AddButton("JoyButtonA", GamepadInput.FaceButtonA, 0f, 0f, showHints: false);
		AddButton("JoyButtonB", GamepadInput.FaceButtonB, 0f, 0f, showHints: false);
		AddButton("JoyButtonX", GamepadInput.FaceButtonX, 0f, 0f, showHints: false);
		AddButton("JoyButtonY", GamepadInput.FaceButtonY, 0f, 0f, showHints: false);
		AddButton("JoyBack", GamepadInput.Select, 0f, 0f, showHints: false);
		AddButton("JoyStart", GamepadInput.Start);
		AddButton("JoyLStick", GamepadInput.StickLButton, 0f, 0f, showHints: false);
		AddButton("JoyRStick", GamepadInput.StickRButton, 0f, 0f, showHints: false);
		AddButton("JoyLStickLeft", GamepadInput.StickLHorizontal, new FloatRange(0f, -1f), repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyLStickRight", GamepadInput.StickLHorizontal, new FloatRange(0f, 1f), repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyLStickUp", GamepadInput.StickLVertical, new FloatRange(0f, 1f), repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyLStickDown", GamepadInput.StickLVertical, new FloatRange(0f, -1f), repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyRStickLeft", GamepadInput.StickRHorizontal, new FloatRange(0f, -1f), repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyRStickRight", GamepadInput.StickRHorizontal, new FloatRange(0f, 1f), repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyRStickUp", GamepadInput.StickRVertical, new FloatRange(0f, 1f), repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyRStickDown", GamepadInput.StickRVertical, new FloatRange(0f, -1f), repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyDPadLeft", GamepadInput.DPadLeft, repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyDPadRight", GamepadInput.DPadRight, repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyDPadUp", GamepadInput.DPadUp, repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyDPadDown", GamepadInput.DPadDown, repeatDelay, repeatInterval, showHints: false);
		AddButton("JoyLTrigger", leftTrigger, 0f, 0f, showHints: false);
		AddButton("JoyRTrigger", rightTrigger, 0f, 0f, showHints: false);
		AddButton("JoyLBumper", GamepadInput.BumperL);
		AddButton("JoyRBumper", GamepadInput.BumperR);
	}

	private void ResetToClassicLayout(float repeatDelay, float repeatInterval)
	{
		GamepadInput gamepadInput = (SwapTriggers ? GamepadInput.TriggerR : GamepadInput.TriggerL);
		GamepadInput gamepadInput2 = (SwapTriggers ? GamepadInput.TriggerL : GamepadInput.TriggerR);
		SetGeneralInputMappings(repeatDelay, repeatInterval, gamepadInput, gamepadInput2);
		AddButton("JoyBuildMenu", GamepadInput.FaceButtonA);
		AddButton("JoyUse", GamepadInput.FaceButtonA);
		AddButton("JoyHide", GamepadInput.StickRButton);
		AddButton("JoyJump", GamepadInput.FaceButtonB);
		AddButton("JoyDodge", GamepadInput.FaceButtonB, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoySit", GamepadInput.FaceButtonX);
		AddButton("JoyGP", GamepadInput.DPadDown);
		AddButton("JoyInventory", GamepadInput.FaceButtonY);
		AddButton("JoyRun", GamepadInput.BumperL);
		AddButton("JoyCrouch", GamepadInput.StickLButton);
		AddButton("JoyMap", GamepadInput.Select);
		AddButton("JoyChat", GamepadInput.Select, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyMenu", GamepadInput.Start);
		AddButton("JoyToggleHUD", GamepadInput.Start, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyHotbarUse", GamepadInput.DPadUp);
		AddButton("JoyCamZoomIn", GamepadInput.DPadUp, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyCamZoomOut", GamepadInput.DPadDown, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyMapZoomIn", GamepadInput.DPadRight, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyMapZoomOut", GamepadInput.DPadLeft, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyHotbarLeft", GamepadInput.DPadLeft, repeatDelay, repeatInterval);
		AddButton("JoyHotbarRight", GamepadInput.DPadRight, repeatDelay, repeatInterval);
		AddButton("JoyAutoPickup", GamepadInput.StickLButton, 0f, 0f, showHints: true, altKey: true);
		AddButton("JoyBlock", gamepadInput);
		AddButton("JoyAttack", gamepadInput2);
		AddButton("JoySecondaryAttack", GamepadInput.BumperR);
		if (UISettings.UseRadials)
		{
			AddButton("JoyRadial", GamepadInput.BumperL);
			AddButton("JoyRadialInteract", gamepadInput2);
			AddButton("JoyRadialBack", gamepadInput);
			AddButton("JoyRadialClose", GamepadInput.FaceButtonB);
		}
		AddButton("JoyAltPlace", GamepadInput.BumperL);
		AddButton("JoyRotate", gamepadInput);
		AddButton("JoyPlace", gamepadInput2);
		AddButton("JoyRemove", GamepadInput.BumperR);
		AddButton("JoyTabLeft", GamepadInput.BumperL, 0f, 0f, showHints: false);
		AddButton("JoyTabRight", GamepadInput.BumperR, 0f, 0f, showHints: false);
		AddButton("JoyNextSnap", GamepadInput.StickRButton);
		AddButton("JoyPrevSnap", GamepadInput.StickLButton);
		AddButton("JoyAltKeys", gamepadInput);
		AddButton("JoyScrollChatUp", GamepadInput.StickRVertical, new FloatRange(0f, 1f), 0.5f, 0.05f);
		AddButton("JoyScrollChatDown", GamepadInput.StickRVertical, new FloatRange(0f, -1f), 0.5f, 0.05f);
	}

	public static bool IsGamepadEnabled()
	{
		return s_inputSwitchingMode.HasFlag(InputSource.AllowGamepadInputBit);
	}

	public static void SetGamepadEnabled(bool enabled)
	{
		SetInputSwitchingMode(enabled ? InputSource.AutomaticNonBlocking : InputSource.KeyboardMouseOnly);
	}

	public static void SetInputSwitchingMode(InputSource inputSwitchingMode)
	{
		if (inputSwitchingMode != 0)
		{
			s_inputSwitchingMode = inputSwitchingMode;
		}
		else
		{
			ZLog.LogError($"Can't set input switching mode to {InputSource.None}! Forcing value to {InputSource.AutomaticNonBlocking}");
			s_inputSwitchingMode = InputSource.AutomaticNonBlocking;
		}
		InvalidateInputSwitchingModeValid();
		InputSource inputSource = m_inputSource;
		switch (s_inputSwitchingMode)
		{
		case InputSource.KeyboardMouseOnly:
		case InputSource.BothKeyboardMouseHints:
			inputSource = InputSource.KeyboardMouse;
			break;
		case InputSource.GamepadOnly:
		case InputSource.BothGamepadHints:
			inputSource = InputSource.Gamepad;
			break;
		}
		bool flag = inputSource != m_inputSource;
		m_inputSource = inputSource;
		if (flag && instance != null)
		{
			ZInput.OnInputLayoutChanged?.Invoke();
		}
	}

	public static bool IsGamepadActive()
	{
		if (m_virtualKeyboardOpen)
		{
			return false;
		}
		if (m_instance == null || !IsGamepadEnabled())
		{
			return false;
		}
		return m_inputSource == InputSource.Gamepad;
	}

	public static bool IsKeyboardAvailable()
	{
		if (Keyboard.current != null)
		{
			return !m_virtualKeyboardOpen;
		}
		return false;
	}

	protected virtual bool Input_IsMouseActive()
	{
		if (Mouse.current != null)
		{
			return m_inputSource == InputSource.KeyboardMouse;
		}
		return false;
	}

	public void Save()
	{
		PlayerPrefs.DeleteKey("gamepad_enabled");
		PlayerPrefs.SetInt("input_switching_mode", (int)s_inputSwitchingMode);
		foreach (ButtonDef value in m_buttons.Values)
		{
			if (value.Source == InputSource.KeyboardMouse)
			{
				string text = "kbmKey_" + value.m_name;
				PlayerPrefs.SetInt(text + "_isMouseButton", value.m_bMouseButtonSet ? 1 : 0);
				PlayerPrefs.SetInt(text, value.m_bMouseButtonSet ? ((int)value.m_mouseButton) : ((int)value.m_key));
				string key = "key_" + value.m_name;
				if (PlayerPrefs.HasKey(key))
				{
					PlayerPrefs.DeleteKey(key);
				}
			}
		}
	}

	public void Load()
	{
		SwapTriggers = PlayerPrefs.GetInt("SwapTriggers", 0) == 1;
		ToggleRun = PlayerPrefs.GetInt("ToggleRun", IsGamepadActive() ? 1 : 0) == 1;
		Reset();
		InputSource @int = (InputSource)PlayerPrefs.GetInt("input_switching_mode", 0);
		int int2 = PlayerPrefs.GetInt("gamepad_enabled", -1);
		if (Enum.IsDefined(typeof(InputSource), @int) && @int.HasFlag(InputSource.InputSwitchingModeBit | InputSource.ValidBit))
		{
			SetInputSwitchingMode(@int);
		}
		else if (int2 >= 0)
		{
			SetInputSwitchingMode((int2 == 1) ? InputSource.AutomaticNonBlocking : InputSource.KeyboardMouseOnly);
		}
		else
		{
			SetInputSwitchingMode(InputSource.AutomaticNonBlocking);
		}
		foreach (ButtonDef value in m_buttons.Values)
		{
			if (value.Source != InputSource.KeyboardMouse)
			{
				continue;
			}
			string text = "kbmKey_" + value.m_name;
			if (PlayerPrefs.HasKey(text))
			{
				value.m_bMouseButtonSet = PlayerPrefs.GetInt(text + "_isMouseButton", 0) != 0;
				if (value.m_bMouseButtonSet)
				{
					MouseButton int3 = (MouseButton)PlayerPrefs.GetInt(text, -1);
					value.m_mouseButton = ((int3 != (MouseButton)(-1)) ? int3 : MouseButton.Back);
				}
				else
				{
					Key int4 = (Key)PlayerPrefs.GetInt(text, -1);
					value.m_key = ((int4 != (Key)(-1)) ? int4 : Key.None);
				}
				continue;
			}
			KeyCode int5 = (KeyCode)PlayerPrefs.GetInt("key_" + value.m_name, -1);
			if (int5 == (KeyCode)(-1))
			{
				continue;
			}
			Key key = KeyCodeToKey(int5);
			if (key != 0)
			{
				value.m_key = key;
				continue;
			}
			var (flag, mouseButton) = KeyCodeToMouseButton(int5);
			if (flag)
			{
				value.m_mouseButton = mouseButton;
				value.m_bMouseButtonSet = true;
			}
		}
		PlayStationGlyphs = PlayerPrefs.GetInt("AltGlyphs", 0) == 1;
	}

	public ButtonDef AddButton(string name, GamepadInput gamepadInput, FloatRange mappedRange, float repeatDelay = 0f, float repeatInterval = 0f, bool showHints = true, bool altKey = false)
	{
		if (gamepadInput == GamepadInput.None)
		{
			ZLog.LogWarning($"Set button \"{name}\" to {GamepadInput.None}!");
			return null;
		}
		ButtonDef buttonDef = new ButtonDef(name, gamepadInput, mappedRange, repeatDelay, repeatInterval, showHints, altKey);
		m_buttons.Add(name, buttonDef);
		return buttonDef;
	}

	public ButtonDef AddButton(string name, GamepadInput gamepadInput, float repeatDelay = 0f, float repeatInterval = 0f, bool showHints = true, bool altKey = false)
	{
		if (gamepadInput == GamepadInput.None)
		{
			ZLog.LogWarning($"Set button \"{name}\" to {GamepadInput.None}!");
			return null;
		}
		ButtonDef buttonDef = new ButtonDef(name, gamepadInput, repeatDelay, repeatInterval, showHints, altKey);
		m_buttons.Add(name, buttonDef);
		return buttonDef;
	}

	public ButtonDef AddButton(string name, Key keyboardKey, float repeatDelay = 0f, float repeatInterval = 0f, bool showHints = true, bool altKey = false)
	{
		if (keyboardKey == Key.None)
		{
			ZLog.LogWarning($"Set button \"{name}\" to {Key.None}!");
			return null;
		}
		ButtonDef buttonDef = new ButtonDef(name, keyboardKey, repeatDelay, repeatInterval, showHints, altKey);
		m_buttons.Add(name, buttonDef);
		return buttonDef;
	}

	public ButtonDef AddButton(string name, MouseButton mouseButton, float repeatDelay = 0f, float repeatInterval = 0f, bool showHints = true, bool altKey = false)
	{
		ButtonDef buttonDef = new ButtonDef(name, mouseButton, repeatDelay, repeatInterval, showHints, altKey);
		m_buttons.Add(name, buttonDef);
		return buttonDef;
	}

	public string GetBoundKeyString(string name, bool emptyStringOnMissing = false)
	{
		string controllerPlatform = (PlayStationGlyphs ? "ps5" : "xbox");
		if (m_buttons.TryGetValue(name, out var value))
		{
			if (value.Source == InputSource.KeyboardMouse)
			{
				if (!value.m_bMouseButtonSet)
				{
					switch (value.m_key)
					{
					case Key.Comma:
						return ",";
					case Key.Period:
						return ".";
					case Key.Space:
						return "$button_space";
					case Key.LeftShift:
						return "$button_lshift";
					case Key.RightShift:
						return "$button_rshift";
					case Key.LeftAlt:
						return "$button_lalt";
					case Key.RightAlt:
						return "$button_ralt";
					case Key.LeftCtrl:
						return "$button_lctrl";
					case Key.RightCtrl:
						return "$button_rctrl";
					case Key.Enter:
						return "$button_return";
					case Key.NumpadEnter:
						return "$button_return";
					case Key.None:
						return "$menu_none";
					default:
						if (Keyboard.current == null)
						{
							return value.m_key.ToString();
						}
						return Keyboard.current[value.m_key].displayName;
					}
				}
				switch (value.m_mouseButton)
				{
				case MouseButton.Left:
					return "$button_mouse0";
				case MouseButton.Right:
					return "$button_mouse1";
				case MouseButton.Middle:
					return "$button_mouse2";
				case MouseButton.Forward:
					if (Mouse.current != null)
					{
						return Mouse.current.forwardButton.displayName;
					}
					return "Mouse Forward";
				case MouseButton.Back:
					if (Mouse.current != null)
					{
						return Mouse.current.backButton.displayName;
					}
					return "Mouse Back";
				}
			}
			if (value.TryGetButtonSprite(controllerPlatform, out var result))
			{
				return result;
			}
		}
		if (ButtonDef.TryGetStickSprite(controllerPlatform, name, out var result2))
		{
			return result2;
		}
		if (emptyStringOnMissing || m_virtualKeyboardOpen || m_blockGamePadInput > 0f)
		{
			return "";
		}
		return "MISSING KEY BINDING \"" + name + "\"";
	}

	public ButtonDef GetButtonDef(string name)
	{
		if (m_buttons.TryGetValue(name, out var value))
		{
			return value;
		}
		return null;
	}

	public string GetBoundActionString(KeyCode keycode)
	{
		string str = "";
		foreach (KeyValuePair<string, ButtonDef> button in m_buttons)
		{
			if (button.Value.m_showHints && button.Value.m_key == KeyCodeToKey(keycode))
			{
				tryAddString(ref str, button.Value);
			}
		}
		return str;
	}

	public string GetBoundActionString(GamepadInput gamepadInput, FloatRange? mappedRange)
	{
		string str = "";
		foreach (KeyValuePair<string, ButtonDef> button in m_buttons)
		{
			if (button.Value.m_showHints && button.Value.m_gamepadInput == gamepadInput && button.Value.m_mappedRange == mappedRange)
			{
				tryAddString(ref str, button.Value);
			}
		}
		return str;
	}

	private void tryAddString(ref string str, ButtonDef button)
	{
		string text = button.m_name.ToLower();
		if (!text.Contains("bumper") && !text.Contains("start") && !(text == "joyaltkeys"))
		{
			if (str.Length > 0)
			{
				str += " / ";
			}
			if (text.Length > 3 && text.ToLower().StartsWith("joy"))
			{
				text = text.Substring(3);
			}
			if (button.m_altKey)
			{
				str = str + "<color=#AAAAAA>$settings_" + text + "</color>";
			}
			else if (IsNonClassicFunctionality() && (text == "rotate" || text == "rotateright"))
			{
				str += "$rotate_build_mode";
			}
			else
			{
				str = str + "$settings_" + text;
			}
		}
	}

	public List<string> GetDuplicateBindings()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, ButtonDef> button in m_buttons)
		{
			foreach (KeyValuePair<string, ButtonDef> button2 in m_buttons)
			{
				if (button.Value != button2.Value && button2.Value.m_showHints && ((button.Value.m_key != 0 && button.Value.m_key == button2.Value.m_key) || (button.Value.m_gamepadInput != 0 && button.Value.m_gamepadInput == button2.Value.m_gamepadInput && button.Value.m_mappedRange == button2.Value.m_mappedRange)))
				{
					list.Add(button.Key);
				}
			}
		}
		return list;
	}

	public static void ResetButtonStatus(string name)
	{
		if (m_instance.m_buttons.TryGetValue(name, out var value))
		{
			value.m_down = false;
			value.m_up = false;
			value.m_downFixed = false;
			value.m_upFixed = false;
		}
	}

	public static bool GetButtonDown(string name)
	{
		if (m_instance.m_buttons.TryGetValue(name, out var value))
		{
			if (Time.inFixedTimeStep)
			{
				return value.m_downFixed;
			}
			return value.m_down;
		}
		return false;
	}

	public static bool GetButtonUp(string name)
	{
		if (m_instance.m_buttons.TryGetValue(name, out var value))
		{
			if (Time.inFixedTimeStep)
			{
				return value.m_upFixed;
			}
			return value.m_up;
		}
		return false;
	}

	public static bool GetButton(string name)
	{
		if (m_instance.m_buttons.TryGetValue(name, out var value))
		{
			return value.m_pressed;
		}
		return false;
	}

	public static float GetButtonPressedTimer(string name)
	{
		if (m_instance.m_buttons.TryGetValue(name, out var value))
		{
			return value.m_pressedTimer;
		}
		return 0f;
	}

	public static float GetButtonLastPressedTimer(string name)
	{
		if (m_instance.m_buttons.TryGetValue(name, out var value))
		{
			return value.m_lastPressedTimer;
		}
		return 0f;
	}

	public Key GetPressedKey()
	{
		foreach (Key s_keyCodeValue in s_keyCodeValues)
		{
			if (GetKeyNew(s_keyCodeValue))
			{
				return s_keyCodeValue;
			}
		}
		return Key.None;
	}

	public (bool, MouseButton) GetPressedMouseButton()
	{
		if (Mouse.current == null)
		{
			return (false, MouseButton.Back);
		}
		if (Mouse.current.leftButton.isPressed && Mouse.current.leftButton.wasPressedThisFrame)
		{
			return (true, MouseButton.Left);
		}
		if (Mouse.current.rightButton.isPressed && Mouse.current.rightButton.wasPressedThisFrame)
		{
			return (true, MouseButton.Right);
		}
		if (Mouse.current.middleButton.isPressed && Mouse.current.middleButton.wasPressedThisFrame)
		{
			return (true, MouseButton.Middle);
		}
		if (Mouse.current.forwardButton.isPressed && Mouse.current.forwardButton.wasPressedThisFrame)
		{
			return (true, MouseButton.Forward);
		}
		if (Mouse.current.backButton.isPressed && Mouse.current.backButton.wasPressedThisFrame)
		{
			return (true, MouseButton.Back);
		}
		return (false, MouseButton.Back);
	}

	public void StartBindKey(string name)
	{
		if (m_buttons.TryGetValue(name, out var value))
		{
			m_binding = value;
		}
	}

	public bool EndBindKey()
	{
		if (m_binding == null)
		{
			return true;
		}
		Key pressedKey = GetPressedKey();
		if (pressedKey != 0)
		{
			return EndBindKey(pressedKey);
		}
		var (flag, btn) = GetPressedMouseButton();
		if (flag)
		{
			return EndBindKey(btn);
		}
		return false;
	}

	private bool EndBindKey(Key key)
	{
		if (m_binding == null)
		{
			return true;
		}
		if (key != 0)
		{
			m_binding.m_key = key;
			m_binding.m_bMouseButtonSet = false;
			return true;
		}
		return false;
	}

	private bool EndBindKey(MouseButton btn)
	{
		if (m_binding == null)
		{
			return true;
		}
		m_binding.m_key = Key.None;
		m_binding.m_mouseButton = btn;
		m_binding.m_bMouseButtonSet = true;
		return true;
	}

	public static float ApplyDeadzone(float v, bool soften)
	{
		float num = Mathf.Sign(v);
		v = Mathf.Abs(v);
		v = Mathf.Clamp01(v - 0.2f);
		v *= 1.25f;
		if (soften)
		{
			v *= v;
		}
		v *= num;
		return v;
	}

	private static float ReadAnalogueGamepadInput(GamepadInput gamepadInput, bool applyDeadzone = false, bool smooth = false)
	{
		InputDefinition definition = m_instance.m_definition.GetDefinition(gamepadInput);
		float num = definition.ReadAsAnalogue();
		if (num != 0f)
		{
			m_instance.OnInput(InputSource.Gamepad, definition.IsDigital);
		}
		if (!AcceptInputFromSource(InputSource.Gamepad))
		{
			return 0f;
		}
		if (applyDeadzone)
		{
			num = ApplyDeadzone(num, smooth);
		}
		return num;
	}

	public static float GetJoyLeftStickX(bool smooth = false)
	{
		return ReadAnalogueGamepadInput(GamepadInput.StickLHorizontal, applyDeadzone: true, smooth);
	}

	public static float GetJoyLeftStickY(bool smooth = true)
	{
		return 0f - ReadAnalogueGamepadInput(GamepadInput.StickLVertical, applyDeadzone: true, smooth);
	}

	public static float GetJoyRightStickX()
	{
		return ReadAnalogueGamepadInput(GamepadInput.StickRHorizontal, applyDeadzone: true, smooth: true);
	}

	public static float GetJoyRightStickY()
	{
		return 0f - ReadAnalogueGamepadInput(GamepadInput.StickRVertical, applyDeadzone: true, smooth: true);
	}

	public static float GetJoyLTrigger()
	{
		return ReadAnalogueGamepadInput(GamepadInput.TriggerL);
	}

	public static float GetJoyRTrigger()
	{
		return ReadAnalogueGamepadInput(GamepadInput.TriggerR);
	}

	public static Vector2 GetMouseDelta()
	{
		if (Mouse.current == null)
		{
			return Vector2.zero;
		}
		Vector2 vector = Mouse.current.delta.ReadUnprocessedValue() * 0.05f;
		if (vector != Vector2.zero)
		{
			m_instance.OnInput(InputSource.KeyboardMouse, allowSwitchInputSource: true);
		}
		if (!AcceptInputFromSource(InputSource.KeyboardMouse))
		{
			return Vector2.zero;
		}
		return vector;
	}

	public static float GetMouseScrollWheel()
	{
		if (Mouse.current == null)
		{
			return 0f;
		}
		float num = Mouse.current.scroll.ReadUnprocessedValue().normalized.y * 0.05f;
		if (num != 0f)
		{
			m_instance.OnInput(InputSource.KeyboardMouse, allowSwitchInputSource: true);
		}
		if (!AcceptInputFromSource(InputSource.KeyboardMouse))
		{
			return 0f;
		}
		return num;
	}

	public static float GetAxis(string axisName)
	{
		return m_instance.Input_GetAxis(axisName);
	}

	public static bool GetKey(KeyCode key, bool logWarning = true)
	{
		return m_instance.Input_GetKey(key, logWarning);
	}

	public static bool GetKeyDown(KeyCode key, bool logWarning = true)
	{
		return m_instance.Input_GetKeyDown(key, logWarning);
	}

	public static bool GetMouseButton(int button)
	{
		return m_instance.Input_GetMouseButton(button);
	}

	public static bool GetMouseButtonDown(int button)
	{
		return m_instance.Input_GetMouseButtonDown(button);
	}

	public static bool GetMouseButtonUp(int button)
	{
		return m_instance.Input_GetMouseButtonUp(button);
	}

	public static bool IsMouseActive()
	{
		return m_instance.Input_IsMouseActive();
	}

	public virtual Vector3 Input_GetMousePosition()
	{
		if (Mouse.current == null)
		{
			return Vector2.zero;
		}
		return Mouse.current.position.ReadUnprocessedValue();
	}

	public virtual float Input_GetAxis(string axisName)
	{
		return axisName switch
		{
			"JoyAxis 1" => GetJoyLeftStickX(), 
			"JoyAxis 2" => GetJoyLeftStickY(), 
			"JoyAxis 3" => GetJoyRTrigger(), 
			"JoyAxis 4" => GetJoyRightStickX(), 
			"JoyAxis 5" => GetJoyRightStickY(), 
			_ => Input.GetAxis(axisName), 
		};
	}

	public virtual bool Input_GetKey(KeyCode key, bool logWarning = true)
	{
		if (key >= KeyCode.JoystickButton0 && key <= KeyCode.JoystickButton19)
		{
			if (Gamepad.current != null)
			{
				return Gamepad.current[KeyCodeToGamepadButton(key)].isPressed;
			}
			return false;
		}
		if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse4)
		{
			if (Mouse.current == null)
			{
				return false;
			}
			var (flag, mouseButton) = KeyCodeToMouseButton(key, logWarning);
			if (!flag)
			{
				return false;
			}
			return mouseButton switch
			{
				MouseButton.Left => Mouse.current.leftButton.isPressed, 
				MouseButton.Right => Mouse.current.rightButton.isPressed, 
				MouseButton.Middle => Mouse.current.middleButton.isPressed, 
				MouseButton.Forward => Mouse.current.forwardButton.isPressed, 
				MouseButton.Back => Mouse.current.backButton.isPressed, 
				_ => false, 
			};
		}
		if (key >= KeyCode.Mouse5 && key <= KeyCode.Mouse6)
		{
			return Input.GetKey(key);
		}
		if (Keyboard.current == null)
		{
			return false;
		}
		Key key2 = KeyCodeToKey(key, logWarning);
		if (key2 != 0)
		{
			return Keyboard.current[key2].isPressed;
		}
		return Input.GetKey(key);
	}

	public virtual bool Input_GetKeyDown(KeyCode key, bool logWarning = true)
	{
		if (key >= KeyCode.JoystickButton0 && key <= KeyCode.JoystickButton19)
		{
			if (Gamepad.current != null)
			{
				return Gamepad.current[KeyCodeToGamepadButton(key)].wasPressedThisFrame;
			}
			return false;
		}
		if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse4)
		{
			if (Mouse.current == null)
			{
				return false;
			}
			var (flag, mouseButton) = KeyCodeToMouseButton(key, logWarning);
			if (!flag)
			{
				return false;
			}
			return mouseButton switch
			{
				MouseButton.Left => Mouse.current.leftButton.wasPressedThisFrame, 
				MouseButton.Right => Mouse.current.rightButton.wasPressedThisFrame, 
				MouseButton.Middle => Mouse.current.middleButton.wasPressedThisFrame, 
				MouseButton.Forward => Mouse.current.forwardButton.wasPressedThisFrame, 
				MouseButton.Back => Mouse.current.backButton.wasPressedThisFrame, 
				_ => false, 
			};
		}
		if (key >= KeyCode.Mouse5 && key <= KeyCode.Mouse6)
		{
			return Input.GetKeyDown(key);
		}
		if (Keyboard.current == null)
		{
			return false;
		}
		Key key2 = KeyCodeToKey(key, logWarning);
		if (key2 != 0)
		{
			return Keyboard.current[key2].wasPressedThisFrame;
		}
		return Input.GetKeyDown(key);
	}

	public virtual bool Input_GetMouseButton(int button, bool logWarning = true)
	{
		if (Mouse.current == null)
		{
			return false;
		}
		switch (button)
		{
		case 0:
			return Mouse.current.leftButton.isPressed;
		case 1:
			return Mouse.current.rightButton.isPressed;
		case 2:
			return Mouse.current.middleButton.isPressed;
		case 3:
			return Mouse.current.forwardButton.isPressed;
		case 4:
			return Mouse.current.backButton.isPressed;
		default:
			if (logWarning)
			{
				Debug.LogError($"{button} is not a valid Mouse Button.");
			}
			return false;
		}
	}

	public virtual bool Input_GetMouseButtonDown(int button, bool logWarning = true)
	{
		if (Mouse.current == null)
		{
			return false;
		}
		switch (button)
		{
		case 0:
			return Mouse.current.leftButton.wasPressedThisFrame;
		case 1:
			return Mouse.current.rightButton.wasPressedThisFrame;
		case 2:
			return Mouse.current.middleButton.wasPressedThisFrame;
		case 3:
			return Mouse.current.forwardButton.wasPressedThisFrame;
		case 4:
			return Mouse.current.backButton.wasPressedThisFrame;
		default:
			if (logWarning)
			{
				Debug.LogError($"{button} is not a valid Mouse Button.");
			}
			return false;
		}
	}

	public virtual bool Input_GetMouseButtonUp(int button, bool logWarning = true)
	{
		if (Mouse.current == null)
		{
			return false;
		}
		switch (button)
		{
		case 0:
			return Mouse.current.leftButton.wasReleasedThisFrame;
		case 1:
			return Mouse.current.rightButton.wasReleasedThisFrame;
		case 2:
			return Mouse.current.middleButton.wasReleasedThisFrame;
		case 3:
			return Mouse.current.forwardButton.wasReleasedThisFrame;
		case 4:
			return Mouse.current.backButton.wasReleasedThisFrame;
		default:
			if (logWarning)
			{
				Debug.LogError($"{button} is not a valid Mouse Button.");
			}
			return false;
		}
	}

	public bool GetKeyNew(Key key)
	{
		if (key == Key.None || key == Key.IMESelected || Keyboard.current == null)
		{
			return false;
		}
		return Keyboard.current[key].isPressed;
	}

	public bool GetMouseButtonNew(MouseButton mouseButton)
	{
		if (Mouse.current == null)
		{
			return false;
		}
		return mouseButton switch
		{
			MouseButton.Left => Mouse.current.leftButton.isPressed, 
			MouseButton.Right => Mouse.current.rightButton.isPressed, 
			MouseButton.Middle => Mouse.current.middleButton.isPressed, 
			MouseButton.Forward => Mouse.current.forwardButton.isPressed, 
			MouseButton.Back => Mouse.current.backButton.isPressed, 
			_ => false, 
		};
	}

	public static Key KeyCodeToKey(KeyCode keyCode, bool logWarning = true)
	{
		switch (keyCode)
		{
		case KeyCode.None:
			return Key.None;
		case KeyCode.Backspace:
			return Key.Backspace;
		case KeyCode.Delete:
			return Key.Delete;
		case KeyCode.Tab:
			return Key.Tab;
		case KeyCode.Clear:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Return:
			return Key.Enter;
		case KeyCode.Pause:
			return Key.Pause;
		case KeyCode.Escape:
			return Key.Escape;
		case KeyCode.Space:
			return Key.Space;
		case KeyCode.Keypad0:
			return Key.Numpad0;
		case KeyCode.Keypad1:
			return Key.Numpad1;
		case KeyCode.Keypad2:
			return Key.Numpad2;
		case KeyCode.Keypad3:
			return Key.Numpad3;
		case KeyCode.Keypad4:
			return Key.Numpad4;
		case KeyCode.Keypad5:
			return Key.Numpad5;
		case KeyCode.Keypad6:
			return Key.Numpad6;
		case KeyCode.Keypad7:
			return Key.Numpad7;
		case KeyCode.Keypad8:
			return Key.Numpad8;
		case KeyCode.Keypad9:
			return Key.Numpad9;
		case KeyCode.KeypadPeriod:
			return Key.NumpadPeriod;
		case KeyCode.KeypadDivide:
			return Key.NumpadDivide;
		case KeyCode.KeypadMultiply:
			return Key.NumpadMultiply;
		case KeyCode.KeypadMinus:
			return Key.NumpadMinus;
		case KeyCode.KeypadPlus:
			return Key.NumpadPlus;
		case KeyCode.KeypadEnter:
			return Key.NumpadEnter;
		case KeyCode.KeypadEquals:
			return Key.NumpadEquals;
		case KeyCode.UpArrow:
			return Key.UpArrow;
		case KeyCode.DownArrow:
			return Key.DownArrow;
		case KeyCode.RightArrow:
			return Key.RightArrow;
		case KeyCode.LeftArrow:
			return Key.LeftArrow;
		case KeyCode.Insert:
			return Key.Insert;
		case KeyCode.Home:
			return Key.Home;
		case KeyCode.End:
			return Key.End;
		case KeyCode.PageUp:
			return Key.PageUp;
		case KeyCode.PageDown:
			return Key.PageDown;
		case KeyCode.F1:
			return Key.F1;
		case KeyCode.F2:
			return Key.F2;
		case KeyCode.F3:
			return Key.F3;
		case KeyCode.F4:
			return Key.F4;
		case KeyCode.F5:
			return Key.F5;
		case KeyCode.F6:
			return Key.F6;
		case KeyCode.F7:
			return Key.F7;
		case KeyCode.F8:
			return Key.F8;
		case KeyCode.F9:
			return Key.F9;
		case KeyCode.F10:
			return Key.F10;
		case KeyCode.F11:
			return Key.F11;
		case KeyCode.F12:
			return Key.F12;
		case KeyCode.F13:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.F14:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.F15:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Alpha0:
			return Key.Digit0;
		case KeyCode.Alpha1:
			return Key.Digit1;
		case KeyCode.Alpha2:
			return Key.Digit2;
		case KeyCode.Alpha3:
			return Key.Digit3;
		case KeyCode.Alpha4:
			return Key.Digit4;
		case KeyCode.Alpha5:
			return Key.Digit5;
		case KeyCode.Alpha6:
			return Key.Digit6;
		case KeyCode.Alpha7:
			return Key.Digit7;
		case KeyCode.Alpha8:
			return Key.Digit8;
		case KeyCode.Alpha9:
			return Key.Digit9;
		case KeyCode.Exclaim:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.DoubleQuote:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Hash:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Dollar:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Percent:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Ampersand:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Quote:
			return Key.Quote;
		case KeyCode.LeftParen:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.RightParen:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Asterisk:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Plus:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Comma:
			return Key.Comma;
		case KeyCode.Minus:
			return Key.Minus;
		case KeyCode.Period:
			return Key.Period;
		case KeyCode.Slash:
			return Key.Slash;
		case KeyCode.Colon:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Semicolon:
			return Key.Semicolon;
		case KeyCode.Less:
			return Key.None;
		case KeyCode.Equals:
			return Key.Equals;
		case KeyCode.Greater:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Question:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.At:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.LeftBracket:
			return Key.LeftBracket;
		case KeyCode.Backslash:
			return Key.Backslash;
		case KeyCode.RightBracket:
			return Key.RightBracket;
		case KeyCode.Caret:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Underscore:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.BackQuote:
			return Key.Backquote;
		case KeyCode.A:
			return Key.A;
		case KeyCode.B:
			return Key.B;
		case KeyCode.C:
			return Key.C;
		case KeyCode.D:
			return Key.D;
		case KeyCode.E:
			return Key.E;
		case KeyCode.F:
			return Key.F;
		case KeyCode.G:
			return Key.G;
		case KeyCode.H:
			return Key.H;
		case KeyCode.I:
			return Key.I;
		case KeyCode.J:
			return Key.J;
		case KeyCode.K:
			return Key.K;
		case KeyCode.L:
			return Key.L;
		case KeyCode.M:
			return Key.M;
		case KeyCode.N:
			return Key.N;
		case KeyCode.O:
			return Key.O;
		case KeyCode.P:
			return Key.P;
		case KeyCode.Q:
			return Key.Q;
		case KeyCode.R:
			return Key.R;
		case KeyCode.S:
			return Key.S;
		case KeyCode.T:
			return Key.T;
		case KeyCode.U:
			return Key.U;
		case KeyCode.V:
			return Key.V;
		case KeyCode.W:
			return Key.W;
		case KeyCode.X:
			return Key.X;
		case KeyCode.Y:
			return Key.Y;
		case KeyCode.Z:
			return Key.Z;
		case KeyCode.LeftCurlyBracket:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Pipe:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.RightCurlyBracket:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Tilde:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Numlock:
			return Key.NumLock;
		case KeyCode.CapsLock:
			return Key.CapsLock;
		case KeyCode.ScrollLock:
			return Key.ScrollLock;
		case KeyCode.RightShift:
			return Key.RightShift;
		case KeyCode.LeftShift:
			return Key.LeftShift;
		case KeyCode.RightControl:
			return Key.RightCtrl;
		case KeyCode.LeftControl:
			return Key.LeftCtrl;
		case KeyCode.RightAlt:
			return Key.RightAlt;
		case KeyCode.LeftAlt:
			return Key.LeftAlt;
		case KeyCode.LeftMeta:
			return Key.LeftMeta;
		case KeyCode.LeftWindows:
			return Key.LeftMeta;
		case KeyCode.RightMeta:
			return Key.RightMeta;
		case KeyCode.RightWindows:
			return Key.RightMeta;
		case KeyCode.AltGr:
			return Key.RightAlt;
		case KeyCode.Help:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Print:
			return Key.PrintScreen;
		case KeyCode.SysReq:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Break:
			return MissingCounterpart(keyCode, logWarning);
		case KeyCode.Menu:
			return Key.ContextMenu;
		case KeyCode.Mouse0:
		case KeyCode.Mouse1:
		case KeyCode.Mouse2:
		case KeyCode.Mouse3:
		case KeyCode.Mouse4:
		case KeyCode.Mouse5:
		case KeyCode.Mouse6:
			if (logWarning)
			{
				Debug.LogWarning("Mouse KeyCodes have been deprecated with the new input system. Returning \"Key.None\"");
			}
			return Key.None;
		default:
			if (logWarning)
			{
				Debug.LogWarning("Joystick KeyCodes have been deprecated with the new input system. Returning \"Key.None\"");
			}
			return Key.None;
		}
	}

	private static Key MissingCounterpart(KeyCode keyCode, bool logWarning = true)
	{
		if (logWarning)
		{
			Debug.LogWarning($"The KeyCode: {keyCode}, lacks a proper counterpart in the Key enum. Returning \"Key.None\".");
		}
		return Key.None;
	}

	public static (bool, MouseButton) KeyCodeToMouseButton(KeyCode keyCode, bool logWarning = true)
	{
		switch (keyCode)
		{
		case KeyCode.Mouse0:
			return (true, MouseButton.Left);
		case KeyCode.Mouse1:
			return (true, MouseButton.Right);
		case KeyCode.Mouse2:
			return (true, MouseButton.Middle);
		case KeyCode.Mouse3:
			return (true, MouseButton.Forward);
		case KeyCode.Mouse4:
			return (true, MouseButton.Back);
		case KeyCode.Mouse5:
		case KeyCode.Mouse6:
			if (logWarning)
			{
				Debug.LogWarning($"The KeyCode: {keyCode}, lacks a proper counterpart in the Key enum. Returning Left Button.");
			}
			return (false, MouseButton.Left);
		default:
			return (false, MouseButton.Left);
		}
	}

	public static GamepadButton KeyCodeToGamepadButton(KeyCode keyCode)
	{
		if (keyCode < KeyCode.JoystickButton0 || keyCode > KeyCode.JoystickButton19)
		{
			return GamepadButton.South;
		}
		if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXServer || Application.platform == RuntimePlatform.OSXEditor)
		{
			switch (keyCode)
			{
			case KeyCode.JoystickButton5:
				return GamepadButton.DpadUp;
			case KeyCode.JoystickButton6:
				return GamepadButton.DpadDown;
			case KeyCode.JoystickButton7:
				return GamepadButton.DpadLeft;
			case KeyCode.JoystickButton8:
				return GamepadButton.DpadRight;
			case KeyCode.JoystickButton9:
				return GamepadButton.Start;
			}
		}
		switch (keyCode)
		{
		case KeyCode.JoystickButton0:
		case KeyCode.JoystickButton16:
			return GamepadButton.South;
		case KeyCode.JoystickButton1:
		case KeyCode.JoystickButton17:
			return GamepadButton.East;
		case KeyCode.JoystickButton2:
		case KeyCode.JoystickButton18:
			return GamepadButton.West;
		case KeyCode.JoystickButton3:
		case KeyCode.JoystickButton19:
			return GamepadButton.North;
		case KeyCode.JoystickButton4:
		case KeyCode.JoystickButton13:
			return GamepadButton.LeftShoulder;
		case KeyCode.JoystickButton5:
		case KeyCode.JoystickButton14:
			return GamepadButton.RightShoulder;
		case KeyCode.JoystickButton6:
		case KeyCode.JoystickButton10:
			return GamepadButton.Select;
		case KeyCode.JoystickButton7:
			return GamepadButton.Start;
		case KeyCode.JoystickButton9:
			return GamepadButton.RightStick;
		case KeyCode.JoystickButton8:
		case KeyCode.JoystickButton11:
			return GamepadButton.LeftStick;
		case KeyCode.JoystickButton12:
			return GamepadButton.RightStick;
		default:
			return GamepadButton.South;
		}
	}

	public static string KeyCodeToString(KeyCode keyCode)
	{
		if (keyCode >= KeyCode.JoystickButton0 && keyCode <= KeyCode.JoystickButton19)
		{
			if (Gamepad.current != null)
			{
				return Gamepad.current[KeyCodeToGamepadButton(keyCode)].displayName;
			}
			return keyCode.ToString();
		}
		if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse4)
		{
			if (Mouse.current == null)
			{
				return keyCode.ToString();
			}
			return KeyCodeToMouseButton(keyCode).Item2 switch
			{
				MouseButton.Left => Mouse.current.leftButton.displayName, 
				MouseButton.Right => Mouse.current.rightButton.displayName, 
				MouseButton.Middle => Mouse.current.middleButton.displayName, 
				MouseButton.Forward => Mouse.current.forwardButton.displayName, 
				MouseButton.Back => Mouse.current.backButton.displayName, 
				_ => keyCode.ToString(), 
			};
		}
		if (keyCode >= KeyCode.Mouse5 && keyCode <= KeyCode.Mouse6)
		{
			return keyCode.ToString();
		}
		if (Keyboard.current == null)
		{
			return keyCode.ToString();
		}
		Key key = KeyCodeToKey(keyCode);
		if (key != 0)
		{
			return Keyboard.current[key].displayName;
		}
		return keyCode.ToString();
	}
}
