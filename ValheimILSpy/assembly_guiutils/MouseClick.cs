using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MouseClick : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public UnityEvent m_leftClick;

	public UnityEvent m_middleClick;

	public UnityEvent m_rightClick;

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			m_leftClick.Invoke();
		}
		else if (eventData.button == PointerEventData.InputButton.Middle)
		{
			m_middleClick.Invoke();
		}
		else if (eventData.button == PointerEventData.InputButton.Right)
		{
			m_rightClick.Invoke();
		}
	}
}
