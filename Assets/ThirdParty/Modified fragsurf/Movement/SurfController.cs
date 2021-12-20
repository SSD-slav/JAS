// Fragsurf.Movement.SurfController
using Fragsurf.Movement;
using Fragsurf.TraceUtil;
using UnityEngine;

public class SurfController
{
	public KeyCode up;

	[HideInInspector]
	public Transform playerTransform;

	private ISurfControllable _surfer;

	private MovementConfig _config;

	private float _deltaTime;

	public bool jumping;

	public bool crouching;

	public float speed;

	public Transform camera;

	public float cameraYPos;

	private float slideSpeedCurrent;

	private Vector3 slideDirection = Vector3.forward;

	private bool wasSliding;

	private float slideDelay;

	private bool uncrouchDown;

	private float crouchLerp;

	private float frictionMult = 1f;

	private Vector3 groundNormal = Vector3.up;

	public void ProcessMovement(ISurfControllable surfer, MovementConfig config, float deltaTime)
	{
		_surfer = surfer;
		_config = config;
		_deltaTime = deltaTime;
		if (_surfer.moveData.laddersEnabled && !_surfer.moveData.climbingLadder)
		{
			LadderCheck(new Vector3(1f, 0.95f, 1f), _surfer.moveData.velocity * Mathf.Clamp(Time.deltaTime * 2f, 0.025f, 0.25f));
		}
		if (_surfer.moveData.laddersEnabled && _surfer.moveData.climbingLadder)
		{
			LadderPhysics();
		}
		else if (!_surfer.moveData.underwater)
		{
			if (_surfer.moveData.velocity.y <= 0f)
			{
				jumping = false;
			}
			if (_surfer.groundObject == null)
			{
				_surfer.moveData.velocity.y -= _surfer.moveData.gravityFactor * _config.gravity * _deltaTime;
				_surfer.moveData.velocity.y += _surfer.baseVelocity.y * _deltaTime;
			}
			CheckGrounded();
			CalculateMovementVelocity();
		}
		else
		{
			UnderwaterPhysics();
		}
		float y = _surfer.moveData.velocity.y;
		_surfer.moveData.velocity.y = 0f;
		_surfer.moveData.velocity = Vector3.ClampMagnitude(_surfer.moveData.velocity, _config.maxVelocity);
		speed = _surfer.moveData.velocity.magnitude;
		_surfer.moveData.velocity.y = y;
		if (_surfer.moveData.velocity.sqrMagnitude == 0f)
		{
			SurfPhysics.ResolveCollisions(_surfer.collider, ref _surfer.moveData.origin, ref _surfer.moveData.velocity, _surfer.moveData.rigidbodyPushForce, 1f, _surfer.moveData.stepOffset, _surfer);
		}
		else
		{
			float a = 0.2f;
			Vector3 vector = _surfer.moveData.velocity * _deltaTime;
			float num = vector.magnitude;
			float num2 = num;
			while (num > 0f)
			{
				float num3 = Mathf.Min(a, num);
				num -= num3;
				Vector3 vector2 = vector * (num3 / num2);
				_surfer.moveData.origin += vector2;
				SurfPhysics.ResolveCollisions(_surfer.collider, ref _surfer.moveData.origin, ref _surfer.moveData.velocity, _surfer.moveData.rigidbodyPushForce, num3 / num2, _surfer.moveData.stepOffset, _surfer);
			}
		}
		_surfer.moveData.groundedTemp = _surfer.moveData.grounded;
		_surfer = null;
	}

