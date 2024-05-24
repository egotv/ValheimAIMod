using Unity.Collections;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;

[ExecuteAlways]
public class VortexParticles : MonoBehaviour
{
	private struct VortexParticlesJob : IJobParticleSystemParallelFor
	{
		[ReadOnly]
		public Vector3 vortexCenter;

		[ReadOnly]
		public float pullStrength;

		[ReadOnly]
		public Vector3 upDir;

		[ReadOnly]
		public float vortexStrength;

		[ReadOnly]
		public bool lineAttraction;

		[ReadOnly]
		public bool useCustomData;

		[ReadOnly]
		public float deltaTime;

		[ReadOnly]
		public bool distanceStrengthFalloff;

		public void Execute(ParticleSystemJobData particles, int i)
		{
			Vector3 vector = new Vector3(particles.velocities.x[i], particles.velocities.y[i], particles.velocities.z[i]);
			Vector3 vector2 = new Vector3(particles.positions.x[i], particles.positions.y[i], particles.positions.z[i]);
			Vector3 vector3 = vortexCenter;
			float num = (useCustomData ? particles.customData1.x[i] : vortexStrength);
			if (lineAttraction)
			{
				vector3.y = vector2.y;
			}
			Vector3 vector4 = vector3 - vector2;
			if (distanceStrengthFalloff)
			{
				float num2 = Vector3.Magnitude(vector4);
				num *= (0f - num2) / Mathf.Sqrt(num2);
			}
			vector4 = Vector3.Normalize(vector4);
			Vector3 vector5 = Vector3.Cross(Vector3.Normalize(vector4), upDir);
			Vector3 vector6 = vector + vector4 * pullStrength * deltaTime;
			vector6 += vector5 * num * deltaTime;
			NativeArray<float> x = particles.velocities.x;
			NativeArray<float> y = particles.velocities.y;
			NativeArray<float> z = particles.velocities.z;
			x[i] = vector6.x;
			y[i] = vector6.y;
			z[i] = vector6.z;
		}
	}

	private ParticleSystem ps;

	private VortexParticlesJob job;

	[SerializeField]
	private bool effectOn = true;

	[SerializeField]
	private Vector3 centerOffset;

	[SerializeField]
	private float pullStrength;

	[SerializeField]
	private float vortexStrength;

	[SerializeField]
	private bool lineAttraction;

	[SerializeField]
	private bool useCustomData;

	[SerializeField]
	private bool distanceStrengthFalloff;

	private void Start()
	{
		ps = GetComponent<ParticleSystem>();
		if (ps == null)
		{
			ZLog.LogWarning("VortexParticles object '" + base.gameObject.name + "' is missing a particle system and disabled!");
			effectOn = false;
		}
	}

	private void Update()
	{
		if (ps.main.simulationSpace == ParticleSystemSimulationSpace.Local)
		{
			job.vortexCenter = centerOffset;
			job.upDir = new Vector3(0f, 1f, 0f);
		}
		else
		{
			job.vortexCenter = base.transform.position + centerOffset;
			job.upDir = base.transform.up;
		}
		job.pullStrength = pullStrength;
		job.vortexStrength = vortexStrength;
		job.lineAttraction = lineAttraction;
		job.useCustomData = useCustomData;
		job.deltaTime = Time.deltaTime;
		job.distanceStrengthFalloff = distanceStrengthFalloff;
	}

	private void OnParticleUpdateJobScheduled()
	{
		if (ps == null)
		{
			ps = GetComponent<ParticleSystem>();
			if (ps == null)
			{
				ZLog.LogWarning("VortexParticles object '" + base.gameObject.name + "' is missing a particle system and disabled!");
				effectOn = false;
			}
		}
		if (effectOn)
		{
			job.Schedule(ps, 1024);
		}
	}
}
