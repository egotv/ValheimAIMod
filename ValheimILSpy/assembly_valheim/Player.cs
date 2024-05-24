using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using Valheim.SettingsGui;
using Valheim.UI;

public class Player : Humanoid
{
	public enum RequirementMode
	{
		CanBuild,
		IsKnown,
		CanAlmostBuild
	}

	public class Food
	{
		public string m_name = "";

		public ItemDrop.ItemData m_item;

		public float m_time;

		public float m_health;

		public float m_stamina;

		public float m_eitr;

		public bool CanEatAgain()
		{
			return m_time < m_item.m_shared.m_foodBurnTime / 2f;
		}
	}

	public class MinorActionData
	{
		public enum ActionType
		{
			Equip,
			Unequip,
			Reload
		}

		public ActionType m_type;

		public ItemDrop.ItemData m_item;

		public string m_progressText = "";

		public float m_time;

		public float m_duration;

		public string m_animation = "";

		public string m_doneAnimation = "";

		public float m_staminaDrain;

		public float m_eitrDrain;

		public EffectList m_startEffect;
	}

	public enum PlacementStatus
	{
		Valid,
		Invalid,
		BlockedbyPlayer,
		NoBuildZone,
		PrivateZone,
		MoreSpace,
		NoTeleportArea,
		ExtensionMissingStation,
		WrongBiome,
		NeedCultivated,
		NeedDirt,
		NotInDungeon,
		NoRayHits
	}

	private class RaycastHitComparer : IComparer<RaycastHit>
	{
		public static RaycastHitComparer Instance = new RaycastHitComparer();

		public int Compare(RaycastHit x, RaycastHit y)
		{
			return x.distance.CompareTo(y.distance);
		}
	}

	private Vector3 m_lastDistCheck;

	private float m_statCheck;

	[Header("Effects")]
	public EffectList m_buttonEffects = new EffectList();

	private List<string> m_readyEvents = new List<string>();

	private float[] m_equipmentModifierValues;

	private static FieldInfo[] s_equipmentModifierSourceFields;

	private static readonly string[] s_equipmentModifierSources = new string[10] { "m_movementModifier", "m_homeItemsStaminaModifier", "m_heatResistanceModifier", "m_jumpStaminaModifier", "m_attackStaminaModifier", "m_blockStaminaModifier", "m_dodgeStaminaModifier", "m_swimStaminaModifier", "m_sneakStaminaModifier", "m_runStaminaModifier" };

	private static readonly string[] s_equipmentModifierTooltips = new string[10] { "$item_movement_modifier", "$base_item_modifier", "$item_heat_modifier", "$se_jumpstamina", "$se_attackstamina", "$se_blockstamina", "$se_dodgestamina", "$se_swimstamina", "$se_sneakstamina", "$se_runstamina" };

	private float m_baseValueUpdateTimer;

	private float m_rotatePieceTimer;

	private bool m_altPlace;

	public static Player m_localPlayer = null;

	private static readonly List<Player> s_players = new List<Player>();

	public static List<string> m_addUniqueKeyQueue = new List<string>();

	public static List<string> s_FilterCraft = new List<string>();

	public static bool m_debugMode = false;

	[Header("Player")]
	public float m_maxPlaceDistance = 5f;

	public float m_maxInteractDistance = 5f;

	public float m_scrollSens = 4f;

	public float m_staminaRegen = 5f;

	public float m_staminaRegenTimeMultiplier = 1f;

	public float m_staminaRegenDelay = 1f;

	public float m_runStaminaDrain = 10f;

	public float m_sneakStaminaDrain = 5f;

	public float m_swimStaminaDrainMinSkill = 5f;

	public float m_swimStaminaDrainMaxSkill = 2f;

	public float m_dodgeStaminaUsage = 10f;

	public float m_weightStaminaFactor = 0.1f;

	public float m_eiterRegen = 5f;

	public float m_eitrRegenDelay = 1f;

	public float m_autoPickupRange = 2f;

	public float m_maxCarryWeight = 300f;

	public float m_encumberedStaminaDrain = 10f;

	public float m_hardDeathCooldown = 10f;

	public float m_baseCameraShake = 4f;

	public float m_placeDelay = 0.4f;

	public float m_removeDelay = 0.25f;

	public EffectList m_drownEffects = new EffectList();

	public EffectList m_spawnEffects = new EffectList();

	public EffectList m_removeEffects = new EffectList();

	public EffectList m_dodgeEffects = new EffectList();

	public EffectList m_autopickupEffects = new EffectList();

	public EffectList m_skillLevelupEffects = new EffectList();

	public EffectList m_equipStartEffects = new EffectList();

	public GameObject m_placeMarker;

	public GameObject m_tombstone;

	public GameObject m_valkyrie;

	public Sprite m_textIcon;

	public float m_baseHP = 25f;

	public float m_baseStamina = 75f;

	public double m_wakeupTime;

	private Skills m_skills;

	private PieceTable m_buildPieces;

	private bool m_noPlacementCost;

	private const bool m_hideUnavailable = false;

	private static bool m_enableAutoPickup = true;

	private readonly HashSet<string> m_knownRecipes = new HashSet<string>();

	private readonly Dictionary<string, int> m_knownStations = new Dictionary<string, int>();

	private readonly HashSet<string> m_knownMaterial = new HashSet<string>();

	private readonly HashSet<string> m_shownTutorials = new HashSet<string>();

	private readonly HashSet<string> m_uniques = new HashSet<string>();

	private readonly HashSet<string> m_trophies = new HashSet<string>();

	private readonly HashSet<Heightmap.Biome> m_knownBiome = new HashSet<Heightmap.Biome>();

	private readonly Dictionary<string, string> m_knownTexts = new Dictionary<string, string>();

	private float m_stationDiscoverTimer;

	private bool m_debugFly;

	private bool m_godMode;

	private bool m_ghostMode;

	private float m_lookPitch;

	private const int m_maxFoods = 3;

	private const float m_foodDrainPerSec = 0.1f;

	private float m_foodUpdateTimer;

	private float m_foodRegenTimer;

	private readonly List<Food> m_foods = new List<Food>();

	private float m_stamina = 100f;

	private float m_maxStamina = 100f;

	private float m_staminaRegenTimer;

	private float m_eitr;

	private float m_maxEitr;

	private float m_eitrRegenTimer;

	private string m_guardianPower = "";

	private int m_guardianPowerHash;

	public float m_guardianPowerCooldown;

	private StatusEffect m_guardianSE;

	private float m_placePressedTime = -1000f;

	private float m_removePressedTime = -1000f;

	private bool m_blockRemove;

	private float m_lastToolUseTime;

	private GameObject m_placementMarkerInstance;

	private GameObject m_placementGhost;

	private string m_placementGhostLast;

	private PlacementStatus m_placementStatus = PlacementStatus.Invalid;

	private float m_placeRotationDegrees = 22.5f;

	private int m_placeRotation;

	private int m_placeRayMask;

	private int m_placeGroundRayMask;

	private int m_placeWaterRayMask;

	private int m_removeRayMask;

	private int m_interactMask;

	private int m_autoPickupMask;

	private readonly List<MinorActionData> m_actionQueue = new List<MinorActionData>();

	private float m_actionQueuePause;

	private string m_actionAnimation;

	private GameObject m_hovering;

	private Character m_hoveringCreature;

	private float m_lastHoverInteractTime;

	private bool m_pvp;

	private float m_updateCoverTimer;

	private float m_coverPercentage;

	private bool m_underRoof = true;

	private float m_nearFireTimer;

	private bool m_isLoading;

	private ItemDrop.ItemData m_weaponLoaded;

	private float m_queuedAttackTimer;

	private float m_queuedSecondAttackTimer;

	private float m_queuedDodgeTimer;

	private Vector3 m_queuedDodgeDir = Vector3.zero;

	private bool m_inDodge;

	private bool m_dodgeInvincible;

	private CraftingStation m_currentStation;

	private bool m_inCraftingStation;

	private Ragdoll m_ragdoll;

	private Piece m_hoveringPiece;

	private Dictionary<Material, float> m_ghostRippleDistance = new Dictionary<Material, float>();

	private bool m_attackTowardsPlayerLookDir;

	private string m_emoteState = "";

	private int m_emoteID;

	private bool m_intro;

	private bool m_firstSpawn = true;

	private bool m_crouchToggled;

	public bool m_autoRun;

	private bool m_safeInHome;

	private IDoodadController m_doodadController;

	private bool m_attached;

	private string m_attachAnimation = "";

	private bool m_sleeping;

	private bool m_attachedToShip;

	private Transform m_attachPoint;

	private Vector3 m_detachOffset = Vector3.zero;

	private Transform m_attachPointCamera;

	private Collider[] m_attachColliders;

	private int m_modelIndex;

	private Vector3 m_skinColor = Vector3.one;

	private Vector3 m_hairColor = Vector3.one;

	private bool m_teleporting;

	private bool m_distantTeleport;

	private float m_teleportTimer;

	private float m_teleportCooldown;

	private Vector3 m_teleportFromPos;

	private Quaternion m_teleportFromRot;

	private Vector3 m_teleportTargetPos;

	private Quaternion m_teleportTargetRot;

	private Heightmap.Biome m_currentBiome;

	private float m_biomeTimer;

	private List<string> m_tempUniqueKeys = new List<string>();

	private int m_baseValue;

	private int m_baseValueOld = -1;

	private int m_comfortLevel;

	private float m_drownDamageTimer;

	private float m_timeSinceTargeted;

	private float m_timeSinceSensed;

	private float m_stealthFactorUpdateTimer;

	private float m_stealthFactor;

	private float m_stealthFactorTarget;

	private Vector3 m_lastStealthPosition = Vector3.zero;

	private float m_lastVelocity;

	private float m_wakeupTimer = -1f;

	private float m_timeSinceDeath = 999999f;

	private float m_runSkillImproveTimer;

	private float m_swimSkillImproveTimer;

	private float m_sneakSkillImproveTimer;

	private int m_manualSnapPoint = -1;

	private readonly List<PieceTable> m_tempOwnedPieceTables = new List<PieceTable>();

	private readonly List<Transform> m_tempSnapPoints1 = new List<Transform>();

	private readonly List<Transform> m_tempSnapPoints2 = new List<Transform>();

	private readonly List<Piece> m_tempPieces = new List<Piece>();

	[HideInInspector]
	public Dictionary<string, string> m_customData = new Dictionary<string, string>();

	private static int s_attackMask = 0;

	private static readonly int s_crouching = ZSyncAnimation.GetHash("crouching");

	private static readonly int s_animatorTagDodge = ZSyncAnimation.GetHash("dodge");

	private static readonly int s_animatorTagCutscene = ZSyncAnimation.GetHash("cutscene");

	private static readonly int s_animatorTagCrouch = ZSyncAnimation.GetHash("crouch");

	private static readonly int s_animatorTagMinorAction = ZSyncAnimation.GetHash("minoraction");

	private static readonly int s_animatorTagMinorActionFast = ZSyncAnimation.GetHash("minoraction_fast");

	private static readonly int s_animatorTagEmote = ZSyncAnimation.GetHash("emote");

	public const string BaseValueKey = "baseValue";

	private int m_cachedFrame;

	private bool m_cachedAttack;

	[Header("Seasonal Items")]
	[SerializeField]
	private List<SeasonalItemGroup> m_seasonalItemGroups = new List<SeasonalItemGroup>();

	private SeasonalItemGroup m_currentSeason;

	private readonly RaycastHit[] m_raycastHoverHits = new RaycastHit[64];

	public bool AttackTowardsPlayerLookDir
	{
		get
		{
			return m_attackTowardsPlayerLookDir;
		}
		set
		{
			m_attackTowardsPlayerLookDir = value;
		}
	}

	public bool AlternativePlacementActive => m_altPlace;

	public SeasonalItemGroup CurrentSeason => m_currentSeason;

