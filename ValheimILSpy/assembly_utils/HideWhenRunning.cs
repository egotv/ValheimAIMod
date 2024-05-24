using UnityEngine;

public class HideWhenRunning : MonoBehaviour
{
	private void Awake()
	{
		if (Application.isPlaying)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
