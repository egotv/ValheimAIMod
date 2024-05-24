using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GUIFramework;
using TMPro;
using UnityEngine;

public abstract class Terminal : MonoBehaviour
{
	public class ConsoleEventArgs
	{
		public string[] Args;

		public string ArgsAll;

		public string FullLine;

		public Terminal Context;

		public int Length => Args.Length;

		public string this[int i] => Args[i];

		public ConsoleEventArgs(string line, Terminal context)
		{
			Context = context;
			FullLine = line;
			int num = line.IndexOf(' ');
			ArgsAll = ((num > 0) ? line.Substring(num + 1) : "");
			Args = line.Split(' ', StringSplitOptions.None);
		}

		public int TryParameterInt(int parameterIndex, int defaultValue = 1)
		{
			if (TryParameterInt(parameterIndex, out var value))
			{
				return value;
			}
			return defaultValue;
		}

		public bool TryParameterInt(int parameterIndex, out int value)
		{
			if (Args.Length <= parameterIndex || !int.TryParse(Args[parameterIndex], out value))
			{
				value = 0;
				return false;
			}
			return true;
		}

		public bool TryParameterLong(int parameterIndex, out long value)
		{
			if (Args.Length <= parameterIndex || !long.TryParse(Args[parameterIndex], out value))
			{
				value = 0L;
				return false;
			}
			return true;
		}

		public float TryParameterFloat(int parameterIndex, float defaultValue = 1f)
		{
			if (TryParameterFloat(parameterIndex, out var value))
			{
				return value;
			}
			return defaultValue;
		}

