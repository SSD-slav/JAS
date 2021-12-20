// Fragsurf.Movement.SurfPhysics
using Fragsurf.Movement;
using Fragsurf.TraceUtil;
using UnityEngine;
namespace Fragsurf.TraceUtil {
public class SurfPhysics
{
	public static int groundLayerMask = LayerMask.GetMask("Default", "Ground", "Player clip");

	private static Collider[] _colliders = new Collider[128];

	private static Vector3[] _planes = new Vector3[5];

	public const float HU2M = 52.49344f;

	private const int maxCollisions = 128;

	private const int maxClipPlanes = 5;

	private const int numBumps = 1;

	public const float SurfSlope = 0.7f;

	public static void ResolveCollisions(Collider collider, ref Vector3 origin, ref Vector3 velocity, float rigidbodyPushForce, float velocityMultiplier = 1f, float stepOffset = 0f, ISurfControllable surfer = null)
	{
		int num = 0;
		if (collider is CapsuleCollider)
		{
			CapsuleCollider capsuleCollider = collider as CapsuleCollider;
			GetCapsulePoints(capsuleCollider, origin, out var p, out var p2);
			num = Physics.OverlapCapsuleNonAlloc(p, p2, capsuleCollider.radius, _colliders, groundLayerMask, QueryTriggerInteraction.Ignore);
		}
		else if (collider is BoxCollider)
		{
			num = Physics.OverlapBoxNonAlloc(origin, collider.bounds.extents, _colliders, Quaternion.identity, groundLayerMask, QueryTriggerInteraction.Ignore);
		}
		Vector3 forwardVelocity = Vector3.Scale(velocity, new Vector3(1f, 0f, 1f));
		for (int i = 0; i < num; i++)
		{
			if (Physics.ComputePenetration(collider, origin, Quaternion.identity, _colliders[i], _colliders[i].transform.position, _colliders[i].transform.rotation, out var direction, out var distance))
			{
				if (stepOffset > 0f && surfer != null && surfer.moveData.useStepOffset && StepOffset(collider, _colliders[i], ref origin, ref velocity, rigidbodyPushForce, velocityMultiplier, stepOffset, direction, distance, forwardVelocity, surfer))
				{
					break;
				}
				direction.Normalize();
				Vector3 vector = direction * distance;
				Vector3 vector2 = Vector3.Project(velocity, -direction);
				vector2.y = 0f;
				origin += vector;
				velocity -= vector2 * velocityMultiplier;
				Rigidbody componentInParent = _colliders[i].GetComponentInParent<Rigidbody>();
				if (componentInParent != null && !componentInParent.isKinematic)
				{
					componentInParent.AddForceAtPosition(vector2 * velocityMultiplier * rigidbodyPushForce, origin, ForceMode.Impulse);
				}
			}
		}
	}

	public static bool StepOffset(Collider collider, Collider otherCollider, ref Vector3 origin, ref Vector3 velocity, float rigidbodyPushForce, float velocityMultiplier, float stepOffset, Vector3 direction, float distance, Vector3 forwardVelocity, ISurfControllable surfer)
	{
		if (stepOffset <= 0f)
		{
			return false;
		}
		Vector3 normalized = forwardVelocity.normalized;
		if (normalized.sqrMagnitude == 0f)
		{
			return false;
		}
		Trace trace = Tracer.TraceCollider(collider, origin, origin + Vector3.down * 0.1f, groundLayerMask);
		if (trace.hitCollider == null || Vector3.Angle(Vector3.up, trace.planeNormal) > surfer.moveData.slopeLimit)
		{
			return false;
		}
		Trace trace2 = Tracer.TraceCollider(collider, origin, origin + velocity, groundLayerMask, 0.9f);
		if (trace2.hitCollider == null || Vector3.Angle(Vector3.up, trace2.planeNormal) <= surfer.moveData.slopeLimit)
		{
			return false;
		}
		float num = stepOffset;
		Trace trace3 = Tracer.TraceCollider(collider, origin, origin + Vector3.up * stepOffset, groundLayerMask);
		if (trace3.hitCollider != null)
		{
			num = trace3.distance;
		}
		if (num <= 0f)
		{
			return false;
		}
		Vector3 vector = origin + Vector3.up * num;
		float num2 = stepOffset;
		Trace trace4 = Tracer.TraceCollider(collider, vector, vector + normalized * Mathf.Max(0.2f, stepOffset), groundLayerMask);
		if (trace4.hitCollider != null)
		{
			num2 = trace4.distance;
		}
		if (num2 <= 0f)
		{
			return false;
		}
		Vector3 vector2 = vector + normalized * num2;
		float num3 = num;
		Trace trace5 = Tracer.TraceCollider(collider, vector2, vector2 + Vector3.down * num, groundLayerMask);
		if (trace5.hitCollider != null)
		{
			num3 = trace5.distance;
		}
		float num4 = Mathf.Clamp(num - num3, 0f, stepOffset);
		float z = num2;
		if (Vector3.Angle(Vector3.forward, new Vector3(0f, num4, z)) > surfer.moveData.slopeLimit)
		{
			return false;
		}
		Vector3 vector3 = origin + Vector3.up * num4;
		if (origin != vector3 && num2 > 0f)
		{
			origin = vector3 + normalized * num2 * Time.deltaTime;
			return true;
		}
		return false;
	}

