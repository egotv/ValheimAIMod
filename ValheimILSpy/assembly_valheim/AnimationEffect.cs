using System.Collections.Generic;
using UnityEngine;

public class AnimationEffect : MonoBehaviour
{
	public Transform m_effectRoot;

	private Animator m_animator;

	private List<GameObject> m_attachments;

	private int m_attachStateHash;

	private void Start()
	{
		m_animator = GetComponent<Animator>();
	}

	public void Effect(AnimationEvent e)
	{
		string stringParameter = e.stringParameter;
		GameObject gameObject = e.objectReferenceParameter as GameObject;
		if (!(gameObject == null))
		{
			Transform transform = null;
			if (stringParameter.Length > 0)
			{
				transform = Utils.FindChild(base.transform, stringParameter);
			}
			if (transform == null)
			{
				transform = (m_effectRoot ? m_effectRoot : base.transform);
			}
			Object.Instantiate(gameObject, transform.position, transform.rotation);
		}
	}

	public void Attach(AnimationEvent e)
	{
		string stringParameter = e.stringParameter;
		GameObject gameObject = e.objectReferenceParameter as GameObject;
		bool flag = e.intParameter < 0;
		int intParameter = e.intParameter;
		bool flag2 = intParameter == 10 || intParameter == -10;
		if (gameObject == null)
		{
			return;
		}
		if (stringParameter == "")
		{
			ZLog.LogWarning("No joint name specified for Attach in animation " + e.animatorClipInfo.clip.name);
			return;
		}
		Transform transform = Utils.FindChild(base.transform, stringParameter);
		if (transform == null)
		{
			ZLog.LogWarning("Failed to find attach joint " + stringParameter + " for animation " + e.animatorClipInfo.clip.name);
			return;
		}
		ClearAttachment(transform);
		GameObject gameObject2 = Object.Instantiate(gameObject, transform.position, transform.rotation);
		Vector3 localScale = gameObject2.transform.localScale;
		gameObject2.transform.SetParent(transform, worldPositionStays: true);
		if (flag2)
		{
			gameObject2.transform.localScale = localScale;
		}
		if (!flag)
		{
			if (m_attachments == null)
			{
				m_attachments = new List<GameObject>();
			}
			m_attachments.Add(gameObject2);
			m_attachStateHash = e.animatorStateInfo.fullPathHash;
			CancelInvoke("UpdateAttachments");
			InvokeRepeating("UpdateAttachments", 0.1f, 0.1f);
		}
	}

	private void ClearAttachment(Transform parent)
	{
		if (m_attachments == null)
		{
			return;
		}
		foreach (GameObject attachment in m_attachments)
		{
			if ((bool)attachment && attachment.transform.parent == parent)
			{
				m_attachments.Remove(attachment);
				Object.Destroy(attachment);
				break;
			}
		}
	}

	public void RemoveAttachments()
	{
		if (m_attachments == null)
		{
			return;
		}
		foreach (GameObject attachment in m_attachments)
		{
			Object.Destroy(attachment);
		}
		m_attachments.Clear();
	}

	private void UpdateAttachments()
	{
		if (m_attachments != null && m_attachments.Count > 0)
		{
			if (m_attachStateHash != m_animator.GetCurrentAnimatorStateInfo(0).fullPathHash && m_attachStateHash != m_animator.GetNextAnimatorStateInfo(0).fullPathHash)
			{
				RemoveAttachments();
			}
		}
		else
		{
			CancelInvoke("UpdateAttachments");
		}
	}
}
