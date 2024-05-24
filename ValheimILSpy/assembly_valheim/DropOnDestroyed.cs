using System;
using System.Collections.Generic;
using UnityEngine;

public class DropOnDestroyed : MonoBehaviour
{
	[Header("Drops")]
	public DropTable m_dropWhenDestroyed = new DropTable();

	public float m_spawnYOffset = 0.5f;

	public float m_spawnYStep = 0.3f;

	private void Awake()
	{
		IDestructible component = GetComponent<IDestructible>();
		Destructible destructible = component as Destructible;
		if ((bool)destructible)
		{
			destructible.m_onDestroyed = (Action)Delegate.Combine(destructible.m_onDestroyed, new Action(OnDestroyed));
		}
		WearNTear wearNTear = component as WearNTear;
		if ((bool)wearNTear)
		{
			wearNTear.m_onDestroyed = (Action)Delegate.Combine(wearNTear.m_onDestroyed, new Action(OnDestroyed));
		}
	}

	private void OnDestroyed()
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		Vector3 position = base.transform.position;
		if (position.y < groundHeight)
		{
			position.y = groundHeight + 0.1f;
		}
		List<GameObject> dropList = m_dropWhenDestroyed.GetDropList();
		for (int i = 0; i < dropList.Count; i++)
		{
			Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.5f;
			Vector3 position2 = position + Vector3.up * m_spawnYOffset + new Vector3(vector.x, m_spawnYStep * (float)i, vector.y);
			Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
			ItemDrop.OnCreateNew(UnityEngine.Object.Instantiate(dropList[i], position2, rotation));
		}
	}
}
