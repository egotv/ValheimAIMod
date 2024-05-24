using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class KeyUI : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler
{
	public static KeyUI m_lastKeyUI;

	public virtual void OnPointerEnter(PointerEventData eventData)
	{
		m_lastKeyUI = this;
		SetToolTip();
	}

	public void OnValueChanged()
	{
		ServerOptionsGUI.m_instance.OnCustomValueChanged(this);
	}

	public virtual void Update()
	{
		if (m_lastKeyUI != this && EventSystem.current.currentSelectedGameObject == base.gameObject && ZInput.IsGamepadActive())
		{
			OnPointerEnter(null);
		}
	}

	public abstract bool TryMatch(World world, bool checkAllKeys = false);

	public abstract bool TryMatch(List<string> keys, out string label, bool setElement = true);

	public abstract void SetKeys(World world);

	protected abstract void SetToolTip();
}
