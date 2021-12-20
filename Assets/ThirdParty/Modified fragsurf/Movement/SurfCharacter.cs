// Fragsurf.Movement.SurfCharacter
using System.Collections.Generic;
using Fragsurf.Movement;
using Unity.Netcode;
using UnityEngine;

[AddComponentMenu("Fragsurf/Surf Character")]
public class SurfCharacter : NetworkBehaviour, ISurfControllable
{
	public enum ColliderType
	{
		Capsule,
		Box
	}

	public float maxVectorLenth;

	private float hor;

	private float ver;

	private KeyCode Forward = KeyCode.W;

	private KeyCode Left = KeyCode.A;

	private KeyCode Back = KeyCode.S;

	private KeyCode Right = KeyCode.D;

	private KeyCode Shift = KeyCode.LeftShift;

	private KeyCode Ctrl = KeyCode.LeftControl;

	private KeyCode Space = KeyCode.Space;

	[Header("Physics Settings")]
	public Vector3 colliderSize = new Vector3(1f, 2f, 1f);

	public float weight = 75f;

	public float rigidbodyPushForce = 2f;

	public bool solidCollider;

	[Header("View Settings")]
	public Transform viewTransform;

	public Transform playerRotationTransform;

	[Header("Crouching setup")]
	public float crouchingHeightMultiplier = 0.5f;

	public float crouchingSpeed = 10f;

	private float defaultHeight;

	private bool allowCrouch = true;

	[Header("Features")]
	public bool crouchingEnabled = true;

	public bool slidingEnabled;

	public bool laddersEnabled = true;

	public bool supportAngledLadders = true;

	[Header("Step offset (can be buggy, enable at your own risk)")]
	public bool useStepOffset;

	public float stepOffset = 0.35f;

	[SerializeField]
	[Header("Movement Config")]
	public MovementConfig movementConfig;

	private GameObject _groundObject;

	private Collider _collider;

	private Vector3 _angles;

	private Vector3 _startPosition;

	private GameObject _colliderObject;

	private GameObject _cameraWaterCheckObject;

	private CameraWaterCheck _cameraWaterCheck;

	private MoveData _moveData = new MoveData();

	private SurfController _controller = new SurfController();

	private Rigidbody rb;

	private List<Collider> triggers = new List<Collider>();

	private int numberOfTriggers;

	private bool underwater;

	private Vector3 prevPosition;

	[HideInInspector]
	public ColliderType collisionType => ColliderType.Box;

	public MoveType moveType => MoveType.Walk;

	public MovementConfig moveConfig => movementConfig;

	public MoveData moveData => _moveData;

	public Collider collider => _collider;

	public GameObject groundObject
	{
		get
		{
			return _groundObject;
		}
		set
		{
			_groundObject = value;
		}
	}

	public Vector3 baseVelocity => _moveData.velocity;

	public Vector3 forward => viewTransform.forward;

	public Vector3 right => viewTransform.right;

