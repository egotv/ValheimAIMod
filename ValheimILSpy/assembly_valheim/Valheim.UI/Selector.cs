using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Valheim.UI;

public class Selector : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI label;

	public UnityEvent OnLeftButtonClickedEvent;

	public UnityEvent OnRightButtonClickedEvent;

	public void SetText(string text)
	{
		if (label != null)
		{
			label.text = text;
		}
	}

	public void OnLeftButtonClicked()
	{
		OnLeftButtonClickedEvent?.Invoke();
	}

	public void OnRightButtonClicked()
	{
		OnRightButtonClickedEvent?.Invoke();
	}
}