	protected override void Awake()
	{
		base.Awake();
		s_players.Add(this);
		m_skills = GetComponent<Skills>();
		SetupAwake();
		m_equipmentModifierValues = new float[s_equipmentModifierSources.Length];
		if (s_equipmentModifierSourceFields == null)
		{
			s_equipmentModifierSourceFields = new FieldInfo[s_equipmentModifierSources.Length];
			for (int i = 0; i < s_equipmentModifierSources.Length; i++)
			{
				s_equipmentModifierSourceFields[i] = typeof(ItemDrop.ItemData.SharedData).GetField(s_equipmentModifierSources[i], BindingFlags.Instance | BindingFlags.Public);
			}
			if (s_equipmentModifierSources.Length != s_equipmentModifierTooltips.Length)
			{
				ZLog.LogError("Equipment modifier tooltip missmatch in player!");
			}
		}
		if (m_nview.GetZDO() == null)
		{
			return;
		}
		m_placeRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");
		m_placeWaterRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "Water", "vehicle");
		m_removeRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "vehicle");
		m_interactMask = LayerMask.GetMask("item", "piece", "piece_nonsolid", "Default", "static_solid", "Default_small", "character", "character_net", "terrain", "vehicle");
		m_autoPickupMask = LayerMask.GetMask("item");
		Inventory inventory = m_inventory;
		inventory.m_onChanged = (Action)Delegate.Combine(inventory.m_onChanged, new Action(OnInventoryChanged));
		if (s_attackMask == 0)
		{
			s_attackMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
		}
		m_nview.Register("OnDeath", RPC_OnDeath);
		if (m_nview.IsOwner())
		{
			m_nview.Register<int, string, int>("Message", RPC_Message);
			m_nview.Register<bool, bool>("OnTargeted", RPC_OnTargeted);
			m_nview.Register<float>("UseStamina", RPC_UseStamina);
			if ((bool)MusicMan.instance)
			{
				MusicMan.instance.TriggerMusic("Wakeup");
			}
			UpdateKnownRecipesList();
			UpdateAvailablePiecesList();
			SetupPlacementGhost();
		}
		m_placeRotation = UnityEngine.Random.Range(0, 16);
		float f = UnityEngine.Random.Range(0f, (float)Math.PI * 2f);
		SetLookDir(new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f)));
		FaceLookDirection();
		AddQueuedKeys();
		UpdateCurrentSeason();
		m_attackTowardsPlayerLookDir = PlatformPrefs.GetInt("AttackTowardsPlayerLookDir", 1) == 1;
	}

	protected override void OnEnable()
	{
		base.OnEnable();
	}

	protected override void OnDisable()
	{
		base.OnDisable();
	}

	public void SetLocalPlayer()
	{
		if (!(m_localPlayer == this))
		{
			m_localPlayer = this;
			Game.instance.IncrementPlayerStat(PlayerStatType.WorldLoads);
			ZNet.instance.SetReferencePosition(base.transform.position);
			EnvMan.instance.SetForceEnvironment("");
			AddQueuedKeys();
		}
	}

	private void AddQueuedKeys()
	{
		if (m_addUniqueKeyQueue.Count <= 0)
		{
			return;
		}
		foreach (string item in m_addUniqueKeyQueue)
		{
			AddUniqueKey(item);
		}
		m_addUniqueKeyQueue.Clear();
	}

	public void SetPlayerID(long playerID, string name)
	{
		if (m_nview.GetZDO() != null && GetPlayerID() == 0L)
		{
			m_nview.GetZDO().Set(ZDOVars.s_playerID, playerID);
			m_nview.GetZDO().Set(ZDOVars.s_playerName, name);
		}
	}

	public long GetPlayerID()
	{
		if (!m_nview.IsValid())
		{
			return 0L;
		}
		return m_nview.GetZDO().GetLong(ZDOVars.s_playerID, 0L);
	}

	public string GetPlayerName()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		return m_nview.GetZDO().GetString(ZDOVars.s_playerName, "...");
	}

	public override string GetHoverText()
	{
		return "";
	}

	public override string GetHoverName()
	{
		return CensorShittyWords.FilterUGC(GetPlayerName(), UGCType.CharacterName, null, GetPlayerID());
	}

	protected override void Start()
	{
		base.Start();
		InvalidateCachedLiquidDepth();
	}

	protected override void OnDestroy()
	{
		ZDO zDO = m_nview.GetZDO();
		if (zDO != null && ZNet.instance != null)
		{
			ZLog.LogWarning("Player destroyed sec:" + zDO.GetSector().ToString() + "  pos:" + base.transform.position.ToString() + "  zdopos:" + zDO.GetPosition().ToString() + "  ref " + ZNet.instance.GetReferencePosition().ToString());
		}
		if ((bool)m_placementGhost)
		{
			UnityEngine.Object.Destroy(m_placementGhost);
			m_placementGhost = null;
		}
		base.OnDestroy();
		s_players.Remove(this);
		if (m_localPlayer == this)
		{
			ZLog.LogWarning("Local player destroyed");
			m_localPlayer = null;
		}
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		UpdateAwake(fixedDeltaTime);
		if (m_nview.GetZDO() == null)
		{
			return;
		}
		UpdateTargeted(fixedDeltaTime);
		if (!m_nview.IsOwner())
		{
			return;
		}
		if (m_localPlayer != this)
		{
			ZLog.Log("Destroying old local player");
			ZNetScene.instance.Destroy(base.gameObject);
		}
		else if (!IsDead())
		{
			UpdateActionQueue(fixedDeltaTime);
			PlayerAttackInput(fixedDeltaTime);
			UpdateAttach();
			UpdateDoodadControls(fixedDeltaTime);
			UpdateCrouch(fixedDeltaTime);
			UpdateDodge(fixedDeltaTime);
			UpdateCover(fixedDeltaTime);
			UpdateStations(fixedDeltaTime);
			UpdateGuardianPower(fixedDeltaTime);
			UpdateBaseValue(fixedDeltaTime);
			UpdateStats(fixedDeltaTime);
			UpdateTeleport(fixedDeltaTime);
			AutoPickup(fixedDeltaTime);
			EdgeOfWorldKill(fixedDeltaTime);
			UpdateBiome(fixedDeltaTime);
			UpdateStealth(fixedDeltaTime);
			if ((bool)GameCamera.instance && m_attachPointCamera == null && Vector3.Distance(GameCamera.instance.transform.position, base.transform.position) < 2f)
			{
				SetVisible(visible: false);
			}
			AudioMan.instance.SetIndoor(InShelter() || ShieldGenerator.IsInsideShield(base.transform.position));
		}
	}

	private void Update()
	{
		bool flag = InventoryGui.IsVisible();
		if (ZInput.InputLayout != 0 && ZInput.IsGamepadActive() && !flag && ZInput.GetButtonUp("JoyAltPlace") && ZInput.GetButton("JoyAltKeys"))
		{
			m_altPlace = !m_altPlace;
			if (MessageHud.instance != null)
			{
				string text = Localization.instance.Localize("$hud_altplacement");
				string text2 = (m_altPlace ? Localization.instance.Localize("$hud_on") : Localization.instance.Localize("$hud_off"));
				MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text + " " + text2);
			}
		}
		UpdateClothFix();
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		bool flag2 = TakeInput();
		UpdateHover();
		bool num;
		if (flag2)
		{
			if (m_debugMode && Console.instance.IsCheatsEnabled())
			{
				if (ZInput.GetKeyDown(KeyCode.Z))
				{
					ToggleDebugFly();
				}
				if (ZInput.GetKeyDown(KeyCode.B))
				{
					ToggleNoPlacementCost();
				}
				if (ZInput.GetKeyDown(KeyCode.K))
				{
					Console.instance.TryRunCommand("killenemies");
				}
				if (ZInput.GetKeyDown(KeyCode.L))
				{
					Console.instance.TryRunCommand("removedrops");
				}
			}
			bool alt = ((ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive()) ? ZInput.GetButton("JoyAltKeys") : (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltPlace")));
			if ((ZInput.GetButtonDown("Use") || ZInput.GetButtonDown("JoyUse")) && !Hud.InRadial())
			{
				if ((bool)m_hovering)
				{
					Interact(m_hovering, hold: false, alt);
				}
				else if (m_doodadController != null)
				{
					StopDoodadControl();
				}
			}
			else if ((ZInput.GetButton("Use") || ZInput.GetButton("JoyUse")) && !Hud.InRadial() && (bool)m_hovering)
			{
				Interact(m_hovering, hold: true, alt);
			}
			bool flag3 = ZInput.GetButtonUp("JoyHide") && ZInput.GetButtonLastPressedTimer("JoyHide") < 0.33f;
			if (ZInput.InputLayout == InputLayout.Default || !ZInput.IsGamepadActive())
			{
				if (ZInput.GetButtonDown("Hide"))
				{
					goto IL_0283;
				}
				if (flag3 && !ZInput.GetButton("JoyAltKeys"))
				{
					num = !InPlaceMode();
					goto IL_0281;
				}
			}
			else if (!InPlaceMode() && flag3)
			{
				num = !ZInput.GetButton("JoyAltKeys");
				goto IL_0281;
			}
			goto IL_02c9;
		}
		goto IL_04e3;
		IL_04e3:
		UpdatePlacement(flag2, Time.deltaTime);
		UpdateStats();
		return;
		IL_0283:
		if (GetRightItem() != null || GetLeftItem() != null)
		{
			if (!InAttack() && !InDodge())
			{
				HideHandItems();
			}
		}
		else if ((!IsSwimming() || IsOnGround()) && !InDodge())
		{
			ShowHandItems();
		}
		goto IL_02c9;
		IL_02c9:
		if (ZInput.GetButtonDown("ToggleWalk") && !Hud.InRadial())
		{
			SetWalk(!GetWalk());
			if (GetWalk())
			{
				Message(MessageHud.MessageType.TopLeft, "$msg_walk $hud_on");
			}
			else
			{
				Message(MessageHud.MessageType.TopLeft, "$msg_walk $hud_off");
			}
		}
		if (UISettings.UseRadials)
		{
			HandleRadialInput();
		}
		else if ((!InPlaceMode() && !ZInput.GetButton("JoyAltKeys") && ZInput.GetButtonUp("JoySit")) || ZInput.GetButtonDown("Sit"))
		{
			if (InEmote() && IsSitting())
			{
				StopEmote();
			}
			else
			{
				StartEmote("sit", oneshot: false);
			}
		}
		bool flag4 = ZInput.IsGamepadActive() && !ZInput.GetButton("JoyAltKeys");
		bool flag5 = ZInput.InputLayout == InputLayout.Default && ZInput.GetButtonDown("JoyGP");
		bool flag6 = ZInput.IsNonClassicFunctionality() && ZInput.GetButton("JoyLStick") && ZInput.GetButton("JoyRStick");
		if (!Hud.InRadial() && !Hud.IsPieceSelectionVisible() && (ZInput.GetButtonDown("GP") || (flag4 && (flag5 || flag6))))
		{
			StartGuardianPower();
		}
		bool flag7 = ZInput.GetButtonDown("JoyAutoPickup") && ZInput.GetButton("JoyAltKeys");
		if (ZInput.GetButtonDown("AutoPickup") || flag7)
		{
			m_enableAutoPickup = !m_enableAutoPickup;
			Message(MessageHud.MessageType.TopLeft, "$hud_autopickup:" + (m_enableAutoPickup ? "$hud_on" : "$hud_off"));
		}
		if (ZInput.GetButtonDown("Hotbar1"))
		{
			UseHotbarItem(1);
		}
		if (ZInput.GetButtonDown("Hotbar2"))
		{
			UseHotbarItem(2);
		}
		if (ZInput.GetButtonDown("Hotbar3"))
		{
			UseHotbarItem(3);
		}
		if (ZInput.GetButtonDown("Hotbar4"))
		{
			UseHotbarItem(4);
		}
		if (ZInput.GetButtonDown("Hotbar5"))
		{
			UseHotbarItem(5);
		}
		if (ZInput.GetButtonDown("Hotbar6"))
		{
			UseHotbarItem(6);
		}
		if (ZInput.GetButtonDown("Hotbar7"))
		{
			UseHotbarItem(7);
		}
		if (ZInput.GetButtonDown("Hotbar8"))
		{
			UseHotbarItem(8);
		}
		goto IL_04e3;
		IL_0281:
		if (num)
		{
			goto IL_0283;
		}
		goto IL_02c9;
	}

	private void UpdateClothFix()
	{
		float magnitude = GetVelocity().magnitude;
		if (magnitude > 0.01f && m_lastVelocity < 0.01f)
		{
			ResetCloth();
			Terminal.Increment("resetcloth");
		}
		m_lastVelocity = magnitude;
	}

	private void HandleRadialInput()
	{
		if (Hud.InRadial())
		{
			return;
		}
		if (!Hud.instance.m_radialMenu.CanOpen)
		{
			DynamicRadialMenu radialMenu = Hud.instance.m_radialMenu;
			radialMenu.CanOpen = ZInput.InputLayout switch
			{
				InputLayout.Alternative1 => ZInput.GetButtonDown("JoyLTrigger"), 
				InputLayout.Alternative2 => ZInput.GetButtonDown("JoyLBumper"), 
				_ => false, 
			};
		}
		if (!ZInput.GetButton("JoyAltKeys") && ((ZInput.InputLayout == InputLayout.Alternative1 && ZInput.GetButtonPressedTimer("JoyLTrigger") > 0.33f) || (ZInput.InputLayout == InputLayout.Alternative2 && ZInput.GetButtonPressedTimer("JoyLBumper") > 0.33f)))
		{
			Hud.instance.m_radialMenu.Open(Hud.instance.m_config);
		}
		if ((!ZInput.GetButton("JoyAltKeys") && ZInput.GetButtonPressedTimer("JoySit") > 0.33f) || ZInput.GetKeyDown(KeyCode.BackQuote))
		{
			Hud.instance.m_radialMenu.CanOpen = true;
			Hud.instance.m_radialMenu.Open(Hud.instance.m_config);
		}
		else if ((!ZInput.GetButton("JoyAltKeys") && ZInput.GetButtonUp("JoySit")) || ZInput.GetButtonDown("Sit"))
		{
			if (InEmote() && IsSitting())
			{
				StopEmote();
			}
			else
			{
				StartEmote("sit", oneshot: false);
			}
		}
	}

	private void UpdateStats()
	{
		if (IsDebugFlying())
		{
			return;
		}
		m_statCheck += Time.deltaTime;
		if (m_statCheck < 0.5f)
		{
			return;
		}
		m_statCheck = 0f;
		PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
		playerProfile.IncrementStat(IsSafeInHome() ? PlayerStatType.TimeInBase : PlayerStatType.TimeOutOfBase, 0.5f);
		float num = Vector3.Distance(base.transform.position, m_lastDistCheck);
		if (!(num > 1f))
		{
			return;
		}
		if (num < 20f)
		{
			playerProfile.IncrementStat(PlayerStatType.DistanceTraveled, num);
			if (Ship.GetLocalShip() != null)
			{
				playerProfile.IncrementStat(PlayerStatType.DistanceSail);
			}
			else if (IsOnGround())
			{
				playerProfile.IncrementStat(IsRunning() ? PlayerStatType.DistanceRun : PlayerStatType.DistanceWalk, num);
			}
			else
			{
				playerProfile.IncrementStat(PlayerStatType.DistanceAir, num);
			}
		}
		m_lastDistCheck = base.transform.position;
	}

	private void UpdatePlacement(bool takeInput, float dt)
	{
		UpdateWearNTearHover();
		if (InPlaceMode() && !IsDead())
		{
			if (!takeInput)
			{
				return;
			}
			UpdateBuildGuiInput();
			if (Hud.IsPieceSelectionVisible())
			{
				return;
			}
			ItemDrop.ItemData rightItem = GetRightItem();
			if ((ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltKeys")) && (ZInput.GetButtonDown("JoyLStick") || ZInput.GetButtonDown("JoyRStick") || ZInput.GetButtonDown("JoyButtonA") || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("JoyButtonX") || ZInput.GetButtonDown("JoyButtonY") || ZInput.GetButtonDown("JoyDPadUp") || ZInput.GetButtonDown("JoyDPadDown") || ZInput.GetButtonDown("JoyDPadLeft") || ZInput.GetButtonDown("JoyDPadRight")))
			{
				m_blockRemove = true;
			}
			if ((ZInput.GetButtonDown("Remove") || ZInput.GetButtonDown("JoyRemove")) && (ZInput.GetButton("AltPlace") || ZInput.GetButton("JoyAltKeys")) && (ZInput.InputLayout == InputLayout.Default || !ZInput.IsGamepadActive()))
			{
				CopyPiece();
				m_blockRemove = true;
			}
			else if (!m_blockRemove && (ZInput.GetButtonUp("Remove") || ZInput.GetButtonUp("JoyRemove")))
			{
				m_removePressedTime = Time.time;
			}
			if (!ZInput.GetButton("AltPlace") && !ZInput.GetButton("JoyAltKeys"))
			{
				m_blockRemove = false;
			}
			if (Time.time - m_removePressedTime < 0.2f && rightItem.m_shared.m_buildPieces.m_canRemovePieces && Time.time - m_lastToolUseTime > m_removeDelay)
			{
				m_removePressedTime = -9999f;
				if (HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
				{
					if (RemovePiece())
					{
						m_lastToolUseTime = Time.time;
						AddNoise(50f);
						UseStamina(rightItem.m_shared.m_attack.m_attackStamina, isHomeUsage: true);
						if (rightItem.m_shared.m_useDurability)
						{
							rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
						}
					}
				}
				else
				{
					Hud.instance.StaminaBarEmptyFlash();
				}
			}
			if ((ZInput.GetButtonDown("Attack") || ZInput.GetButtonDown("JoyPlace")) && !Hud.InRadial())
			{
				m_placePressedTime = Time.time;
			}
			if (Time.time - m_placePressedTime < 0.2f && Time.time - m_lastToolUseTime > m_placeDelay)
			{
				m_placePressedTime = -9999f;
				if (ZInput.GetButton("JoyAltKeys"))
				{
					CopyPiece();
					m_blockRemove = true;
				}
				else
				{
					Piece selectedPiece = m_buildPieces.GetSelectedPiece();
					if (selectedPiece != null)
					{
						if (HaveStamina(rightItem.m_shared.m_attack.m_attackStamina))
						{
							if (selectedPiece.m_repairPiece)
							{
								Repair(rightItem, selectedPiece);
							}
							else if (m_placementGhost != null)
							{
								if (m_noPlacementCost || HaveRequirements(selectedPiece, RequirementMode.CanBuild))
								{
									if (PlacePiece(selectedPiece))
									{
										m_lastToolUseTime = Time.time;
										if (!ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost))
										{
											ConsumeResources(selectedPiece.m_resources, 0);
										}
										UseStamina(rightItem.m_shared.m_attack.m_attackStamina, isHomeUsage: true);
										if (rightItem.m_shared.m_useDurability)
										{
											rightItem.m_durability -= rightItem.m_shared.m_useDurabilityDrain;
										}
									}
								}
								else
								{
									Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
								}
							}
						}
						else
						{
							Hud.instance.StaminaBarEmptyFlash();
						}
					}
				}
			}
			if ((bool)m_placementGhost)
			{
				m_placementGhost.gameObject.GetComponent<IPieceMarker>()?.ShowBuildMarker();
			}
			Piece hoveringPiece = GetHoveringPiece();
			if ((bool)hoveringPiece)
			{
				hoveringPiece.gameObject.GetComponent<IPieceMarker>()?.ShowHoverMarker();
			}
			if ((bool)m_placementGhost)
			{
				Piece component = m_placementGhost.GetComponent<Piece>();
				if ((object)component != null && component.m_canRotate && m_placementGhost.activeInHierarchy)
				{
					if (ZInput.GetMouseScrollWheel() < 0f)
					{
						m_placeRotation--;
					}
					if (ZInput.GetMouseScrollWheel() > 0f)
					{
						m_placeRotation++;
					}
				}
			}
			float num = 0f;
			bool flag = false;
			if (ZInput.IsGamepadActive())
			{
				switch (ZInput.InputLayout)
				{
				case InputLayout.Alternative1:
				{
					bool button2 = ZInput.GetButton("JoyRotate");
					bool button3 = ZInput.GetButton("JoyRotateRight");
					flag = button2 || button3;
					if (button2)
					{
						num = 0.5f;
					}
					else if (button3)
					{
						num = -0.5f;
					}
					break;
				}
				case InputLayout.Alternative2:
				{
					bool num2 = ZInput.GetButtonLastPressedTimer("JoyRotate") < 0.33f && ZInput.GetButtonUp("JoyRotate");
					bool button = ZInput.GetButton("JoyRotateRight");
					flag = num2 || button;
					if (num2)
					{
						num = 0.5f;
					}
					else if (button)
					{
						num = -0.5f;
					}
					break;
				}
				case InputLayout.Default:
					num = ZInput.GetJoyRightStickX();
					flag = ZInput.GetButton("JoyRotate") && Mathf.Abs(num) > 0.5f;
					break;
				}
			}
			if (flag)
			{
				if (m_rotatePieceTimer == 0f)
				{
					if (num < 0f)
					{
						m_placeRotation++;
					}
					else
					{
						m_placeRotation--;
					}
				}
				else if (m_rotatePieceTimer > 0.25f)
				{
					if (num < 0f)
					{
						m_placeRotation++;
					}
					else
					{
						m_placeRotation--;
					}
					m_rotatePieceTimer = 0.17f;
				}
				m_rotatePieceTimer += dt;
			}
			else
			{
				m_rotatePieceTimer = 0f;
			}
			{
				foreach (KeyValuePair<Material, float> item in m_ghostRippleDistance)
				{
					item.Key.SetFloat("_RippleDistance", ZInput.GetKey(KeyCode.LeftControl) ? item.Value : 0f);
				}
				return;
			}
		}
		if ((bool)m_placementGhost)
		{
			m_placementGhost.SetActive(value: false);
		}
	}

	private void UpdateBuildGuiInputAlternative1()
	{
		if (!Hud.IsPieceSelectionVisible() && ZInput.GetButtonDown("JoyBuildMenu") && !PlayerController.HasInputDelay && !Hud.InRadial())
		{
			for (int i = 0; i < m_buildPieces.m_selectedPiece.Length; i++)
			{
				m_buildPieces.m_lastSelectedPiece[i] = m_buildPieces.m_selectedPiece[i];
			}
			Hud.instance.TogglePieceSelection();
		}
		else
		{
			if (!Hud.IsPieceSelectionVisible())
			{
				return;
			}
			if (ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("BuildMenu"))
			{
				for (int j = 0; j < m_buildPieces.m_selectedPiece.Length; j++)
				{
					m_buildPieces.m_selectedPiece[j] = m_buildPieces.m_lastSelectedPiece[j];
				}
				Hud.HidePieceSelection();
				SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyButtonA"))
			{
				Hud.HidePieceSelection();
				PlayButtonSound();
			}
			if (ZInput.GetButtonDown("JoyTabLeft") || ZInput.GetButtonDown("TabLeft") || ZInput.GetMouseScrollWheel() > 0f)
			{
				m_buildPieces.PrevCategory();
				UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyTabRight") || ZInput.GetButtonDown("TabRight") || ZInput.GetMouseScrollWheel() < 0f)
			{
				m_buildPieces.NextCategory();
				UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyDPadLeft"))
			{
				m_buildPieces.LeftPiece();
				SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyDPadRight"))
			{
				m_buildPieces.RightPiece();
				SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				m_buildPieces.UpPiece();
				SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				m_buildPieces.DownPiece();
				SetupPlacementGhost();
			}
		}
	}

	private void UpdateBuildGuiInput()
	{
		if (ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive())
		{
			UpdateBuildGuiInputAlternative1();
		}
		else if (!Hud.IsPieceSelectionVisible())
		{
			if (Hud.instance.IsQuickPieceSelectEnabled())
			{
				if (!Hud.IsPieceSelectionVisible() && ZInput.GetButtonDown("BuildMenu") && !PlayerController.HasInputDelay && !Hud.InRadial())
				{
					Hud.instance.TogglePieceSelection();
				}
			}
			else if (ZInput.GetButtonDown("BuildMenu") && !PlayerController.HasInputDelay && !Hud.InRadial())
			{
				Hud.instance.TogglePieceSelection();
			}
			if (ZInput.GetButtonDown("JoyUse") && !PlayerController.HasInputDelay && !Hud.InRadial())
			{
				Hud.instance.TogglePieceSelection();
			}
		}
		else if (Hud.IsPieceSelectionVisible())
		{
			if (ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetButtonDown("BuildMenu"))
			{
				Hud.HidePieceSelection();
			}
			if (ZInput.GetButtonDown("JoyUse"))
			{
				Hud.HidePieceSelection();
				PlayButtonSound();
			}
			if (ZInput.GetButtonDown("JoyTabLeft") || ZInput.GetButtonDown("TabLeft") || ZInput.GetMouseScrollWheel() > 0f)
			{
				m_buildPieces.PrevCategory();
				UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyTabRight") || ZInput.GetButtonDown("TabRight") || ZInput.GetMouseScrollWheel() < 0f)
			{
				m_buildPieces.NextCategory();
				UpdateAvailablePiecesList();
			}
			if (ZInput.GetButtonDown("JoyLStickLeft") || ZInput.GetButtonDown("JoyDPadLeft"))
			{
				m_buildPieces.LeftPiece();
				SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickRight") || ZInput.GetButtonDown("JoyDPadRight"))
			{
				m_buildPieces.RightPiece();
				SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickUp") || ZInput.GetButtonDown("JoyDPadUp"))
			{
				m_buildPieces.UpPiece();
				SetupPlacementGhost();
			}
			if (ZInput.GetButtonDown("JoyLStickDown") || ZInput.GetButtonDown("JoyDPadDown"))
			{
				m_buildPieces.DownPiece();
				SetupPlacementGhost();
			}
		}
	}

	private void PlayButtonSound()
	{
		if ((bool)m_localPlayer)
		{
			m_buttonEffects?.Create(m_localPlayer.transform.position, Quaternion.identity);
		}
	}

	public bool SetSelectedPiece(Piece p)
	{
		if (m_buildPieces.GetPieceIndex(p, out var index, out var category))
		{
			SetBuildCategory(category);
			SetSelectedPiece(index);
			return true;
		}
		return false;
	}

	public void SetSelectedPiece(Vector2Int p)
	{
		if ((bool)m_buildPieces && m_buildPieces.GetSelectedIndex() != p)
		{
			m_buildPieces.SetSelected(p);
			SetupPlacementGhost();
		}
	}

	public Piece GetPiece(Vector2Int p)
	{
		if (!(m_buildPieces != null))
		{
			return null;
		}
		return m_buildPieces.GetPiece(p);
	}

	public bool IsPieceAvailable(Piece piece)
	{
		if (m_buildPieces != null)
		{
			return m_buildPieces.IsPieceAvailable(piece);
		}
		return false;
	}

	public Piece GetSelectedPiece()
	{
		if (!(m_buildPieces != null))
		{
			return null;
		}
		return m_buildPieces.GetSelectedPiece();
	}

	private void LateUpdate()
	{
		if (m_nview.IsValid())
		{
			UpdateEmote();
			if (m_nview.IsOwner())
			{
				ZNet.instance.SetReferencePosition(base.transform.position);
				UpdatePlacementGhost(flashGuardStone: false);
			}
		}
	}

	public void UpdateEvents()
	{
		if (!RandEventSystem.instance)
		{
			return;
		}
		m_readyEvents.Clear();
		foreach (RandomEvent @event in RandEventSystem.instance.m_events)
		{
			if (RandEventSystem.instance.PlayerIsReadyForEvent(this, @event))
			{
				m_readyEvents.Add(@event.m_name);
			}
		}
		if ((bool)ZNet.instance)
		{
			RandEventSystem.SetRandomEventsNeedsRefresh();
			ZNet.instance.m_serverSyncedPlayerData["possibleEvents"] = string.Join(",", m_readyEvents);
		}
	}

	private void SetupAwake()
	{
		if (m_nview.GetZDO() == null)
		{
			m_animator.SetBool("wakeup", value: false);
			return;
		}
		bool @bool = m_nview.GetZDO().GetBool(ZDOVars.s_wakeup, defaultValue: true);
		m_animator.SetBool("wakeup", @bool);
		if (@bool)
		{
			m_wakeupTimer = 0f;
		}
	}

	private void UpdateAwake(float dt)
	{
		if (!(m_wakeupTimer >= 0f))
		{
			return;
		}
		m_wakeupTimer += dt;
		if (m_wakeupTimer > 1f)
		{
			m_wakeupTimer = -1f;
			m_animator.SetBool("wakeup", value: false);
			if (m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_wakeup, value: false);
			}
		}
	}

	private void EdgeOfWorldKill(float dt)
	{
		if (!IsDead())
		{
			float num = Utils.DistanceXZ(Vector3.zero, base.transform.position);
			float num2 = 10420f;
			if (num > num2 && (IsSwimming() || base.transform.position.y < 30f))
			{
				Vector3 vector = Vector3.Normalize(base.transform.position);
				float num3 = Utils.LerpStep(num2, 10500f, num) * 10f;
				m_body.MovePosition(m_body.position + vector * num3 * dt);
			}
			if (num > num2 && base.transform.position.y < -10f)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = 99999f;
				hitData.m_hitType = HitData.HitType.EdgeOfWorld;
				Damage(hitData);
			}
		}
	}

	private void AutoPickup(float dt)
	{
		if (IsTeleporting() || !m_enableAutoPickup)
		{
			return;
		}
		Vector3 vector = base.transform.position + Vector3.up;
		Collider[] array = Physics.OverlapSphere(vector, m_autoPickupRange, m_autoPickupMask);
		foreach (Collider collider in array)
		{
			if (!collider.attachedRigidbody)
			{
				continue;
			}
			ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
			FloatingTerrainDummy floatingTerrainDummy = null;
			if (component == null && (bool)(floatingTerrainDummy = collider.attachedRigidbody.gameObject.GetComponent<FloatingTerrainDummy>()) && (bool)floatingTerrainDummy)
			{
				component = floatingTerrainDummy.m_parent.gameObject.GetComponent<ItemDrop>();
			}
			if (component == null || !component.m_autoPickup || HaveUniqueKey(component.m_itemData.m_shared.m_name) || !component.GetComponent<ZNetView>().IsValid())
			{
				continue;
			}
			if (!component.CanPickup())
			{
				component.RequestOwn();
			}
			else
			{
				if (component.InTar())
				{
					continue;
				}
				component.Load();
				if (!m_inventory.CanAddItem(component.m_itemData) || component.m_itemData.GetWeight() + m_inventory.GetTotalWeight() > GetMaxCarryWeight())
				{
					continue;
				}
				float num = Vector3.Distance(component.transform.position, vector);
				if (num > m_autoPickupRange)
				{
					continue;
				}
				if (num < 0.3f)
				{
					Pickup(component.gameObject);
					continue;
				}
				Vector3 vector2 = Vector3.Normalize(vector - component.transform.position);
				float num2 = 15f;
				Vector3 vector3 = vector2 * num2 * dt;
				component.transform.position += vector3;
				if ((bool)floatingTerrainDummy)
				{
					floatingTerrainDummy.transform.position += vector3;
				}
			}
		}
	}

	private void PlayerAttackInput(float dt)
	{
		if (InPlaceMode())
		{
			return;
		}
		ItemDrop.ItemData currentWeapon = GetCurrentWeapon();
		UpdateWeaponLoading(currentWeapon, dt);
		if (currentWeapon != null && currentWeapon.m_shared.m_attack.m_bowDraw)
		{
			UpdateAttackBowDraw(currentWeapon, dt);
		}
		else
		{
			if (m_attack)
			{
				m_queuedAttackTimer = 0.5f;
				m_queuedSecondAttackTimer = 0f;
			}
			if (m_secondaryAttack)
			{
				m_queuedSecondAttackTimer = 0.5f;
				m_queuedAttackTimer = 0f;
			}
			m_queuedAttackTimer -= Time.fixedDeltaTime;
			m_queuedSecondAttackTimer -= Time.fixedDeltaTime;
			if ((m_queuedAttackTimer > 0f || m_attackHold) && StartAttack(null, secondaryAttack: false))
			{
				m_queuedAttackTimer = 0f;
			}
			if ((m_queuedSecondAttackTimer > 0f || m_secondaryAttackHold) && StartAttack(null, secondaryAttack: true))
			{
				m_queuedSecondAttackTimer = 0f;
			}
		}
		if (m_currentAttack != null && m_currentAttack.m_loopingAttack && !(m_currentAttackIsSecondary ? m_secondaryAttackHold : m_attackHold))
		{
			m_currentAttack.Abort();
		}
	}

	private void UpdateWeaponLoading(ItemDrop.ItemData weapon, float dt)
	{
		if (weapon == null || !weapon.m_shared.m_attack.m_requiresReload)
		{
			SetWeaponLoaded(null);
		}
		else if (m_weaponLoaded != weapon && weapon.m_shared.m_attack.m_requiresReload && !IsReloadActionQueued() && TryUseEitr(weapon.m_shared.m_attack.m_reloadEitrDrain))
		{
			QueueReloadAction();
		}
	}

	private void CancelReloadAction()
	{
		foreach (MinorActionData item in m_actionQueue)
		{
			if (item.m_type == MinorActionData.ActionType.Reload)
			{
				m_actionQueue.Remove(item);
				break;
			}
		}
	}

	public override void ResetLoadedWeapon()
	{
		SetWeaponLoaded(null);
		foreach (MinorActionData item in m_actionQueue)
		{
			if (item.m_type == MinorActionData.ActionType.Reload)
			{
				m_actionQueue.Remove(item);
				break;
			}
		}
	}

	private void SetWeaponLoaded(ItemDrop.ItemData weapon)
	{
		if (weapon != m_weaponLoaded)
		{
			m_weaponLoaded = weapon;
			m_nview.GetZDO().Set(ZDOVars.s_weaponLoaded, weapon != null);
		}
	}

	public override bool IsWeaponLoaded()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (!m_nview.IsOwner())
		{
			return m_nview.GetZDO().GetBool(ZDOVars.s_weaponLoaded);
		}
		return m_weaponLoaded != null;
	}

	private void UpdateAttackBowDraw(ItemDrop.ItemData weapon, float dt)
	{
		if (m_blocking || InMinorAction() || IsAttached())
		{
			m_attackDrawTime = -1f;
			if (!string.IsNullOrEmpty(weapon.m_shared.m_attack.m_drawAnimationState))
			{
				m_zanim.SetBool(weapon.m_shared.m_attack.m_drawAnimationState, value: false);
			}
			return;
		}
		float num = weapon.GetDrawStaminaDrain();
		float drawEitrDrain = weapon.GetDrawEitrDrain();
		if ((double)GetAttackDrawPercentage() >= 1.0)
		{
			num *= 0.5f;
		}
		num += num * GetEquipmentAttackStaminaModifier();
		m_seman.ModifyAttackStaminaUsage(num, ref num);
		bool flag = num <= 0f || HaveStamina();
		bool flag2 = drawEitrDrain <= 0f || HaveEitr();
		if (m_attackDrawTime < 0f)
		{
			if (!m_attackHold)
			{
				m_attackDrawTime = 0f;
			}
		}
		else if (m_attackHold && flag && m_attackDrawTime >= 0f)
		{
			if (m_attackDrawTime == 0f)
			{
				if (!weapon.m_shared.m_attack.StartDraw(this, weapon))
				{
					m_attackDrawTime = -1f;
					return;
				}
				weapon.m_shared.m_holdStartEffect.Create(base.transform.position, Quaternion.identity, base.transform);
			}
			m_attackDrawTime += Time.fixedDeltaTime;
			if (!string.IsNullOrEmpty(weapon.m_shared.m_attack.m_drawAnimationState))
			{
				m_zanim.SetBool(weapon.m_shared.m_attack.m_drawAnimationState, value: true);
				m_zanim.SetFloat("drawpercent", GetAttackDrawPercentage());
			}
			UseStamina(num * dt);
			UseEitr(drawEitrDrain * dt);
		}
		else if (m_attackDrawTime > 0f)
		{
			if (flag && flag2)
			{
				StartAttack(null, secondaryAttack: false);
			}
			if (!string.IsNullOrEmpty(weapon.m_shared.m_attack.m_drawAnimationState))
			{
				m_zanim.SetBool(weapon.m_shared.m_attack.m_drawAnimationState, value: false);
			}
			m_attackDrawTime = 0f;
		}
	}

	protected override bool HaveQueuedChain()
	{
		if ((m_queuedAttackTimer > 0f || m_attackHold) && GetCurrentWeapon() != null && m_currentAttack != null)
		{
			return m_currentAttack.CanStartChainAttack();
		}
		return false;
	}

	private void UpdateBaseValue(float dt)
	{
		m_baseValueUpdateTimer += dt;
		if (m_baseValueUpdateTimer > 2f)
		{
			m_baseValueUpdateTimer = 0f;
			m_baseValue = EffectArea.GetBaseValue(base.transform.position, 20f);
			m_comfortLevel = SE_Rested.CalculateComfortLevel(this);
			if (m_baseValueOld != m_baseValue)
			{
				m_baseValueOld = m_baseValue;
				ZNet.instance.m_serverSyncedPlayerData["baseValue"] = m_baseValue.ToString();
				m_nview.GetZDO().Set(ZDOVars.s_baseValue, m_baseValue);
				RandEventSystem.SetRandomEventsNeedsRefresh();
			}
		}
	}

	public int GetComfortLevel()
	{
		if (m_nview == null)
		{
			return 0;
		}
		return m_comfortLevel;
	}

	public int GetBaseValue()
	{
		if (!m_nview.IsValid())
		{
			return 0;
		}
		if (m_nview.IsOwner())
		{
			return m_baseValue;
		}
		return m_nview.GetZDO().GetInt(ZDOVars.s_baseValue);
	}

	public bool IsSafeInHome()
	{
		return m_safeInHome;
	}

	private void UpdateBiome(float dt)
	{
		if (InIntro())
		{
			return;
		}
		if (m_biomeTimer == 0f)
		{
			Location location = Location.GetLocation(base.transform.position, checkDungeons: false);
			if ((object)location != null && !string.IsNullOrEmpty(location.m_discoverLabel))
			{
				AddKnownLocationName(location.m_discoverLabel);
			}
		}
		m_biomeTimer += dt;
		if (m_biomeTimer > 1f)
		{
			m_biomeTimer = 0f;
			Heightmap.Biome biome = Heightmap.FindBiome(base.transform.position);
			if (m_currentBiome != biome)
			{
				m_currentBiome = biome;
				AddKnownBiome(biome);
			}
		}
	}

	public Heightmap.Biome GetCurrentBiome()
	{
		return m_currentBiome;
	}

	public override void RaiseSkill(Skills.SkillType skill, float value = 1f)
	{
		if (skill != 0)
		{
			float multiplier = 1f;
			m_seman.ModifyRaiseSkill(skill, ref multiplier);
			value *= multiplier;
			m_skills.RaiseSkill(skill, value);
		}
	}

	private void UpdateStats(float dt)
	{
		if (InIntro() || IsTeleporting())
		{
			return;
		}
		m_timeSinceDeath += dt;
		UpdateModifiers();
		UpdateFood(dt, forceUpdate: false);
		bool flag = IsEncumbered();
		float maxStamina = GetMaxStamina();
		float num = 1f;
		if (IsBlocking())
		{
			num *= 0.8f;
		}
		if ((IsSwimming() && !IsOnGround()) || InAttack() || InDodge() || m_wallRunning || flag)
		{
			num = 0f;
		}
		float num2 = (m_staminaRegen + (1f - m_stamina / maxStamina) * m_staminaRegen * m_staminaRegenTimeMultiplier) * num;
		float staminaMultiplier = 1f;
		m_seman.ModifyStaminaRegen(ref staminaMultiplier);
		num2 *= staminaMultiplier;
		m_staminaRegenTimer -= dt;
		if (m_stamina < maxStamina && m_staminaRegenTimer <= 0f)
		{
			m_stamina = Mathf.Min(maxStamina, m_stamina + num2 * dt * Game.m_staminaRegenRate);
		}
		m_nview.GetZDO().Set(ZDOVars.s_stamina, m_stamina);
		float maxEitr = GetMaxEitr();
		float num3 = 1f;
		if (IsBlocking())
		{
			num3 *= 0.8f;
		}
		if (InAttack() || InDodge())
		{
			num3 = 0f;
		}
		float num4 = (m_eiterRegen + (1f - m_eitr / maxEitr) * m_eiterRegen) * num3;
		float eitrMultiplier = 1f;
		m_seman.ModifyEitrRegen(ref eitrMultiplier);
		eitrMultiplier += GetEquipmentEitrRegenModifier();
		num4 *= eitrMultiplier;
		m_eitrRegenTimer -= dt;
		if (m_eitr < maxEitr && m_eitrRegenTimer <= 0f)
		{
			m_eitr = Mathf.Min(maxEitr, m_eitr + num4 * dt);
		}
		m_nview.GetZDO().Set(ZDOVars.s_eitr, m_eitr);
		if (flag)
		{
			if (m_moveDir.magnitude > 0.1f)
			{
				UseStamina(m_encumberedStaminaDrain * dt);
			}
			m_seman.AddStatusEffect(SEMan.s_statusEffectEncumbered);
			ShowTutorial("encumbered");
		}
		else
		{
			m_seman.RemoveStatusEffect(SEMan.s_statusEffectEncumbered);
		}
		if (!HardDeath())
		{
			m_seman.AddStatusEffect(SEMan.s_statusEffectSoftDeath);
		}
		else
		{
			m_seman.RemoveStatusEffect(SEMan.s_statusEffectSoftDeath);
		}
		UpdateEnvStatusEffects(dt);
	}

	public float GetEquipmentEitrRegenModifier()
	{
		float num = 0f;
		if (m_chestItem != null)
		{
			num += m_chestItem.m_shared.m_eitrRegenModifier;
		}
		if (m_legItem != null)
		{
			num += m_legItem.m_shared.m_eitrRegenModifier;
		}
		if (m_helmetItem != null)
		{
			num += m_helmetItem.m_shared.m_eitrRegenModifier;
		}
		if (m_shoulderItem != null)
		{
			num += m_shoulderItem.m_shared.m_eitrRegenModifier;
		}
		if (m_leftItem != null)
		{
			num += m_leftItem.m_shared.m_eitrRegenModifier;
		}
		if (m_rightItem != null)
		{
			num += m_rightItem.m_shared.m_eitrRegenModifier;
		}
		if (m_utilityItem != null)
		{
			num += m_utilityItem.m_shared.m_eitrRegenModifier;
		}
		return num;
	}

	private void UpdateEnvStatusEffects(float dt)
	{
		m_nearFireTimer += dt;
		HitData.DamageModifiers damageModifiers = GetDamageModifiers();
		bool flag = m_nearFireTimer < 0.25f;
		bool flag2 = m_seman.HaveStatusEffect(SEMan.s_statusEffectBurning);
		bool flag3 = InShelter();
		HitData.DamageModifier modifier = damageModifiers.GetModifier(HitData.DamageType.Frost);
		bool flag4 = EnvMan.IsFreezing();
		bool num = EnvMan.IsCold();
		bool flag5 = EnvMan.IsWet();
		bool flag6 = IsSensed();
		bool flag7 = m_seman.HaveStatusEffect(SEMan.s_statusEffectWet);
		bool flag8 = IsSitting();
		bool flag9 = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.WarmCozyArea, 1f);
		bool flag10 = ShieldGenerator.IsInsideShield(base.transform.position);
		bool flag11 = flag4 && !flag && !flag3;
		bool flag12 = (num && !flag) || (flag4 && flag && !flag3) || (flag4 && !flag && flag3);
		if (modifier == HitData.DamageModifier.Resistant || modifier == HitData.DamageModifier.VeryResistant || flag9)
		{
			flag11 = false;
			flag12 = false;
		}
		if (flag5 && !m_underRoof && !flag10)
		{
			m_seman.AddStatusEffect(SEMan.s_statusEffectWet, resetTime: true);
		}
		if (flag3)
		{
			m_seman.AddStatusEffect(SEMan.s_statusEffectShelter);
		}
		else
		{
			m_seman.RemoveStatusEffect(SEMan.s_statusEffectShelter);
		}
		if (flag)
		{
			m_seman.AddStatusEffect(SEMan.s_statusEffectCampFire);
		}
		else
		{
			m_seman.RemoveStatusEffect(SEMan.s_statusEffectCampFire);
		}
		bool flag13 = !flag6 && (flag8 || flag3) && !flag12 && !flag11 && (!flag7 || flag9) && !flag2 && flag;
		if (flag13)
		{
			m_seman.AddStatusEffect(SEMan.s_statusEffectResting);
		}
		else
		{
			m_seman.RemoveStatusEffect(SEMan.s_statusEffectResting);
		}
		m_safeInHome = flag13 && flag3 && (float)GetBaseValue() >= 1f;
		if (flag11)
		{
			if (!m_seman.RemoveStatusEffect(SEMan.s_statusEffectCold, quiet: true))
			{
				m_seman.AddStatusEffect(SEMan.s_statusEffectFreezing);
			}
		}
		else if (flag12)
		{
			if (!m_seman.RemoveStatusEffect(SEMan.s_statusEffectFreezing, quiet: true) && (bool)m_seman.AddStatusEffect(SEMan.s_statusEffectCold))
			{
				ShowTutorial("cold");
			}
		}
		else
		{
			m_seman.RemoveStatusEffect(SEMan.s_statusEffectCold);
			m_seman.RemoveStatusEffect(SEMan.s_statusEffectFreezing);
		}
	}

	private bool CanEat(ItemDrop.ItemData item, bool showMessages)
	{
		foreach (Food food in m_foods)
		{
			if (food.m_item.m_shared.m_name == item.m_shared.m_name)
			{
				if (food.CanEatAgain())
				{
					return true;
				}
				Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_nomore", item.m_shared.m_name));
				return false;
			}
		}
		foreach (Food food2 in m_foods)
		{
			if (food2.CanEatAgain())
			{
				return true;
			}
		}
		if (m_foods.Count >= 3)
		{
			Message(MessageHud.MessageType.Center, "$msg_isfull");
			return false;
		}
		return true;
	}

	private Food GetMostDepletedFood()
	{
		Food food = null;
		foreach (Food food2 in m_foods)
		{
			if (food2.CanEatAgain() && (food == null || food2.m_time < food.m_time))
			{
				food = food2;
			}
		}
		return food;
	}

	public void ClearFood()
	{
		m_foods.Clear();
	}

	public bool RemoveOneFood()
	{
		if (m_foods.Count == 0)
		{
			return false;
		}
		m_foods.RemoveAt(UnityEngine.Random.Range(0, m_foods.Count));
		return true;
	}

	private bool EatFood(ItemDrop.ItemData item)
	{
		if (!CanEat(item, showMessages: false))
		{
			return false;
		}
		string text = "";
		if (item.m_shared.m_food > 0f)
		{
			text = text + " +" + item.m_shared.m_food + " $item_food_health ";
		}
		if (item.m_shared.m_foodStamina > 0f)
		{
			text = text + " +" + item.m_shared.m_foodStamina + " $item_food_stamina ";
		}
		if (item.m_shared.m_foodEitr > 0f)
		{
			text = text + " +" + item.m_shared.m_foodEitr + " $item_food_eitr ";
		}
		Message(MessageHud.MessageType.Center, text);
		foreach (Food food2 in m_foods)
		{
			if (food2.m_item.m_shared.m_name == item.m_shared.m_name)
			{
				if (food2.CanEatAgain())
				{
					food2.m_time = item.m_shared.m_foodBurnTime;
					food2.m_health = item.m_shared.m_food;
					food2.m_stamina = item.m_shared.m_foodStamina;
					food2.m_eitr = item.m_shared.m_foodEitr;
					UpdateFood(0f, forceUpdate: true);
					return true;
				}
				return false;
			}
		}
		if (m_foods.Count < 3)
		{
			Food food = new Food();
			food.m_name = item.m_dropPrefab.name;
			food.m_item = item;
			food.m_time = item.m_shared.m_foodBurnTime;
			food.m_health = item.m_shared.m_food;
			food.m_stamina = item.m_shared.m_foodStamina;
			food.m_eitr = item.m_shared.m_foodEitr;
			m_foods.Add(food);
			UpdateFood(0f, forceUpdate: true);
			return true;
		}
		Food mostDepletedFood = GetMostDepletedFood();
		if (mostDepletedFood != null)
		{
			mostDepletedFood.m_name = item.m_dropPrefab.name;
			mostDepletedFood.m_item = item;
			mostDepletedFood.m_time = item.m_shared.m_foodBurnTime;
			mostDepletedFood.m_health = item.m_shared.m_food;
			mostDepletedFood.m_stamina = item.m_shared.m_foodStamina;
			UpdateFood(0f, forceUpdate: true);
			return true;
		}
		Game.instance.IncrementPlayerStat(PlayerStatType.FoodEaten);
		return false;
	}

	private void UpdateFood(float dt, bool forceUpdate)
	{
		m_foodUpdateTimer += dt;
		if (m_foodUpdateTimer >= 1f || forceUpdate)
		{
			m_foodUpdateTimer -= 1f;
			foreach (Food food in m_foods)
			{
				food.m_time -= 1f;
				float f = Mathf.Clamp01(food.m_time / food.m_item.m_shared.m_foodBurnTime);
				f = Mathf.Pow(f, 0.3f);
				food.m_health = food.m_item.m_shared.m_food * f;
				food.m_stamina = food.m_item.m_shared.m_foodStamina * f;
				food.m_eitr = food.m_item.m_shared.m_foodEitr * f;
				if (food.m_time <= 0f)
				{
					Message(MessageHud.MessageType.Center, "$msg_food_done");
					m_foods.Remove(food);
					break;
				}
			}
			GetTotalFoodValue(out var hp, out var stamina, out var eitr);
			SetMaxHealth(hp, flashBar: true);
			SetMaxStamina(stamina, flashBar: true);
			SetMaxEitr(eitr, flashBar: true);
			if (eitr > 0f)
			{
				ShowTutorial("eitr");
			}
		}
		if (forceUpdate)
		{
			return;
		}
		m_foodRegenTimer += dt;
		if (!(m_foodRegenTimer >= 10f))
		{
			return;
		}
		m_foodRegenTimer = 0f;
		float num = 0f;
		foreach (Food food2 in m_foods)
		{
			num += food2.m_item.m_shared.m_foodRegen;
		}
		if (num > 0f)
		{
			float regenMultiplier = 1f;
			m_seman.ModifyHealthRegen(ref regenMultiplier);
			num *= regenMultiplier;
			Heal(num);
		}
	}

	private void GetTotalFoodValue(out float hp, out float stamina, out float eitr)
	{
		hp = m_baseHP;
		stamina = m_baseStamina;
		eitr = 0f;
		foreach (Food food in m_foods)
		{
			hp += food.m_health;
			stamina += food.m_stamina;
			eitr += food.m_eitr;
		}
	}

	public float GetBaseFoodHP()
	{
		return m_baseHP;
	}

	public List<Food> GetFoods()
	{
		return m_foods;
	}

	public void OnSpawned()
	{
		m_spawnEffects.Create(base.transform.position, Quaternion.identity);
		if (m_firstSpawn)
		{
			if (m_valkyrie != null)
			{
				UnityEngine.Object.Instantiate(m_valkyrie, base.transform.position, Quaternion.identity);
			}
			m_firstSpawn = false;
		}
	}

	protected override bool CheckRun(Vector3 moveDir, float dt)
	{
		if (!base.CheckRun(moveDir, dt))
		{
			return false;
		}
		bool flag = HaveStamina();
		float skillFactor = m_skills.GetSkillFactor(Skills.SkillType.Run);
		float num = Mathf.Lerp(1f, 0.5f, skillFactor);
		float num2 = m_runStaminaDrain * num;
		num2 -= num2 * GetEquipmentMovementModifier();
		num2 += num2 * GetEquipmentRunStaminaModifier();
		m_seman.ModifyRunStaminaDrain(num2, ref num2);
		UseStamina(dt * num2 * Game.m_moveStaminaRate);
		if (HaveStamina())
		{
			m_runSkillImproveTimer += dt;
			if (m_runSkillImproveTimer > 1f)
			{
				m_runSkillImproveTimer = 0f;
				RaiseSkill(Skills.SkillType.Run);
			}
			ClearActionQueue();
			return true;
		}
		if (flag)
		{
			Hud.instance.StaminaBarEmptyFlash();
		}
		return false;
	}

	private void UpdateModifiers()
	{
		if (s_equipmentModifierSourceFields == null)
		{
			return;
		}
		for (int i = 0; i < m_equipmentModifierValues.Length; i++)
		{
			float num = 0f;
			if (m_rightItem != null)
			{
				num += (float)s_equipmentModifierSourceFields[i].GetValue(m_rightItem.m_shared);
			}
			if (m_leftItem != null)
			{
				num += (float)s_equipmentModifierSourceFields[i].GetValue(m_leftItem.m_shared);
			}
			if (m_chestItem != null)
			{
				num += (float)s_equipmentModifierSourceFields[i].GetValue(m_chestItem.m_shared);
			}
			if (m_legItem != null)
			{
				num += (float)s_equipmentModifierSourceFields[i].GetValue(m_legItem.m_shared);
			}
			if (m_helmetItem != null)
			{
				num += (float)s_equipmentModifierSourceFields[i].GetValue(m_helmetItem.m_shared);
			}
			if (m_shoulderItem != null)
			{
				num += (float)s_equipmentModifierSourceFields[i].GetValue(m_shoulderItem.m_shared);
			}
			if (m_utilityItem != null)
			{
				num += (float)s_equipmentModifierSourceFields[i].GetValue(m_utilityItem.m_shared);
			}
			m_equipmentModifierValues[i] = num;
		}
	}

	public void AppendEquipmentModifierTooltips(ItemDrop.ItemData item, StringBuilder sb)
	{
		for (int i = 0; i < m_equipmentModifierValues.Length; i++)
		{
			if (s_equipmentModifierSourceFields[i].GetValue(item.m_shared) is float num && num != 0f)
			{
				sb.AppendFormat("\n" + s_equipmentModifierTooltips[i] + ": <color=orange>" + (num * 100f).ToString("+0;-0") + "%</color> ($item_total:<color=yellow>" + (GetEquipmentModifierPlusSE(i) * 100f).ToString("+0;-0") + "%</color>)");
			}
		}
	}

	public void OnSkillLevelup(Skills.SkillType skill, float level)
	{
		m_skillLevelupEffects.Create(m_head.position, m_head.rotation, m_head);
	}

	protected override void OnJump()
	{
		ClearActionQueue();
		float staminaUse = m_jumpStaminaUsage - m_jumpStaminaUsage * GetEquipmentMovementModifier() + m_jumpStaminaUsage * GetEquipmentJumpStaminaModifier();
		m_seman.ModifyJumpStaminaUsage(staminaUse, ref staminaUse);
		UseStamina(staminaUse * Game.m_moveStaminaRate);
		Game.instance.IncrementPlayerStat(PlayerStatType.Jumps);
	}

	protected override void OnSwimming(Vector3 targetVel, float dt)
	{
		base.OnSwimming(targetVel, dt);
		if (targetVel.magnitude > 0.1f)
		{
			float skillFactor = m_skills.GetSkillFactor(Skills.SkillType.Swim);
			float num = Mathf.Lerp(m_swimStaminaDrainMinSkill, m_swimStaminaDrainMaxSkill, skillFactor);
			num += num * GetEquipmentSwimStaminaModifier();
			m_seman.ModifySwimStaminaUsage(num, ref num);
			UseStamina(dt * num * Game.m_moveStaminaRate);
			m_swimSkillImproveTimer += dt;
			if (m_swimSkillImproveTimer > 1f)
			{
				m_swimSkillImproveTimer = 0f;
				RaiseSkill(Skills.SkillType.Swim);
			}
		}
		if (!HaveStamina())
		{
			m_drownDamageTimer += dt;
			if (m_drownDamageTimer > 1f)
			{
				m_drownDamageTimer = 0f;
				float damage = Mathf.Ceil(GetMaxHealth() / 20f);
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = damage;
				hitData.m_point = GetCenterPoint();
				hitData.m_dir = Vector3.down;
				hitData.m_pushForce = 10f;
				hitData.m_hitType = HitData.HitType.Drowning;
				Damage(hitData);
				Vector3 position = base.transform.position;
				position.y = GetLiquidLevel();
				m_drownEffects.Create(position, base.transform.rotation);
			}
		}
	}

	protected override bool TakeInput()
	{
		bool result = (!Chat.instance || !Chat.instance.HasFocus()) && !Console.IsVisible() && !TextInput.IsVisible() && !StoreGui.IsVisible() && !InventoryGui.IsVisible() && !Menu.IsVisible() && (!TextViewer.instance || !TextViewer.instance.IsVisible()) && !Minimap.IsOpen() && !GameCamera.InFreeFly() && !PlayerCustomizaton.IsBarberGuiVisible();
		if (IsDead() || InCutscene() || IsTeleporting())
		{
			result = false;
		}
		return result;
	}

	public void UseHotbarItem(int index)
	{
		ItemDrop.ItemData itemAt = m_inventory.GetItemAt(index - 1, 0);
		if (itemAt != null)
		{
			UseItem(null, itemAt, fromInventoryGui: false);
		}
	}

	public bool RequiredCraftingStation(Recipe recipe, int qualityLevel, bool checkLevel)
	{
		CraftingStation requiredStation = recipe.GetRequiredStation(qualityLevel);
		if (requiredStation != null)
		{
			if (m_currentStation == null)
			{
				return false;
			}
			if (requiredStation.m_name != m_currentStation.m_name)
			{
				return false;
			}
			if (checkLevel)
			{
				int requiredStationLevel = recipe.GetRequiredStationLevel(qualityLevel);
				if (m_currentStation.GetLevel() < requiredStationLevel)
				{
					return false;
				}
			}
		}
		else if (m_currentStation != null && !m_currentStation.m_showBasicRecipies)
		{
			return false;
		}
		return true;
	}

	public bool HaveRequirements(Recipe recipe, bool discover, int qualityLevel)
	{
		if (discover)
		{
			if ((bool)recipe.m_craftingStation && !KnowStationLevel(recipe.m_craftingStation.m_name, recipe.m_minStationLevel))
			{
				return false;
			}
		}
		else if (!RequiredCraftingStation(recipe, qualityLevel, checkLevel: true))
		{
			return false;
		}
		if (recipe.m_item.m_itemData.m_shared.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc))
		{
			return false;
		}
		if (!HaveRequirementItems(recipe, discover, qualityLevel))
		{
			return false;
		}
		return true;
	}

	private bool HaveRequirementItems(Recipe piece, bool discover, int qualityLevel)
	{
		Piece.Requirement[] resources = piece.m_resources;
		foreach (Piece.Requirement requirement in resources)
		{
			if (!requirement.m_resItem)
			{
				continue;
			}
			if (discover)
			{
				if (requirement.m_amount <= 0)
				{
					continue;
				}
				if (piece.m_requireOnlyOneIngredient)
				{
					if (m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
					{
						return true;
					}
				}
				else if (!m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
				{
					return false;
				}
				continue;
			}
			int amount = requirement.GetAmount(qualityLevel);
			int num = m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
			if (piece.m_requireOnlyOneIngredient)
			{
				if (num >= amount)
				{
					return true;
				}
			}
			else if (num < amount)
			{
				return false;
			}
		}
		if (piece.m_requireOnlyOneIngredient)
		{
			return false;
		}
		return true;
	}

	public ItemDrop.ItemData GetFirstRequiredItem(Inventory inventory, Recipe recipe, int qualityLevel, out int amount, out int extraAmount)
	{
		Piece.Requirement[] resources = recipe.m_resources;
		foreach (Piece.Requirement requirement in resources)
		{
			if (!requirement.m_resItem)
			{
				continue;
			}
			int amount2 = requirement.GetAmount(qualityLevel);
			for (int j = 0; j <= requirement.m_resItem.m_itemData.m_shared.m_maxQuality; j++)
			{
				if (m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name, j) >= amount2)
				{
					amount = amount2;
					extraAmount = requirement.m_extraAmountOnlyOneIngredient;
					return inventory.GetItem(requirement.m_resItem.m_itemData.m_shared.m_name, j);
				}
			}
		}
		amount = 0;
		extraAmount = 0;
		return null;
	}

	public bool HaveRequirements(Piece piece, RequirementMode mode)
	{
		if ((bool)piece.m_craftingStation)
		{
			if (mode == RequirementMode.IsKnown || mode == RequirementMode.CanAlmostBuild)
			{
				if (!m_knownStations.ContainsKey(piece.m_craftingStation.m_name))
				{
					return false;
				}
			}
			else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
			{
				return false;
			}
		}
		if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
		{
			return false;
		}
		if (mode != RequirementMode.IsKnown && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoBuildCost))
		{
			return true;
		}
		Piece.Requirement[] resources = piece.m_resources;
		foreach (Piece.Requirement requirement in resources)
		{
			if (!requirement.m_resItem || requirement.m_amount <= 0)
			{
				continue;
			}
			switch (mode)
			{
			case RequirementMode.IsKnown:
				if (!m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name))
				{
					return false;
				}
				break;
			case RequirementMode.CanAlmostBuild:
				if (!m_inventory.HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name))
				{
					return false;
				}
				break;
			case RequirementMode.CanBuild:
				if (m_inventory.CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) < requirement.m_amount)
				{
					return false;
				}
				break;
			}
		}
		return true;
	}

	public void ConsumeResources(Piece.Requirement[] requirements, int qualityLevel, int itemQuality = -1)
	{
		foreach (Piece.Requirement requirement in requirements)
		{
			if ((bool)requirement.m_resItem)
			{
				int amount = requirement.GetAmount(qualityLevel);
				if (amount > 0)
				{
					m_inventory.RemoveItem(requirement.m_resItem.m_itemData.m_shared.m_name, amount, itemQuality);
				}
			}
		}
	}

	private void UpdateHover()
	{
		if (InPlaceMode() || IsDead() || m_doodadController != null)
		{
			m_hovering = null;
			m_hoveringCreature = null;
		}
		else
		{
			FindHoverObject(out m_hovering, out m_hoveringCreature);
		}
	}

	public bool IsMaterialKnown(string sharedName)
	{
		return m_knownMaterial.Contains(sharedName);
	}

	private bool CheckCanRemovePiece(Piece piece)
	{
		if (!m_noPlacementCost && piece.m_craftingStation != null && !CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name, base.transform.position) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoWorkbench))
		{
			Message(MessageHud.MessageType.Center, "$msg_missingstation");
			return false;
		}
		return true;
	}

	private bool CopyPiece()
	{
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, m_removeRayMask) && Vector3.Distance(hitInfo.point, m_eye.position) < m_maxPlaceDistance)
		{
			Piece piece = hitInfo.collider.GetComponentInParent<Piece>();
			if (piece == null && (bool)hitInfo.collider.GetComponent<Heightmap>())
			{
				piece = TerrainModifier.FindClosestModifierPieceInRange(hitInfo.point, 2.5f);
			}
			if ((bool)piece)
			{
				if (SetSelectedPiece(piece))
				{
					m_placeRotation = (int)Math.Round(piece.transform.rotation.eulerAngles.y / m_placeRotationDegrees);
					return true;
				}
				Message(MessageHud.MessageType.Center, "$msg_missingrequirement");
				return false;
			}
		}
		return false;
	}

	private bool RemovePiece()
	{
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, m_removeRayMask) && Vector3.Distance(hitInfo.point, m_eye.position) < m_maxPlaceDistance)
		{
			Piece piece = hitInfo.collider.GetComponentInParent<Piece>();
			if (piece == null && (bool)hitInfo.collider.GetComponent<Heightmap>())
			{
				piece = TerrainModifier.FindClosestModifierPieceInRange(hitInfo.point, 2.5f);
			}
			if ((bool)piece)
			{
				if (!piece.m_canBeRemoved)
				{
					return false;
				}
				if (Location.IsInsideNoBuildLocation(piece.transform.position))
				{
					Message(MessageHud.MessageType.Center, "$msg_nobuildzone");
					return false;
				}
				if (!PrivateArea.CheckAccess(piece.transform.position))
				{
					Message(MessageHud.MessageType.Center, "$msg_privatezone");
					return false;
				}
				if (!CheckCanRemovePiece(piece))
				{
					return false;
				}
				ZNetView component = piece.GetComponent<ZNetView>();
				if (component == null)
				{
					return false;
				}
				if (!piece.CanBeRemoved())
				{
					Message(MessageHud.MessageType.Center, "$msg_cantremovenow");
					return false;
				}
				WearNTear component2 = piece.GetComponent<WearNTear>();
				if ((bool)component2)
				{
					component2.Remove();
				}
				else
				{
					ZLog.Log("Removing non WNT object with hammer " + piece.name);
					component.ClaimOwnership();
					piece.DropResources();
					piece.m_placeEffect.Create(piece.transform.position, piece.transform.rotation, piece.gameObject.transform);
					m_removeEffects.Create(piece.transform.position, Quaternion.identity);
					ZNetScene.instance.Destroy(piece.gameObject);
				}
				ItemDrop.ItemData rightItem = GetRightItem();
				if (rightItem != null)
				{
					FaceLookDirection();
					m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
				}
				return true;
			}
		}
		return false;
	}

	public void FaceLookDirection()
	{
		base.transform.rotation = GetLookYaw();
		Physics.SyncTransforms();
	}

	private bool PlacePiece(Piece piece)
	{
		UpdatePlacementGhost(flashGuardStone: true);
		Vector3 position = m_placementGhost.transform.position;
		Quaternion rotation = m_placementGhost.transform.rotation;
		GameObject gameObject = piece.gameObject;
		switch (m_placementStatus)
		{
		case PlacementStatus.NoBuildZone:
			Message(MessageHud.MessageType.Center, "$msg_nobuildzone");
			return false;
		case PlacementStatus.BlockedbyPlayer:
			Message(MessageHud.MessageType.Center, "$msg_blocked");
			return false;
		case PlacementStatus.PrivateZone:
			Message(MessageHud.MessageType.Center, "$msg_privatezone");
			return false;
		case PlacementStatus.MoreSpace:
			Message(MessageHud.MessageType.Center, "$msg_needspace");
			return false;
		case PlacementStatus.NoTeleportArea:
			Message(MessageHud.MessageType.Center, "$msg_noteleportarea");
			return false;
		case PlacementStatus.Invalid:
		case PlacementStatus.NoRayHits:
			Message(MessageHud.MessageType.Center, "$msg_invalidplacement");
			return false;
		case PlacementStatus.ExtensionMissingStation:
			Message(MessageHud.MessageType.Center, "$msg_extensionmissingstation");
			return false;
		case PlacementStatus.WrongBiome:
			Message(MessageHud.MessageType.Center, "$msg_wrongbiome");
			return false;
		case PlacementStatus.NeedCultivated:
			Message(MessageHud.MessageType.Center, "$msg_needcultivated");
			return false;
		case PlacementStatus.NeedDirt:
			Message(MessageHud.MessageType.Center, "$msg_needdirt");
			return false;
		case PlacementStatus.NotInDungeon:
			Message(MessageHud.MessageType.Center, "$msg_notindungeon");
			return false;
		default:
		{
			TerrainModifier.SetTriggerOnPlaced(trigger: true);
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, position, rotation);
			TerrainModifier.SetTriggerOnPlaced(trigger: false);
			CraftingStation componentInChildren = gameObject2.GetComponentInChildren<CraftingStation>();
			if ((bool)componentInChildren)
			{
				AddKnownStation(componentInChildren);
			}
			Piece component = gameObject2.GetComponent<Piece>();
			if ((bool)component)
			{
				component.SetCreator(GetPlayerID());
			}
			PrivateArea component2 = gameObject2.GetComponent<PrivateArea>();
			if ((bool)component2)
			{
				component2.Setup(Game.instance.GetPlayerProfile().GetName());
			}
			WearNTear component3 = gameObject2.GetComponent<WearNTear>();
			if ((bool)component3)
			{
				component3.OnPlaced();
			}
			ItemDrop.ItemData rightItem = GetRightItem();
			if (rightItem != null)
			{
				FaceLookDirection();
				m_zanim.SetTrigger(rightItem.m_shared.m_attack.m_attackAnimation);
			}
			if (piece.m_randomInitBuildRotation)
			{
				m_placeRotation = UnityEngine.Random.Range(0, 16);
			}
			piece.m_placeEffect.Create(position, rotation, gameObject2.transform);
			AddNoise(50f);
			Game.instance.IncrementPlayerStat(PlayerStatType.Builds);
			ZLog.Log("Placed " + gameObject.name);
			Gogan.LogEvent("Game", "PlacedPiece", gameObject.name, 0L);
			return true;
		}
		}
	}

	public override bool IsPlayer()
	{
		return true;
	}

	public void GetBuildSelection(out Piece go, out Vector2Int id, out int total, out Piece.PieceCategory category, out bool useCategory)
	{
		category = m_buildPieces.m_selectedCategory;
		useCategory = m_buildPieces.m_useCategories;
		if (m_buildPieces.GetAvailablePiecesInSelectedCategory() == 0)
		{
			go = null;
			id = Vector2Int.zero;
			total = 0;
		}
		else
		{
			GameObject selectedPrefab = m_buildPieces.GetSelectedPrefab();
			go = (selectedPrefab ? selectedPrefab.GetComponent<Piece>() : null);
			id = m_buildPieces.GetSelectedIndex();
			total = m_buildPieces.GetAvailablePiecesInSelectedCategory();
		}
	}

	public List<Piece> GetBuildPieces()
	{
		if (!(m_buildPieces != null))
		{
			return null;
		}
		return m_buildPieces.GetPiecesInSelectedCategory();
	}

	public int GetAvailableBuildPiecesInCategory(Piece.PieceCategory cat)
	{
		if (!(m_buildPieces != null))
		{
			return 0;
		}
		return m_buildPieces.GetAvailablePiecesInCategory(cat);
	}

	private void RPC_OnDeath(long sender)
	{
		m_visual.SetActive(value: false);
	}

	private void CreateDeathEffects()
	{
		GameObject[] array = m_deathEffects.Create(base.transform.position, base.transform.rotation, base.transform);
		for (int i = 0; i < array.Length; i++)
		{
			Ragdoll component = array[i].GetComponent<Ragdoll>();
			if ((bool)component)
			{
				Vector3 velocity = m_body.velocity;
				if (m_pushForce.magnitude * 0.5f > velocity.magnitude)
				{
					velocity = m_pushForce * 0.5f;
				}
				component.Setup(velocity, 0f, 0f, 0f, null);
				OnRagdollCreated(component);
				m_ragdoll = component;
			}
		}
	}

	public void UnequipDeathDropItems()
	{
		if (m_rightItem != null)
		{
			UnequipItem(m_rightItem, triggerEquipEffects: false);
		}
		if (m_leftItem != null)
		{
			UnequipItem(m_leftItem, triggerEquipEffects: false);
		}
		if (m_ammoItem != null)
		{
			UnequipItem(m_ammoItem, triggerEquipEffects: false);
		}
		if (m_utilityItem != null)
		{
			UnequipItem(m_utilityItem, triggerEquipEffects: false);
		}
	}

	public void CreateTombStone()
	{
		if (m_inventory.NrOfItems() != 0)
		{
			if (!ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathKeepEquip) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteUnequipped))
			{
				UnequipAllItems();
			}
			if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteItems) || ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteUnequipped))
			{
				m_inventory.RemoveUnequipped();
			}
			if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathDeleteUnequipped) && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathKeepEquip))
			{
				UnequipAllItems();
			}
			GameObject obj = UnityEngine.Object.Instantiate(m_tombstone, GetCenterPoint(), base.transform.rotation);
			obj.GetComponent<Container>().GetInventory().MoveInventoryToGrave(m_inventory);
			TombStone component = obj.GetComponent<TombStone>();
			PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
			component.Setup(playerProfile.GetName(), playerProfile.GetPlayerID());
		}
	}

	private bool HardDeath()
	{
		return m_timeSinceDeath > m_hardDeathCooldown;
	}

	public void ClearHardDeath()
	{
		m_timeSinceDeath = m_hardDeathCooldown + 1f;
	}

	protected override void OnDeath()
	{
		if (!m_nview.IsOwner())
		{
			Debug.Log("OnDeath call but not the owner");
			return;
		}
		bool flag = HardDeath();
		m_nview.GetZDO().Set(ZDOVars.s_dead, value: true);
		m_nview.InvokeRPC(ZNetView.Everybody, "OnDeath");
		Game.instance.IncrementPlayerStat(PlayerStatType.Deaths);
		switch (m_lastHit.m_hitType)
		{
		case HitData.HitType.Undefined:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByUndefined);
			break;
		case HitData.HitType.EnemyHit:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEnemyHit);
			break;
		case HitData.HitType.PlayerHit:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPlayerHit);
			break;
		case HitData.HitType.Fall:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFall);
			break;
		case HitData.HitType.Drowning:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByDrowning);
			break;
		case HitData.HitType.Burning:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBurning);
			break;
		case HitData.HitType.Freezing:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByFreezing);
			break;
		case HitData.HitType.Poisoned:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByPoisoned);
			break;
		case HitData.HitType.Water:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByWater);
			break;
		case HitData.HitType.Smoke:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySmoke);
			break;
		case HitData.HitType.EdgeOfWorld:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByEdgeOfWorld);
			break;
		case HitData.HitType.Impact:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByImpact);
			break;
		case HitData.HitType.Cart:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByCart);
			break;
		case HitData.HitType.Tree:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTree);
			break;
		case HitData.HitType.Self:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathBySelf);
			break;
		case HitData.HitType.Structural:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStructural);
			break;
		case HitData.HitType.Turret:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByTurret);
			break;
		case HitData.HitType.Boat:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByBoat);
			break;
		case HitData.HitType.Stalagtite:
			Game.instance.IncrementPlayerStat(PlayerStatType.DeathByStalagtite);
			break;
		default:
			ZLog.LogWarning("Not implemented death type " + m_lastHit.m_hitType);
			break;
		}
		Game.instance.GetPlayerProfile().SetDeathPoint(base.transform.position);
		CreateDeathEffects();
		CreateTombStone();
		m_foods.Clear();
		if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.DeathSkillsReset))
		{
			m_skills.Clear();
		}
		else if (flag)
		{
			m_skills.OnDeath();
		}
		m_seman.RemoveAllStatusEffects();
		Game.instance.RequestRespawn(10f, afterDeath: true);
		m_timeSinceDeath = 0f;
		if (!flag)
		{
			Message(MessageHud.MessageType.TopLeft, "$msg_softdeath");
		}
		Message(MessageHud.MessageType.Center, "$msg_youdied");
		ShowTutorial("death");
		Minimap.instance.AddPin(base.transform.position, Minimap.PinType.Death, $"$hud_mapday {EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds())}", save: true, isChecked: false, 0L);
		if (m_onDeath != null)
		{
			m_onDeath();
		}
		string eventLabel = "biome:" + GetCurrentBiome();
		Gogan.LogEvent("Game", "Death", eventLabel, 0L);
	}

	public void OnRespawn()
	{
		m_nview.GetZDO().Set(ZDOVars.s_dead, value: false);
		SetHealth(GetMaxHealth());
	}

	private void SetupPlacementGhost()
	{
		if ((bool)m_placementGhost)
		{
			UnityEngine.Object.Destroy(m_placementGhost);
			m_placementGhost = null;
		}
		if (m_buildPieces == null || IsDead())
		{
			return;
		}
		GameObject selectedPrefab = m_buildPieces.GetSelectedPrefab();
		if (selectedPrefab == null || selectedPrefab.GetComponent<Piece>().m_repairPiece)
		{
			return;
		}
		bool flag = false;
		TerrainModifier componentInChildren = selectedPrefab.GetComponentInChildren<TerrainModifier>();
		if ((bool)componentInChildren)
		{
			flag = componentInChildren.enabled;
			componentInChildren.enabled = false;
		}
		TerrainOp.m_forceDisableTerrainOps = true;
		ZNetView.m_forceDisableInit = true;
		_ = m_placementGhost;
		m_placementGhost = UnityEngine.Object.Instantiate(selectedPrefab);
		Piece component = m_placementGhost.GetComponent<Piece>();
		if ((object)component != null && component.m_randomInitBuildRotation)
		{
			m_placeRotation = UnityEngine.Random.Range(0, 16);
		}
		ZNetView.m_forceDisableInit = false;
		TerrainOp.m_forceDisableTerrainOps = false;
		m_placementGhost.name = selectedPrefab.name;
		if (m_placementGhostLast != m_placementGhost.name)
		{
			m_manualSnapPoint = -1;
		}
		m_placementGhostLast = m_placementGhost.name;
		if ((bool)componentInChildren)
		{
			componentInChildren.enabled = flag;
		}
		Joint[] componentsInChildren = m_placementGhost.GetComponentsInChildren<Joint>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren[i]);
		}
		Rigidbody[] componentsInChildren2 = m_placementGhost.GetComponentsInChildren<Rigidbody>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren2[i]);
		}
		ParticleSystemForceField[] componentsInChildren3 = m_placementGhost.GetComponentsInChildren<ParticleSystemForceField>();
		for (int i = 0; i < componentsInChildren3.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren3[i]);
		}
		Demister[] componentsInChildren4 = m_placementGhost.GetComponentsInChildren<Demister>();
		for (int i = 0; i < componentsInChildren4.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren4[i]);
		}
		Collider[] componentsInChildren5 = m_placementGhost.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren5)
		{
			if (((1 << collider.gameObject.layer) & m_placeRayMask) == 0)
			{
				ZLog.Log("Disabling " + collider.gameObject.name + "  " + LayerMask.LayerToName(collider.gameObject.layer));
				collider.enabled = false;
			}
		}
		Transform[] componentsInChildren6 = m_placementGhost.GetComponentsInChildren<Transform>();
		int layer = LayerMask.NameToLayer("ghost");
		Transform[] array = componentsInChildren6;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].gameObject.layer = layer;
		}
		TerrainModifier[] componentsInChildren7 = m_placementGhost.GetComponentsInChildren<TerrainModifier>();
		for (int i = 0; i < componentsInChildren7.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren7[i]);
		}
		GuidePoint[] componentsInChildren8 = m_placementGhost.GetComponentsInChildren<GuidePoint>();
		for (int i = 0; i < componentsInChildren8.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren8[i]);
		}
		Light[] componentsInChildren9 = m_placementGhost.GetComponentsInChildren<Light>();
		for (int i = 0; i < componentsInChildren9.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren9[i]);
		}
		AudioSource[] componentsInChildren10 = m_placementGhost.GetComponentsInChildren<AudioSource>();
		for (int i = 0; i < componentsInChildren10.Length; i++)
		{
			componentsInChildren10[i].enabled = false;
		}
		ZSFX[] componentsInChildren11 = m_placementGhost.GetComponentsInChildren<ZSFX>();
		for (int i = 0; i < componentsInChildren11.Length; i++)
		{
			componentsInChildren11[i].enabled = false;
		}
		WispSpawner componentInChildren2 = m_placementGhost.GetComponentInChildren<WispSpawner>();
		if ((bool)componentInChildren2)
		{
			UnityEngine.Object.Destroy(componentInChildren2);
		}
		Windmill componentInChildren3 = m_placementGhost.GetComponentInChildren<Windmill>();
		if ((bool)componentInChildren3)
		{
			componentInChildren3.enabled = false;
		}
		ParticleSystem[] componentsInChildren12 = m_placementGhost.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren12.Length; i++)
		{
			componentsInChildren12[i].gameObject.SetActive(value: false);
		}
		Transform transform = m_placementGhost.transform.Find("_GhostOnly");
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: true);
		}
		m_placementGhost.transform.position = base.transform.position;
		m_placementGhost.transform.localScale = selectedPrefab.transform.localScale;
		m_ghostRippleDistance.Clear();
		CleanupGhostMaterials<MeshRenderer>(m_placementGhost);
		CleanupGhostMaterials<SkinnedMeshRenderer>(m_placementGhost);
	}

	public static bool IsPlacementGhost(GameObject obj)
	{
		if ((bool)m_localPlayer)
		{
			return (object)obj == m_localPlayer.m_placementGhost;
		}
		return false;
	}

	private void CleanupGhostMaterials<T>(GameObject ghost) where T : Renderer
	{
		T[] componentsInChildren = m_placementGhost.GetComponentsInChildren<T>();
		foreach (T val in componentsInChildren)
		{
			if (val.sharedMaterial == null)
			{
				continue;
			}
			Material[] sharedMaterials = val.sharedMaterials;
			for (int j = 0; j < sharedMaterials.Length; j++)
			{
				Material material = new Material(sharedMaterials[j]);
				if (material.HasProperty("_RippleDistance"))
				{
					m_ghostRippleDistance[material] = material.GetFloat("_RippleDistance");
				}
				material.SetFloat("_ValueNoise", 0f);
				material.SetFloat("_TriplanarLocalPos", 1f);
				sharedMaterials[j] = material;
			}
			val.sharedMaterials = sharedMaterials;
			val.shadowCastingMode = ShadowCastingMode.Off;
		}
	}

	private void SetPlacementGhostValid(bool valid)
	{
		m_placementGhost.GetComponent<Piece>().SetInvalidPlacementHeightlight(!valid);
	}

	protected override void SetPlaceMode(PieceTable buildPieces)
	{
		base.SetPlaceMode(buildPieces);
		m_buildPieces = buildPieces;
		UpdateAvailablePiecesList();
	}

	public void SetBuildCategory(int index)
	{
		if (m_buildPieces != null)
		{
			m_buildPieces.SetCategory(index);
			UpdateAvailablePiecesList();
		}
	}

	public override bool InPlaceMode()
	{
		return m_buildPieces != null;
	}

	public bool InRepairMode()
	{
		if (InPlaceMode())
		{
			Piece selectedPiece = m_buildPieces.GetSelectedPiece();
			if ((object)selectedPiece != null)
			{
				return selectedPiece.m_repairPiece;
			}
		}
		return false;
	}

	public PlacementStatus GetPlacementStatus()
	{
		return m_placementStatus;
	}

	public bool CanRotatePiece()
	{
		if (InPlaceMode())
		{
			Piece selectedPiece = m_buildPieces.GetSelectedPiece();
			if ((object)selectedPiece != null)
			{
				return selectedPiece.m_canRotate;
			}
		}
		return false;
	}

	private void Repair(ItemDrop.ItemData toolItem, Piece repairPiece)
	{
		if (!InPlaceMode())
		{
			return;
		}
		Piece hoveringPiece = GetHoveringPiece();
		if (!hoveringPiece || !CheckCanRemovePiece(hoveringPiece) || !PrivateArea.CheckAccess(hoveringPiece.transform.position))
		{
			return;
		}
		bool flag = false;
		WearNTear component = hoveringPiece.GetComponent<WearNTear>();
		if ((bool)component && component.Repair())
		{
			flag = true;
		}
		if (flag)
		{
			FaceLookDirection();
			m_zanim.SetTrigger(toolItem.m_shared.m_attack.m_attackAnimation);
			hoveringPiece.m_placeEffect.Create(hoveringPiece.transform.position, hoveringPiece.transform.rotation);
			Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_repaired", hoveringPiece.m_name));
			UseStamina(toolItem.m_shared.m_attack.m_attackStamina, isHomeUsage: true);
			UseEitr(toolItem.m_shared.m_attack.m_attackEitr);
			if (toolItem.m_shared.m_useDurability)
			{
				toolItem.m_durability -= toolItem.m_shared.m_useDurabilityDrain;
			}
		}
		else
		{
			Message(MessageHud.MessageType.TopLeft, hoveringPiece.m_name + " $msg_doesnotneedrepair");
		}
	}

	private void UpdateWearNTearHover()
	{
		if (!InPlaceMode())
		{
			m_hoveringPiece = null;
			return;
		}
		m_hoveringPiece = null;
		if (!Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, m_removeRayMask) || !(Vector3.Distance(m_eye.position, hitInfo.point) < m_maxPlaceDistance))
		{
			return;
		}
		Piece piece = (m_hoveringPiece = hitInfo.collider.GetComponentInParent<Piece>());
		if ((bool)piece)
		{
			WearNTear component = piece.GetComponent<WearNTear>();
			if ((bool)component)
			{
				component.Highlight();
			}
		}
	}

	public Piece GetHoveringPiece()
	{
		if (!InPlaceMode())
		{
			return null;
		}
		return m_hoveringPiece;
	}

	private void UpdatePlacementGhost(bool flashGuardStone)
	{
		if (m_placementGhost == null)
		{
			if ((bool)m_placementMarkerInstance)
			{
				m_placementMarkerInstance.SetActive(value: false);
			}
			return;
		}
		bool flag = ((ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive()) ? m_altPlace : (ZInput.GetButton("AltPlace") || (ZInput.GetButton("JoyAltPlace") && !ZInput.GetButton("JoyRotate"))));
		Piece component = m_placementGhost.GetComponent<Piece>();
		bool water = component.m_waterPiece || component.m_noInWater;
		Quaternion quaternion;
		int manualSnapPoint;
		bool num;
		if (PieceRayTest(out var point, out var normal, out var piece, out var heightmap, out var waterSurface, water))
		{
			m_placementStatus = PlacementStatus.Valid;
			quaternion = Quaternion.Euler(0f, m_placeRotationDegrees * (float)m_placeRotation, 0f);
			if (m_placementMarkerInstance == null)
			{
				m_placementMarkerInstance = UnityEngine.Object.Instantiate(m_placeMarker, point, Quaternion.identity);
			}
			m_placementMarkerInstance.SetActive(value: true);
			m_placementMarkerInstance.transform.position = point;
			m_placementMarkerInstance.transform.rotation = Quaternion.LookRotation(normal, quaternion * Vector3.forward);
			if (component.m_groundOnly || component.m_groundPiece || component.m_cultivatedGroundOnly)
			{
				m_placementMarkerInstance.SetActive(value: false);
			}
			WearNTear wearNTear = ((piece != null) ? piece.GetComponent<WearNTear>() : null);
			StationExtension component2 = component.GetComponent<StationExtension>();
			if (component2 != null)
			{
				CraftingStation craftingStation = component2.FindClosestStationInRange(point);
				if ((bool)craftingStation)
				{
					component2.StartConnectionEffect(craftingStation);
				}
				else
				{
					component2.StopConnectionEffect();
					m_placementStatus = PlacementStatus.ExtensionMissingStation;
				}
				if (component2.OtherExtensionInRange(component.m_spaceRequirement))
				{
					m_placementStatus = PlacementStatus.MoreSpace;
				}
			}
			if (component.m_blockRadius > 0f && component.m_blockingPieces.Count > 0)
			{
				Collider[] array = Physics.OverlapSphere(point, component.m_blockRadius, LayerMask.GetMask("piece"));
				for (int i = 0; i < array.Length; i++)
				{
					Piece componentInParent = array[i].gameObject.GetComponentInParent<Piece>();
					if ((object)componentInParent == null || !(componentInParent != component))
					{
						continue;
					}
					foreach (Piece blockingPiece in component.m_blockingPieces)
					{
						if (blockingPiece.m_name == componentInParent.m_name)
						{
							m_placementStatus = PlacementStatus.MoreSpace;
							break;
						}
					}
				}
			}
			if (component.m_mustConnectTo != null)
			{
				ZNetView zNetView = null;
				Collider[] array = Physics.OverlapSphere(component.transform.position, component.m_connectRadius);
				for (int i = 0; i < array.Length; i++)
				{
					ZNetView componentInParent2 = array[i].GetComponentInParent<ZNetView>();
					if ((object)componentInParent2 == null || !(componentInParent2 != m_nview) || !componentInParent2.name.Contains(component.m_mustConnectTo.name))
					{
						continue;
					}
					if (component.m_mustBeAboveConnected)
					{
						Physics.Raycast(component.transform.position, Vector3.down, out var hitInfo);
						if (hitInfo.transform.GetComponentInParent<ZNetView>() != componentInParent2)
						{
							continue;
						}
					}
					zNetView = componentInParent2;
					break;
				}
				if (!zNetView)
				{
					m_placementStatus = PlacementStatus.Invalid;
				}
			}
			if ((bool)wearNTear && !wearNTear.m_supports)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_waterPiece && waterSurface == null && !flag)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_noInWater && waterSurface != null)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_groundPiece && heightmap == null)
			{
				m_placementGhost.SetActive(value: false);
				m_placementStatus = PlacementStatus.Invalid;
				return;
			}
			if (component.m_groundOnly && heightmap == null)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_cultivatedGroundOnly && (heightmap == null || !heightmap.IsCultivated(point)))
			{
				m_placementStatus = PlacementStatus.NeedCultivated;
			}
			if (component.m_vegetationGroundOnly)
			{
				bool flag2 = heightmap == null;
				if (!flag2)
				{
					Heightmap.Biome biome = heightmap.GetBiome(point);
					float vegetationMask = heightmap.GetVegetationMask(point);
					flag2 = ((biome == Heightmap.Biome.AshLands) ? (vegetationMask > 0.1f) : (vegetationMask < 0.25f));
				}
				if (flag2)
				{
					m_placementStatus = PlacementStatus.NeedDirt;
				}
			}
			if (component.m_notOnWood && (bool)piece && (bool)wearNTear && (wearNTear.m_materialType == WearNTear.MaterialType.Wood || wearNTear.m_materialType == WearNTear.MaterialType.HardWood))
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_notOnTiltingSurface && normal.y < 0.8f)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_inCeilingOnly && normal.y > -0.5f)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_notOnFloor && normal.y > 0.1f)
			{
				m_placementStatus = PlacementStatus.Invalid;
			}
			if (component.m_onlyInTeleportArea && !EffectArea.IsPointInsideArea(point, EffectArea.Type.Teleport))
			{
				m_placementStatus = PlacementStatus.NoTeleportArea;
			}
			if (!component.m_allowedInDungeons && InInterior() && !EnvMan.instance.CheckInteriorBuildingOverride() && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.DungeonBuild))
			{
				m_placementStatus = PlacementStatus.NotInDungeon;
			}
			if ((bool)heightmap)
			{
				normal = Vector3.up;
			}
			m_placementGhost.SetActive(value: true);
			manualSnapPoint = m_manualSnapPoint;
			if (!ZInput.GetButton("JoyAltKeys") && !Hud.IsPieceSelectionVisible() && Minimap.instance.m_mode != Minimap.MapMode.Large && !Console.IsVisible() && !Chat.instance.HasFocus())
			{
				if (ZInput.GetButtonDown("TabLeft") || (ZInput.GetButtonUp("JoyPrevSnap") && ZInput.GetButtonLastPressedTimer("JoyPrevSnap") < 0.33f))
				{
					m_manualSnapPoint--;
				}
				if (ZInput.GetButtonDown("TabRight") || (ZInput.GetButtonUp("JoyNextSnap") && ZInput.GetButtonLastPressedTimer("JoyNextSnap") < 0.33f))
				{
					m_manualSnapPoint++;
				}
			}
			m_tempSnapPoints1.Clear();
			m_placementGhost.GetComponent<Piece>().GetSnapPoints(m_tempSnapPoints1);
			if (m_manualSnapPoint < -1)
			{
				m_manualSnapPoint = m_tempSnapPoints1.Count - 1;
			}
			if (m_manualSnapPoint >= m_tempSnapPoints1.Count)
			{
				m_manualSnapPoint = -1;
			}
			if (((component.m_groundPiece || component.m_clipGround) && (bool)heightmap) || component.m_clipEverything)
			{
				GameObject selectedPrefab = m_buildPieces.GetSelectedPrefab();
				TerrainModifier component3 = selectedPrefab.GetComponent<TerrainModifier>();
				TerrainOp component4 = selectedPrefab.GetComponent<TerrainOp>();
				if (((bool)component3 || (bool)component4) && component.m_allowAltGroundPlacement)
				{
					if (!ZInput.IsNonClassicFunctionality() || !ZInput.IsGamepadActive())
					{
						if (component.m_groundPiece && !ZInput.GetButton("AltPlace"))
						{
							num = !ZInput.GetButton("JoyAltPlace");
							goto IL_06ea;
						}
					}
					else if (component.m_groundPiece)
					{
						num = !m_altPlace;
						goto IL_06ea;
					}
				}
				goto IL_070c;
			}
			Collider[] componentsInChildren = m_placementGhost.GetComponentsInChildren<Collider>();
			if (componentsInChildren.Length != 0)
			{
				m_placementGhost.transform.position = point + normal * 50f;
				m_placementGhost.transform.rotation = quaternion;
				Vector3 vector = Vector3.zero;
				float num2 = 999999f;
				Collider[] array = componentsInChildren;
				foreach (Collider collider in array)
				{
					if (collider.isTrigger || !collider.enabled)
					{
						continue;
					}
					MeshCollider meshCollider = collider as MeshCollider;
					if (!(meshCollider != null) || meshCollider.convex)
					{
						Vector3 vector2 = collider.ClosestPoint(point);
						float num3 = Vector3.Distance(vector2, point);
						if (num3 < num2)
						{
							vector = vector2;
							num2 = num3;
						}
					}
				}
				Vector3 vector3 = m_placementGhost.transform.position - vector;
				if (component.m_waterPiece)
				{
					vector3.y = 3f;
				}
				m_placementGhost.transform.position = point + ((m_manualSnapPoint < 0) ? vector3 : (quaternion * -m_tempSnapPoints1[m_manualSnapPoint].localPosition));
				m_placementGhost.transform.rotation = quaternion;
			}
			goto IL_08b4;
		}
		if ((bool)m_placementMarkerInstance)
		{
			m_placementMarkerInstance.SetActive(value: false);
		}
		m_placementGhost.SetActive(value: false);
		m_placementStatus = PlacementStatus.NoRayHits;
		goto IL_0a9d;
		IL_070c:
		m_placementGhost.transform.position = point + ((m_manualSnapPoint < 0) ? Vector3.zero : (quaternion * -m_tempSnapPoints1[m_manualSnapPoint].localPosition));
		m_placementGhost.transform.rotation = quaternion;
		goto IL_08b4;
		IL_0a9d:
		SetPlacementGhostValid(m_placementStatus == PlacementStatus.Valid);
		return;
		IL_06ea:
		if (num)
		{
			float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
			point.y = groundHeight;
		}
		goto IL_070c;
		IL_08b4:
		if (manualSnapPoint != m_manualSnapPoint)
		{
			Message(MessageHud.MessageType.Center, "$msg_snapping " + ((m_manualSnapPoint == -1) ? "$msg_snapping_auto" : m_tempSnapPoints1[m_manualSnapPoint].name));
		}
		if (!flag)
		{
			m_tempPieces.Clear();
			if (FindClosestSnapPoints(m_placementGhost.transform, 0.5f, out var a, out var b, m_tempPieces))
			{
				_ = b.parent.position;
				Vector3 vector4 = b.position - (a.position - m_placementGhost.transform.position);
				if (!IsOverlappingOtherPiece(vector4, m_placementGhost.transform.rotation, m_placementGhost.name, m_tempPieces, component.m_allowRotatedOverlap))
				{
					m_placementGhost.transform.position = vector4;
				}
			}
		}
		if (Location.IsInsideNoBuildLocation(m_placementGhost.transform.position))
		{
			m_placementStatus = PlacementStatus.NoBuildZone;
		}
		PrivateArea component5 = component.GetComponent<PrivateArea>();
		float radius = (component5 ? component5.m_radius : 0f);
		bool wardCheck = component5 != null;
		if (!PrivateArea.CheckAccess(m_placementGhost.transform.position, radius, flashGuardStone, wardCheck))
		{
			m_placementStatus = PlacementStatus.PrivateZone;
		}
		if (CheckPlacementGhostVSPlayers())
		{
			m_placementStatus = PlacementStatus.BlockedbyPlayer;
		}
		if (component.m_onlyInBiome != 0 && (Heightmap.FindBiome(m_placementGhost.transform.position) & component.m_onlyInBiome) == 0)
		{
			m_placementStatus = PlacementStatus.WrongBiome;
		}
		if (component.m_noClipping && TestGhostClipping(m_placementGhost, 0.2f))
		{
			m_placementStatus = PlacementStatus.Invalid;
		}
		goto IL_0a9d;
	}

	private bool IsOverlappingOtherPiece(Vector3 p, Quaternion rotation, string pieceName, List<Piece> pieces, bool allowRotatedOverlap)
	{
		foreach (Piece tempPiece in m_tempPieces)
		{
			if (Vector3.Distance(p, tempPiece.transform.position) < 0.05f && (!allowRotatedOverlap || !(Quaternion.Angle(tempPiece.transform.rotation, rotation) > 10f)) && tempPiece.gameObject.name.CustomStartsWith(pieceName))
			{
				return true;
			}
		}
		return false;
	}

	private bool FindClosestSnapPoints(Transform ghost, float maxSnapDistance, out Transform a, out Transform b, List<Piece> pieces)
	{
		m_tempSnapPoints1.Clear();
		ghost.GetComponent<Piece>().GetSnapPoints(m_tempSnapPoints1);
		m_tempSnapPoints2.Clear();
		m_tempPieces.Clear();
		Piece.GetSnapPoints(ghost.transform.position, 10f, m_tempSnapPoints2, m_tempPieces);
		float num = 9999999f;
		a = null;
		b = null;
		if (m_manualSnapPoint >= 0)
		{
			if (FindClosestSnappoint(m_tempSnapPoints1[m_manualSnapPoint].position, m_tempSnapPoints2, maxSnapDistance, out var closest, out var _))
			{
				a = m_tempSnapPoints1[m_manualSnapPoint];
				b = closest;
				return true;
			}
			return false;
		}
		foreach (Transform item in m_tempSnapPoints1)
		{
			if (FindClosestSnappoint(item.position, m_tempSnapPoints2, maxSnapDistance, out var closest2, out var distance2) && distance2 < num)
			{
				num = distance2;
				a = item;
				b = closest2;
			}
		}
		return a != null;
	}

	private bool FindClosestSnappoint(Vector3 p, List<Transform> snapPoints, float maxDistance, out Transform closest, out float distance)
	{
		closest = null;
		distance = 999999f;
		foreach (Transform snapPoint in snapPoints)
		{
			float num = Vector3.Distance(snapPoint.position, p);
			if (!(num > maxDistance) && num < distance)
			{
				closest = snapPoint;
				distance = num;
			}
		}
		return closest != null;
	}

	private bool TestGhostClipping(GameObject ghost, float maxPenetration)
	{
		Collider[] componentsInChildren = ghost.GetComponentsInChildren<Collider>();
		Collider[] array = Physics.OverlapSphere(ghost.transform.position, 10f, m_placeRayMask);
		Collider[] array2 = componentsInChildren;
		foreach (Collider collider in array2)
		{
			Collider[] array3 = array;
			foreach (Collider collider2 in array3)
			{
				if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out var _, out var distance) && distance > maxPenetration)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool CheckPlacementGhostVSPlayers()
	{
		if (m_placementGhost == null)
		{
			return false;
		}
		List<Character> list = new List<Character>();
		Character.GetCharactersInRange(base.transform.position, 30f, list);
		Collider[] componentsInChildren = m_placementGhost.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (collider.isTrigger || !collider.enabled)
			{
				continue;
			}
			MeshCollider meshCollider = collider as MeshCollider;
			if (meshCollider != null && !meshCollider.convex)
			{
				continue;
			}
			foreach (Character item in list)
			{
				CapsuleCollider collider2 = item.GetCollider();
				if (Physics.ComputePenetration(collider, collider.transform.position, collider.transform.rotation, collider2, collider2.transform.position, collider2.transform.rotation, out var _, out var _))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool PieceRayTest(out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water)
	{
		int layerMask = m_placeRayMask;
		if (water)
		{
			layerMask = m_placeWaterRayMask;
		}
		if (Physics.Raycast(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, out var hitInfo, 50f, layerMask))
		{
			float num = m_maxPlaceDistance;
			if ((bool)m_placementGhost)
			{
				Piece component = m_placementGhost.GetComponent<Piece>();
				if ((object)component != null)
				{
					num += (float)component.m_extraPlacementDistance;
				}
			}
			if ((bool)hitInfo.collider && !hitInfo.collider.attachedRigidbody && Vector3.Distance(m_eye.position, hitInfo.point) < num)
			{
				point = hitInfo.point;
				normal = hitInfo.normal;
				piece = hitInfo.collider.GetComponentInParent<Piece>();
				heightmap = hitInfo.collider.GetComponent<Heightmap>();
				if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
				{
					waterSurface = hitInfo.collider;
				}
				else
				{
					waterSurface = null;
				}
				return true;
			}
		}
		point = Vector3.zero;
		normal = Vector3.zero;
		piece = null;
		heightmap = null;
		waterSurface = null;
		return false;
	}

	private void FindHoverObject(out GameObject hover, out Character hoverCreature)
	{
		hover = null;
		hoverCreature = null;
		int num = Physics.RaycastNonAlloc(GameCamera.instance.transform.position, GameCamera.instance.transform.forward, m_raycastHoverHits, 50f, m_interactMask);
		Array.Sort(m_raycastHoverHits, 0, num, RaycastHitComparer.Instance);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = m_raycastHoverHits[i];
			if ((bool)raycastHit.collider.attachedRigidbody && raycastHit.collider.attachedRigidbody.gameObject == base.gameObject)
			{
				continue;
			}
			if (hoverCreature == null)
			{
				Character character = (raycastHit.collider.attachedRigidbody ? raycastHit.collider.attachedRigidbody.GetComponent<Character>() : raycastHit.collider.GetComponent<Character>());
				if (character != null && (!character.GetBaseAI() || !character.GetBaseAI().IsSleeping()) && !ParticleMist.IsMistBlocked(GetCenterPoint(), character.GetCenterPoint()))
				{
					hoverCreature = character;
				}
			}
			if (Vector3.Distance(m_eye.position, raycastHit.point) < m_maxInteractDistance)
			{
				if (raycastHit.collider.GetComponent<Hoverable>() != null)
				{
					hover = raycastHit.collider.gameObject;
				}
				else if ((bool)raycastHit.collider.attachedRigidbody)
				{
					hover = raycastHit.collider.attachedRigidbody.gameObject;
				}
				else
				{
					hover = raycastHit.collider.gameObject;
				}
			}
			break;
		}
	}

	private void Interact(GameObject go, bool hold, bool alt)
	{
		if (InAttack() || InDodge() || (hold && Time.time - m_lastHoverInteractTime < 0.2f))
		{
			return;
		}
		Interactable componentInParent = go.GetComponentInParent<Interactable>();
		if (componentInParent != null)
		{
			m_lastHoverInteractTime = Time.time;
			if (componentInParent.Interact(this, hold, alt))
			{
				DoInteractAnimation(go.transform.position);
			}
		}
	}

	private void UpdateStations(float dt)
	{
		m_stationDiscoverTimer += dt;
		if (m_stationDiscoverTimer > 1f)
		{
			m_stationDiscoverTimer = 0f;
			CraftingStation.UpdateKnownStationsInRange(this);
		}
		if (m_currentStation != null)
		{
			if (!m_currentStation.InUseDistance(this))
			{
				InventoryGui.instance.Hide();
				SetCraftingStation(null);
				return;
			}
			if (!InventoryGui.IsVisible())
			{
				SetCraftingStation(null);
				return;
			}
			m_currentStation.PokeInUse();
			if (!AlwaysRotateCamera())
			{
				Vector3 normalized = (m_currentStation.transform.position - base.transform.position).normalized;
				normalized.y = 0f;
				normalized.Normalize();
				Quaternion to = Quaternion.LookRotation(normalized);
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, m_turnSpeed * dt);
			}
			m_zanim.SetInt("crafting", m_currentStation.m_useAnimation);
			m_inCraftingStation = true;
		}
		else if (m_inCraftingStation)
		{
			m_zanim.SetInt("crafting", 0);
			m_inCraftingStation = false;
			if (InventoryGui.IsVisible())
			{
				InventoryGui.instance.Hide();
			}
		}
	}

	public void SetCraftingStation(CraftingStation station)
	{
		if (!(m_currentStation == station))
		{
			if ((bool)station)
			{
				AddKnownStation(station);
				station.PokeInUse();
				HideHandItems();
			}
			m_currentStation = station;
		}
	}

	public CraftingStation GetCurrentCraftingStation()
	{
		return m_currentStation;
	}

	private void UpdateCover(float dt)
	{
		m_updateCoverTimer += dt;
		if (m_updateCoverTimer > 1f)
		{
			m_updateCoverTimer = 0f;
			Cover.GetCoverForPoint(GetCenterPoint(), out m_coverPercentage, out m_underRoof);
		}
	}

	public Character GetHoverCreature()
	{
		return m_hoveringCreature;
	}

	public override GameObject GetHoverObject()
	{
		return m_hovering;
	}

	public override void OnNearFire(Vector3 point)
	{
		m_nearFireTimer = 0f;
	}

	public bool InShelter()
	{
		if (m_coverPercentage >= 0.8f)
		{
			return m_underRoof;
		}
		return false;
	}

	public float GetStamina()
	{
		return m_stamina;
	}

	public override float GetMaxStamina()
	{
		return m_maxStamina;
	}

	public float GetEitr()
	{
		return m_eitr;
	}

	public override float GetMaxEitr()
	{
		return m_maxEitr;
	}

	public override float GetEitrPercentage()
	{
		return m_eitr / m_maxEitr;
	}

	public override float GetStaminaPercentage()
	{
		return m_stamina / m_maxStamina;
	}

	public void SetGodMode(bool godMode)
	{
		m_godMode = godMode;
	}

	public override bool InGodMode()
	{
		return m_godMode;
	}

	public void SetGhostMode(bool ghostmode)
	{
		m_ghostMode = ghostmode;
	}

	public override bool InGhostMode()
	{
		return m_ghostMode;
	}

	public override bool IsDebugFlying()
	{
		if (m_nview == null || !m_nview.IsValid())
		{
			return false;
		}
		if (m_nview.IsOwner())
		{
			return m_debugFly;
		}
		return m_nview.GetZDO().GetBool(ZDOVars.s_debugFly);
	}

	public override void AddEitr(float v)
	{
		m_eitr += v;
		if (m_eitr > m_maxEitr)
		{
			m_eitr = m_maxEitr;
		}
	}

	public override void AddStamina(float v)
	{
		m_stamina += v;
		if (m_stamina > m_maxStamina)
		{
			m_stamina = m_maxStamina;
		}
	}

	public override void UseEitr(float v)
	{
		if (v != 0f && m_nview.IsValid())
		{
			if (m_nview.IsOwner())
			{
				RPC_UseEitr(0L, v);
				return;
			}
			m_nview.InvokeRPC("UseEitr", v);
		}
	}

	private void RPC_UseEitr(long sender, float v)
	{
		if (v != 0f)
		{
			m_eitr -= v;
			if (m_eitr < 0f)
			{
				m_eitr = 0f;
			}
			m_eitrRegenTimer = m_eitrRegenDelay;
		}
	}

	public override bool HaveEitr(float amount = 0f)
	{
		if (m_nview.IsValid() && !m_nview.IsOwner())
		{
			return m_nview.GetZDO().GetFloat(ZDOVars.s_eitr, m_maxEitr) > amount;
		}
		return m_eitr > amount;
	}

	public override void UseStamina(float v, bool isHomeUsage = false)
	{
		if (v == 0f)
		{
			return;
		}
		v *= Game.m_staminaRate;
		if (isHomeUsage)
		{
			v *= 1f + GetEquipmentHomeItemModifier();
			m_seman.ModifyHomeItemStaminaUsage(v, ref v);
		}
		if (m_nview.IsValid())
		{
			if (m_nview.IsOwner())
			{
				RPC_UseStamina(0L, v);
				return;
			}
			m_nview.InvokeRPC("UseStamina", v);
		}
	}

	private void RPC_UseStamina(long sender, float v)
	{
		if (v != 0f)
		{
			m_stamina -= v;
			if (m_stamina < 0f)
			{
				m_stamina = 0f;
			}
			m_staminaRegenTimer = m_staminaRegenDelay;
		}
	}

	public override bool HaveStamina(float amount = 0f)
	{
		if (m_nview.IsValid() && !m_nview.IsOwner())
		{
			return m_nview.GetZDO().GetFloat(ZDOVars.s_stamina, m_maxStamina) > amount;
		}
		return m_stamina > amount;
	}

	public void Save(ZPackage pkg)
	{
		pkg.Write(27);
		pkg.Write(GetMaxHealth());
		pkg.Write(GetHealth());
		pkg.Write(GetMaxStamina());
		pkg.Write(m_firstSpawn);
		pkg.Write(m_timeSinceDeath);
		pkg.Write(m_guardianPower);
		pkg.Write(m_guardianPowerCooldown);
		m_inventory.Save(pkg);
		pkg.Write(m_knownRecipes.Count);
		foreach (string knownRecipe in m_knownRecipes)
		{
			pkg.Write(knownRecipe);
		}
		pkg.Write(m_knownStations.Count);
		foreach (KeyValuePair<string, int> knownStation in m_knownStations)
		{
			pkg.Write(knownStation.Key);
			pkg.Write(knownStation.Value);
		}
		pkg.Write(m_knownMaterial.Count);
		foreach (string item in m_knownMaterial)
		{
			pkg.Write(item);
		}
		pkg.Write(m_shownTutorials.Count);
		foreach (string shownTutorial in m_shownTutorials)
		{
			pkg.Write(shownTutorial);
		}
		pkg.Write(m_uniques.Count);
		foreach (string unique in m_uniques)
		{
			pkg.Write(unique);
		}
		pkg.Write(m_trophies.Count);
		foreach (string trophy in m_trophies)
		{
			pkg.Write(trophy);
		}
		pkg.Write(m_knownBiome.Count);
		foreach (Heightmap.Biome item2 in m_knownBiome)
		{
			pkg.Write((int)item2);
		}
		pkg.Write(m_knownTexts.Count);
		foreach (KeyValuePair<string, string> knownText in m_knownTexts)
		{
			pkg.Write(knownText.Key.Replace("\u0016", ""));
			pkg.Write(knownText.Value.Replace("\u0016", ""));
		}
		pkg.Write(m_beardItem);
		pkg.Write(m_hairItem);
		pkg.Write(m_skinColor);
		pkg.Write(m_hairColor);
		pkg.Write(m_modelIndex);
		pkg.Write(m_foods.Count);
		foreach (Food food in m_foods)
		{
			pkg.Write(food.m_name);
			pkg.Write(food.m_time);
		}
		m_skills.Save(pkg);
		pkg.Write(m_customData.Count);
		foreach (KeyValuePair<string, string> customDatum in m_customData)
		{
			pkg.Write(customDatum.Key);
			pkg.Write(customDatum.Value);
		}
		pkg.Write(GetStamina());
		pkg.Write(GetMaxEitr());
		pkg.Write(GetEitr());
	}

	public void Load(ZPackage pkg)
	{
		m_isLoading = true;
		UnequipAllItems();
		int num = pkg.ReadInt();
		if (num >= 7)
		{
			SetMaxHealth(pkg.ReadSingle(), flashBar: false);
		}
		float num2 = pkg.ReadSingle();
		float maxHealth = GetMaxHealth();
		if (num2 <= 0f || num2 > maxHealth || float.IsNaN(num2))
		{
			num2 = maxHealth;
		}
		SetHealth(num2);
		if (num >= 10)
		{
			float stamina = pkg.ReadSingle();
			SetMaxStamina(stamina, flashBar: false);
			m_stamina = stamina;
		}
		if (num >= 8)
		{
			m_firstSpawn = pkg.ReadBool();
		}
		if (num >= 20)
		{
			m_timeSinceDeath = pkg.ReadSingle();
		}
		if (num >= 23)
		{
			string guardianPower = pkg.ReadString();
			SetGuardianPower(guardianPower);
		}
		if (num >= 24)
		{
			m_guardianPowerCooldown = pkg.ReadSingle();
		}
		if (num == 2)
		{
			pkg.ReadZDOID();
		}
		m_inventory.Load(pkg);
		int num3 = pkg.ReadInt();
		for (int i = 0; i < num3; i++)
		{
			string item = pkg.ReadString();
			m_knownRecipes.Add(item);
		}
		if (num < 15)
		{
			int num4 = pkg.ReadInt();
			for (int j = 0; j < num4; j++)
			{
				pkg.ReadString();
			}
		}
		else
		{
			int num5 = pkg.ReadInt();
			for (int k = 0; k < num5; k++)
			{
				string key = pkg.ReadString();
				int value = pkg.ReadInt();
				m_knownStations.Add(key, value);
			}
		}
		int num6 = pkg.ReadInt();
		for (int l = 0; l < num6; l++)
		{
			string item2 = pkg.ReadString();
			m_knownMaterial.Add(item2);
		}
		if (num < 19 || num >= 21)
		{
			int num7 = pkg.ReadInt();
			for (int m = 0; m < num7; m++)
			{
				string item3 = pkg.ReadString();
				m_shownTutorials.Add(item3);
			}
		}
		if (num >= 6)
		{
			int num8 = pkg.ReadInt();
			for (int n = 0; n < num8; n++)
			{
				string item4 = pkg.ReadString();
				m_uniques.Add(item4);
			}
		}
		if (num >= 9)
		{
			int num9 = pkg.ReadInt();
			for (int num10 = 0; num10 < num9; num10++)
			{
				string item5 = pkg.ReadString();
				m_trophies.Add(item5);
			}
		}
		if (num >= 18)
		{
			int num11 = pkg.ReadInt();
			for (int num12 = 0; num12 < num11; num12++)
			{
				Heightmap.Biome item6 = (Heightmap.Biome)pkg.ReadInt();
				m_knownBiome.Add(item6);
			}
		}
		if (num >= 22)
		{
			int num13 = pkg.ReadInt();
			for (int num14 = 0; num14 < num13; num14++)
			{
				string key2 = pkg.ReadString();
				string value2 = pkg.ReadString();
				m_knownTexts[key2] = value2;
			}
		}
		if (num >= 4)
		{
			string beard = pkg.ReadString();
			string hair = pkg.ReadString();
			SetBeard(beard);
			SetHair(hair);
		}
		if (num >= 5)
		{
			Vector3 skinColor = pkg.ReadVector3();
			Vector3 hairColor = pkg.ReadVector3();
			SetSkinColor(skinColor);
			SetHairColor(hairColor);
		}
		if (num >= 11)
		{
			int playerModel = pkg.ReadInt();
			SetPlayerModel(playerModel);
		}
		if (num >= 12)
		{
			m_foods.Clear();
			int num15 = pkg.ReadInt();
			for (int num16 = 0; num16 < num15; num16++)
			{
				if (num >= 14)
				{
					Food food = new Food();
					food.m_name = pkg.ReadString();
					if (num >= 25)
					{
						food.m_time = pkg.ReadSingle();
					}
					else
					{
						food.m_health = pkg.ReadSingle();
						if (num >= 16)
						{
							food.m_stamina = pkg.ReadSingle();
						}
					}
					GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(food.m_name);
					if (itemPrefab == null)
					{
						ZLog.LogWarning("Failed to find food item " + food.m_name);
						continue;
					}
					food.m_item = itemPrefab.GetComponent<ItemDrop>().m_itemData;
					m_foods.Add(food);
				}
				else
				{
					pkg.ReadString();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					pkg.ReadSingle();
					if (num >= 13)
					{
						pkg.ReadSingle();
					}
				}
			}
		}
		if (num >= 17)
		{
			m_skills.Load(pkg);
		}
		if (num >= 26)
		{
			int num17 = pkg.ReadInt();
			for (int num18 = 0; num18 < num17; num18++)
			{
				string key3 = pkg.ReadString();
				string value3 = pkg.ReadString();
				m_customData[key3] = value3;
			}
			m_stamina = Mathf.Clamp(pkg.ReadSingle(), 0f, m_maxStamina);
			SetMaxEitr(pkg.ReadSingle(), flashBar: false);
			m_eitr = Mathf.Clamp(pkg.ReadSingle(), 0f, m_maxEitr);
		}
		if (num < 27)
		{
			if (m_knownMaterial.Contains("$item_flametal"))
			{
				ZLog.DevLog("Pre ashlands character loaded, replacing flametal with ancient as known material.");
				m_knownMaterial.Remove("$item_flametal");
				m_knownMaterial.Add("$item_flametal_old");
			}
			if (m_knownMaterial.Contains("$item_flametalore"))
			{
				ZLog.DevLog("Pre ashlands character loaded, replacing flametal ore with ancient as known material.");
				m_knownMaterial.Remove("$item_flametalore");
				m_knownMaterial.Add("$item_flametalore_old");
			}
		}
		m_isLoading = false;
		UpdateAvailablePiecesList();
		EquipInventoryItems();
		UpdateEvents();
	}

	private void EquipInventoryItems()
	{
		foreach (ItemDrop.ItemData equippedItem in m_inventory.GetEquippedItems())
		{
			if (!EquipItem(equippedItem, triggerEquipEffects: false))
			{
				equippedItem.m_equipped = false;
			}
		}
	}

	public override bool CanMove()
	{
		if (m_teleporting)
		{
			return false;
		}
		if (InCutscene())
		{
			return false;
		}
		if (IsEncumbered() && !HaveStamina())
		{
			return false;
		}
		return base.CanMove();
	}

	public override bool IsEncumbered()
	{
		return m_inventory.GetTotalWeight() > GetMaxCarryWeight();
	}

	public float GetMaxCarryWeight()
	{
		float limit = m_maxCarryWeight;
		m_seman.ModifyMaxCarryWeight(limit, ref limit);
		return limit;
	}

	public override bool HaveUniqueKey(string name)
	{
		return m_uniques.Contains(name);
	}

	public override void AddUniqueKey(string name)
	{
		if (!m_uniques.Contains(name))
		{
			m_uniques.Add(name);
		}
		ZoneSystem.instance?.UpdateWorldRates();
		UpdateEvents();
	}

	public override bool RemoveUniqueKey(string name)
	{
		if (m_uniques.Contains(name))
		{
			m_uniques.Remove(name);
			ZoneSystem.instance.UpdateWorldRates();
			UpdateEvents();
			return true;
		}
		return false;
	}

	public List<string> GetUniqueKeys()
	{
		m_tempUniqueKeys.Clear();
		m_tempUniqueKeys.AddRange(m_uniques);
		return m_tempUniqueKeys;
	}

	public void ResetUniqueKeys()
	{
		m_uniques.Clear();
	}

	public bool IsBiomeKnown(Heightmap.Biome biome)
	{
		return m_knownBiome.Contains(biome);
	}

	private void AddKnownBiome(Heightmap.Biome biome)
	{
		if (!m_knownBiome.Contains(biome))
		{
			m_knownBiome.Add(biome);
			if (biome != Heightmap.Biome.Meadows && biome != 0)
			{
				string text = "$biome_" + biome.ToString().ToLower();
				MessageHud.instance.ShowBiomeFoundMsg(text, playStinger: true);
			}
			if (biome == Heightmap.Biome.BlackForest && !ZoneSystem.instance.GetGlobalKey("defeated_eikthyr"))
			{
				ShowTutorial("blackforest");
			}
			Gogan.LogEvent("Game", "BiomeFound", biome.ToString(), 0L);
		}
		if (biome == Heightmap.Biome.BlackForest)
		{
			ShowTutorial("haldor");
		}
		if (biome == Heightmap.Biome.AshLands)
		{
			ShowTutorial("ashlands");
		}
	}

	public void AddKnownLocationName(string label)
	{
		if (!m_shownTutorials.Contains(label))
		{
			m_shownTutorials.Add(label);
			MessageHud.instance.ShowBiomeFoundMsg(label, playStinger: true);
		}
	}

	public bool IsRecipeKnown(string name)
	{
		return m_knownRecipes.Contains(name);
	}

	private void AddKnownRecipe(Recipe recipe)
	{
		if (!m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name))
		{
			m_knownRecipes.Add(recipe.m_item.m_itemData.m_shared.m_name);
			MessageHud.instance.QueueUnlockMsg(recipe.m_item.m_itemData.GetIcon(), "$msg_newrecipe", recipe.m_item.m_itemData.m_shared.m_name);
			Gogan.LogEvent("Game", "RecipeFound", recipe.m_item.m_itemData.m_shared.m_name, 0L);
		}
	}

	private void AddKnownPiece(Piece piece)
	{
		if (!m_knownRecipes.Contains(piece.m_name))
		{
			m_knownRecipes.Add(piece.m_name);
			MessageHud.instance.QueueUnlockMsg(piece.m_icon, "$msg_newpiece", piece.m_name);
			Gogan.LogEvent("Game", "PieceFound", piece.m_name, 0L);
		}
	}

	public void AddKnownStation(CraftingStation station)
	{
		int level = station.GetLevel();
		if (m_knownStations.TryGetValue(station.m_name, out var value))
		{
			if (value < level)
			{
				m_knownStations[station.m_name] = level;
				MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation_level", station.m_name + " $msg_level " + level);
				UpdateKnownRecipesList();
			}
		}
		else
		{
			m_knownStations.Add(station.m_name, level);
			MessageHud.instance.QueueUnlockMsg(station.m_icon, "$msg_newstation", station.m_name);
			Gogan.LogEvent("Game", "StationFound", station.m_name, 0L);
			UpdateKnownRecipesList();
		}
	}

	private bool KnowStationLevel(string name, int level)
	{
		if (m_knownStations.TryGetValue(name, out var value))
		{
			return value >= level;
		}
		return false;
	}

	public void AddKnownText(string label, string text)
	{
		if (label.Length == 0)
		{
			ZLog.LogWarning("Text " + text + " Is missing label");
		}
		else if (!m_knownTexts.ContainsKey(label.Replace("\u0016", "")))
		{
			m_knownTexts.Add(label, text);
			Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_newtext", label), 0, m_textIcon);
		}
	}

	public List<KeyValuePair<string, string>> GetKnownTexts()
	{
		return m_knownTexts.ToList();
	}

	public void AddKnownItem(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
		{
			AddTrophy(item);
		}
		if (!m_knownMaterial.Contains(item.m_shared.m_name))
		{
			m_knownMaterial.Add(item.m_shared.m_name);
			if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material)
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newmaterial", item.m_shared.m_name);
			}
			else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newtrophy", item.m_shared.m_name);
			}
			else
			{
				MessageHud.instance.QueueUnlockMsg(item.GetIcon(), "$msg_newitem", item.m_shared.m_name);
			}
			Gogan.LogEvent("Game", "ItemFound", item.m_shared.m_name, 0L);
			UpdateKnownRecipesList();
			UpdateEvents();
		}
	}

	private void AddTrophy(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy && !m_trophies.Contains(item.m_dropPrefab.name))
		{
			m_trophies.Add(item.m_dropPrefab.name);
		}
	}

	public List<string> GetTrophies()
	{
		List<string> list = new List<string>();
		list.AddRange(m_trophies);
		return list;
	}

	private void UpdateKnownRecipesList()
	{
		if (Game.instance == null)
		{
			return;
		}
		foreach (Recipe recipe in ObjectDB.instance.m_recipes)
		{
			bool flag = m_currentSeason != null && m_currentSeason.Recipes.Contains(recipe);
			if ((recipe.m_enabled || flag) && (bool)recipe.m_item && !m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) && HaveRequirements(recipe, discover: true, 0))
			{
				AddKnownRecipe(recipe);
			}
		}
		m_tempOwnedPieceTables.Clear();
		m_inventory.GetAllPieceTables(m_tempOwnedPieceTables);
		bool flag2 = false;
		foreach (PieceTable tempOwnedPieceTable in m_tempOwnedPieceTables)
		{
			foreach (GameObject piece in tempOwnedPieceTable.m_pieces)
			{
				Piece component = piece.GetComponent<Piece>();
				bool flag3 = m_currentSeason != null && m_currentSeason.Pieces.Contains(piece);
				if ((component.m_enabled || flag3) && !m_knownRecipes.Contains(component.m_name) && HaveRequirements(component, RequirementMode.IsKnown))
				{
					AddKnownPiece(component);
					flag2 = true;
				}
			}
		}
		if (flag2)
		{
			UpdateAvailablePiecesList();
		}
	}

	private void UpdateAvailablePiecesList()
	{
		if (m_buildPieces != null)
		{
			m_buildPieces.UpdateAvailable(m_knownRecipes, this, hideUnavailable: false, m_noPlacementCost || ZoneSystem.instance.GetGlobalKey(GlobalKeys.AllPiecesUnlocked));
		}
		SetupPlacementGhost();
	}

	private void UpdateCurrentSeason()
	{
		m_currentSeason = null;
		foreach (SeasonalItemGroup seasonalItemGroup in m_seasonalItemGroups)
		{
			if (seasonalItemGroup.IsInSeason())
			{
				m_currentSeason = seasonalItemGroup;
				break;
			}
		}
	}

	public override void Message(MessageHud.MessageType type, string msg, int amount = 0, Sprite icon = null)
	{
		if (m_nview == null || !m_nview.IsValid())
		{
			return;
		}
		if (m_nview.IsOwner())
		{
			if ((bool)MessageHud.instance)
			{
				MessageHud.instance.ShowMessage(type, msg, amount, icon);
			}
		}
		else
		{
			m_nview.InvokeRPC("Message", (int)type, msg, amount);
		}
	}

	private void RPC_Message(long sender, int type, string msg, int amount)
	{
		if (m_nview.IsOwner() && (bool)MessageHud.instance)
		{
			MessageHud.instance.ShowMessage((MessageHud.MessageType)type, msg, amount);
		}
	}

	public static Player GetPlayer(long playerID)
	{
		foreach (Player s_player in s_players)
		{
			if (s_player.GetPlayerID() == playerID)
			{
				return s_player;
			}
		}
		return null;
	}

	public static Player GetClosestPlayer(Vector3 point, float maxRange)
	{
		Player result = null;
		float num = 999999f;
		foreach (Player s_player in s_players)
		{
			float num2 = Vector3.Distance(s_player.transform.position, point);
			if (num2 < num && num2 < maxRange)
			{
				num = num2;
				result = s_player;
			}
		}
		return result;
	}

	public static bool IsPlayerInRange(Vector3 point, float range, long playerID)
	{
		foreach (Player s_player in s_players)
		{
			if (s_player.GetPlayerID() == playerID)
			{
				return Utils.DistanceXZ(s_player.transform.position, point) < range;
			}
		}
		return false;
	}

	public static void MessageAllInRange(Vector3 point, float range, MessageHud.MessageType type, string msg, Sprite icon = null)
	{
		foreach (Player s_player in s_players)
		{
			if (Vector3.Distance(s_player.transform.position, point) < range)
			{
				s_player.Message(type, msg, 0, icon);
			}
		}
	}

	public static int GetPlayersInRangeXZ(Vector3 point, float range)
	{
		int num = 0;
		foreach (Player s_player in s_players)
		{
			if (Utils.DistanceXZ(s_player.transform.position, point) < range)
			{
				num++;
			}
		}
		return num;
	}

	private static void GetPlayersInRange(Vector3 point, float range, List<Player> players)
	{
		foreach (Player s_player in s_players)
		{
			if (Vector3.Distance(s_player.transform.position, point) < range)
			{
				players.Add(s_player);
			}
		}
	}

	public static bool IsPlayerInRange(Vector3 point, float range)
	{
		foreach (Player s_player in s_players)
		{
			if (Vector3.Distance(s_player.transform.position, point) < range)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsPlayerInRange(Vector3 point, float range, float minNoise)
	{
		foreach (Player s_player in s_players)
		{
			if (Vector3.Distance(s_player.transform.position, point) < range)
			{
				float noiseRange = s_player.GetNoiseRange();
				if (range <= noiseRange && noiseRange >= minNoise)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static Player GetPlayerNoiseRange(Vector3 point, float maxNoiseRange = 100f)
	{
		foreach (Player s_player in s_players)
		{
			float num = Vector3.Distance(s_player.transform.position, point);
			float num2 = Mathf.Min(s_player.GetNoiseRange(), maxNoiseRange);
			if (num < num2)
			{
				return s_player;
			}
		}
		return null;
	}

	public static List<Player> GetAllPlayers()
	{
		return s_players;
	}

	public static Player GetRandomPlayer()
	{
		if (s_players.Count == 0)
		{
			return null;
		}
		return s_players[UnityEngine.Random.Range(0, s_players.Count)];
	}

	public void GetAvailableRecipes(ref List<Recipe> available)
	{
		available.Clear();
		foreach (Recipe recipe in ObjectDB.instance.m_recipes)
		{
			bool flag = m_currentSeason != null && m_currentSeason.Recipes.Contains(recipe);
			if ((!recipe.m_enabled && !flag) || !recipe.m_item)
			{
				continue;
			}
			if (s_FilterCraft.Count > 0)
			{
				bool flag2 = false;
				for (int i = 0; i < s_FilterCraft.Count && (s_FilterCraft[i].Length <= 0 || (!recipe.m_item.name.ToLower().Contains(s_FilterCraft[i].ToLower()) && !recipe.m_item.m_itemData.m_shared.m_name.ToLower().Contains(s_FilterCraft[i].ToLower()) && !Localization.instance.Localize(recipe.m_item.m_itemData.m_shared.m_name).ToLower().Contains(s_FilterCraft[i].ToLower()))); i++)
				{
					if (i + 1 == s_FilterCraft.Count)
					{
						flag2 = true;
					}
				}
				if (flag2)
				{
					continue;
				}
			}
			if ((recipe.m_item.m_itemData.m_shared.m_dlc.Length <= 0 || DLCMan.instance.IsDLCInstalled(recipe.m_item.m_itemData.m_shared.m_dlc)) && (m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) || m_noPlacementCost || ZoneSystem.instance.GetGlobalKey(GlobalKeys.AllRecipesUnlocked)) && (RequiredCraftingStation(recipe, 1, checkLevel: false) || m_noPlacementCost))
			{
				available.Add(recipe);
			}
		}
	}

	private void OnInventoryChanged()
	{
		if (m_isLoading)
		{
			return;
		}
		foreach (ItemDrop.ItemData allItem in m_inventory.GetAllItems())
		{
			AddKnownItem(allItem);
			if (!allItem.m_pickedUp)
			{
				allItem.m_pickedUp = true;
				PlayerProfile playerProfile = Game.instance.GetPlayerProfile();
				playerProfile.IncrementStat(PlayerStatType.ItemsPickedUp);
				playerProfile.m_itemPickupStats.IncrementOrSet(allItem.m_shared.m_name, allItem.m_stack);
			}
			if (allItem.m_shared.m_name == "$item_hammer")
			{
				ShowTutorial("hammer");
			}
			else if (allItem.m_shared.m_name == "$item_hoe")
			{
				ShowTutorial("hoe");
			}
			else if (allItem.m_shared.m_name == "$item_bellfragment")
			{
				ShowTutorial("bellfragment");
			}
			else if (allItem.m_shared.m_name == "$item_pickaxe_antler")
			{
				ShowTutorial("pickaxe");
			}
			else if (allItem.m_shared.m_name.CustomStartsWith("$item_shield"))
			{
				ShowTutorial("shield");
			}
			if (allItem.m_shared.m_name == "$item_trophy_eikthyr")
			{
				ShowTutorial("boss_trophy");
			}
			if (allItem.m_shared.m_name == "$item_wishbone")
			{
				ShowTutorial("wishbone");
			}
			else if (allItem.m_shared.m_name == "$item_copperore" || allItem.m_shared.m_name == "$item_tinore")
			{
				ShowTutorial("ore");
			}
			else if (allItem.m_shared.m_food > 0f || allItem.m_shared.m_foodStamina > 0f)
			{
				ShowTutorial("food");
			}
		}
		UpdateKnownRecipesList();
		UpdateAvailablePiecesList();
	}

	public bool InDebugFlyMode()
	{
		return m_debugFly;
	}

	public void ShowTutorial(string name, bool force = false)
	{
		if (!HaveSeenTutorial(name))
		{
			Tutorial.instance.ShowText(name, force);
		}
	}

	public void SetSeenTutorial(string name)
	{
		if (name.Length != 0 && !m_shownTutorials.Contains(name))
		{
			m_shownTutorials.Add(name);
		}
	}

	public bool HaveSeenTutorial(string name)
	{
		if (name.Length == 0)
		{
			return false;
		}
		return m_shownTutorials.Contains(name);
	}

	public static bool IsSeenTutorialsCleared()
	{
		if ((bool)m_localPlayer)
		{
			return m_localPlayer.m_shownTutorials.Count == 0;
		}
		return true;
	}

	public static void ResetSeenTutorials()
	{
		if ((bool)m_localPlayer)
		{
			m_localPlayer.m_shownTutorials.Clear();
		}
	}

	public void SetMouseLook(Vector2 mouseLook)
	{
		Quaternion quaternion = m_lookYaw * Quaternion.Euler(0f, mouseLook.x, 0f);
		if (PlayerCustomizaton.IsBarberGuiVisible())
		{
			if (Vector3.Dot(base.transform.rotation * Vector3.forward, m_lookYaw * Vector3.forward) > 0f)
			{
				SetMouseLookBackward();
			}
			if (Vector3.Dot(base.transform.rotation * Vector3.forward, quaternion * Vector3.forward) < 0f)
			{
				m_lookYaw = quaternion;
			}
		}
		else
		{
			m_lookYaw = quaternion;
		}
		m_lookPitch = Mathf.Clamp(m_lookPitch - mouseLook.y, -89f, 89f);
		UpdateEyeRotation();
		m_lookDir = m_eye.forward;
		if (m_lookTransitionTime > 0f && mouseLook != Vector2.zero)
		{
			m_lookTransitionTime = 0f;
		}
	}

	public void SetMouseLookForward(bool includePitch = true)
	{
		m_lookYaw = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y, 0f);
		if (includePitch)
		{
			m_lookPitch = 0f;
		}
	}

	public void SetMouseLookBackward(bool includePitch = true)
	{
		m_lookYaw = Quaternion.Euler(0f, base.transform.rotation.eulerAngles.y + 180f, 0f);
		if (includePitch)
		{
			m_lookPitch = 0f;
		}
	}

	protected override void UpdateEyeRotation()
	{
		m_eye.rotation = m_lookYaw * Quaternion.Euler(m_lookPitch, 0f, 0f);
	}

	public Ragdoll GetRagdoll()
	{
		return m_ragdoll;
	}

	public void OnDodgeMortal()
	{
		m_dodgeInvincible = false;
	}

	private void UpdateDodge(float dt)
	{
		m_queuedDodgeTimer -= dt;
		if (m_queuedDodgeTimer > 0f && IsOnGround() && !IsDead() && !InAttack() && !IsEncumbered() && !InDodge() && !IsStaggering())
		{
			float staminaUse = m_dodgeStaminaUsage - m_dodgeStaminaUsage * GetEquipmentMovementModifier() + m_dodgeStaminaUsage * GetEquipmentDodgeStaminaModifier();
			m_seman.ModifyDodgeStaminaUsage(staminaUse, ref staminaUse);
			if (HaveStamina(staminaUse))
			{
				ClearActionQueue();
				m_queuedDodgeTimer = 0f;
				m_dodgeInvincible = true;
				base.transform.rotation = Quaternion.LookRotation(m_queuedDodgeDir);
				m_body.rotation = base.transform.rotation;
				m_zanim.SetTrigger("dodge");
				AddNoise(5f);
				UseStamina(staminaUse);
				m_dodgeEffects.Create(base.transform.position, Quaternion.identity, base.transform);
			}
			else
			{
				Hud.instance.StaminaBarEmptyFlash();
			}
		}
		bool flag = m_animator.GetBool(s_animatorTagDodge) || GetNextOrCurrentAnimHash() == s_animatorTagDodge;
		bool value = flag && m_dodgeInvincible;
		m_nview.GetZDO().Set(ZDOVars.s_dodgeinv, value);
		m_inDodge = flag;
	}

	public override bool IsDodgeInvincible()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetBool(ZDOVars.s_dodgeinv);
	}

	public override bool InDodge()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return false;
		}
		return m_inDodge;
	}

	public override bool IsDead()
	{
		return m_nview.GetZDO()?.GetBool(ZDOVars.s_dead) ?? false;
	}

	private void Dodge(Vector3 dodgeDir)
	{
		m_queuedDodgeTimer = 0.5f;
		m_queuedDodgeDir = dodgeDir;
	}

	protected override bool AlwaysRotateCamera()
	{
		ItemDrop.ItemData currentWeapon = GetCurrentWeapon();
		if ((currentWeapon != null && m_currentAttack != null && m_lastCombatTimer < 1f && m_currentAttack.m_attackType != Attack.AttackType.None && !m_attackTowardsPlayerLookDir) || IsDrawingBow() || m_blocking)
		{
			return true;
		}
		if (currentWeapon != null && currentWeapon.m_shared.m_alwaysRotate && m_moveDir.magnitude < 0.01f)
		{
			return true;
		}
		if (m_currentAttack != null && m_currentAttack.m_loopingAttack && InAttack())
		{
			return true;
		}
		if (InPlaceMode())
		{
			Vector3 from = GetLookYaw() * Vector3.forward;
			Vector3 forward = base.transform.forward;
			if (Vector3.Angle(from, forward) > 95f)
			{
				return true;
			}
		}
		return false;
	}

	public override bool TeleportTo(Vector3 pos, Quaternion rot, bool distantTeleport)
	{
		if (!m_nview.IsOwner())
		{
			m_nview.InvokeRPC("RPC_TeleportTo", pos, rot, distantTeleport);
			return false;
		}
		if (IsTeleporting())
		{
			return false;
		}
		if (m_teleportCooldown < 2f)
		{
			return false;
		}
		m_teleporting = true;
		m_distantTeleport = distantTeleport;
		m_teleportTimer = 0f;
		m_teleportCooldown = 0f;
		InvalidateCachedLiquidDepth();
		m_teleportFromPos = base.transform.position;
		m_teleportFromRot = base.transform.rotation;
		m_teleportTargetPos = pos;
		m_teleportTargetRot = rot;
		return true;
	}

	private void UpdateTeleport(float dt)
	{
		if (!m_teleporting)
		{
			m_teleportCooldown += dt;
			return;
		}
		m_teleportCooldown = 0f;
		m_teleportTimer += dt;
		if (!(m_teleportTimer > 2f))
		{
			return;
		}
		Vector3 dir = m_teleportTargetRot * Vector3.forward;
		base.transform.position = m_teleportTargetPos;
		base.transform.rotation = m_teleportTargetRot;
		m_body.velocity = Vector3.zero;
		m_maxAirAltitude = base.transform.position.y;
		SetLookDir(dir);
		if ((!(m_teleportTimer > 8f) && m_distantTeleport) || !ZNetScene.instance.IsAreaReady(m_teleportTargetPos))
		{
			return;
		}
		float height = 0f;
		if (ZoneSystem.instance.FindFloor(m_teleportTargetPos, out height))
		{
			m_teleportTimer = 0f;
			m_teleporting = false;
			ResetCloth();
		}
		else if (m_teleportTimer > 15f || !m_distantTeleport)
		{
			if (m_distantTeleport)
			{
				Vector3 position = base.transform.position;
				position.y = ZoneSystem.instance.GetSolidHeight(m_teleportTargetPos) + 0.5f;
				base.transform.position = position;
			}
			else
			{
				base.transform.rotation = m_teleportFromRot;
				base.transform.position = m_teleportFromPos;
				m_maxAirAltitude = base.transform.position.y;
				Message(MessageHud.MessageType.Center, "$msg_portal_blocked");
			}
			m_teleportTimer = 0f;
			m_teleporting = false;
			ResetCloth();
		}
	}

	public override bool IsTeleporting()
	{
		return m_teleporting;
	}

	public bool ShowTeleportAnimation()
	{
		if (m_teleporting)
		{
			return m_distantTeleport;
		}
		return false;
	}

	public void SetPlayerModel(int index)
	{
		if (m_modelIndex != index)
		{
			m_modelIndex = index;
			m_visEquipment.SetModel(index);
		}
	}

	public int GetPlayerModel()
	{
		return m_modelIndex;
	}

	public void SetSkinColor(Vector3 color)
	{
		if (!(color == m_skinColor))
		{
			m_skinColor = color;
			m_visEquipment.SetSkinColor(m_skinColor);
		}
	}

	public void SetHairColor(Vector3 color)
	{
		if (!(m_hairColor == color))
		{
			m_hairColor = color;
			m_visEquipment.SetHairColor(m_hairColor);
		}
	}

	public Vector3 GetHairColor()
	{
		return m_hairColor;
	}

	protected override void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
	{
		base.SetupVisEquipment(visEq, isRagdoll);
		visEq.SetModel(m_modelIndex);
		visEq.SetSkinColor(m_skinColor);
		visEq.SetHairColor(m_hairColor);
	}

	public override bool CanConsumeItem(ItemDrop.ItemData item, bool checkWorldLevel = false)
	{
		if (!base.CanConsumeItem(item, checkWorldLevel))
		{
			return false;
		}
		if (item.m_shared.m_food > 0f && !CanEat(item, showMessages: true))
		{
			return false;
		}
		if ((bool)item.m_shared.m_consumeStatusEffect)
		{
			StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
			if (m_seman.HaveStatusEffect(item.m_shared.m_consumeStatusEffect.NameHash()) || m_seman.HaveStatusEffectCategory(consumeStatusEffect.m_category))
			{
				Message(MessageHud.MessageType.Center, "$msg_cantconsume");
				return false;
			}
		}
		return true;
	}

	public override bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item, bool checkWorldLevel = false)
	{
		if (!CanConsumeItem(item, checkWorldLevel))
		{
			return false;
		}
		if ((bool)item.m_shared.m_consumeStatusEffect)
		{
			_ = item.m_shared.m_consumeStatusEffect;
			m_seman.AddStatusEffect(item.m_shared.m_consumeStatusEffect, resetTime: true);
		}
		if (item.m_shared.m_food > 0f)
		{
			EatFood(item);
		}
		inventory.RemoveOneItem(item);
		return true;
	}

	public void SetIntro(bool intro)
	{
		if (m_intro != intro)
		{
			m_intro = intro;
			m_zanim.SetBool("intro", intro);
		}
	}

	public override bool InIntro()
	{
		return m_intro;
	}

	public override bool InCutscene()
	{
		if (GetCurrentAnimHash() == s_animatorTagCutscene)
		{
			return true;
		}
		if (InIntro())
		{
			return true;
		}
		if (m_sleeping)
		{
			return true;
		}
		return base.InCutscene();
	}

	public void SetMaxStamina(float stamina, bool flashBar)
	{
		if (flashBar && Hud.instance != null && stamina > m_maxStamina)
		{
			Hud.instance.StaminaBarUppgradeFlash();
		}
		m_maxStamina = stamina;
		m_stamina = Mathf.Clamp(m_stamina, 0f, m_maxStamina);
	}

	private void SetMaxEitr(float eitr, bool flashBar)
	{
		if (flashBar && Hud.instance != null && eitr > m_maxEitr)
		{
			Hud.instance.EitrBarUppgradeFlash();
		}
		m_maxEitr = eitr;
		m_eitr = Mathf.Clamp(m_eitr, 0f, m_maxEitr);
	}

	public void SetMaxHealth(float health, bool flashBar)
	{
		if (flashBar && Hud.instance != null && health > GetMaxHealth())
		{
			Hud.instance.FlashHealthBar();
		}
		SetMaxHealth(health);
	}

	public override bool IsPVPEnabled()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (m_nview.IsOwner())
		{
			return m_pvp;
		}
		return m_nview.GetZDO().GetBool(ZDOVars.s_pvp);
	}

	public void SetPVP(bool enabled)
	{
		if (m_pvp != enabled)
		{
			m_pvp = enabled;
			m_nview.GetZDO().Set(ZDOVars.s_pvp, m_pvp);
			if (m_pvp)
			{
				Message(MessageHud.MessageType.Center, "$msg_pvpon");
			}
			else
			{
				Message(MessageHud.MessageType.Center, "$msg_pvpoff");
			}
		}
	}

	public bool CanSwitchPVP()
	{
		return m_lastCombatTimer > 10f;
	}

	public bool NoCostCheat()
	{
		return m_noPlacementCost;
	}

	public bool StartEmote(string emote, bool oneshot = true)
	{
		if (!CanMove() || InAttack() || IsDrawingBow() || IsAttached() || IsAttachedToShip())
		{
			return false;
		}
		SetCrouch(crouch: false);
		int @int = m_nview.GetZDO().GetInt(ZDOVars.s_emoteID);
		m_nview.GetZDO().Set(ZDOVars.s_emoteID, @int + 1);
		m_nview.GetZDO().Set(ZDOVars.s_emote, emote);
		m_nview.GetZDO().Set(ZDOVars.s_emoteOneshot, oneshot);
		return true;
	}

	protected override void StopEmote()
	{
		if (m_nview.GetZDO().GetString(ZDOVars.s_emote) != "")
		{
			int @int = m_nview.GetZDO().GetInt(ZDOVars.s_emoteID);
			m_nview.GetZDO().Set(ZDOVars.s_emoteID, @int + 1);
			m_nview.GetZDO().Set(ZDOVars.s_emote, "");
		}
	}

	private void UpdateEmote()
	{
		if (m_nview.IsOwner() && InEmote() && m_moveDir != Vector3.zero)
		{
			StopEmote();
		}
		int @int = m_nview.GetZDO().GetInt(ZDOVars.s_emoteID);
		if (@int == m_emoteID)
		{
			return;
		}
		m_emoteID = @int;
		if (!string.IsNullOrEmpty(m_emoteState))
		{
			m_animator.SetBool("emote_" + m_emoteState, value: false);
		}
		m_emoteState = "";
		m_animator.SetTrigger("emote_stop");
		string @string = m_nview.GetZDO().GetString(ZDOVars.s_emote);
		if (!string.IsNullOrEmpty(@string))
		{
			bool @bool = m_nview.GetZDO().GetBool(ZDOVars.s_emoteOneshot);
			m_animator.ResetTrigger("emote_stop");
			if (@bool)
			{
				m_animator.SetTrigger("emote_" + @string);
				return;
			}
			m_emoteState = @string;
			m_animator.SetBool("emote_" + @string, value: true);
		}
	}

	public override bool InEmote()
	{
		if (string.IsNullOrEmpty(m_emoteState))
		{
			return GetCurrentAnimHash() == s_animatorTagEmote;
		}
		return true;
	}

	public override bool IsCrouching()
	{
		return GetCurrentAnimHash() == s_animatorTagCrouch;
	}

	private void UpdateCrouch(float dt)
	{
		if (m_crouchToggled)
		{
			if (!HaveStamina() || IsSwimming() || InBed() || InPlaceMode() || m_run || IsBlocking() || IsFlying())
			{
				SetCrouch(crouch: false);
			}
			bool flag = InAttack() || IsDrawingBow();
			m_zanim.SetBool(s_crouching, m_crouchToggled && !flag);
		}
		else
		{
			m_zanim.SetBool(s_crouching, value: false);
		}
	}

	protected override void SetCrouch(bool crouch)
	{
		m_crouchToggled = crouch;
	}

	public void SetGuardianPower(string name)
	{
		m_guardianPower = name;
		m_guardianPowerHash = ((!string.IsNullOrEmpty(name)) ? name.GetStableHashCode() : 0);
		m_guardianSE = ObjectDB.instance.GetStatusEffect(m_guardianPowerHash);
		if ((bool)ZoneSystem.instance)
		{
			AddUniqueKey(name);
			Game.instance.IncrementPlayerStat(PlayerStatType.SetGuardianPower);
			switch (name)
			{
			case "GP_Eikthyr":
				Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerEikthyr);
				break;
			case "GP_TheElder":
				Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerElder);
				break;
			case "GP_Bonemass":
				Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerBonemass);
				break;
			case "GP_Moder":
				Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerModer);
				break;
			case "GP_Yagluth":
				Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerYagluth);
				break;
			case "GP_Queen":
				Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerQueen);
				break;
			case "GP_Ashlands":
				Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerAshlands);
				break;
			case "GP_DeepNorth":
				Game.instance.IncrementPlayerStat(PlayerStatType.SetPowerDeepNorth);
				break;
			default:
				ZLog.LogWarning("Missing stat for guardian power: " + name);
				break;
			}
		}
	}

	public string GetGuardianPowerName()
	{
		return m_guardianPower;
	}

	public void GetGuardianPowerHUD(out StatusEffect se, out float cooldown)
	{
		se = m_guardianSE;
		cooldown = m_guardianPowerCooldown;
	}

	public bool StartGuardianPower()
	{
		if (m_guardianSE == null)
		{
			return false;
		}
		if ((InAttack() && !HaveQueuedChain()) || InDodge() || !CanMove() || IsKnockedBack() || IsStaggering() || InMinorAction())
		{
			return false;
		}
		if (m_guardianPowerCooldown > 0f)
		{
			Message(MessageHud.MessageType.Center, "$hud_powernotready");
			return false;
		}
		m_zanim.SetTrigger("gpower");
		Game.instance.IncrementPlayerStat(PlayerStatType.UseGuardianPower);
		string prefabName = Utils.GetPrefabName(m_guardianSE.name);
		switch (prefabName)
		{
		case "GP_Eikthyr":
			Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerEikthyr);
			break;
		case "GP_TheElder":
			Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerElder);
			break;
		case "GP_Bonemass":
			Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerBonemass);
			break;
		case "GP_Moder":
			Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerModer);
			break;
		case "GP_Yagluth":
			Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerYagluth);
			break;
		case "GP_Queen":
			Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerQueen);
			break;
		case "GP_Ashlands":
			Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerAshlands);
			break;
		case "GP_DeepNorth":
			Game.instance.IncrementPlayerStat(PlayerStatType.UsePowerDeepNorth);
			break;
		default:
			ZLog.LogWarning("Missing stat for guardian power: " + prefabName);
			break;
		}
		return true;
	}

	public bool ActivateGuardianPower()
	{
		if (m_guardianPowerCooldown > 0f)
		{
			return false;
		}
		if (m_guardianSE == null)
		{
			return false;
		}
		List<Player> list = new List<Player>();
		GetPlayersInRange(base.transform.position, 10f, list);
		foreach (Player item in list)
		{
			item.GetSEMan().AddStatusEffect(m_guardianSE.NameHash(), resetTime: true);
		}
		m_guardianPowerCooldown = m_guardianSE.m_cooldown;
		return false;
	}

	private void UpdateGuardianPower(float dt)
	{
		m_guardianPowerCooldown -= dt;
		if (m_guardianPowerCooldown < 0f)
		{
			m_guardianPowerCooldown = 0f;
		}
	}

	public override void AttachStart(Transform attachPoint, GameObject colliderRoot, bool hideWeapons, bool isBed, bool onShip, string attachAnimation, Vector3 detachOffset, Transform cameraPos = null)
	{
		if (m_attached)
		{
			return;
		}
		m_attached = true;
		m_attachedToShip = onShip;
		m_attachPoint = attachPoint;
		m_detachOffset = detachOffset;
		m_attachAnimation = attachAnimation;
		m_attachPointCamera = cameraPos;
		m_zanim.SetBool(attachAnimation, value: true);
		m_nview.GetZDO().Set(ZDOVars.s_inBed, isBed);
		if (colliderRoot != null)
		{
			m_attachColliders = colliderRoot.GetComponentsInChildren<Collider>();
			ZLog.Log("Ignoring " + m_attachColliders.Length + " colliders");
			Collider[] attachColliders = m_attachColliders;
			foreach (Collider collider in attachColliders)
			{
				Physics.IgnoreCollision(m_collider, collider, ignore: true);
			}
		}
		if (hideWeapons)
		{
			HideHandItems();
		}
		UpdateAttach();
		ResetCloth();
	}

	private void UpdateAttach()
	{
		if (m_attached)
		{
			if (m_attachPoint != null)
			{
				base.transform.position = m_attachPoint.position;
				base.transform.rotation = m_attachPoint.rotation;
				Rigidbody componentInParent = m_attachPoint.GetComponentInParent<Rigidbody>();
				m_body.useGravity = false;
				m_body.velocity = (componentInParent ? componentInParent.GetPointVelocity(base.transform.position) : Vector3.zero);
				m_body.angularVelocity = Vector3.zero;
				m_maxAirAltitude = base.transform.position.y;
			}
			else
			{
				AttachStop();
			}
		}
	}

	public override bool IsAttached()
	{
		if (!m_attached)
		{
			return base.IsAttached();
		}
		return true;
	}

	public Transform GetAttachPoint()
	{
		return m_attachPoint;
	}

	public Transform GetAttachCameraPoint()
	{
		return m_attachPointCamera;
	}

	public void ResetAttachCameraPoint()
	{
		m_attachPointCamera = null;
	}

	public override bool IsAttachedToShip()
	{
		if (m_attached)
		{
			return m_attachedToShip;
		}
		return false;
	}

	public override bool IsRiding()
	{
		if (m_doodadController != null && m_doodadController.IsValid())
		{
			return m_doodadController is Sadle;
		}
		return false;
	}

	public override bool InBed()
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetBool(ZDOVars.s_inBed);
	}

	public override void AttachStop()
	{
		if (m_sleeping || !m_attached)
		{
			return;
		}
		if (m_attachPoint != null)
		{
			base.transform.position = m_attachPoint.TransformPoint(m_detachOffset);
		}
		if (m_attachColliders != null)
		{
			Collider[] attachColliders = m_attachColliders;
			foreach (Collider collider in attachColliders)
			{
				if ((bool)collider)
				{
					Physics.IgnoreCollision(m_collider, collider, ignore: false);
				}
			}
			m_attachColliders = null;
		}
		m_body.useGravity = true;
		m_attached = false;
		m_attachPoint = null;
		m_attachPointCamera = null;
		m_zanim.SetBool(m_attachAnimation, value: false);
		m_nview.GetZDO().Set(ZDOVars.s_inBed, value: false);
		ResetCloth();
	}

	public void StartDoodadControl(IDoodadController shipControl)
	{
		m_doodadController = shipControl;
		ZLog.Log("Doodad controlls set " + shipControl.GetControlledComponent().gameObject.name);
	}

	public void StopDoodadControl()
	{
		if (m_doodadController != null)
		{
			if (m_doodadController.IsValid())
			{
				m_doodadController.OnUseStop(this);
			}
			ZLog.Log("Stop doodad controlls");
			m_doodadController = null;
		}
	}

	private void SetDoodadControlls(ref Vector3 moveDir, ref Vector3 lookDir, ref bool run, ref bool autoRun, bool block)
	{
		if (m_doodadController.IsValid())
		{
			m_doodadController.ApplyControlls(moveDir, lookDir, run, autoRun, block);
		}
		moveDir = Vector3.zero;
		autoRun = false;
		run = false;
	}

	public Ship GetControlledShip()
	{
		if (m_doodadController != null && m_doodadController.IsValid())
		{
			return m_doodadController.GetControlledComponent() as Ship;
		}
		return null;
	}

	public IDoodadController GetDoodadController()
	{
		return m_doodadController;
	}

	private void UpdateDoodadControls(float dt)
	{
		if (m_doodadController == null)
		{
			return;
		}
		if (!m_doodadController.IsValid())
		{
			StopDoodadControl();
			return;
		}
		Vector3 forward = m_doodadController.GetControlledComponent().transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Quaternion to = Quaternion.LookRotation(forward);
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, 100f * dt);
		if (Vector3.Distance(m_doodadController.GetPosition(), base.transform.position) > m_maxInteractDistance)
		{
			StopDoodadControl();
		}
	}

	public bool IsSleeping()
	{
		return m_sleeping;
	}

	public void SetSleeping(bool sleep)
	{
		if (m_sleeping != sleep)
		{
			m_sleeping = sleep;
			if (!sleep)
			{
				Message(MessageHud.MessageType.Center, "$msg_goodmorning");
				m_seman.AddStatusEffect(SEMan.s_statusEffectRested, resetTime: true);
				m_wakeupTime = ZNet.instance.GetTimeSeconds();
				Game.instance.IncrementPlayerStat(PlayerStatType.Sleep);
			}
		}
	}

	public void SetControls(Vector3 movedir, bool attack, bool attackHold, bool secondaryAttack, bool secondaryAttackHold, bool block, bool blockHold, bool jump, bool crouch, bool run, bool autoRun, bool dodge = false)
	{
		if ((IsAttached() || InEmote()) && (movedir != Vector3.zero || attack || secondaryAttack || block || blockHold || jump || crouch) && GetDoodadController() == null)
		{
			attack = false;
			attackHold = false;
			secondaryAttack = false;
			secondaryAttackHold = false;
			StopEmote();
			AttachStop();
		}
		if (m_doodadController != null)
		{
			SetDoodadControlls(ref movedir, ref m_lookDir, ref run, ref autoRun, blockHold);
			if (jump || attack || secondaryAttack || dodge)
			{
				attack = false;
				attackHold = false;
				secondaryAttack = false;
				secondaryAttackHold = false;
				StopDoodadControl();
			}
		}
		if (run)
		{
			m_walk = false;
		}
		if (!m_autoRun)
		{
			Vector3 lookDir = m_lookDir;
			lookDir.y = 0f;
			lookDir.Normalize();
			m_moveDir = movedir.z * lookDir + movedir.x * Vector3.Cross(Vector3.up, lookDir);
		}
		if (!m_autoRun && autoRun && !InPlaceMode())
		{
			m_autoRun = true;
			SetCrouch(crouch: false);
			m_moveDir = m_lookDir;
			m_moveDir.y = 0f;
			m_moveDir.Normalize();
		}
		else if (m_autoRun)
		{
			if (attack || jump || dodge || crouch || movedir != Vector3.zero || InPlaceMode() || attackHold || secondaryAttackHold)
			{
				m_autoRun = false;
			}
			else if (autoRun || blockHold)
			{
				m_moveDir = m_lookDir;
				m_moveDir.y = 0f;
				m_moveDir.Normalize();
				blockHold = false;
				block = false;
			}
		}
		m_attack = attack;
		m_attackHold = attackHold;
		m_secondaryAttack = secondaryAttack;
		m_secondaryAttackHold = secondaryAttackHold;
		m_blocking = blockHold;
		m_run = run;
		if (crouch)
		{
			SetCrouch(!m_crouchToggled);
		}
		if (ZInput.InputLayout == InputLayout.Default || !ZInput.IsGamepadActive())
		{
			if (!jump)
			{
				return;
			}
			if (m_blocking)
			{
				Vector3 dodgeDir = m_moveDir;
				if (dodgeDir.magnitude < 0.1f)
				{
					dodgeDir = -m_lookDir;
					dodgeDir.y = 0f;
					dodgeDir.Normalize();
				}
				Dodge(dodgeDir);
			}
			else if (IsCrouching() || m_crouchToggled)
			{
				Vector3 dodgeDir2 = m_moveDir;
				if (dodgeDir2.magnitude < 0.1f)
				{
					dodgeDir2 = m_lookDir;
					dodgeDir2.y = 0f;
					dodgeDir2.Normalize();
				}
				Dodge(dodgeDir2);
			}
			else
			{
				Jump();
			}
		}
		else
		{
			if (!ZInput.IsNonClassicFunctionality())
			{
				return;
			}
			if (dodge)
			{
				if (m_blocking)
				{
					Vector3 dodgeDir3 = m_moveDir;
					if (dodgeDir3.magnitude < 0.1f)
					{
						dodgeDir3 = -m_lookDir;
						dodgeDir3.y = 0f;
						dodgeDir3.Normalize();
					}
					Dodge(dodgeDir3);
				}
				else if (IsCrouching() || m_crouchToggled)
				{
					Vector3 dodgeDir4 = m_moveDir;
					if (dodgeDir4.magnitude < 0.1f)
					{
						dodgeDir4 = m_lookDir;
						dodgeDir4.y = 0f;
						dodgeDir4.Normalize();
					}
					Dodge(dodgeDir4);
				}
			}
			if (jump)
			{
				Jump();
			}
		}
	}

	private void UpdateTargeted(float dt)
	{
		m_timeSinceTargeted += dt;
		m_timeSinceSensed += dt;
	}

	public override void OnTargeted(bool sensed, bool alerted)
	{
		if (sensed)
		{
			if (m_timeSinceSensed > 0.5f)
			{
				m_timeSinceSensed = 0f;
				m_nview.InvokeRPC("OnTargeted", sensed, alerted);
			}
		}
		else if (m_timeSinceTargeted > 0.5f)
		{
			m_timeSinceTargeted = 0f;
			m_nview.InvokeRPC("OnTargeted", sensed, alerted);
		}
	}

	private void RPC_OnTargeted(long sender, bool sensed, bool alerted)
	{
		m_timeSinceTargeted = 0f;
		if (sensed)
		{
			m_timeSinceSensed = 0f;
		}
		if (alerted)
		{
			MusicMan.instance.ResetCombatTimer();
		}
	}

	protected override void OnDamaged(HitData hit)
	{
		base.OnDamaged(hit);
		if (hit.GetTotalDamage() > GetMaxHealth() / 10f)
		{
			Hud.instance.DamageFlash();
		}
	}

	public bool IsTargeted()
	{
		return m_timeSinceTargeted < 1f;
	}

	public bool IsSensed()
	{
		return m_timeSinceSensed < 1f;
	}

	protected override void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
	{
		if (m_chestItem != null)
		{
			mods.Apply(m_chestItem.m_shared.m_damageModifiers);
		}
		if (m_legItem != null)
		{
			mods.Apply(m_legItem.m_shared.m_damageModifiers);
		}
		if (m_helmetItem != null)
		{
			mods.Apply(m_helmetItem.m_shared.m_damageModifiers);
		}
		if (m_shoulderItem != null)
		{
			mods.Apply(m_shoulderItem.m_shared.m_damageModifiers);
		}
	}

	public override float GetBodyArmor()
	{
		float num = 0f;
		if (m_chestItem != null)
		{
			num += m_chestItem.GetArmor();
		}
		if (m_legItem != null)
		{
			num += m_legItem.GetArmor();
		}
		if (m_helmetItem != null)
		{
			num += m_helmetItem.GetArmor();
		}
		if (m_shoulderItem != null)
		{
			num += m_shoulderItem.GetArmor();
		}
		return num;
	}

	protected override void OnSneaking(float dt)
	{
		float t = Mathf.Pow(m_skills.GetSkillFactor(Skills.SkillType.Sneak), 0.5f);
		float num = Mathf.Lerp(1f, 0.25f, t);
		float num2 = dt * m_sneakStaminaDrain * num;
		num2 += num2 * GetEquipmentSneakStaminaModifier();
		m_seman.ModifySneakStaminaUsage(num2, ref num2);
		UseStamina(num2);
		if (!HaveStamina())
		{
			Hud.instance.StaminaBarEmptyFlash();
		}
		m_sneakSkillImproveTimer += dt;
		if (m_sneakSkillImproveTimer > 1f)
		{
			m_sneakSkillImproveTimer = 0f;
			if (BaseAI.InStealthRange(this))
			{
				RaiseSkill(Skills.SkillType.Sneak);
			}
			else
			{
				RaiseSkill(Skills.SkillType.Sneak, 0.1f);
			}
		}
	}

	private void UpdateStealth(float dt)
	{
		m_stealthFactorUpdateTimer += dt;
		if (m_stealthFactorUpdateTimer > 0.5f)
		{
			m_stealthFactorUpdateTimer = 0f;
			m_stealthFactorTarget = 0f;
			if (IsCrouching())
			{
				m_lastStealthPosition = base.transform.position;
				float skillFactor = m_skills.GetSkillFactor(Skills.SkillType.Sneak);
				float lightFactor = StealthSystem.instance.GetLightFactor(GetCenterPoint());
				m_stealthFactorTarget = Mathf.Lerp(0.5f + lightFactor * 0.5f, 0.2f + lightFactor * 0.4f, skillFactor);
				m_stealthFactorTarget = Mathf.Clamp01(m_stealthFactorTarget);
				m_seman.ModifyStealth(m_stealthFactorTarget, ref m_stealthFactorTarget);
				m_stealthFactorTarget = Mathf.Clamp01(m_stealthFactorTarget);
			}
			else
			{
				m_stealthFactorTarget = 1f;
			}
		}
		m_stealthFactor = Mathf.MoveTowards(m_stealthFactor, m_stealthFactorTarget, dt / 4f);
		m_nview.GetZDO().Set(ZDOVars.s_stealth, m_stealthFactor);
	}

	public override float GetStealthFactor()
	{
		if (!m_nview.IsValid())
		{
			return 0f;
		}
		if (m_nview.IsOwner())
		{
			return m_stealthFactor;
		}
		return m_nview.GetZDO().GetFloat(ZDOVars.s_stealth);
	}

	public override bool InAttack()
	{
		if (MonoUpdaters.UpdateCount == m_cachedFrame)
		{
			return m_cachedAttack;
		}
		m_cachedFrame = MonoUpdaters.UpdateCount;
		if (GetNextOrCurrentAnimHash() == Humanoid.s_animatorTagAttack)
		{
			m_cachedAttack = true;
			return true;
		}
		for (int i = 1; i < m_animator.layerCount; i++)
		{
			if ((m_animator.IsInTransition(i) ? m_animator.GetNextAnimatorStateInfo(i).tagHash : m_animator.GetCurrentAnimatorStateInfo(i).tagHash) == Humanoid.s_animatorTagAttack)
			{
				m_cachedAttack = true;
				return true;
			}
		}
		m_cachedAttack = false;
		return false;
	}

	private float GetEquipmentModifier(int index)
	{
		if (m_equipmentModifierValues != null)
		{
			return m_equipmentModifierValues[index];
		}
		return 0f;
	}

	public override float GetEquipmentMovementModifier()
	{
		return GetEquipmentModifier(0);
	}

	public override float GetEquipmentHomeItemModifier()
	{
		return GetEquipmentModifier(1);
	}

	public override float GetEquipmentHeatResistanceModifier()
	{
		return GetEquipmentModifier(2);
	}

	public override float GetEquipmentJumpStaminaModifier()
	{
		return GetEquipmentModifier(3);
	}

	public override float GetEquipmentAttackStaminaModifier()
	{
		return GetEquipmentModifier(4);
	}

	public override float GetEquipmentBlockStaminaModifier()
	{
		return GetEquipmentModifier(5);
	}

	public override float GetEquipmentDodgeStaminaModifier()
	{
		return GetEquipmentModifier(6);
	}

	public override float GetEquipmentSwimStaminaModifier()
	{
		return GetEquipmentModifier(7);
	}

	public override float GetEquipmentSneakStaminaModifier()
	{
		return GetEquipmentModifier(8);
	}

	public override float GetEquipmentRunStaminaModifier()
	{
		return GetEquipmentModifier(9);
	}

	private float GetEquipmentModifierPlusSE(int index)
	{
		float staminaUse = m_equipmentModifierValues[index];
		switch (index)
		{
		case 3:
			m_seman.ModifyJumpStaminaUsage(1f, ref staminaUse, minZero: false);
			break;
		case 4:
			m_seman.ModifyAttackStaminaUsage(1f, ref staminaUse, minZero: false);
			break;
		case 5:
			m_seman.ModifyBlockStaminaUsage(1f, ref staminaUse, minZero: false);
			break;
		case 6:
			m_seman.ModifyDodgeStaminaUsage(1f, ref staminaUse, minZero: false);
			break;
		case 7:
			m_seman.ModifySwimStaminaUsage(1f, ref staminaUse, minZero: false);
			break;
		case 8:
			m_seman.ModifySneakStaminaUsage(1f, ref staminaUse, minZero: false);
			break;
		case 9:
			m_seman.ModifyRunStaminaDrain(1f, ref staminaUse, minZero: false);
			break;
		}
		return staminaUse;
	}

	protected override float GetJogSpeedFactor()
	{
		return 1f + GetEquipmentMovementModifier();
	}

	protected override float GetRunSpeedFactor()
	{
		float skillFactor = m_skills.GetSkillFactor(Skills.SkillType.Run);
		return (1f + skillFactor * 0.25f) * (1f + GetEquipmentMovementModifier() * 1.5f);
	}

	public override bool InMinorAction()
	{
		int tagHash = m_animator.GetCurrentAnimatorStateInfo(1).tagHash;
		if (tagHash == s_animatorTagMinorAction || tagHash == s_animatorTagMinorActionFast)
		{
			return true;
		}
		if (m_animator.IsInTransition(1))
		{
			int tagHash2 = m_animator.GetNextAnimatorStateInfo(1).tagHash;
			if (tagHash2 != s_animatorTagMinorAction)
			{
				return tagHash2 == s_animatorTagMinorActionFast;
			}
			return true;
		}
		return false;
	}

	public override bool InMinorActionSlowdown()
	{
		if (m_animator.GetCurrentAnimatorStateInfo(1).tagHash == s_animatorTagMinorAction)
		{
			return true;
		}
		if (m_animator.IsInTransition(1))
		{
			return m_animator.GetNextAnimatorStateInfo(1).tagHash == s_animatorTagMinorAction;
		}
		return false;
	}

	public override bool GetRelativePosition(out ZDOID parent, out string attachJoint, out Vector3 relativePos, out Quaternion relativeRot, out Vector3 relativeVel)
	{
		if (m_attached && (bool)m_attachPoint)
		{
			ZNetView componentInParent = m_attachPoint.GetComponentInParent<ZNetView>();
			if ((bool)componentInParent && componentInParent.IsValid())
			{
				parent = componentInParent.GetZDO().m_uid;
				if (componentInParent.GetComponent<Character>() != null)
				{
					attachJoint = m_attachPoint.name;
					relativePos = Vector3.zero;
					relativeRot = Quaternion.identity;
				}
				else
				{
					attachJoint = "";
					relativePos = componentInParent.transform.InverseTransformPoint(base.transform.position);
					relativeRot = Quaternion.Inverse(componentInParent.transform.rotation) * base.transform.rotation;
				}
				relativeVel = Vector3.zero;
				return true;
			}
		}
		return base.GetRelativePosition(out parent, out attachJoint, out relativePos, out relativeRot, out relativeVel);
	}

	public override Skills GetSkills()
	{
		return m_skills;
	}

	public override float GetRandomSkillFactor(Skills.SkillType skill)
	{
		return m_skills.GetRandomSkillFactor(skill);
	}

	public override float GetSkillFactor(Skills.SkillType skill)
	{
		return m_skills.GetSkillFactor(skill);
	}

	protected override void DoDamageCameraShake(HitData hit)
	{
		float totalStaggerDamage = hit.m_damage.GetTotalStaggerDamage();
		if ((bool)GameCamera.instance && totalStaggerDamage > 0f)
		{
			float num = Mathf.Clamp01(totalStaggerDamage / GetMaxHealth());
			GameCamera.instance.AddShake(base.transform.position, 50f, m_baseCameraShake * num, continous: false);
		}
	}

	protected override void DamageArmorDurability(HitData hit)
	{
		List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
		if (m_chestItem != null)
		{
			list.Add(m_chestItem);
		}
		if (m_legItem != null)
		{
			list.Add(m_legItem);
		}
		if (m_helmetItem != null)
		{
			list.Add(m_helmetItem);
		}
		if (m_shoulderItem != null)
		{
			list.Add(m_shoulderItem);
		}
		if (list.Count != 0)
		{
			float num = hit.GetTotalPhysicalDamage() + hit.GetTotalElementalDamage();
			if (!(num <= 0f))
			{
				int index = UnityEngine.Random.Range(0, list.Count);
				ItemDrop.ItemData itemData = list[index];
				itemData.m_durability = Mathf.Max(0f, itemData.m_durability - num);
			}
		}
	}

	protected override bool ToggleEquipped(ItemDrop.ItemData item)
	{
		if (!item.IsEquipable())
		{
			return false;
		}
		if (InAttack())
		{
			return true;
		}
		if (item.m_shared.m_equipDuration <= 0f)
		{
			if (IsItemEquiped(item))
			{
				UnequipItem(item);
			}
			else
			{
				EquipItem(item);
			}
		}
		else if (IsItemEquiped(item))
		{
			QueueUnequipAction(item);
		}
		else
		{
			QueueEquipAction(item);
		}
		return true;
	}

	public void GetActionProgress(out string name, out float progress)
	{
		if (m_actionQueue.Count > 0)
		{
			MinorActionData minorActionData = m_actionQueue[0];
			if (minorActionData.m_duration > 0.5f)
			{
				float num = Mathf.Clamp01(minorActionData.m_time / minorActionData.m_duration);
				if (num > 0f)
				{
					name = minorActionData.m_progressText;
					progress = num;
					return;
				}
			}
		}
		name = null;
		progress = 0f;
	}

	private void UpdateActionQueue(float dt)
	{
		if (m_actionQueuePause > 0f)
		{
			m_actionQueuePause -= dt;
			if (m_actionAnimation != null)
			{
				m_zanim.SetBool(m_actionAnimation, value: false);
				m_actionAnimation = null;
			}
			return;
		}
		if (InAttack())
		{
			if (m_actionAnimation != null)
			{
				m_zanim.SetBool(m_actionAnimation, value: false);
				m_actionAnimation = null;
			}
			return;
		}
		if (m_actionQueue.Count == 0)
		{
			if (m_actionAnimation != null)
			{
				m_zanim.SetBool(m_actionAnimation, value: false);
				m_actionAnimation = null;
			}
			return;
		}
		MinorActionData minorActionData = m_actionQueue[0];
		if (m_actionAnimation != null && m_actionAnimation != minorActionData.m_animation)
		{
			m_zanim.SetBool(m_actionAnimation, value: false);
			m_actionAnimation = null;
		}
		m_zanim.SetBool(minorActionData.m_animation, value: true);
		m_actionAnimation = minorActionData.m_animation;
		if (minorActionData.m_time == 0f && minorActionData.m_startEffect != null)
		{
			minorActionData.m_startEffect.Create(base.transform.position, Quaternion.identity);
		}
		if (minorActionData.m_staminaDrain > 0f)
		{
			UseStamina(minorActionData.m_staminaDrain * dt);
		}
		if (minorActionData.m_eitrDrain > 0f)
		{
			UseEitr(minorActionData.m_eitrDrain * dt);
		}
		minorActionData.m_time += dt;
		if (minorActionData.m_time > minorActionData.m_duration)
		{
			m_actionQueue.RemoveAt(0);
			m_zanim.SetBool(m_actionAnimation, value: false);
			m_actionAnimation = null;
			if (!string.IsNullOrEmpty(minorActionData.m_doneAnimation))
			{
				m_zanim.SetTrigger(minorActionData.m_doneAnimation);
			}
			switch (minorActionData.m_type)
			{
			case MinorActionData.ActionType.Equip:
				EquipItem(minorActionData.m_item);
				break;
			case MinorActionData.ActionType.Unequip:
				UnequipItem(minorActionData.m_item);
				break;
			case MinorActionData.ActionType.Reload:
				SetWeaponLoaded(minorActionData.m_item);
				break;
			}
			m_actionQueuePause = 0.3f;
		}
	}

	private void QueueEquipAction(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		if (IsEquipActionQueued(item))
		{
			RemoveEquipAction(item);
			return;
		}
		CancelReloadAction();
		MinorActionData minorActionData = new MinorActionData();
		minorActionData.m_item = item;
		minorActionData.m_type = MinorActionData.ActionType.Equip;
		minorActionData.m_duration = item.m_shared.m_equipDuration;
		minorActionData.m_progressText = "$hud_equipping " + item.m_shared.m_name;
		minorActionData.m_animation = "equipping";
		if (minorActionData.m_duration >= 1f)
		{
			minorActionData.m_startEffect = m_equipStartEffects;
		}
		m_actionQueue.Add(minorActionData);
	}

	private void QueueUnequipAction(ItemDrop.ItemData item)
	{
		if (item != null)
		{
			if (IsEquipActionQueued(item))
			{
				RemoveEquipAction(item);
				return;
			}
			CancelReloadAction();
			MinorActionData minorActionData = new MinorActionData();
			minorActionData.m_item = item;
			minorActionData.m_type = MinorActionData.ActionType.Unequip;
			minorActionData.m_duration = item.m_shared.m_equipDuration;
			minorActionData.m_progressText = "$hud_unequipping " + item.m_shared.m_name;
			minorActionData.m_animation = "equipping";
			m_actionQueue.Add(minorActionData);
		}
	}

	private void QueueReloadAction()
	{
		if (!IsReloadActionQueued())
		{
			ItemDrop.ItemData currentWeapon = GetCurrentWeapon();
			if (currentWeapon != null && currentWeapon.m_shared.m_attack.m_requiresReload)
			{
				MinorActionData minorActionData = new MinorActionData();
				minorActionData.m_item = currentWeapon;
				minorActionData.m_type = MinorActionData.ActionType.Reload;
				minorActionData.m_duration = currentWeapon.GetWeaponLoadingTime();
				minorActionData.m_progressText = "$hud_reloading " + currentWeapon.m_shared.m_name;
				minorActionData.m_animation = currentWeapon.m_shared.m_attack.m_reloadAnimation;
				minorActionData.m_doneAnimation = currentWeapon.m_shared.m_attack.m_reloadAnimation + "_done";
				minorActionData.m_staminaDrain = currentWeapon.m_shared.m_attack.m_reloadStaminaDrain;
				minorActionData.m_eitrDrain = currentWeapon.m_shared.m_attack.m_reloadEitrDrain;
				m_actionQueue.Add(minorActionData);
			}
		}
	}

	protected override void ClearActionQueue()
	{
		m_actionQueue.Clear();
	}

	public override void RemoveEquipAction(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return;
		}
		foreach (MinorActionData item2 in m_actionQueue)
		{
			if (item2.m_item == item)
			{
				m_actionQueue.Remove(item2);
				break;
			}
		}
	}

	public bool IsEquipActionQueued(ItemDrop.ItemData item)
	{
		if (item == null)
		{
			return false;
		}
		foreach (MinorActionData item2 in m_actionQueue)
		{
			if ((item2.m_type == MinorActionData.ActionType.Equip || item2.m_type == MinorActionData.ActionType.Unequip) && item2.m_item == item)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsReloadActionQueued()
	{
		foreach (MinorActionData item in m_actionQueue)
		{
			if (item.m_type == MinorActionData.ActionType.Reload)
			{
				return true;
			}
		}
		return false;
	}

	public void ResetCharacter()
	{
		m_guardianPowerCooldown = 0f;
		ResetSeenTutorials();
		m_knownRecipes.Clear();
		m_knownStations.Clear();
		m_knownMaterial.Clear();
		m_uniques.Clear();
		m_trophies.Clear();
		m_skills.Clear();
		m_knownBiome.Clear();
		m_knownTexts.Clear();
	}

	public void ResetCharacterKnownItems()
	{
		m_knownRecipes.Clear();
		m_knownStations.Clear();
		m_knownMaterial.Clear();
		m_trophies.Clear();
	}

	public bool ToggleDebugFly()
	{
		m_debugFly = !m_debugFly;
		m_nview.GetZDO().Set(ZDOVars.s_debugFly, m_debugFly);
		Message(MessageHud.MessageType.TopLeft, "Debug fly:" + m_debugFly);
		return m_debugFly;
	}

	public void SetNoPlacementCost(bool value)
	{
		if (value != m_noPlacementCost)
		{
			ToggleNoPlacementCost();
		}
	}

	public bool ToggleNoPlacementCost()
	{
		m_noPlacementCost = !m_noPlacementCost;
		Message(MessageHud.MessageType.TopLeft, "No placement cost:" + m_noPlacementCost);
		UpdateAvailablePiecesList();
		return m_noPlacementCost;
	}

	public bool IsKnownMaterial(string name)
	{
		return m_knownMaterial.Contains(name);
	}
}