	private void CalculateMovementVelocity()
	{
		if (_surfer.moveType != MoveType.Walk)
		{
			return;
		}
		if (_surfer.groundObject == null)
		{
			wasSliding = false;
			_surfer.moveData.velocity += AirInputMovement();
			SurfPhysics.Reflect(ref _surfer.moveData.velocity, _surfer.collider, _surfer.moveData.origin, _deltaTime);
			return;
		}
		if (!wasSliding)
		{
			slideDirection = new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z).normalized;
			slideSpeedCurrent = Mathf.Max(_config.maximumSlideSpeed, new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z).magnitude);
		}
		if (_surfer.moveData.velocity.magnitude > _config.minimumSlideSpeed && _surfer.moveData.slidingEnabled && _surfer.moveData.crouching && slideDelay <= 0f)
		{
			if (!wasSliding)
			{
				slideSpeedCurrent = Mathf.Clamp(slideSpeedCurrent * _config.slideSpeedMultiplier, _config.minimumSlideSpeed, _config.maximumSlideSpeed);
			}
			wasSliding = true;
			SlideMovement();
			return;
		}
		if (slideDelay > 0f)
		{
			slideDelay -= _deltaTime;
		}
		if (wasSliding)
		{
			slideDelay = _config.slideDelay;
		}
		wasSliding = false;
		if (!crouching)
		{
			_ = _config.friction;
		}
		else
		{
			_ = _config.crouchFriction;
		}
		float num = (crouching ? _config.crouchAcceleration : _config.acceleration);
		if (!crouching)
		{
			_ = _config.deceleration;
		}
		else
		{
			_ = _config.crouchDeceleration;
		}
		Vector3 vector = Vector3.Cross(groundNormal, -playerTransform.right);
		Vector3 vector2 = Vector3.Cross(groundNormal, vector);
		float num2 = (_surfer.moveData.sprinting ? _config.sprintSpeed : _config.walkSpeed);
		if (crouching)
		{
			num2 = _config.crouchSpeed;
		}
		if (_surfer.moveData.wishJump)
		{
			ApplyFriction(0f, yAffected: true, grounded: true);
			Jump();
			return;
		}
		ApplyFriction(1f * frictionMult, yAffected: true, grounded: true);
		float verticalAxis = _surfer.moveData.verticalAxis;
		float horizontalAxis = _surfer.moveData.horizontalAxis;
		Vector3 wishDir = verticalAxis * vector + horizontalAxis * vector2;
		wishDir.Normalize();
		Vector3 vector3 = Vector3.Cross(groundNormal, Quaternion.AngleAxis(-90f, Vector3.up) * new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z));
		float magnitude = wishDir.magnitude;
		magnitude *= num2;
		float y = _surfer.moveData.velocity.y;
		Accelerate(wishDir, magnitude, num * Mathf.Min(frictionMult, 1f), yMovement: false);
		float maxVelocity = _config.maxVelocity;
		_surfer.moveData.velocity = Vector3.ClampMagnitude(new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z), maxVelocity);
		_surfer.moveData.velocity.y = y;
		float num3 = vector3.normalized.y * new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z).magnitude;
		_surfer.moveData.velocity.y = num3 * ((wishDir.y < 0f) ? 1.2f : 1f);
		_ = _surfer.moveData.velocity;
	}

	private void UnderwaterPhysics()
	{
		_surfer.moveData.velocity = Vector3.Lerp(_surfer.moveData.velocity, Vector3.zero, _config.underwaterVelocityDampening * _deltaTime);
		if (!CheckGrounded())
		{
			_surfer.moveData.velocity.y -= _config.underwaterGravity * _deltaTime;
		}
		if (Input.GetKey(up))
		{
			_surfer.moveData.velocity.y += _config.swimUpSpeed * _deltaTime;
		}
		_ = _config.underwaterFriction;
		float underwaterAcceleration = _config.underwaterAcceleration;
		_ = _config.underwaterDeceleration;
		ApplyFriction(1f, yAffected: true, grounded: false);
		Vector3 vector = Vector3.Cross(groundNormal, -playerTransform.right);
		Vector3 vector2 = Vector3.Cross(groundNormal, vector);
		float underwaterSwimSpeed = _config.underwaterSwimSpeed;
		float verticalAxis = _surfer.moveData.verticalAxis;
		float horizontalAxis = _surfer.moveData.horizontalAxis;
		Vector3 wishDir = verticalAxis * vector + horizontalAxis * vector2;
		wishDir.Normalize();
		Vector3 vector3 = Vector3.Cross(groundNormal, Quaternion.AngleAxis(-90f, Vector3.up) * new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z));
		float magnitude = wishDir.magnitude;
		magnitude *= underwaterSwimSpeed;
		float y = _surfer.moveData.velocity.y;
		Accelerate(wishDir, magnitude, underwaterAcceleration, yMovement: false);
		float maxVelocity = _config.maxVelocity;
		_surfer.moveData.velocity = Vector3.ClampMagnitude(new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z), maxVelocity);
		_surfer.moveData.velocity.y = y;
		float y2 = _surfer.moveData.velocity.y;
		_surfer.moveData.velocity.y = 0f;
		float b = vector3.normalized.y * new Vector3(_surfer.moveData.velocity.x, 0f, _surfer.moveData.velocity.z).magnitude;
		_surfer.moveData.velocity.y = Mathf.Min(Mathf.Max(0f, b) + y2, underwaterSwimSpeed);
		bool flag = playerTransform.InverseTransformVector(_surfer.moveData.velocity).z > 0f;
		Trace trace = TraceBounds(playerTransform.position, playerTransform.position + playerTransform.forward * 0.1f, SurfPhysics.groundLayerMask);
		if (trace.hitCollider != null && Vector3.Angle(Vector3.up, trace.planeNormal) >= _config.slopeLimit && Input.GetButton("Jump") && !_surfer.moveData.cameraUnderwater && flag)
		{
			_surfer.moveData.velocity.y = Mathf.Max(_surfer.moveData.velocity.y, _config.jumpForce);
		}
	}

	private void LadderCheck(Vector3 colliderScale, Vector3 direction)
	{
		if (_surfer.moveData.velocity.sqrMagnitude <= 0f)
		{
			return;
		}
		bool flag = false;
		RaycastHit[] array = Physics.BoxCastAll(_surfer.moveData.origin, Vector3.Scale(_surfer.collider.bounds.size * 0.5f, colliderScale), Vector3.Scale(direction, new Vector3(1f, 0f, 1f)), Quaternion.identity, direction.magnitude, SurfPhysics.groundLayerMask, QueryTriggerInteraction.Collide);
		for (int i = 0; i < array.Length; i++)
		{
			RaycastHit raycastHit = array[i];
			if (!(raycastHit.transform.GetComponentInParent<Ladder>() != null))
			{
				continue;
			}
			bool flag2 = true;
			float num = Vector3.Angle(Vector3.up, raycastHit.normal);
			if (_surfer.moveData.angledLaddersEnabled)
			{
				if (raycastHit.normal.y < 0f)
				{
					flag2 = false;
				}
				else if (num <= _surfer.moveData.slopeLimit)
				{
					flag2 = false;
				}
			}
			else if (raycastHit.normal.y != 0f)
			{
				flag2 = false;
			}
			if (!flag2)
			{
				continue;
			}
			flag = true;
			if (!_surfer.moveData.climbingLadder)
			{
				_surfer.moveData.climbingLadder = true;
				_surfer.moveData.ladderNormal = raycastHit.normal;
				_surfer.moveData.ladderDirection = -raycastHit.normal * direction.magnitude * 2f;
				if (_surfer.moveData.angledLaddersEnabled)
				{
					Vector3 vector = raycastHit.normal;
					vector.y = 0f;
					vector = Quaternion.AngleAxis(-90f, Vector3.up) * vector;
					_surfer.moveData.ladderClimbDir = Quaternion.AngleAxis(90f, vector) * raycastHit.normal;
					_surfer.moveData.ladderClimbDir *= 1f / _surfer.moveData.ladderClimbDir.y;
				}
				else
				{
					_surfer.moveData.ladderClimbDir = Vector3.up;
				}
			}
		}
		if (!flag)
		{
			_surfer.moveData.ladderNormal = Vector3.zero;
			_surfer.moveData.ladderVelocity = Vector3.zero;
			_surfer.moveData.climbingLadder = false;
			_surfer.moveData.ladderClimbDir = Vector3.up;
		}
	}

	private void LadderPhysics()
	{
		_surfer.moveData.ladderVelocity = _surfer.moveData.ladderClimbDir * _surfer.moveData.verticalAxis * 6f;
		_surfer.moveData.velocity = Vector3.Lerp(_surfer.moveData.velocity, _surfer.moveData.ladderVelocity, Time.deltaTime * 10f);
		LadderCheck(Vector3.one, _surfer.moveData.ladderDirection);
		Trace trace = TraceToFloor();
		if (_surfer.moveData.verticalAxis < 0f && trace.hitCollider != null && Vector3.Angle(Vector3.up, trace.planeNormal) <= _surfer.moveData.slopeLimit)
		{
			_surfer.moveData.velocity = _surfer.moveData.ladderNormal * 0.5f;
			_surfer.moveData.ladderVelocity = Vector3.zero;
			_surfer.moveData.climbingLadder = false;
		}
		if (_surfer.moveData.wishJump)
		{
			_surfer.moveData.velocity = _surfer.moveData.ladderNormal * 4f;
			_surfer.moveData.ladderVelocity = Vector3.zero;
			_surfer.moveData.climbingLadder = false;
		}
	}

	public void Accelerate(Vector3 wishDir, float wishSpeed, float acceleration, bool yMovement)
	{
		float num = Vector3.Dot(_surfer.moveData.velocity, wishDir);
		float num2 = wishSpeed - num;
		if (!(num2 <= 0f))
		{
			float num3 = Mathf.Min(acceleration * _deltaTime * wishSpeed, num2);
			_surfer.moveData.velocity.x += num3 * wishDir.x;
			if (yMovement)
			{
				_surfer.moveData.velocity.y += num3 * wishDir.y;
			}
			_surfer.moveData.velocity.z += num3 * wishDir.z;
		}
	}

	private void ApplyFriction(float t, bool yAffected, bool grounded)
	{
		Vector3 velocity = _surfer.moveData.velocity;
		velocity.y = 0f;
		float magnitude = velocity.magnitude;
		float num = 0f;
		float num2 = (crouching ? _config.crouchFriction : _config.friction);
		if (!crouching)
		{
			_ = _config.acceleration;
		}
		else
		{
			_ = _config.crouchAcceleration;
		}
		float num3 = (crouching ? _config.crouchDeceleration : _config.deceleration);
		if (grounded)
		{
			velocity.y = _surfer.moveData.velocity.y;
			num = ((magnitude < num3) ? num3 : magnitude) * num2 * _deltaTime * t;
		}
		float num4 = Mathf.Max(magnitude - num, 0f);
		if (magnitude > 0f)
		{
			num4 /= magnitude;
		}
		_surfer.moveData.velocity.x *= num4;
		if (yAffected)
		{
			_surfer.moveData.velocity.y *= num4;
		}
		_surfer.moveData.velocity.z *= num4;
	}

	private Vector3 AirInputMovement()
	{
		GetWishValues(out var wishVel, out var wishDir, out var wishSpeed);
		if (_config.clampAirSpeed && wishSpeed != 0f && wishSpeed > _config.maxSpeed)
		{
			wishVel *= _config.maxSpeed / wishSpeed;
			wishSpeed = _config.maxSpeed;
		}
		return SurfPhysics.AirAccelerate(_surfer.moveData.velocity, wishDir, wishSpeed, _config.airAcceleration, _config.airCap, _deltaTime);
	}

	private void GetWishValues(out Vector3 wishVel, out Vector3 wishDir, out float wishSpeed)
	{
		wishVel = Vector3.zero;
		wishDir = Vector3.zero;
		wishSpeed = 0f;
		Vector3 forward = _surfer.forward;
		Vector3 right = _surfer.right;
		forward[1] = 0f;
		right[1] = 0f;
		forward.Normalize();
		right.Normalize();
		for (int i = 0; i < 3; i++)
		{
			wishVel[i] = forward[i] * _surfer.moveData.forwardMove + right[i] * _surfer.moveData.sideMove;
		}
		wishVel[1] = 0f;
		wishSpeed = wishVel.magnitude;
		wishDir = wishVel.normalized;
	}

	private void Jump()
	{
		if (!_config.autoBhop)
		{
			_surfer.moveData.wishJump = false;
		}
		_surfer.moveData.velocity.y += _config.jumpForce;
		jumping = true;
	}

	public bool CheckGrounded()
	{
		_surfer.moveData.surfaceFriction = 1f;
		bool flag = _surfer.moveData.velocity.y > 0f;
		Trace trace = TraceToFloor();
		float num = Vector3.Angle(Vector3.up, trace.planeNormal);
		if (trace.hitCollider == null || num > _config.slopeLimit || (jumping && _surfer.moveData.velocity.y > 0f))
		{
			SetGround(null);
			if (flag && _surfer.moveType != MoveType.Noclip)
			{
				_surfer.moveData.surfaceFriction = _config.airFriction;
			}
			return false;
		}
		groundNormal = trace.planeNormal;
		SetGround(trace.hitCollider.gameObject);
		return true;
	}

	private void SetGround(GameObject obj)
	{
		if (obj != null)
		{
			_surfer.groundObject = obj;
			_surfer.moveData.velocity.y = 0f;
		}
		else
		{
			_surfer.groundObject = null;
		}
	}

	private Trace TraceBounds(Vector3 start, Vector3 end, int layerMask)
	{
		return Tracer.TraceCollider(_surfer.collider, start, end, layerMask);
	}

	private Trace TraceToFloor()
	{
		Vector3 origin = _surfer.moveData.origin;
		origin.y -= 0.15f;
		return Tracer.TraceCollider(_surfer.collider, _surfer.moveData.origin, origin, SurfPhysics.groundLayerMask);
	}

	public void Crouch(ISurfControllable surfer, MovementConfig config, float deltaTime)
	{
		_surfer = surfer;
		_config = config;
		_deltaTime = deltaTime;
		if (_surfer == null || _surfer.collider == null)
		{
			return;
		}
		bool flag = _surfer.groundObject != null;
		bool flag2 = _surfer.moveData.crouching;
		float num = Mathf.Clamp(_surfer.moveData.crouchingHeight, 0.01f, 1f);
		float num2 = _surfer.moveData.defaultHeight - _surfer.moveData.defaultHeight * num;
		if (flag)
		{
			uncrouchDown = false;
		}
		if (flag)
		{
			crouchLerp = Mathf.Lerp(crouchLerp, flag2 ? 1f : 0f, _deltaTime * _surfer.moveData.crouchingSpeed);
		}
		else if (!flag && !flag2 && crouchLerp < 0.95f)
		{
			crouchLerp = 0f;
		}
		else if (!flag && flag2)
		{
			crouchLerp = 1f;
		}
		if (crouchLerp > 0.9f && !crouching)
		{
			crouching = true;
			if (_surfer.collider.GetType() == typeof(BoxCollider))
			{
				BoxCollider boxCollider = (BoxCollider)_surfer.collider;
				boxCollider.size = new Vector3(boxCollider.size.x, _surfer.moveData.defaultHeight * num, boxCollider.size.z);
			}
			else if (_surfer.collider.GetType() == typeof(CapsuleCollider))
			{
				((CapsuleCollider)_surfer.collider).height = _surfer.moveData.defaultHeight * num;
			}
			_surfer.moveData.origin += num2 / 2f * (flag ? Vector3.down : Vector3.up);
			foreach (Transform item in playerTransform)
			{
				if (!(item == _surfer.moveData.viewTransform))
				{
					item.localPosition = new Vector3(item.localPosition.x, item.localPosition.y * num, item.localPosition.z);
				}
			}
			uncrouchDown = !flag;
		}
		else if (crouching)
		{
			bool flag3 = true;
			if (_surfer.collider.GetType() == typeof(BoxCollider))
			{
				BoxCollider boxCollider2 = (BoxCollider)_surfer.collider;
				Vector3 extents = boxCollider2.size * 0.5f;
				Vector3 position = boxCollider2.transform.position;
				Vector3 destination = boxCollider2.transform.position + (uncrouchDown ? Vector3.down : Vector3.up) * num2;
				if (Tracer.TraceBox(position, destination, extents, boxCollider2.contactOffset, SurfPhysics.groundLayerMask).hitCollider != null)
				{
					flag3 = false;
				}
			}
			else if (_surfer.collider.GetType() == typeof(CapsuleCollider))
			{
				CapsuleCollider capsuleCollider = (CapsuleCollider)_surfer.collider;
				if (Tracer.TraceCapsule(capsuleCollider.center + Vector3.up * capsuleCollider.height * 0.5f, capsuleCollider.center + Vector3.down * capsuleCollider.height * 0.5f, start: capsuleCollider.transform.position, destination: capsuleCollider.transform.position + (uncrouchDown ? Vector3.down : Vector3.up) * num2, radius: capsuleCollider.radius, contactOffset: capsuleCollider.contactOffset, layerMask: SurfPhysics.groundLayerMask).hitCollider != null)
				{
					flag3 = false;
				}
			}
			if (flag3 && crouchLerp <= 0.9f)
			{
				crouching = false;
				if (_surfer.collider.GetType() == typeof(BoxCollider))
				{
					BoxCollider boxCollider3 = (BoxCollider)_surfer.collider;
					boxCollider3.size = new Vector3(boxCollider3.size.x, _surfer.moveData.defaultHeight, boxCollider3.size.z);
				}
				else if (_surfer.collider.GetType() == typeof(CapsuleCollider))
				{
					((CapsuleCollider)_surfer.collider).height = _surfer.moveData.defaultHeight;
				}
				_surfer.moveData.origin += num2 / 2f * (uncrouchDown ? Vector3.down : Vector3.up);
				foreach (Transform item2 in playerTransform)
				{
					item2.localPosition = new Vector3(item2.localPosition.x, item2.localPosition.y / num, item2.localPosition.z);
				}
			}
			if (!flag3)
			{
				crouchLerp = 1f;
			}
		}
		if (!crouching)
		{
			_surfer.moveData.viewTransform.localPosition = Vector3.Lerp(_surfer.moveData.viewTransformDefaultLocalPos, _surfer.moveData.viewTransformDefaultLocalPos * num + Vector3.down * num2 * 0.5f, crouchLerp);
		}
		else
		{
			_surfer.moveData.viewTransform.localPosition = Vector3.Lerp(_surfer.moveData.viewTransformDefaultLocalPos - Vector3.down * num2 * 0.5f, _surfer.moveData.viewTransformDefaultLocalPos * num, crouchLerp);
		}
	}

	private void SlideMovement()
	{
		slideDirection += new Vector3(groundNormal.x, 0f, groundNormal.z) * slideSpeedCurrent * _deltaTime;
		slideDirection = slideDirection.normalized;
		Vector3 vector = Vector3.Cross(groundNormal, Quaternion.AngleAxis(-90f, Vector3.up) * slideDirection);
		slideSpeedCurrent -= _config.slideFriction * _deltaTime;
		slideSpeedCurrent = Mathf.Clamp(slideSpeedCurrent, 0f, _config.maximumSlideSpeed);
		slideSpeedCurrent -= (vector * slideSpeedCurrent).y * _deltaTime * _config.downhillSlideSpeedMultiplier;
		_surfer.moveData.velocity = vector * slideSpeedCurrent;
		if (_surfer.moveData.wishJump && slideSpeedCurrent < _config.minimumSlideSpeed * _config.slideSpeedMultiplier)
		{
			Jump();
		}
	}
}
