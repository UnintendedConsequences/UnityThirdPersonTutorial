using UnityEngine;
using System.Collections;


// Struct to hold data for aligning camera
// Struct: a collection of values that we store our character position in.
struct CameraPosition
{
	// Position to align camera to, probably somewhere behind the characer
	// or position to point camera at, probably somewhere along character's axis
	private Vector3 position;
	// Transform used for any rotation
	private Transform xForm;

	public Vector3 Position {get {return position; } set {position = value; }}
	public Transform XForm {get { return xForm; } set { xForm = value; }}

	public void Init(string camName, Vector3 pos, Transform transform, Transform parent)
	{
		position = pos;
		xForm = transform;
		xForm.name = camName;
		xForm.parent = parent;
		xForm.localPosition = Vector3.zero;
		xForm.localPosition = position;
	}

}


[RequireComponent (typeof (BarsEffect))]
public class ThirdPersonCamera : MonoBehaviour 
{

	#region Variables (private)

	[SerializeField]
	private float distanceAway;
	[SerializeField]
	private float distanceUp;
	[SerializeField]
	private float smooth;
	[SerializeField]
	private CharacterControllerLogic follow;
	[SerializeField]
	private Transform followXForm;
	[SerializeField]
	private float widescreen = 0.2f;
	[SerializeField]
	private float targetingTime = 0.5f;
	[SerializeField]
	private float firstPersonThreshold = 0.5f;
	[SerializeField]
	private float firstPersonLookSpeed = 1.5f;
	[SerializeField]
	private Vector2 firstPersonXAxisClamp = new Vector2(-70.0f, 90.0f);
	[SerializeField]
	private float fPSRotationDegreePerSecond = 120f;

	// Smoothing and Dampening
	private Vector3 velocityCamSmooth = Vector3.zero;
	[SerializeField]
	private float camSmoothDampTime = 0.1f;


	// private global only
	private Vector3 lookDir;
	private Vector3 targetPosition;
	private BarsEffect barEffect;
	private CamStates camState = CamStates.Behind;
	private float xAxisRot = 0.0f;
	private CameraPosition firstPersonCamPos;
	private float lookWeight;
	private const float TARGETING_THRESHOLD = 0.01f;


	#endregion
	
	
	#region Properties (public)

	public CamStates CamState
	{
		get
		{
			return camState;
		}
	}

	public enum CamStates
	{
		Behind,
		FirstPerson,
		Target,
		Free
	}

	#endregion
	
	
	#region Unity Event Functions
	
