/// <summary>
/// Flight system. This script is Core plane system
/// </summary>
using UnityEngine;
using System.Collections;
using HWRWeaponSystem;

namespace AirStrikeKit
{
	// included all necessary component
	[RequireComponent (typeof(Rigidbody))]
	[RequireComponent (typeof(Collider))]
	[RequireComponent (typeof(WeaponController))]


	public class FlightSystem : DamageManager
	{
	
		public float AccelerationSpeed = 0.5f;
		// acceleration speed per frame
		public float Speed = 50.0f;
		// Speed
		public float SpeedMax = 60.0f;
		// Max speed
		public float ReverseSpeed = -30.0f;
		// Reverse speed
		public float SpeedInputDeadZone = 0.2f;
		// Input dead zone for 3-speed mode
		public float RotationSpeed = 10.0f;
		// Turn Speed
		public float SpeedPitch = 2;
		// rotation X
		public float SpeedRoll = 3;
		// rotation Z
		public float SpeedYaw = 1;
		// rotation Y
		public float DampingTarget = 10.0f;
		// rotation speed to facing to a target
		public bool AutoPilot = false;
		// if True this plane will follow a target automatically
		private float MoveSpeed = 0;
		// current move speed target
		public float VelocitySpeed = 0;

		[HideInInspector]
		public bool SimpleControl = false;
		// set true is enabled casual controling
		[HideInInspector]
		public bool FollowTarget = false;
		[HideInInspector]
		public Vector3 PositionTarget = Vector3.zero;
		// current target position
		[HideInInspector]
		public WeaponController WeaponControl;
		// weapon system
		private Vector3 positionTarget = Vector3.zero;
		private Quaternion mainRot = Quaternion.identity;
		[HideInInspector]
		public float roll = 0;
		[HideInInspector]
		public float pitch = 0;
		[HideInInspector]
		public float yaw = 0;
		public Vector2 LimitAxisControl = new Vector2 (2, 1);
		// limited of axis rotation magnitude
		public bool FixedX;
		public bool FixedY;
		public bool FixedZ;

		void Start ()
		{
			// define all component
			WeaponControl = this.gameObject.GetComponent<WeaponController> ();
			mainRot = this.transform.rotation;
			MoveSpeed = Speed;
			GetComponent<Rigidbody>().velocity = Vector3.zero;
			GetComponent<Rigidbody>().useGravity = false;
		}

		float SignedAngleBetween (Vector3 a, Vector3 b, Vector3 n)
		{
			// angle in [0,180]
			float angle = Vector3.Angle (a, b);
			float sign = Mathf.Sign (Vector3.Dot (n, Vector3.Cross (a, b)));

			// angle in [-179,180]
			float signed_angle = angle * sign;

			// angle in [0,360] (not used but included here for completeness)
			//float angle360 =  (signed_angle + 180) % 360;

			return signed_angle;
		}