		public bool TryParameterFloat(int parameterIndex, out float value)
		{
			if (Args.Length <= parameterIndex || !float.TryParse(Args[parameterIndex].Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
			{
				value = 0f;
				return false;
			}
			return true;
		}

		public bool HasArgumentAnywhere(string value, int firstIndexToCheck = 0, bool toLower = true)
		{
			for (int i = firstIndexToCheck; i < Args.Length; i++)
			{
				if ((toLower && Args[i].ToLower() == value) || (!toLower && Args[i] == value))
				{
					return true;
				}
			}
			return false;
		}
	}

	public class ConsoleCommand
	{
		public string Command;

		public string Description;

		public bool IsCheat;

		public bool IsNetwork;

		public bool OnlyServer;

		public bool IsSecret;

		public bool AllowInDevBuild;

		public bool RemoteCommand;

		public bool OnlyAdmin;

		private ConsoleEventFailable actionFailable;

		private ConsoleEvent action;

		private ConsoleOptionsFetcher m_tabOptionsFetcher;

		private List<string> m_tabOptions;

		private bool m_alwaysRefreshTabOptions;

		public ConsoleCommand(string command, string description, ConsoleEventFailable action, bool isCheat = false, bool isNetwork = false, bool onlyServer = false, bool isSecret = false, bool allowInDevBuild = false, ConsoleOptionsFetcher optionsFetcher = null, bool alwaysRefreshTabOptions = false, bool remoteCommand = false, bool onlyAdmin = false)
		{
			commands[command.ToLower()] = this;
			Command = command;
			Description = description;
			actionFailable = action;
			IsCheat = isCheat;
			OnlyServer = onlyServer || onlyAdmin;
			IsSecret = isSecret;
			IsNetwork = isNetwork;
			AllowInDevBuild = allowInDevBuild;
			m_tabOptionsFetcher = optionsFetcher;
			m_alwaysRefreshTabOptions = alwaysRefreshTabOptions;
			RemoteCommand = remoteCommand;
			OnlyAdmin = onlyAdmin;
		}

		public ConsoleCommand(string command, string description, ConsoleEvent action, bool isCheat = false, bool isNetwork = false, bool onlyServer = false, bool isSecret = false, bool allowInDevBuild = false, ConsoleOptionsFetcher optionsFetcher = null, bool alwaysRefreshTabOptions = false, bool remoteCommand = false, bool onlyAdmin = false)
		{
			commands[command.ToLower()] = this;
			Command = command;
			Description = description;
			this.action = action;
			IsCheat = isCheat;
			OnlyServer = onlyServer;
			IsSecret = isSecret;
			IsNetwork = isNetwork;
			AllowInDevBuild = allowInDevBuild;
			m_tabOptionsFetcher = optionsFetcher;
			m_alwaysRefreshTabOptions = alwaysRefreshTabOptions;
			RemoteCommand = remoteCommand;
			OnlyAdmin = onlyAdmin;
		}

		public List<string> GetTabOptions()
		{
			if (m_tabOptionsFetcher != null && (m_tabOptions == null || m_alwaysRefreshTabOptions))
			{
				m_tabOptions = m_tabOptionsFetcher();
			}
			return m_tabOptions;
		}

		public void RunAction(ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				List<string> tabOptions = GetTabOptions();
				if (tabOptions != null)
				{
					foreach (string item in tabOptions)
					{
						if (item != null && args[1].ToLower() == item.ToLower())
						{
							args.Args[1] = item;
							break;
						}
					}
				}
			}
			if (action != null)
			{
				action(args);
			}
			else
			{
				object obj = actionFailable(args);
				if (obj is bool && !(bool)obj)
				{
					args.Context.AddString("<color=#8b0000>Error executing command. Check parameters and context.</color>\n   <color=#888888>" + Command + " - " + Description + "</color>");
				}
				if (obj is string text)
				{
					args.Context.AddString("<color=#8b0000>Error executing command: " + text + "</color>\n   <color=#888888>" + Command + " - " + Description + "</color>");
				}
			}
			if ((bool)Game.instance)
			{
				PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
				if (IsCheat)
				{
					playerProfile.m_usedCheats = true;
					playerProfile.IncrementStat(PlayerStatType.Cheats);
				}
				playerProfile.m_knownCommands.IncrementOrSet(args[0].ToLower());
			}
		}

		public bool ShowCommand(Terminal context)
		{
			if (!IsSecret)
			{
				if (!IsValid(context))
				{
					if ((bool)ZNet.instance && !ZNet.instance.IsServer())
					{
						return RemoteCommand;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		public bool IsValid(Terminal context, bool skipAllowedCheck = false)
		{
			if ((!IsCheat || context.IsCheatsEnabled()) && (context.isAllowedCommand(this) || skipAllowedCheck) && (!IsNetwork || (bool)ZNet.instance))
			{
				if (OnlyServer)
				{
					if ((bool)ZNet.instance)
					{
						return ZNet.instance.IsServer();
					}
					return false;
				}
				return true;
			}
			return false;
		}
	}

	public delegate object ConsoleEventFailable(ConsoleEventArgs args);

	public delegate void ConsoleEvent(ConsoleEventArgs args);

	public delegate List<string> ConsoleOptionsFetcher();

	private static bool m_terminalInitialized;

	protected static List<string> m_bindList;

	public static Dictionary<string, string> m_testList = new Dictionary<string, string>();

	protected static Dictionary<KeyCode, List<string>> m_binds = new Dictionary<KeyCode, List<string>>();

	private static bool m_cheat = false;

	public static bool m_showTests;

	protected float m_lastDebugUpdate;

	protected static Dictionary<string, ConsoleCommand> commands = new Dictionary<string, ConsoleCommand>();

	public static ConcurrentQueue<string> m_threadSafeMessages = new ConcurrentQueue<string>();

	public static ConcurrentQueue<string> m_threadSafeConsoleLog = new ConcurrentQueue<string>();

	protected char m_tabPrefix;

	protected bool m_autoCompleteSecrets;

	private List<string> m_history = new List<string>();

	protected string[] m_quickSelect = new string[4];

	private List<string> m_tabOptions = new List<string>();

	private int m_historyPosition;

	private int m_tabCaretPosition = -1;

	private int m_tabCaretPositionEnd;

	private int m_tabLength;

	private int m_tabIndex;

	private List<string> m_commandList = new List<string>();

	private List<Minimap.PinData> m_findPins = new List<Minimap.PinData>();

	protected bool m_focused;

	public RectTransform m_chatWindow;

	public TextMeshProUGUI m_output;

	public GuiInputField m_input;

	public TMP_Text m_search;

	private int m_lastSearchLength;

	private List<string> m_lastSearch = new List<string>();

	protected List<string> m_chatBuffer = new List<string>();

	protected const int m_maxBufferLength = 300;

	public int m_maxVisibleBufferLength = 30;

	private const int m_maxScrollHeight = 5;

	private int m_scrollHeight;

	protected abstract Terminal m_terminalInstance { get; }

	private static void InitTerminal()
	{
		if (m_terminalInitialized)
		{
			return;
		}
		m_terminalInitialized = true;
		AddConsoleCheatCommands();
		new ConsoleCommand("help", "Shows a list of console commands (optional: help 2 4 shows the second quarter)", delegate(ConsoleEventArgs args)
		{
			if ((bool)ZNet.instance && ZNet.instance.IsServer())
			{
				_ = (bool)Player.m_localPlayer;
			}
			else
				_ = 0;
			args.Context.IsCheatsEnabled();
			List<string> list17 = new List<string>();
			foreach (KeyValuePair<string, ConsoleCommand> command in commands)
			{
				if (command.Value.ShowCommand(args.Context))
				{
					list17.Add(command.Value.Command + " - " + command.Value.Description);
				}
			}
			list17.Sort();
			if (args.Context != null)
			{
				int num35 = args.TryParameterInt(2, 5);
				if (!args.TryParameterInt(1, out var value21))
				{
					foreach (string item2 in list17)
					{
						args.Context.AddString(item2);
					}
					return;
				}
				int num36 = list17.Count / num35;
				for (int num37 = num36 * (value21 - 1); num37 < Mathf.Min(list17.Count, num36 * (value21 - 1) + num36); num37++)
				{
					args.Context.AddString(list17[num37]);
				}
			}
		});
		new ConsoleCommand("devcommands", "enables cheats", delegate(ConsoleEventArgs args)
		{
			if ((bool)ZNet.instance && !ZNet.instance.IsServer())
			{
				ZNet.instance.RemoteCommand("devcommands");
			}
			m_cheat = !m_cheat;
			args.Context?.AddString("Dev commands: " + m_cheat);
			args.Context?.AddString("WARNING: using any dev commands is not recommended and is done at your own risk.");
			Gogan.LogEvent("Cheat", "CheatsEnabled", m_cheat.ToString(), 0L);
			args.Context.updateCommandList();
		}, isCheat: false, isNetwork: false, onlyServer: false, isSecret: true);
		new ConsoleCommand("hidebetatext", "", delegate
		{
			if ((bool)Hud.instance)
			{
				Hud.instance.ToggleBetaTextVisible();
			}
		}, isCheat: false, isNetwork: false, onlyServer: false, isSecret: true);
		new ConsoleCommand("ping", "ping server", delegate
		{
			if ((bool)Game.instance)
			{
				Game.instance.Ping();
			}
		});
		new ConsoleCommand("dpsdebug", "toggle dps debug print", delegate(ConsoleEventArgs args)
		{
			Character.SetDPSDebug(!Character.IsDPSDebugEnabled());
			args.Context?.AddString("DPS debug " + Character.IsDPSDebugEnabled());
		}, isCheat: true);
		new ConsoleCommand("lodbias", "set distance lod bias", delegate(ConsoleEventArgs args)
		{
			float value20;
			if (args.Length == 1)
			{
				args.Context.AddString("Lod bias:" + QualitySettings.lodBias);
			}
			else if (args.TryParameterFloat(1, out value20))
			{
				args.Context.AddString("Setting lod bias:" + value20);
				QualitySettings.lodBias = value20;
			}
		});
		new ConsoleCommand("info", "print system info", delegate(ConsoleEventArgs args)
		{
			args.Context.AddString("Render threading mode:" + SystemInfo.renderingThreadingMode);
			long totalMemory3 = GC.GetTotalMemory(forceFullCollection: false);
			args.Context.AddString("Total allocated mem: " + (totalMemory3 / 1048576).ToString("0") + "mb");
		});
		new ConsoleCommand("gc", "shows garbage collector information", delegate(ConsoleEventArgs args)
		{
			long totalMemory = GC.GetTotalMemory(forceFullCollection: false);
			GC.Collect();
			long totalMemory2 = GC.GetTotalMemory(forceFullCollection: true);
			long num34 = totalMemory2 - totalMemory;
			args.Context.AddString("GC collect, Delta: " + (num34 / 1048576).ToString("0") + "mb   Total left:" + (totalMemory2 / 1048576).ToString("0") + "mb");
		}, isCheat: true);
		new ConsoleCommand("cr", "unloads unused assets", delegate(ConsoleEventArgs args)
		{
			args.Context.AddString("Unloading unused assets");
			Game.instance.CollectResources(displayMessage: true);
		}, isCheat: true);
		new ConsoleCommand("fov", "changes camera field of view", delegate(ConsoleEventArgs args)
		{
			Camera mainCamera = Utils.GetMainCamera();
			if ((bool)mainCamera)
			{
				float value19;
				if (args.Length == 1)
				{
					args.Context.AddString("Fov:" + mainCamera.fieldOfView);
				}
				else if (args.TryParameterFloat(1, out value19) && value19 > 5f)
				{
					args.Context.AddString("Setting fov to " + value19);
					Camera[] componentsInChildren2 = mainCamera.GetComponentsInChildren<Camera>();
					for (int num33 = 0; num33 < componentsInChildren2.Length; num33++)
					{
						componentsInChildren2[num33].fieldOfView = value19;
					}
				}
			}
		});
		new ConsoleCommand("kick", "[name/ip/userID] - kick user", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string user3 = args[1];
			ZNet.instance.Kick(user3);
			return true;
		}, isCheat: false, isNetwork: true);
		new ConsoleCommand("ban", "[name/ip/userID] - ban user", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string user2 = args[1];
			ZNet.instance.Ban(user2);
			return true;
		}, isCheat: false, isNetwork: true);
		new ConsoleCommand("unban", "[ip/userID] - unban user", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string user = args[1];
			ZNet.instance.Unban(user);
			return true;
		}, isCheat: false, isNetwork: true);
		new ConsoleCommand("banned", "list banned users", delegate
		{
			ZNet.instance.PrintBanned();
		}, isCheat: false, isNetwork: true);
		new ConsoleCommand("save", "force saving of world and resets world save interval", delegate
		{
			ZNet.instance.SaveWorldAndPlayerProfiles();
		}, isCheat: false, isNetwork: true);
		new ConsoleCommand("optterrain", "optimize old terrain modifications", delegate
		{
			TerrainComp.UpgradeTerrain();
			Heightmap.UpdateTerrainAlpha();
		}, isCheat: false, isNetwork: true);
		new ConsoleCommand("genloc", "regenerate all locations.", delegate
		{
			ZoneSystem.instance.GenerateLocations();
		}, isCheat: false, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("players", "[nr] - force diffuculty scale ( 0 = reset)", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			if (args.TryParameterInt(1, out var value18))
			{
				Game.instance.SetForcePlayerDifficulty(value18);
				args.Context.AddString("Setting players to " + value18);
			}
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("exclusivefullscreen", "changes window mode to exclusive fullscreen, or back to borderless", delegate
		{
			if (Screen.fullScreenMode != 0)
			{
				Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
			}
			else
			{
				Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
			}
		});
		new ConsoleCommand("setkey", "[name]", delegate(ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				ZoneSystem.instance.SetGlobalKey(args.FullLine.Substring(args[0].Length + 1));
				args.Context.AddString("Setting global key " + args[1]);
			}
			else
			{
				args.Context.AddString("Syntax: setkey [key]");
			}
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, delegate
		{
			List<string> list16 = Enum.GetNames(typeof(GlobalKeys)).ToList();
			list16.Remove(GlobalKeys.NonServerOption.ToString());
			return list16;
		}, alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("removekey", "[name]", delegate(ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				ZoneSystem.instance.RemoveGlobalKey(args.FullLine.Substring(args[0].Length + 1));
				args.Context.AddString("Removing global key " + args[1]);
			}
			else
			{
				args.Context.AddString("Syntax: setkey [key]");
			}
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, () => (!ZoneSystem.instance) ? null : ZoneSystem.instance.GetGlobalKeys(), alwaysRefreshTabOptions: true, remoteCommand: true);
		new ConsoleCommand("resetkeys", "[name]", delegate(ConsoleEventArgs args)
		{
			ZoneSystem.instance.ResetGlobalKeys();
			Player.m_localPlayer?.ResetUniqueKeys();
			args.Context.AddString("Global and player keys cleared");
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("resetworldkeys", "[name] Resets all world modifiers to default", delegate(ConsoleEventArgs args)
		{
			ZoneSystem.instance.ResetWorldKeys();
			args.Context.AddString("Server keys cleared");
		}, isCheat: false, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("setworldpreset", "[name] Resets all world modifiers to a named preset", delegate(ConsoleEventArgs args)
		{
			if (!Enum.TryParse<WorldPresets>(args[1], ignoreCase: true, out var result10))
			{
				return "Invalid preset";
			}
			ZoneSystem.instance.ResetWorldKeys();
			ServerOptionsGUI.m_instance.ReadKeys(ZNet.World);
			ServerOptionsGUI.m_instance.SetPreset(ZNet.World, result10);
			ServerOptionsGUI.m_instance.SetKeys(ZNet.World);
			return true;
		}, isCheat: false, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, () => Enum.GetNames(typeof(WorldPresets)).ToList(), alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("setworldmodifier", "[name] [value] Sets a world modifier value", delegate(ConsoleEventArgs args)
		{
			if (!Enum.TryParse<WorldModifiers>(args[1], ignoreCase: true, out var result8) || !Enum.TryParse<WorldModifierOption>(args[2], ignoreCase: true, out var result9))
			{
				return "Invalid input, possible valid values are: " + string.Join(", ", Enum.GetNames(typeof(WorldModifierOption)));
			}
			ServerOptionsGUI.m_instance.ReadKeys(ZNet.World);
			ServerOptionsGUI.m_instance.SetPreset(ZNet.World, result8, result9);
			ServerOptionsGUI.m_instance.SetKeys(ZNet.World);
			return true;
		}, isCheat: false, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, () => Enum.GetNames(typeof(WorldModifiers)).ToList(), alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("setkeyplayer", "[name]", delegate(ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				Player.m_localPlayer.AddUniqueKey(args.FullLine.Substring(args[0].Length + 1));
				args.Context.AddString("Setting player key " + args[1]);
			}
			else
			{
				args.Context.AddString("Syntax: setkey [key]");
			}
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, () => Enum.GetNames(typeof(PlayerKeys)).ToList());
		new ConsoleCommand("removekeyplayer", "[name]", delegate(ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				Player.m_localPlayer.RemoveUniqueKey(args.FullLine.Substring(args[0].Length + 1));
				args.Context.AddString("Removing player key " + args[1]);
			}
			else
			{
				args.Context.AddString("Syntax: setkey [key]");
			}
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, () => (!Player.m_localPlayer) ? null : Player.m_localPlayer.GetUniqueKeys(), alwaysRefreshTabOptions: true);
		new ConsoleCommand("listkeys", "", delegate(ConsoleEventArgs args)
		{
			List<string> globalKeys = ZoneSystem.instance.GetGlobalKeys();
			args.Context.AddString($"Current Keys: {globalKeys.Count}");
			foreach (string item3 in globalKeys)
			{
				args.Context.AddString("  " + item3);
			}
			args.Context.AddString($"Server Option Keys: {ZNet.World.m_startingGlobalKeys.Count}");
			foreach (string startingGlobalKey in ZNet.World.m_startingGlobalKeys)
			{
				args.Context.AddString("  " + startingGlobalKey);
			}
			if (args.Length > 2)
			{
				args.Context.AddString($"Current Keys Values: {globalKeys.Count}");
				foreach (KeyValuePair<string, string> globalKeysValue in ZoneSystem.instance.m_globalKeysValues)
				{
					args.Context.AddString("  " + globalKeysValue.Key + ": " + globalKeysValue.Value);
				}
				args.Context.AddString($"Current Keys Enums: {globalKeys.Count}");
				foreach (GlobalKeys globalKeysEnum in ZoneSystem.instance.m_globalKeysEnums)
				{
					args.Context.AddString($"  {globalKeysEnum}");
				}
			}
			if ((bool)Player.m_localPlayer)
			{
				globalKeys = Player.m_localPlayer.GetUniqueKeys();
				args.Context.AddString($"Player Keys: {globalKeys.Count}");
				foreach (string item4 in globalKeys)
				{
					args.Context.AddString("  " + item4);
				}
			}
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("debugmode", "fly mode", delegate(ConsoleEventArgs args)
		{
			Player.m_debugMode = !Player.m_debugMode;
			args.Context.AddString("Debugmode " + Player.m_debugMode);
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("fly", "fly mode", delegate(ConsoleEventArgs args)
		{
			Player.m_localPlayer.ToggleDebugFly();
			if (args.TryParameterInt(1, out var value17))
			{
				Character.m_debugFlySpeed = value17;
			}
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("nocost", "no build cost", delegate(ConsoleEventArgs args)
		{
			if (args.HasArgumentAnywhere("on"))
			{
				Player.m_localPlayer.SetNoPlacementCost(value: true);
			}
			else if (args.HasArgumentAnywhere("off"))
			{
				Player.m_localPlayer.SetNoPlacementCost(value: false);
			}
			else
			{
				Player.m_localPlayer.ToggleNoPlacementCost();
			}
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("raiseskill", "[skill] [amount]", delegate(ConsoleEventArgs args)
		{
			if (args.TryParameterInt(2, out var value16))
			{
				Player.m_localPlayer.GetSkills().CheatRaiseSkill(args[1], value16);
			}
			else
			{
				args.Context.AddString("Syntax: raiseskill [skill] [amount]");
			}
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, delegate
		{
			List<string> list15 = Enum.GetNames(typeof(Skills.SkillType)).ToList();
			list15.Remove(Skills.SkillType.All.ToString());
			list15.Remove(Skills.SkillType.None.ToString());
			return list15;
		});
		new ConsoleCommand("resetskill", "[skill]", delegate(ConsoleEventArgs args)
		{
			if (args.Length > 1)
			{
				string text14 = args[1];
				Player.m_localPlayer.GetSkills().CheatResetSkill(text14);
			}
			else
			{
				args.Context.AddString("Syntax: resetskill [skill]");
			}
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, delegate
		{
			List<string> list14 = Enum.GetNames(typeof(Skills.SkillType)).ToList();
			list14.Remove(Skills.SkillType.All.ToString());
			list14.Remove(Skills.SkillType.None.ToString());
			return list14;
		});
		new ConsoleCommand("sleep", "skips to next morning", delegate
		{
			EnvMan.instance.SkipToMorning();
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("stats", "shows player stats", delegate(ConsoleEventArgs args)
		{
			if ((bool)Game.instance)
			{
				PlayerProfile playerProfile2 = Game.instance.GetPlayerProfile();
				args.Context.AddString("Player stats");
				if (playerProfile2.m_usedCheats)
				{
					args.Context.AddString("Cheater!");
				}
				foreach (KeyValuePair<PlayerStatType, float> stat in playerProfile2.m_playerStats.m_stats)
				{
					if (PlayerProfile.m_statTypeDates.TryGetValue(stat.Key, out var value15))
					{
						args.Context.AddString("  " + value15);
					}
					args.Context.AddString($"    {stat.Key}: {stat.Value}");
				}
				args.Context.AddString("Known worlds:");
				foreach (KeyValuePair<string, float> knownWorld in playerProfile2.m_knownWorlds)
				{
					args.Context.AddString("  " + knownWorld.Key + ": " + TimeSpan.FromSeconds(knownWorld.Value).ToString("c"));
				}
				args.Context.AddString("Enemies:");
				foreach (KeyValuePair<string, float> enemyStat in playerProfile2.m_enemyStats)
				{
					args.Context.AddString($"  {Localization.instance.Localize(enemyStat.Key)}: {enemyStat.Value}");
				}
				args.Context.AddString("Items found:");
				foreach (KeyValuePair<string, float> itemPickupStat in playerProfile2.m_itemPickupStats)
				{
					args.Context.AddString($"  {Localization.instance.Localize(itemPickupStat.Key)}: {itemPickupStat.Value}");
				}
				args.Context.AddString("Crafts:");
				foreach (KeyValuePair<string, float> itemCraftStat in playerProfile2.m_itemCraftStats)
				{
					args.Context.AddString($"  {Localization.instance.Localize(itemCraftStat.Key)}: {itemCraftStat.Value}");
				}
				if (args.Length > 1)
				{
					args.Context.AddString("Known world keys:");
					foreach (KeyValuePair<string, float> knownWorldKey in playerProfile2.m_knownWorldKeys)
					{
						args.Context.AddString("  " + knownWorldKey.Key + ": " + TimeSpan.FromSeconds(knownWorldKey.Value).ToString("c"));
					}
					args.Context.AddString("Used commands:");
					foreach (KeyValuePair<string, float> knownCommand in playerProfile2.m_knownCommands)
					{
						args.Context.AddString($"  {knownCommand.Key}: {knownCommand.Value}");
					}
				}
			}
		}, isCheat: false, isNetwork: false, onlyServer: true);
		new ConsoleCommand("skiptime", "[gameseconds] skips head in seconds", delegate(ConsoleEventArgs args)
		{
			double timeSeconds2 = ZNet.instance.GetTimeSeconds();
			float num32 = args.TryParameterFloat(1, 240f);
			timeSeconds2 += (double)num32;
			ZNet.instance.SetNetTime(timeSeconds2);
			args.Context.AddString("Skipping " + num32.ToString("0") + "s , Day:" + EnvMan.instance.GetDay(timeSeconds2));
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("time", "shows current time", delegate(ConsoleEventArgs args)
		{
			double timeSeconds = ZNet.instance.GetTimeSeconds();
			bool flag2 = EnvMan.CanSleep();
			args.Context.AddString(string.Format("{0} sec, Day: {1} ({2}), {3}, Session start: {4}", timeSeconds.ToString("0.00"), EnvMan.instance.GetDay(timeSeconds), EnvMan.instance.GetDayFraction().ToString("0.00"), flag2 ? "Can sleep" : "Can NOT sleep", ZoneSystem.instance.TimeSinceStart()));
		}, isCheat: true);
		new ConsoleCommand("maxfps", "[FPS] sets fps limit", delegate(ConsoleEventArgs args)
		{
			if (args.TryParameterInt(1, out var value14))
			{
				Settings.FPSLimit = value14;
				PlatformPrefs.SetInt("FPSLimit", value14);
				return true;
			}
			return false;
		});
		new ConsoleCommand("resetcharacter", "reset character data", delegate(ConsoleEventArgs args)
		{
			args.Context?.AddString("Reseting character");
			Player.m_localPlayer.ResetCharacter();
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("resetknownitems", "reset character known items & recipes", delegate(ConsoleEventArgs args)
		{
			args.Context?.AddString("Reseting known items for character");
			Player.m_localPlayer.ResetCharacterKnownItems();
		});
		new ConsoleCommand("tutorialreset", "reset tutorial data", delegate(ConsoleEventArgs args)
		{
			args.Context?.AddString("Reseting tutorials");
			Player.ResetSeenTutorials();
		});
		new ConsoleCommand("timescale", "[target] [fadetime, default: 1, max: 3] sets timescale", delegate(ConsoleEventArgs args)
		{
			if (args.TryParameterFloat(1, out var value13))
			{
				Game.FadeTimeScale(Mathf.Min(5f, value13), args.TryParameterFloat(2, 0f));
				return true;
			}
			return false;
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("randomevent", "start a random event", delegate
		{
			RandEventSystem.instance.StartRandomEvent();
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("event", "[name] - start event", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string text13 = args[1];
			if (!RandEventSystem.instance.HaveEvent(text13))
			{
				args.Context.AddString("Random event not found:" + text13);
				return true;
			}
			RandEventSystem.instance.SetRandomEventByName(text13, Player.m_localPlayer.transform.position);
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: false, delegate
		{
			List<string> list13 = new List<string>();
			foreach (RandomEvent @event in RandEventSystem.instance.m_events)
			{
				list13.Add(@event.m_name);
			}
			return list13;
		});
		new ConsoleCommand("stopevent", "stop current event", delegate
		{
			RandEventSystem.instance.ResetRandomEvent();
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("removedrops", "remove all item-drops in area", delegate
		{
			int num30 = 0;
			ItemDrop[] array19 = UnityEngine.Object.FindObjectsOfType<ItemDrop>();
			foreach (ItemDrop itemDrop in array19)
			{
				Fish component16 = itemDrop.gameObject.GetComponent<Fish>();
				if (!component16 || component16.IsOutOfWater())
				{
					ZNetView component17 = itemDrop.GetComponent<ZNetView>();
					if ((bool)component17 && component17.IsValid() && component17.IsOwner())
					{
						component17.Destroy();
						num30++;
					}
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Removed item drops: " + num30);
		}, isCheat: true);
		new ConsoleCommand("removefish", "remove all fish", delegate
		{
			int num28 = 0;
			Fish[] array18 = UnityEngine.Object.FindObjectsOfType<Fish>();
			for (int num29 = 0; num29 < array18.Length; num29++)
			{
				ZNetView component15 = array18[num29].GetComponent<ZNetView>();
				if ((bool)component15 && component15.IsValid() && component15.IsOwner())
				{
					component15.Destroy();
					num28++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Removed fish: " + num28);
		}, isCheat: true);
		new ConsoleCommand("printcreatures", "shows counts and levels of active creatures", delegate(ConsoleEventArgs args)
		{
			Dictionary<string, Dictionary<int, int>> counts2 = new Dictionary<string, Dictionary<int, int>>();
			GetInfo(Character.GetAllCharacters());
			GetInfo(UnityEngine.Object.FindObjectsOfType<RandomFlyingBird>());
			GetInfo(UnityEngine.Object.FindObjectsOfType<Fish>());
			foreach (KeyValuePair<string, Dictionary<int, int>> item5 in counts2)
			{
				string text12 = Localization.instance.Localize(item5.Key) + ": ";
				foreach (KeyValuePair<int, int> item6 in item5.Value)
				{
					text12 += $"Level {item6.Key}: {item6.Value}, ";
				}
				args.Context.AddString(text12);
			}
			void GetInfo(IEnumerable collection)
			{
				foreach (object item7 in collection)
				{
					if (item7 is Character character)
					{
						count(character.m_name, character.GetLevel());
					}
					else if (item7 is RandomFlyingBird)
					{
						count("Bird", 1);
					}
					else if (item7 is Fish fish)
					{
						ItemDrop component14 = fish.GetComponent<ItemDrop>();
						if ((object)component14 != null)
						{
							count(component14.m_itemData.m_shared.m_name, component14.m_itemData.m_quality, component14.m_itemData.m_stack);
						}
					}
				}
				foreach (object item8 in collection)
				{
					if (item8 is MonoBehaviour monoBehaviour)
					{
						args.Context.AddString(string.Format("   {0}, Dist: {1}, Offset: {2}", monoBehaviour.name, Vector3.Distance(Player.m_localPlayer.transform.position, monoBehaviour.transform.position).ToString("0.0"), monoBehaviour.transform.position - Player.m_localPlayer.transform.position));
					}
				}
			}
		}, isCheat: true);
		new ConsoleCommand("printnetobj", "[radius = 5] lists number of network objects by name surrounding the player", delegate(ConsoleEventArgs args)
		{
			float num25 = args.TryParameterFloat(1, 5f);
			ZNetView[] array16 = UnityEngine.Object.FindObjectsOfType<ZNetView>();
			Dictionary<string, int> counts = new Dictionary<string, int>();
			int total = 0;
			ZNetView[] array17 = array16;
			foreach (ZNetView zNetView in array17)
			{
				Transform transform = ((zNetView.transform.parent != null) ? zNetView.transform.parent : zNetView.transform);
				if (!(num25 > 0f) || !(Vector3.Distance(transform.position, Player.m_localPlayer.transform.position) > num25))
				{
					string text11 = transform.name;
					int num27 = text11.IndexOf('(');
					if (num27 > 0)
					{
						add(text11.Substring(0, num27));
					}
					else
					{
						add("Other");
					}
				}
			}
			args.Context.AddString($"Total network objects found: {total}");
			foreach (KeyValuePair<string, int> item9 in counts)
			{
				args.Context.AddString($"   {item9.Key}: {item9.Value}");
			}
			void add(string key)
			{
				total++;
				if (counts.TryGetValue(key, out var value12))
				{
					counts[key] = value12 + 1;
				}
				else
				{
					counts[key] = 1;
				}
			}
		}, isCheat: true);
		new ConsoleCommand("removebirds", "remove all birds", delegate
		{
			int num23 = 0;
			RandomFlyingBird[] array15 = UnityEngine.Object.FindObjectsOfType<RandomFlyingBird>();
			for (int num24 = 0; num24 < array15.Length; num24++)
			{
				ZNetView component13 = array15[num24].GetComponent<ZNetView>();
				if ((bool)component13 && component13.IsValid() && component13.IsOwner())
				{
					component13.Destroy();
					num23++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Removed birds: " + num23);
		}, isCheat: true);
		new ConsoleCommand("printlocations", "shows counts of loaded locations", delegate(ConsoleEventArgs args)
		{
			new Dictionary<string, Dictionary<int, int>>();
			Location[] array14 = UnityEngine.Object.FindObjectsOfType<Location>();
			foreach (Location location in array14)
			{
				args.Context.AddString(string.Format("   {0}, Dist: {1}, Offset: {2}", location.name, Vector3.Distance(Player.m_localPlayer.transform.position, location.transform.position).ToString("0.0"), location.transform.position - Player.m_localPlayer.transform.position));
			}
		}, isCheat: true);
		new ConsoleCommand("find", "[text] [pingmax] searches loaded objects and location list matching name and pings them on the map. pingmax defaults to 1, if more will place pins on map instead", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string text10 = args[1].ToLower();
			List<Tuple<object, Vector3>> list12 = find(text10);
			list12.Sort((Tuple<object, Vector3> a, Tuple<object, Vector3> b) => Vector3.Distance(a.Item2, Player.m_localPlayer.transform.position).CompareTo(Vector3.Distance(b.Item2, Player.m_localPlayer.transform.position)));
			foreach (Tuple<object, Vector3> item10 in list12)
			{
				args.Context.AddString(string.Format("   {0}, Dist: {1}, Pos: {2}", (item10.Item1 is GameObject gameObject4) ? gameObject4.name.ToString() : ((item10.Item1 is ZoneSystem.LocationInstance locationInstance) ? locationInstance.m_location.m_prefab.Name : "unknown"), Vector3.Distance(Player.m_localPlayer.transform.position, item10.Item2).ToString("0.0"), item10.Item2));
			}
			foreach (Minimap.PinData findPin in args.Context.m_findPins)
			{
				Minimap.instance.RemovePin(findPin);
			}
			args.Context.m_findPins.Clear();
			int num20 = Math.Min(list12.Count, args.TryParameterInt(2));
			if (num20 == 1)
			{
				Chat.instance.SendPing(list12[0].Item2);
			}
			else
			{
				for (int num21 = 0; num21 < num20; num21++)
				{
					args.Context.m_findPins.Add(Minimap.instance.AddPin(list12[num21].Item2, (list12[num21].Item1 is ZDO) ? Minimap.PinType.Icon2 : ((list12[num21].Item1 is ZoneSystem.LocationInstance) ? Minimap.PinType.Icon1 : Minimap.PinType.Icon3), (list12[num21].Item1 is ZDO zDO) ? zDO.GetString("tag") : "", save: false, isChecked: true, Player.m_localPlayer.GetPlayerID()));
				}
			}
			args.Context.AddString($"Found {list12.Count} objects containing '{text10}'");
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, findOpt);
		new ConsoleCommand("findtp", "[text] [index=-1] [closerange=30] searches loaded objects and location list matching name and teleports you to the closest one outside of closerange. Specify an index to tp to any other in the found list, a minus value means index by closest.", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2 || Player.m_localPlayer == null)
			{
				return false;
			}
			string text9 = args[1].ToLower();
			if (text9.Length < 1)
			{
				args.Context.AddString("You must specify a search query");
				return false;
			}
			List<Tuple<object, Vector3>> list11 = find(text9);
			int num17 = args.TryParameterInt(2, -1);
			if (num17 < 0)
			{
				list11.Sort((Tuple<object, Vector3> a, Tuple<object, Vector3> b) => Vector3.Distance(a.Item2, Player.m_localPlayer.transform.position).CompareTo(Vector3.Distance(b.Item2, Player.m_localPlayer.transform.position)));
				num17 *= -1;
				num17--;
			}
			num17 = Math.Min(list11.Count - 1, num17);
			if (list11.Count > 0)
			{
				int num18 = args.TryParameterInt(3, 30);
				for (int num19 = num17; num19 < list11.Count; num19++)
				{
					if (!(Vector3.Distance(Player.m_localPlayer.transform.position, list11[num19].Item2) < (float)num18))
					{
						Player.m_localPlayer.TeleportTo(list11[num19].Item2, Player.m_localPlayer.transform.rotation, distantTeleport: true);
					}
				}
			}
			args.Context.AddString($"Found {list11.Count} objects containing '{text9}'");
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, findOpt);
		new ConsoleCommand("setfuel", "[amount=10] Sets all light fuel to specified amount", delegate(ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			UnityEngine.Object[] array12 = UnityEngine.Object.FindObjectsOfType(typeof(Fireplace));
			int num15 = args.TryParameterInt(1, 10);
			UnityEngine.Object[] array13 = array12;
			for (int num16 = 0; num16 < array13.Length; num16++)
			{
				((Fireplace)array13[num16]).SetFuel(num15);
			}
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("freefly", "freefly photo mode", delegate(ConsoleEventArgs args)
		{
			args.Context.AddString("Toggling free fly camera");
			GameCamera.instance.ToggleFreeFly();
		}, isCheat: true);
		new ConsoleCommand("ffsmooth", "freefly smoothness", delegate(ConsoleEventArgs args)
		{
			if (args.Length <= 1)
			{
				args.Context.AddString(GameCamera.instance.GetFreeFlySmoothness().ToString());
				return true;
			}
			if (args.TryParameterFloat(1, out var value11))
			{
				args.Context.AddString("Setting free fly camera smoothing:" + value11);
				GameCamera.instance.SetFreeFlySmoothness(value11);
				return true;
			}
			return false;
		}, isCheat: true);
		new ConsoleCommand("location", "[SAVE*] spawn location (CAUTION: saving permanently disabled, *unless you specify SAVE)", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			string text8 = args[1];
			Vector3 pos2 = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 10f;
			ZoneSystem.instance.TestSpawnLocation(text8, pos2, args.Length < 3 || args[2] != "SAVE");
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, delegate
		{
			List<string> list10 = new List<string>();
			foreach (ZoneSystem.ZoneLocation location2 in ZoneSystem.instance.m_locations)
			{
				if (location2.m_prefab.IsValid)
				{
					list10.Add(location2.m_prefab.Name);
				}
			}
			return list10;
		}, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("vegetation", "spawn vegetation", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			Vector3 p = Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f;
			string text7 = args[1].ToLower();
			foreach (ZoneSystem.ZoneVegetation item11 in ZoneSystem.instance.m_vegetation)
			{
				if (item11.m_prefab.name.ToLower() == text7)
				{
					float y = UnityEngine.Random.Range(0, 360);
					float num13 = UnityEngine.Random.Range(item11.m_scaleMin, item11.m_scaleMax);
					float x = UnityEngine.Random.Range(0f - item11.m_randTilt, item11.m_randTilt);
					float z = UnityEngine.Random.Range(0f - item11.m_randTilt, item11.m_randTilt);
					ZoneSystem.instance.GetGroundData(ref p, out var normal, out var _, out var _, out var _);
					if (item11.m_snapToStaticSolid && ZoneSystem.instance.GetStaticSolidHeight(p, out var height, out var normal2))
					{
						p.y = height;
						normal = normal2;
					}
					if (item11.m_snapToWater)
					{
						p.y = 30f;
					}
					p.y += item11.m_groundOffset;
					Quaternion identity = Quaternion.identity;
					if (item11.m_chanceToUseGroundTilt > 0f && UnityEngine.Random.value <= item11.m_chanceToUseGroundTilt)
					{
						Quaternion quaternion = Quaternion.Euler(0f, y, 0f);
						identity = Quaternion.LookRotation(Vector3.Cross(normal, quaternion * Vector3.forward), normal);
					}
					else
					{
						identity = Quaternion.Euler(x, y, z);
					}
					GameObject obj3 = UnityEngine.Object.Instantiate(item11.m_prefab, p, identity);
					obj3.GetComponent<ZNetView>().SetLocalScale(new Vector3(num13, num13, num13));
					Collider[] componentsInChildren = obj3.GetComponentsInChildren<Collider>();
					foreach (Collider obj4 in componentsInChildren)
					{
						obj4.enabled = false;
						obj4.enabled = true;
					}
					return true;
				}
			}
			return "No vegeration prefab named '" + args[1] + "' found";
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, delegate
		{
			List<string> list9 = new List<string>();
			foreach (ZoneSystem.ZoneVegetation item12 in ZoneSystem.instance.m_vegetation)
			{
				if (item12.m_prefab != null)
				{
					list9.Add(item12.m_prefab.name);
				}
			}
			return list9;
		}, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("nextseed", "forces the next dungeon to a seed (CAUTION: saving permanently disabled)", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return true;
			}
			if (args.TryParameterInt(1, out var value10))
			{
				DungeonGenerator.m_forceSeed = value10;
				ZoneSystem.instance.m_didZoneTest = true;
				MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Location seed set, world saving DISABLED until restart");
			}
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("spawn", "[amount] [level] [radius] [p/e/i] - spawn something. (End word with a star (*) to create each object containing that word.) Add a 'p' after to try to pick up the spawned items, adding 'e' will try to use/equip, 'i' will only spawn and pickup if you don't have one in your inventory.", delegate(ConsoleEventArgs args)
		{
			if (args.Length <= 1 || !ZNetScene.instance)
			{
				return false;
			}
			string text4 = args[1];
			int count = args.TryParameterInt(2);
			int level2 = args.TryParameterInt(3);
			float radius = args.TryParameterFloat(4, 0.5f);
			args.TryParameterInt(5, -1);
			bool pickup = args.HasArgumentAnywhere("p", 2);
			bool use = args.HasArgumentAnywhere("e", 2);
			bool onlyIfMissing = args.HasArgumentAnywhere("i", 2);
			Dictionary<string, object> vals = null;
			string[] args2 = args.Args;
			foreach (string text5 in args2)
			{
				if (text5.Contains("::"))
				{
					string[] array9 = text5.Split(new string[1] { "::" }, StringSplitOptions.None);
					string[] array10 = array9[0].Split('.', StringSplitOptions.None);
					if (array9.Length >= 2 && array10.Length >= 2)
					{
						if (vals == null)
						{
							vals = new Dictionary<string, object>();
						}
						bool result3;
						float result4;
						float result5;
						float result6;
						float result7;
						if (int.TryParse(array9[1], out var result2))
						{
							vals[array9[0]] = result2;
						}
						else if (bool.TryParse(array9[1], out result3))
						{
							vals[array9[0]] = result3;
						}
						else if (float.TryParse(array9[1], NumberStyles.Float, CultureInfo.InvariantCulture, out result4))
						{
							vals[array9[0]] = result4;
						}
						else if (array9.Length >= 4 && float.TryParse(array9[1], out result5) && float.TryParse(array9[2], out result6) && float.TryParse(array9[3], out result7))
						{
							vals[array9[0]] = new Vector3(result5, result6, result7);
						}
						else
						{
							vals[array9[0]] = array9[1];
						}
					}
				}
			}
			DateTime now = DateTime.Now;
			if (text4.Length >= 2 && text4[text4.Length - 1] == '*')
			{
				text4 = text4.Substring(0, text4.Length - 1).ToLower();
				foreach (string prefabName in ZNetScene.instance.GetPrefabNames())
				{
					string text6 = prefabName.ToLower();
					if (text6.Contains(text4) && (text4.Contains("fx") || !text6.Contains("fx")))
					{
						spawn(prefabName);
					}
				}
			}
			else
			{
				spawn(text4);
			}
			ZLog.Log("Spawn time :" + (DateTime.Now - now).TotalMilliseconds + " ms");
			Gogan.LogEvent("Cheat", "Spawn", text4, count);
			return true;
			void spawn(string name)
			{
				GameObject prefab2 = ZNetScene.instance.GetPrefab(name);
				if (!prefab2)
				{
					Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Missing object " + name);
				}
				else
				{
					for (int num12 = 0; num12 < count; num12++)
					{
						Vector3 vector = UnityEngine.Random.insideUnitSphere * ((count == 1) ? 0f : radius);
						Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Spawning object " + name);
						GameObject gameObject3 = UnityEngine.Object.Instantiate(prefab2, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 2f + Vector3.up + vector, Quaternion.identity);
						if (vals != null)
						{
							ZNetView component10 = gameObject3.GetComponent<ZNetView>();
							if ((object)component10 != null && component10.IsValid())
							{
								component10.GetZDO().Set("HasFields", value: true);
								foreach (KeyValuePair<string, object> item13 in vals)
								{
									string[] array11 = item13.Key.Split('.', StringSplitOptions.None);
									if (array11.Length >= 2)
									{
										("HasFields" + array11[0]).GetStableHashCode();
										component10.GetZDO().Set("HasFields" + array11[0], value: true);
										item13.Value.GetType();
										if (item13.Value is float)
										{
											component10.GetZDO().Set(item13.Key, (float)item13.Value);
										}
										else if (item13.Value is int)
										{
											component10.GetZDO().Set(item13.Key, (int)item13.Value);
										}
										else if (item13.Value is bool)
										{
											component10.GetZDO().Set(item13.Key, (bool)item13.Value);
										}
										else
										{
											component10.GetZDO().Set(item13.Key, item13.Value.ToString());
										}
									}
								}
								component10.LoadFields();
							}
						}
						ItemDrop component11 = gameObject3.GetComponent<ItemDrop>();
						ItemDrop.OnCreateNew(gameObject3);
						if (level2 > 1)
						{
							if ((bool)component11)
							{
								level2 = Mathf.Min(level2, 4);
							}
							else
							{
								level2 = Mathf.Min(level2, 9);
							}
							gameObject3.GetComponent<Character>()?.SetLevel(level2);
							if (level2 > 4)
							{
								level2 = 4;
							}
							if ((bool)component11)
							{
								component11.SetQuality(level2);
							}
						}
						if (pickup || use || onlyIfMissing)
						{
							if (onlyIfMissing && (bool)component11 && Player.m_localPlayer.GetInventory().HaveItem(component11.m_itemData.m_shared.m_name))
							{
								ZNetView component12 = gameObject3.GetComponent<ZNetView>();
								if ((object)component12 != null)
								{
									component12.Destroy();
									continue;
								}
							}
							if (Player.m_localPlayer.Pickup(gameObject3, autoequip: false, autoPickupDelay: false) && use && (bool)component11)
							{
								Player.m_localPlayer.UseItem(Player.m_localPlayer.GetInventory(), component11.m_itemData, fromInventoryGui: false);
							}
						}
					}
				}
			}
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => (!ZNetScene.instance) ? new List<string>() : ZNetScene.instance.GetPrefabNames(), alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("catch", "[fishname] [level] simulates catching a fish", delegate(ConsoleEventArgs args)
		{
			string text3 = args[1];
			int a2 = args.TryParameterInt(2);
			a2 = Mathf.Min(a2, 4);
			GameObject prefab = ZNetScene.instance.GetPrefab(text3);
			if (!prefab)
			{
				return "No prefab named: " + text3;
			}
			Fish componentInChildren = prefab.GetComponentInChildren<Fish>();
			if (!componentInChildren)
			{
				return "No fish prefab named: " + text3;
			}
			GameObject obj2 = UnityEngine.Object.Instantiate(prefab, Player.m_localPlayer.transform.position, Quaternion.identity);
			componentInChildren = obj2.GetComponentInChildren<Fish>();
			ItemDrop component9 = obj2.GetComponent<ItemDrop>();
			if ((bool)component9)
			{
				component9.SetQuality(a2);
			}
			string msg = FishingFloat.Catch(componentInChildren, Player.m_localPlayer);
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, msg);
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => new List<string>
		{
			"Fish1", "Fish2", "Fish3", "Fish4_cave", "Fish5", "Fish6", "Fish7", "Fish8", "Fish9", "Fish10",
			"Fish11", "Fish12"
		});
		new ConsoleCommand("itemset", "[name] [item level override] [keep] - spawn a premade named set, add 'keep' to not drop current items.", delegate(ConsoleEventArgs args)
		{
			if (args.Length >= 2)
			{
				ItemSets.instance.TryGetSet(args.Args[1], !args.HasArgumentAnywhere("keep"), args.TryParameterInt(2, -1), args.TryParameterInt(3, -1));
				return true;
			}
			return "Specify name of itemset.";
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, () => ItemSets.instance.GetSetNames(), alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("pos", "print current player position", delegate(ConsoleEventArgs args)
		{
			Player localPlayer2 = Player.m_localPlayer;
			if ((bool)localPlayer2 && (bool)ZoneSystem.instance)
			{
				args.Context?.AddString(string.Format("Player position (X,Y,Z): {0} (Zone: {1})", localPlayer2.transform.position.ToString("F0"), ZoneSystem.instance.GetZone(localPlayer2.transform.position)));
			}
		}, isCheat: true);
		new ConsoleCommand("recall", "[*name] recalls players to you, optionally that match given name", delegate(ConsoleEventArgs args)
		{
			foreach (ZNetPeer peer in ZNet.instance.GetPeers())
			{
				if (peer.m_playerName != Player.m_localPlayer.GetPlayerName() && (args.Length < 2 || peer.m_playerName.ToLower().Contains(args[1].ToLower())))
				{
					Chat.instance.TeleportPlayer(peer.m_uid, Player.m_localPlayer.transform.position, Player.m_localPlayer.transform.rotation, distantTeleport: true);
				}
			}
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("goto", "[x,z] - teleport", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 3 || !args.TryParameterInt(1, out var value8) || !args.TryParameterInt(2, out var value9))
			{
				return false;
			}
			Player localPlayer = Player.m_localPlayer;
			if ((bool)localPlayer)
			{
				Vector3 pos = new Vector3(value8, localPlayer.transform.position.y, value9);
				float max = (localPlayer.IsDebugFlying() ? 400f : 30f);
				pos.y = Mathf.Clamp(pos.y, 30f, max);
				localPlayer.TeleportTo(pos, localPlayer.transform.rotation, distantTeleport: true);
			}
			Gogan.LogEvent("Cheat", "Goto", "", 0L);
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("exploremap", "explore entire map", delegate
		{
			Minimap.instance.ExploreAll();
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("resetmap", "reset map exploration", delegate
		{
			Minimap.instance.Reset();
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("resetsharedmap", "removes any shared map data from cartography table", delegate
		{
			Minimap.instance.ResetSharedMapData();
		});
		new ConsoleCommand("restartparty", "restart playfab party network", delegate
		{
			if (ZNet.m_onlineBackend == OnlineBackendType.PlayFab)
			{
				if (ZNet.instance.IsServer())
				{
					ZPlayFabMatchmaking.ResetParty();
				}
				else
				{
					ZPlayFabSocket.ScheduleResetParty();
				}
			}
		}, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: true);
		new ConsoleCommand("puke", "empties your stomach of food", delegate
		{
			if ((bool)Player.m_localPlayer)
			{
				Player.m_localPlayer.ClearFood();
			}
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("tame", "tame all nearby tameable creatures", delegate
		{
			Tameable.TameAllInArea(Player.m_localPlayer.transform.position, 20f);
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("aggravate", "aggravated all nearby neutrals", delegate
		{
			BaseAI.AggravateAllInArea(Player.m_localPlayer.transform.position, 20f, BaseAI.AggravatedReason.Damage);
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("killall", "kill nearby creatures", delegate
		{
			List<Character> allCharacters4 = Character.GetAllCharacters();
			int num8 = 0;
			int num9 = 0;
			foreach (Character item14 in allCharacters4)
			{
				if (!item14.IsPlayer())
				{
					item14.Damage(new HitData(1E+10f));
					num8++;
				}
			}
			SpawnArea[] array8 = UnityEngine.Object.FindObjectsByType<SpawnArea>(FindObjectsSortMode.None);
			for (int num10 = 0; num10 < array8.Length; num10++)
			{
				Destructible component8 = array8[num10].gameObject.GetComponent<Destructible>();
				if ((object)component8 != null)
				{
					component8.Damage(new HitData(1E+10f));
					num9++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format("Killed {0} monsters{1}", num8, (num9 > 0) ? $" & {num9} spawners." : "."));
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("killenemycreatures", "kill nearby enemies", delegate
		{
			List<Character> allCharacters3 = Character.GetAllCharacters();
			int num7 = 0;
			foreach (Character item15 in allCharacters3)
			{
				if (!item15.IsPlayer() && !item15.IsTamed())
				{
					item15.Damage(new HitData(1E+10f));
					num7++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"Killed {num7} monsters.");
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("killenemies", "kill nearby enemies", delegate
		{
			List<Character> allCharacters2 = Character.GetAllCharacters();
			int num5 = 0;
			int num6 = 0;
			foreach (Character item16 in allCharacters2)
			{
				if (!item16.IsPlayer() && !item16.IsTamed())
				{
					item16.Damage(new HitData(1E+10f));
					num5++;
				}
			}
			SpawnArea[] array7 = UnityEngine.Object.FindObjectsByType<SpawnArea>(FindObjectsSortMode.None);
			for (int n = 0; n < array7.Length; n++)
			{
				Destructible component7 = array7[n].gameObject.GetComponent<Destructible>();
				if ((object)component7 != null)
				{
					component7.Damage(new HitData(1E+10f));
					num6++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, string.Format("Killed {0} monsters{1}", num5, (num6 > 0) ? $" & {num6} spawners." : "."));
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("killtame", "kill nearby tame creatures.", delegate
		{
			List<Character> allCharacters = Character.GetAllCharacters();
			int num4 = 0;
			foreach (Character item17 in allCharacters)
			{
				if (!item17.IsPlayer() && item17.IsTamed())
				{
					item17.Damage(new HitData
					{
						m_damage = 
						{
							m_damage = 1E+10f
						}
					});
					num4++;
				}
			}
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Killing all tame creatures:" + num4);
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("heal", "heal to full health & stamina", delegate
		{
			if (!(Player.m_localPlayer == null))
			{
				Player.m_localPlayer.Heal(Player.m_localPlayer.GetMaxHealth());
				Player.m_localPlayer.AddStamina(Player.m_localPlayer.GetMaxStamina());
				Player.m_localPlayer.AddEitr(Player.m_localPlayer.GetMaxEitr());
			}
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: true, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("god", "invincible mode", delegate(ConsoleEventArgs args)
		{
			Player.m_localPlayer.SetGodMode(args.HasArgumentAnywhere("on") || (!args.HasArgumentAnywhere("off") && !Player.m_localPlayer.InGodMode()));
			args.Context.AddString("God mode:" + Player.m_localPlayer.InGodMode());
			Gogan.LogEvent("Cheat", "God", Player.m_localPlayer.InGodMode().ToString(), 0L);
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("ghost", "", delegate(ConsoleEventArgs args)
		{
			Player.m_localPlayer.SetGhostMode(args.HasArgumentAnywhere("on") || (!args.HasArgumentAnywhere("off") && !Player.m_localPlayer.InGhostMode()));
			args.Context.AddString("Ghost mode:" + Player.m_localPlayer.InGhostMode());
			Gogan.LogEvent("Cheat", "Ghost", Player.m_localPlayer.InGhostMode().ToString(), 0L);
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("beard", "change beard", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			if ((bool)Player.m_localPlayer)
			{
				Player.m_localPlayer.SetBeard(args[1]);
			}
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, delegate
		{
			List<string> list8 = new List<string>();
			foreach (ItemDrop allItem in ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Beard"))
			{
				list8.Add(allItem.name);
			}
			return list8;
		});
		new ConsoleCommand("hair", "change hair", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			if ((bool)Player.m_localPlayer)
			{
				Player.m_localPlayer.SetHair(args[1]);
			}
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, delegate
		{
			List<string> list7 = new List<string>();
			foreach (ItemDrop allItem2 in ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, "Hair"))
			{
				list7.Add(allItem2.name);
			}
			return list7;
		});
		new ConsoleCommand("model", "change player model", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			if ((bool)Player.m_localPlayer && args.TryParameterInt(1, out var value7))
			{
				Player.m_localPlayer.SetPlayerModel(value7);
			}
			return true;
		}, isCheat: true);
		new ConsoleCommand("tod", "-1 OR [0-1]", delegate(ConsoleEventArgs args)
		{
			if (EnvMan.instance == null || args.Length < 2 || !args.TryParameterFloat(1, out var value6))
			{
				return false;
			}
			args.Context.AddString("Setting time of day:" + value6);
			if (value6 < 0f)
			{
				EnvMan.instance.m_debugTimeOfDay = false;
			}
			else
			{
				EnvMan.instance.m_debugTimeOfDay = true;
				EnvMan.instance.m_debugTime = Mathf.Clamp01(value6);
			}
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: true);
		new ConsoleCommand("env", "[env] override environment", delegate(ConsoleEventArgs args)
		{
			if (EnvMan.instance == null || args.Length < 2)
			{
				return false;
			}
			string text2 = string.Join(" ", args.Args, 1, args.Args.Length - 1);
			args.Context.AddString("Setting debug enviornment:" + text2);
			EnvMan.instance.m_debugEnv = text2;
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: true, delegate
		{
			List<string> list6 = new List<string>();
			foreach (EnvSetup environment in EnvMan.instance.m_environments)
			{
				list6.Add(environment.m_name);
			}
			return list6;
		});
		new ConsoleCommand("resetenv", "disables environment override", delegate(ConsoleEventArgs args)
		{
			if (EnvMan.instance == null)
			{
				return false;
			}
			args.Context.AddString("Resetting debug environment");
			EnvMan.instance.m_debugEnv = "";
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: true, isSecret: false, allowInDevBuild: true);
		new ConsoleCommand("wind", "[angle] [intensity]", delegate(ConsoleEventArgs args)
		{
			if (args.TryParameterFloat(1, out var value4) && args.TryParameterFloat(2, out var value5))
			{
				EnvMan.instance.SetDebugWind(value4, value5);
				return true;
			}
			return false;
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("resetwind", "", delegate
		{
			EnvMan.instance.ResetDebugWind();
		}, isCheat: true, isNetwork: false, onlyServer: true);
		new ConsoleCommand("clear", "clear the console window", delegate(ConsoleEventArgs args)
		{
			args.Context.m_chatBuffer.Clear();
			args.Context.UpdateChat();
		});
		new ConsoleCommand("filtercraft", "[name] filters crafting list to contain part of text", delegate(ConsoleEventArgs args)
		{
			if (args.Length <= 1)
			{
				Player.s_FilterCraft.Clear();
			}
			else
			{
				Player.s_FilterCraft = args.ArgsAll.Split(' ', StringSplitOptions.None).ToList();
			}
		});
		new ConsoleCommand("clearstatus", "clear any status modifiers", delegate
		{
			Player.m_localPlayer.ClearHardDeath();
			Player.m_localPlayer.GetSEMan().RemoveAllStatusEffects();
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: true, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("addstatus", "[name] adds a status effect (ex: Rested, Burning, SoftDeath, Wet, etc)", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			Player.m_localPlayer.GetSEMan().AddStatusEffect(args[1].GetStableHashCode(), resetTime: true);
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: true, delegate
		{
			List<StatusEffect> statusEffects2 = ObjectDB.instance.m_StatusEffects;
			List<string> list5 = new List<string>();
			foreach (StatusEffect item18 in statusEffects2)
			{
				list5.Add(item18.name);
			}
			return list5;
		}, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("setpower", "[name] sets your current guardian power and resets cooldown (ex: GP_Eikthyr, GP_TheElder, etc)", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			Player.m_localPlayer.SetGuardianPower(args[1]);
			Player.m_localPlayer.m_guardianPowerCooldown = 0f;
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: true, delegate
		{
			List<StatusEffect> statusEffects = ObjectDB.instance.m_StatusEffects;
			List<string> list4 = new List<string>();
			foreach (StatusEffect item19 in statusEffects)
			{
				list4.Add(item19.name);
			}
			return list4;
		}, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("bind", "[keycode] [command and parameters] bind a key to a console command. note: may cause conflicts with game controls", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			if (!Enum.TryParse<KeyCode>(args[1], ignoreCase: true, out var _))
			{
				args.Context.AddString("'" + args[1] + "' is not a valid UnityEngine.KeyCode.");
			}
			else
			{
				string item = string.Join(" ", args.Args, 1, args.Length - 1);
				m_bindList.Add(item);
				updateBinds();
			}
			return true;
		});
		new ConsoleCommand("unbind", "[keycode] clears all binds connected to keycode", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				return false;
			}
			for (int num3 = m_bindList.Count - 1; num3 >= 0; num3--)
			{
				if (m_bindList[num3].Split(' ', StringSplitOptions.None)[0].ToLower() == args[1].ToLower())
				{
					m_bindList.RemoveAt(num3);
				}
			}
			updateBinds();
			return true;
		});
		new ConsoleCommand("printbinds", "prints current binds", delegate(ConsoleEventArgs args)
		{
			foreach (string bind in m_bindList)
			{
				args.Context.AddString(bind);
			}
		});
		new ConsoleCommand("resetbinds", "resets all custom binds to default dev commands", delegate
		{
			for (int num2 = m_bindList.Count - 1; num2 >= 0; num2--)
			{
				m_bindList.Remove(m_bindList[num2]);
			}
			updateBinds();
		});
		new ConsoleCommand("tombstone", "[name] creates a tombstone with given name", delegate(ConsoleEventArgs args)
		{
			GameObject obj = UnityEngine.Object.Instantiate(Player.m_localPlayer.m_tombstone, Player.m_localPlayer.GetCenterPoint(), Player.m_localPlayer.transform.rotation);
			Container component5 = obj.GetComponent<Container>();
			ItemDrop coinPrefab = StoreGui.instance.m_coinPrefab;
			component5.GetInventory().AddItem(coinPrefab.gameObject.name, 1, coinPrefab.m_itemData.m_quality, coinPrefab.m_itemData.m_variant, 0L, "", pickedUp: true);
			TombStone component6 = obj.GetComponent<TombStone>();
			PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
			string ownerName = ((args.Args.Length >= 2) ? args.Args[1] : playerProfile.GetName());
			component6.Setup(ownerName, playerProfile.GetPlayerID());
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("test", "[key] [value] set test string, with optional value. set empty existing key to remove", delegate(ConsoleEventArgs args)
		{
			if (args.Length < 2)
			{
				m_showTests = !m_showTests;
				return true;
			}
			string text = ((args.Length >= 3) ? args[2] : "");
			if (m_testList.ContainsKey(args[1]) && text.Length == 0)
			{
				m_testList.Remove(args[1]);
				args.Context?.AddString("'" + args[1] + "' removed");
			}
			else
			{
				m_testList[args[1]] = text;
				args.Context?.AddString("'" + args[1] + "' added with value '" + text + "'");
			}
			switch (args[1].ToLower())
			{
			case "ngenemyac":
				Game.instance.m_worldLevelEnemyBaseAC = int.Parse(args[2]);
				break;
			case "ngenemyhp":
				Game.instance.m_worldLevelEnemyHPMultiplier = float.Parse(args[2]);
				break;
			case "ngenemydamage":
				Game.instance.m_worldLevelEnemyBaseDamage = int.Parse(args[2]);
				break;
			case "ngplayerac":
				Game.instance.m_worldLevelGearBaseAC = int.Parse(args[2]);
				break;
			case "ngplayerdamage":
				Game.instance.m_worldLevelGearBaseDamage = int.Parse(args[2]);
				break;
			}
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: true);
		new ConsoleCommand("forcedelete", "[radius] [*name] force remove all objects within given radius. If name is entered, only deletes items with matching names. Caution! Use at your own risk. Make backups! Radius default: 5, max: 50.", delegate(ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			float num = Math.Min(50f, args.TryParameterFloat(1, 5f));
			UnityEngine.Object[] array6 = UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
			for (int m = 0; m < array6.Length; m++)
			{
				GameObject gameObject2 = (GameObject)array6[m];
				if (Vector3.Distance(gameObject2.transform.position, Player.m_localPlayer.transform.position) < num)
				{
					string path = gameObject2.gameObject.transform.GetPath();
					if (!(gameObject2.GetComponentInParent<Game>() != null) && !(gameObject2.GetComponentInParent<Player>() != null) && !(gameObject2.GetComponentInParent<Valkyrie>() != null) && !(gameObject2.GetComponentInParent<LocationProxy>() != null) && !(gameObject2.GetComponentInParent<Room>() != null) && !(gameObject2.GetComponentInParent<Vegvisir>() != null) && !(gameObject2.GetComponentInParent<DungeonGenerator>() != null) && !path.Contains("StartTemple") && !path.Contains("BossStone") && (args.Length <= 2 || gameObject2.name.ToLower().Contains(args[2].ToLower())))
					{
						Destructible component3 = gameObject2.GetComponent<Destructible>();
						ZNetView component4 = gameObject2.GetComponent<ZNetView>();
						if (component3 != null)
						{
							component3.DestroyNow();
						}
						else if (component4 != null && (bool)ZNetScene.instance)
						{
							ZNetScene.instance.Destroy(gameObject2);
						}
					}
				}
			}
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("stopfire", "Puts out all spreading fires and smoke", delegate
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			RemoveObj(UnityEngine.Object.FindObjectsOfType(typeof(Fire)));
			RemoveObj(UnityEngine.Object.FindObjectsOfType(typeof(Smoke)));
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("stopsmoke", "Puts out all spreading fires", delegate
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			UnityEngine.Object[] array5 = UnityEngine.Object.FindObjectsOfType(typeof(Fire));
			for (int l = 0; l < array5.Length; l++)
			{
				Fire fire = (Fire)array5[l];
				Destructible component = fire.GetComponent<Destructible>();
				ZNetView component2 = fire.GetComponent<ZNetView>();
				if (component != null)
				{
					component.DestroyNow();
				}
				else if (component2 != null && (bool)ZNetScene.instance)
				{
					ZNetScene.instance.Destroy(fire.gameObject);
				}
			}
			return true;
		}, isCheat: true, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("printseeds", "print seeds of loaded dungeons", delegate(ConsoleEventArgs args)
		{
			if (Player.m_localPlayer == null)
			{
				return false;
			}
			Math.Min(20f, args.TryParameterFloat(1, 5f));
			UnityEngine.Object[] array3 = UnityEngine.Object.FindObjectsOfType(typeof(DungeonGenerator));
			args.Context.AddString(string.Format("{0} version {1}, world seed: {2}/{3}", ((bool)ZNet.instance && ZNet.instance.IsServer()) ? "Server" : "Client", Version.GetVersionString(), ZNet.World.m_seed, ZNet.World.m_seedName));
			UnityEngine.Object[] array4 = array3;
			for (int k = 0; k < array4.Length; k++)
			{
				DungeonGenerator dungeonGenerator = (DungeonGenerator)array4[k];
				args.Context.AddString(string.Format("  {0}: Seed: {1}/{2}, Distance: {3}", dungeonGenerator.name, dungeonGenerator.m_generatedSeed, dungeonGenerator.GetSeed(), Utils.DistanceXZ(Player.m_localPlayer.transform.position, dungeonGenerator.transform.position).ToString("0.0")));
			}
			return true;
		});
		new ConsoleCommand("nomap", "disables map for this character. If used as host, will disable for all joining players from now on.", delegate(ConsoleEventArgs args)
		{
			if (Player.m_localPlayer != null)
			{
				string key2 = "mapenabled_" + Player.m_localPlayer.GetPlayerName();
				bool flag = PlayerPrefs.GetFloat(key2, 1f) == 1f;
				PlayerPrefs.SetFloat(key2, (!flag) ? 1 : 0);
				Minimap.instance.SetMapMode(Minimap.MapMode.None);
				args.Context?.AddString("Map " + (flag ? "disabled" : "enabled"));
				if ((bool)ZNet.instance && ZNet.instance.IsServer())
				{
					if (flag)
					{
						ZoneSystem.instance.SetGlobalKey(GlobalKeys.NoMap);
					}
					else
					{
						ZoneSystem.instance.RemoveGlobalKey(GlobalKeys.NoMap);
					}
				}
			}
		});
		new ConsoleCommand("noportals", "disables portals for server.", delegate(ConsoleEventArgs args)
		{
			if (Player.m_localPlayer != null)
			{
				bool globalKey = ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals);
				if (globalKey)
				{
					ZoneSystem.instance.RemoveGlobalKey(GlobalKeys.NoPortals);
				}
				else
				{
					ZoneSystem.instance.SetGlobalKey(GlobalKeys.NoPortals);
				}
				args.Context?.AddString("Portals " + (globalKey ? "enabled" : "disabled"));
			}
		}, isCheat: false, isNetwork: false, onlyServer: false, isSecret: false, allowInDevBuild: false, null, alwaysRefreshTabOptions: false, remoteCommand: false, onlyAdmin: true);
		new ConsoleCommand("resetspawn", "resets spawn location", delegate(ConsoleEventArgs args)
		{
			if (!Game.instance)
			{
				return false;
			}
			Game.instance.GetPlayerProfile()?.ClearCustomSpawnPoint();
			args.Context?.AddString("Reseting spawn point");
			return true;
		});
		new ConsoleCommand("respawntime", "sets respawntime", delegate(ConsoleEventArgs args)
		{
			if (!Game.instance)
			{
				return false;
			}
			if (args.TryParameterFloat(1, out var value3))
			{
				Game.instance.m_respawnLoadDuration = (Game.instance.m_fadeTimeDeath = value3);
			}
			return true;
		}, isCheat: true);
		new ConsoleCommand("die", "kill yourself", delegate
		{
			if (!Player.m_localPlayer)
			{
				return false;
			}
			HitData hit = new HitData
			{
				m_damage = 
				{
					m_damage = 99999f
				},
				m_hitType = HitData.HitType.Self
			};
			Player.m_localPlayer.Damage(hit);
			return true;
		});
		new ConsoleCommand("say", "chat message", delegate(ConsoleEventArgs args)
		{
			if (args.FullLine.Length < 5 || Chat.instance == null)
			{
				return false;
			}
			Chat.instance.SendText(Talker.Type.Normal, args.FullLine.Substring(4));
			return true;
		});
		new ConsoleCommand("s", "shout message", delegate(ConsoleEventArgs args)
		{
			if (args.FullLine.Length < 3 || Chat.instance == null)
			{
				return false;
			}
			Chat.instance.SendText(Talker.Type.Shout, args.FullLine.Substring(2));
			return true;
		});
		new ConsoleCommand("w", "[playername] whispers a private message to a player", delegate(ConsoleEventArgs args)
		{
			if (args.FullLine.Length < 3 || Chat.instance == null)
			{
				return false;
			}
			Chat.instance.SendText(Talker.Type.Whisper, args.FullLine.Substring(2));
			return true;
		});
		new ConsoleCommand("resetplayerprefs", "Resets any saved settings and variables (not the save game)", delegate(ConsoleEventArgs args)
		{
			PlayerPrefs.DeleteAll();
			args.Context?.AddString("Reset saved player preferences");
		}, isCheat: false, isNetwork: false, onlyServer: false, isSecret: true, allowInDevBuild: true);
		for (int i = 0; i < 20; i++)
		{
			Emotes emote = (Emotes)i;
			new ConsoleCommand(emote.ToString().ToLower(), $"emote: {emote}", delegate
			{
				Emote.DoEmote(emote);
			});
		}
		new ConsoleCommand("resetplayerprefs", "Resets any saved settings and variables (not the save game)", delegate(ConsoleEventArgs args)
		{
			PlayerPrefs.DeleteAll();
			args.Context?.AddString("Reset saved player preferences");
		}, isCheat: false, isNetwork: false, onlyServer: false, isSecret: true, allowInDevBuild: true);
		void count(string key, int level, int increment = 1)
		{
			if (!P_3.counts.TryGetValue(key, out var value))
			{
				value = (P_3.counts[key] = new Dictionary<int, int>());
			}
			if (value.TryGetValue(level, out var value2))
			{
				value[level] = value2 + increment;
			}
			else
			{
				value[level] = increment;
			}
		}
		static List<Tuple<object, Vector3>> find(string q)
		{
			new Dictionary<string, Dictionary<int, int>>();
			GameObject[] array = UnityEngine.Object.FindObjectsOfType<GameObject>();
			List<Tuple<object, Vector3>> list2 = new List<Tuple<object, Vector3>>();
			GameObject[] array2 = array;
			foreach (GameObject gameObject in array2)
			{
				if (gameObject.name.ToLower().Contains(q))
				{
					list2.Add(new Tuple<object, Vector3>(gameObject, gameObject.transform.position));
				}
			}
			foreach (ZoneSystem.LocationInstance location3 in ZoneSystem.instance.GetLocationList())
			{
				if (location3.m_location.m_prefab.Name.ToLower().Contains(q))
				{
					list2.Add(new Tuple<object, Vector3>(location3, location3.m_position));
				}
			}
			List<ZDO> list3 = new List<ZDO>();
			int index = 0;
			while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(q, list3, ref index))
			{
			}
			foreach (ZDO item20 in list3)
			{
				list2.Add(new Tuple<object, Vector3>(item20, item20.GetPosition()));
			}
			return list2;
		}
		static List<string> findOpt()
		{
			if (!ZNetScene.instance)
			{
				return null;
			}
			List<string> list = new List<string>(ZNetScene.instance.GetPrefabNames());
			foreach (ZoneSystem.ZoneLocation location4 in ZoneSystem.instance.m_locations)
			{
				if (location4.m_enable || location4.m_prefab.IsValid)
				{
					list.Add(location4.m_prefab.Name);
				}
			}
			return list;
		}
	}

	private static void RemoveObj(UnityEngine.Object[] objs)
	{
		for (int i = 0; i < objs.Length; i++)
		{
			MonoBehaviour monoBehaviour = (MonoBehaviour)objs[i];
			Destructible component = monoBehaviour.GetComponent<Destructible>();
			ZNetView component2 = monoBehaviour.GetComponent<ZNetView>();
			if (component != null)
			{
				component.DestroyNow();
			}
			else if (component2 != null && (bool)ZNetScene.instance)
			{
				ZNetScene.instance.Destroy(monoBehaviour.gameObject);
			}
			else
			{
				UnityEngine.Object.Destroy(monoBehaviour.gameObject);
			}
		}
	}

	private static void AddConsoleCheatCommands()
	{
		new ConsoleCommand("xb:version", "Prints mercurial hashset used for this build", delegate(ConsoleEventArgs args)
		{
			args.Context?.AddString("Buildhash: " + Version.GetVersionString(includeMercurialHash: true));
		});
	}

	protected static void updateBinds()
	{
		m_binds.Clear();
		foreach (string bind in m_bindList)
		{
			string[] array = bind.Split(' ', StringSplitOptions.None);
			string item = string.Join(" ", array, 1, array.Length - 1);
			if (Enum.TryParse<KeyCode>(array[0], ignoreCase: true, out var result))
			{
				if (m_binds.TryGetValue(result, out var value))
				{
					value.Add(item);
					continue;
				}
				m_binds[result] = new List<string> { item };
			}
		}
		PlayerPrefs.SetString("ConsoleBindings", string.Join("\n", m_bindList));
	}

	private void updateCommandList()
	{
		m_commandList.Clear();
		foreach (KeyValuePair<string, ConsoleCommand> command in commands)
		{
			if (command.Value.ShowCommand(this) && (m_autoCompleteSecrets || !command.Value.IsSecret))
			{
				m_commandList.Add(command.Key);
			}
		}
	}

	public bool IsCheatsEnabled()
	{
		if (m_cheat)
		{
			if ((bool)ZNet.instance)
			{
				return ZNet.instance.IsServer();
			}
			return false;
		}
		return false;
	}

	public void TryRunCommand(string text, bool silentFail = false, bool skipAllowedCheck = false)
	{
		string[] array = text.Split(' ', StringSplitOptions.None);
		if (commands.TryGetValue(array[0].ToLower(), out var value))
		{
			if (value.IsValid(this, skipAllowedCheck))
			{
				value.RunAction(new ConsoleEventArgs(text, this));
			}
			else if (value.RemoteCommand && (bool)ZNet.instance && !ZNet.instance.IsServer())
			{
				ZNet.instance.RemoteCommand(text);
			}
			else if (!silentFail)
			{
				AddString("'" + text.Split(' ', StringSplitOptions.None)[0] + "' is not valid in the current context.");
			}
		}
		else if (!silentFail)
		{
			AddString("'" + array[0] + "' is not a recognized command. Type 'help' to see a list of valid commands.");
		}
	}

	public virtual void Awake()
	{
		InitTerminal();
	}

	public virtual void Update()
	{
		if (m_focused)
		{
			UpdateInput();
		}
	}

	private void UpdateInput()
	{
		if (ZInput.GetButton("JoyButtonX"))
		{
			if (ZInput.GetButtonDown("JoyDPadLeft"))
			{
				m_quickSelect[0] = m_input.text;
				PlatformPrefs.SetString("quick_save_left", m_quickSelect[0]);
				PlayerPrefs.Save();
			}
			if (ZInput.GetButtonDown("JoyDPadRight"))
			{
				m_quickSelect[1] = m_input.text;
				PlatformPrefs.SetString("quick_save_right", m_quickSelect[1]);
				PlayerPrefs.Save();
			}
			if (ZInput.GetButtonDown("JoyDPadUp"))
			{
				m_quickSelect[2] = m_input.text;
				PlatformPrefs.SetString("quick_save_up", m_quickSelect[2]);
				PlayerPrefs.Save();
			}
			if (ZInput.GetButtonDown("JoyDPadDown"))
			{
				m_quickSelect[3] = m_input.text;
				PlatformPrefs.SetString("quick_save_down", m_quickSelect[3]);
				PlayerPrefs.Save();
			}
		}
		else if (ZInput.GetButton("JoyButtonY"))
		{
			if (ZInput.GetButtonDown("JoyDPadLeft"))
			{
				m_input.text = m_quickSelect[0];
				m_input.caretPosition = m_input.text.Length;
			}
			if (ZInput.GetButtonDown("JoyDPadRight"))
			{
				m_input.text = m_quickSelect[1];
				m_input.caretPosition = m_input.text.Length;
			}
			if (ZInput.GetButtonDown("JoyDPadUp"))
			{
				m_input.caretPosition = m_input.text.Length;
				m_input.text = m_quickSelect[2];
			}
			if (ZInput.GetButtonDown("JoyDPadDown"))
			{
				m_input.caretPosition = m_input.text.Length;
				m_input.text = m_quickSelect[3];
			}
		}
		else if ((ZInput.GetButtonDown("ChatUp") || ZInput.GetButtonDown("JoyDPadUp")) && !m_input.IsCompositionActive())
		{
			if (m_historyPosition > 0)
			{
				m_historyPosition--;
			}
			m_input.text = ((m_history.Count > 0) ? m_history[m_historyPosition] : "");
			m_input.caretPosition = m_input.text.Length;
		}
		else if ((ZInput.GetButtonDown("ChatDown") || ZInput.GetButtonDown("JoyDPadDown")) && !m_input.IsCompositionActive())
		{
			if (m_historyPosition < m_history.Count)
			{
				m_historyPosition++;
			}
			m_input.text = ((m_historyPosition < m_history.Count) ? m_history[m_historyPosition] : "");
			m_input.caretPosition = m_input.text.Length;
		}
		else if (ZInput.GetKeyDown(KeyCode.Tab) || ZInput.GetButtonDown("JoyDPadRight"))
		{
			if (m_commandList.Count == 0)
			{
				updateCommandList();
			}
			string[] array = m_input.text.Split(' ', StringSplitOptions.None);
			if (array.Length == 1)
			{
				tabCycle(array[0], m_commandList, usePrefix: true);
			}
			else
			{
				string key = ((m_tabPrefix == '\0') ? array[0] : array[0].Substring(1));
				if (commands.TryGetValue(key, out var value))
				{
					tabCycle(array[1], value.GetTabOptions(), usePrefix: false);
				}
			}
		}
		if ((ZInput.GetButtonDown("ScrollChatUp") || ZInput.GetButtonDown("JoyScrollChatUp")) && m_scrollHeight < m_chatBuffer.Count - 5)
		{
			m_scrollHeight++;
			UpdateChat();
		}
		if ((ZInput.GetButtonDown("ScrollChatDown") || ZInput.GetButtonDown("JoyScrollChatDown")) && m_scrollHeight > 0)
		{
			m_scrollHeight--;
			UpdateChat();
		}
		if (m_input.caretPosition != m_tabCaretPositionEnd)
		{
			m_tabCaretPosition = -1;
		}
		if (m_lastSearchLength == m_input.text.Length)
		{
			return;
		}
		m_lastSearchLength = m_input.text.Length;
		if (m_commandList.Count == 0)
		{
			updateCommandList();
		}
		string[] array2 = m_input.text.Split(' ', StringSplitOptions.None);
		if (array2.Length == 1)
		{
			updateSearch(array2[0], m_commandList, usePrefix: true);
			return;
		}
		string key2 = ((m_tabPrefix == '\0') ? array2[0] : ((array2[0].Length == 0) ? "" : array2[0].Substring(1)));
		if (commands.TryGetValue(key2, out var value2))
		{
			updateSearch(array2[1], value2.GetTabOptions(), usePrefix: false);
		}
	}

	protected void SendInput()
	{
		if (!string.IsNullOrEmpty(m_input.text))
		{
			InputText();
			if (m_history.Count == 0 || m_history[m_history.Count - 1] != m_input.text)
			{
				m_history.Add(m_input.text);
			}
			m_historyPosition = m_history.Count;
			m_input.text = "";
			m_scrollHeight = 0;
			UpdateChat();
			if (!Application.isConsolePlatform && !Application.isMobilePlatform)
			{
				m_input.ActivateInputField();
			}
		}
	}

	protected virtual void InputText()
	{
		string text = m_input.text;
		AddString(text);
		TryRunCommand(text);
	}

	protected virtual bool isAllowedCommand(ConsoleCommand cmd)
	{
		return true;
	}

	public void AddString(string user, string text, Talker.Type type, bool timestamp = false)
	{
		Color white = Color.white;
		switch (type)
		{
		case Talker.Type.Shout:
			white = Color.yellow;
			text = text.ToUpper();
			break;
		case Talker.Type.Whisper:
			white = new Color(1f, 1f, 1f, 0.75f);
			text = text.ToLowerInvariant();
			break;
		default:
			white = Color.white;
			break;
		}
		string text2 = (timestamp ? ("[" + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss") + "] ") : "");
		text2 = text2 + "<color=orange>" + user + "</color>: <color=#" + ColorUtility.ToHtmlStringRGBA(white) + ">" + text + "</color>";
		AddString(text2);
	}

	public void AddString(string text)
	{
		while (m_maxVisibleBufferLength > 1)
		{
			try
			{
				m_chatBuffer.Add(text);
				while (m_chatBuffer.Count > 300)
				{
					m_chatBuffer.RemoveAt(0);
				}
				UpdateChat();
				break;
			}
			catch (Exception)
			{
				m_maxVisibleBufferLength--;
			}
		}
	}

	public void UpdateDisplayName(string oldName, string newName)
	{
		if (string.IsNullOrEmpty(oldName))
		{
			Debug.LogError("Failed to update display to \"" + newName + "\"! oldName was " + ((oldName == null) ? "null" : "empty") + " ");
		}
		else
		{
			for (int i = 0; i < m_chatBuffer.Count; i++)
			{
				m_chatBuffer[i] = m_chatBuffer[i].Replace(oldName, newName);
			}
			UpdateChat();
		}
	}

	private void UpdateChat()
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = Mathf.Min(m_chatBuffer.Count, Mathf.Max(5, m_chatBuffer.Count - m_scrollHeight));
		for (int i = Mathf.Max(0, num - m_maxVisibleBufferLength); i < num; i++)
		{
			stringBuilder.Append(m_chatBuffer[i]);
			stringBuilder.Append("\n");
		}
		m_output.text = stringBuilder.ToString();
	}

	public static float GetTestValue(string key, float defaultIfMissing = 0f)
	{
		if (m_testList.TryGetValue(key, out var value) && float.TryParse(value, out var result))
		{
			return result;
		}
		return defaultIfMissing;
	}

	private void tabCycle(string word, List<string> options, bool usePrefix)
	{
		if (options == null || options.Count == 0)
		{
			return;
		}
		usePrefix = usePrefix && m_tabPrefix != '\0';
		if (usePrefix)
		{
			if (word.Length < 1 || word[0] != m_tabPrefix)
			{
				return;
			}
			word = word.Substring(1);
		}
		if (m_tabCaretPosition == -1)
		{
			m_tabOptions.Clear();
			m_tabCaretPosition = m_input.caretPosition;
			word = word.ToLower();
			m_tabLength = word.Length;
			if (m_tabLength == 0)
			{
				m_tabOptions.AddRange(options);
			}
			else
			{
				foreach (string option in options)
				{
					if (option != null && option.Length > m_tabLength && safeSubstring(option, 0, m_tabLength).ToLower() == word)
					{
						m_tabOptions.Add(option);
					}
				}
			}
			m_tabOptions.Sort();
			m_tabIndex = -1;
		}
		if (m_tabOptions.Count == 0)
		{
			m_tabOptions.AddRange(m_lastSearch);
		}
		if (m_tabOptions.Count != 0)
		{
			if (++m_tabIndex >= m_tabOptions.Count)
			{
				m_tabIndex = 0;
			}
			if (m_tabCaretPosition - m_tabLength >= 0)
			{
				m_input.text = safeSubstring(m_input.text, 0, m_tabCaretPosition - m_tabLength) + m_tabOptions[m_tabIndex];
			}
			int tabCaretPositionEnd = (m_input.caretPosition = m_input.text.Length);
			m_tabCaretPositionEnd = tabCaretPositionEnd;
		}
	}

	private void updateSearch(string word, List<string> options, bool usePrefix)
	{
		if (m_search == null)
		{
			return;
		}
		m_search.text = "";
		if (options == null || options.Count == 0)
		{
			return;
		}
		usePrefix = usePrefix && m_tabPrefix != '\0';
		if (usePrefix)
		{
			if (word.Length < 1 || word[0] != m_tabPrefix)
			{
				return;
			}
			word = word.Substring(1);
		}
		m_lastSearch.Clear();
		foreach (string option in options)
		{
			if (option != null)
			{
				string text = option.ToLower();
				if (text.Contains(word.ToLower()) && (word.Contains("fx") || !text.Contains("fx")))
				{
					m_lastSearch.Add(option);
				}
			}
		}
		int num = 10;
		for (int i = 0; i < Math.Min(m_lastSearch.Count, num); i++)
		{
			string text2 = m_lastSearch[i];
			int num2 = text2.ToLower().IndexOf(word.ToLower());
			m_search.text += safeSubstring(text2, 0, num2);
			TMP_Text search = m_search;
			search.text = search.text + "<color=white>" + safeSubstring(text2, num2, word.Length) + "</color>";
			TMP_Text search2 = m_search;
			search2.text = search2.text + safeSubstring(text2, num2 + word.Length) + " ";
		}
		if (m_lastSearch.Count > num)
		{
			m_search.text += $"... {m_lastSearch.Count - num} more.";
		}
	}

	private string safeSubstring(string text, int start, int length = -1)
	{
		if (text.Length == 0)
		{
			return text;
		}
		if (start < 0)
		{
			start = 0;
		}
		if (start + length >= text.Length)
		{
			length = text.Length - start;
		}
		if (length >= 0)
		{
			return text.Substring(start, length);
		}
		return text.Substring(start);
	}

	protected void LoadQuickSelect()
	{
		m_quickSelect[0] = PlatformPrefs.GetString("quick_save_left");
		m_quickSelect[1] = PlatformPrefs.GetString("quick_save_right");
		m_quickSelect[2] = PlatformPrefs.GetString("quick_save_up");
		m_quickSelect[3] = PlatformPrefs.GetString("quick_save_down");
	}

	public static float TryTestFloat(string key, float defaultValue = 1f)
	{
		if (m_testList.TryGetValue(key, out var value) && float.TryParse(value, out var result))
		{
			return result;
		}
		return defaultValue;
	}

	public static int TryTestInt(string key, int defaultValue = 1)
	{
		if (m_testList.TryGetValue(key, out var value) && int.TryParse(value, out var result))
		{
			return result;
		}
		return defaultValue;
	}

	public static string TryTest(string key, string defaultValue = "")
	{
		if (m_testList.TryGetValue(key, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public static int Increment(string key, int by = 1)
	{
		if (m_testList.TryGetValue(key, out var value))
		{
			m_testList[key] = (int.Parse(value) + by).ToString();
		}
		else
		{
			m_testList[key] = by.ToString();
		}
		return int.Parse(m_testList[key]);
	}

	public static void Log(object obj)
	{
		if (m_showTests)
		{
			ZLog.Log(obj);
			if ((bool)Console.instance)
			{
				Console.instance.AddString("Log", obj.ToString(), Talker.Type.Whisper, timestamp: true);
			}
		}
	}

	public static void LogWarning(object obj)
	{
		if (m_showTests)
		{
			ZLog.LogWarning(obj);
			if ((bool)Console.instance)
			{
				Console.instance.AddString("Warning", obj.ToString(), Talker.Type.Whisper, timestamp: true);
			}
		}
	}

	public static void LogError(object obj)
	{
		if (m_showTests)
		{
			ZLog.LogError(obj);
			if ((bool)Console.instance)
			{
				Console.instance.AddString("Warning", obj.ToString(), Talker.Type.Whisper, timestamp: true);
			}
		}
	}
}
