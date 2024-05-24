using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class InputActionsSwitcher : MonoBehaviour
{
	[SerializeField]
	protected InputActionAsset standaloneInputActions;

	[SerializeField]
	protected InputActionAsset consoleInputActions;

	[SerializeField]
	protected InputSystemUIInputModule inputModule;
}
