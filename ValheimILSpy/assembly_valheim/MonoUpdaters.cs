using System.Collections.Generic;
using UnityEngine;

public class MonoUpdaters : MonoBehaviour
{
	private static MonoUpdaters s_instance;

	private readonly List<IMonoUpdater> m_update = new List<IMonoUpdater>();

	private readonly List<IUpdateAI> m_ai = new List<IUpdateAI>();

	private readonly List<WaterVolume> m_waterVolumeInstances = new List<WaterVolume>();

	private static int s_updateCount;

	private float m_updateAITimer;

	public static int UpdateCount => s_updateCount;

	private void Awake()
	{
		s_instance = this;
	}

	private void OnDestroy()
	{
		s_instance = null;
	}

	private void FixedUpdate()
	{
		s_updateCount++;
		float fixedDeltaTime = Time.fixedDeltaTime;
		if (WaterVolume.Instances.Count > 0)
		{
			WaterVolume.StaticUpdate();
		}
		m_update.CustomFixedUpdate(ZSyncTransform.Instances, "MonoUpdaters.FixedUpdate.ZSyncTransform", fixedDeltaTime);
		m_update.CustomFixedUpdate(ZSyncAnimation.Instances, "MonoUpdaters.FixedUpdate.ZSyncAnimation", fixedDeltaTime);
		m_update.CustomFixedUpdate(Floating.Instances, "MonoUpdaters.FixedUpdate.Floating", fixedDeltaTime);
		m_update.CustomFixedUpdate(Ship.Instances, "MonoUpdaters.FixedUpdate.Ship", fixedDeltaTime);
		m_update.CustomFixedUpdate(Fish.Instances, "MonoUpdaters.FixedUpdate.Fish", fixedDeltaTime);
		m_update.CustomFixedUpdate(CharacterAnimEvent.Instances, "MonoUpdaters.FixedUpdate.CharacterAnimEvent", fixedDeltaTime);
		m_updateAITimer += fixedDeltaTime;
		if (m_updateAITimer >= 0.05f)
		{
			m_ai.UpdateAI(BaseAI.Instances, "MonoUpdaters.FixedUpdate.BaseAI", 0.05f);
			m_updateAITimer -= 0.05f;
		}
		m_update.CustomFixedUpdate(Character.Instances, "MonoUpdaters.FixedUpdate.Character", fixedDeltaTime);
		m_update.CustomFixedUpdate(Aoe.Instances, "MonoUpdaters.FixedUpdate.Aoe", fixedDeltaTime);
		m_update.CustomFixedUpdate(RandomFlyingBird.Instances, "MonoUpdaters.FixedUpdate.RandomFlyingBird", fixedDeltaTime);
		m_update.CustomFixedUpdate(MeleeWeaponTrail.Instances, "MonoUpdaters.FixedUpdate.MeleeWeaponTrail", fixedDeltaTime);
	}

	private void Update()
	{
		s_updateCount++;
		float deltaTime = Time.deltaTime;
		float time = Time.time;
		m_waterVolumeInstances.AddRange(WaterVolume.Instances);
		if (m_waterVolumeInstances.Count > 0)
		{
			WaterVolume.StaticUpdate();
			foreach (WaterVolume waterVolumeInstance in m_waterVolumeInstances)
			{
				waterVolumeInstance.UpdateFloaters();
			}
			foreach (WaterVolume waterVolumeInstance2 in m_waterVolumeInstances)
			{
				waterVolumeInstance2.UpdateMaterials();
			}
			m_waterVolumeInstances.Clear();
		}
		m_update.CustomUpdate(Smoke.Instances, "MonoUpdaters.Update.Smoke", deltaTime, time);
		m_update.CustomUpdate(ZSFX.Instances, "MonoUpdaters.Update.ZSFX", deltaTime, time);
		m_update.CustomUpdate(VisEquipment.Instances, "MonoUpdaters.Update.VisEquipment", deltaTime, time);
		m_update.CustomUpdate(FootStep.Instances, "MonoUpdaters.Update.FootStep", deltaTime, time);
		m_update.CustomUpdate(InstanceRenderer.Instances, "MonoUpdaters.Update.InstanceRenderer", deltaTime, time);
		m_update.CustomUpdate(WaterTrigger.Instances, "MonoUpdaters.Update.WaterTrigger", deltaTime, time);
		m_update.CustomUpdate(LightFlicker.Instances, "MonoUpdaters.Update.LightFlicker", deltaTime, time);
		m_update.CustomUpdate(SmokeSpawner.Instances, "MonoUpdaters.Update.SmokeSpawner", deltaTime, time);
		m_update.CustomUpdate(CraftingStation.Instances, "MonoUpdaters.Update.CraftingStation", deltaTime, time);
	}

	private void LateUpdate()
	{
		float deltaTime = Time.deltaTime;
		if (WaterVolume.Instances.Count > 0)
		{
			WaterVolume.StaticUpdate();
		}
		m_update.CustomLateUpdate(ZSyncTransform.Instances, "MonoUpdaters.LateUpdate.ZSyncTransform", deltaTime);
		m_update.CustomLateUpdate(CharacterAnimEvent.Instances, "MonoUpdaters.LateUpdate.CharacterAnimEvent", deltaTime);
		m_update.CustomLateUpdate(Heightmap.Instances, "MonoUpdaters.LateUpdate.Heightmap", deltaTime);
		m_update.CustomLateUpdate(ShipEffects.Instances, "MonoUpdaters.LateUpdate.ShipEffects", deltaTime);
		m_update.CustomLateUpdate(Tail.Instances, "MonoUpdaters.LateUpdate.Tail", deltaTime);
		m_update.CustomLateUpdate(LineAttach.Instances, "MonoUpdaters.LateUpdate.LineAttach", deltaTime);
	}
}