	// Use this for initialization
	void Start () 
	{
		follow = GameObject.FindWithTag("Player").GetComponent<CharacterControllerLogic>();
		followXForm = GameObject.FindWithTag("Player").transform;
		lookDir = followXForm.forward;

		barEffect = GetComponent<BarsEffect>();
		if(barEffect == null)
		{
			Debug.LogError("Attach a widescreen BarsEffect script to the camera.", this);
		}

		// Position and parent a GameObject where first person view should be.
		firstPersonCamPos = new CameraPosition();
		firstPersonCamPos.Init
			(
				"First Person Camera",
				new Vector3(0.0f, 1.6f, 0.2f),
				new GameObject().transform,
				followXForm
			);
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	void OnDrawGizmos()
	{

	}

	// Late update is useful for camera stuff because it will happen after you've positioned all your objects.
	void LateUpdate()
	{
		// Pull valuesfrom controller/keyboard
		float rightX = Input.GetAxis("RightStickX");
		float rightY = Input.GetAxis("RightStickY"); 
		float leftX = Input.GetAxis("Horizontal");
		float leftY = Input.GetAxis("Vertical");
		Vector3 characterOffset = followXForm.position + new Vector3(0f, distanceUp, 0f);
		Vector3 lookAt = characterOffset;

		// Determine camera state
		if(Input.GetAxis("Target") > TARGETING_THRESHOLD)
		{
			barEffect.coverage = Mathf.SmoothStep(barEffect.coverage, widescreen, targetingTime);

			camState = CamStates.Target;
		}
		else
		{
			barEffect.coverage = Mathf.SmoothStep(barEffect.coverage, 0f, targetingTime);

			// * First Person *
			if(rightY > firstPersonThreshold && !follow.IsInLocomotion())
			{
				// Reset look before entering the first person mode
				xAxisRot = 0;
				lookWeight = 0f;
				camState = CamStates.FirstPerson;
			}

			// *Behind the Back *
			if((camState == CamStates.FirstPerson && Input.GetButton("ExitFPV")) ||
				(camState == CamStates.Target && (Input.GetAxis("Target") <= TARGETING_THRESHOLD)))
			{
				camState = CamStates.Behind;
			}

		}

		// Get the Look At Weight - amount to use look at IK vs using the head's animation
		follow.Animator.SetLookAtWeight(lookWeight);

		// Execute Camera State
		switch(camState)
		{
			case CamStates.Behind:
				ResetCamera();	
				// Calculate direction from camera to player, kill Y, and normalize to give a valid direction with unity magnitude.
				lookDir = characterOffset - this.transform.position;
				lookDir.y = 0;
				lookDir.Normalize();
				Debug.DrawRay(this.transform.position, lookDir, Color.green);

				targetPosition = characterOffset + followXForm.up * distanceUp - lookDir * distanceAway;
				Debug.DrawLine(followXForm.position, targetPosition, Color.magenta);
				break;
			case CamStates.Target:
				ResetCamera();
				lookDir = followXForm.forward;
				targetPosition = characterOffset + followXForm.up * distanceUp - lookDir * distanceAway;
				break;
			case CamStates.FirstPerson:
				// Looking up and down
				// Calculate the amount of rotation and apply to the firstPersonCamPos GameObject
				xAxisRot += (leftY * firstPersonLookSpeed);
				xAxisRot = Mathf.Clamp(xAxisRot, firstPersonXAxisClamp.x, firstPersonXAxisClamp.y);
				firstPersonCamPos.XForm.localRotation = Quaternion.Euler(xAxisRot, 0, 0);
				
				// Superimpose firstPersonCam's Game Object on camera
				Quaternion rotationShift = Quaternion.FromToRotation(this.transform.forward, firstPersonCamPos.XForm.forward);
				this.transform.rotation = rotationShift * this.transform.rotation;

				// Mover character's Head
				follow.Animator.SetLookAtPosition(firstPersonCamPos.XForm.position + firstPersonCamPos.XForm.forward);
				lookWeight = Mathf.Lerp (lookWeight, 1.0f, Time.deltaTime * firstPersonLookSpeed);

				// Looking Left and Right
				// Similarily to how character is rotated while in locomotion, use Quaternion + to add rotation to character
				// INFO: Lerp doesn't work with negitive values, so use .Abs (which makes all values positive to my knowledge)

				Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, fPSRotationDegreePerSecond * (leftX < 0f ? -1f : 1f), 0f), Mathf.Abs(leftX));
				Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
				follow.transform.rotation = (follow.transform.rotation * deltaRotation);

				// Move camera to firstPersonCamPos
				targetPosition = firstPersonCamPos.XForm.position;

				// Smoothy transform look direciton towards firstPersonCamPos when entering first person mode.
				lookAt = Vector3.Lerp(targetPosition + followXForm.forward, this.transform.position + this.transform.forward, camSmoothDampTime * Time.deltaTime);
				Debug.DrawRay(Vector3.zero, lookAt, Color.black);
				Debug.DrawRay(Vector3.zero, targetPosition + followXForm.forward, Color.white);
				Debug.DrawRay(Vector3.zero, firstPersonCamPos.XForm.position + firstPersonCamPos.XForm.forward, Color.cyan);


				// Choose lookAt target based on distance
				lookAt = Vector3.Lerp (this.transform.position + this.transform.forward, lookAt, Vector3.Distance(this.transform.position, firstPersonCamPos.XForm.position));
				break;
		}



		CompensateForWalls(characterOffset, ref targetPosition);
		smoothPosition(this.transform.position, targetPosition);

		// Make sure the camera is looking the right way!
		transform.LookAt(lookAt);
	}
	
	#endregion
	
	#region Methods

	private void smoothPosition(Vector3 fromPos, Vector3 toPos)
	{
		// Making a smoooth transition between camer;s  current position and the position it wants to be in.
		this.transform.position = Vector3.SmoothDamp(fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);

	}

	private void CompensateForWalls(Vector3 fromObject, ref Vector3 toTarget)
	{
		Debug.DrawLine(fromObject, toTarget, Color.cyan);
		// Compensate for walls between camera
		RaycastHit wallHit = new RaycastHit();
		if(Physics.Linecast(fromObject, toTarget, out wallHit))
		{
			Debug.DrawRay(wallHit.point, Vector3.left, Color.red);
			toTarget = new Vector3(wallHit.point.x, toTarget.y, wallHit.point.z);
		}
	}

	private void ResetCamera()
	{
		lookWeight = Mathf.Lerp(lookWeight, 0.0f, Time.deltaTime * firstPersonLookSpeed);
		transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime);
	}

	#endregion
}