	public static void Friction(ref Vector3 velocity, float stopSpeed, float friction, float deltaTime)
	{
		float magnitude = velocity.magnitude;
		if (!(magnitude < 0.0001905f))
		{
			float num = 0f;
			float num2 = ((magnitude < stopSpeed) ? stopSpeed : magnitude);
			num += num2 * friction * deltaTime;
			float num3 = magnitude - num;
			if (num3 < 0f)
			{
				num3 = 0f;
			}
			if (num3 != magnitude)
			{
				num3 /= magnitude;
				velocity *= num3;
			}
		}
	}

	public static Vector3 AirAccelerate(Vector3 velocity, Vector3 wishdir, float wishspeed, float accel, float airCap, float deltaTime)
	{
		float num = Mathf.Min(wishspeed, airCap);
		float num2 = Vector3.Dot(velocity, wishdir);
		float num3 = num - num2;
		if (num3 <= 0f)
		{
			return Vector3.zero;
		}
		float a = accel * wishspeed * deltaTime;
		a = Mathf.Min(a, num3);
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < 3; i++)
		{
			zero[i] += a * wishdir[i];
		}
		return zero;
	}

	public static Vector3 Accelerate(Vector3 currentVelocity, Vector3 wishdir, float wishspeed, float accel, float deltaTime, float surfaceFriction)
	{
		float num = Vector3.Dot(currentVelocity, wishdir);
		float num2 = wishspeed - num;
		if (num2 <= 0f)
		{
			return Vector3.zero;
		}
		float num3 = accel * deltaTime * wishspeed * surfaceFriction;
		if (num3 > num2)
		{
			num3 = num2;
		}
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < 3; i++)
		{
			zero[i] += num3 * wishdir[i];
		}
		return zero;
	}

	public static int Reflect(ref Vector3 velocity, Collider collider, Vector3 origin, float deltaTime)
	{
		Vector3 output = Vector3.zero;
		int num = 0;
		int num2 = 0;
		Vector3 input = velocity;
		Vector3 rhs = velocity;
		float num3 = 0f;
		float num4 = deltaTime;
		for (int i = 0; i < 1; i++)
		{
			if (velocity.magnitude == 0f)
			{
				break;
			}
			Vector3 end = VectorExtensions.VectorMa(origin, num4, velocity);
			Trace trace = Tracer.TraceCollider(collider, origin, end, groundLayerMask);
			num3 += trace.fraction;
			if (trace.fraction > 0f)
			{
				input = velocity;
				num2 = 0;
			}
			if (trace.fraction == 1f)
			{
				break;
			}
			if (trace.planeNormal.y > 0.7f)
			{
				num |= 1;
			}
			if (trace.planeNormal.y == 0f)
			{
				num |= 2;
			}
			num4 -= num4 * trace.fraction;
			if (num2 >= 5)
			{
				velocity = Vector3.zero;
				break;
			}
			_planes[num2] = trace.planeNormal;
			num2++;
			if (num2 == 1)
			{
				for (int j = 0; j < num2; j++)
				{
					if (_planes[j][1] > 0.7f)
					{
						return num;
					}
					ClipVelocity(input, _planes[j], ref output, 1f);
				}
				velocity = output;
				input = output;
				continue;
			}
			int num5 = 0;
			for (num5 = 0; num5 < num2; num5++)
			{
				ClipVelocity(input, _planes[num5], ref velocity, 1f);
				int num6 = 0;
				for (num6 = 0; num6 < num2 && (num6 == num5 || !(Vector3.Dot(velocity, _planes[num6]) < 0f)); num6++)
				{
				}
				if (num6 == num2)
				{
					break;
				}
			}
			float num7;
			if (num5 == num2)
			{
				if (num2 != 2)
				{
					velocity = Vector3.zero;
					break;
				}
				Vector3 normalized = Vector3.Cross(_planes[0], _planes[1]).normalized;
				num7 = Vector3.Dot(normalized, velocity);
				velocity = normalized * num7;
			}
			num7 = Vector3.Dot(velocity, rhs);
			if (num7 <= 0f)
			{
				velocity = Vector3.zero;
				break;
			}
		}
		if (num3 == 0f)
		{
			velocity = Vector3.zero;
		}
		return num;
	}

	public static int ClipVelocity(Vector3 input, Vector3 normal, ref Vector3 output, float overbounce)
	{
		float num = normal[1];
		int num2 = 0;
		if (num > 0f)
		{
			num2 |= 1;
		}
		if (num == 0f)
		{
			num2 |= 2;
		}
		float num3 = Vector3.Dot(input, normal) * overbounce;
		for (int i = 0; i < 3; i++)
		{
			float num4 = normal[i] * num3;
			output[i] = input[i] - num4;
		}
		float num5 = Vector3.Dot(output, normal);
		if (num5 < 0f)
		{
			output -= normal * num5;
		}
		return num2;
	}

	public static void GetCapsulePoints(CapsuleCollider capc, Vector3 origin, out Vector3 p1, out Vector3 p2)
	{
		float num = capc.height / 2f - capc.radius;
		p1 = origin + capc.center + Vector3.up * num;
		p2 = origin + capc.center - Vector3.up * num;
	}
}
}