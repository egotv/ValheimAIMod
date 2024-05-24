using UnityEngine;

public class Sadle : MonoBehaviour, Interactable, Hoverable, IDoodadController
{
	private enum Speed
	{
		Stop,
		Walk,
		Run,
		Turn,
		NoChange
	}

	public string m_hoverText = "";

	public float m_maxUseRange = 10f;

	public Transform m_attachPoint;

	public Vector3 m_detachOffset = new Vector3(0f, 0.5f, 0f);

	public string m_attachAnimation = "attach_chair";

	public float m_maxStamina = 100f;

	public float m_runStaminaDrain = 10f;

	public float m_swimStaminaDrain = 10f;

	public float m_staminaRegen = 10f;

	public float m_staminaRegenHungry = 10f;

	public EffectList m_drownEffects = new EffectList();

	public Sprite m_mountIcon;

	private const float m_staminaRegenDelay = 1f;

	private Vector3 m_controlDir;

	private Speed m_speed;

	private float m_rideSkill;

	private float m_staminaRegenTimer;

	private float m_drownDamageTimer;

	private float m_raiseSkillTimer;

	private Character m_character;

	private ZNetView m_nview;

	private Tameable m_tambable;

	private MonsterAI m_monsterAI;

	private bool m_haveValidUser;

	private void Awake()
	{
		m_character = base.gameObject.GetComponentInParent<Character>();
		m_nview = m_character.GetComponent<ZNetView>();
		m_tambable = m_character.GetComponent<Tameable>();
		m_monsterAI = m_character.GetComponent<MonsterAI>();
		m_nview.Register<long>("RequestControl", RPC_RequestControl);
		m_nview.Register<long>("ReleaseControl", RPC_ReleaseControl);
		m_nview.Register<bool>("RequestRespons", RPC_RequestRespons);
		m_nview.Register<Vector3>("RemoveSaddle", RPC_RemoveSaddle);
		m_nview.Register<Vector3, int, float>("Controls", RPC_Controls);
	}

	public bool IsValid()
	{
		return this;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	private void FixedUpdate()
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		CalculateHaveValidUser();
		if (m_character.IsTamed())
		{
			if (IsLocalUser())
			{
				UpdateRidingSkill(Time.fixedDeltaTime);
			}
			if (m_nview.IsOwner())
			{
				float fixedDeltaTime = Time.fixedDeltaTime;
				UpdateStamina(fixedDeltaTime);
				UpdateDrown(fixedDeltaTime);
			}
		}
	}

	private void UpdateDrown(float dt)
	{
		if (m_character.IsSwimming() && !m_character.IsOnGround() && !HaveStamina())
		{
			m_drownDamageTimer += dt;
			if (m_drownDamageTimer > 1f)
			{
				m_drownDamageTimer = 0f;
				float damage = Mathf.Ceil(m_character.GetMaxHealth() / 20f);
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = damage;
				hitData.m_point = m_character.GetCenterPoint();
				hitData.m_dir = Vector3.down;
				hitData.m_pushForce = 10f;
				hitData.m_hitType = HitData.HitType.Drowning;
				m_character.Damage(hitData);
				Vector3 position = base.transform.position;
				position.y = m_character.GetLiquidLevel();
				m_drownEffects.Create(position, base.transform.rotation);
			}
		}
	}

	public bool UpdateRiding(float dt)
	{
		if (!base.isActiveAndEnabled)
		{
			return false;
		}
		if (!m_character.IsTamed())
		{
			return false;
		}
		if (!HaveValidUser())
		{
			return false;
		}
		if (m_speed == Speed.Stop || m_controlDir.magnitude == 0f)
		{
			return false;
		}
		if (m_speed == Speed.Walk || m_speed == Speed.Run)
		{
			if (m_speed == Speed.Run && !HaveStamina())
			{
				m_speed = Speed.Walk;
			}
			m_monsterAI.MoveTowards(m_controlDir, m_speed == Speed.Run);
			float riderSkill = GetRiderSkill();
			float num = Mathf.Lerp(1f, 0.5f, riderSkill);
			if (m_character.IsSwimming())
			{
				UseStamina(m_swimStaminaDrain * num * dt);
			}
			else if (m_speed == Speed.Run)
			{
				UseStamina(m_runStaminaDrain * num * dt);
			}
		}
		else if (m_speed == Speed.Turn)
		{
			m_monsterAI.StopMoving();
			m_character.SetRun(run: false);
			m_monsterAI.LookTowards(m_controlDir);
		}
		m_monsterAI.ResetRandomMovement();
		return true;
	}