		void FixedUpdate ()
		{
			if (!this.GetComponent<Rigidbody>())
				return;

			Quaternion AddRot = Quaternion.identity;
			Vector3 velocityTarget = Vector3.zero;
			if (AutoPilot) {// if auto pilot
				if (FollowTarget) {
					// rotation facing to the positionTarget
					Vector3 targetDir = PositionTarget - this.transform.position;
					float directToGo = Vector3.Dot (this.transform.forward.normalized, targetDir.normalized);
					float rotateMult = 1;

			
					if (directToGo > 0.3f) {
						rotateMult = 1;
						positionTarget = Vector3.Lerp (positionTarget, PositionTarget, Time.fixedDeltaTime * DampingTarget);
					} else {
						Vector3 reflectionVector = Vector3.Reflect (targetDir.normalized, this.transform.forward);
						reflectionVector.x *= 0.3f;
						reflectionVector.y *= 0.5f;
						positionTarget = Vector3.Slerp (positionTarget, this.transform.position + reflectionVector, Time.fixedDeltaTime * DampingTarget);
						rotateMult = 0.5f;
						if (directToGo < -0.9) {
							positionTarget.y += 0.1f;
						}
					}

					mainRot = Quaternion.LookRotation (positionTarget - this.transform.position);
					Vector3 relativePoint = this.transform.InverseTransformPoint (positionTarget).normalized;
					this.GetComponent<Rigidbody>().rotation = Quaternion.Lerp (GetComponent<Rigidbody>().rotation, mainRot, rotateMult * 0.1f * RotationSpeed * Time.fixedDeltaTime);
					this.GetComponent<Rigidbody>().rotation *= Quaternion.Euler (0, 0, -relativePoint.x * 150 * SpeedRoll * Time.fixedDeltaTime);

				}
				velocityTarget = (GetComponent<Rigidbody>().rotation * Vector3.forward) * VelocitySpeed;
			} else {
				// axis control by input
				AddRot.eulerAngles = new Vector3 (pitch, yaw, -roll);

				mainRot *= AddRot;

				if (SimpleControl) {
					Quaternion saveQ = mainRot;
				
					Vector3 fixedAngles = new Vector3 (mainRot.eulerAngles.x, mainRot.eulerAngles.y, mainRot.eulerAngles.z);
				
					if (FixedX)
						fixedAngles.x = 1;
					if (FixedY)
						fixedAngles.y = 1;
					if (FixedZ)
						fixedAngles.z = 1;
				
					saveQ.eulerAngles = fixedAngles;
					mainRot = Quaternion.Lerp (mainRot, saveQ, Time.fixedDeltaTime * 2);
				}

				velocityTarget = (GetComponent<Rigidbody>().rotation * Vector3.forward) * VelocitySpeed;
				GetComponent<Rigidbody>().rotation = Quaternion.Lerp (GetComponent<Rigidbody>().rotation, mainRot, Time.fixedDeltaTime * RotationSpeed);
			}

			yaw = Mathf.Lerp (yaw, 0, Time.deltaTime);
			Vector3 velocityChange = (velocityTarget - GetComponent<Rigidbody>().velocity);
			GetComponent<Rigidbody>().AddForce (velocityChange, ForceMode.VelocityChange);

			if (speedMode > 0) {
				MoveSpeed = Mathf.Lerp (MoveSpeed, SpeedMax, Time.fixedDeltaTime * AccelerationSpeed);
			} else if (speedMode < 0) {
				MoveSpeed = Mathf.Lerp (MoveSpeed, ReverseSpeed, Time.fixedDeltaTime * AccelerationSpeed);
			} else {
				MoveSpeed = Mathf.Lerp (MoveSpeed, Speed, Time.fixedDeltaTime * 0.1f);
			}
			VelocitySpeed = MoveSpeed;
		}

		// Input function. ( roll and pitch)
		public void AxisControl (Vector2 axis)
		{
			if (SimpleControl) {
				LimitAxisControl.y = LimitAxisControl.x;	
			}
			roll = Mathf.Lerp (roll, Mathf.Clamp (axis.x, -LimitAxisControl.x, LimitAxisControl.x) * SpeedRoll, Time.deltaTime);
			pitch = Mathf.Lerp (pitch, Mathf.Clamp (axis.y, -LimitAxisControl.y, LimitAxisControl.y) * SpeedPitch, Time.deltaTime);
		}

		// Input function ( yaw)
		public void TurnControl (float turn)
		{
			yaw += turn * Time.deltaTime * SpeedYaw;
		}

		private float speedDelta;
		private int speedMode;
		// Speed up
		public void SpeedUp (float delta)
		{
			speedDelta = delta;
			if (delta > SpeedInputDeadZone) {
				speedMode = 1;
			} else if (delta < -SpeedInputDeadZone) {
				speedMode = -1;
			} else {
				speedMode = 0;
			}
		}

		public void SpeedUp ()
		{
			speedMode = 1;
			MoveSpeed = Mathf.Lerp (MoveSpeed, SpeedMax, Time.deltaTime * AccelerationSpeed);
			speedDelta = 1;
		}
	}
}
