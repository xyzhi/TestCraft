/// <summary>
/// Player controller.
/// </summary>
using UnityEngine;
using System.Collections;
using UnityEngine.XR;
using HWRWeaponSystem;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AirStrikeKit
{
	[RequireComponent (typeof(FlightSystem))]

	public class PlayerController : MonoBehaviour
	{
	
		FlightSystem flight;
		// Core plane system
		FlightView View;
		public bool Active = true;
		public bool SimpleControl;
		// make it easy to control Plane will turning easier.
		public bool Acceleration;
		// Mobile*** enabled gyroscope controller
		public float AccelerationSensitivity = 5;
		// Mobile*** gyroscope sensitivity
		private TouchScreenVal controllerTouch;
		// Mobile*** move
		private TouchScreenVal fireTouch;
		// Mobile*** fire
		private TouchScreenVal switchTouch;
		// Mobile*** swich
		private TouchScreenVal sliceTouch;
		// Mobile*** slice
		public GUISkin skin;
		public bool ShowHowto;
		[Header ("VR")]
		public VRAimProvider VRAimProvider;
		public float VRAxisSensitivity = 1.2f;
		public float VRYawSensitivity = 1.2f;
		public float VRTriggerThreshold = 0.6f;
		public float VRJoystickDeadZone = 0.2f;
		public bool VRUseRightControllerAiming = true;
		public bool DisableLegacyHUDInVR = true;

		private UnityEngine.XR.InputDevice leftHandDevice;
		private UnityEngine.XR.InputDevice rightHandDevice;
		private bool vrSwitchWeaponPressed;
		private bool vrPausePressed;
		private bool vrViewPressed;
		private bool useVrInput;
		#if ENABLE_INPUT_SYSTEM
		private InputAction leftPrimary2DAxisAction;
		private InputAction rightPrimary2DAxisAction;
		private InputAction rightTriggerAction;
		private InputAction leftPrimaryButtonAction;
		private InputAction rightPrimaryButtonAction;
		private InputAction leftMenuButtonAction;
		private InputAction rightSecondaryButtonAction;
		private InputAction leftSecondaryButtonAction;
		#endif

		public bool IsVRActive {
			get {
				return useVrInput;
			}
		}

		void Awake(){
			AirStrikeGame.playerController = this;
		}

		void Start ()
		{
			flight = this.GetComponent<FlightSystem> ();
			View = (FlightView)GameObject.FindObjectOfType (typeof(FlightView));
			// setting all Touch screen controller in the position
			controllerTouch = new TouchScreenVal (new Rect (0, 0, Screen.width / 2, Screen.height - 100));
			fireTouch = new TouchScreenVal (new Rect (Screen.width / 2, 0, Screen.width / 2, Screen.height));
			switchTouch = new TouchScreenVal (new Rect (0, Screen.height - 100, Screen.width / 2, 100));
			sliceTouch = new TouchScreenVal (new Rect (0, 0, Screen.width / 2, 50));
			if (!VRAimProvider) {
				VRAimProvider = GameObject.FindObjectOfType<VRAimProvider> ();
			}
			if (!VRAimProvider) {
				VRAimProvider = CreateOrFindVRAimProvider ();
			}
			SetupVRActions ();
			RefreshVRState ();

		}

		void OnDestroy ()
		{
			DisposeVRActions ();
		}

		void Update ()
		{
			if (!flight || !Active)
				return;

			RefreshVRState ();
			if (useVrInput) {
				VRController ();
				return;
			}
			#if UNITY_EDITOR || UNITY_WEBPLAYER || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
			// On Desktop
			DesktopController ();
			#else
			// On Mobile device
			MobileController ();
			#endif
		
		}

		void RefreshVRState ()
		{
			bool xrDetected = XRSettings.isDeviceActive;
			useVrInput = xrDetected || IsEditorSimulatorActive ();
			if (!useVrInput)
				return;

			if (!leftHandDevice.isValid) {
				leftHandDevice = InputDevices.GetDeviceAtXRNode (XRNode.LeftHand);
			}
			if (!rightHandDevice.isValid) {
				rightHandDevice = InputDevices.GetDeviceAtXRNode (XRNode.RightHand);
			}

			if (!VRAimProvider) {
				VRAimProvider = GameObject.FindObjectOfType<VRAimProvider> ();
			}
			if (!VRAimProvider) {
				VRAimProvider = CreateOrFindVRAimProvider ();
			}
			if (VRAimProvider) {
				VRAimProvider.CurrentAimSource = VRUseRightControllerAiming ? VRAimProvider.AimSource.RightController : VRAimProvider.AimSource.Head;
				VRAimProvider.RefreshReferences ();
			}
			ApplyVRAimingTargets ();
		}

		bool IsEditorSimulatorActive ()
		{
			#if UNITY_EDITOR
			return GameObject.Find ("XR Device Simulator") != null;
			#else
			return false;
			#endif
		}

		void ApplyVRAimingTargets ()
		{
			if (!flight || flight.WeaponControl == null || VRAimProvider == null)
				return;

			WeaponLauncher[] launchers = flight.WeaponControl.WeaponLists;
			if (launchers == null)
				return;

			Transform aimTransform = VRAimProvider.GetAimTransform ();
			Camera aimCamera = VRAimProvider.GetAimCamera ();
			for (int i = 0; i < launchers.Length; i++) {
				if (launchers [i] == null)
					continue;
				launchers [i].AimOverride = aimTransform;
				launchers [i].AimCameraOverride = aimCamera;
				if (DisableLegacyHUDInVR) {
					launchers [i].ShowHUD = false;
					launchers [i].ShowCrosshair = false;
				}
			}
		}

		void VRController ()
		{
			flight.SimpleControl = false;
			flight.FixedX = false;
			flight.FixedY = false;
			flight.FixedZ = false;
			MouseLock.MouseLocked = false;

			Vector2 leftAxis = ReadVRPrimary2DAxis (leftHandDevice, leftPrimary2DAxisAction);
			Vector2 rightAxis = ReadVRPrimary2DAxis (rightHandDevice, rightPrimary2DAxisAction);
			leftAxis = ApplyDeadZone (leftAxis);
			rightAxis = ApplyDeadZone (rightAxis);

			flight.AxisControl (new Vector2 (rightAxis.x, -leftAxis.y) * VRAxisSensitivity);
			flight.TurnControl (leftAxis.x * VRYawSensitivity);
			flight.SpeedUp (rightAxis.y);

			float triggerValue = ReadVRTrigger (rightHandDevice, rightTriggerAction);
			bool firePressed = triggerValue >= VRTriggerThreshold;
			if (firePressed) {
				flight.WeaponControl.LaunchWeapon ();
			}

			bool switchPressed = ReadVRButton (rightHandDevice, UnityEngine.XR.CommonUsages.primaryButton, rightPrimaryButtonAction) || ReadVRButton (leftHandDevice, UnityEngine.XR.CommonUsages.primaryButton, leftPrimaryButtonAction);
			if (switchPressed && !vrSwitchWeaponPressed) {
				flight.WeaponControl.SwitchWeapon ();
			}
			vrSwitchWeaponPressed = switchPressed;

			bool pausePressed = ReadVRButton (leftHandDevice, UnityEngine.XR.CommonUsages.menuButton, leftMenuButtonAction) || ReadVRButton (rightHandDevice, UnityEngine.XR.CommonUsages.secondaryButton, rightSecondaryButtonAction);
			if (pausePressed && !vrPausePressed && AirStrikeGame.gameUI) {
				AirStrikeGame.gameUI.TogglePause ();
			}
			vrPausePressed = pausePressed;

			bool viewPressed = ReadVRButton (leftHandDevice, UnityEngine.XR.CommonUsages.secondaryButton, leftSecondaryButtonAction);
			if (viewPressed && !vrViewPressed && View) {
				View.SwitchCameras ();
			}
			vrViewPressed = viewPressed;
		}

		Vector2 ReadVRPrimary2DAxis (UnityEngine.XR.InputDevice device, InputAction action)
		{
			#if ENABLE_INPUT_SYSTEM
			if (action != null && action.enabled) {
				return action.ReadValue<Vector2> ();
			}
			#endif
			return ReadAxis2D (device, UnityEngine.XR.CommonUsages.primary2DAxis);
		}

		float ReadVRTrigger (UnityEngine.XR.InputDevice device, InputAction action)
		{
			#if ENABLE_INPUT_SYSTEM
			if (action != null && action.enabled) {
				return action.ReadValue<float> ();
			}
			#endif
			return ReadAxis1D (device, UnityEngine.XR.CommonUsages.trigger);
		}

		Vector2 ReadAxis2D (UnityEngine.XR.InputDevice device, InputFeatureUsage<Vector2> usage)
		{
			Vector2 value;
			if (device.isValid && device.TryGetFeatureValue (usage, out value)) {
				return value;
			}
			return Vector2.zero;
		}

		float ReadAxis1D (UnityEngine.XR.InputDevice device, InputFeatureUsage<float> usage)
		{
			float value;
			if (device.isValid && device.TryGetFeatureValue (usage, out value)) {
				return value;
			}
			return 0;
		}

		bool ReadButton (UnityEngine.XR.InputDevice device, InputFeatureUsage<bool> usage)
		{
			bool value;
			if (device.isValid && device.TryGetFeatureValue (usage, out value)) {
				return value;
			}
			return false;
		}

		bool ReadVRButton (UnityEngine.XR.InputDevice device, InputFeatureUsage<bool> usage, InputAction action)
		{
			#if ENABLE_INPUT_SYSTEM
			if (action != null && action.enabled) {
				return action.IsPressed ();
			}
			#endif
			return ReadButton (device, usage);
		}

		void SetupVRActions ()
		{
			#if ENABLE_INPUT_SYSTEM
			if (leftPrimary2DAxisAction != null)
				return;

			leftPrimary2DAxisAction = CreateVRValueAction ("Left Primary 2D Axis", "<XRController>{LeftHand}/primary2DAxis");
			rightPrimary2DAxisAction = CreateVRValueAction ("Right Primary 2D Axis", "<XRController>{RightHand}/primary2DAxis");
			rightTriggerAction = CreateVRValueAction ("Right Trigger", "<XRController>{RightHand}/trigger");
			leftPrimaryButtonAction = CreateVRButtonAction ("Left Primary Button", "<XRController>{LeftHand}/primaryButton");
			rightPrimaryButtonAction = CreateVRButtonAction ("Right Primary Button", "<XRController>{RightHand}/primaryButton");
			leftMenuButtonAction = CreateVRButtonAction ("Left Menu Button", "<XRController>{LeftHand}/menuButton");
			rightSecondaryButtonAction = CreateVRButtonAction ("Right Secondary Button", "<XRController>{RightHand}/secondaryButton");
			leftSecondaryButtonAction = CreateVRButtonAction ("Left Secondary Button", "<XRController>{LeftHand}/secondaryButton");
			#endif
		}

		void DisposeVRActions ()
		{
			#if ENABLE_INPUT_SYSTEM
			DisposeAction (leftPrimary2DAxisAction);
			DisposeAction (rightPrimary2DAxisAction);
			DisposeAction (rightTriggerAction);
			DisposeAction (leftPrimaryButtonAction);
			DisposeAction (rightPrimaryButtonAction);
			DisposeAction (leftMenuButtonAction);
			DisposeAction (rightSecondaryButtonAction);
			DisposeAction (leftSecondaryButtonAction);
			leftPrimary2DAxisAction = null;
			rightPrimary2DAxisAction = null;
			rightTriggerAction = null;
			leftPrimaryButtonAction = null;
			rightPrimaryButtonAction = null;
			leftMenuButtonAction = null;
			rightSecondaryButtonAction = null;
			leftSecondaryButtonAction = null;
			#endif
		}

		#if ENABLE_INPUT_SYSTEM
		InputAction CreateVRValueAction (string actionName, string bindingPath)
		{
			InputAction action = new InputAction (actionName, InputActionType.Value, bindingPath);
			action.Enable ();
			return action;
		}

		InputAction CreateVRButtonAction (string actionName, string bindingPath)
		{
			InputAction action = new InputAction (actionName, InputActionType.Button, bindingPath);
			action.Enable ();
			return action;
		}

		void DisposeAction (InputAction action)
		{
			if (action == null)
				return;
			action.Disable ();
			action.Dispose ();
		}
		#endif

		Vector2 ApplyDeadZone (Vector2 axis)
		{
			if (axis.magnitude < VRJoystickDeadZone)
				return Vector2.zero;
			float normalizedMagnitude = Mathf.InverseLerp (VRJoystickDeadZone, 1, axis.magnitude);
			return axis.normalized * normalizedMagnitude;
		}

		VRAimProvider CreateOrFindVRAimProvider ()
		{
			Camera mainCamera = Camera.main;
			if (!mainCamera) {
				mainCamera = GameObject.FindObjectOfType<Camera> ();
			}

			GameObject host = mainCamera != null ? mainCamera.gameObject : this.gameObject;
			VRAimProvider provider = host.GetComponent<VRAimProvider> ();
			if (!provider) {
				provider = host.AddComponent<VRAimProvider> ();
			}
			return provider;
		}

		void DesktopController ()
		{
			// Desktop controller
			flight.SimpleControl = SimpleControl;
		
			// lock mouse position to the center.
			MouseLock.MouseLocked = true;
		
			flight.AxisControl (new Vector2 (Input.GetAxis ("Mouse X"), Input.GetAxis ("Mouse Y")));

			if (SimpleControl) {
				flight.TurnControl (Input.GetAxis ("Mouse X"));
			} 

			flight.TurnControl (Input.GetAxis ("Horizontal"));
			flight.SpeedUp (Input.GetAxis ("Vertical"));
		
		
			if (Input.GetButton ("Fire1")) {
				flight.WeaponControl.LaunchWeapon ();
			}
		
			if (Input.GetButtonDown ("Fire2")) {
				flight.WeaponControl.SwitchWeapon ();
			}
		
			if (Input.GetKeyDown (KeyCode.C)) {
				if (View)
					View.SwitchCameras ();	
			}	
		}

		void MobileController ()
		{
			// Mobile controller
		
			flight.SimpleControl = SimpleControl;
		
			if (Acceleration) {
				// get axis control from device acceleration
				Vector3 acceleration = Input.acceleration;
				Vector2 accValActive = new Vector2 (acceleration.x, (acceleration.y + 0.3f) * 0.5f) * AccelerationSensitivity;
				flight.FixedX = false;
				flight.FixedY = false;
				flight.FixedZ = true;
			
				flight.AxisControl (accValActive);
				flight.TurnControl (accValActive.x);
			} else {
				flight.FixedX = true;
				flight.FixedY = false;
				flight.FixedZ = true;
				// get axis control from touch screen
				Vector2 dir = controllerTouch.OnDragDirection (true);
				dir = Vector2.ClampMagnitude (dir, 1.0f);
				flight.AxisControl (new Vector2 (dir.x, -dir.y) * AccelerationSensitivity * 0.7f);
				flight.TurnControl (dir.x * AccelerationSensitivity * 0.3f);
			}
			sliceTouch.OnDragDirection (true);
			// slice speed
			flight.SpeedUp (sliceTouch.slideVal.x);
		
			if (fireTouch.OnTouchPress ()) {
				flight.WeaponControl.LaunchWeapon ();
			}	
			if (switchTouch.OnTouchPress ()) {
		
			}	
		}
	
	
		// you can remove this part..
		void OnGUI ()
		{
			if (!ShowHowto)
				return;
		
			if (skin)
				GUI.skin = skin;
		
			if (GUI.Button (new Rect (20, 150, 200, 40), "Gyroscope " + Acceleration)) {
				Acceleration = !Acceleration;
			}
		
			if (GUI.Button (new Rect (20, 200, 200, 40), "Change View")) {
				if (View)
					View.SwitchCameras ();	
			}

			if (useVrInput) {
				GUI.Label (new Rect (20, 390, 500, 40), "VR Left Stick : Yaw/Pitch  Right Stick : Roll/3-Speed  Trigger : Fire");
			}
		
			if (GUI.Button (new Rect (20, 250, 200, 40), "Change Weapons")) {
				if (flight)
					flight.WeaponControl.SwitchWeapon ();
			}
		
			if (GUI.Button (new Rect (20, 300, 200, 40), "Simple Control " + SimpleControl)) {
				if (flight)
					SimpleControl = !SimpleControl;
			}

			GUI.Label (new Rect (20, 350, 500, 40), "you can remove this in OnGUI in PlayerController.cs");
		}
	}
}
