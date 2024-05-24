using UnityEngine;

public class TestCollision : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
	}

	public void OnCollisionEnter(Collision info)
	{
		ZLog.Log("Hit by " + info.rigidbody.gameObject.name);
		ZLog.Log("rel vel " + info.relativeVelocity.ToString() + " " + info.relativeVelocity.ToString());
		ZLog.Log("Vel " + info.rigidbody.velocity.ToString() + "  " + info.rigidbody.angularVelocity.ToString());
	}
}
