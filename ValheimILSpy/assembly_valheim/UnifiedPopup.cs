using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnifiedPopup : MonoBehaviour
{
	public delegate void PopupEnabledHandler();

	private static UnifiedPopup instance;

	[Header("References")]
	[Tooltip("A reference to the parent object of the rest of the popup. This is what gets enabled and disabled to show and hide the popup.")]
	[SerializeField]
	private GameObject popupUIParent;

	[Tooltip("A reference to the left button of the popup, assigned to escape on keyboards and B on controllers. This usually gets assigned to \"back\", \"no\" or similar in dual-action popups. It's not necessary to assign buttons to any Unity Events - that is done automatically.")]
	[SerializeField]
	private Button buttonLeft;

	[Tooltip("A reference to the center button of the popup, assigned to enter on keyboards and A on controllers. This usually gets assigned to \"Ok\" or similar in single-action popups. It's not necessary to assign buttons to any Unity Events - that is done automatically.")]
	[SerializeField]
	private Button buttonCenter;

	[Tooltip("A reference to the right button of the popup, assigned to enter on keyboards and A on controllers. This usually gets assigned to \"yes\", \"accept\" or similar in dual-action popups. It's not necessary to assign buttons to any Unity Events - that is done automatically.")]
	[SerializeField]
	private Button buttonRight;

	[Tooltip("A reference to the header text of the popup.")]
	[SerializeField]
	private TextMeshProUGUI headerText;

	[Tooltip("A reference to the body text of the popup.")]
	[SerializeField]
	private TextMeshProUGUI bodyText;

	[Header("Button text")]
	[SerializeField]
	private string yesText = "$menu_yes";

	[SerializeField]
	private string noText = "$menu_no";

	[SerializeField]
	private string okText = "$menu_ok";

	[SerializeField]
	private string cancelText = "$menu_cancel";

	private TMP_Text buttonLeftText;

	private TMP_Text buttonCenterText;

	private TMP_Text buttonRightText;

	private bool wasClosedThisFrame;

	private Stack<PopupBase> popupStack = new Stack<PopupBase>();

	public static event PopupEnabledHandler OnPopupEnabled;

	private void Awake()
	{
		if (buttonLeft != null)
		{
			buttonLeftText = buttonLeft.GetComponentInChildren<TMP_Text>();
		}
		if (buttonCenter != null)
		{
			buttonCenterText = buttonCenter.GetComponentInChildren<TMP_Text>();
		}
		if (buttonRight != null)
		{
			buttonRightText = buttonRight.GetComponentInChildren<TMP_Text>();
		}
		Hide();
	}

	private void OnEnable()
	{
		if (instance != null && instance != this)
		{
			ZLog.LogError("Can't have more than one UnifiedPopup component enabled at the same time!");
			return;
		}
		instance = this;
		UnifiedPopup.OnPopupEnabled?.Invoke();
	}

	private void OnDisable()
	{
		if (instance == null)
		{
			ZLog.LogError("Instance of UnifiedPopup was already null! This may have happened because you had more than one UnifiedPopup component enabled at the same time, which isn't allowed!");
		}
		else
		{
			instance = null;
		}
	}

	private void LateUpdate()
	{
		while (popupStack.Count > 0 && popupStack.Peek() is LivePopupBase && (popupStack.Peek() as LivePopupBase).ShouldClose)
		{
			Pop();
		}
		if (!IsVisible())
		{
			wasClosedThisFrame = false;
		}
	}

	private static bool InstanceIsNullError()
	{
		if (instance == null)
		{
			ZLog.LogError("Can't show popup when there is no enabled UnifiedPopup component in the scene!");
			return true;
		}
		return false;
	}

	public static bool IsAvailable()
	{
		return instance != null;
	}

	public static void Push(PopupBase popup)
	{
		if (!InstanceIsNullError())
		{
			instance.popupStack.Push(popup);
			instance.ShowTopmost();
		}
	}

	public static void Pop()
	{
		if (InstanceIsNullError())
		{
			return;
		}
		if (instance.popupStack.Count <= 0)
		{
			ZLog.LogError("Push/pop mismatch! Tried to pop a popup element off the stack when it was empty!");
			return;
		}
		PopupBase popupBase = instance.popupStack.Pop();
		if (popupBase is LivePopupBase)
		{
			instance.StopCoroutine((popupBase as LivePopupBase).updateCoroutine);
		}
		if (instance.popupStack.Count <= 0)
		{
			instance.Hide();
		}
		else
		{
			instance.ShowTopmost();
		}
	}

	public static void SetFocus()
	{
		if (instance.buttonCenter != null && instance.buttonCenter.gameObject.activeInHierarchy)
		{
			instance.buttonCenter.Select();
		}
		else if (instance.buttonRight != null && instance.buttonRight.gameObject.activeInHierarchy)
		{
			instance.buttonRight.Select();
		}
		else if (instance.buttonLeft != null && instance.buttonLeft.gameObject.activeInHierarchy)
		{
			instance.buttonLeft.Select();
		}
	}

	public static bool IsVisible()
	{
		if (IsAvailable())
		{
			return instance.popupUIParent.activeInHierarchy;
		}
		return false;
	}

	public static bool WasVisibleThisFrame()
	{
		if (!IsVisible())
		{
			if (IsAvailable())
			{
				return instance.wasClosedThisFrame;
			}
			return false;
		}
		return true;
	}

	private void ShowTopmost()
	{
		Show(instance.popupStack.Peek());
	}

	private void Show(PopupBase popup)
	{
		ResetUI();
		switch (popup.Type)
		{
		case PopupType.YesNo:
			ShowYesNo(popup as YesNoPopup);
			break;
		case PopupType.Warning:
			ShowWarning(popup as WarningPopup);
			break;
		case PopupType.CancelableTask:
			ShowCancelableTask(popup as CancelableTaskPopup);
			break;
		}
		popupUIParent.SetActive(value: true);
		popupUIParent.transform.parent.SetAsLastSibling();
	}

	private void ResetUI()
	{
		buttonLeft.onClick.RemoveAllListeners();
		buttonCenter.onClick.RemoveAllListeners();
		buttonRight.onClick.RemoveAllListeners();
		buttonLeft.gameObject.SetActive(value: false);
		buttonCenter.gameObject.SetActive(value: false);
		buttonRight.gameObject.SetActive(value: false);
	}

	private void ShowYesNo(YesNoPopup popup)
	{
		headerText.text = popup.header;
		bodyText.text = popup.text;
		buttonRightText.text = Localization.instance.Localize(yesText);
		buttonRight.gameObject.SetActive(value: true);
		buttonRight.onClick.AddListener(delegate
		{
			popup.yesCallback?.Invoke();
		});
		buttonLeftText.text = Localization.instance.Localize(noText);
		buttonLeft.gameObject.SetActive(value: true);
		buttonLeft.onClick.AddListener(delegate
		{
			popup.noCallback?.Invoke();
		});
	}

	private void ShowWarning(WarningPopup popup)
	{
		headerText.text = popup.header;
		bodyText.text = popup.text;
		buttonCenterText.text = Localization.instance.Localize(okText);
		buttonCenter.gameObject.SetActive(value: true);
		buttonCenter.onClick.AddListener(delegate
		{
			popup.okCallback?.Invoke();
		});
	}

	private void ShowCancelableTask(CancelableTaskPopup popup)
	{
		popup.SetTextReferences(headerText, bodyText);
		popup.SetUpdateCoroutineReference(StartCoroutine(popup.updateRoutine));
		buttonCenterText.text = Localization.instance.Localize(cancelText);
		buttonCenter.gameObject.SetActive(value: true);
		buttonCenter.onClick.AddListener(delegate
		{
			popup.cancelCallback?.Invoke();
			StopCoroutine(popup.updateCoroutine);
		});
	}

	private void Hide()
	{
		wasClosedThisFrame = true;
		popupUIParent.SetActive(value: false);
	}
}
