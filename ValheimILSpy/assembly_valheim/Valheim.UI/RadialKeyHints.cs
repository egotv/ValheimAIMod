using UnityEngine;

namespace Valheim.UI;

public class RadialKeyHints : MonoBehaviour
{
	[SerializeField]
	protected GameObject m_Next;

	[SerializeField]
	protected GameObject m_Prev;

	private void Update()
	{
		if (m_Next != null)
		{
			m_Next.SetActive(ZInput.IsGamepadActive());
		}
		if (m_Prev != null)
		{
			m_Prev.SetActive(ZInput.IsGamepadActive());
		}
	}
}
