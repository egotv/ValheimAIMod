using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnAbility : MonoBehaviour, IProjectile
{
	public enum TargetType
	{
		ClosestEnemy,
		RandomEnemy,
		Caster,
		Position,
		RandomPathfindablePosition
	}

	[Serializable]
	public class LevelUpSettings
	{
		public Skills.SkillType m_skill;

		public int m_skillLevel;

		public int m_setLevel;

		public int m_maxSpawns;
	}

	[Header("Spawn")]
	public GameObject[] m_spawnPrefab;

	public bool m_spawnOnAwake;

	public bool m_alertSpawnedCreature = true;

	public bool m_passiveAggressive;

	public bool m_spawnAtTarget = true;

	public int m_minToSpawn = 1;

	public int m_maxToSpawn = 1;

	public int m_maxSpawned = 3;

	public float m_spawnRadius = 3f;

	public bool m_circleSpawn;

	public bool m_snapToTerrain = true;

	[Tooltip("Used to give random Y rotations to things like AOEs that aren't circular")]
	public bool m_randomYRotation;

	public float m_spawnGroundOffset;

	public int m_getSolidHeightMargin = 1000;

	public float m_initialSpawnDelay;

	public float m_spawnDelay;

	public float m_preSpawnDelay;

	public bool m_commandOnSpawn;

	public bool m_wakeUpAnimation;

	public Skills.SkillType m_copySkill;

	public float m_copySkillToRandomFactor;

	public bool m_setMaxInstancesFromWeaponLevel;

	public List<LevelUpSettings> m_levelUpSettings;

	public TargetType m_targetType;

	public Pathfinding.AgentType m_targetWhenPathfindingType = Pathfinding.AgentType.Humanoid;

	public float m_maxTargetRange = 40f;

	public EffectList m_spawnEffects = new EffectList();

	public EffectList m_preSpawnEffects = new EffectList();

	[Tooltip("Used for the troll summoning staff, to spawn an AOE that's friendly to the spawned creature.")]
	public GameObject m_aoePrefab;

	[Header("Projectile")]
	public float m_projectileVelocity = 10f;

	public float m_projectileVelocityMax;

	public float m_projectileAccuracy = 10f;

	public bool m_randomDirection;

	public float m_randomAngleMin;

	public float m_randomAngleMax;

	private Character m_owner;

	private ItemDrop.ItemData m_weapon;

	public void Awake()
	{
		if (m_spawnOnAwake)
		{
			StartCoroutine("Spawn");
		}
	}

	public void Setup(Character owner, Vector3 velocity, float hitNoise, HitData hitData, ItemDrop.ItemData item, ItemDrop.ItemData ammo)
	{
		m_owner = owner;
		m_weapon = item;
		StartCoroutine("Spawn");
	}

	public string GetTooltipString(int itemQuality)
	{
		return "";
	}

	private IEnumerator Spawn()
	{
		if (m_initialSpawnDelay > 0f)
		{
			yield return new WaitForSeconds(m_initialSpawnDelay);
		}
		int toSpawn = UnityEngine.Random.Range(m_minToSpawn, m_maxToSpawn);
		Skills skills = (m_owner ? m_owner.GetSkills() : null);
		int num3;
		for (int i = 0; i < toSpawn; num3 = i + 1, i = num3)
		{
			Vector3 targetPosition = base.transform.position;
			bool foundSpawnPoint = false;
			int tries = ((m_targetType != TargetType.RandomPathfindablePosition) ? 1 : 5);
			for (int j = 0; j < tries; j++)
			{
				bool flag;
				foundSpawnPoint = (flag = FindTarget(out targetPosition, i, toSpawn));
				if (flag)
				{
					break;
				}
				if (m_targetType == TargetType.RandomPathfindablePosition)
				{
					if (j == tries - 1)
					{
						Terminal.LogWarning($"SpawnAbility failed to pathfindable target after {tries} tries, defaulting to transform position.");
						targetPosition = base.transform.position;
						foundSpawnPoint = true;
					}
					else
					{
						Terminal.Log("SpawnAbility failed to pathfindable target, waiting before retry.");
						yield return new WaitForSeconds(0.2f);
					}
				}
			}
			if (!foundSpawnPoint)
			{
				Terminal.LogWarning("SpawnAbility failed to find spawn point, aborting spawn.");
				continue;
			}
			Vector3 spawnPoint = targetPosition;
			if (m_targetType != TargetType.RandomPathfindablePosition)
			{
				Vector3 vector = (m_spawnAtTarget ? targetPosition : base.transform.position);
				Vector2 vector2 = UnityEngine.Random.insideUnitCircle * m_spawnRadius;
				if (m_circleSpawn)
				{
					vector2 = GetCirclePoint(i, toSpawn) * m_spawnRadius;
				}
				spawnPoint = vector + new Vector3(vector2.x, 0f, vector2.y);
				if (m_snapToTerrain)
				{
					ZoneSystem.instance.GetSolidHeight(spawnPoint, out var height, m_getSolidHeightMargin);
					spawnPoint.y = height;
				}
				spawnPoint.y += m_spawnGroundOffset;
				if (Mathf.Abs(spawnPoint.y - vector.y) > 100f)
				{
					continue;
				}
			}
			GameObject prefab = m_spawnPrefab[UnityEngine.Random.Range(0, m_spawnPrefab.Length)];
			if (m_maxSpawned > 0 && SpawnSystem.GetNrOfInstances(prefab) >= m_maxSpawned)
			{
				continue;
			}
			m_preSpawnEffects.Create(spawnPoint, Quaternion.identity);
			if (m_preSpawnDelay > 0f)
			{
				yield return new WaitForSeconds(m_preSpawnDelay);
			}
			Terminal.Log("SpawnAbility spawning a " + prefab.name);
			GameObject gameObject = UnityEngine.Object.Instantiate(prefab, spawnPoint, Quaternion.Euler(0f, UnityEngine.Random.value * (float)Math.PI * 2f, 0f));
			ZNetView component = gameObject.GetComponent<ZNetView>();
			Projectile component2 = gameObject.GetComponent<Projectile>();
			if ((bool)component2)
			{
				SetupProjectile(component2, targetPosition);
			}
			if (m_randomYRotation)
			{
				gameObject.transform.Rotate(Vector3.up, UnityEngine.Random.Range(-180, 180));
			}
			if ((bool)skills)
			{
				if (m_copySkill != 0 && m_copySkillToRandomFactor > 0f)
				{
					component.GetZDO().Set(ZDOVars.s_randomSkillFactor, 1f + skills.GetSkillLevel(m_copySkill) * m_copySkillToRandomFactor);
				}
				if (m_levelUpSettings.Count > 0)
				{
					Character component3 = gameObject.GetComponent<Character>();
					if ((object)component3 != null)
					{
						for (int num = m_levelUpSettings.Count - 1; num >= 0; num--)
						{
							LevelUpSettings levelUpSettings = m_levelUpSettings[num];
							if (skills.GetSkillLevel(levelUpSettings.m_skill) >= (float)levelUpSettings.m_skillLevel)
							{
								component3.SetLevel(levelUpSettings.m_setLevel);
								int num2 = (m_setMaxInstancesFromWeaponLevel ? m_weapon.m_quality : levelUpSettings.m_maxSpawns);
								if (num2 > 0)
								{
									component.GetZDO().Set(ZDOVars.s_maxInstances, num2);
								}
								break;
							}
						}
					}
				}
			}
			if (m_commandOnSpawn)
			{
				Tameable component4 = gameObject.GetComponent<Tameable>();
				if ((object)component4 != null && m_owner is Humanoid humanoid)
				{
					component4.Command(humanoid, message: false);
					if (humanoid == Player.m_localPlayer)
					{
						Game.instance.IncrementPlayerStat(PlayerStatType.SkeletonSummons);
					}
				}
			}
			if (m_wakeUpAnimation)
			{
				gameObject.GetComponent<ZSyncAnimation>()?.SetBool("wakeup", value: true);
			}
			BaseAI component5 = gameObject.GetComponent<BaseAI>();
			if (component5 != null)
			{
				if (m_alertSpawnedCreature)
				{
					component5.Alert();
				}
				BaseAI baseAI = m_owner.GetBaseAI();
				if (component5.m_aggravatable && (bool)baseAI && baseAI.m_aggravatable)
				{
					component5.SetAggravated(baseAI.IsAggravated(), BaseAI.AggravatedReason.Damage);
				}
				if (m_passiveAggressive)
				{
					component5.m_passiveAggresive = true;
				}
			}
			SetupAoe(gameObject.GetComponent<Character>(), spawnPoint);
			m_spawnEffects.Create(spawnPoint, Quaternion.identity);
			if (m_spawnDelay > 0f)
			{
				yield return new WaitForSeconds(m_spawnDelay);
			}
		}
		if (!m_spawnOnAwake)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private Vector3 GetRandomConeDirection()
	{
		int num = UnityEngine.Random.Range(0, 360);
		float f = UnityEngine.Random.Range(m_randomAngleMin, m_randomAngleMax);
		return Quaternion.AngleAxis(num, Vector3.up) * new Vector3(Mathf.Sin(f), Mathf.Cos(f), 0f);
	}

	private void SetupProjectile(Projectile projectile, Vector3 targetPoint)
	{
		Vector3 vector = (m_randomDirection ? GetRandomConeDirection() : (targetPoint - projectile.transform.position).normalized);
		Vector3 axis = Vector3.Cross(vector, Vector3.up);
		Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - m_projectileAccuracy, m_projectileAccuracy), Vector3.up);
		vector = Quaternion.AngleAxis(UnityEngine.Random.Range(0f - m_projectileAccuracy, m_projectileAccuracy), axis) * vector;
		vector = quaternion * vector;
		float num = ((m_projectileVelocityMax > 0f) ? UnityEngine.Random.Range(m_projectileVelocity, m_projectileVelocityMax) : m_projectileVelocity);
		projectile.Setup(m_owner, vector * num, -1f, null, null, null);
	}

	private void SetupAoe(Character owner, Vector3 targetPoint)
	{
		if (!(m_aoePrefab == null) && !(owner == null))
		{
			Aoe component = UnityEngine.Object.Instantiate(m_aoePrefab, targetPoint, Quaternion.identity).GetComponent<Aoe>();
			if (!(component == null))
			{
				component.Setup(owner, Vector3.zero, -1f, null, null, null);
			}
		}
	}

	private bool FindTarget(out Vector3 point, int i, int spawnCount)
	{
		point = Vector3.zero;
		switch (m_targetType)
		{
		case TargetType.ClosestEnemy:
		{
			if (m_owner == null)
			{
				return false;
			}
			Character character2 = BaseAI.FindClosestEnemy(m_owner, base.transform.position, m_maxTargetRange);
			if (character2 != null)
			{
				point = character2.transform.position;
				return true;
			}
			return false;
		}
		case TargetType.RandomEnemy:
		{
			if (m_owner == null)
			{
				return false;
			}
			Character character = BaseAI.FindRandomEnemy(m_owner, base.transform.position, m_maxTargetRange);
			if (character != null)
			{
				point = character.transform.position;
				return true;
			}
			return false;
		}
		case TargetType.Position:
			point = base.transform.position;
			return true;
		case TargetType.Caster:
			if (m_owner == null)
			{
				return false;
			}
			point = m_owner.transform.position;
			return true;
		case TargetType.RandomPathfindablePosition:
		{
			if (m_owner == null)
			{
				return false;
			}
			List<Vector3> list = new List<Vector3>();
			Vector2 vector = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(m_spawnRadius / 2f, m_spawnRadius);
			point = base.transform.position + new Vector3(vector.x, 2f, vector.y);
			ZoneSystem.instance.GetSolidHeight(point, out var height, 2);
			point.y = height;
			if (Pathfinding.instance.GetPath(m_owner.transform.position, point, list, m_targetWhenPathfindingType, requireFullPath: true, cleanup: false, havePath: true))
			{
				Terminal.Log($"SpawnAbility found path target, distance: {Vector3.Distance(base.transform.position, list[0])}");
				point = list[list.Count - 1];
				return true;
			}
			return false;
		}
		default:
			return false;
		}
	}

	private Vector2 GetCirclePoint(int i, int spawnCount)
	{
		float num = (float)i / (float)spawnCount;
		float x = Mathf.Sin(num * (float)Math.PI * 2f);
		float y = Mathf.Cos(num * (float)Math.PI * 2f);
		return new Vector2(x, y);
	}
}