	public string GetHoverText()
	{
		if (!InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		string text = Localization.instance.Localize(m_hoverText);
		text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] $piece_use");
		if (ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive())
		{
			return text + Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>] $hud_saddle_remove");
		}
		return text + Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $hud_saddle_remove");
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(m_hoverText);
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (!InUseDistance(character))
		{
			return false;
		}
		if (!m_character.IsTamed())
		{
			return false;
		}
		Player player = character as Player;
		if (player == null)
		{
			return false;
		}
		if (alt)
		{
			m_nview.InvokeRPC("RemoveSaddle", character.transform.position);
			return true;
		}
		m_nview.InvokeRPC("RequestControl", player.GetZDOID().UserID);
		return false;
	}

	public Character GetCharacter()
	{
		return m_character;
	}

	public Tameable GetTameable()
	{
		return m_tambable;
	}

	public void ApplyControlls(Vector3 moveDir, Vector3 lookDir, bool run, bool autoRun, bool block)
	{
		if (!(Player.m_localPlayer == null))
		{
			float skillFactor = Player.m_localPlayer.GetSkills().GetSkillFactor(Skills.SkillType.Ride);
			Speed speed = Speed.NoChange;
			Vector3 vector = Vector3.zero;
			if (block || (double)moveDir.z > 0.5 || run)
			{
				Vector3 vector2 = lookDir;
				vector2.y = 0f;
				vector2.Normalize();
				vector = vector2;
			}
			if (run)
			{
				speed = Speed.Run;
			}
			else if ((double)moveDir.z > 0.5)
			{
				speed = Speed.Walk;
			}
			else if ((double)moveDir.z < -0.5)
			{
				speed = Speed.Stop;
			}
			else if (block)
			{
				speed = Speed.Turn;
			}
			m_nview.InvokeRPC("Controls", vector, (int)speed, skillFactor);
		}
	}

	private void RPC_Controls(long sender, Vector3 rideDir, int rideSpeed, float skill)
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		m_rideSkill = skill;
		if (rideDir != Vector3.zero)
		{
			m_controlDir = rideDir;
		}
		switch (rideSpeed)
		{
		case 4:
			if (m_speed == Speed.Turn)
			{
				m_speed = Speed.Stop;
			}
			return;
		case 3:
			if (m_speed == Speed.Walk || m_speed == Speed.Run)
			{
				return;
			}
			break;
		}
		m_speed = (Speed)rideSpeed;
	}

	private void UpdateRidingSkill(float dt)
	{
		m_raiseSkillTimer += dt;
		if (m_raiseSkillTimer > 1f)
		{
			m_raiseSkillTimer = 0f;
			if (m_speed == Speed.Run)
			{
				Player.m_localPlayer.RaiseSkill(Skills.SkillType.Ride);
			}
		}
	}

	private void ResetControlls()
	{
		m_controlDir = Vector3.zero;
		m_speed = Speed.Stop;
		m_rideSkill = 0f;
	}

	public Component GetControlledComponent()
	{
		return m_character;
	}

	public Vector3 GetPosition()
	{
		return base.transform.position;
	}

	private void RPC_RemoveSaddle(long sender, Vector3 userPoint)
	{
		if (m_nview.IsOwner() && !HaveValidUser())
		{
			m_tambable.DropSaddle(userPoint);
		}
	}

	private void RPC_RequestControl(long sender, long playerID)
	{
		if (m_nview.IsOwner())
		{
			CalculateHaveValidUser();
			if (GetUser() == playerID || !HaveValidUser())
			{
				m_nview.GetZDO().Set(ZDOVars.s_user, playerID);
				ResetControlls();
				m_nview.InvokeRPC(sender, "RequestRespons", true);
				m_nview.GetZDO().SetOwner(sender);
			}
			else
			{
				m_nview.InvokeRPC(sender, "RequestRespons", false);
			}
		}
	}

	public bool HaveValidUser()
	{
		return m_haveValidUser;
	}

	private void CalculateHaveValidUser()
	{
		m_haveValidUser = false;
		long user = GetUser();
		if (user == 0L)
		{
			return;
		}
		foreach (ZDO allCharacterZDO in ZNet.instance.GetAllCharacterZDOS())
		{
			if (allCharacterZDO.m_uid.UserID == user)
			{
				m_haveValidUser = Vector3.Distance(allCharacterZDO.GetPosition(), base.transform.position) < m_maxUseRange;
				break;
			}
		}
	}

	private void RPC_ReleaseControl(long sender, long playerID)
	{
		if (m_nview.IsOwner() && GetUser() == playerID)
		{
			m_nview.GetZDO().Set(ZDOVars.s_user, 0L);
			ResetControlls();
		}
	}

	private void RPC_RequestRespons(long sender, bool granted)
	{
		if (!Player.m_localPlayer)
		{
			return;
		}
		if (granted)
		{
			Player.m_localPlayer.StartDoodadControl(this);
			if (m_attachPoint != null)
			{
				Player.m_localPlayer.AttachStart(m_attachPoint, m_character.gameObject, hideWeapons: false, isBed: false, onShip: false, m_attachAnimation, m_detachOffset);
			}
		}
		else
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_inuse");
		}
	}

	public void OnUseStop(Player player)
	{
		if (m_nview.IsValid())
		{
			m_nview.InvokeRPC("ReleaseControl", player.GetZDOID().UserID);
			if (m_attachPoint != null)
			{
				player.AttachStop();
			}
		}
	}

	private bool IsLocalUser()
	{
		if (!Player.m_localPlayer)
		{
			return false;
		}
		long user = GetUser();
		if (user == 0L)
		{
			return false;
		}
		return user == Player.m_localPlayer.GetZDOID().UserID;
	}

	private long GetUser()
	{
		if (m_nview == null || !m_nview.IsValid())
		{
			return 0L;
		}
		return m_nview.GetZDO().GetLong(ZDOVars.s_user, 0L);
	}

	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, m_attachPoint.position) < m_maxUseRange;
	}

	private void UseStamina(float v)
	{
		if (v != 0f && m_nview.IsValid() && m_nview.IsOwner())
		{
			float stamina = GetStamina();
			stamina -= v;
			if (stamina < 0f)
			{
				stamina = 0f;
			}
			SetStamina(stamina);
			m_staminaRegenTimer = 1f;
		}
	}

	private bool HaveStamina(float amount = 0f)
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		return GetStamina() > amount;
	}

	public float GetStamina()
	{
		if (m_nview == null)
		{
			return 0f;
		}
		if (m_nview.GetZDO() == null)
		{
			return 0f;
		}
		return m_nview.GetZDO().GetFloat(ZDOVars.s_stamina, GetMaxStamina());
	}

	private void SetStamina(float stamina)
	{
		m_nview.GetZDO().Set(ZDOVars.s_stamina, stamina);
	}

	public float GetMaxStamina()
	{
		return m_maxStamina;
	}

	private void UpdateStamina(float dt)
	{
		m_staminaRegenTimer -= dt;
		if (m_staminaRegenTimer > 0f || m_character.InAttack() || m_character.IsSwimming())
		{
			return;
		}
		float stamina = GetStamina();
		float maxStamina = GetMaxStamina();
		if (stamina < maxStamina || stamina > maxStamina)
		{
			float num = (m_tambable.IsHungry() ? m_staminaRegenHungry : m_staminaRegen);
			float num2 = num + (1f - stamina / maxStamina) * num;
			stamina += num2 * dt;
			if (stamina > maxStamina)
			{
				stamina = maxStamina;
			}
			SetStamina(stamina);
		}
	}

	public float GetRiderSkill()
	{
		return m_rideSkill;
	}
}
