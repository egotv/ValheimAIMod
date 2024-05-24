using UnityEngine;

public class SE_Puke : SE_Stats
{
	[Header("__SE_Puke__")]
	public float m_removeInterval = 1f;

	private float m_removeTimer;

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_removeTimer += dt;
		if (m_removeTimer > m_removeInterval)
		{
			m_removeTimer = 0f;
			if ((m_character as Player).RemoveOneFood())
			{
				Hud.instance.DamageFlash();
			}
		}
	}
}
