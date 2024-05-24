using UnityEngine;

public class AnimationObjectToggle : MonoBehaviour
{
	public Transform m_parentTransform;

	private GameObject GetGameObject(string objectName)
	{
		if (m_parentTransform == null)
		{
			return base.transform.Find(objectName).gameObject;
		}
		return m_parentTransform.Find(objectName).gameObject;
	}

	private void HideObject(string objectName)
	{
		GetGameObject(objectName).SetActive(value: false);
	}

	private void ShowObject(string objectName)
	{
		GetGameObject(objectName).SetActive(value: true);
	}
}
