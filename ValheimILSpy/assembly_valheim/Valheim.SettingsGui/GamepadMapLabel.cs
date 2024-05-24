using TMPro;
using UnityEngine;

namespace Valheim.SettingsGui;

public class GamepadMapLabel : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI label;

	[SerializeField]
	private TextMeshProUGUI button;

	public TextMeshProUGUI Label => label;

	public TextMeshProUGUI Button => button;
}
