using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIInputHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
	public Action<UIInputHandler> m_onLeftClick;

	public Action<UIInputHandler> m_onLeftDown;

	public Action<UIInputHandler> m_onLeftUp;

	public Action<UIInputHandler> m_onRightClick;

	public Action<UIInputHandler> m_onRightDown;

	public Action<UIInputHandler> m_onRightUp;

	public Action<UIInputHandler> m_onMiddleClick;

	public Action<UIInputHandler> m_onMiddleDown;

	public Action<UIInputHandler> m_onMiddleUp;

	public Action<UIInputHandler> m_onPointerEnter;

	public Action<UIInputHandler> m_onPointerExit;

	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			if (m_onRightDown != null)
			{
				m_onRightDown(this);
			}
		}
		else if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (m_onLeftDown != null)
			{
				m_onLeftDown(this);
			}
		}
		else if (eventData.button == PointerEventData.InputButton.Middle && m_onMiddleDown != null)
		{
			m_onMiddleDown(this);
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			if (m_onRightUp != null)
			{
				m_onRightUp(this);
			}
		}
		else if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (m_onLeftUp != null)
			{
				m_onLeftUp(this);
			}
		}
		else if (eventData.button == PointerEventData.InputButton.Middle && m_onMiddleUp != null)
		{
			m_onMiddleUp(this);
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			if (m_onRightClick != null)
			{
				m_onRightClick(this);
			}
		}
		else if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (m_onLeftClick != null)
			{
				m_onLeftClick(this);
			}
		}
		else if (eventData.button == PointerEventData.InputButton.Middle && m_onMiddleClick != null)
		{
			m_onMiddleClick(this);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (m_onPointerEnter != null)
		{
			m_onPointerEnter(this);
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (m_onPointerExit != null)
		{
			m_onPointerExit(this);
		}
	}
}