	public Vector3 up => viewTransform.up;

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(base.transform.position, colliderSize);
	}

	private void Awake()
	{
		_controller.playerTransform = playerRotationTransform;
		if (viewTransform != null)
		{
			_controller.camera = viewTransform;
			_controller.cameraYPos = viewTransform.localPosition.y;
		}
	}

	private void Start()
	{
		if (!base.IsOwner)
		{
			GetComponentInChildren<Camera>().gameObject.SetActive(value: false);
			base.enabled = false;
			return;
		}
		_colliderObject = new GameObject("PlayerCollider");
		_colliderObject.layer = base.gameObject.layer;
		_colliderObject.transform.SetParent(base.transform);
		_colliderObject.transform.rotation = Quaternion.identity;
		_colliderObject.transform.localPosition = Vector3.zero;
		_colliderObject.transform.SetSiblingIndex(0);
		_cameraWaterCheckObject = new GameObject("Camera water check");
		_cameraWaterCheckObject.layer = LayerMask.NameToLayer("Ignore Raycast");
		_cameraWaterCheckObject.transform.position = viewTransform.position;
		Rigidbody rigidbody = _cameraWaterCheckObject.AddComponent<Rigidbody>();
		rigidbody.useGravity = false;
		rigidbody.isKinematic = true;
		SphereCollider sphereCollider = _cameraWaterCheckObject.AddComponent<SphereCollider>();
		sphereCollider.radius = 0.1f;
		sphereCollider.isTrigger = true;
		_cameraWaterCheck = _cameraWaterCheckObject.AddComponent<CameraWaterCheck>();
		prevPosition = base.transform.position;
		if (viewTransform == null)
		{
			viewTransform = Camera.main.transform;
		}
		if (playerRotationTransform == null && base.transform.childCount > 0)
		{
			playerRotationTransform = base.transform.GetChild(0);
		}
		_collider = base.gameObject.GetComponent<Collider>();
		if (_collider != null)
		{
			Object.Destroy(_collider);
		}
		rb = base.gameObject.GetComponent<Rigidbody>();
		if (rb == null)
		{
			rb = base.gameObject.AddComponent<Rigidbody>();
		}
		allowCrouch = crouchingEnabled;
		rb.isKinematic = true;
		rb.useGravity = false;
		rb.angularDrag = 0f;
		rb.drag = 0f;
		rb.mass = weight;
		switch (collisionType)
		{
		case ColliderType.Box:
		{
			_collider = _colliderObject.AddComponent<BoxCollider>();
			BoxCollider boxCollider = (BoxCollider)_collider;
			boxCollider.size = colliderSize;
			defaultHeight = boxCollider.size.y;
			break;
		}
		case ColliderType.Capsule:
		{
			_collider = _colliderObject.AddComponent<CapsuleCollider>();
			CapsuleCollider capsuleCollider = (CapsuleCollider)_collider;
			capsuleCollider.height = colliderSize.y;
			capsuleCollider.radius = colliderSize.x / 2f;
			defaultHeight = capsuleCollider.height;
			break;
		}
		}
		_moveData.slopeLimit = movementConfig.slopeLimit;
		_moveData.rigidbodyPushForce = rigidbodyPushForce;
		_moveData.slidingEnabled = slidingEnabled;
		_moveData.laddersEnabled = laddersEnabled;
		_moveData.angledLaddersEnabled = supportAngledLadders;
		_moveData.playerTransform = base.transform;
		_moveData.viewTransform = viewTransform;
		_moveData.viewTransformDefaultLocalPos = viewTransform.localPosition;
		_moveData.defaultHeight = defaultHeight;
		_moveData.crouchingHeight = crouchingHeightMultiplier;
		_moveData.crouchingSpeed = crouchingSpeed;
		_collider.isTrigger = !solidCollider;
		_moveData.origin = base.transform.position;
		_startPosition = base.transform.position;
		_moveData.useStepOffset = useStepOffset;
		_moveData.stepOffset = stepOffset;
		
	}

	private void Update()
	{
		if (!base.IsOwner)
		{
			GetComponentInChildren<PlayerAiming>().gameObject.SetActive(value: false);
			base.enabled = false;
			return;
		}
		_colliderObject.transform.rotation = Quaternion.identity;
		UpdateMoveData();
		Vector3 vector = base.transform.position - prevPosition;
		base.transform.position = prevPosition;
		moveData.origin += vector;
		if (numberOfTriggers != triggers.Count)
		{
			numberOfTriggers = triggers.Count;
			underwater = false;
			triggers.RemoveAll((Collider item) => item == null);
			foreach (Collider trigger in triggers)
			{
				if (!(trigger == null) && (bool)trigger.GetComponentInParent<Water>())
				{
					underwater = true;
				}
			}
		}
		_moveData.cameraUnderwater = _cameraWaterCheck.IsUnderwater();
		_cameraWaterCheckObject.transform.position = viewTransform.position;
		moveData.underwater = underwater;
		if (allowCrouch)
		{
			_controller.Crouch(this, movementConfig, Time.deltaTime);
		}
		_controller.ProcessMovement(this, movementConfig, Time.deltaTime);
		base.transform.position = moveData.origin;
		prevPosition = base.transform.position;
		_colliderObject.transform.rotation = Quaternion.identity;
	}

	private void UpdateMoveData()
	{
		if (Input.GetKey(Forward))
		{
			ver = 1f;
		}
		else if (Input.GetKey(Back))
		{
			ver = -1f;
		}
		else
		{
			ver = 0f;
		}
		if (Input.GetKey(Left))
		{
			hor = -1f;
		}
		else if (Input.GetKey(Right))
		{
			hor = 1f;
		}
		else
		{
			hor = 0f;
		}
		_moveData.verticalAxis = ver;
		_moveData.horizontalAxis = hor;
		_moveData.sprinting = Input.GetKey(Shift);
		if (Input.GetKey(Ctrl))
		{
			_moveData.crouching = true;
		}
		if (!Input.GetKey(Ctrl))
		{
			_moveData.crouching = false;
		}
		bool flag = _moveData.horizontalAxis < 0f;
		bool flag2 = _moveData.horizontalAxis > 0f;
		bool flag3 = _moveData.verticalAxis > 0f;
		bool flag4 = _moveData.verticalAxis < 0f;
		Input.GetKey(Space);
		if (!flag && !flag2)
		{
			_moveData.sideMove = 0f;
		}
		else if (flag)
		{
			_moveData.sideMove = 0f - moveConfig.acceleration;
		}
		else if (flag2)
		{
			_moveData.sideMove = moveConfig.acceleration;
		}
		if (!flag3 && !flag4)
		{
			_moveData.forwardMove = 0f;
		}
		else if (flag3)
		{
			_moveData.forwardMove = moveConfig.acceleration;
		}
		else if (flag4)
		{
			_moveData.forwardMove = 0f - moveConfig.acceleration;
		}
		if (Input.GetKeyDown(Space))
		{
			_moveData.wishJump = true;
		}
		if (!Input.GetKey(Space))
		{
			_moveData.wishJump = false;
		}
		_moveData.viewAngles = _angles;
	}

	private void DisableInput()
	{
		_moveData.verticalAxis = 0f;
		_moveData.horizontalAxis = 0f;
		_moveData.sideMove = 0f;
		_moveData.forwardMove = 0f;
		_moveData.wishJump = false;
	}

	public static float ClampAngle(float angle, float from, float to)
	{
		if (angle < 0f)
		{
			angle = 360f + angle;
		}
		if (angle > 180f)
		{
			return Mathf.Max(angle, 360f + from);
		}
		return Mathf.Min(angle, to);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!triggers.Contains(other))
		{
			triggers.Add(other);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (triggers.Contains(other))
		{
			triggers.Remove(other);
		}
	}

	private void OnCollisionStay(Collision collision)
	{
		if (!(collision.rigidbody == null))
		{
			Vector3 vector = collision.relativeVelocity * collision.rigidbody.mass / 50f;
			Vector3 vector2 = new Vector3(vector.x * 0.0025f, vector.y * 0.00025f, vector.z * 0.0025f);
			float num = Mathf.Max(moveData.velocity.y, 10f);
			Vector3 vector3 = new Vector3(moveData.velocity.x + vector2.x, Mathf.Clamp(moveData.velocity.y + Mathf.Clamp(vector2.y, -0.5f, 0.5f), 0f - num, num), moveData.velocity.z + vector2.z);
			vector3 = Vector3.ClampMagnitude(vector3, Mathf.Max(moveData.velocity.magnitude, 30f));
			moveData.velocity = vector3;
		}
	}
}
