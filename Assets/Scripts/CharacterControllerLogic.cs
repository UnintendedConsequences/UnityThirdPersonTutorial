using UnityEngine;
using System.Collections;

public class CharacterControllerLogic : MonoBehaviour {

	#region Variables (private)

	// Inspector Serialized
	[SerializeField]
	private Animator animator;
	[SerializeField]
	private float directionDampTime = .25f;
	[SerializeField]
	private ThirdPersonCamera gamecam;
	[SerializeField]
	private float directionSpeed = 3.0f;
	[SerializeField]
	private float rotationDegreePerSecond = 120f;

	// private global variables only
	private float speed = 0.0f;
	private float direction = 0f;
	private float horizontal = 0.0f;
	private float vertical = 0.0f;
	private AnimatorStateInfo stateInfo;

	// Hashes
	private int m_LocomotionId = 0;

	#endregion


	#region Properties (public)

	public Animator Animator
	{
		get
		{
			return this.animator;
		}
	}

	#endregion


	#region Unity Event Functions

	// Use this for initialization
	void Start () 
	{
		animator = GetComponent<Animator>();

		if(animator.layerCount >= 2)
		{
			animator.SetLayerWeight(1,1);
		}

		// Hash all animation names for prefrence
		m_LocomotionId = Animator.StringToHash("Base Layer.Locomotion");
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(animator && gamecam.CamState != ThirdPersonCamera.CamStates.FirstPerson)
		{
			stateInfo = animator.GetCurrentAnimatorStateInfo(0);

			// Pull values from controller/keyboard
			horizontal = Input.GetAxis("Horizontal");
			vertical = Input.GetAxis("Vertical");

			/* The below speed is the same as the one used.
			//speed = h * h + v * v;
			speed = new Vector2(vertical, horizontal).sqrMagnitude;*/

			// Translate control stick coordinates into world/cam/character space
			// ref = taking a value, modify it and turn it back. (Its like "Puesdo returning 2 values".)
			StickToWorldspace(this.transform, gamecam.transform, ref direction, ref speed);

			// This is taking the speed values from the script and putting them in our character controller.
			animator.SetFloat("Speed", speed);
			//animator.SetFloat("Direction", horizontal, directionDampTime, Time.deltaTime);
			animator.SetFloat("Direction", direction, directionDampTime, Time.deltaTime);

		}

	}

	// Any code that moves the character needs to be checked against physics
	void FixedUpdate()
	{
		// Rotate character model if stick is tilted right or left, but only if character is moving in that direction
		if(IsInLocomotion() && ((direction >= 0 && horizontal >= 0) || (direction < 0 && horizontal < 0)))
		{
			Vector3 rotationAmount = Vector3.Lerp (Vector3.zero, new Vector3(0f, rotationDegreePerSecond * (horizontal < 0f ? -1f : 1f), 0f), Mathf.Abs(horizontal));
			Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
			this.transform.rotation = (this.transform.rotation * deltaRotation);
		}
	}

	#endregion

	#region Methods

	public void StickToWorldspace(Transform root, Transform camera, ref float directionOut, ref float speedOut)
	{
		Vector3 rootDirection = root.forward;
		
		Vector3 stickDirection = new Vector3(horizontal, 0, vertical);
		
		speedOut = stickDirection.sqrMagnitude;
		
		// Get camera rotation
		Vector3 CameraDirection = camera.forward;
		CameraDirection.y = 0.0f;
		Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, CameraDirection);
		
		// Convert joydstick input in Worldspace coordinates
		Vector3 moveDirection = referentialShift * stickDirection;
		Vector3 axisSign = Vector3.Cross(moveDirection, rootDirection);
		
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), moveDirection, Color.green);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), axisSign, Color.red);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), rootDirection, Color.magenta);
		Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), stickDirection, Color.blue);
		
		float angleRootToMove = Vector3.Angle(rootDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);
		
		angleRootToMove /= 100;
		
		directionOut = angleRootToMove * directionSpeed;
	}

	public bool IsInLocomotion()
	{
		return stateInfo.nameHash == m_LocomotionId;
	}

	#endregion












}
